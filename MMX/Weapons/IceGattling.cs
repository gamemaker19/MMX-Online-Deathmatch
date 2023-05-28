using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class IceGattling : AxlWeapon
    {
        public IceGattling(int altFire) : base(altFire)
        {
            shootSounds = new List<string>() { "iceGattling", "iceGattling", "iceGattling", "gaeaShield" };
            rateOfFire = 0.1f;
            index = (int)WeaponIds.IceGattling;
            weaponBarBaseIndex = 37;
            weaponSlotIndex = 57;
            killFeedIndex = 72;

            sprite = "axl_arm_icegattling";
            flashSprite = "axl_pistol_flash";
            chargedFlashSprite = "axl_pistol_flash";
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            if (chargeLevel == 3)
            {
                return 8;
            }
            return 0.5f;
        }

        public override void axlGetProjectile(Weapon weapon, Point bulletPos, int xDir, Player player, float angle, IDamagable target, Character headshotTarget, Point cursorPos, int chargeLevel, ushort netId)
        {
            if (!player.ownedByLocalPlayer) return;
            Point bulletDir = Point.createFromAngle(angle);
            Projectile bullet = null;
            if (chargeLevel == 0)
            {
                bullet = new IceGattlingProj(weapon, bulletPos, xDir, player, bulletDir, netId);
                RPC.axlShoot.sendRpc(player.id, bullet.projId, netId, bulletPos, xDir, angle);
            }
            else if (chargeLevel == 3)
            {
                player.character.gaeaShield = new GaeaShieldProj(weapon, bulletPos, xDir, player, netId, rpc: true);
                RPC.playSound.sendRpc(shootSounds[3], player.character?.netId);
            }
        }
    }


    public class IceGattlingProj : Projectile
    {
        public float sparkleTime = 0;
        public IceGattlingProj(Weapon weapon, Point pos, int xDir, Player player, Point bulletDir, ushort netProjId) : 
            base(weapon, pos, xDir, 400, 1, player, "icegattling_proj", 0, 0.1f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 0.4f;
            reflectable = true;
            vel.x = bulletDir.x * speed;
            vel.y = bulletDir.y * speed;
            projId = (int)ProjIds.IceGattling;
            fadeSprite = "icegattling_proj_fade";
            updateAngle();
            if (player.character?.isWhiteAxl() == true)
            {
                projId = (int)ProjIds.IceGattlingHyper;
            }
        }

        public void updateAngle()
        {
            angle = vel.angle;
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            destroySelf();
        }

        public override void update()
        {
            /*
            if (getHeadshotVictim(owner, out IDamagable victim, out Point? hitPoint))
            {
                damager.applyDamage(victim, false, weapon, this, (int)ProjIds.IceGattlingHeadshot);
                damager.damage = 0;
                destroySelf();
                return;
            }
            */

            base.update();

            sparkleTime += Global.spf;
            if (sparkleTime > 0.05)
            {
                sparkleTime = 0;
                new Anim(pos, "shotgun_ice_sparkles", 1, null, true);
            }
        }
    }


    public class GaeaShieldProj : Projectile
    {
        public Character character;
        public GaeaShieldProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, 1, 0, 0, player, "gaea_shield_proj", 0, 1, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 10;
            if (player.character?.isWhiteAxl() == true)
            {
                maxTime = 10;
            }
            fadeSound = "explosion";
            fadeSprite = "explosion";
            fadeOnAutoDestroy = true;
            projId = (int)ProjIds.GaeaShield;
            destroyOnHit = false;
            shouldVortexSuck = false;
            character = player.character;
            isReflectShield = true;
            isShield = true;
            shouldShieldBlock = false;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            if (character == null || character.destroyed)
            {
                destroySelf();
                return;
            }

            if (character.player.input.isPressed(Control.Special1, character.player))
            {
                destroySelf();
            }
        }

        public override void postUpdate()
        {
            base.postUpdate();
            if (!ownedByLocalPlayer) return;
            if (destroyed) return;

            Point bulletPos = character.getAxlBulletPos(1);
            angle = character.getShootAngle(true);
            changePos(bulletPos);
        }

        public override void onDestroy()
        {
            base.onDestroy();
            character.gaeaShield = null;
        }
    }
}
