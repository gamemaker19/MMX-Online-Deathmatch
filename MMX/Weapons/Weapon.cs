using System;
using System.Collections.Generic;
using System.Text;

namespace MMXOnline
{
    public class Weapon
    {
        public List<string> shootSounds = new List<string>();
        public float ammo;
        public float maxAmmo;
        public float rateOfFire;
        public float? switchCooldown;
        public float soundTime = 0;
        public bool isStream = false;
        public float shootTime;
        public float altShootTime;
        public float streamTime;
        public string displayName;
        public string[] description;
        public Damager damager;
        public int type;    // For "swappable category" weapons, like techniques, vile weapon sections, etc.

        public int streams;
        public int maxStreams;
        public float streamCooldown;

        public int index;
        public int killFeedIndex;
        public int weaponBarBaseIndex;
        public int weaponBarIndex;
        public int weaponSlotIndex;
        public int weaknessIndex;
        public int vileWeight;

        public Weapon()
        {
            ammo = 32;
            maxAmmo = 32;
            rateOfFire = 0.15f;
            shootSounds = new List<string>() { "", "", "", "" };
        }

        public Weapon(WeaponIds index, int killFeedIndex, Damager damager = null)
        {
            this.index = (int)index;
            this.killFeedIndex = killFeedIndex;
            this.damager = damager;
        }

        public Weapon clone()
        {
            return MemberwiseClone() as Weapon;
        }

        public static List<Weapon> getAllSwitchableWeapons(AxlLoadout axlLoadout)
        {
            var weaponList = new List<Weapon>()
            {   
                new GigaCrush(),     
                new HyperBuster(),
                new NovaStrike(null),
                new DoubleBullet(),
                new DNACore(null),
                new VileMissile(VileMissileType.StunShot),
                new VileCannon(VileCannonType.FrontRunner),
                new Vulcan(VulcanType.CherryBlast),
            };
            weaponList.AddRange(getAllXWeapons());
            weaponList.AddRange(getAllAxlWeapons(axlLoadout));
            weaponList.AddRange(getAllSigmaWeapons(null));
            return weaponList;
        }

        public static List<Weapon> getAllSigmaWeapons(Player player, int? sigmaForm = null)
        {
            var weapons = new List<Weapon>()
            {
                new SigmaMenuWeapon(),
            };

            if (sigmaForm == null || sigmaForm == 0)
            {
                weapons.Add(new ChillPenguinWeapon(player));
                weapons.Add(new SparkMandrillWeapon(player));
                weapons.Add(new ArmoredArmadilloWeapon(player));
                weapons.Add(new LaunchOctopusWeapon(player));
                weapons.Add(new BoomerKuwangerWeapon(player));
                weapons.Add(new StingChameleonWeapon(player));
                weapons.Add(new StormEagleWeapon(player));
                weapons.Add(new FlameMammothWeapon(player));
                weapons.Add(new VelguarderWeapon(player));
            }
            if (sigmaForm == null || sigmaForm == 1)
            {
                weapons.Add(new WireSpongeWeapon(player));
                weapons.Add(new WheelGatorWeapon(player));
                weapons.Add(new BubbleCrabWeapon(player));
                weapons.Add(new FlameStagWeapon(player));
                weapons.Add(new MorphMothWeapon(player));
                weapons.Add(new MagnaCentipedeWeapon(player));
                weapons.Add(new CrystalSnailWeapon(player));
                weapons.Add(new OverdriveOstrichWeapon(player));
                weapons.Add(new FakeZeroWeapon(player));
            }
            if (sigmaForm == null || sigmaForm == 2)
            {
                weapons.Add(new BlizzardBuffaloWeapon(player));
                weapons.Add(new ToxicSeahorseWeapon(player));
                weapons.Add(new TunnelRhinoWeapon(player));
                weapons.Add(new VoltCatfishWeapon(player));
                weapons.Add(new CrushCrawfishWeapon(player));
                weapons.Add(new NeonTigerWeapon(player));
                weapons.Add(new GravityBeetleWeapon(player));
                weapons.Add(new BlastHornetWeapon(player));
                weapons.Add(new DrDopplerWeapon(player));
            }

            return weapons;
        }

