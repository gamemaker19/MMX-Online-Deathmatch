using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class SonicSlicer : Weapon
    {
        public SonicSlicer() : base()
        {
            shootSounds = new List<string>() { "sonicSlicer", "sonicSlicer", "sonicSlicer", "sonicSlicerCharged" };
            rateOfFire = 1f;
            index = (int)WeaponIds.SonicSlicer;
            weaponBarBaseIndex = 13;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 13;
            killFeedIndex = 24;
            weaknessIndex = 9;
        }

        public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId)
        {
            if (chargeLevel != 3)
            {
                new SonicSlicerStart(this, pos, xDir, player, netProjId);
            }
            else
            {
                new Anim(pos, "sonicslicer_charge_start", xDir, null, true);
                player.setNextActorNetId(netProjId);
                new SonicSlicerProjCharged(this, pos, 0, player, player.getNextActorNetId(true));
                new SonicSlicerProjCharged(this, pos, 1, player, player.getNextActorNetId(true));
                new SonicSlicerProjCharged(this, pos, 2, player, player.getNextActorNetId(true));
                new SonicSlicerProjCharged(this, pos, 3, player, player.getNextActorNetId(true));
                new SonicSlicerProjCharged(this, pos, 4, player, player.getNextActorNetId(true));
            }
        }
    }

    public class SonicSlicerStart : Projectile
    {
        public SonicSlicerStart(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 0, 1, player, "sonicslicer_start", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.SonicSlicerChargedStart;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (sprite.isAnimOver())
            {
                new SonicSlicerProj(weapon, pos, xDir, 0, damager.owner, damager.owner.getNextActorNetId(), rpc: true);
                new SonicSlicerProj(weapon, pos, xDir, 1, damager.owner, damager.owner.getNextActorNetId(), rpc: true);
                destroySelf();
            }
        }
    }

    public class SonicSlicerProj : Projectile
    {
        public Sprite twin;
        int type;
        public SonicSlicerProj(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 200, 2, player, "sonicslicer_proj", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 0.75f;
            this.type = type;
            collider.wallOnly = true;
            projId = (int)ProjIds.SonicSlicer;

            twin = Global.sprites["sonicslicer_twin"].clone();

            vel.y = 50;
            if (type == 1)
            {
                vel.x *= 1.25f;
                frameIndex = 1;
            }
            if (type == 1)
            {
                vel.y = 0;
            }

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (type == 0) vel.y -= Global.spf * 100;
            else vel.y -= Global.spf * 50;

            var collideData = Global.level.checkCollisionActor(this, xDir, 0, vel);
            if (collideData != null && collideData.hitData != null)
            {
                playSound("dingX2");
                xDir *= -1;
                vel.x *= -1;
                new Anim(pos, "sonicslicer_sparks", xDir, null, true);
                RPC.actorToggle.sendRpc(netId, RPCActorToggleType.SonicSlicerBounce);
            }

            int velYSign = MathF.Sign(vel.y);
            if (velYSign != 0)
            {
                collideData = Global.level.checkCollisionActor(this, 0, velYSign, vel);
                if (collideData != null && collideData.hitData != null)
                {
                    playSound("dingX2");
                    vel.y *= -1;
                    new Anim(pos, "sonicslicer_sparks", xDir, null, true);
                    RPC.actorToggle.sendRpc(netId, RPCActorToggleType.SonicSlicerBounce);
                }
            }
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            float ox = -vel.x * Global.spf * 3;
            float oy = -vel.y * Global.spf * 3;
            twin.draw(frameIndex, pos.x + x + ox, pos.y + y + oy, 1, 1, null, 1, 1, 1, zIndex);
        }
    }

    public class SonicSlicerProjCharged : Projectile
    {
        public Point dest;
        public bool fall;
        public SonicSlicerProjCharged(Weapon weapon, Point pos, int num, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, 1, 300, 4, player, "sonicslicer_charged", Global.defFlinch, 0.25f, netProjId, player.ownedByLocalPlayer)
        {
            fadeSprite = "sonicslicer_charged_fade";
            maxTime = 1;
            projId = (int)ProjIds.SonicSlicerCharged;
            destroyOnHit = true;

            if (num == 0) dest = pos.addxy(-60, -100);
            if (num == 1) dest = pos.addxy(-30, -100);
            if (num == 2) dest = pos.addxy(-0, -100);
            if (num == 3) dest = pos.addxy(30, -100);
            if (num == 4) dest = pos.addxy(60, -100);

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
}
