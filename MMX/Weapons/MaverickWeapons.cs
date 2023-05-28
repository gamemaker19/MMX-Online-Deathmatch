using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class MaverickWeapon : Weapon
    {
        public Player player;
        public bool isMenuOpened;
        public float cooldown;
        public bool summonedOnce;
        public float lastHealth;
        public const float summonerCooldown = 2;
        public const float tagTeamCooldown = 4;
        public const float strikerCooldown = 4;
        public SavedMaverickData smd;
        protected Maverick _maverick;
        public Maverick maverick
        {
            get
            {
                if (_maverick != null && _maverick.destroyed)
                {
                    cooldown = _maverick.player.isTagTeam() ? tagTeamCooldown : strikerCooldown;
                    lastHealth = _maverick.health;
                    if (_maverick.health <= 0) smd = null;
                    else smd = new SavedMaverickData(_maverick);
                    _maverick = null;
                }
                return _maverick;
            }
            set
            {
                _maverick = value;
            }
        }
        public int selCommandIndex = 2;
        public int selCommandIndexX = 1;
        public const int maxCommandIndex = 4;
        public float scrapHUDAnimTime;
        public const float scrapHUDMaxAnimTime = 0.75f;
        public float scrapGainCooldown;
        public float scrapGainMaxCooldown
        {
            get
            {
                return 10 + player.scrap;
            }
        }

        public bool isMoth;

        public MaverickWeapon(Player player)
        {
            lastHealth = player?.getMaverickMaxHp() ?? 32;
            this.player = player;
        }

        public override void update()
        {
            base.update();
            Helpers.decrementTime(ref cooldown);

            if (player != null && !summonedOnce && !player.isStriker() && player.character != null)
            {
                if (scrapGainCooldown < scrapGainMaxCooldown)
                {
                    scrapGainCooldown += Global.spf;
                    if (scrapGainCooldown >= scrapGainMaxCooldown)
                    {
                        scrapGainCooldown = 0;
                        scrapHUDAnimTime = Global.spf;
                        player.scrap++;
                    }
                }
            }

            if (scrapHUDAnimTime > 0)
            {
                scrapHUDAnimTime += Global.spf;
                if (scrapHUDAnimTime > scrapHUDMaxAnimTime)
                {
                    scrapHUDAnimTime = 0;
                }
            }
        }

        public Maverick summon(Player player, Point pos, Point destPos, int xDir, bool isMothHatch = false)
        {
            // X1
            if (this is ChillPenguinWeapon)
            {
                maverick = new ChillPenguin(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is SparkMandrillWeapon)
            {
                maverick = new SparkMandrill(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is ArmoredArmadilloWeapon)
            {
                maverick = new ArmoredArmadillo(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is LaunchOctopusWeapon)
            {
                maverick = new LaunchOctopus(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is BoomerKuwangerWeapon)
            {
                maverick = new BoomerKuwanger(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is StingChameleonWeapon)
            {
                maverick = new StingChameleon(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is StormEagleWeapon)
            {
                maverick = new StormEagle(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is FlameMammothWeapon)
            {
                maverick = new FlameMammoth(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is VelguarderWeapon)
            {
                maverick = new Velguarder(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            // X2
            else if (this is WireSpongeWeapon)
            {
                maverick = new WireSponge(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is WheelGatorWeapon)
            {
                maverick = new WheelGator(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is BubbleCrabWeapon)
            {
                maverick = new BubbleCrab(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is FlameStagWeapon)
            {
                maverick = new FlameStag(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is MorphMothWeapon mmw)
            {
                if (mmw.isMoth)
                {
                    maverick = new MorphMoth(player, pos, destPos, xDir, player.getNextActorNetId(), true, isMothHatch, sendRpc: true);
                }
                else
                {
                    maverick = new MorphMothCocoon(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
                }
            }
            else if (this is MagnaCentipedeWeapon)
            {
                maverick = new MagnaCentipede(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is CrystalSnailWeapon)
            {
                maverick = new CrystalSnail(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is OverdriveOstrichWeapon)
            {
                maverick = new OverdriveOstrich(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is FakeZeroWeapon)
            {
                maverick = new FakeZero(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            // X3
            else if (this is BlizzardBuffaloWeapon)
            {
                maverick = new BlizzardBuffalo(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is ToxicSeahorseWeapon)
            {
                maverick = new ToxicSeahorse(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is TunnelRhinoWeapon)
            {
                maverick = new TunnelRhino(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is VoltCatfishWeapon)
            {
                maverick = new VoltCatfish(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is CrushCrawfishWeapon)
            {
                maverick = new CrushCrawfish(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is NeonTigerWeapon)
            {
                maverick = new NeonTiger(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is GravityBeetleWeapon)
            {
                maverick = new GravityBeetle(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is BlastHornetWeapon)
            {
                maverick = new BlastHornet(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            else if (this is DrDopplerWeapon ddw)
            {
                maverick = new DrDoppler(player, pos, destPos, xDir, player.getNextActorNetId(), true, sendRpc: true);
                (maverick as DrDoppler).ballType = ddw.ballType;
            }

            if (maverick != null)
            {
                maverick.setHealth(lastHealth);
                smd?.applySavedMaverickData(maverick, player.isPuppeteer());
                if (player.isStriker())
                {
                    if (maverick is not MorphMothCocoon)
                    {
                        maverick.ammo = maverick.maxAmmo;
                    }
                }
                summonedOnce = true;
            }

            return maverick;
        }

        public bool canUseSubtank(SubTank subtank)
        {
            if (player.isTagTeam()) return false;
            return maverick != null && maverick.health < maverick.maxHealth;
        }
    }

    public class SigmaMenuWeapon : Weapon
    {
        public SigmaMenuWeapon()
        {
            index = (int)WeaponIds.Sigma;
            weaponSlotIndex = 65;
            displayName = "Sigma";
            rateOfFire = 4;
        }
    }

    public class ChillPenguinWeapon : MaverickWeapon
    {
        public ChillPenguinWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.ChillPenguin;
            weaponSlotIndex = 66;
            displayName = "Chill Penguin";
        }
    }

    public class SparkMandrillWeapon : MaverickWeapon
    {
        public SparkMandrillWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.SparkMandrill;
            weaponSlotIndex = 67;
            displayName = "Spark Mandrill";
        }
    }

    public class ArmoredArmadilloWeapon : MaverickWeapon
    {
        public ArmoredArmadilloWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.ArmoredArmadillo;
            weaponSlotIndex = 68;
            displayName = "Armored Armadillo";
        }
    }

    public class LaunchOctopusWeapon : MaverickWeapon
    {
        public LaunchOctopusWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.LaunchOctopus;
            weaponSlotIndex = 69;
            displayName = "Launch Octopus";
        }
    }

    public class BoomerKuwangerWeapon : MaverickWeapon
    {
        public BoomerKuwangerWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.BoomerKuwanger;
            weaponSlotIndex = 70;
            displayName = "Boomer Kuwanger";
        }
    }

    public class StingChameleonWeapon : MaverickWeapon
    {
        public StingChameleonWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.StingChameleon;
            weaponSlotIndex = 71;
            displayName = "Sting Chameleon";
        }
    }

    public class StormEagleWeapon : MaverickWeapon
    {
        public StormEagleWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.StormEagle;
            weaponSlotIndex = 72;
            displayName = "Storm Eagle";
        }
    }

    public class FlameMammothWeapon : MaverickWeapon
    {
        public FlameMammothWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.FlameMammoth;
            weaponSlotIndex = 73;
            displayName = "Flame Mammoth";
        }
    }

    public class VelguarderWeapon : MaverickWeapon
    {
        public VelguarderWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.Velguarder;
            weaponSlotIndex = 74;
            displayName = "Velguarder";
        }
    }

    public class WireSpongeWeapon : MaverickWeapon
    {
        public WireSpongeWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.WireSponge;
            weaponSlotIndex = 75;
            displayName = "Wire Sponge";
        }
    }

    public class WheelGatorWeapon : MaverickWeapon
    {
        public WheelGatorWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.WheelGator;
            weaponSlotIndex = 76;
            displayName = "Wheel Gator";
        }
    }

    public class BubbleCrabWeapon : MaverickWeapon
    {
        public BubbleCrabWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.BubbleCrab;
            weaponSlotIndex = 77;
            displayName = "Bubble Crab";
        }
    }

    public class FlameStagWeapon : MaverickWeapon
    {
        public FlameStagWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.FlameStag;
            weaponSlotIndex = 78;
            displayName = "Flame Stag";
        }
    }

    public class MorphMothWeapon : MaverickWeapon
    {
        public MorphMothWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.MorphMoth;
            weaponSlotIndex = 79;
            displayName = "Morph Moth";
        }

        public override void update()
        {
            base.update();
            if (!isMoth) weaponSlotIndex = 109;
            else weaponSlotIndex = 79;
        }
    }

    public class MagnaCentipedeWeapon : MaverickWeapon
    {
        public MagnaCentipedeWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.MagnaCentipede;
            weaponSlotIndex = 80;
            displayName = "Magna Centipede";
        }
    }

    public class CrystalSnailWeapon : MaverickWeapon
    {
        public CrystalSnailWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.CrystalSnail;
            weaponSlotIndex = 81;
            displayName = "Crystal Snail";
        }
    }

    public class OverdriveOstrichWeapon : MaverickWeapon
    {
        public OverdriveOstrichWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.OverdriveOstrich;
            weaponSlotIndex = 82;
            displayName = "Overdrive Ostrich";
        }
    }

    public class FakeZeroWeapon : MaverickWeapon
    {
        public FakeZeroWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.FakeZero;
            weaponSlotIndex = 83;
            displayName = "Fake Zero";
        }
    }

    public class BlizzardBuffaloWeapon : MaverickWeapon
    {
        public BlizzardBuffaloWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.BlizzardBuffalo;
            weaponSlotIndex = 84;
            displayName = "Blizzard Buffalo";
        }
    }

    public class ToxicSeahorseWeapon : MaverickWeapon
    {
        public ToxicSeahorseWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.ToxicSeahorse;
            weaponSlotIndex = 85;
            displayName = "Toxic Seahorse";
        }
    }

    public class TunnelRhinoWeapon : MaverickWeapon
    {
        public TunnelRhinoWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.TunnelRhino;
            weaponSlotIndex = 86;
            displayName = "Tunnel Rhino";
        }
    }

    public class VoltCatfishWeapon : MaverickWeapon
    {
        public VoltCatfishWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.VoltCatfish;
            weaponSlotIndex = 87;
            displayName = "Volt Catfish";
        }
    }

    public class CrushCrawfishWeapon : MaverickWeapon
    {
        public CrushCrawfishWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.CrushCrawfish;
            weaponSlotIndex = 88;
            displayName = "Crush Crawfish";
        }
    }

    public class NeonTigerWeapon : MaverickWeapon
    {
        public NeonTigerWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.NeonTiger;
            weaponSlotIndex = 89;
            displayName = "Neon Tiger";
        }
    }

    public class GravityBeetleWeapon : MaverickWeapon
    {
        public GravityBeetleWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.GravityBeetle;
            weaponSlotIndex = 90;
            displayName = "Gravity Beetle";
        }
    }

    public class BlastHornetWeapon : MaverickWeapon
    {
        public BlastHornetWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.BlastHornet;
            weaponSlotIndex = 91;
            displayName = "Blast Hornet";
        }
    }

    public class DrDopplerWeapon : MaverickWeapon
    {
        public int ballType; // 0 = shock gun, 1 = vaccine
        public DrDopplerWeapon(Player player) : base(player)
        {
            index = (int)WeaponIds.DrDoppler;
            weaponSlotIndex = 92;
            displayName = "Dr. Doppler";
        }
    }
}