        public static List<Weapon> getAllXWeapons()
        {
            return new List<Weapon>()
            {
                new Buster(),
                new Torpedo(),
                new Sting(),
                new RollingShield(),
                new FireWave(),
                new Tornado(),
                new ElectricSpark(),
                new Boomerang(),
                new ShotgunIce(),
                new CrystalHunter(),
                new BubbleSplash(),
                new SilkShot(),
                new SpinWheel(),
                new SonicSlicer(),
                new StrikeChain(),
                new MagnetMine(),
                new SpeedBurner(null),
                new AcidBurst(),
                new ParasiticBomb(),
                new TriadThunder(),
                new SpinningBlade(),
                new RaySplasher(),
                new GravityWell(),
                new FrostShield(),
                new TunnelFang(),
            };
        }

        public static List<Weapon> getAllAxlWeapons(AxlLoadout axlLoadout)
        {
            return new List<Weapon>()
            {
                new AxlBullet(),
                new RayGun(axlLoadout.rayGunAlt),
                new GLauncher(axlLoadout.blastLauncherAlt),
                new BlackArrow(axlLoadout.blackArrowAlt),
                new SpiralMagnum(axlLoadout.spiralMagnumAlt),
                new BoundBlaster(axlLoadout.boundBlasterAlt),
                new PlasmaGun(axlLoadout.plasmaGunAlt),
                new IceGattling(axlLoadout.iceGattlingAlt),
                new FlameBurner(axlLoadout.flameBurnerAlt),
            };
        }

        // friendlyIndex is 0-8.
        // Don't use this to generate weapons for use as they don't come with the right alt fire
        public static AxlWeapon fiToAxlWep(int friendlyIndex)
        {
            if (friendlyIndex == 0) return new AxlBullet();
            if (friendlyIndex == 1) return new RayGun(0);
            if (friendlyIndex == 2) return new GLauncher(0);
            if (friendlyIndex == 3) return new BlackArrow(0);
            if (friendlyIndex == 4) return new SpiralMagnum(0);
            if (friendlyIndex == 5) return new BoundBlaster(0);
            if (friendlyIndex == 6) return new PlasmaGun(0);
            if (friendlyIndex == 7) return new IceGattling(0);
            if (friendlyIndex == 8) return new FlameBurner(0);
            return null;
        }

        public static int wiToFi(int weaponIndex)
        {
            if (weaponIndex == (int)WeaponIds.AxlBullet) return 0;
            if (weaponIndex == (int)WeaponIds.RayGun) return 1;
            if (weaponIndex == (int)WeaponIds.GLauncher) return 2;
            if (weaponIndex == (int)WeaponIds.BlackArrow) return 3;
            if (weaponIndex == (int)WeaponIds.SpiralMagnum) return 4;
            if (weaponIndex == (int)WeaponIds.BoundBlaster) return 5;
            if (weaponIndex == (int)WeaponIds.PlasmaGun) return 6;
            if (weaponIndex == (int)WeaponIds.IceGattling) return 7;
            if (weaponIndex == (int)WeaponIds.FlameBurner) return 8;
            return 0;
        }

        public static List<int> getWeaponPool(bool includeBuster)
        {
            List<int> weaponPool;
            weaponPool = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };
            if (includeBuster) weaponPool.Insert(0, 0);
            return weaponPool;
        }

        public static List<int> getRandomXWeapons()
        {
            return Helpers.getRandomSubarray(getWeaponPool(true), 3);
        }

