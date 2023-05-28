using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class SparkMandrill : Maverick
    {
        public SparkMPunchWeapon punchWeapon;
        public SparkMSparkWeapon sparkWeapon;
        public SparkMStompWeapon stompWeapon;

        public SparkMandrill(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            isHeavy = true;
            punchWeapon = new SparkMPunchWeapon(player);
            sparkWeapon = new SparkMSparkWeapon(player);
            stompWeapon = new SparkMStompWeapon(player);

            stateCooldowns.Add(typeof(SparkMPunchState), new MaverickStateCooldown(true, true, 1f));
            stateCooldowns.Add(typeof(SparkMDashPunchState), new MaverickStateCooldown(true, false, 0.75f));
            stateCooldowns.Add(typeof(MShoot), new MaverickStateCooldown(true, true, 2f));
            spriteToCollider.Add("dash_punch", getDashCollider());

            weapon = new Weapon(WeaponIds.SparkMGeneric, 94);

            awardWeaponId = WeaponIds.ElectricSpark;
            weakWeaponId = WeaponIds.ShotgunIce;
            weakMaverickWeaponId = WeaponIds.ChillPenguin;

            netActorCreateId = NetActorCreateId.SparkMandrill;
            netOwner = player;
            if (sendRpc)
            {
                createActorRpc(player.id);
            }
        }

        public override void update()
        {
            base.update();

            //rechargeAmmo(8);

            if (aiBehavior == MaverickAIBehavior.Control)
            {
                if (state is MIdle || state is MRun)
                {
                    if (specialPressed())
                    {
                        //if (ammo >= 32)
                        {
                            changeState(getShootState());
                        }
                    }
                    else if (input.isPressed(Control.Dash, player))
                    {
                        changeState(new SparkMDashPunchState());
                    }
                    else if (shootPressed())
                    {
                        changeState(new SparkMPunchState());
                    }
                }
                else if (state is MJump || state is MFall)
                {
                    if (input.isHeld(Control.Up, player))
                    {
                        var hit = Global.level.checkCollisionActor(this, 0, -15);
                        if (vel.y < 0 && hit?.gameObject is Wall wall && !wall.topWall)
                        {
                            changeState(new SparkMClimbState(hit.getHitPointSafe()));
                        }
                    }
                }
            }
        }

        public override string getMaverickPrefix()
        {
            return "sparkm";
        }

        public MaverickState getShootState()
        {
            return new MShoot((Point pos, int xDir) =>
            {
                shakeCamera(sendRpc: true);
                playSound("sparkmSpark", sendRpc: true);
                //deductAmmo(32);
                new TriadThunderProjCharged(sparkWeapon, pos, xDir, 1, player, player.getNextActorNetId(), rpc: true);
                new TriadThunderProjCharged(sparkWeapon, pos, -xDir, 1, player, player.getNextActorNetId(), rpc: true);
            }, null);
        }

        public override MaverickState[] aiAttackStates()
        {
            return new MaverickState[]
            {
                new SparkMPunchState(),
                getShootState(),
                new SparkMDashPunchState(),
            };
        }

        public override MaverickState getRandomAttackState()
        {
            var attacks = new MaverickState[]
            {
                getShootState(),
                new SparkMDashPunchState(),
            };
            return attacks.GetRandomItem();
        }

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            if (sprite.name.Contains("punch"))
            {
                return new GenericMeleeProj(punchWeapon, centerPoint, ProjIds.SparkMPunch, player);
            }
            if (sprite.name.Contains("shoot"))
            {
                return new GenericMeleeProj(sparkWeapon, centerPoint, ProjIds.SparkMSpark, player);
            }
            if (sprite.name.Contains("fall"))
            {
                float damagePercent = 0;
                if (deltaPos.y > 100 * Global.spf) damagePercent = 0.5f;
                if (deltaPos.y > 200 * Global.spf) damagePercent = 0.75f;
                if (deltaPos.y > 300 * Global.spf) damagePercent = 1;
                if (damagePercent > 0)
                {
                    return new GenericMeleeProj(stompWeapon, centerPoint, ProjIds.GBeetleStomp, player, damage: stompWeapon.damager.damage * damagePercent);
                }
            }
            return null;
        }
    }

    #region weapons
    public class SparkMSparkWeapon : Weapon
    {
        public SparkMSparkWeapon(Player player)
        {
            index = (int)WeaponIds.SparkMSpark;
            killFeedIndex = 94;
            damager = new Damager(player, 4, Global.defFlinch, 0.5f);
        }
    }

    public class SparkMStompWeapon : Weapon
    {
        public SparkMStompWeapon(Player player)
        {
            index = (int)WeaponIds.SparkMStomp;
            killFeedIndex = 94;
            damager = new Damager(player, 4, Global.defFlinch, 0.5f);
        }
    }

    public class SparkMPunchWeapon : Weapon
    {
        public SparkMPunchWeapon(Player player)
        {
            index = (int)WeaponIds.SparkMPunch;
            killFeedIndex = 94;
            damager = new Damager(player, 4, Global.defFlinch, 0.75f);
        }
    }
    #endregion

    #region states
    public class SparkMPunchState : MaverickState
    {
        public float dustTime;
        public SparkMPunchState() : base("punch", "")
        {
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (maverick.isAnimOver())
            {
                maverick.changeState(new MIdle());
            }
        }
    }

    public class SparkMDashPunchState : MaverickState
    {
        public float dustTime;
        public SparkMDashPunchState() : base("dash_punch", "")
        {
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

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
                maverick.playSound("crash", sendRpc: true);
                maverick.shakeCamera(sendRpc: true);
                maverick.changeState(new MIdle());
                return;
            }

            maverick.move(move);

            if (stateTime > 0.6)
            {
                maverick.changeState(new MIdle());
                return;
            }

            dustTime += Global.spf;
            if (dustTime > 0.1)
            {
                dustTime = 0;
                new Anim(maverick.pos.addxy(0, -4), "dust", maverick.xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
        }
    }

    public class SparkMClimbState : MaverickState
    {
        Point hitPoint;
        float climbSpeed = 100;
        public SparkMClimbState(Point hitPoint) : base("climb", "")
        {
            this.hitPoint = hitPoint;
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (input.isPressed(Control.Jump, player) || input.isPressed(Control.Down, player))
            {
                maverick.changeState(new MFall());
                return;
            }

            bool leftHeld = input.isHeld(Control.Left, player);
            bool rightHeld = input.isHeld(Control.Right, player);

            Point moveAmount = new Point();
            if (leftHeld)
            {
                maverick.xDir = -1;
                moveAmount.x = -climbSpeed * Global.spf;
            }
            else if (rightHeld)
            {
                maverick.xDir = 1;
                moveAmount.x = climbSpeed * Global.spf;
            }

            if (moveAmount.x != 0)
            {
                maverick.move(moveAmount, useDeltaTime: false);

                // Get amount to snap up to ceiling
                Point origin = maverick.pos.addxy(0, 0);
                Point dest = origin.addxy(0, -maverick.height - 5);
                var ceiling = Global.level.raycast(origin, dest, new List<Type> { typeof(Wall) });
                if (ceiling?.gameObject is Wall wall && !wall.topWall)
                {
                    float newY = ceiling.getHitPointSafe().y + maverick.height;
                    if (MathF.Abs(newY - maverick.pos.y) > 1)
                    {
                        maverick.changePos(new Point(maverick.pos.x, newY));
                    }
                }
                else
                {
                    maverick.changeState(new MFall());
                    return;
                }

                maverick.frameSpeed = 1;
            }
            else
            {
                maverick.frameSpeed = 0;
                maverick.frameIndex = 0;
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.useGravity = false;
            maverick.stopMoving();
            maverick.changePos(new Point(maverick.pos.x, hitPoint.y + maverick.height));
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            maverick.useGravity = true;
        }
    }

    public class SparkMFrozenState : MaverickState
    {
        public SparkMFrozenState() : base("freeze")
        {
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (maverick.frameIndex >= 1 && !once)
            {
                once = true;
                maverick.breakFreeze(player, maverick.getCenterPos(), sendRpc: true);
            }

            if (maverick.isAnimOver())
            {
                maverick.changeState(new MIdle());
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.stopMoving();
        }
    }
    #endregion
}
