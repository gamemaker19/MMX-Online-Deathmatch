using SFML.Graphics;

namespace MMXOnline
{
    public class GravityBeetle : Maverick
    {
        public static Weapon getWeapon() { return new Weapon(WeaponIds.GravityBeetle, 157); }
        public static Weapon getMeleeWeapon(Player player) { return new Weapon(WeaponIds.GravityBeetle, 157, new Damager(player, 6, Global.defFlinch, 0.5f)); }

        public Weapon meleeWeapon;
        public GBeetleGravityWellProj well;

        public GravityBeetle(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            stateCooldowns.Add(typeof(GBeetleShoot), new MaverickStateCooldown(false, false, 1f));
            stateCooldowns.Add(typeof(GBeetleGravityWellState), new MaverickStateCooldown(false, false, 1));
            stateCooldowns.Add(typeof(GBeetleDashState), new MaverickStateCooldown(false, false, 1.5f));

            weapon = getWeapon();
            meleeWeapon = getMeleeWeapon(player);

            isHeavy = true;

            awardWeaponId = WeaponIds.GravityWell;
            weakWeaponId = WeaponIds.RaySplasher;
            weakMaverickWeaponId = WeaponIds.NeonTGeneric;

            netActorCreateId = NetActorCreateId.GravityBeetle;
            netOwner = player;
            if (sendRpc)
            {
                createActorRpc(player.id);
            }
        }

        public override void update()
        {
            base.update();
            if (well?.destroyed == true)
            {
                well = null;
            }

            if (aiBehavior == MaverickAIBehavior.Control)
            {
                if (state is MIdle || state is MRun)
                {
                    if (input.isPressed(Control.Shoot, player))
                    {
                        changeState(new GBeetleShoot(false));
                    }
                    else if (input.isPressed(Control.Special1, player) && well == null)
                    {
                        changeState(new GBeetleGravityWellState());
                    }
                    else if (input.isPressed(Control.Dash, player))
                    {
                        changeState(new GBeetleDashState());
                    }
                }
                else if (state is MJump || state is MFall)
                {
                }
            }
        }

        public override float getRunSpeed()
        {
            return 80;
        }

        public override string getMaverickPrefix()
        {
            return "gbeetle";
        }

        public override MaverickState getRandomAttackState()
        {
            return aiAttackStates().GetRandomItem();
        }

        public override MaverickState[] aiAttackStates()
        {
            return new MaverickState[] 
            {
                new GBeetleShoot(false),
                new GBeetleGravityWellState(),
                new GBeetleDashState(),
            };
        }

        public float getStompDamage()
        {
            float damagePercent = 0;
            if (deltaPos.y > 150 * Global.spf) damagePercent = 0.5f;
            if (deltaPos.y > 225 * Global.spf) damagePercent = 0.75f;
            if (deltaPos.y > 300 * Global.spf) damagePercent = 1;
            return damagePercent;
        }

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            if (sprite.name.Contains("gbeetle_dash"))
            {
                return new GenericMeleeProj(weapon, centerPoint, ProjIds.GBeetleLift, player, damage: 0, flinch: 0, hitCooldown: 0, owningActor: this);
            }
            if (sprite.name.Contains("fall"))
            {
                float damagePercent = getStompDamage();
                if (damagePercent > 0)
                {
                    return new GenericMeleeProj(weapon, centerPoint, ProjIds.GBeetleStomp, player, damage: 4 * damagePercent, flinch: Global.defFlinch, hitCooldown: 0.5f);
                }
            }
            return null;
        }