        public static List<int> getRandomAxlWeapons()
        {
            List<int> weaponPool = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8 };
            List<int> selWepIndices = Helpers.getRandomSubarray(weaponPool, 2);
            return new List<int>()
            {
                0,
                selWepIndices[0],
                selWepIndices[1],
            };
        }

        // Friendly reminder that this method MUST be deterministic across all clients, i.e. don't vary it on a field that could vary locally.
        public virtual void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId)
        {
        }

        public virtual float getAmmoUsage(int chargeLevel)
        {
            if (chargeLevel == 3) return 8;
            else return 1;
        }

        public float rechargeTime;
        public float rechargeCooldown;
        public virtual void rechargeAmmo(float maxRechargeTime)
        {
            rechargeCooldown -= Global.spf;
            if (rechargeCooldown < 0)
            {
                rechargeCooldown = 0;
            }
            if (rechargeCooldown == 0)
            {
                rechargeTime += Global.spf;
                if (rechargeTime > maxRechargeTime)
                {
                    rechargeTime = 0;
                    ammo++;
                    if (ammo > maxAmmo) ammo = maxAmmo;
                }
            }
        }

        public int spriteIndex
        {
            get
            {
                return index;  
            }
        
        }

        public float? timeSinceLastShoot;
        public virtual void update()
        {
            if (soundTime > 0)
            {
                soundTime = Helpers.clampMin(soundTime - Global.spf, 0);
            }
            Helpers.decrementTime(ref shootTime);
            Helpers.decrementTime(ref altShootTime);
            if (timeSinceLastShoot != null) timeSinceLastShoot += Global.spf;
        }
        
        public float getDamage(float currentDamage)
        {
            if (currentDamage == 1) return 1;
            if (ammo <= 0)
            {
                if (currentDamage == 7) return 3;
                if (currentDamage == 5) return 2;
                if (currentDamage == 3) return 1;
            }
            return currentDamage;
        }

        public void createBuster4Line(float x, float y, int xDir, Player player, float offsetTime)
        {
            new Buster4Proj(this, new Point(x + xDir, y), xDir, player, 3, 4, offsetTime, player.getNextActorNetId(true));
            new Buster4Proj(this, new Point(x + xDir * 8, y), xDir, player, 2, 3, offsetTime, player.getNextActorNetId(true));
            new Buster4Proj(this, new Point(x + xDir * 18, y), xDir, player, 2, 2, offsetTime, player.getNextActorNetId(true));
            new Buster4Proj(this, new Point(x + xDir * 32, y), xDir, player, 1, 1, offsetTime, player.getNextActorNetId(true));
            new Buster4Proj(this, new Point(x + xDir * 46, y), xDir, player, 0, 0, offsetTime, player.getNextActorNetId(true));
        }

        public bool isCooldownPercentDone(float percent)
        {
            if (rateOfFire == 0) return true;
            return (shootTime / rateOfFire) < (1 - percent);
        }

        public virtual void vileShoot(WeaponIds weaponInput, Character character)
        {
        }

        // For melee / zero weapons, etc.
        public virtual void attack(Character character)
        {

        }

        // Raijingeki2, etc.
        public virtual void attack2(Character character)
        {

        }

        public void shoot(Point pos, int xDir, Player player, int chargeLevel, ushort netProjId)
        {
            if (player.character == null) return;
            if (player.character.stockedCharge)
            {
                chargeLevel = 3;
            }

            getProjectile(pos, xDir, player, chargeLevel, netProjId);

            if (soundTime == 0)
            {
                if (shootSounds != null && shootSounds.Count > 0)
                {
                    player.character.playSound(shootSounds[chargeLevel]);
                }
                if (this is FireWave) 
                {
                    soundTime = 0.25f;
                }
            }

            // Only deduct ammo if owned by local player
            if (player.character.ownedByLocalPlayer)
            {
                float ammoUsage;
                if (player.character.isInvisibleBS.getValue() && chargeLevel != 3)
                {
                    ammoUsage = 4;
                }
                else if (this is FireWave)
                {
                    if (chargeLevel != 3)
                    {
                        float chargeTime = player.character.chargeTime;
                        if (chargeTime < 1)
                        {
                            ammoUsage = Global.spf * 10;
                        }
                        else
                        {
                            ammoUsage = Global.spf * 20;
                        }
                    }
                    else
                    {
                        ammoUsage = 8;
                    }
                }
                else if (this is Buster buster)
                {
                    ammoUsage = 0;
                }
                else
                {
                    ammoUsage = getAmmoUsage(chargeLevel);
                }
                addAmmo(-ammoUsage, player);

                /*
                if (ammo <= 0 && player.character?.isHyperX == true)
                {
                    player.weapons.Remove(this);
                    player.weaponSlot--;
                    if (player.weaponSlot < 0) player.weaponSlot = 0;
                }
                */
            }
        }

        public void addAmmo(float amount, Player player)
        {
            if (player.isX && player.hasChip(3) && amount < 0) amount *= 0.5f;
            ammo += amount;
            ammo = Helpers.clamp(ammo, 0, maxAmmo);
        }

        public virtual bool noAmmo()
        {
            return ammo <= 0;
        }

        public virtual bool canShoot(int chargeLevel, Player player)
        {
            return ammo > 0;
        }

        public virtual bool applyDamage(IDamagable victim, bool weakness, Actor actor, int projId, float? overrideDamage = null, int? overrideFlinch = null, bool sendRpc = true)
        {
            return damager?.applyDamage(victim, weakness, this, actor, projId, overrideDamage, overrideFlinch, sendRpc) ?? false;
        }

        public bool isCmWeapon()
        {
            return type > 0 && (this is AxlBullet || this is DoubleBullet);
        }
    }
}