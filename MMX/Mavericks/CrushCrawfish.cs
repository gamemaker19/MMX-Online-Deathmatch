namespace MMXOnline
{
    public class CrushCrawfish : Maverick
    {
        public static Weapon getWeapon() { return new Weapon(WeaponIds.CrushCGeneric, 155); }
        public static Weapon getMeleeWeapon(Player player) { return new Weapon(WeaponIds.CrushCGeneric, 155, new Damager(player, 1, 0, 0)); }

        public Weapon meleeWeapon;
        public CrushCrawfish(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            stateCooldowns.Add(typeof(MShoot), new MaverickStateCooldown(false, true, 0.75f));
            stateCooldowns.Add(typeof(CrushCShootArmState), new MaverickStateCooldown(false, true, 0.75f));
            stateCooldowns.Add(typeof(CrushCDashState), new MaverickStateCooldown(false, false, 0.5f));

            weapon = getWeapon();
            meleeWeapon = getMeleeWeapon(player);

            awardWeaponId = WeaponIds.SpinningBlade;
            weakWeaponId = WeaponIds.TriadThunder;
            weakMaverickWeaponId = WeaponIds.VoltCatfish;

            netActorCreateId = NetActorCreateId.CrushCrawfish;
            netOwner = player;
            if (sendRpc)
            {
                createActorRpc(player.id);
            }
        }

        public override void update()
        {
            base.update();
            if (aiBehavior == MaverickAIBehavior.Control)
            {
                if (state is MIdle || state is MRun)
                {
                    if (input.isPressed(Control.Shoot, player))
                    {
                        changeState(new CrushCShootArmState());
                    }
                    else if (input.isPressed(Control.Special1, player))
                    {
                        changeState(getShootState(false));
                    }
                    else if (input.isPressed(Control.Dash, player))
                    {
                        changeState(new CrushCDashState());
                    }
                }
                else if (state is MJump || state is MFall)
                {
                }
            }
        }

        public override string getMaverickPrefix()
        {
            return "crushc";
        }
    
        public override MaverickState getRandomAttackState()
        {
            return aiAttackStates().GetRandomItem();
        }

        public override MaverickState[] aiAttackStates()
        {
            return new MaverickState[]
            {
                new CrushCShootArmState(),
                getShootState(true),
                new CrushCDashState(),
            };
        }

        public MaverickState getShootState(bool isAI)
        {
            var mshoot = new MShoot((Point pos, int xDir) =>
            {
                playSound("crushcShoot", sendRpc: true);
                new CrushCProj(weapon, pos, xDir, player, player.getNextActorNetId(), sendRpc: true);
            }, null);
            if (isAI)
            {
                mshoot.consecutiveData = new MaverickStateConsecutiveData(0, 4, 0.75f);
            }
            return mshoot;
        }

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            if (sprite.name.EndsWith("_dash"))
            {
                return new GenericMeleeProj(weapon, centerPoint, ProjIds.CrushCGrab, player, damage: 0, flinch: 0, hitCooldown: 0, owningActor: this);
            }
            return null;
        }

    }

    public class CrushCProj : Projectile
    {
        bool once;
        public CrushCProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, xDir, 200, 3, player, "crushc_proj", 0, 0.01f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.CrushCProj;
            maxDistance = 150f;

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            base.onHitDamagable(damagable);
            if (damagable is Character chr)
            {
                chr.vel = Point.lerp(chr.vel, Point.zero, Global.spf * 10);
                chr.slowdownTime = 0.25f;
            }
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            if (!once)
            {
                once = true;
                var normal = other?.hitData?.normal;
                if (normal != null)
                {
                    if (normal.Value.x == 0)
                    {
                        normal = new Point(-1, 0);
                    }
                    normal = ((Point)normal).leftNormal();
                }
                else
                {
                    normal = new Point(0, 1);
                    return;
                }
                vel = normal.Value.times(speed);
                if (vel.y > 0) vel = vel.times(-1);
            }
        }
    }

    public class CrushCArmProj : Projectile
    {
        public int state;
        Point moveDir;
        float moveDistance2;
        float maxDistance2 = 100;
        CrushCrawfish cc;
        public CrushCArmProj(Weapon weapon, Point pos, int xDir, Point moveDir, CrushCrawfish cc, Player player, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, xDir, 0, 4, player, getSprite(moveDir), Global.defFlinch, 1f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.CrushCArmProj;
            this.moveDir = moveDir.normalize();
            this.cc = cc;
            speed = 200;
            setIndestructableProperties();

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public static string getSprite(Point moveDir)
        {
            if (moveDir.x != 0 && moveDir.y != 0) return "crushc_proj_claw2";
            else if (moveDir.x == 0 && moveDir.y != 0) return "crushc_proj_claw3";
            return "crushc_proj_claw";
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            if (state == 0)
            {
                move(moveDir.times(speed));
                moveDistance2 += speed * Global.spf;
                if (moveDistance2 > maxDistance2 || (cc.input.isPressed(Control.Shoot, cc.player) && time > 0.25f))
                {
                    state = 1;
                }
            }
            else if (state == 1)
            {
                move(moveDir.times(-speed));
                moveDistance2 += speed * Global.spf;
                if (moveDistance2 > maxDistance2 * 2 || pos.distanceTo(cc.getFirstPOIOrDefault()) < 10)
                {
                    state = 2;
                    destroySelf();
                }
            }
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            if (!ownedByLocalPlayer) return;

            state = 1;
        }
    }

    public class CrushCShootArmState : MaverickState
    {
        CrushCArmProj proj;
        public CrushCShootArmState() : base("attack_claw", "")
        {
            superArmor = true;
        }

        public override void update()
        {
            base.update();

            Point? shootPos = maverick.getFirstPOI();
            if (!once && shootPos != null)
            {
                once = true;
                maverick.playSound("crushcClaw", sendRpc: true);
                var inputDir = input.getInputDir(player);
                if (inputDir.y > 0) inputDir.y = 0;
                if (inputDir.isZero()) inputDir = new Point(maverick.xDir, 0);
                proj = new CrushCArmProj(maverick.weapon, shootPos.Value, maverick.xDir, inputDir, maverick as CrushCrawfish, player, player.getNextActorNetId(), sendRpc: true);
            }

            if (proj != null && proj.destroyed)
            {
                maverick.changeToIdleOrFall();
            }
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            proj?.destroySelf();
        }
    }

    public class CrushCDashState : MaverickState
    {
        float dustTime;
        float ftdWaitTime;
        public CrushCDashState() : base("dash", "dash_start")
        {
        }

        public override void update()
        {
            base.update();
            if (inTransition()) return;

            if (ftdWaitTime > 0)
            {
                tryChangeToIdleOrFall();
                return;
            }

            Helpers.decrementTime(ref dustTime);
            if (dustTime == 0)
            {
                new Anim(maverick.pos.addxy(-maverick.xDir * 10, 0), "dust", maverick.xDir, null, true);
                dustTime = 0.075f;
            }

            if (!player.ownedByLocalPlayer) return;

            var move = new Point(150 * maverick.xDir, 0);

            var hitGround = Global.level.checkCollisionActor(maverick, move.x * Global.spf * 5, 20);
            if (hitGround == null)
            {
                tryChangeToIdleOrFall();
                return;
            }

            maverick.move(move);

            if (isHoldStateOver(0.25f, 1f, 0.75f, Control.Dash))
            {
                tryChangeToIdleOrFall();
                return;
            }
        }

        public void tryChangeToIdleOrFall()
        {
            if (player.isDefenderFavored)
            {
                ftdWaitTime += Global.spf;
                if (ftdWaitTime < 0.25f) return;
            }
            maverick.changeToIdleOrFall();
        }

        public override bool trySetGrabVictim(Character grabbed)
        {
            maverick.changeState(new CrushCGrabState(grabbed));
            return true;
        }
    }
    
    public class CrushCGrabState : MaverickState
    {
        Character victim;
        float hurtTime;
        public bool victimWasGrabbedSpriteOnce;
        float timeWaiting;
        public CrushCGrabState(Character grabbedChar) : base("grab_attack", "")
        {
            victim = grabbedChar;
        }

        public override void update()
        {
            base.update();
            if (!victimWasGrabbedSpriteOnce)
            {
                maverick.frameSpeed = 0;
            }
            else
            {
                maverick.frameSpeed = 1;
                if (maverick.frameIndex > 0)
                {
                    Helpers.decrementTime(ref hurtTime);
                    if (hurtTime == 0)
                    {
                        hurtTime = 0.16666f;
                        (maverick as CrushCrawfish).meleeWeapon.applyDamage(victim, false, maverick, (int)ProjIds.CrushCGrabAttack);
                    }
                }
            }

            if (victimWasGrabbedSpriteOnce && !victim.sprite.name.EndsWith("_grabbed"))
            {
                maverick.changeToIdleOrFall();
                return;
            }

            if (victim.sprite.name.EndsWith("_grabbed") || victim.sprite.name.EndsWith("_die"))
            {
                victimWasGrabbedSpriteOnce = true;
            }
            if (!victimWasGrabbedSpriteOnce)
            {
                timeWaiting += Global.spf;
                if (timeWaiting > 1)
                {
                    victimWasGrabbedSpriteOnce = true;
                }
            }

            if (stateTime > CrushCGrabbed.maxGrabTime)
            {
                maverick.changeToIdleOrFall();
            }
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            victim?.releaseGrab(maverick);
        }
    }

    public class CrushCGrabbed : GenericGrabbedState
    {
        public const float maxGrabTime = 4;
        public CrushCGrabbed(CrushCrawfish grabber) : base(grabber, maxGrabTime, "grab_attack", maxNotGrabbedTime: 1f)
        {
        }
    }
}
