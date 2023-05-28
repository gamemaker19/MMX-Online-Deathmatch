using Lidgren.Network;
using System;

namespace MMXOnline
{
    public class RPCCreateProj : RPC
    {
        public RPCCreateProj()
        {
            netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
        }

        public override void invoke(params byte[] arguments)
        {
            ushort projId = BitConverter.ToUInt16(new byte[] { arguments[0], arguments[1] }, 0);
            float xPos = BitConverter.ToSingle(new byte[] { arguments[2], arguments[3], arguments[4], arguments[5] }, 0);
            float yPos = BitConverter.ToSingle(new byte[] { arguments[6], arguments[7], arguments[8], arguments[9] }, 0);
            var playerId = arguments[10];
            var netProjByte = BitConverter.ToUInt16(new byte[] { arguments[11], arguments[12] }, 0);
            int xDir = (int)arguments[13] - 128;
            float angle = arguments[13] * 2;    // Can alternatively use xDir for angle for angled projectiles
            float damage = BitConverter.ToSingle(new byte[] { arguments[14], arguments[15], arguments[16], arguments[17] }, 0);
            int flinch = arguments[18];

            int extraDataIndex = 19;
            Point bulletDir = Point.createFromAngle(angle);

            var player = Global.level.getPlayerById(playerId);
            if (player == null) return;

            Point pos = new Point(xPos, yPos);
            Projectile proj = null;
            if (projId == (int)ProjIds.ItemTracer)
            {
                proj = new ItemTracerProj(new ItemTracer(), pos, xDir, player, null, netProjByte);
            }
            else if (projId == (int)ProjIds.ZSaberProj)
            {
                proj = new ZSaberProj(new ZSaber(player), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.XSaberProj)
            {
                proj = new XSaberProj(new XSaber(player), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.Buster3)
            {
                proj = new Buster3Proj(new Buster(), pos, xDir, arguments[extraDataIndex], player, netProjByte);
            }
            else if (projId == (int)ProjIds.BusterX3Proj2)
            {
                proj = new BusterX3Proj2(new Buster(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.BusterX3Plasma)
            {
                proj = new BusterPlasmaProj(new Buster(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.BusterX3PlasmaHit)
            {
                proj = new BusterPlasmaHitProj(new Buster(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.ZBuster)
            {
                proj = new BusterProj(new ZeroBuster(), pos, xDir, 1, player, netProjByte);
            }
            else if (projId == (int)ProjIds.ZBuster2)
            {
                proj = new ZBuster2Proj(new ZeroBuster(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.ZBuster3)
            {
                proj = new ZBuster3Proj(new ZeroBuster(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.ZBuster4)
            {
                proj = new ZBuster4Proj(new ZeroBuster(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.ZBuster2b)
            {
                proj = new ZBuster2Proj(new ZeroBuster(), pos, xDir, 1, player, netProjByte);
            }
            else if (projId == (int)ProjIds.ZBuster3b)
            {
                proj = new ZBuster3Proj(new ZeroBuster(), pos, xDir, 1, player, netProjByte);
            }
            else if (projId == (int)ProjIds.ZBuster4b)
            {
                proj = new ZBuster4Proj(new ZeroBuster(), pos, xDir, 1, player, netProjByte);
            }
            else if (projId == (int)ProjIds.Sting || projId == (int)ProjIds.StingDiag)
            {
                proj = new StingProj(new Sting(), pos, xDir, player, 1, netProjByte);
            }
            else if (projId == (int)ProjIds.FireWaveCharged)
            {
                proj = new FireWaveProjCharged(new FireWave(), pos, xDir, player, 0, netProjByte, 0);
            }
            else if (projId == (int)ProjIds.ElectricSpark)
            {
                proj = new ElectricSparkProj(new ElectricSpark(), pos, xDir, player, 1, netProjByte);
            }
            else if (projId == (int)ProjIds.ElectricSparkCharged)
            {
                proj = new ElectricSparkProjCharged(new ElectricSpark(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.ShotgunIce)
            {
                proj = new ShotgunIceProj(new ShotgunIce(), pos, xDir, player, 1, netProjByte);
            }
            else if (projId == (int)ProjIds.ShotgunIceCharged)
            {
                proj = new ShotgunIceProjCharged(new ShotgunIce(), pos, xDir, player, 1, false, netProjByte);
            }
            else if (projId == (int)ProjIds.ChillPIceBlow)
            {
                proj = new ShotgunIceProjCharged(new ShotgunIce(), pos, xDir, player, 1, true, netProjByte);
            }
            else if (projId == (int)ProjIds.Rakuhouha)
            {
                proj = new RakuhouhaProj(new RakuhouhaWeapon(player), pos, false, 0, 0, player, netProjByte, 0);
            }
            else if (projId == (int)ProjIds.CFlasher)
            {
                proj = new RakuhouhaProj(new CFlasher(player), pos, true, 0, 0, player, netProjByte, 0);
            }
            else if (projId == (int)ProjIds.Hadouken)
            {
                proj = new HadoukenProj(new HadoukenWeapon(player), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.StunShot)
            {
                proj = new StunShotProj(new VileMissile(VileMissileType.StunShot), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.MK2StunShot)
            {
                proj = new VileMK2StunShotProj(new VileMK2StunShot(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.VileBomb)
            {
                proj = new VileBombProj(new VileBall(VileBallType.AirBombs), pos, xDir, player, 0, netProjByte);
            }
            else if (projId == (int)ProjIds.VileBombSplit)
            {
                proj = new VileBombProj(new VileBall(VileBallType.AirBombs), pos, xDir, player, 1, netProjByte);
            }
            else if (projId == (int)ProjIds.MK2Cannon)
            {
                proj = new VileCannonProj(new VileCannon(VileCannonType.FrontRunner), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.LongshotGizmo)
            {
                proj = new VileCannonProj(new VileCannon(VileCannonType.LongshotGizmo), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.FatBoy)
            {
                proj = new VileCannonProj(new VileCannon(VileCannonType.FatBoy), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.MechMissile)
            {
                proj = new MechMissileProj(new MechMissileWeapon(player), pos, xDir, false, player, netProjByte);
            }
            else if (projId == (int)ProjIds.MechTorpedo)
            {
                proj = new TorpedoProj(new MechTorpedoWeapon(player), pos, xDir, player, 2, netProjByte);
            }
            else if (projId == (int)ProjIds.MechChain)
            {
                proj = new MechChainProj(new MechChainWeapon(player), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.MechBuster)
            {
                proj = new MechBusterProj(new MechBusterWeapon(player), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.MechBuster2)
            {
                proj = new MechBusterProj2(new MechBusterWeapon(player), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.GLauncherSplash)
            {
                proj = new GrenadeExplosionProj(new Weapon(), pos, xDir, player, 0, null, 0, netProjByte);
            }
            else if (projId == (int)ProjIds.ExplosionSplash)
            {
                proj = new GrenadeExplosionProjCharged(new Weapon(), pos, xDir, player, 0, null, 1, netProjByte);
            }
            else if (projId == (int)ProjIds.NapalmGrenade)
            {
                proj = new NapalmGrenadeProj(new Weapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.Napalm)
            {
                proj = new NapalmPartProj(new Weapon(), pos, xDir, player, netProjByte, arguments[extraDataIndex] != 0, 0);
            }
            else if (projId == (int)ProjIds.NapalmGrenade2)
            {
                proj = new MK2NapalmGrenadeProj(new Napalm(NapalmType.FireGrenade), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.Napalm2)
            {
                proj = new MK2NapalmProj(new Napalm(NapalmType.FireGrenade), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.Napalm2Wall)
            {
                proj = new MK2NapalmWallProj(new Napalm(NapalmType.FireGrenade), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.Napalm2Flame)
            {
                proj = new MK2NapalmFlame(new Napalm(NapalmType.FireGrenade), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.RocketPunch)
            {
                proj = new RocketPunchProj(new RocketPunch(RocketPunchType.GoGetterRight), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.SpoiledBrat)
            {
                proj = new RocketPunchProj(new RocketPunch(RocketPunchType.SpoiledBrat), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.InfinityGig)
            {
                proj = new RocketPunchProj(new RocketPunch(RocketPunchType.InfinityGig), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.Vulcan)
            {
                proj = new VulcanProj(new Vulcan(VulcanType.CherryBlast), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.DistanceNeedler)
            {
                proj = new VulcanProj(new Vulcan(VulcanType.DistanceNeedler), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.BuckshotDance)
            {
                proj = new VulcanProj(new Vulcan(VulcanType.BuckshotDance), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.SilkShotShrapnel)
            {
                proj = new SilkShotProjShrapnel(new SilkShot(), pos, xDir, player, 0, new Point(), netProjByte);
            }
            else if (projId == (int)ProjIds.SpinWheelCharged)
            {
                proj = new SpinWheelProjCharged(new SpinWheel(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.SonicSlicer)
            {
                proj = new SonicSlicerProj(new SonicSlicer(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.StrikeChain)
            {
                proj = new StrikeChainProj(new StrikeChain(), pos, xDir, arguments[extraDataIndex], arguments[extraDataIndex + 1] - 128, player, netProjByte);
            }
            else if (projId == (int)ProjIds.SpeedBurnerTrail)
            {
                proj = new SpeedBurnerProjGround(new SpeedBurner(null), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.GigaCrush)
            {
                proj = new GigaCrushProj(new GigaCrush(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.Rekkoha)
            {
                proj = new RekkohaProj(new RekkohaWeapon(player), pos, player, netProjByte);
            }
            else if (projId == (int)ProjIds.AcidBurstSmall)
            {
                proj = new AcidBurstProjSmall(new AcidBurst(), pos, xDir, new Point(), (ProjIds)projId, player, netProjByte);
            }
            else if (projId == (int)ProjIds.ParasiticBombCharged)
            {
                proj = new ParasiticBombProjCharged(new ParasiticBomb(), pos, xDir, player, netProjByte, null);
            }
            else if (projId == (int)ProjIds.TriadThunderBeam)
            {
                proj = new TriadThunderBeamPiece(new TriadThunder(), pos, xDir, 1, player, 0, netProjByte);
            }
            else if (projId == (int)ProjIds.TriadThunderCharged)
            {
                proj = new TriadThunderProjCharged(new TriadThunder(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.SparkMSpark)
            {
                proj = new TriadThunderProjCharged(new TriadThunder(), pos, xDir, 1, player, netProjByte);
            }
            else if (projId == (int)ProjIds.TriadThunderQuake)
            {
                proj = new TriadThunderQuake(new TriadThunder(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.RaySplasher || projId == (int)ProjIds.RaySplasherChargedProj)
            {
                proj = new RaySplasherProj(new RaySplasher(), pos, xDir, 0, 0, false, player, netProjByte);
            }
            else if (projId == (int)ProjIds.GravityWellCharged)
            {
                proj = new GravityWellProjCharged(new GravityWell(), pos, xDir, 1, player, netProjByte);
            }
            else if (projId == (int)ProjIds.FrostShieldAir)
            {
                proj = new FrostShieldProjAir(new FrostShield(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.FrostShieldGround)
            {
                proj = new FrostShieldProjGround(new FrostShield(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.FrostShieldChargedGrounded)
            {
                proj = new FrostShieldProjChargedGround(new FrostShield(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.FrostShieldChargedPlatform)
            {
                proj = new FrostShieldProjPlatform(new FrostShield(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.TunnelFang || projId == (int)ProjIds.TunnelFang2)
            {
                proj = new TunnelFangProj(new TunnelFang(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.SplashLaser)
            {
                proj = new SplashLaserProj(new RayGun(0), pos, player, bulletDir, netProjByte);
            }
            else if (projId == (int)ProjIds.BlackArrow)
            {
                proj = new BlackArrowProj(new BlackArrow(0), pos, player, bulletDir, 0, netProjByte);
            }
            else if (projId == (int)ProjIds.WindCutter)
            {
                proj = new WindCutterProj(new BlackArrow(0), pos, player, bulletDir, netProjByte);
            }
            else if (projId == (int)ProjIds.SniperMissile)
            {
                proj = new SniperMissileProj(new SpiralMagnum(0), pos, player, bulletDir, netProjByte);
            }
            else if (projId == (int)ProjIds.SniperMissileBlast)
            {
                proj = new SniperMissileExplosionProj(new SpiralMagnum(0), pos, xDir, 1, player, netProjByte);
            }
            else if (projId == (int)ProjIds.BoundBlaster)
            {
                proj = new BoundBlasterProj(new BoundBlaster(0), pos, xDir, player, bulletDir, netProjByte);
            }
            else if (projId == (int)ProjIds.BoundBlaster2)
            {
                proj = new BoundBlasterAltProj(new BoundBlaster(0), pos, xDir, player, bulletDir, netProjByte);
            }
            else if (projId == (int)ProjIds.MovingWheel)
            {
                proj = new MovingWheelProj(new BoundBlaster(0), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.PlasmaGun)
            {
                proj = new PlasmaGunProj(new PlasmaGun(0), pos, xDir, player, bulletDir, netProjByte);
            }
            else if (projId == (int)ProjIds.PlasmaGun2 || projId == (int)ProjIds.PlasmaGun2Hyper)
            {
                proj = new PlasmaGunAltProj(new PlasmaGun(0), pos, pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.VoltTornado || projId == (int)ProjIds.VoltTornadoHyper)
            {
                proj = new VoltTornadoProj(new PlasmaGun(0), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.GaeaShield)
            {
                proj = new GaeaShieldProj(new IceGattling(0), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.FlameBurner || projId == (int)ProjIds.FlameBurnerHyper)
            {
                proj = new FlameBurnerProj(new FlameBurner(0), pos, xDir, player, bulletDir, netProjByte);
            }
            else if (projId == (int)ProjIds.FlameBurner2)
            {
                proj = new FlameBurnerAltProj(new FlameBurner(0), pos, xDir, player, bulletDir, netProjByte);
            }
            else if (projId == (int)ProjIds.CircleBlaze)
            {
                proj = new CircleBlazeProj(new FlameBurner(0), pos, xDir, player, bulletDir, netProjByte);
            }
            else if (projId == (int)ProjIds.CircleBlazeExplosion)
            {
                proj = new CircleBlazeExplosionProj(new FlameBurner(0), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.QuakeBlazer)
            {
                proj = new QuakeBlazerExplosionProj(new QuakeBlazerWeapon(null), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.QuakeBlazerFlame)
            {
                proj = new QuakeBlazerFlamePart(new QuakeBlazerWeapon(null), pos, xDir, new Point(), player, netProjByte);
            }
            else if (projId == (int)ProjIds.MechFrogStompShockwave)
            {
                proj = new MechFrogStompShockwave(new MechFrogStompWeapon(null), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.NapalmGrenadeSplashHit)
            {
                proj = new SplashHitGrenadeProj(new Napalm(NapalmType.SplashHit), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.NapalmSplashHit)
            {
                proj = new SplashHitProj(new Napalm(NapalmType.SplashHit), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.ShinMessenkou)
            {
                proj = new ShinMessenkouProj(new ShinMessenkou(null), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.Shingetsurin)
            {
                proj = new ShingetsurinProj(new Shingetsurin(null), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.VileMissile)
            {
                proj = new VileMissileProj(new VileMissile(VileMissileType.HumerusCrush), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.PopcornDemon)
            {
                proj = new VileMissileProj(new VileMissile(VileMissileType.PopcornDemon), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.PopcornDemonSplit)
            {
                proj = new VileMissileProj(new VileMissile(VileMissileType.PopcornDemon), pos, xDir, 1, player, netProjByte, vel: new Point());
            }
            else if (projId == (int)ProjIds.Gemnu)
            {
                proj = new GenmuProj(new Genmu(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.PeaceOutRoller)
            {
                proj = new PeaceOutRollerProj(new VileBall(VileBallType.PeaceOutRoller), pos, xDir, player, 0, netProjByte);
            }
            else if (projId == (int)ProjIds.NecroBurst)
            {
                proj = new NecroBurstProj(new VileLaser(VileLaserType.NecroBurst), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.NecroBurstShrapnel)
            {
                ushort spriteIndex = BitConverter.ToUInt16(new byte[] { arguments[extraDataIndex], arguments[extraDataIndex + 1] }, 0);
                string spriteName = Global.spriteNames[spriteIndex];
                byte hasRaColorShaderByte = arguments[extraDataIndex + 2];
                proj = new RAShrapnelProj(new VileLaser(VileLaserType.NecroBurst), pos, spriteName, xDir, hasRaColorShaderByte == 1 ? true : false, player, netProjByte);
            }
            else if (projId == (int)ProjIds.ChillPIceShot)
            {
                proj = new ChillPIceProj(new ChillPIceShotWeapon(), pos, xDir, player, 0, netProjByte);
            }
            else if (projId == (int)ProjIds.ChillPIcePenguin)
            {
                proj = new ChillPIceStatueProj(new ChillPIceStatueWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.ChillPBlizzard)
            {
                proj = new ChillPBlizzardProj(new ChillPBlizzardWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.ArmoredAProj)
            {
                proj = new ArmoredAProj(new ArmoredAProjWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.ArmoredAChargeRelease)
            {
                proj = new ArmoredAChargeReleaseProj(new ArmoredAChargeReleaseWeapon(), pos, xDir, new Point(), 6, player, netProjByte);
            }
            else if (projId == (int)ProjIds.LaunchOMissle)
            {
                proj = new LaunchOMissile(new LaunchOMissileWeapon(), pos, xDir, player, new Point(), netProjByte);
            }
            else if (projId == (int)ProjIds.LaunchOWhirlpool)
            {
                proj = new LaunchOWhirlpoolProj(new LaunchOWhirlpoolWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.LaunchOTorpedo)
            {
                proj = new TorpedoProj(new LaunchOHomingTorpedoWeapon(), pos, xDir, player, 3, netProjByte);
            }
            else if (projId == (int)ProjIds.BoomerKBoomerang)
            {
                proj = new BoomerKBoomerangProj(new BoomerKBoomerangWeapon(), pos, xDir, null, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.StingCSting)
            {
                proj = new StingCStingProj(new StingCStingWeapon(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.StingCSpike)
            {
                proj = new StingCSpikeProj(new StingCSpikeWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.StormEEgg)
            {
                proj = new StormEEggProj(new StormEEggWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.StormEGust)
            {
                proj = new StormEGustProj(new StormEGustWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.StormEBird)
            {
                proj = new StormEBirdProj(new StormEBirdWeapon(), pos, xDir, new Point(), new Point(), player, netProjByte);
            }
            else if (projId == (int)ProjIds.StormETornado)
            {
                proj = new TornadoProj(new StormETornadoWeapon(), pos, xDir, true, player, netProjByte);
            }
            else if (projId == (int)ProjIds.FlameMFireball)
            {
                proj = new FlameMFireballProj(new FlameMFireballWeapon(), pos, xDir, false, player, netProjByte);
            }
            else if (projId == (int)ProjIds.FlameMOil)
            {
                proj = new FlameMOilProj(new FlameMOilWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.FlameMOilSpill)
            {
                proj = new FlameMOilSpillProj(new FlameMOilWeapon(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.FlameMOilFire)
            {
                proj = new FlameMBigFireProj(new FlameMOilFireWeapon(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.FlameMStompShockwave)
            {
                proj = new FlameMStompShockwave(new FlameMStompWeapon(player), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.VelGFire)
            {
                proj = new VelGFireProj(new VelGFireWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.VelGIce)
            {
                proj = new VelGIceProj(new VelGIceWeapon(), pos, xDir, new Point(), player, netProjByte);
            }
            else if (projId == (int)ProjIds.SigmaSlash)
            {
                proj = new SigmaSlashProj(new SigmaSlashWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.SigmaBall)
            {
                proj = new SigmaBallProj(new SigmaBallWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.SigmaHandElecBeam)
            {
                proj = new WolfSigmaBeam(new WolfSigmaBeamWeapon(), pos, xDir, 1, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.WSpongeLightning)
            {
                proj = new WolfSigmaBeam(WireSponge.getWeapon(), pos, xDir, 1, 1, player, netProjByte);
            }
            else if (projId == (int)ProjIds.SigmaWolfHeadBallProj)
            {
                proj = new WolfSigmaBall(new WolfSigmaHeadWeapon(), pos, new Point(), player, netProjByte);
            }
            else if (projId == (int)ProjIds.SigmaWolfHeadFlameProj)
            {
                proj = new WolfSigmaFlame(new WolfSigmaHeadWeapon(), pos, new Point(), player, netProjByte);
            }
            else if (projId == (int)ProjIds.FSplasher)
            {
                proj = new FSplasherProj(new FSplasherWeapon(player), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.HyorogaProj)
            {
                proj = new HyorogaProj(new HyorogaWeapon(player), pos, new Point(0, 0), player, netProjByte);
            }
            else if (projId == (int)ProjIds.SuiretsusanProj)
            {
                proj = new SuiretsusenProj(new SuiretsusenWeapon(player), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.TBreakerProj)
            {
                proj = new TBreakerProj(new TBreakerWeapon(player), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.QuickHomesick)
            {
                proj = new VileCutterProj(new VileCutter(VileCutterType.QuickHomesick), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.ParasiteSword)
            {
                proj = new VileCutterProj(new VileCutter(VileCutterType.ParasiteSword), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.MaroonedTomahawk)
            {
                proj = new VileCutterProj(new VileCutter(VileCutterType.MaroonedTomahawk), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.WildHorseKick)
            {
                proj = new FlamethrowerProj(new VileFlamethrower(VileFlamethrowerType.WildHorseKick), pos, xDir, false, player, netProjByte);
            }
            else if (projId == (int)ProjIds.SeaDragonRage)
            {
                proj = new FlamethrowerProj(new VileFlamethrower(VileFlamethrowerType.SeaDragonRage), pos, xDir, false, player, netProjByte);
            }
            else if (projId == (int)ProjIds.DragonsWrath)
            {
                proj = new FlamethrowerProj(new VileFlamethrower(VileFlamethrowerType.DragonsWrath), pos, xDir, false, player, netProjByte);
            }
            else if (projId == (int)ProjIds.RisingSpecter)
            {
                proj = new RisingSpecterProj(new VileLaser(VileLaserType.RisingSpecter), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.StraightNightmare)
            {
                proj = new StraightNightmareProj(new VileLaser(VileLaserType.StraightNightmare), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.MetteurCrash)
            {
                proj = new MettaurCrashProj(new AxlBullet(AxlBulletWeaponType.MetteurCrash), pos, player, Point.zero, netProjByte);
            }
            else if (projId == (int)ProjIds.BeastKiller)
            {
                proj = new BeastKillerProj(new AxlBullet(AxlBulletWeaponType.BeastKiller), pos, player, Point.zero, netProjByte);
            }
            else if (projId == (int)ProjIds.MachineBullets)
            {
                proj = new MachineBulletProj(new AxlBullet(AxlBulletWeaponType.MachineBullets), pos, player, Point.zero, netProjByte);
            }
            else if (projId == (int)ProjIds.RevolverBarrel)
            {
                proj = new RevolverBarrelProj(new AxlBullet(AxlBulletWeaponType.RevolverBarrel), pos, player, Point.zero, netProjByte);
            }
            else if (projId == (int)ProjIds.AncientGun)
            {
                proj = new AncientGunProj(new AxlBullet(AxlBulletWeaponType.AncientGun), pos, player, Point.zero, netProjByte);
            }
            else if (projId == (int)ProjIds.WSpongeChain)
            {
                proj = new WSpongeSideChainProj(WireSponge.getChainWeapon(player), pos, xDir, null, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.WSpongeUpChain)
            {
                proj = new WSpongeUpChainProj(WireSponge.getChainWeapon(player), pos, xDir, null, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.WSpongeChainSpin)
            {
                proj = new WSpongeChainSpinProj(WireSponge.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.WSpongeSeed)
            {
                proj = new WSpongeSeedProj(WireSponge.getWeapon(), pos, xDir, Point.zero, player, netProjByte);
            }
            else if (projId == (int)ProjIds.WSpongeSpike)
            {
                proj = new WSpongeSpike(WireSponge.getWeapon(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.WheelGSpinWheel)
            {
                proj = new WheelGSpinWheelProj(WheelGator.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.WheelGSpit)
            {
                proj = new WheelGSpitProj(WheelGator.getWeapon(), pos, xDir, Point.zero, player, netProjByte);
            }
            else if (projId == (int)ProjIds.BCrabBubbleSplash)
            {
                proj = new BCrabBubbleSplashProj(BubbleCrab.getWeapon(), pos, xDir, 0, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.BCrabBubbleShield)
            {
                proj = new BCrabShieldProj(BubbleCrab.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.BCrabCrabling)
            {
                proj = new BCrabSummonCrabProj(BubbleCrab.getWeapon(), pos, Point.zero, null, player, netProjByte);
            }
            else if (projId == (int)ProjIds.BCrabCrablingBubble)
            {
                proj = new BCrabSummonBubbleProj(BubbleCrab.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.FStagFireball)
            {
                proj = new FStagFireballProj(FlameStag.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.FStagDashTrail)
            {
                proj = new FStagTrailProj(FlameStag.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.FStagDashCharge)
            {
                proj = new FStagDashChargeProj(FlameStag.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.FStagDash)
            {
                proj = new FStagDashProj(FlameStag.getWeapon(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.MorphMCScrap)
            {
                proj = new MorphMCScrapProj(MorphMothCocoon.getWeapon(), pos, xDir, Point.zero, float.MaxValue, false, null, player, netProjByte);
            }
            else if (projId == (int)ProjIds.MorphMCThread)
            {
                proj = new MorphMCThreadProj(MorphMothCocoon.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.MorphMBeam)
            {
                proj = new MorphMBeamProj(MorphMoth.getWeapon(), pos, pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.MorphMPowder)
            {
                proj = new MorphMPowderProj(MorphMoth.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.MagnaCShuriken)
            {
                proj = new MagnaCShurikenProj(MagnaCentipede.getWeapon(), pos, xDir, Point.zero, player, netProjByte);
            }
            else if (projId == (int)ProjIds.MagnaCMagnetMine)
            {
                proj = new MagnaCMagnetMineProj(MagnaCentipede.getWeapon(), pos, Point.zero, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.MagnaCMagnetPull)
            {
                proj = new MagnaCMagnetPullProj(MagnaCentipede.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.CSnailCrystalHunter)
            {
                proj = new CSnailCrystalHunterProj(CrystalSnail.getWeapon(), pos, xDir, Point.zero, player, netProjByte);
            }
            else if (projId == (int)ProjIds.OverdriveOSonicSlicer)
            {
                proj = new OverdriveOSonicSlicerProj(OverdriveOstrich.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.OverdriveOSonicSlicerUp)
            {
                proj = new OverdriveOSonicSlicerUpProj(OverdriveOstrich.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.FakeZeroBuster)
            {
                proj = new FakeZeroBusterProj(FakeZero.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.FakeZeroBuster2)
            {
                proj = new FakeZeroBuster2Proj(FakeZero.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.FakeZeroSwordBeam)
            {
                proj = new FakeZeroSwordBeamProj(FakeZero.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.FakeZeroMelee)
            {
                proj = new FakeZeroMeleeProj(FakeZero.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.FakeZeroGroundPunch)
            {
                proj = new FakeZeroRockProj(FakeZero.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.Sigma2Ball)
            {
                proj = new SigmaElectricBallProj(new SigmaElectricBallWeapon(), pos, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.Sigma2Ball2)
            {
                proj = new SigmaElectricBall2Proj(new SigmaElectricBallWeapon(), pos, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.Sigma2ViralProj)
            {
                proj = new ViralSigmaShootProj(null, pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.Sigma2ViralBeam)
            {
                proj = new ViralSigmaBeamProj(new ViralSigmaBeamWeapon(), pos, player, netProjByte);
            }
            else if (projId == (int)ProjIds.Sigma2BirdProj)
            {
                proj = new BirdMechaniloidProj(new MechaniloidWeapon(player, MechaniloidType.Bird), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.Sigma2TankProj)
            {
                proj = new TankMechaniloidProj(new MechaniloidWeapon(player, MechaniloidType.Tank), pos, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.Sigma3Shield)
            {
                proj = new SigmaShieldProj(player.sigmaShieldWeapon, pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.Sigma3Fire)
            {
                proj = new Sigma3FireProj(player.sigmaFireWeapon, pos, 0, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.Sigma3KaiserMine)
            {
                proj = new KaiserSigmaMineProj(new KaiserMineWeapon(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.Sigma3KaiserMissile)
            {
                proj = new KaiserSigmaMissileProj(new KaiserMissileWeapon(), pos, player, netProjByte);
            }
            else if (projId == (int)ProjIds.Sigma3KaiserBeam)
            {
                proj = new KaiserSigmaBeamProj(new KaiserBeamWeapon(), pos, xDir, false, player, netProjByte);
            }
            else if (projId == (int)ProjIds.TSeahorseAcid1)
            {
                proj = new TSeahorseAcidProj(ToxicSeahorse.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.TSeahorseAcid2)
            {
                proj = new TSeahorseAcid2Proj(ToxicSeahorse.getWeapon(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.TunnelRTornadoFang)
            {
                proj = new TunnelRTornadoFang(ToxicSeahorse.getWeapon(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.TunnelRTornadoFang2)
            {
                proj = new TunnelRTornadoFang(TunnelRhino.getWeapon(), pos, xDir, 1, player, netProjByte);
            }
            else if (projId == (int)ProjIds.TunnelRTornadoFangDiag)
            {
                proj = new TunnelRTornadoFangDiag(TunnelRhino.getWeapon(), pos, xDir * -1, player, netProjByte);
            }
            else if (projId == (int)ProjIds.VoltCBall)
            {
                proj = new TriadThunderProjCharged(new TriadThunder(), pos, xDir, 2, player, netProjByte);
            }
            else if (projId == (int)ProjIds.VoltCBarrier)
            {
                proj = new VoltCBarrierProj(VoltCatfish.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.VoltCCharge)
            {
                proj = new VoltCChargeProj(VoltCatfish.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.VoltCSparkle)
            {
                proj = new VoltCSparkleProj(VoltCatfish.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.VoltCSuck)
            {
                proj = new VoltCSuckProj(VoltCatfish.getWeapon(), pos, xDir, null, player, netProjByte);
            }
            else if (projId == (int)ProjIds.VoltCTriadThunder)
            {
                proj = new VoltCTriadThunderProj(VoltCatfish.getWeapon(), pos, xDir, 0, new Point(xDir, 0.5f), null, player, netProjByte);
            }
            else if (projId == (int)ProjIds.VoltCUpBeam)
            {
                proj = new VoltCUpBeamProj(VoltCatfish.getWeapon(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.VoltCUpBeam2)
            {
                proj = new VoltCUpBeamProj(VoltCatfish.getWeapon(), pos, xDir, 1, player, netProjByte);
            }
            else if (projId == (int)ProjIds.CrushCProj)
            {
                proj = new CrushCProj(CrushCrawfish.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.CrushCArmProj)
            {
                proj = new CrushCArmProj(CrushCrawfish.getWeapon(), pos, xDir, Point.zero, null, player, netProjByte);
            }
            else if (projId == (int)ProjIds.NeonTRaySplasher)
            {
                proj = new NeonTRaySplasherProj(NeonTiger.getWeapon(), pos, xDir, 0, false, player, netProjByte);
            }
            else if (projId == (int)ProjIds.GBeetleBall)
            {
                proj = new GBeetleBallProj(GravityBeetle.getWeapon(), pos, xDir, false, player, netProjByte);
            }
            else if (projId == (int)ProjIds.GBeetleGravityWell)
            {
                proj = new GBeetleGravityWellProj(GravityBeetle.getWeapon(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.DrDopplerBall)
            {
                proj = new DrDopplerBallProj(DrDoppler.getWeapon(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.DrDopplerBall2)
            {
                proj = new DrDopplerBallProj(DrDoppler.getWeapon(), pos, xDir, 1, player, netProjByte);
            }
            else if (projId == (int)ProjIds.RideChaserProj)
            {
                proj = new RCProj(RideChaser.getGunWeapon(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.Buster)
            {
                proj = new RCProj(RideChaser.getGunWeapon(), pos, xDir, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.UPParryMelee)
            {
                proj = new UPParryMeleeProj(new XUPParry(), pos, xDir, damage, player, netProjByte);
            }
            else if (projId == (int)ProjIds.UPParryProj)
            {
                float hitCooldown = BitConverter.ToUInt16(new byte[] { arguments[extraDataIndex], arguments[extraDataIndex + 1], arguments[extraDataIndex + 2], arguments[extraDataIndex + 3] }, 0);
                proj = new UPParryRangedProj(new XUPParry(), pos, xDir, "empty", damage, flinch, hitCooldown, player, netProjByte);
            }
            else if (projId == (int)ProjIds.BBuffaloCrash)
            {
                proj = new BBuffaloCrashProj(BlizzardBuffalo.getWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.BBuffaloIceProj)
            {
                proj = new BBuffaloIceProj(BlizzardBuffalo.getWeapon(), pos, xDir, Point.zero, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.BBuffaloIceProjGround)
            {
                proj = new BBuffaloIceProjGround(BlizzardBuffalo.getWeapon(), pos, 0, player, netProjByte);
            }
            else if (projId == (int)ProjIds.BBuffaloBeam)
            {
                ushort bbNetIdBytes = BitConverter.ToUInt16(new byte[] { arguments[extraDataIndex], arguments[extraDataIndex + 1] }, 0);
                var bb = Global.level.getActorByNetId(bbNetIdBytes) as BlizzardBuffalo;
                proj = new BBuffaloBeamProj(BlizzardBuffalo.getWeapon(), pos, xDir, bb, player, netProjByte);
            }
            else if (projId == (int)ProjIds.BHornetBee)
            {
                proj = new BHornetBeeProj(BlastHornet.getWeapon(), pos, xDir, Point.zero, player, netProjByte);
            }
            else if (projId == (int)ProjIds.BHornetHomingBee)
            {
                proj = new BHornetHomingBeeProj(BlastHornet.getWeapon(), pos, xDir, null, player, netProjByte);
            }
            else if (projId == (int)ProjIds.BHornetCursor)
            {
                proj = new BHornetCursorProj(BlastHornet.getWeapon(), pos, xDir, Point.zero, null, player, netProjByte);
            }
            else if (projId == (int)ProjIds.DarkHold)
            {
                proj = new DarkHoldProj(new DarkHoldWeapon(), pos, xDir, player, netProjByte);
            }
            else if (projId == (int)ProjIds.HexaInvolute)
            {
                proj = new HexaInvoluteProj(new HexaInvoluteWeapon(), pos, xDir, player, netProjByte);
            }

            /*
            // Template
            else if (projId == (int)ProjIds.PROJID)
            {
                proj = new PROJ(new WEP(), pos, xDir, player, netProjByte);
            }
            */

            if (proj.damager != null)
            {
                proj.damager.damage = damage;
                proj.damager.flinch = flinch;
            }
        }
    }
}
