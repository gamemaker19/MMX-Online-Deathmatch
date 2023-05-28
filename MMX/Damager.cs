using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MMXOnline
{
    public class Damager
    {
        public Player owner;
        public float damage;
        public float hitCooldown;
        public int flinch;  // number of frames to flinch
        public float knockback;

        public const float envKillDamage = 2000;
        public const float switchKillDamage = 1000;
        public const float ohkoDamage = 500;
        public const float headshotModifier = 2;

        public static readonly Dictionary<int, float> projectileFlinchCooldowns = new Dictionary<int, float>()
        {
            { (int)ProjIds.ElectricSpark, 1 },
            { (int)ProjIds.TriadThunder, 2.25f },
            { (int)ProjIds.TriadThunderBall, 2.25f },
            { (int)ProjIds.TriadThunderBeam, 2.25f },
            { (int)ProjIds.PlasmaGun2, 1 },
            { (int)ProjIds.VoltTornado, 1 },
            { (int)ProjIds.TornadoCharged, 1 },
            //{ (int)ProjIds.KKnuckle, 1 },
            { (int)ProjIds.KKnuckle2, 1 },
            { (int)ProjIds.KKnuckleSpinKick, 1 },
            { (int)ProjIds.KKnuckleAirKick, 1 },
            { (int)ProjIds.MechPunch, 1 },
            { (int)ProjIds.MechKangarooPunch, 1 },
            { (int)ProjIds.MechGoliathPunch, 1 },
            { (int)ProjIds.MechDevilBearPunch, 1 },
            { (int)ProjIds.MechStomp, 1 },
            { (int)ProjIds.MechChain, 1 },
            { (int)ProjIds.TunnelFangCharged, 1 },
            { (int)ProjIds.Headbutt, 1 },
            { (int)ProjIds.RocketPunch, 1 },
            { (int)ProjIds.InfinityGig, 1 },
            { (int)ProjIds.SpoiledBrat, 1 },
            { (int)ProjIds.SpinningBladeCharged, 1 },
            { (int)ProjIds.Shingetsurin, 1 },
            { (int)ProjIds.MagnetMineCharged, 1 },
            { (int)ProjIds.Sigma2ViralBeam, 1 },
            { (int)ProjIds.Sigma2HopperDrill, 0.9f },
            { (int)ProjIds.WSpongeChainSpin, 1 },
            { (int)ProjIds.MorphMCSpin, 1 },
            { (int)ProjIds.BCrabClaw, 1 },
            { (int)ProjIds.SpeedBurnerCharged, 0.5f },
            { (int)ProjIds.VelGMelee, 1f },
            { (int)ProjIds.OverdriveOMelee, 1f },
            { (int)ProjIds.WheelGSpinWheel, 1f },
            { (int)ProjIds.Sigma3KaiserStomp, 1f },
            { (int)ProjIds.Sigma3KaiserBeam, 1f },
            { (int)ProjIds.UPPunch, 1f },
            { (int)ProjIds.CopyShot, 1f },
            { (int)ProjIds.NeonTClawAir, 1f },
            { (int)ProjIds.NeonTClawDash, 1f },
            { (int)ProjIds.VoltCTriadThunder, 1f },
            { (int)ProjIds.Rekkoha, 1f },
            { (int)ProjIds.HexaInvolute, 1f },
            { (int)ProjIds.ZSaber3, 1f }
        };

        public Damager(Player owner, float damage, int flinch, float hitCooldown, float knockback = 0)
        {
            this.owner = owner;
            this.damage = damage;
            this.flinch = flinch;
            this.hitCooldown = hitCooldown;
            this.knockback = knockback;
        }

        // Normally, sendRpc would default to false, but literally over 20 places need it to true and one place needs it false so in this case, we invert the convention
        public bool applyDamage(IDamagable victim, bool weakness, Weapon weapon, Actor actor, int projId, float? overrideDamage = null, int? overrideFlinch = null, bool sendRpc = true)
        {
            if (weapon == null) return false;
            if (weapon is ItemTracer) return false;
            if (projId == (int)ProjIds.GravityWellCharged) return false;

            var newDamage = (overrideDamage != null ? (float)overrideDamage : damage);
            var newFlinch = (overrideFlinch != null ? (int)overrideFlinch : flinch);

            var chr = victim as Character;

            if (chr != null)
            {
                if (chr.isCCImmune())
                {
                    newFlinch = 0;
                    weakness = false;
                }

                if (chr.player.isAxl && newFlinch > 0)
                {
                    if (newFlinch < 4)
                    {
                        newFlinch = 4;
                    }
                    else if (newFlinch < 12)
                    {
                        newFlinch = 12;
                    }
                    else if (newFlinch < 26)
                    {
                        newFlinch = 26;
                    }
                    else
                    {
                        newFlinch = 36;
                    }
                }
            }

            return applyDamage(owner, newDamage, hitCooldown, newFlinch, victim as Actor, weakness, weapon.index, weapon.killFeedIndex, actor, projId, sendRpc);
        }

        public static bool applyDamage(Player owner, float damage, float hitCooldown, int flinch, Actor victim, bool weakness, int weaponIndex, int weaponKillFeedIndex, Actor damagingActor, int projId, bool sendRpc = true)
        {
            if (victim is Character chr && chr.isInvulnBS.getValue()) return false;
            if (projId == (int)ProjIds.TriadThunderQuake && victim.ownedByLocalPlayer && isVictimImmuneToQuake(victim)) return false;
            if (owner.character?.isDarkHoldBS.getValue() == true) return false;

            string key = projId.ToString() + "_" + owner.id.ToString();

            // Key adjustment overrides for more fine tuned balance cases
            if (projId == (int)ProjIds.Hyouretsuzan2)
            {
                key = ((int)ProjIds.Hyouretsuzan).ToString() + "_" + owner.id.ToString();
            }
            if (projId == (int)ProjIds.GLauncherSplash)
            {
                key += "_" + damagingActor?.netId?.ToString();
            }

            IDamagable damagable = victim as IDamagable;
            Character character = victim as Character;
            RideArmor rideArmor = victim as RideArmor;
            Maverick maverick = victim as Maverick;

            if (damagable == null) return false;
            if (!damagable.projectileCooldown.ContainsKey(key))
            {
                damagable.projectileCooldown[key] = 0;
            }

            if (damagable.projectileCooldown[key] != 0)
            {
                return false;
            }

            damagable.projectileCooldown[key] = hitCooldown;

            // Run the RPC on all clients first, before it can modify the parameters, so clients can act accordingly
            if (sendRpc && victim.netId != null && Global.serverClient?.isLagging() == false)
            {
                var damageBytes = BitConverter.GetBytes(damage);
                var hitCooldownBytes = BitConverter.GetBytes(hitCooldown);
                var victimNetIdBytes = BitConverter.GetBytes((ushort)victim.netId);
                var actorNetIdBytes = BitConverter.GetBytes(damagingActor?.netId ?? 0);
                var projIdBytes = BitConverter.GetBytes(projId);

                var byteParams = new List<byte>
                {
                    (byte)owner.id,
                    damageBytes[0],
                    damageBytes[1],
                    damageBytes[2],
                    damageBytes[3],
                    hitCooldownBytes[0],
                    hitCooldownBytes[1],
                    hitCooldownBytes[2],
                    hitCooldownBytes[3],
                    (byte)flinch,
                    victimNetIdBytes[0],
                    victimNetIdBytes[1],
                    weakness ? (byte)1 : (byte)0,
                    (byte)weaponIndex,
                    (byte)weaponKillFeedIndex,
                    actorNetIdBytes[0],
                    actorNetIdBytes[1],
                    projIdBytes[0],
                    projIdBytes[1],
                };

                RPC.applyDamage.sendRpc(byteParams.ToArray());
            }

            if (owner.isDisguisedAxl && owner.character != null)
            {
                owner.character.disguiseCoverBlown = true;
            }

            if (damagable.isInvincible(owner, projId) && damage > 0)
            {
                victim.playSound("ding");
                return true;
            }

            // Would only get reached due to lag. Otherwise, the owner that initiates the applyDamage call would have already considered it and avoided entering the method
            // This allows dodge abilities to "favor the defender"
            if (!damagable.canBeDamaged(owner.alliance, owner.id, projId))
            {
                return true;
            }

            if (damagable != null && damagable is not CrackedWall && owner != null && owner.isMainPlayer && !isDot(projId))
            {
                owner.delaySubtank();
            }

            if (damagable is CrackedWall cw)
            {
                float? overrideDamage = CrackedWall.canDamageCrackedWall(projId, cw);
                if (overrideDamage != null && overrideDamage == 0 && damage > 0)
                {
                    cw.playSound("ding");
                    return true;
                }
                damage = overrideDamage ?? damage;
            }

            if (damagable != null && owner != null)
            {
                DamagerMessage damagerMessage = null;

                var proj = damagingActor as Projectile;
                if (proj != null)
                {
                    damagerMessage = proj.onDamage(damagable, owner);
                    if (damagerMessage?.flinch != null) flinch = damagerMessage.flinch.Value;
                    if (damagerMessage?.damage != null) damage = damagerMessage.damage.Value;
                }

                if (projId == (int)ProjIds.CrystalHunter) damagerMessage = onCrystalDamage(damagable, owner, 2);
                else if (projId == (int)ProjIds.CSnailCrystalHunter) damagerMessage = onCrystalDamage(damagable, owner, 2);
                else if (projId == (int)ProjIds.AcidBurst) damagerMessage = onAcidDamage(damagable, owner, 2);
                else if (projId == (int)ProjIds.AcidBurstSmall) damagerMessage = onAcidDamage(damagable, owner, 1);
                else if (projId == (int)ProjIds.AcidBurstCharged) damagerMessage = onAcidDamage(damagable, owner, 3);
                else if (projId == (int)ProjIds.TSeahorseAcid1) damagerMessage = onAcidDamage(damagable, owner, 2);
                else if (projId == (int)ProjIds.TSeahorseAcid2) damagerMessage = onAcidDamage(damagable, owner, 2);
                //else if (projId == (int)ProjIds.TSeahorsePuddle) damagerMessage = onAcidDamage(damagable, owner, 1);
                //else if (projId == (int)ProjIds.TSeahorseEmerge) damagerMessage = onAcidDamage(damagable, owner, 2);
                else if (projId == (int)ProjIds.ParasiticBomb) damagerMessage = onParasiticBombDamage(damagable, owner);
                else if (projId == (int)ProjIds.StunShot || projId == (int)ProjIds.MK2StunShot || projId == (int)ProjIds.MorphMPowder) damagerMessage = onStunShotDamage(damagable, owner);

                if (damagerMessage?.flinch != null) flinch = damagerMessage.flinch.Value;
                if (damagerMessage?.damage != null) damage = damagerMessage.damage.Value;

                if (projId == (int)ProjIds.CrystalHunter && weakness)
                {
                    damage = 4;
                    weakness = false;
                    flinch = 0;
                    victim?.playSound("hurt");
                }
                if (projId == (int)ProjIds.StrikeChain && weakness)
                {
                    damage *= 2;
                    weakness = false;
                    flinch = 0;
                    victim?.playSound("hurt");
                }
                if (projId == (int)ProjIds.Tornado && weakness)
                {
                    damage = 1;
                    weakness = true;
                    flinch = Global.defFlinch;
                }
                if (projId == (int)ProjIds.AcidBurst && weakness)
                {
                    damage = 1;
                    weakness = true;
                    flinch = Global.defFlinch;
                }
                if (projId == (int)ProjIds.GravityWell && weakness)
                {
                    damage = 2;
                    weakness = true;
                    flinch = Global.defFlinch;
                }
                if (projId == (int)ProjIds.CSnailMelee && character != null && character.isCrystalized)
                {
                    damage *= 2;
                }
            }

            // Character section
            bool spiked = false;
            bool playHurtSound = false;
            if (character != null)
            {
                bool isStompWeapon = (weaponKillFeedIndex == 19 || weaponKillFeedIndex == 58 || weaponKillFeedIndex == 51 || weaponKillFeedIndex == 59 || weaponKillFeedIndex == 60) && projId != (int)ProjIds.MechFrogStompShockwave;
                if (projId == (int)ProjIds.FlameMStomp || projId == (int)ProjIds.TBreaker || projId == (int)ProjIds.SparkMStomp || projId == (int)ProjIds.WheelGStomp || projId == (int)ProjIds.GBeetleStomp || 
                    projId == (int)ProjIds.TunnelRStomp || projId == (int)ProjIds.Sigma3KaiserStomp || projId == (int)ProjIds.BBuffaloStomp)
                {
                    isStompWeapon = true;
                }

                // Ride armor stomp
                if (isStompWeapon)
                {
                    character.flattenedTime = 0.5f;
                }

                if (character.charState is SwordBlock)
                {
                    weakness = false;
                }

                if (character.isAlwaysHeadshot() && (projId == (int)ProjIds.RevolverBarrel || projId == (int)ProjIds.AncientGun))
                {
                    damage *= 1.5f;
                    playHurtSound = true;
                }

                if (character.ownedByLocalPlayer && character.charState.superArmor && projId != (int)ProjIds.PlasmaGun)
                {
                    flinch = 0;
                }

                #region effects

                // Burn effects. If adding one here add it to canDamageFrostShield() method too
                if (projId == (int)ProjIds.FireWave) character.addBurnTime(owner, new FireWave(), 0.5f);
                else if (projId == (int)ProjIds.FireWaveCharged) character.addBurnTime(owner, new FireWave(), 2f);
                else if (projId == (int)ProjIds.SpeedBurner) character.addBurnTime(owner, new SpeedBurner(null), 1);
                else if (projId == (int)ProjIds.SpeedBurnerCharged) { if (character != owner.character) character.addBurnTime(owner, new SpeedBurner(null), 1); }
                else if (projId == (int)ProjIds.Napalm2 || projId == (int)ProjIds.Napalm2Wall) character.addBurnTime(owner, new Napalm(NapalmType.FireGrenade), 1);
                else if (projId == (int)ProjIds.Napalm2Flame) character.addBurnTime(owner, new Napalm(NapalmType.FireGrenade), 0.5f);
                else if (projId == (int)ProjIds.Ryuenjin) character.addBurnTime(owner, new RyuenjinWeapon(owner), 2);
                else if (projId == (int)ProjIds.FlameBurner) character.addBurnTime(owner, new FlameBurner(0), 0.5f);
                else if (projId == (int)ProjIds.FlameBurnerHyper) character.addBurnTime(owner, new FlameBurner(0), 1);
                else if (projId == (int)ProjIds.CircleBlazeExplosion) character.addBurnTime(owner, new FlameBurner(0), 2);
                else if (projId == (int)ProjIds.QuakeBlazer) character.addBurnTime(owner, new QuakeBlazerWeapon(null), 0.5f);
                else if (projId == (int)ProjIds.QuakeBlazerFlame) character.addBurnTime(owner, new QuakeBlazerWeapon(null), 0.5f);
                else if (projId == (int)ProjIds.FlameMFireball) character.addBurnTime(owner, new FlameMFireballWeapon(), 1);
                else if (projId == (int)ProjIds.FlameMOilFire) character.addBurnTime(owner, new FlameMOilFireWeapon(), 8);
                else if (projId == (int)ProjIds.VelGFire) character.addBurnTime(owner, new VelGFireWeapon(), 0.5f);
                else if (projId == (int)ProjIds.SigmaWolfHeadFlameProj) character.addBurnTime(owner, new WolfSigmaHeadWeapon(), 3);
                else if (projId == (int)ProjIds.WildHorseKick) character.addBurnTime(owner, new VileFlamethrower(VileFlamethrowerType.WildHorseKick), 0.5f);
                else if (projId == (int)ProjIds.FStagFireball) character.addBurnTime(owner, FlameStag.getWeapon(), 1f);
                else if (projId == (int)ProjIds.FStagDash) character.addBurnTime(owner, FlameStag.getUppercutWeapon(null), 2f);
                else if (projId == (int)ProjIds.DrDopplerDash) character.addBurnTime(owner, new Weapon(WeaponIds.DrDopplerGeneric, 156), 1f);
                else if (projId == (int)ProjIds.Sigma3Fire) character.addBurnTime(owner, new Sigma3FireWeapon(), 0.5f);

                // Other effects
                if (projId == (int)ProjIds.IceGattling)
                {
                    character.addIgFreezeProgress(1, 2);
                }
                else if (projId == (int)ProjIds.IceGattlingHeadshot)
                {
                    character.addIgFreezeProgress(2, 2);
                }
                else if (projId == (int)ProjIds.IceGattlingHyper)
                {
                    character.addIgFreezeProgress(2, 2);
                }
                else if (projId == (int)ProjIds.Hyouretsuzan)
                {
                    character.addIgFreezeProgress(3, 2);
                }
                else if (projId == (int)ProjIds.Hyouretsuzan2)
                {
                    character.freeze(2);
                    flinch = 0;
                }
                else if (projId == (int)ProjIds.VelGIce)
                {
                    character.addIgFreezeProgress(2, 2);
                }
                else if (projId == (int)ProjIds.BBuffaloBeam)
                {
                    character.freeze(2);
                }
                else if (projId == (int)ProjIds.PlasmaGun)
                {
                    character.barrierCooldown = 3;
                    character.barrierTime = 0;
                }
                else if (projId == (int)ProjIds.ShotgunIceCharged)
                {
                    character.addIgFreezeProgress(4, 5);
                }
                else if (projId == (int)ProjIds.ChillPIceBlow)
                {
                    character.addIgFreezeProgress(4, 2);
                }
                else if (projId == (int)ProjIds.HyorogaProj)
                {
                    character.addIgFreezeProgress(1.5f, 2);
                }
                else if (projId == (int)ProjIds.HyorogaSwing)
                {
                    character.addIgFreezeProgress(4, 2);
                }
                else if (projId == (int)ProjIds.SeaDragonRage)
                {
                    character.addIgFreezeProgress(1, 2);
                }
                else if (projId == (int)ProjIds.SplashLaser)
                {
                    if (damagingActor != null)
                    {
                        character.splashLaserKnockback(damagingActor.deltaPos);
                    }
                }
                else if (projId == (int)ProjIds.MechFrogStompShockwave || projId == (int)ProjIds.FlameMStompShockwave || projId == (int)ProjIds.TBreakerProj)
                {
                    if (character.grounded)
                    {
                        character.changeState(new KnockedDown(character.pos.x < damagingActor?.pos.x ? -1 : 1), true);
                    }
                }
                else if (projId == (int)ProjIds.MechFrogGroundPound)
                {
                    if (!character.grounded)
                    {
                        character.vel.y += 300;
                        spiked = true;
                    }
                }
                else if (weaponIndex == (int)WeaponIds.Boomerang || weaponIndex == (int)WeaponIds.BoomerKBoomerang)
                {
                    if (character.player.isX) character.stingChargeTime = 0;
                }
                else if (projId == (int)ProjIds.FlameMOil)
                {
                    character.addOilTime(owner, 8);
                    character.playSound("flamemOil");
                }
                else if (projId == (int)ProjIds.DarkHold)
                {
                    character.addDarkHoldTime(owner, 4);
                }
                else if (projId == (int)ProjIds.MagnaCTail)
                {
                    character.addInfectedTime(owner, 4f);
                }

                if (owner?.character?.isNightmareZeroBS.getValue() == true)
                {
                    character.addInfectedTime(owner, damage);
                }

                #endregion

                if (character.player.isX)
                {
                    if (character.checkMaverickWeakness((ProjIds)projId))
                    {
                        weakness = true;
                        flinch = Global.defFlinch;
                        if (damage == 0) damage = 4;
                    }
                }

                float flinchCooldown = 0;
                if (projectileFlinchCooldowns.ContainsKey(projId))
                {
                    flinchCooldown = projectileFlinchCooldowns[projId];
                }

                if (owner.character != null && owner.character.isBlackZero() && projId != (int)ProjIds.Burn)
                {
                    if (flinch >= Global.halfFlinch)
                    {
                        flinch = Global.defFlinch;
                    }
                    else
                    {
                        flinch = Global.halfFlinch;
                        flinchCooldown = 1;
                    }
                    damage = MathF.Ceiling(damage * 1.5f);
                }

                if (flinchCooldown > 0)
                {
                    int flinchKey = getFlinchKeyFromProjId(projId);
                    if (!character.flinchCooldown.ContainsKey(flinchKey))
                    {
                        character.flinchCooldown[flinchKey] = 0;
                    }
                    if (character.flinchCooldown[flinchKey] > 0)
                    {
                        flinch = 0;
                    }
                    else
                    {
                        character.flinchCooldown[flinchKey] = flinchCooldown;
                    }
                }

                if (character.isVileMK2 && damage > 0 && !isArmorPiercing(projId))
                {
                    if (hitFromBehind(character, damagingActor, owner))
                    {
                        damage--;

                        if (damage < 1)
                        {
                            damage = 0;
                            character.playSound("ding");
                        }
                    }
                }

                if (damage > 0)
                {
                    bool isShotgunIceAndFrozen = character.sprite.name.Contains("frozen") && weaponKillFeedIndex == 8;
                    if ((flinch > 0 || weakness) && !isShotgunIceAndFrozen)
                    {
                        float miniFlinchTime = 0;
                        bool isMiniFlinch = getIsMiniFlinch(projId);
                        
                        if (isMiniFlinch)
                        {
                            miniFlinchTime = 0.1f;
                            victim?.playSound("hit");
                        }
                        else
                        {
                            victim?.playSound("hurt");
                        }

                        if (flinch == 0) flinch = Global.defFlinch;
                        int hurtDir = -character.xDir;
                        if (damagingActor != null)
                        {
                            hurtDir = damagingActor.pos.x > character.pos.x ? -1 : 1;
                        }
                        if (projId == (int)ProjIds.GravityWellCharged)
                        {
                            hurtDir = 0;
                        }
                        character.setHurt(hurtDir, flinch, miniFlinchTime, spiked);

                        if (weaponKillFeedIndex == 18)
                        {
                            //character.punchFlinchCooldown = Global.spf;
                        }
                    }
                    else
                    {
                        if (playHurtSound || weaponKillFeedIndex == 18 ||
                            (projId == (int)ProjIds.BlackArrow && damage > 1) ||
                            ((projId == (int)ProjIds.SpiralMagnum || projId == (int)ProjIds.SpiralMagnumScoped) && damage > 2) ||
                            ((projId == (int)ProjIds.AssassinBullet || projId == (int)ProjIds.AssassinBulletQuick) && damage > 8))
                        {
                            victim?.playSound("hurt");
                        }
                        else victim?.playSound("hit");
                    }
                }
            }
            // Ride armor section
            else if (rideArmor != null)
            {
                // Ride armor v. ride armor punch knockback
                if (flinch > 0 && rideArmor.ownedByLocalPlayer && owner != null)
                {
                    float pushDirection = -victim.xDir;
                    if (owner?.character != null)
                    {
                        if (victim.pos.x > owner.character.pos.x) pushDirection = 1;
                        if (victim.pos.x < owner.character.pos.x) pushDirection = -1;
                    }
                    rideArmor.xPushVel = pushDirection * 240f * (flinch / 26f);
                    if (rideArmor.raNum == 1 || rideArmor.raNum == 4) rideArmor.xPushVel *= 0.5f;
                    rideArmor.playHurtAnim();
                }

                if (projId == (int)ProjIds.BeastKiller || projId == (int)ProjIds.AncientGun)
                {
                    damage *= 2;
                }

                if (damage > 0)
                {
                    victim.playSound("hurt");
                }
            }
            // Maverick section
            else if (maverick != null)
            {
                if (projId == (int)ProjIds.BeastKiller || projId == (int)ProjIds.AncientGun)
                {
                    damage *= 2;
                }

                if (maverick.player.isTagTeam() && !maverick.state.superArmor)
                {
                    if (flinch < Global.halfFlinch) flinch = 0;
                    // Large mavericks
                    if (maverick.isHeavy)
                    {
                        flinch = 0;
                    }
                    // Small mavericks
                    else if (maverick is ChillPenguin || maverick is Velguarder || maverick is MorphMothCocoon || maverick is BubbleCrab || maverick is CrystalSnail)
                    {
                        
                    }
                    // Medium mavericks
                    else
                    {
                        flinch = (flinch == Global.defFlinch ? Global.halfFlinch : flinch);
                    }
                }
                else
                {
                    flinch = 0;
                }

                bool isOnFlinchCooldown = false;

                float flinchCooldownTime = projectileFlinchCooldowns.ContainsKey(projId) ? projectileFlinchCooldowns[projId] : 0.75f;
                int flinchKey = getFlinchKeyFromProjId(projId);
                if (!maverick.flinchCooldown.ContainsKey(flinchKey))
                {
                    maverick.flinchCooldown[flinchKey] = 0;
                }
                if (maverick.flinchCooldown[flinchKey] > 0)
                {
                    isOnFlinchCooldown = true;
                }
                else
                {
                    maverick.flinchCooldown[flinchKey] = flinchCooldownTime;
                }

                weakness = maverick.checkWeakness((WeaponIds)weaponIndex, (ProjIds)projId, out MaverickState newState, owner?.isSigma ?? true);
                if (weakness && (projId == (int)ProjIds.CrystalHunter || projId == (int)ProjIds.CSnailCrystalHunter))
                {
                    damage = 2;
                }
                if (weakness && (projId == (int)ProjIds.AcidBurst || projId == (int)ProjIds.TSeahorseAcid1 || projId == (int)ProjIds.TSeahorseAcid2))
                {
                    damage = 1;
                }
                if (weakness && (projId == (int)ProjIds.ParasiticBomb))
                {
                    damage = 2;
                }

                if (newState != null && !isOnFlinchCooldown)
                {
                    if (maverick.ownedByLocalPlayer)
                    {
                        maverick.changeState(newState, true);
                    }    
                }

                if (weakness)
                {
                    flinch = Global.defFlinch;
                }
                else
                {
                    if (maverick is ArmoredArmadillo aa)
                    {
                        if ((hitFromBehind(maverick, damagingActor, owner) || maverick.sprite.name == "armoreda_roll") && !aa.hasNoArmor() && !isArmorPiercingOrElectric(projId))
                        {
                            damage = MathF.Floor(damage * 0.5f);
                            if (damage == 0)
                            {
                                maverick.playSound("ding");
                            }
                        }
                    }

                    /*
                    if (maverick is CrystalSnail cs)
                    {
                        if ((hitFromBehind(maverick, damagingActor, owner)) && !cs.noShell)
                        {
                            damage = 0;
                            maverick.playSound("ding");
                        }
                    }
                    */

                    if (maverick.sprite.name == "armoreda_block" && damage > 0 && !isArmorPiercingOrElectric(projId))
                    {
                        if (isArmoredAGuardBlocking(maverick, damagingActor, owner))
                        {
                            if (maverick.ownedByLocalPlayer && damage > 2 && damagingActor is Projectile proj && proj.shouldVortexSuck && proj.destroyOnHit)
                            {
                                maverick.changeState(new ArmoredAGuardChargeState(damage * 2));
                            }
                            flinch = 0;
                            damage = 0;
                            maverick.playSound("ding");
                            if (owner.isZero && owner.character != null && owner.ownedByLocalPlayer && !owner.character.isHyperZero())
                            {
                                if (projId == (int)ProjIds.ZSaber || projId == (int)ProjIds.ZSaber1 || projId == (int)ProjIds.ZSaber2 || projId == (int)ProjIds.ZSaber3)
                                {
                                    owner.character.changeState(new ZeroClang(-owner.character.xDir));
                                }
                            }
                        }
                    }
                }

                if (damage > 0)
                {
                    if (flinch > 0 && !isOnFlinchCooldown)
                    {
                        victim.playSound("weakness");
                        if (newState == null)
                        {
                            int hurtDir = -maverick.xDir;
                            if (damagingActor != null)
                            {
                                hurtDir = damagingActor.pos.x > maverick.pos.x ? -1 : 1;
                            }
                            if (maverick.ownedByLocalPlayer)
                            {
                                maverick.changeState(new MHurt(hurtDir, flinch), true);
                            }
                        }
                    }
                    else
                    {
                        victim.playSound("hit");
                    }
                }
            }
            // Misc section
            else
            {
                if (damage > 0)
                {
                    victim?.playSound("hit");
                }
            }

            if (damage > 0 && character?.isDarkHoldBS.getValue() != true)
            {
                victim.addRenderEffect(RenderEffectType.Hit, 0.05f, 0.1f);
            }

            float finalDamage = damage * (weakness ? 2 : 1) * owner.getDamageModifier();

            if (finalDamage > 0 && character != null && character.ownedByLocalPlayer && character.charState is XUPParryStartState parryState && parryState.canParry() && !isDot(projId))
            {
                parryState.counterAttack(owner, damagingActor, Math.Max(finalDamage * 2, 4));
                return true;
            }
            else if (finalDamage > 0 && character != null && character.ownedByLocalPlayer && character.charState is KKnuckleParryStartState parryState2 && parryState2.canParry(damagingActor) && !isDot(projId))
            {
                parryState2.counterAttack(owner, damagingActor, 4);
                return true;
            }

            damagable.applyDamage(owner, weaponKillFeedIndex, finalDamage, projId);

            return true;
        }

        private static bool getIsMiniFlinch(int projId)
        {
            return
                projId == (int)ProjIds.ElectricSpark ||
                projId == (int)ProjIds.TriadThunderBall ||
                projId == (int)ProjIds.TriadThunderBeam ||
                projId == (int)ProjIds.TriadThunder ||
                projId == (int)ProjIds.PeaceOutRoller ||
                projId == (int)ProjIds.RayGun ||
                projId == (int)ProjIds.RayGun2 ||
                projId == (int)ProjIds.PlasmaGun2 ||
                projId == (int)ProjIds.PlasmaGun2Hyper ||
                projId == (int)ProjIds.VoltTornado ||
                projId == (int)ProjIds.VoltTornadoHyper ||
                projId == (int)ProjIds.Sigma2Ball ||
                projId == (int)ProjIds.VoltCTriadThunder ||
                projId == (int)ProjIds.DrDopplerBall ||
                projId == (int)ProjIds.CopyShot;
        }

        public static bool isArmorPiercingOrElectric(int? projId)
        {
            return isArmorPiercing(projId) || isElectric(projId);
        }

        public static bool isArmorPiercing(int? projId)
        {
            if (projId == null) return false;
            return
                projId == (int)ProjIds.SpiralMagnum ||
                projId == (int)ProjIds.AssassinBullet ||
                projId == (int)ProjIds.AssassinBulletQuick ||
                projId == (int)ProjIds.VileMK2Grab ||
                projId == (int)ProjIds.UPGrab ||
                projId == (int)ProjIds.LaunchODrain ||
                projId == (int)ProjIds.DistanceNeedler ||
                projId == (int)ProjIds.Raijingeki ||
                projId == (int)ProjIds.Raijingeki2 ||
                projId == (int)ProjIds.CFlasher ||
                projId == (int)ProjIds.AcidBurstPoison ||
                projId == (int)ProjIds.MetteurCrash;
        }

        public static bool isDot(int? projId)
        {
            if (projId == null) return false;
            return
                projId == (int)ProjIds.AcidBurstPoison ||
                projId == (int)ProjIds.Burn;
        }

        public static bool isElectric(int? projId)
        {
            return
                projId == (int)ProjIds.ElectricSpark ||
                projId == (int)ProjIds.ElectricSparkCharged ||
                projId == (int)ProjIds.TriadThunder ||
                projId == (int)ProjIds.TriadThunderBall ||
                projId == (int)ProjIds.TriadThunderBeam ||
                projId == (int)ProjIds.TriadThunderCharged ||
                projId == (int)ProjIds.Raijingeki ||
                projId == (int)ProjIds.Raijingeki2 ||
                projId == (int)ProjIds.EBlade ||
                projId == (int)ProjIds.PeaceOutRoller ||
                projId == (int)ProjIds.PlasmaGun ||
                projId == (int)ProjIds.PlasmaGun2 ||
                projId == (int)ProjIds.PlasmaGun2Hyper ||
                projId == (int)ProjIds.VoltTornado ||
                projId == (int)ProjIds.VoltTornadoHyper ||
                projId == (int)ProjIds.SparkMSpark ||
                projId == (int)ProjIds.SigmaHandElecBeam ||
                projId == (int)ProjIds.Sigma2Ball ||
                projId == (int)ProjIds.Sigma2Ball2 ||
                projId == (int)ProjIds.WSpongeLightning;
        }

        private static int getFlinchKeyFromProjId(int projId)
        {
            if (projId == (int)ProjIds.TriadThunder || projId == (int)ProjIds.TriadThunderBall || projId == (int)ProjIds.TriadThunderBeam)
            {
                projId = (int)ProjIds.TriadThunder;
            }
            return 1000 + projId;
        }

        private static bool hitFromBehind(Actor actor, Actor proj, Player projOwner)
        {
            if (proj != null && proj is Projectile && proj.deltaPos.x != 0)
            {
                if (actor.xDir == -1 && proj.deltaPos.x < 0)
                {
                    return true;
                }
                else if (actor.xDir == 1 && proj.deltaPos.x > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (projOwner.character == null) return false;
                Point damagePos = projOwner.character.pos;
                return ((actor.pos.x < damagePos.x && actor.xDir == -1) || (actor.pos.x > damagePos.x && actor.xDir == 1));
            }
        }

        private static bool isArmoredAGuardBlocking(Maverick aa, Actor proj, Player projOwner)
        {
            if (proj != null && proj is Projectile && proj.deltaPos.x != 0)
            {
                if (aa.xDir == 1 && proj.deltaPos.x < 0)
                {
                    return true;
                }
                else if (aa.xDir == -1 && proj.deltaPos.x > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (projOwner?.character == null) return false;
                Point damagePos = projOwner.character.pos;
                return ((aa.pos.x < damagePos.x && aa.xDir == 1) || (aa.pos.x > damagePos.x && aa.xDir == -1));
            }
        }

        private static bool isVictimImmuneToQuake(Actor victim)
        {
            if (victim is CrackedWall) return false;
            if (!victim.grounded) return true;
            if (victim is Character chr && chr.charState is WallSlide) return true;
            return false;
        }

        public static DamagerMessage onCrystalDamage(IDamagable damagable, Player attacker, int crystalTime)
        {
            var character = damagable as Character;
            if (character != null && character.ownedByLocalPlayer && character.canCrystalize())
            {
                character.vel.y = 0;
                character.changeState(new Crystalized(crystalTime), true);
            }
            return null;
        }

        public static DamagerMessage onAcidDamage(IDamagable damagable, Player attacker, float acidTime)
        {
            (damagable as Character)?.addAcidTime(attacker, acidTime);
            return null;
        }

        public static bool unassistable(int? projId)
        {
            return projId == (int)ProjIds.Burn ||
                   projId == (int)ProjIds.Tornado ||
                   projId == (int)ProjIds.VoltTornado ||
                   projId == (int)ProjIds.VoltTornadoHyper ||
                   projId == (int)ProjIds.FlameBurner ||
                   projId == (int)ProjIds.FlameBurner2 ||
                   projId == (int)ProjIds.FlameBurnerHyper ||
                   projId == (int)ProjIds.BoomerangCharged ||
                   projId == (int)ProjIds.Napalm2Flame ||
                   projId == (int)ProjIds.Napalm2Wall ||
                   projId == (int)ProjIds.TunnelFang ||
                   projId == (int)ProjIds.TunnelFang2 ||
                   projId == (int)ProjIds.GravityWell ||
                   projId == (int)ProjIds.SpinWheel ||
                   projId == (int)ProjIds.DistanceNeedler ||
                   projId == (int)ProjIds.TriadThunder ||
                   projId == (int)ProjIds.TriadThunderBeam ||
                   projId == (int)ProjIds.RayGun2 ||
                   projId == (int)ProjIds.Napalm ||
                   projId == (int)ProjIds.CircleBlaze ||
                   projId == (int)ProjIds.CircleBlazeExplosion ||
                   projId == (int)ProjIds.GLauncher ||
                   projId == (int)ProjIds.GLauncherSplash ||
                   projId == (int)ProjIds.BoundBlaster2 ||
                   projId == (int)ProjIds.NapalmSplashHit;
        }

        public static DamagerMessage onParasiticBombDamage(IDamagable damagable, Player attacker)
        {
            var chr = damagable as Character;
            if (chr != null && chr.ownedByLocalPlayer && !chr.hasParasite)
            {
                chr.addParasite(attacker);
                chr.playSound("parasiteBombLatch", sendRpc: true);
            }

            return null;
        }

        public static DamagerMessage onStunShotDamage(IDamagable damagable, Player attacker)
        {
            var character = damagable as Character;
            if (character != null && !character.isInvulnerable() && !(character.charState is Hurt) && !(character.charState is Die) && character.player.alliance != attacker.alliance)
            {
                if (!(character.charState is Stunned))
                {
                    character.changeState(new Stunned(), true);
                }
            }
            return null;
        }

        public static bool canDamageFrostShield(int projId)
        {
            if (CrackedWall.canDamageCrackedWall(projId, null) != 0)
            {
                return true;
            }
            if (projId == (int)ProjIds.FireWave) return true;
            if (projId == (int)ProjIds.FireWaveCharged) return true;
            if (projId == (int)ProjIds.SpeedBurner) return true;
            if (projId == (int)ProjIds.SpeedBurnerCharged) return true;
            if (projId == (int)ProjIds.Napalm2) return true;
            if (projId == (int)ProjIds.Napalm2Flame) return true;
            if (projId == (int)ProjIds.Ryuenjin) return true;
            if (projId == (int)ProjIds.FlameBurner) return true;
            if (projId == (int)ProjIds.FlameBurnerHyper) return true;
            if (projId == (int)ProjIds.CircleBlazeExplosion) return true;
            if (projId == (int)ProjIds.QuakeBlazer) return true;
            if (projId == (int)ProjIds.QuakeBlazerFlame) return true;
            if (projId == (int)ProjIds.FlameMFireball) return true;
            if (projId == (int)ProjIds.FlameMOilFire) return true;
            if (projId == (int)ProjIds.VelGFire) return true;
            if (projId == (int)ProjIds.SigmaWolfHeadFlameProj) return true;
            if (projId == (int)ProjIds.WildHorseKick) return true;
            return false;
        }

        public static bool isBoomerang(int? projId)
        {
            if (projId == null) return false;
            return projId == (int)ProjIds.Boomerang || projId == (int)ProjIds.BoomerangCharged || projId == (int)ProjIds.BoomerKBoomerang;
        }

        public static bool isSonicSlicer(int? projId)
        {
            if (projId == null) return false;
            return projId == (int)ProjIds.SonicSlicer || projId == (int)ProjIds.SonicSlicerCharged || projId == (int)ProjIds.SonicSlicerChargedStart || projId == (int)ProjIds.OverdriveOSonicSlicer || projId == (int)ProjIds.OverdriveOSonicSlicerUp;
        }
    }

    public class DamagerMessage
    {
        public int? flinch;
        public float? damage;
    }
}
