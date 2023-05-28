namespace MMXOnline
{
    public class NeonTiger : Maverick
    {
        public static Weapon getWeapon() { return new Weapon(WeaponIds.NeonTGeneric, 156); }

        public const float pounceSpeed = 275;
        public const float wallPounceSpeed = 325;
        public int shootNum;
        public bool isDashing;
        public NeonTiger(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            stateCooldowns.Add(typeof(NeonTClawState), new MaverickStateCooldown(false, true, 0.33f));
            stateCooldowns.Add(typeof(MShoot), new MaverickStateCooldown(false, true, 0.33f));
            // stateCooldowns.Add(typeof(NeonTDashState), new MaverickStateCooldown(false, true, 0.5f));

            weapon = getWeapon();

            canClimbWall = true;

            awardWeaponId = WeaponIds.RaySplasher;
            weakWeaponId = WeaponIds.SpinningBlade;
            weakMaverickWeaponId = WeaponIds.CrushCrawfish;

            netActorCreateId = NetActorCreateId.NeonTiger;
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
                    if (input.isHeld(Control.Special1, player))
                    {
                        changeState(getShootState(false));
                    }
                    else if (input.isPressed(Control.Dash, player))
                    {
                        changeState(new NeonTDashState());
                    }
                    else if (input.isHeld(Control.Shoot, player))
                    {
                        changeState(new NeonTClawState(false));
                    }
                }
                else if (state is MJump || state is MFall || state is MWallKick || state is NeonTPounceState)
                {
                    if (input.isPressed(Control.Shoot, player))
                    {
                        changeState(new NeonTAirClawState());
                    }
                }
                else if (state is MWallSlide wallSlide)
                {
                    if (input.isHeld(Control.Shoot, player))
                    {
                        changeState(new NeonTWallClawState(wallSlide.cloneLeaveOff()));
                    }
                    else if (input.isHeld(Control.Special1, player))
                    {
                        changeState(new NeonTWallShootState(wallSlide.cloneLeaveOff()));
                    }
                }
            }
        }

        public override string getMaverickPrefix()
        {
            return "neont";
        }

        public override float getRunSpeed()
        {
            return 200f;
        }

        public override float getDashSpeed()
        {
            return 1f;
        }

        public override MaverickState getRandomAttackState()
        {
            return aiAttackStates().GetRandomItem();
        }

        public override MaverickState[] aiAttackStates()
        {
            return new MaverickState[]
            {
                new NeonTClawState(false),
                getShootState(true),
                new NeonTDashState(),
            };
        }

        public MaverickState getShootState(bool isAI)
        {
            var mshoot = new MShoot((Point pos, int xDir) =>
            {
                //playSound("neontRaySplasher", sendRpc: true);
                new NeonTRaySplasherProj(weapon, pos, xDir, shootNum, false, player, player.getNextActorNetId(), sendRpc: true);
                shootNum++;
            }, null);
            if (isAI)
            {
                mshoot.consecutiveData = new MaverickStateConsecutiveData(0, 4, 0f);
            }
            return mshoot;
        }

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            if (sprite.name == "neont_slash")
            {
                return new GenericMeleeProj(weapon, centerPoint, ProjIds.NeonTClaw, player, damage: 2, flinch: 0, hitCooldown: 0.2f, owningActor: this);
            }
            else if (sprite.name == "neont_slash2")
            {
                return new GenericMeleeProj(weapon, centerPoint, ProjIds.NeonTClaw2, player, damage: 2, flinch: Global.halfFlinch, hitCooldown: 0.25f, owningActor: this);
            }
            else if (sprite.name == "neont_jump_slash")
            {
                return new GenericMeleeProj(weapon, centerPoint, ProjIds.NeonTClawAir, player, damage: 3, flinch: Global.defFlinch, hitCooldown: 0.25f, owningActor: this);
            }
            else if (sprite.name == "neont_dash_slash")
            {
                return new GenericMeleeProj(weapon, centerPoint, ProjIds.NeonTClawDash, player, damage: 3, flinch: Global.halfFlinch, hitCooldown: 0.25f, owningActor: this);
            }
            else if (sprite.name == "neont_wall_slash")
            {
                return new GenericMeleeProj(weapon, centerPoint, ProjIds.NeonTClawWall, player, damage: 3, flinch: 0, hitCooldown: 0.25f, owningActor: this);
            }
            return null;
        }
    }

    public class NeonTRaySplasherProj : Projectile
    {
        int shootNum;
        bool isHanging;
        public NeonTRaySplasherProj(Weapon weapon, Point pos, int xDir, int shootNum, bool isHanging, Player player, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, xDir, 0, 2, player, "neont_projectile_start", 0, 0.01f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.NeonTRaySplasher;
            maxTime = 0.875f;
            this.shootNum = shootNum;
            this.isHanging = isHanging;

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            if (sprite.name.EndsWith("start"))
            {
                if (isAnimOver())
                {
                    if (!isHanging)
                    {
                        if (shootNum % 3 == 0) vel = new Point(xDir * 250, 0);
                        else if (shootNum % 3 == 1) vel = new Point(xDir * 240, 50);
                        else if (shootNum % 3 == 2) vel = new Point(xDir * 240, -50);
                    }
                    else
                    {
                        if (shootNum % 3 == 0) vel = new Point(xDir * 250, -50);
                        else if (shootNum % 3 == 1) vel = new Point(xDir * 229, 100);
                        else if (shootNum % 3 == 2) vel = new Point(xDir * 150, 200);
                    }
                    changeSprite("neont_projectile", true);
                }
            }
        }
    }

    public class NeonTWallShootState : MaverickState
    {
        MaverickState prevState;
        public NeonTWallShootState(MaverickState prevState) : base("wall_shoot", "")
        {
            useGravity = false;
            this.prevState = prevState;
        }

        public override void update()
        {
            base.update();

            Point? shootPos = maverick.getFirstPOI();
            if (!once && shootPos != null)
            {
                var nt = maverick as NeonTiger;
                once = true;
                //maverick.playSound("neontRaySplasher", sendRpc: true);
                new NeonTRaySplasherProj(maverick.weapon, shootPos.Value, maverick.xDir * -1, nt.shootNum, true, player, player.getNextActorNetId(), sendRpc: true);
                nt.shootNum++;
            }
            if (maverick.isAnimOver())
            {
                maverick.changeState(prevState, true);
            }
        }
    }

    public class NeonTWallClawState : MaverickState
    {
        MaverickState prevState;
        public NeonTWallClawState(MaverickState prevState) : base("wall_slash", "")
        {
            useGravity = false;
            this.prevState = prevState;
            enterSound = "neontSlash";
        }

        public override void update()
        {
            base.update();
            if (maverick.isAnimOver())
            {
                maverick.changeState(prevState, true);
            }
        }
    }

    public class NeonTClawState : MaverickState
    {
        bool isSecond;
        bool shootPressed;
        public NeonTClawState(bool isSecond) : base(isSecond ? "slash2" : "slash", "")
        {
            this.isSecond = isSecond;
            exitOnAnimEnd = true;
            canEnterSelf = true;
            enterSound = "neontSlash";
        }

        public override void update()
        {
            base.update();

            shootPressed = shootPressed || input.isPressed(Control.Shoot, player);
            if (!isSecond && (shootPressed || isAI) && maverick.frameIndex > 2)
            {
                maverick.sprite.restart();
                maverick.changeState(new NeonTClawState(true), true);
                return;
            }

            if (!isSecond && input.isHeld(Control.Shoot, player) && maverick.frameIndex > 2 && maverick.frameTime > 0.08f)
            {
                maverick.sprite.restart();
                maverick.changeState(new NeonTClawState(false), true);
                return;
            }
        }
    }

    public class NeonTAirClawState : MaverickState
    {
        bool wasPounce;
        bool wasWallPounce;
        public NeonTAirClawState() : base("jump_slash", "")
        {
            exitOnAnimEnd = true;
            enterSound = "neontSlash";
        }

        public override void update()
        {
            base.update();
            airCode(canMove: !wasPounce);
            if (wasPounce)
            {
                maverick.move(new Point(maverick.xDir * (wasWallPounce ? NeonTiger.wallPounceSpeed : NeonTiger.pounceSpeed), 0));
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            if (oldState is NeonTPounceState || (maverick as NeonTiger).isDashing)
            {
                wasPounce = true;
                wasWallPounce = (oldState as NeonTPounceState)?.isWallPounce ?? false;
            }
        }
    }

    public class NeonTDashClawState : MaverickState
    {
        float velX;
        public NeonTDashClawState() : base("dash_slash", "")
        {
            exitOnAnimEnd = true;
            enterSound = "neontSlash";
        }

        public override void update()
        {
            base.update();
            maverick.move(new Point(velX, 0));
            velX = Helpers.lerp(velX, 0, Global.spf * 5);
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            velX = maverick.xDir * 250;
        }
    }

    public class NeonTDashState : MaverickState
    {
        float dustTime;
        public NeonTDashState() : base("dash", "")
        {
        }

        public override void update()
        {
            base.update();

            Helpers.decrementTime(ref dustTime);
            if (dustTime == 0)
            {
                new Anim(maverick.pos.addxy(-maverick.xDir * 27, 0), "dust", maverick.xDir, player.getNextActorNetId(), true, sendRpc: true);
                dustTime = 0.075f;
            }

            if (input.isPressed(Control.Jump, player))
            {
                maverick.changeState(new MJumpStart());
                return;
            }

            var move = new Point(250 * maverick.xDir, 0);

            var hitGround = Global.level.checkCollisionActor(maverick, move.x * Global.spf * 5, 20);
            if (hitGround == null)
            {
                maverick.changeState(new MIdle());
                return;
            }

            var hitWall = Global.level.checkCollisionActor(maverick, move.x * Global.spf * 2, -5);
            if (hitWall?.isSideWallHit() == true)
            {
                maverick.changeState(new MIdle());
                return;
            }

            maverick.move(move);

            if (input.isPressed(Control.Shoot, player) || isAI)
            {
                maverick.changeState(new NeonTDashClawState());
            }
            else if (isHoldStateOver(0.1f, 0.6f, 0.4f, Control.Dash))
            {
                maverick.changeToIdleOrFall();
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            (maverick as NeonTiger).isDashing = true;
        }
    }

    public class NeonTPounceState : MaverickState
    {
        public bool isWallPounce;
        public NeonTPounceState() : base("fall")
        {
        }

        public override void update()
        {
            base.update();

            if (maverick.grounded)
            {
                landingCode();
                return;
            }

            if (stateTime > 0.25f)
            {
                wallClimbCode();
            }

            if (Global.level.checkCollisionActor(maverick, 0, -1) != null && maverick.vel.y < 0)
            {
                maverick.vel.y = 0;
            }

            maverick.move(new Point(maverick.xDir * (isWallPounce ? NeonTiger.wallPounceSpeed : NeonTiger.pounceSpeed), 0));
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            if (oldState is MWallSlide)
            {
                isWallPounce = true;
            }
        }
    }
}
