using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    [ProtoContract]
    public class XLoadout
    {
        [ProtoMember(1)] public int weapon1;    //0 indexed
        [ProtoMember(2)] public int weapon2;
        [ProtoMember(3)] public int weapon3;
        [ProtoMember(4)] public int melee;

        public List<int> getXWeaponIndices()
        {
            return new List<int>() { weapon1, weapon2, weapon3 };
        }

        public void validate()
        {
            if (weapon1 < 0 || weapon1 > 24) weapon1 = 0;
            if (weapon2 < 0 || weapon2 > 24) weapon2 = 0;
            if (weapon3 < 0 || weapon3 > 24) weapon3 = 0;

            if ((weapon1 == weapon2 && weapon1 >= 0) ||
                (weapon1 == weapon3 && weapon2 >= 0) ||
                (weapon2 == weapon3 && weapon3 >= 0))
            {
                weapon1 = 0;
                weapon2 = 1;
                weapon3 = 2;
            }

            if (melee < 0 || melee > 1) melee = 0;
        }

        public List<Weapon> getWeaponsFromLoadout(Player player)
        {
            var indices = new List<byte>();
            indices.Add((byte)weapon1);
            indices.Add((byte)weapon2);
            indices.Add((byte)weapon3);
            if (player.hasArmArmor(3)) indices.Add((int)WeaponIds.HyperBuster);
            if (player.hasBodyArmor(2)) indices.Add((int)WeaponIds.GigaCrush);
            if (player.hasUltimateArmor()) indices.Add((int)WeaponIds.NovaStrike);

            return indices.Select(index =>
            {
                return Weapon.getAllSwitchableWeapons(new AxlLoadout()).Find(w => w.index == index).clone();
            }).ToList();
        }

        public static XLoadout createRandom()
        {
            var randomXWeapons = Weapon.getRandomXWeapons();
            return new XLoadout()
            {
                weapon1 = randomXWeapons[0],
                weapon2 = randomXWeapons[1],
                weapon3 = randomXWeapons[2],
            };
        }
    }

    [ProtoContract]
    public class ZeroLoadout
    {
        [ProtoMember(1)] public int uppercutS;
        [ProtoMember(2)] public int uppercutA;
        [ProtoMember(3)] public int downThrustS;
        [ProtoMember(4)] public int downThrustA;
        [ProtoMember(5)] public int gigaAttack;
        [ProtoMember(6)] public int hyperMode;
        [ProtoMember(7)] public int groundSpecial;
        [ProtoMember(8)] public int airSpecial;
        [ProtoMember(9)] public int melee;

        public static ZeroLoadout createRandom()
        {
            return new ZeroLoadout()
            {
                uppercutS = Helpers.randomRange(0, 2),
                uppercutA = Helpers.randomRange(0, 2),
                downThrustS = Helpers.randomRange(0, 2),
                downThrustA = Helpers.randomRange(0, 2),
                gigaAttack = Helpers.randomRange(0, 2),
                hyperMode = Helpers.randomRange(0, 2),
                groundSpecial = Helpers.randomRange(0, 2),
                airSpecial = Helpers.randomRange(0, 2),
                melee = Helpers.randomRange(0, 2),
            };
        }

        public void validate()
        {
            if (!inRange(uppercutS)) uppercutS = 0;
            if (!inRange(uppercutA)) uppercutA = 0;
            if (!inRange(downThrustS)) downThrustS = 0;
            if (!inRange(downThrustA)) downThrustA = 0;
            if (!inRange(gigaAttack)) gigaAttack = 0;
            if (!inRange(groundSpecial)) groundSpecial = 0;
            if (!inRange(airSpecial)) airSpecial = 0;
            if (uppercutA == uppercutS)
            {
                uppercutA++;
                if (uppercutA > 2) uppercutA = 0;
            }
            if (downThrustA == downThrustS)
            {
                downThrustA++;
                if (downThrustA > 2) downThrustA = 0;
            }
            if (hyperMode < 0 || hyperMode > 2) hyperMode = 0;
            if (melee < 0 || melee > 2) melee = 0;
        }

        private bool inRange(int weaponNum)
        {
            return weaponNum >= 0 && weaponNum <= 2;
        }
    }

    [ProtoContract]
    public class VileLoadout
    {
        [ProtoMember(1)] public int cannon;
        [ProtoMember(2)] public int vulcan;
        [ProtoMember(3)] public int missile;
        [ProtoMember(4)] public int rocketPunch;
        [ProtoMember(5)] public int napalm;
        [ProtoMember(6)] public int ball;
        [ProtoMember(7)] public int laser;
        [ProtoMember(8)] public int cutter;
        [ProtoMember(9)] public int flamethrower;
        [ProtoMember(10)] public int hyperMode;

        public const int maxWeight = 1000;

        public List<Weapon> getWeaponsFromLoadout(bool includeMech)
        {
            var weapons = new List<Weapon>();
            if (cannon > -1) weapons.Add(new VileCannon((VileCannonType)cannon));
            if (vulcan > -1) weapons.Add(new Vulcan((VulcanType)vulcan));

            if (includeMech)
            {
                weapons.Add(new MechMenuWeapon(VileMechMenuType.All));
                weapons = Helpers.sortWeapons(weapons, Options.main.weaponOrderingVile);
            }

            return weapons;
        }

        public static VileLoadout createRandom()
        {
            return new VileLoadout()
            {
                cannon = Helpers.randomRange(0, 2),
                vulcan = Helpers.randomRange(0, 2),
                missile = Helpers.randomRange(0, 2),
                rocketPunch = Helpers.randomRange(0, 2),
                napalm = Helpers.randomRange(0, 2),
                ball = Helpers.randomRange(0, 2),
                laser = Helpers.randomRange(0, 2),
                cutter = Helpers.randomRange(0, 2),
                flamethrower = Helpers.randomRange(0, 2),
            };
        }

        public void validate()
        {
            if (!inRange(cannon, 0, 2)) cannon = 0;
            if (!inRange(vulcan, 0, 2)) vulcan = 0;
            if (!inRange(missile, 0, 2)) missile = 0;
            if (!inRange(rocketPunch, 0, 2)) rocketPunch = 0;
            if (!inRange(napalm, -1, 3)) napalm = 0;
            if (!inRange(ball, -1, 3)) ball = 0;
            if (!inRange(laser, 0, 2)) laser = 0;
            if (cutter < -1 || cutter > 2) cutter = 0;
            if (!inRange(flamethrower, -1, 3)) flamethrower = 0;
            if (hyperMode < 0 || hyperMode > 2) hyperMode = 0;

            //if (cannon == -1 && vulcan == -1) cannon = 0;

            if (getTotalWeight() > maxWeight)
            {
                cannon = 0;
                vulcan = 0;
                missile = 0;
                rocketPunch = 0;
                napalm = 0;
                ball = 0;
                laser = -1;
                cutter = -1;
                flamethrower = -1;
            }
        }

        public int getTotalWeight()
        {
            int totalWeight =
                SelectVileWeaponMenu.vileWeaponCategories[0].Item2.FirstOrDefault(w => w.type == cannon).vileWeight +
                SelectVileWeaponMenu.vileWeaponCategories[1].Item2.FirstOrDefault(w => w.type == vulcan).vileWeight +
                SelectVileWeaponMenu.vileWeaponCategories[2].Item2.FirstOrDefault(w => w.type == missile).vileWeight +
                SelectVileWeaponMenu.vileWeaponCategories[3].Item2.FirstOrDefault(w => w.type == rocketPunch).vileWeight +
                SelectVileWeaponMenu.vileWeaponCategories[4].Item2.FirstOrDefault(w => w.type == napalm).vileWeight +
                SelectVileWeaponMenu.vileWeaponCategories[5].Item2.FirstOrDefault(w => w.type == ball).vileWeight +
                SelectVileWeaponMenu.vileWeaponCategories[6].Item2.FirstOrDefault(w => w.type == cutter).vileWeight +
                SelectVileWeaponMenu.vileWeaponCategories[7].Item2.FirstOrDefault(w => w.type == flamethrower).vileWeight +
                SelectVileWeaponMenu.vileWeaponCategories[8].Item2.FirstOrDefault(w => w.type == laser).vileWeight;
            return totalWeight;
        }

        private bool inRange(int weaponNum, int min = -1, int max = 2)
        {
            return weaponNum >= min && weaponNum <= max;
        }
    }

    [ProtoContract]
    public class AxlLoadout
    {
        [ProtoMember(1)] public int weapon2;  //0 indexed
        [ProtoMember(2)] public int weapon3;
        [ProtoMember(3)] public int hyperMode;

        [ProtoMember(4)] public int blastLauncherAlt;
        [ProtoMember(5)] public int rayGunAlt;
        [ProtoMember(6)] public int blackArrowAlt;
        [ProtoMember(7)] public int spiralMagnumAlt;
        [ProtoMember(8)] public int boundBlasterAlt;
        [ProtoMember(9)] public int plasmaGunAlt;
        [ProtoMember(10)] public int iceGattlingAlt;
        [ProtoMember(11)] public int flameBurnerAlt;

        public List<int> getAxlWeaponFIs()
        {
            return new List<int>() { 0, weapon2, weapon3 };
        }

        public List<int> getAxlWeaponIndices()
        {
            return new List<int>() { Weapon.fiToAxlWep(0).index, Weapon.fiToAxlWep(weapon2).index, Weapon.fiToAxlWep(weapon3).index };
        }

        public List<Weapon> getWeaponsFromLoadout()
        {
            var indices = new List<byte>();
            indices.Add((byte)Weapon.fiToAxlWep(weapon2).index);
            indices.Add((byte)Weapon.fiToAxlWep(weapon3).index);
            return indices.Select(index =>
            {
                return Weapon.getAllAxlWeapons(this).Find(w => w.index == index).clone();
            }).ToList();
        }

        [JsonIgnore]
        public int[] altFireArray => new int[]
        {
            0,
            rayGunAlt,
            blastLauncherAlt,
            blackArrowAlt,
            spiralMagnumAlt,
            boundBlasterAlt,
            plasmaGunAlt,
            iceGattlingAlt,
            flameBurnerAlt,
        };

        public void setAltFireArray(List<int> altFireArray)
        {
            rayGunAlt = altFireArray[1];
            blastLauncherAlt = altFireArray[2];
            blackArrowAlt = altFireArray[3];
            spiralMagnumAlt = altFireArray[4];
            boundBlasterAlt = altFireArray[5];
            plasmaGunAlt = altFireArray[6];
            iceGattlingAlt = altFireArray[7];
            flameBurnerAlt = altFireArray[8];
        }

        public void validate()
        {
            if (weapon2 < 1 || weapon2 > 8) weapon2 = 1;
            if (weapon3 < 1 || weapon3 > 8) weapon3 = 2;

            if (weapon2 == 0 || weapon3 == 0 || weapon2 == weapon3)
            {
                weapon2 = 1;
                weapon3 = 2;
            }

            if (!inRange(rayGunAlt)) rayGunAlt = 0;
            if (!inRange(blastLauncherAlt)) blastLauncherAlt = 0;
            if (!inRange(blackArrowAlt)) blackArrowAlt = 0;
            if (!inRange(spiralMagnumAlt)) spiralMagnumAlt = 0;
            if (!inRange(boundBlasterAlt)) boundBlasterAlt = 0;
            if (!inRange(plasmaGunAlt)) plasmaGunAlt = 0;
            if (!inRange(iceGattlingAlt)) iceGattlingAlt = 0;
            if (!inRange(flameBurnerAlt)) flameBurnerAlt = 0;
            if (!inRange(hyperMode)) hyperMode = 0;
        }

        private bool inRange(int altFire)
        {
            return altFire >= 0 && altFire <= 1;
        }

        public static AxlLoadout createRandom()
        {
            var randomAxlWeapons = Weapon.getRandomAxlWeapons();
            return new AxlLoadout()
            {
                weapon2 = randomAxlWeapons[1],
                weapon3 = randomAxlWeapons[2],
                blastLauncherAlt = Helpers.randomRange(0, 1),
                rayGunAlt = Helpers.randomRange(0, 1),
                blackArrowAlt = Helpers.randomRange(0, 1),
                spiralMagnumAlt = Helpers.randomRange(0, 1),
                boundBlasterAlt = Helpers.randomRange(0, 1),
                plasmaGunAlt = Helpers.randomRange(0, 1),
                iceGattlingAlt = Helpers.randomRange(0, 1),
                flameBurnerAlt = Helpers.randomRange(0, 1),
            };
        }
    }

    [ProtoContract]
    public class SigmaLoadout
    {
        [ProtoMember(1)] public int maverick1;    //0 indexed
        [ProtoMember(2)] public int maverick2;
        [ProtoMember(4)] public int sigmaForm;
        [ProtoMember(5)] public int commandMode;

        private int startMaverick { get { return (int)WeaponIds.ChillPenguin; } }
        private int endMaverick { get { return 26; } }

        public List<int> getWeaponIndices()
        {
            return new List<int>() { (int)WeaponIds.Sigma, maverick1 + startMaverick, maverick2 + startMaverick };
        }

        public void validate()
        {
            if (maverick1 < 0 || maverick1 > endMaverick) maverick1 = 0;
            if (maverick2 < 0 || maverick2 > endMaverick) maverick2 = 0;

            if (maverick1 == maverick2)
            {
                maverick1 = 0;
                maverick2 = 1;
            }

            commandMode = Helpers.clamp(commandMode, 0, 3);
            sigmaForm = Helpers.clamp(sigmaForm, 0, 2);
        }

        public List<Weapon> getWeaponsFromLoadout(Player player, int sigmaWeaponSlot)
        {
            sigmaWeaponSlot = Helpers.clamp(sigmaWeaponSlot, 0, 2);
            var indices = new List<byte>();
            indices.Add((byte)(maverick1 + startMaverick));
            indices.Add((byte)(maverick2 + startMaverick));
            indices.Insert(sigmaWeaponSlot, (byte)WeaponIds.Sigma);

            return indices.Select(index =>
            {
                return Weapon.getAllSigmaWeapons(player).Find(w => w.index == index).clone();
            }).ToList();
        }

        public static SigmaLoadout createRandom()
        {
            /*
            List<int> weaponPool = new List<int>();
            for (int i = (int)WeaponIds.ChillPenguin; i <= (int)WeaponIds.DrDoppler; i++)
            {
                weaponPool.Add(i);
            }

            var randPool = Helpers.getRandomSubarray(weaponPool, 3);
            return new SigmaLoadout()
            {
                maverick1 = randPool[0],
                maverick2 = randPool[1],
                maverick3 = randPool[2],
            };
            */
            return new SigmaLoadout()
            {
                maverick1 = 0,
                maverick2 = 1
            };
        }
    }

    [ProtoContract]
    public class LoadoutData
    {
        [ProtoMember(1)] public int playerId;
        [ProtoMember(2)] public XLoadout xLoadout = new XLoadout();
        [ProtoMember(3)] public ZeroLoadout zeroLoadout = new ZeroLoadout();
        [ProtoMember(4)] public VileLoadout vileLoadout = new VileLoadout();
        [ProtoMember(5)] public AxlLoadout axlLoadout = new AxlLoadout();
        [ProtoMember(6)] public SigmaLoadout sigmaLoadout = new SigmaLoadout();

        public static LoadoutData createRandom(int playerId)
        {
            return new LoadoutData()
            {
                playerId = playerId,
                xLoadout = XLoadout.createRandom(),
                zeroLoadout = ZeroLoadout.createRandom(),
                vileLoadout = VileLoadout.createRandom(),
                axlLoadout = AxlLoadout.createRandom(),
                sigmaLoadout = SigmaLoadout.createRandom(),
            };
        }

        public static LoadoutData createFromOptions(int playerId)
        {
            return new LoadoutData()
            {
                playerId = playerId,
                xLoadout = Helpers.cloneProtobuf(Options.main.xLoadout),
                zeroLoadout = Helpers.cloneProtobuf(Options.main.zeroLoadout),
                vileLoadout = Helpers.cloneProtobuf(Options.main.vileLoadout),
                axlLoadout = Helpers.cloneProtobuf(Options.main.axlLoadout),
                sigmaLoadout = Helpers.cloneProtobuf(Options.main.sigmaLoadout),
            };
        }
    }
}
