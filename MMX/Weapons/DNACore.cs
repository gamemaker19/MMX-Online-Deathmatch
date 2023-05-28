using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class UndisguiseWeapon : AxlWeapon
    {
        public UndisguiseWeapon() : base(0)
        {
            ammo = 0;
            index = (int)WeaponIds.Undisguise;
            weaponSlotIndex = 50;
            sprite = "axl_arm_pistol";
        }
    }

    public enum DNACoreHyperMode
    {
        None,
        VileMK2,
        VileMK5,
        BlackZero,
        WhiteAxl,
        AwakenedZero,
        NightmareZero,
    }

    public class DNACore : AxlWeapon
    {
        public int charNum;
        public LoadoutData loadout;
        public float maxHealth;
        public string name;
        public int alliance;
        public ushort armorFlag;
        public bool frozenCastle;
        public bool speedDevil;
        public bool ultimateArmor;
        public DNACoreHyperMode hyperMode;
        public float rakuhouhaAmmo;
        public List<Weapon> weapons = new List<Weapon>();

        public DNACore(Character character) : base(0)
        {
            if (character != null)
            {
                charNum = character.player.charNum;
                loadout = character.player.loadout;
                maxHealth = character.player.maxHealth;
                name = character.player.name;
                alliance = character.player.alliance;
                armorFlag = character.player.armorFlag;
                frozenCastle = character.isFrozenCastleActiveBS.getValue();
                speedDevil = character.isSpeedDevilActiveBS.getValue();
                ultimateArmor = character.hasUltimateArmorBS.getValue();
                if (charNum == 0) weapons = loadout.xLoadout.getWeaponsFromLoadout(character.player);
                if (charNum == 1)
                {
                    rakuhouhaAmmo = character.player.zeroGigaAttackWeapon.ammo;
                    if (character.isNightmareZeroBS.getValue()) rakuhouhaAmmo = character.player.zeroDarkHoldWeapon.ammo;
                }
                if (charNum == 2) weapons = loadout.vileLoadout.getWeaponsFromLoadout(false);
                if (charNum == 3)
                {
                    weapons = loadout.axlLoadout.getWeaponsFromLoadout();
                    if (weapons.Count > 0 && character.player.axlBulletType > 0)
                    {
                        weapons[0] = character.player.getAxlBulletWeapon();
                    }
                }
                if (charNum == 4)
                {
                    rakuhouhaAmmo = character.player.sigmaAmmo;
                }

                // For any hyper modes added here, be sure to de-apply them if "preserve undisguise" is used in: axl.updateDisguisedAxl()
                if (character.sprite.name.Contains("vilemk2")) hyperMode = DNACoreHyperMode.VileMK2;
                else if (character.sprite.name.Contains("vilemk5")) hyperMode = DNACoreHyperMode.VileMK5;
                else if (character.isBlackZero()) hyperMode = DNACoreHyperMode.BlackZero;
                else if (character.isWhiteAxl()) hyperMode = DNACoreHyperMode.WhiteAxl;
                else if (character.isAwakenedZeroBS.getValue()) hyperMode = DNACoreHyperMode.AwakenedZero;
                else if (character.isNightmareZeroBS.getValue()) hyperMode = DNACoreHyperMode.NightmareZero;
            }

            rateOfFire = 1f;
            index = (int)WeaponIds.DNACore;
            weaponBarBaseIndex = 30 + charNum;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 30 + charNum;
            if (charNum == 4) weaponSlotIndex = 65;
            sprite = "axl_arm_pistol";
        }

        public override void axlShoot(Player player, AxlBulletType axlBulletType = AxlBulletType.Normal, int? overrideChargeLevel = null)
        {
            player.lastDNACore = this;
            player.lastDNACoreIndex = player.weaponSlot;
            player.savedDNACoreWeapons.Remove(this);
            player.weapons.RemoveAt(player.weaponSlot);
            player.character.cleanupBeforeTransform();
            player.preTransformedAxl = player.character;
            Global.level.gameObjects.Remove(player.preTransformedAxl);
            player.transformAxl(this);
        }
    }
}
