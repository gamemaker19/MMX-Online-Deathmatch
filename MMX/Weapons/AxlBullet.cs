using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public enum AxlBulletWeaponType
    {
        AxlBullets,
        MetteurCrash,
        BeastKiller,
        MachineBullets,
        DoubleBullets,
        RevolverBarrel,
        AncientGun
    }

    public class AxlBullet : AxlWeapon
    {
        public AxlBullet(AxlBulletWeaponType type = AxlBulletWeaponType.AxlBullets) : base(0)
        {
            shootSounds = new List<string>() { "axlBullet", "axlBullet", "axlBullet", "axlBulletCharged" };

            this.type = (int)type;
            if (type == AxlBulletWeaponType.AxlBullets)
            {
                index = (int)WeaponIds.AxlBullet;
                weaponBarBaseIndex = 28;
                weaponBarIndex = 28;
                weaponSlotIndex = 28;
                killFeedIndex = 28;
                sprite = "axl_arm_pistol";
                flashSprite = "axl_pistol_flash";
                chargedFlashSprite = "axl_pistol_flash_charged";
                altFireCooldown = 0.3f;
                displayName = "Axl Bullets";
            }
            else if (type == AxlBulletWeaponType.MetteurCrash)
            {
                index = (int)WeaponIds.MetteurCrash;
                weaponBarBaseIndex = 48;
                weaponBarIndex = 28;
                weaponSlotIndex = 99;
                killFeedIndex = 127;
                sprite = "axl_arm_metteurcrash";
                flashSprite = "axl_pistol_flash";
                chargedFlashSprite = "axl_pistol_flash_charged";
                altFireCooldown = 0.3f;
                displayName = "Mettaur Crash";
                shootSounds = new List<string>() { "mettaurCrash", "axlBullet", "axlBullet", "axlBulletCharged" };
            }
            else if (type == AxlBulletWeaponType.BeastKiller)
            {
                index = (int)WeaponIds.BeastHunter;
                weaponBarBaseIndex = 46;
                weaponBarIndex = 28;
                weaponSlotIndex = 97;
                killFeedIndex = 128;
                sprite = "axl_arm_beastkiller";
                flashSprite = "axl_pistol_flash";
                chargedFlashSprite = "axl_pistol_flash_charged";
                altFireCooldown = 0.3f;
                rateOfFire = 0.75f;
                displayName = "Beast Killer";
                shootSounds = new List<string>() { "beastKiller", "axlBullet", "axlBullet", "axlBulletCharged" };
            }
            else if (type == AxlBulletWeaponType.MachineBullets)
            {
                index = (int)WeaponIds.MachineBullets;
                weaponBarBaseIndex = 45;
                weaponBarIndex = 28;
                weaponSlotIndex = 96;
                killFeedIndex = 129;
                sprite = "axl_arm_machinebullets";
                flashSprite = "axl_pistol_flash";
                chargedFlashSprite = "axl_pistol_flash_charged";
                altFireCooldown = 0.3f;
                rateOfFire = 0.15f;
                displayName = "Machine Bullets";
                shootSounds = new List<string>() { "machineBullets", "axlBullet", "axlBullet", "axlBulletCharged" };
            }
            else if (type == AxlBulletWeaponType.RevolverBarrel)
            {
                index = (int)WeaponIds.RevolverBarrel;
                weaponBarBaseIndex = 47;
                weaponBarIndex = 28;
                weaponSlotIndex = 98;
                killFeedIndex = 130;
                sprite = "axl_arm_revolverbarrel";
                flashSprite = "axl_pistol_flash";
                chargedFlashSprite = "axl_pistol_flash_charged";
                altFireCooldown = 0.3f;
                displayName = "Revolver Barrel";
                shootSounds = new List<string>() { "revolverBarrel", "axlBullet", "axlBullet", "axlBulletCharged" };
            }
            else if (type == AxlBulletWeaponType.AncientGun)
            {
                index = (int)WeaponIds.AncientGun;
                weaponBarBaseIndex = 49;
                weaponBarIndex = 28;
                weaponSlotIndex = 100;
                killFeedIndex = 131;
                sprite = "axl_arm_ancientgun";
                flashSprite = "axl_pistol_flash";
                chargedFlashSprite = "axl_pistol_flash_charged";
                altFireCooldown = 0.225f;
                rateOfFire = 0.1f;
                displayName = "Ancient Gun";
                shootSounds = new List<string>() { "ancientGun3", "axlBullet", "axlBullet", "axlBulletCharged" };
            }
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            if (type == (int)AxlBulletWeaponType.MachineBullets) return 2;
            if (type == (int)AxlBulletWeaponType.BeastKiller) return 3;
            if (chargeLevel == 1) return 4;
            else if (chargeLevel == 2) return 6;
            else if (chargeLevel >= 3) return 8;
            else return 1;
            //return 0;
        }

        public override void axlGetProjectile(Weapon weapon, Point bulletPos, int xDir, Player player, float angle, IDamagable target, Character headshotTarget, Point cursorPos, int chargeLevel, ushort netId)
        {
            Point bulletDir = Point.createFromAngle(angle);
            Projectile bullet = null;
            if (chargeLevel == 0)
            {
                if (type == (int)AxlBulletWeaponType.AxlBullets)
                {
                    bullet = new AxlBulletProj(weapon, bulletPos, player, bulletDir, netId);
                }
                else if (type == (int)AxlBulletWeaponType.MetteurCrash)
                {
                    bullet = new MettaurCrashProj(weapon, bulletPos, player, bulletDir, netId, sendRpc: true);
                }
                else if (type == (int)AxlBulletWeaponType.BeastKiller)
                {
                    bullet = new BeastKillerProj(weapon, bulletPos, player, Point.createFromAngle(angle - 45), player.getNextActorNetId(), sendRpc: true);
                    bullet = new BeastKillerProj(weapon, bulletPos, player, Point.createFromAngle(angle - 22.5f), player.getNextActorNetId(), sendRpc: true);
                    bullet = new BeastKillerProj(weapon, bulletPos, player, bulletDir, player.getNextActorNetId(), sendRpc: true);
                    bullet = new BeastKillerProj(weapon, bulletPos, player, Point.createFromAngle(angle + 22.5f), player.getNextActorNetId(), sendRpc: true);
                    bullet = new BeastKillerProj(weapon, bulletPos, player, Point.createFromAngle(angle + 45), player.getNextActorNetId(), sendRpc: true);
                }
                else if (type == (int)AxlBulletWeaponType.MachineBullets)
                {
                    bullet = new MachineBulletProj(weapon, bulletPos, player, Point.createFromAngle(angle + Helpers.randomRange(-25, 25)), player.getNextActorNetId(), sendRpc: true);
                    bullet = new MachineBulletProj(weapon, bulletPos, player, Point.createFromAngle(angle + Helpers.randomRange(-25, 25)), player.getNextActorNetId(), sendRpc: true);
                }
                else if (type == (int)AxlBulletWeaponType.RevolverBarrel)
                {
                    bullet = new RevolverBarrelProj(weapon, bulletPos, player, bulletDir, netId, sendRpc: true);
                }
                else if (type == (int)AxlBulletWeaponType.AncientGun)
                {
                    bullet = new AncientGunProj(weapon, bulletPos, player, bulletDir, player.getNextActorNetId(), sendRpc: true);
                    bullet = new AncientGunProj(weapon, bulletPos, player, Point.createFromAngle(angle + Helpers.randomRange(-25, 25)), player.getNextActorNetId(), sendRpc: true);
                }
            }
            else
            {
                bullet = new CopyShotProj(weapon, bulletPos, chargeLevel, player, bulletDir, netId);
            }

            if (player.ownedByLocalPlayer)
            {
                RPC.axlShoot.sendRpc(player.id, bullet.projId, netId, bulletPos, xDir, angle);
            }
        }
    }

    public class DoubleBullet : AxlWeapon
    {
        public DoubleBullet() : base(0)
        {
            sprite = "axl_arm_pistol";
            flashSprite = "axl_pistol_flash";
            chargedFlashSprite = "axl_pistol_flash_charged";
            shootSounds = new List<string>() { "axlBullet", "axlBullet", "axlBullet", "axlBulletCharged" };
            index = (int)WeaponIds.DoubleBullet;
            weaponBarBaseIndex = 31;
            weaponBarIndex = 28;
            weaponSlotIndex = 35;
            killFeedIndex = 34;
            altFireCooldown = 0.225f;
            rateOfFire = 0.1f;
            displayName = "Double Bullets";
            type = (int)AxlBulletWeaponType.DoubleBullets;
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            if (chargeLevel == 1) return 4;
            else if (chargeLevel == 2) return 6;
            else if (chargeLevel >= 3) return 8;
            else return 1;
        }

        public override void axlGetProjectile(Weapon weapon, Point bulletPos, int xDir, Player player, float angle, IDamagable target, Character headshotTarget, Point cursorPos, int chargeLevel, ushort netId)
        {
            Point bulletDir = Point.createFromAngle(angle);
            Projectile bullet = null;
            if (chargeLevel == 0)
            {
                bullet = new AxlBulletProj(weapon, bulletPos, player, bulletDir, netId);

            }
            else
            {
                bullet = new CopyShotProj(weapon, bulletPos, chargeLevel, player, bulletDir, netId);
            }

            if (player.ownedByLocalPlayer)
            {
                RPC.axlShoot.sendRpc(player.id, bullet.projId, netId, bulletPos, xDir, angle);
            }
        }
    }

    public class AxlBulletProj : Projectile
    {
        public AxlBulletProj(Weapon weapon, Point pos, Player player, Point bulletDir, ushort netProjId) : 
            base(weapon, pos, 1, 600, 1, player, "axl_bullet", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            fadeSprite = "axl_bullet_fade";
            projId = (int)ProjIds.AxlBullet;
            angle = bulletDir.angle;
            vel.x = bulletDir.x * speed;
            vel.y = bulletDir.y * speed;
            maxTime = 0.22f;
            reflectable = true;
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            destroySelf();
        }
    }

    public class MettaurCrashProj : Projectile
    {
        public MettaurCrashProj(Weapon weapon, Point pos, Player player, Point bulletDir, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, 1, 600, 1, player, "axl_bullet", 0, 0.1f, netProjId, player.ownedByLocalPlayer)
        {
            fadeSprite = "axl_bullet_fade";
            projId = (int)ProjIds.MetteurCrash;
            angle = bulletDir.angle;
            vel.x = bulletDir.x * speed;
            vel.y = bulletDir.y * speed;
            maxTime = 0.3f;
            reflectable = true;
            destroyOnHit = false;

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        /*
        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            destroySelf();
        }
        */

        public override void render(float x, float y)
        {
            DrawWrappers.DrawLine(pos.x, pos.y, pos.x + (deltaPos.normalize().x * 10), pos.y + (deltaPos.normalize().y * 10), SFML.Graphics.Color.Yellow, 2, 0, true);
        }
    }

    public class BeastKillerProj : Projectile
    {
        public BeastKillerProj(Weapon weapon, Point pos, Player player, Point bulletDir, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, 1, 600, 1, player, "beastkiller_proj", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            fadeSprite = "axl_bullet_fade";
            projId = (int)ProjIds.BeastKiller;
            angle = bulletDir.angle;
            vel.x = bulletDir.x * speed;
            vel.y = bulletDir.y * speed;
            maxTime = 0.22f;
            reflectable = true;

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            destroySelf();
        }
    }

    public class MachineBulletProj : Projectile
    {
        public MachineBulletProj(Weapon weapon, Point pos, Player player, Point bulletDir, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, 1, 600, 1, player, "machinebullet_proj", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            fadeSprite = "axl_bullet_fade";
            projId = (int)ProjIds.MachineBullets;
            angle = bulletDir.angle;
            vel.x = bulletDir.x * speed;
            vel.y = bulletDir.y * speed;
            maxTime = 0.22f;
            reflectable = true;

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            destroySelf();
        }
    }

    public class RevolverBarrelProj : Projectile
    {
        public RevolverBarrelProj(Weapon weapon, Point pos, Player player, Point bulletDir, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, 1, 600, 0.5f, player, "revolverbarrel_proj", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            fadeSprite = "axl_bullet_fade";
            projId = (int)ProjIds.RevolverBarrel;
            angle = bulletDir.angle;
            vel.x = bulletDir.x * speed;
            vel.y = bulletDir.y * speed;
            maxTime = 0.22f;
            reflectable = true;

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            destroySelf();
        }

        public override void update()
        {
            if (ownedByLocalPlayer && getHeadshotVictim(owner, out IDamagable victim, out Point? hitPoint))
            {
                if (hitPoint != null) changePos(hitPoint.Value);
                damager.applyDamage(victim, false, weapon, this, projId, overrideDamage: damager.damage * 3f);
                damager.damage = 0;
                playSound("hurt");
                destroySelf();
                return;
            }

            base.update();
        }
    }

    public class AncientGunProj : Projectile
    {
        public float sparkleTime = 0;
        public AncientGunProj(Weapon weapon, Point pos, Player player, Point bulletDir, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, 1, 600, 1, player, "ancientgun_proj", 0, 0.075f, netProjId, player.ownedByLocalPlayer)
        {
            fadeSprite = "axl_bullet_fade";
            projId = (int)ProjIds.AncientGun;
            angle = bulletDir.angle;
            vel.x = bulletDir.x * speed;
            vel.y = bulletDir.y * speed;
            maxTime = 0.3f;
            destroyOnHit = false;

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            sparkleTime += Global.spf;
            if (sparkleTime > 0.05)
            {
                sparkleTime = 0;
                new Anim(pos, "ancient_gun_sparkles", 1, null, true);
            }

            if (ownedByLocalPlayer && getHeadshotVictim(owner, out IDamagable victim, out Point? hitPoint))
            {
                //if (hitPoint != null) changePos(hitPoint.Value);
                damager.applyDamage(victim, false, weapon, this, projId, overrideDamage: damager.damage * 1.5f);
                //damager.damage = 0;
                playSound("hurt");
                //destroySelf();
                //return;
            }

            base.update();
        }
    }

    public class CopyShotProj : Projectile
    {
        public CopyShotProj(Weapon weapon, Point pos, int chargeLevel, Player player, Point bulletDir, ushort netProjId) :
            base(weapon, pos, 1, 250, 2, player, "axl_bullet_charged", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.CopyShot;

            xScale = 0.75f;
            yScale = 0.75f;

            reflectable = true;
            maxTime = 0.5f;

            /*
            if (player?.character?.isWhiteAxl() == true)
            {
                damager.flinch = Global.miniFlinch;
            }
            */

            if (chargeLevel == 2)
            {
                damager.damage = 3;
                speed *= 1.5f;
                maxTime /= 1.5f;
                xScale = 1f;
                yScale = 1f;
            }
            if (chargeLevel >= 3)
            {
                damager.damage = 4;
                speed *= 2f;
                maxTime /= 2f;
                xScale = 1.25f;
                yScale = 1.25f;
            }
            /*
            if (chargeLevel == 4)
            {
                damager.damage = 5;
                speed *= 2.5f;
                maxTime /= 2.5f;
                xScale = 1.5f;
                yScale = 1.5f;
            }
            */
            vel.x = bulletDir.x * speed;
            vel.y = bulletDir.y * speed;
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            destroySelf();
        }
    }

    public class CopyShotDamageEvent
    {
        public Character character;
        public float time;
        public CopyShotDamageEvent(Character character)
        {
            this.character = character;
            time = Global.time;
        }
    }
}
