using Lidgren.Network;
using System;

namespace MMXOnline
{
    public enum NetActorCreateId
    {
        Default,
        RideArmor,
        RaySplasherTurret,
        ChillPenguin,
        SparkMandrill,
        ArmoredArmadillo,
        LaunchOctopus,
        BoomerKuwanger,
        StingChameleon,
        StormEagle,
        FlameMammoth,
        Velguarder,
        WolfSigmaHead,
        WolfSigmaHand,
        LargeHealth,
        SmallHealth,
        LargeAmmo,
        SmallAmmo,
        MechaniloidTank,
        MechaniloidFish,
        MechaniloidHopper,
        WireSponge,
        WheelGator,
        BubbleCrab,
        FlameStag,
        MorphMoth,
        MorphMothCocoon,
        MagnaCentipede,
        CrystalSnail,
        OverdriveOstrich,
        FakeZero,
        CrystalHunterCharged,
        CrystalSnailShell,
        BlizzardBuffalo,
        ToxicSeahorse,
        TunnelRhino,
        VoltCatfish,
        CrushCrawfish,
        NeonTiger,
        GravityBeetle,
        BlastHornet,
        DrDoppler,
        RideChaser,
    }

    public class RPCCreateActor : RPC
    {
        public RPCCreateActor()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            int createId = (int)arguments[0];
            float xPos = BitConverter.ToSingle(new byte[] { arguments[1], arguments[2], arguments[3], arguments[4] }, 0);
            float yPos = BitConverter.ToSingle(new byte[] { arguments[5], arguments[6], arguments[7], arguments[8] }, 0);
            var playerId = arguments[9];
            var netProjByte = BitConverter.ToUInt16(new byte[] { arguments[10], arguments[11] }, 0);
            int xDir = (int)arguments[12] - 128;

            var player = Global.level.getPlayerById(playerId);
            if (player == null) return;

            Actor actor = Global.level.getActorByNetId(netProjByte);
            if (actor != null && (int)actor.netActorCreateId == createId) return;
            if (Global.level.recentlyDestroyedNetActors.ContainsKey(netProjByte)) return;
            Point pos = new Point(xPos, yPos);

            if (createId == (int)NetActorCreateId.RaySplasherTurret)
            {
                new RaySplasherTurret(pos, player, 1, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.RideArmor)
            {
                new RideArmor(player, pos, 0, 0, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.ChillPenguin)
            {
                new ChillPenguin(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.SparkMandrill)
            {
                new SparkMandrill(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.ArmoredArmadillo)
            {
                new ArmoredArmadillo(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.LaunchOctopus)
            {
                new LaunchOctopus(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.BoomerKuwanger)
            {
                new BoomerKuwanger(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.StingChameleon)
            {
                new StingChameleon(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.StormEagle)
            {
                new StormEagle(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.FlameMammoth)
            {
                new FlameMammoth(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.Velguarder)
            {
                new Velguarder(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.WolfSigmaHead)
            {
                new WolfSigmaHead(pos, player, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.WolfSigmaHand)
            {
                new WolfSigmaHand(pos, player, false, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.LargeHealth)
            {
                new LargeHealthPickup(player, pos, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.SmallHealth)
            {
                new SmallHealthPickup(player, pos, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.LargeAmmo)
            {
                new LargeAmmoPickup(player, pos, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.SmallAmmo)
            {
                new SmallAmmoPickup(player, pos, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.WireSponge)
            {
                new WireSponge(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.WheelGator)
            {
                new WheelGator(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.BubbleCrab)
            {
                new BubbleCrab(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.FlameStag)
            {
                new FlameStag(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.MorphMoth)
            {
                new MorphMoth(player, pos, pos, xDir, netProjByte, false, false);
            }
            else if (createId == (int)NetActorCreateId.MorphMothCocoon)
            {
                new MorphMothCocoon(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.MagnaCentipede)
            {
                new MagnaCentipede(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.CrystalSnail)
            {
                new CrystalSnail(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.OverdriveOstrich)
            {
                new OverdriveOstrich(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.FakeZero)
            {
                new FakeZero(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.CrystalSnailShell)
            {
                new CrystalSnailShell(pos, xDir, null, player, netProjByte, false, false);
            }
            else if (createId == (int)NetActorCreateId.CrystalHunterCharged)
            {
                new CrystalHunterCharged(pos, player, netProjByte, false, 2, false);
            }
            else if (createId == (int)NetActorCreateId.MechaniloidTank)
            {
                new Mechaniloid(pos, player, xDir, new MechaniloidWeapon(player, MechaniloidType.Tank), MechaniloidType.Tank, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.MechaniloidHopper)
            {
                new Mechaniloid(pos, player, xDir, new MechaniloidWeapon(player, MechaniloidType.Hopper), MechaniloidType.Hopper, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.MechaniloidFish)
            {
                new Mechaniloid(pos, player, xDir, new MechaniloidWeapon(player, MechaniloidType.Fish), MechaniloidType.Fish, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.BlizzardBuffalo)
            {
                new BlizzardBuffalo(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.ToxicSeahorse)
            {
                new ToxicSeahorse(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.TunnelRhino)
            {
                new TunnelRhino(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.VoltCatfish)
            {
                new VoltCatfish(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.CrushCrawfish)
            {
                new CrushCrawfish(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.NeonTiger)
            {
                new NeonTiger(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.GravityBeetle)
            {
                new GravityBeetle(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.BlastHornet)
            {
                new BlastHornet(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.DrDoppler)
            {
                new DrDoppler(player, pos, pos, xDir, netProjByte, false);
            }
            else if (createId == (int)NetActorCreateId.RideChaser)
            {
                new RideChaser(player, pos, 0, netProjByte, false);
            }
        }
    }

}
