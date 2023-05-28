using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class AcidBurst : Weapon
    {
        public AcidBurst() : base()
        {
            shootSounds = new List<string>() { "acidBurst", "acidBurst", "acidBurst", "acidBurst" };
            rateOfFire = 0.5f;
            index = (int)WeaponIds.AcidBurst;
            weaponBarBaseIndex = 17;
            weaponBarIndex = 17;
            weaponSlotIndex = 17;
            killFeedIndex = 40;
            weaknessIndex = (int)WeaponIds.FrostShield;
        }

        public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId)
        {
            if (chargeLevel != 3)
            {
                /*
                if (timeSinceLastShoot != null && timeSinceLastShoot < rateOfFire)
                {
                    new AcidBurstProj(this, pos, xDir, player, netProjId);
                    timeSinceLastShoot = null;
                }
                else
                {
                    new AcidBurstProj(this, pos, xDir, player, netProjId);
                    timeSinceLastShoot = 0;
                    shootTime = 0.2f;
                }
                */
                new AcidBurstProj(this, pos, xDir, player, netProjId);
            }
            else
            {
                player.setNextActorNetId(netProjId);
                new AcidBurstProjCharged(this, pos, xDir, 0, player, player.getNextActorNetId(true));
                new AcidBurstProjCharged(this, pos, xDir, 1, player, player.getNextActorNetId(true));
            }
        }
    }

    public class AcidBurstProj : Projectile
    {
        public AcidBurstProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 300, 0, player, "acidburst_proj", 0, 0f, netProjId, player.ownedByLocalPlayer)
        {
            useGravity = true;
            maxTime = 1.5f;
            projId = (int)ProjIds.AcidBurst;
            vel = new Point(xDir * 100, -200);
            fadeSound = "acidBurst";
            checkUnderwater();
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;
            checkUnderwater();
        }

        public void checkUnderwater()
        {
            if (isUnderwater())
            {
                new BubbleAnim(pos, "bigbubble1") { vel = new Point(0, -75) };
                Global.level.delayedActions.Add(new DelayedAction(() => { new BubbleAnim(pos, "bigbubble2") { vel = new Point(0, -75) }; }, 0.1f));
                destroySelf();
                return;
            }
        }

        public override void onHitWall(CollideData other)
        {
            if (!ownedByLocalPlayer) return;
            acidSplashEffect(other, ProjIds.AcidBurstSmall);
            destroySelf();
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            if (ownedByLocalPlayer) acidFadeEffect();
            base.onHitDamagable(damagable);
        }
    }

    public class AcidBurstProjSmall : Projectile
    {
        public AcidBurstProjSmall(Weapon weapon, Point pos, int xDir, Point vel, ProjIds projId, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 300, 0, player, "acidburst_proj_small", 0, 0f, netProjId, player.ownedByLocalPlayer)
        {
            useGravity = true;
            maxTime = 1.5f;
            this.projId = (int)projId;
            fadeSprite = "acidburst_fade";
            this.vel = vel;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;
            checkUnderwater();
        }

        public void checkUnderwater()
        {
            if (isUnderwater())
            {
                destroySelfNoEffect();
            }
        }

        public override void onHitWall(CollideData other)
        {
            if (!ownedByLocalPlayer) return;
            destroySelf();
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            if (ownedByLocalPlayer) acidFadeEffect();
            base.onHitDamagable(damagable);
        }
    }

    public class AcidBurstProjCharged : Projectile
    {
        int bounces = 0;
        public AcidBurstProjCharged(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 300, 0, player, "acidburst_charged_start", 0, 0f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 4f;
            projId = (int)ProjIds.AcidBurstCharged;
            useGravity = true;
            fadeSound = "acidBurst";
            if (type == 0)
            {
                vel = new Point(xDir * 75, -270);
            }
            else if (type == 1)
            {
                vel = new Point(xDir * 150, -200);
            }
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
            checkBigAcidUnderwater();
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            if (sprite.name == "acidburst_charged_start" && isAnimOver())
            {
                changeSprite("acidburst_charged", true);
                vel.x = xDir * 100;
            }

            checkBigAcidUnderwater();
        }

        public override void onHitWall(CollideData other)
        {
            if (!ownedByLocalPlayer) return;
            acidSplashEffect(other, ProjIds.AcidBurstSmall);
            bounces++;
            if (bounces > 4)
            {
                destroySelf();
                return;
            }

            var normal = other.hitData.normal ?? new Point(0, -1);

            if (normal.isSideways())
            {
                vel.x *= -1;
                incPos(new Point(5 * MathF.Sign(vel.x), 0));
            }
            else
            {
                vel.y *= -1;
                if (vel.y < -300) vel.y = -300;
                incPos(new Point(0, 5 * MathF.Sign(vel.y)));
            }
            playSound("acidBurst", sendRpc: true);
        }

        bool acidSplashOnce;
        public override void onHitDamagable(IDamagable damagable)
        {
            if (ownedByLocalPlayer)
            {
                if (!acidSplashOnce) 
                {
                    acidSplashOnce = true;
                    acidSplashParticles(pos, false, 1, 1, ProjIds.AcidBurstSmall);
                    acidFadeEffect();
                }
            }
            base.onHitDamagable(damagable);
        }
    }
}
