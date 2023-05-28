using System;

namespace MMXOnline
{
    public class OverdriveOstrich : Maverick
    {
        public static Weapon getWeapon() { return new Weapon(WeaponIds.OverdriveOGeneric, 149); }

        public float dashDist;
        public float baseSpeed = 0;
        public float accSpeed;
        public int lastDirX;
        public float crystalizeCooldown;

        public OverdriveOstrich(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            stateCooldowns.Add(typeof(OverdriveOShootState), new MaverickStateCooldown(false, true, 0.75f));
            stateCooldowns.Add(typeof(OverdriveOShoot2State), new MaverickStateCooldown(true, true, 2f));
            stateCooldowns.Add(typeof(OverdriveOJumpKickState), new MaverickStateCooldown(true, true, 1f));

            weapon = getWeapon();

            awardWeaponId = WeaponIds.SonicSlicer;
            weakWeaponId = WeaponIds.CrystalHunter;
            weakMaverickWeaponId = WeaponIds.CrystalSnail;

            netActorCreateId = NetActorCreateId.OverdriveOstrich;
            netOwner = player;
            if (sendRpc)
            {
                createActorRpc(player.id);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            Helpers.decrementTime(ref crystalizeCooldown);

            if (lastDirX != xDir)
            {
                accSpeed = 0;
                dashDist = 0;
            }
            lastDirX = xDir;

            if (state is MRun)
            {
                dashDist += accSpeed * Global.spf;
                accSpeed += Global.spf * 800;
                if (accSpeed > 300) accSpeed = 300;
            }
            else if (state is not MJumpStart && state is not MJump && state is not MFall && state is not MLand && state is not OverdriveOSkidState &&
                state is not OverdriveOJumpKickState && state is not OverdriveOShootState && state is not OverdriveOShoot2State)
            {
                accSpeed = 0;
            }

            // Momentum carrying states
            if (state is OverdriveOShootState || state is OverdriveOShoot2State || state is MRun || state is MFall)
            {
                var inputDir = input.getInputDir(player);
                if (state is OverdriveOShootState || inputDir.x != xDir)
                {
                    accSpeed = Helpers.lerp(accSpeed, 0, Global.spf * 5);
                }
            }

            if (state is OverdriveOShootState || state is OverdriveOShoot2State)
            {
                var moveAmount = new Point(getRunSpeed() * xDir, 0);
                if (moveAmount.magnitude > 0)
                {
                    move(moveAmount);
                }
            }

            if (aiBehavior == MaverickAIBehavior.Control)
            {
                if (state is MIdle || state is MRun)
                {
                    if (input.isPressed(Control.Shoot, player))
                    {
                        changeState(new OverdriveOShootState());
                    }
                    else if (input.isPressed(Control.Special1, player))
                    {
                        changeState(new OverdriveOShoot2State());
                    }
                    else if (input.isPressed(Control.Dash, player))
                    {
                        changeState(new OverdriveOJumpKickState());
                    }
                }
                else if (state is MJump || state is MFall)
                {
                    if (input.isPressed(Control.Shoot, player))
                    {
                        changeState(new OverdriveOShootState());
                    }
                    else if (input.isPressed(Control.Special1, player))
                    {
                        changeState(new OverdriveOShoot2State());
                    }
                }
            }
        }

        public float dustSpeed { get { return 300; } }
        public float skidSpeed { get { return 300; } }
        public float damageSpeed { get { return 299; } }
        public float wallSkidSpeed { get { return 300; } }

        public override float getRunSpeed()
        {
            float speed = baseSpeed + accSpeed;
            if (state is MRun || state is OverdriveOSkidState || state is OverdriveOShootState || state is OverdriveOShoot2State) return speed;
            else return Math.Max(100, speed);
        }

        public override float getDashSpeed()
        {
            float runSpeed = getRunSpeed();
            if (runSpeed > 150)
            {
                return 1;
            }
            return dashSpeed;
        }

        public override string getMaverickPrefix()
        {
            return "overdriveo";
        }

        public override MaverickState[] aiAttackStates()
        {
            var attacks = new MaverickState[]
            {
                new OverdriveOShootState(),
                new OverdriveOShoot2State(),
                new OverdriveOJumpKickState(),
            };
            return attacks;
        }

        public override MaverickState getRandomAttackState()
        {
            return aiAttackStates().GetRandomItem();
        }

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            if (sprite.name.Contains("skip"))
            {
                return new GenericMeleeProj(weapon, centerPoint, ProjIds.OverdriveOMelee, player, 3, Global.defFlinch, 1);
            }
            else if (sprite.name.EndsWith("_run") && MathF.Abs(deltaPos.x) >= damageSpeed * Global.spf)
            {
                return new GenericMeleeProj(weapon, centerPoint, ProjIds.OverdriveOMelee, player, 2, Global.halfFlinch, 1);
            }
            return null;
        }