        public override void updateProjFromHitbox(Projectile proj)
        {
            if (sprite.name.EndsWith("fall"))
            {
                float damagePercent = getStompDamage();
                if (damagePercent > 0)
                {
                    proj.damager.damage = 4 * damagePercent;
                }
            }
        }
    }

    public class GBeetleBallProj : Projectile
    {
        bool firstHit;
        int size;
        float hitWallCooldown;
        const float moveSpeed = 200;
        bool isSecond;
        public GBeetleBallProj(Weapon weapon, Point pos, int xDir, bool isSecond, Player player, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, xDir, moveSpeed, 2, player, "gbeetle_proj1", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.GBeetleBall;
            maxTime = 3f;
            destroyOnHit = false;
            this.isSecond = isSecond;
            if (isSecond)
            {
                vel = new Point(0, -moveSpeed);
            }

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            Helpers.decrementTime(ref hitWallCooldown);
        }

        public void increaseSize()
        {
            if (!ownedByLocalPlayer) return;

            if (size >= 2) return;
            if (size == 0) changeSprite("gbeetle_proj2", true);
            else if (size == 1) changeSprite("gbeetle_proj3", true);

            int flinch = 0;
            if (size == 1) flinch = Global.halfFlinch;
            if (size == 2) flinch = Global.defFlinch;

            updateDamager((size + 1) * 2, flinch);
            size++;
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            if (!ownedByLocalPlayer) return;
            if (hitWallCooldown > 0) return;
            if (other.myCollider.isTrigger) return;

            bool didHit = false;
            if (!firstHit)
            {
                firstHit = true;
                didHit = true;
                if (!isSecond)
                {
                    vel.x *= -1;
                    vel.y = -moveSpeed;
                }
                else
                {
                    vel.x = xDir * moveSpeed;
                    vel.y = moveSpeed;
                }
            }
            else if (other.isSideWallHit())
            {
                vel.x *= -1;
                didHit = true;
            }
            else if (other.isCeilingHit() || other.isGroundHit())
            {
                vel.y *= -1;
                didHit = true;
            }
            if (didHit)
            {
                playSound("gbeetleProjBounce", sendRpc: true);
                increaseSize();
                //hitWallCooldown += 0.1f;
            }
        }
    }

    public class GBeetleShoot : MaverickState
    {
        bool shotOnce;
        bool isSecond;
        public GBeetleShoot(bool isSecond) : 
            base(isSecond ? "attackproj2" : "attackproj", isSecond ? "attackproj2_start" : "attackproj_start")
        {
            this.isSecond = isSecond;
            exitOnAnimEnd = true;
        }

        public override void update()
        {
            base.update();

            Point? shootPos = maverick.getFirstPOI();
            if (!shotOnce && shootPos != null)
            {
                shotOnce = true;
                new GBeetleBallProj(maverick.weapon, shootPos.Value, maverick.xDir, isSecond, player, player.getNextActorNetId(), sendRpc: true);
            }

            if (!isSecond && maverick.frameIndex >= 5)
            {
                if (isAI || input.isPressed(Control.Shoot, player))
                {
                    maverick.changeState(new GBeetleShoot(true));
                    return;
                }
            }
        }
    }

    public class GBeetleDashState : MaverickState
    {
        float soundTime;
        float dustTime;
        float partTime;
        public GBeetleDashState() : base("dash", "dash_start")
        {
        }

        public override void update()
        {
            base.update();
            if (inTransition()) return;
            if (ftdWaitTime > 0)
            {
                tryChangeToIdle();
                return;
            }

            Helpers.decrementTime(ref soundTime);
            if (soundTime == 0)
            {
                maverick.playSound("gbeetleDash", sendRpc: true);
                soundTime = 0.085f;
            }
            Helpers.decrementTime(ref dustTime);
            if (dustTime == 0)
            {
                new Anim(maverick.getFirstPOIOrDefault(0), "dust", maverick.xDir, player.getNextActorNetId(), true, sendRpc: true);
                dustTime = 0.05f;
            }
            Helpers.decrementTime(ref partTime);
            if (partTime == 0)
            {
                var vel = new Point(0, -250);
                float ttl = 0.6f;
                new Anim(maverick.getFirstPOIOrDefault(1), "gbeetle_debris", maverick.xDir, player.getNextActorNetId(), false, sendRpc: true) { vel = vel, useGravity = true, ttl = ttl };
                partTime = 0.1f;
            }

            var move = new Point(250 * maverick.xDir, 0);

            var hitGround = Global.level.checkCollisionActor(maverick, move.x * Global.spf * 5, 20);
            if (hitGround == null)
            {
                tryChangeToIdle();
                return;
            }

            var hitWall = Global.level.checkCollisionActor(maverick, move.x * Global.spf * 2, -5);
            if (hitWall?.isSideWallHit() == true)
            {
                maverick.playSound("crash", sendRpc: true);
                maverick.shakeCamera(sendRpc: true);
                tryChangeToIdle();
                return;
            }

            maverick.move(move);

            if (stateTime > 1f)
            {
                tryChangeToIdle();
                return;
            }
        }

        float ftdWaitTime;
        public void tryChangeToIdle()
        {
            if (player.isDefenderFavored)
            {
                ftdWaitTime += Global.spf;
                if (ftdWaitTime < 0.25f)
                {
                    return;
                }
            }
            maverick.changeState(new MIdle("dash_end"));
        }

        public override bool trySetGrabVictim(Character grabbed)
        {
            maverick.changeState(new GBeetleLiftState(grabbed), true);
            return true;
        }
    }

    public class GBeetleLiftState : MaverickState
    {
        public Character grabbedChar;
        float timeWaiting;
        bool grabbedOnce;
        public GBeetleLiftState(Character grabbedChar) : base("dash_lift")
        {
            this.grabbedChar = grabbedChar;
        }

        public override void update()
        {
            base.update();

            if (!grabbedOnce && grabbedChar != null && !grabbedChar.sprite.name.EndsWith("_grabbed") && maverick.frameIndex > 1 && timeWaiting < 0.5f)
            {
                maverick.frameSpeed = 0;
                timeWaiting += Global.spf;
            }
            else
            {
                maverick.frameSpeed = 1;
            }

            if (grabbedChar != null && grabbedChar.sprite.name.EndsWith("_grabbed"))
            {
                grabbedOnce = true;
            }

            if (maverick.isAnimOver())
            {
                maverick.changeState(new MIdle());
            }
        }
    }

    public class BeetleGrabbedState : GenericGrabbedState
    {
        public Character grabbedChar;
        public bool launched;
        float launchTime;
        public BeetleGrabbedState(GravityBeetle grabber) : base(grabber, 1, "")
        {
            customUpdate = true;
        }

        public override void update()
        {
            base.update();

            if (launched)
            {
                launchTime += Global.spf;
                if (launchTime > 0.33f)
                {
                    character.changeToIdleOrFall();
                    return;
                }
                if (character.stopCeiling())
                {
                    (grabber as GravityBeetle).meleeWeapon.applyDamage(character, false, grabber, (int)ProjIds.GBeetleLiftCrash);
                }
                return;
            }

            if (grabber.sprite?.name.EndsWith("_dash_lift") == true)
            {
                if (grabber.frameIndex < 2)
                {
                    trySnapToGrabPoint(true);
                }
                else if (!launched)
                {
                    launched = true;
                    character.unstickFromGround();
                    character.vel.y = -600;
                }
            }
            else
            {
                notGrabbedTime += Global.spf;
            }

            if (notGrabbedTime > 0.5f)
            {
                character.changeToIdleOrFall();
            }
        }
    }

    public class GBeetleGravityWellProj : Projectile
    {
        public int state;
        public float drawRadius;
        const float riseSpeed = 150;
        public float radiusFactor;
        float randPartTime;
        public float maxRadius = 50;
        float ttl = 4;
        public GBeetleGravityWellProj(Weapon weapon, Point pos, int xDir, float chargeTime, Player player, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, xDir, 0, 2, player, "gbeetle_proj_blackhole", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.GBeetleGravityWell;
            setIndestructableProperties();
            
            maxRadius = 25 + (50 * (chargeTime / 2f));
            ttl = chargeTime * 2;

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();

            drawRadius = radiusFactor * maxRadius;
            if (radiusFactor > 0)
            {
                globalCollider = new Collider(new Rect(0, 0, 24 + (radiusFactor * maxRadius), 24 + (radiusFactor * maxRadius)).getPoints(), true, this, false, false, 0, Point.zero);
            }

            if (!ownedByLocalPlayer) return;

            if (state == 0)
            {
                move(new Point(0, -riseSpeed));
                moveDistance += riseSpeed * Global.spf;
                var hit = checkCollision(0, -1);
                if (moveDistance > 175 || hit?.isCeilingHit() == true)
                {
                    state = 1;
                    playSound("gbeetleWell", sendRpc: true);
                }
            }
            else if (state == 1)
            {
                radiusFactor += Global.spf * 1.5f;
                if (radiusFactor >= 1)
                {
                    state = 2;
                    maxTime = ttl;
                    time = 0;
                }
            }
            else if (state == 2)
            {
                randPartTime += Global.spf;
                if (randPartTime > 0.025f)
                {
                    randPartTime = 0;
                    var partSpawnAngle = Helpers.randomRange(0, 360);
                    float spawnRadius = maxRadius;
                    float spawnSpeed = 300;
                    var partSpawnPos = pos.addxy(Helpers.cosd(partSpawnAngle) * spawnRadius, Helpers.sind(partSpawnAngle) * spawnRadius);
                    var partVel = partSpawnPos.directionToNorm(pos).times(spawnSpeed);
                    var partSprite = "gbeetle_proj_flare" + (Helpers.randomRange(0, 1) == 0 ? "2" : "");
                    new Anim(partSpawnPos, partSprite, 1, owner.getNextActorNetId(), false, sendRpc: true)
                    {
                        vel = partVel,
                        ttl = ((spawnRadius - 10) / spawnSpeed),
                    };
                }
            }
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            base.onHitDamagable(damagable);
            var actor = damagable.actor();
            Character chr = actor as Character;
            if (chr != null && chr.isCCImmune()) return;
            if (actor is not Character && actor is not RideArmor && actor is not Maverick) return;
            if (chr != null && (chr.charState is DeadLiftGrabbed || chr.charState is BeetleGrabbedState)) return;

            float mag = 100;
            if (!actor.grounded) actor.vel.y = 0;
            Point velVector = actor.getCenterPos().directionToNorm(pos).times(mag);
            actor.move(velVector, true);
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            if (state >= 1)
            {
                DrawWrappers.DrawCircle(pos.x + x, pos.y + y, drawRadius, true, Color.Black, 1, ZIndex.Background + 10);
            }
        }
    }

    public class GBeetleGravityWellState : MaverickState
    {
        int state = 0;
        float partTime;
        float chargeTime;
        public GBeetleGravityWellState() : base("blackhole_start", "")
        {
        }

        public override void update()
        {
            base.update();

            if (state == 0)
            {
                Helpers.decrementTime(ref partTime);
                if (partTime <= 0)
                {
                    partTime = 0.2f;
                    var vel = new Point(0, 50);
                    new Anim(maverick.getFirstPOI(0).Value, "gbeetle_proj_flare", 1, player.getNextActorNetId(), false, sendRpc: true) { ttl = partTime, vel = vel };
                    new Anim(maverick.getFirstPOI(1).Value, "gbeetle_proj_flare", 1, player.getNextActorNetId(), false, sendRpc: true) { ttl = partTime, vel = vel };
                    new Anim(maverick.getFirstPOI(2).Value, "gbeetle_proj_flare", 1, player.getNextActorNetId(), false, sendRpc: true) { ttl = partTime, vel = vel };
                    new Anim(maverick.getFirstPOI(3).Value, "gbeetle_proj_flare", 1, player.getNextActorNetId(), false, sendRpc: true) { ttl = partTime, vel = vel };
                    new Anim(maverick.getFirstPOI(4).Value, "gbeetle_proj_flare", 1, player.getNextActorNetId(), false, sendRpc: true) { ttl = partTime, vel = vel };
                }

                if (isHoldStateOver(0.5f, 2f, 1f, Control.Special1))
                {
                    maverick.changeSpriteFromName("blackhole", true);
                    chargeTime = stateTime;
                    state = 1;
                }
            }
            else if (state == 1)
            {
                Point? shootPos = maverick.getFirstPOI();
                if (!once && shootPos != null)
                {
                    once = true;
                    (maverick as GravityBeetle).well = new GBeetleGravityWellProj(maverick.weapon, shootPos.Value, maverick.xDir, chargeTime, player, player.getNextActorNetId(), sendRpc: true);
                }
                if (maverick.isAnimOver())
                {
                    maverick.changeToIdleOrFall();
                }
            }
        }
    }
}
