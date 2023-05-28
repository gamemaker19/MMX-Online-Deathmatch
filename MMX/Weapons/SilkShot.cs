using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class SilkShot : Weapon
    {
        public SilkShot() : base()
        {
            shootSounds = new List<string>() { "silkShot", "silkShot", "silkShot", "silkShotCharged" };
            rateOfFire = 0.75f;
            index = (int)WeaponIds.SilkShot;
            weaponBarBaseIndex = 11;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 11;
            killFeedIndex = 20 + (index - 9);
            weaknessIndex = 16;
        }

        public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId)
        {
            if (chargeLevel != 3)
            {
                new SilkShotProj(this, pos, xDir, player, netProjId);
            }
            else
            {
                new SilkShotProjCharged(this, pos, xDir, player, netProjId);
            }
        }
    }

    public class SilkShotProj : Projectile
    {
        bool splitOnce;
        public SilkShotProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) : base(weapon, pos, xDir, 200, 2, player, "silkshot_proj", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            fadeSprite = "explosion";
            fadeSound = "explosion";
            useGravity = true;
            vel.y = -100;
            projId = (int)ProjIds.SilkShot;
            healAmount = 2;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
            frameSpeed = 0;
            frameIndex = Helpers.randomRange(0, sprite.frames.Count - 1);
            
        }

        public override void onHitWall(CollideData other)
        {
            if (!ownedByLocalPlayer) return;
            split();
            destroySelf();
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            if (ownedByLocalPlayer) split();
            base.onHitDamagable(damagable);
        }

        public void split()
        {
            if (!splitOnce) splitOnce = true;
            else return;

            new SilkShotProjShrapnel(weapon, pos, xDir, damager.owner, 0, new Point(-1, -1), damager.owner.getNextActorNetId(), rpc: true);
            new SilkShotProjShrapnel(weapon, pos, xDir, damager.owner, 0, new Point(-1, 1), damager.owner.getNextActorNetId(), rpc: true);
            new SilkShotProjShrapnel(weapon, pos, xDir, damager.owner, 0, new Point(1, -1), damager.owner.getNextActorNetId(), rpc: true);
            new SilkShotProjShrapnel(weapon, pos, xDir, damager.owner, 0, new Point(1, 1), damager.owner.getNextActorNetId(), rpc: true);
        }
    }

    public class SilkShotProjShrapnel : Projectile
    {
        public SilkShotProjShrapnel(Weapon weapon, Point pos, int xDir, Player player, int type, Point vel, ushort netProjId, bool rpc = false) : base(weapon, pos, xDir, 300, 1, player, "silkshot_piece", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 0.6f;
            reflectable = true;
            this.vel.x *= MathF.Sign(vel.x);
            this.vel.y = speed * MathF.Sign(vel.y);
            if (type == 1) changeSprite("silkshot_proj", true);
            projId = (int)ProjIds.SilkShotShrapnel;
            healAmount = 1;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }

    public class SilkShotProjCharged : Projectile
    {
        bool splitOnce;
        public SilkShotProjCharged(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) : base(weapon, pos, xDir, 200, 4, player, "silkshot_proj_charged", Global.defFlinch, 0, netProjId, player.ownedByLocalPlayer)
        {
            fadeSprite = "explosion";
            fadeSound = "silkShotChargedExplosion";
            useGravity = true;
            vel.y = -100;
            projId = (int)ProjIds.SilkShotCharged;
            healAmount = 6;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void onHitWall(CollideData other)
        {
            if (!ownedByLocalPlayer) return;
            split();
            destroySelf();
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            if (ownedByLocalPlayer) split();
            base.onHitDamagable(damagable);
        }

        public void split()
        {
            if (!splitOnce) splitOnce = true;
            else return;

            new SilkShotProjShrapnel(weapon, pos, xDir, damager.owner, 1, new Point(-1, -1), damager.owner.getNextActorNetId(), rpc: true);
            new SilkShotProjShrapnel(weapon, pos, xDir, damager.owner, 1, new Point(-1, 1), damager.owner.getNextActorNetId(), rpc: true);
            new SilkShotProjShrapnel(weapon, pos, xDir, damager.owner, 1, new Point(1, -1), damager.owner.getNextActorNetId(), rpc: true);
            new SilkShotProjShrapnel(weapon, pos, xDir, damager.owner, 1, new Point(1, 1), damager.owner.getNextActorNetId(), rpc: true);

            new SilkShotProjShrapnel(weapon, pos, xDir, damager.owner, 0, new Point(-1, 0), damager.owner.getNextActorNetId(), rpc: true);
            new SilkShotProjShrapnel(weapon, pos, xDir, damager.owner, 0, new Point(-1, 0), damager.owner.getNextActorNetId(), rpc: true);
            new SilkShotProjShrapnel(weapon, pos, xDir, damager.owner, 0, new Point(0, -1), damager.owner.getNextActorNetId(), rpc: true);
            new SilkShotProjShrapnel(weapon, pos, xDir, damager.owner, 0, new Point(0, 1), damager.owner.getNextActorNetId(), rpc: true);
        }
    }
}