        public override void updateProjFromHitbox(Projectile proj)
        {
            if (sprite.name.EndsWith("_run"))
            {
                if (MathF.Abs(deltaPos.x) >= damageSpeed * Global.spf)
                {
                    proj.damager.damage = 2;
                }
                else
                {
                    proj.damager.damage = 0;
                }
            }
        }
    }

    public class OverdriveOSonicSlicerProj : Projectile
    {
        bool once;
        public OverdriveOSonicSlicerProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 3, player, "overdriveo_slicer_start", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.OverdriveOSonicSlicer;
            maxTime = 0.5f;
            destroyOnHit = false;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            if (sprite.isAnimOver() && !once)
            {
                once = true;
                changeSprite("overdriveo_slicer", true);
                vel = new Point(xDir * 350, 0);
            }
        }
    }

    public class OverdriveOShootState : MaverickState
    {
        bool shotOnce;
        public OverdriveOShootState() : base("attack", "")
        {
        }

        public override void update()
        {
            base.update();

            //maverick.turnToInput(input, player);
            //maverick.move(new Point(maverick.getRunSpeed() * maverick.xDir, 0), true);

            Point? shootPos = maverick.getFirstPOI();
            if (!shotOnce && shootPos != null)
            {
                shotOnce = true;
                maverick.playSound("overdriveoShoot", sendRpc: true);
                new OverdriveOSonicSlicerProj(maverick.weapon, shootPos.Value, maverick.xDir, player, player.getNextActorNetId(), rpc: true);
            }

            if (maverick.isAnimOver())
            {
                maverick.changeToIdleOrFall();
            }
        }
    }

    public class OverdriveOSonicSlicerUpProj : Projectile
    {
        public Point dest;
        public bool fall;
        public OverdriveOSonicSlicerUpProj(Weapon weapon, Point pos, int num, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, 1, 0, 3, player, "overdriveo_slicer_vertical", Global.defFlinch, 0.25f, netProjId, player.ownedByLocalPlayer)
        {
            fadeSprite = "sonicslicer_charged_fade";
            maxTime = 1;
            projId = (int)ProjIds.OverdriveOSonicSlicerUp;
            destroyOnHit = false;

            if (num == 0) dest = pos.addxy(-90, -100);
            if (num == 1) dest = pos.addxy(-45, -100);
            if (num == 2) dest = pos.addxy(-0, -100);
            if (num == 3) dest = pos.addxy(45, -100);
            if (num == 4) dest = pos.addxy(90, -100);

            vel.x = 0;
            vel.y = -500;
            useGravity = true;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, 1);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            vel.y += Global.spf * Global.level.gravity;
            if (!fall)
            {
                float x = Helpers.lerp(pos.x, dest.x, Global.spf * 10);
                changePos(new Point(x, pos.y));
                if (vel.y > 0)
                {
                    fall = true;
                    yDir = -1;
                }
            }
        }
    }

    public class OverdriveOShoot2State : MaverickState
    {
        bool shotOnce;
        public OverdriveOShoot2State() : base("attack2", "")
        {
        }

        public override void update()
        {
            base.update();

            Point? shootPos = maverick.getFirstPOI();
            if (!shotOnce && shootPos != null)
            {
                shotOnce = true;
                maverick.playSound("overdriveoShoot2", sendRpc: true);
                new OverdriveOSonicSlicerUpProj(maverick.weapon, shootPos.Value, 0, player, player.getNextActorNetId(), rpc: true);
                new OverdriveOSonicSlicerUpProj(maverick.weapon, shootPos.Value, 1, player, player.getNextActorNetId(), rpc: true);
                new OverdriveOSonicSlicerUpProj(maverick.weapon, shootPos.Value, 2, player, player.getNextActorNetId(), rpc: true);
                new OverdriveOSonicSlicerUpProj(maverick.weapon, shootPos.Value, 3, player, player.getNextActorNetId(), rpc: true);
                new OverdriveOSonicSlicerUpProj(maverick.weapon, shootPos.Value, 4, player, player.getNextActorNetId(), rpc: true);
            }

            if (maverick.isAnimOver())
            {
                maverick.changeToIdleOrFall();
            }
        }
    }

    public class OverdriveOJumpKickState : MaverickState
    {
        public OverdriveOJumpKickState() : base("skip")
        {
        }

        public override void update()
        {
            base.update();

            if (maverick.grounded && stateTime > 0.05f)
            {
                landingCode();
                return;
            }

            if (Global.level.checkCollisionActor(maverick, 0, -1) != null && maverick.vel.y < 0)
            {
                maverick.vel.y = 0;
            }

            maverick.move(new Point(maverick.xDir * 300, 0));
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.vel.y = -maverick.getJumpPower() * 0.75f;
        }
    }

    public class OverdriveOSkidState : MaverickState
    {
        float dustTime;
        public OverdriveOSkidState() : base("skid", "")
        {
            enterSound = "overdriveoSkid";
        }

        public override void update()
        {
            base.update();

            var oo = maverick as OverdriveOstrich;
            oo.accSpeed = Helpers.lerp(oo.accSpeed, 0, Global.spf * 5);

            Helpers.decrementTime(ref dustTime);
            if (dustTime == 0)
            {
                new Anim(maverick.pos.addxy(maverick.xDir * 10, 0), "dust", maverick.xDir, player.getNextActorNetId(), true, sendRpc: true) { frameSpeed = 1.5f };
                dustTime = 0.05f;
            }

            var inputDir = input.getInputDir(player);
            if (inputDir.x == -oo.xDir) maverick.frameSpeed = 1.5f;
            else maverick.frameSpeed = 1;

            var move = new Point(maverick.getRunSpeed() * maverick.xDir, 0);
            if (move.magnitude > 0)
            {
                maverick.move(move);
            }

            if (!once)
            {
                if (maverick.loopCount > 2)
                {
                    maverick.changeSpriteFromName("skid_end", true);
                    once = true;
                }
            }
            else
            {
                if (maverick.isAnimOver())
                {
                    maverick.changeToIdleOrFall();
                }
            }
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            var oo = maverick as OverdriveOstrich;
            oo.accSpeed = 0;
        }
    }

    public class OverdriveOCrystalizedState : MaverickState
    {
        public OverdriveOCrystalizedState() : base("hurt_weakness")
        {
            enterSound = "crystalize";
        }

        public override bool canEnter(Maverick maverick)
        {
            return base.canEnter(maverick);
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.stopMoving();
        }

        public override void update()
        {
            base.update();

            if (maverick.isAnimOver())
            {
                Anim.createGibEffect("overdriveo_weakness_glass", maverick.getCenterPos(), player, sendRpc: true);
                maverick.playSound("freezebreak2", sendRpc: true);
                maverick.changeToIdleOrFall();
            }
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            (maverick as OverdriveOstrich).crystalizeCooldown = 1;
        }
    }
}
