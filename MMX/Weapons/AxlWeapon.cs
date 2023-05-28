using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public enum AxlBulletType
    {
        Normal,
        AltFire,
        Assassin,
        WhiteAxlCopyShot2
    }

    public class AxlWeapon : Weapon
    {
        public int altFire;
        public float altFireCooldown;
        public string flashSprite;
        public string chargedFlashSprite;
        public string sprite;

        public AxlWeapon(int altFire)
        {
            this.altFire = altFire;
        }

        public bool isTwoHanded(bool includeDoubleBullet)
        {
            if (includeDoubleBullet && this is DoubleBullet) return true;
            return this is GLauncher || this is IceGattling || this is FlameBurner;
        }
        public bool isSecondShot;

        public override void update()
        {
            base.update();
        }

        public virtual float whiteAxlFireRateMod()
        {
            return 1;
        }

        public virtual float whiteAxlAmmoMod()
        {
            return 1;
        }

        public virtual float miscAmmoMod(Character character)
        {
            return 1;
        }

        public virtual void axlShoot(Player player, AxlBulletType axlBulletType = AxlBulletType.Normal, int? overrideChargeLevel = null)
        {
            bool isWhiteAxlCopyShot = axlBulletType == AxlBulletType.WhiteAxlCopyShot2;
            if (axlBulletType == AxlBulletType.WhiteAxlCopyShot2) axlBulletType = AxlBulletType.AltFire;

            Character chr = player.character;

            if (player.ping != null)
            {
                float ping = player.ping.Value;
                chr.stealthRevealTime = ping / Character.stealthRevealPingDenom;
                if (chr.stealthRevealTime < Character.maxStealthRevealTime) chr.stealthRevealTime = Character.maxStealthRevealTime;
            }
            else
            {
                chr.stealthRevealTime = Character.maxStealthRevealTime;
            }
            int chargeLevel = axlBulletType == AxlBulletType.AltFire ? 3 : chr.getChargeLevel();
            if (chargeLevel == 3 && (this is AxlBullet || this is DoubleBullet))
            {
                chargeLevel = chr.getChargeLevel() + 1;
            }
            if (overrideChargeLevel != null)
            {
                chargeLevel = overrideChargeLevel.Value;
            }
            if (chr.isWhiteAxl())
            {
                if (this is AxlBullet)
                {
                    chargeLevel += 1;
                    if (chargeLevel >= 3) chargeLevel = 3;
                }
            }

            float ammoUsage = getAmmoUsage(chargeLevel) * Global.level.gameMode.getAmmoModifier() * (chr.isWhiteAxl() ? whiteAxlAmmoMod() : 1) * miscAmmoMod(chr);
            ammo -= ammoUsage;
            if (ammo < 0) ammo = 0;

            if (player.weapon.type > 0 && !chr.isWhiteAxl())
            {
                if (axlBulletType == AxlBulletType.AltFire && player.weapon is not DoubleBullet)
                {
                    for (int i = 0; i < ammoUsage; i++)
                    {
                        chr.ammoUsages.Add(0);
                    }
                }
                else
                {
                    for (int i = 0; i < ammoUsage; i++)
                    {
                        chr.ammoUsages.Add(1);
                    }
                }
            }

            bool isCharged = chargeLevel >= 3;

            Point bulletPos = chr.getAxlBulletPos();

            Point cursorPos = chr.getCorrectedCursorPos();

            var dirTo = bulletPos.directionTo(cursorPos);
            float aimAngle = dirTo.angle;

            if (isWhiteAxlCopyShot && isSecondShot)
            {
                bulletPos.inc(dirTo.normalize().times(-5));
            }

            Weapon weapon = player.weapon;
            if (axlBulletType == AxlBulletType.Assassin) weapon = new AssassinBullet();
            axlGetProjectile(weapon, bulletPos, chr.axlXDir, player, aimAngle, chr.axlCursorTarget, chr.axlHeadshotTarget, cursorPos, chargeLevel, player.getNextActorNetId());

            string fs = !isCharged ? flashSprite : chargedFlashSprite;
            if (this is RayGun && axlBulletType == AxlBulletType.AltFire && player.axlLoadout.rayGunAlt == 0) fs = "";
            if (!string.IsNullOrEmpty(fs))
            {
                if (fs == "axl_raygun_flash" && Global.level.gameMode.isTeamMode && player.alliance == GameMode.redAlliance) fs = "axl_raygun_flash2";
                chr.muzzleFlash.changeSprite(fs, true);
                chr.muzzleFlash.sprite.restart();
            }

            if (axlBulletType == AxlBulletType.AltFire)
            {
                chr.playSound(!isCharged ? "axlBullet" : shootSounds[3]);
            }
            else
            {
                chr.playSound(!isCharged ? shootSounds[0] : shootSounds[3]);
            }

            float rateOfFireMode = (chr.isWhiteAxl() ? whiteAxlFireRateMod() : 1);
            shootTime = rateOfFire / rateOfFireMode;

            if (axlBulletType == AxlBulletType.AltFire)
            {
                altShootTime = altFireCooldown / rateOfFireMode;
            }

            float switchCooldown = 0.3f;
            float slowSwitchCooldown = 0.6f;

            chr.switchTime = switchCooldown;
            chr.altSwitchTime = switchCooldown;
            if (shootTime > 0.25f || altShootTime > 0.25f)
            {
                chr.switchTime = slowSwitchCooldown;
                chr.altSwitchTime = slowSwitchCooldown;
            }

            float aimBackwardsAmount = chr.getAimBackwardsAmount();
            shootTime *= (1 + aimBackwardsAmount * 0.25f);
            altShootTime *= (1 + aimBackwardsAmount * 0.25f);

            isSecondShot = !isSecondShot;
        }

        public virtual void axlGetProjectile(Weapon weapon, Point bulletPos, int xDir, Player player, float angle, IDamagable target, Character headshotTarget, Point cursorPos, int chargeLevel, ushort netId)
        {
        }

        public float axlRechargeTime;
        public virtual void rechargeAxlBulletAmmo(Player player, Character chr, bool shootHeld, float modifier)
        {
            if (shootTime == 0 && chr.shootAnimTime == 0 && !shootHeld && ammo < maxAmmo)
            {
                float waMod = chr.isWhiteAxl() ? 0 : 1;
                axlRechargeTime += Global.spf;
                if (axlRechargeTime > 0.1f * modifier * waMod)
                {
                    axlRechargeTime = 0;

                    bool canAddAmmo = true;
                    if (type > 0)
                    {
                        int lastAmmoUsage = 1;
                        if (chr.ammoUsages.Count > 0)
                        {
                            lastAmmoUsage = chr.ammoUsages.Pop();
                        }

                        if (lastAmmoUsage > 0)
                        {
                            float maxAmmo = player.axlBulletTypeAmmo[type];
                            maxAmmo = Helpers.clampMin0(maxAmmo - 1);
                            player.axlBulletTypeAmmo[type] = maxAmmo;
                            if (maxAmmo <= 0) canAddAmmo = false;
                        }
                    }
                    if (canAddAmmo)
                    {
                        addAmmo(1, player);
                    }
                }
            }
        }
    }
}
