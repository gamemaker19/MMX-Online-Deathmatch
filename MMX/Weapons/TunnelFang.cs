using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class TunnelFang : Weapon
    {
        public TunnelFang() : base()
        {
            shootSounds = new List<string>() { "buster", "buster", "buster", "tunnelFang" };
            rateOfFire = 1;
            index = (int)WeaponIds.TunnelFang;
            weaponBarBaseIndex = 24;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 24;
            killFeedIndex = 47;
            weaknessIndex = (int)WeaponIds.AcidBurst;
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            if (chargeLevel != 3)
            {
                if (timeSinceLastShoot != null && timeSinceLastShoot < rateOfFire) return 1;
                else return 2;
            }
            return 8;
        }

        public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId)
        {
            if (chargeLevel != 3)
            {
                if (player.character.ownedByLocalPlayer)
                {
                    if (timeSinceLastShoot != null && timeSinceLastShoot < rateOfFire)
                    {
                        new TunnelFangProj(this, pos, xDir, 1, player, netProjId, rpc: true);
                        new TunnelFangProj(this, pos, xDir, 2, player, player.getNextActorNetId(), rpc: true);
                        timeSinceLastShoot = null;
                    }
                    else
                    {
                        new TunnelFangProj(this, pos, xDir, 0, player, netProjId, rpc: true);
                        timeSinceLastShoot = 0;
                        shootTime = 0.5f;
                    }
                }
            }
            else
            {
                var ct = new TunnelFangProjCharged(this, pos, xDir, player, netProjId);
                if (player.character.ownedByLocalPlayer) player.character.chargedTunnelFang = ct;
            }
        }
    }

    public class TunnelFangProj : Projectile
    {
        int state = 0;
        float stateTime = 0;
        public Anim exhaust;
        int type;
        float sparksCooldown;
        public TunnelFangProj(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 100, 1, player, "tunnelfang_proj", 0, 0.25f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 1.5f;
            projId = (int)ProjIds.TunnelFang;
            exhaust = new Anim(pos, "tunnelfang_exhaust", xDir, null, false);
            exhaust.setzIndex(zIndex - 100);
            destroyOnHit = false;
            this.type = type;
            if (type != 0)
            {
                vel.x = 0;
                vel.y = (type == 1 ? -100 : 100);
                projId = (int)ProjIds.TunnelFang2;
            }

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            Helpers.decrementTime(ref sparksCooldown);
            exhaust.pos = pos;
            exhaust.xDir = xDir;

            if (!ownedByLocalPlayer) return;

            if (state == 0)
            {
                if (type == 0)
                {
                    if (stateTime > 0.15f)
                    {
                        vel.x = 0;
                    }
                }
                else if (type == 1 || type == 2)
                {
                    if (stateTime > 0.15f)
                    {
                        vel.y = 0;
                    }
                    if (stateTime > 0.15f && stateTime < 0.3f) vel.x = 100 * xDir;
                    else vel.x = 0;
                }
                stateTime += Global.spf;
                if (stateTime >= 0.75f)
                {
                    state = 1;
                }
            }
            else if (state == 1)
            {
                vel.x += Global.spf * 500 * xDir;
                if (MathF.Abs(vel.x) > 350) vel.x = 350 * xDir;
            }
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            base.onHitDamagable(damagable);
            if (ownedByLocalPlayer) vel.x = 4 * xDir;

            if (damagable is not CrackedWall)
            {
                time -= Global.spf;
                if (time < 0) time = 0;
            }

            if (sparksCooldown == 0)
            {
                playSound("tunnelFangDrill");
                var sparks = new Anim(pos, "tunnelfang_sparks", xDir, null, true);
                sparks.setzIndex(zIndex + 100);
                sparksCooldown = 0.25f;
            }
            var chr = damagable as Character;
            if (chr != null && chr.ownedByLocalPlayer && !chr.isImmuneToKnockback())
            {
                chr.vel = Point.lerp(chr.vel, Point.zero, Global.spf * 10);
                chr.slowdownTime = 0.25f;
            }
        }

        public override void onDestroy()
        {
            base.onDestroy();
            exhaust?.destroySelf();
        }
    }

    public class TunnelFangProjCharged : Projectile
    {
        public Character character;
        float sparksCooldown;
        public TunnelFangProjCharged(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 300, 1, player, "tunnelfang_charged", Global.defFlinch, 0.125f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.TunnelFangCharged;
            destroyOnHit = false;
            shouldShieldBlock = false;
            shouldVortexSuck = false;
            character = player.character;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            Helpers.decrementTime(ref sparksCooldown);

            if (!ownedByLocalPlayer) return;

            if (character == null || character.destroyed)
            {
                destroySelf();
                return;
            }

            character.player.weapon.addAmmo(-Global.spf * 5, character.player);
            if (character.player.weapon.ammo <= 0)
            {
                destroySelf();
                return;
            }

            if (character.player.weapon is not TunnelFang && character.player.weapon is not HyperBuster)
            {
                destroySelf();
                return;
            }
        }

        public override void postUpdate()
        {
            base.postUpdate();
            if (!ownedByLocalPlayer) return;
            if (destroyed) return;

            changePos(character.getShootPos());
            xDir = character.getShootXDir();
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            base.onHitDamagable(damagable);
            if (sparksCooldown == 0)
            {
                playSound("tunnelFangDrill");
                var sparks = new Anim(pos.addxy(15 * xDir, 0), "tunnelfang_sparks", xDir, null, true);
                sparks.setzIndex(zIndex + 100);
                sparksCooldown = 0.25f;
            }

            if (damagable is Character chr)
            {
                chr.vel = Point.lerp(chr.vel, Point.zero, Global.spf * 10);
                chr.slowdownTime = 0.25f;
            }
        }

        public override void onDestroy()
        {
            base.onDestroy();
            character?.removeBusterProjs();
        }
    }
}
