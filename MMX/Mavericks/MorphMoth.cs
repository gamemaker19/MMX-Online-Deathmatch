using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class MorphMoth : Maverick
    {
        public static Weapon getWeapon() { return new Weapon(WeaponIds.MorphMGeneric, 146); }

        public MorphMoth(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool isHatch, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer, overrideState: isHatch ? new MorphMHatchState() : null)
        {
            stateCooldowns.Add(typeof(MorphMShoot), new MaverickStateCooldown(true, false, 0.5f));
            stateCooldowns.Add(typeof(MorphMShootAir), new MaverickStateCooldown(true, false, 0.5f));

            weapon = getWeapon();
            spriteToCollider.Add("sweep", getDashCollider());

            canFly = true;

            awardWeaponId = WeaponIds.SilkShot;
            weakWeaponId = WeaponIds.SpeedBurner;
            weakMaverickWeaponId = WeaponIds.FlameStag;

            netActorCreateId = NetActorCreateId.MorphMoth;
            netOwner = player;
            if (sendRpc)
            {
                createActorRpc(player.id);
            }
        }

        public override void update()
        {
            base.update();

            if (!isUnderwater())
            {
                spriteFrameToSounds["morphm_fly/0"] = "morphmFlap";
                spriteFrameToSounds["morphm_fly/4"] = "morphmFlap";
                spriteFrameToSounds["morphm_fly/8"] = "morphmFlap";
                spriteFrameToSounds["morphm_fly_fall/0"] = "morphmFlap";
                spriteFrameToSounds["morphm_fly_fall/4"] = "morphmFlap";
                spriteFrameToSounds["morphm_fly_fall/8"] = "morphmFlap";
            }
            else
            {
                spriteFrameToSounds.Clear();
            }

            if (!ownedByLocalPlayer) return;

            if (aiBehavior == MaverickAIBehavior.Control)
            {
                if (state is MIdle || state is MRun)
                {
                    if (input.isPressed(Control.Shoot, player))
                    {
                        changeState(new MorphMShoot());
                    }
                }
                else if (state is MFly)
                {
                    if (input.isPressed(Control.Dash, player))
                    {
                        changeState(new MorphMSweepState());
                    }
                    else if (input.isPressed(Control.Shoot, player))
                    {
                        changeState(new MorphMShootAir());
                    }
                }
            }
        }

        public override string getMaverickPrefix()
        {
            return "morphm";
        }

        public override MaverickState[] aiAttackStates()
        {
            var attacks = new List<MaverickState>
            {
                grounded ? new MorphMShoot() : new MorphMShootAir()
            };
            if (!grounded)
            {
                attacks.Add(new MorphMSweepState());
            }
            return attacks.ToArray();
        }

        public override MaverickState getRandomAttackState()
        {
            return aiAttackStates().GetRandomItem();
        }
    }

    public class MorphMBeamProj : Projectile
    {
        public Point endPos;
        public MorphMBeamProj(Weapon weapon, Point pos, Point endPos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 5, player, "morphm_beam", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.MorphMBeam;
            maxTime = 0.33f;
            setIndestructableProperties();

            var hits = Global.level.raycastAllSorted(pos, endPos, new List<Type>() { typeof(Wall) });
            var hit = hits.FirstOrDefault();
            if (hit != null)
            {
                endPos = hit.getHitPointSafe();
            }

            setEndPos(endPos);

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public void setEndPos(Point endPos)
        {
            this.endPos = endPos;
            float ang = pos.directionToNorm(endPos).angle;
            var points = new List<Point>();
            if (xDir == 1)
            {
                float sideY = 8 * Helpers.cosd(ang);
                float sideX = -8 * Helpers.sind(ang);
                points.Add(new Point(pos.x - sideX, pos.y - sideY));
                points.Add(new Point(endPos.x - sideX, endPos.y - sideY));
                points.Add(new Point(endPos.x + sideX, endPos.y + sideY));
                points.Add(new Point(pos.x + sideX, pos.y + sideY));
            }
            else
            {
                float sideY = 8 * Helpers.cosd(ang);
                float sideX = 8 * Helpers.sind(ang);
                points.Add(new Point(endPos.x - sideX, endPos.y + sideY));
                points.Add(new Point(endPos.x + sideX, endPos.y - sideY));
                points.Add(new Point(pos.x + sideX, pos.y - sideY));
                points.Add(new Point(pos.x - sideX, pos.y + sideY));
            }

            globalCollider = new Collider(points, true, null, false, false, 0, Point.zero);
        }

        public override void render(float x, float y)
        {
            var colors = new List<Color>()
            {
                new Color(49, 255, 255, 192),
                new Color(66, 40, 255, 192),
                new Color(49, 255, 33, 192),
                new Color(255, 255, 255, 192),
                new Color(255, 40, 33, 192),
                new Color(255, 40, 255, 192),
                new Color(255, 255, 33, 192),
            };

            if (MathF.Abs(pos.y - endPos.y) < 0.1f) DrawWrappers.DrawLine(pos.x, pos.y, endPos.x, endPos.y, colors[frameIndex], 16, ZIndex.Actor);
            else
            {
                var points = new List<Point>()
                {
                    pos.addxy(-8, 0),
                    endPos.addxy(-8, 0),
                    endPos.addxy(8, 0),
                    pos.addxy(8, 0),
                };
                DrawWrappers.DrawPolygon(points, colors[frameIndex], true, ZIndex.AboveFont);
            }
        }
    }

    public class MorphMShoot : MaverickState
    {
        bool shotOnce;
        public MorphMShoot() : base("shoot_ground", "shoot_ground_start")
        {
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (!shotOnce) maverick.turnToInput(input, player);
            Point? shootPos = maverick.getFirstPOI();
            if (!shotOnce && shootPos != null)
            {
                shotOnce = true;
                morphMothBeam(shootPos.Value, true);
            }

            if (maverick.isAnimOver())
            {
                maverick.changeState(new MIdle());
            }
        }
    }

    public class MorphMShootAir : MaverickState
    {
        bool shotOnce;
        public MorphMShootAir() : base("shoot", "shoot_start")
        {
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (!shotOnce) maverick.turnToInput(input, player);
            Point? shootPos = maverick.getFirstPOI();
            if (!shotOnce && shootPos != null)
            {
                shotOnce = true;
                morphMothBeam(shootPos.Value, false);
            }

            if (maverick.isAnimOver())
            {
                maverick.changeState(new MFly());
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.useGravity = false;
        }
    }

    public class MorphMPowderProj : Projectile
    {
        public float sparkleTime = 0;
        public MorphMPowderProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 1, player, "morphm_sparkles", 0, 0f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.MorphMPowder;
            maxTime = 1f;
            vel = new Point(0, 100);
            healAmount = 1;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            sparkleTime += Global.spf;
            if (sparkleTime > 0.05)
            {
                sparkleTime = 0;
                new Anim(pos, "morphm_sparkles_fade", 1, null, true);
            }
        }
    }

    public class MorphMSweepState : MaverickState
    {
        public MorphMSweepState() : base("sweep", "sweep_start")
        {
        }

        float shootTime;
        float xVel;
        float yVel;
        const float maxSpeed = 250;
        const float startSpeed = 50;
        public override void update()
        {
            base.update();
            if (player == null) return;

            Helpers.decrementTime(ref shootTime);
            if (isAI || input.isHeld(Control.Shoot, player))
            {
                Helpers.decrementTime(ref maverick.ammo);
                if (shootTime == 0)
                {
                    shootTime = 0.15f;
                    Point shootPos = maverick.getFirstPOIOrDefault().addRand(10, 5);
                    new MorphMPowderProj(maverick.weapon, shootPos, maverick.xDir,player, player.getNextActorNetId(), rpc: true);
                }
            }

            var inputDir = input.getInputDir(player);

            if (inputDir.x != 0 && MathF.Sign(inputDir.x) != maverick.xDir)
            {
                xVel = MathF.Clamp(xVel + (inputDir.x * 1000 * Global.spf), -maxSpeed, maxSpeed);
                if (xVel < 0) maverick.xDir = -1;
                else if (xVel > 0) maverick.xDir = 1;
            }
            else
            {
                xVel = MathF.Clamp(xVel + (maverick.xDir * 400 * Global.spf), -maxSpeed, maxSpeed);
            }

            if (inputDir.y == 0)
            {
                float yVelSign = MathF.Sign(yVel);
                if (yVelSign == 0) yVelSign = 1;
                yVel = MathF.Clamp(yVel + (yVelSign * 1000 * Global.spf), -100, 100);
            }
            else
            {
                yVel = MathF.Clamp(yVel + (inputDir.y * 1000 * Global.spf), -100, 100);
            }
            
            Point moveAmount = new Point(xVel, yVel);

            var hit = checkCollisionNormal(moveAmount.x * Global.spf, moveAmount.y * Global.spf);
            if (hit != null)
            {
                if (hit.getNormalSafe().isCeilingNormal())
                {
                    yVel *= -1;
                }
                else if (hit.getNormalSafe().isSideways())
                {
                    maverick.xDir *= -1;
                    xVel *= -0.5f;
                }
                else
                {
                    changeBack();
                    return;
                }
            }

            moveAmount = new Point(xVel, yVel);
            maverick.move(moveAmount);

            if (maverick.ammo <= 0) changeBack();
            else if (!isAI && !input.isHeld(Control.Dash, player)) changeBack();
            else if (isAI && stateTime > 1) changeBack();
            else if (stateTime > 2.5f) changeBack();
        }

        public void changeBack()
        {
            maverick.changeState(new MFly(), true);
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.stopMoving();
            maverick.useGravity = false;
            xVel = maverick.xDir * startSpeed;
        }
    }

    public class MorphMHatchState : MaverickState
    {
        float riseDist;
        public MorphMHatchState() : base("fly", "")
        {
        }

        public override void update()
        {
            base.update();

            if (maverick.grounded)
            {
                maverick.grounded = false;
                maverick.incPos(new Point(0, -5));
            }
            maverick.move(new Point(0, -75));
            riseDist += Global.spf * 75;
            if (riseDist > 37.5f)
            {
                maverick.changeState(new MFly());
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.stopMoving();
            maverick.useGravity = false;
        }
    }
}
