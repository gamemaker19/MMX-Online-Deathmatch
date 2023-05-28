using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class Sting : Weapon
    {
        public Sting() : base()
        {
            index = (int)WeaponIds.Sting;
            killFeedIndex = 2;
            weaponBarBaseIndex = 2;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 2;
            weaknessIndex = 7;
            shootSounds = new List<string>() { "csting", "csting", "csting", "stingCharge" };
            rateOfFire = 0.75f;
        }

        public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId)
        {
            if (chargeLevel != 3)
            {
                new StingProj(this, pos, xDir, player, 0, netProjId);
            }
            else
            {
                player.character.stingChargeTime = 8;
            }
        }
    }

    public class StingProj : Projectile
    {
        public int type = 0; //0 = initial proj, 1 = horiz, 2 = up, 3 = down
        public Point origVel;
        public StingProj(Weapon weapon, Point pos, int xDir, Player player, int type, ushort netProjId, Point? vel = null, bool rpc = false) : 
            base(weapon, pos, xDir, 300, 2, player, "sting_start", 0, 0.25f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.Sting;
            maxTime = 0.6f;
            if (type == 0)
            {
                origVel = this.vel.clone();
            }
            else if (type == 1)
            {
                var sprite = "sting_flat";
                changeSprite(sprite, false);
                reflectable = true;
            }
            else if (type == 2 || type == 3)
            {
                var sprite = "sting_up";
                if (type == 3)
                {
                    yDir = -1;
                }
                changeSprite(sprite, false);
                reflectable = true;
                damager.damage = 2;
                projId = (int)ProjIds.StingDiag;
            }
            if (vel != null) this.vel = (Point)vel;
            fadeSprite = "buster1_fade";
            this.type = type;
            /*
            if (player.character?.isInvisibleBS?.getValue() == true)
            {
                damager.damage = 1;
            }
            */

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();

            if (!ownedByLocalPlayer) return;

            if (type == 0 && time > 0.05)
            {
                vel.x = 0;
            }
            if (type == 0)
            {
                if (isAnimOver())
                {
                    new StingProj(weapon, pos.addxy(15 * xDir, 0), xDir, damager.owner, 1, Global.level.mainPlayer.getNextActorNetId(), origVel, rpc: true);
                    new StingProj(weapon, pos.addxy(15 * xDir, -8), xDir, damager.owner, 2, Global.level.mainPlayer.getNextActorNetId(), origVel.addxy(0, -150), rpc: true);
                    new StingProj(weapon, pos.addxy(15 * xDir, 8), xDir, damager.owner, 3, Global.level.mainPlayer.getNextActorNetId(), origVel.addxy(0, 150), rpc: true);
                    destroySelfNoEffect();
                }
            }
        }
    }
}
