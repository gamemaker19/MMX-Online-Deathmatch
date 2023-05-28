
using SFML.Graphics;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static SFML.Window.Keyboard;

namespace MMXOnline
{
    public partial class Character : Actor, IDamagable
    {
        public static string[] charDisplayNames =
        {
            "X",
            "Zero",
            "Vile",
            "Axl",
            "Sigma"
        };

        public CharState charState;
        public Player player;
        public bool isDashing;
        public float shootTime
        {
            get { return player.weapon.shootTime; }
            set { player.weapon.shootTime = value; }
        }
        public bool changedStateInFrame;
        public bool pushedByTornadoInFrame;
        public float chargeTime;
        public const float charge1Time = 0.75f;
        public const float charge2Time = 1.75f;
        public const float charge3Time = 3f;
        public const float charge4Time = 4.25f;
        public float hyperProgress;

        public Point? sigmaHeadGroundCamCenterPos;
        public float chargeFlashTime;
        public ChargeEffect chargeEffect;
        public float shootAnimTime = 0;
        public AI ai;
        public bool slowdown;
        public bool boughtUltimateArmorOnce;
        public bool boughtGoldenArmorOnce;

        public float headbuttAirTime = 0;
        public int dashedInAir = 0;
        public bool lastAirDashWasSide;
        public float healAmount = 0;
        public SubTank usedSubtank;
        public float netSubtankHealAmount;
        public bool playHealSound;
        public float healTime = 0;
        public float weaponHealAmount = 0;
        public float weaponHealTime = 0;
        public float healthBarInnerWidth;
        public float slideVel = 0;
        public Flag flag;
        public float stingChargeTime;
        public bool isCrystalized;
        public bool insideCharacter;
        public float invulnTime = 0;
        public float parryCooldown;
        public float maxParryCooldown = 0.5f;

        public bool stockedCharge;
        public void stockCharge(bool stockOrUnstock)
        {
            stockedCharge = stockOrUnstock;
            if (ownedByLocalPlayer)
            {
                RPC.playerToggle.sendRpc(player.id, stockOrUnstock ? RPCToggleType.StockCharge : RPCToggleType.UnstockCharge);
            }
        }

        public bool stockedXSaber;
        public void stockSaber(bool stockOrUnstock)
        {
            stockedXSaber = stockOrUnstock;
            if (ownedByLocalPlayer)
            {
                RPC.playerToggle.sendRpc(player.id, stockOrUnstock ? RPCToggleType.StockSaber : RPCToggleType.UnstockSaber);
            }
        }
        public float xSaberCooldown;
        public float stockedChargeFlashTime;

        public List<Trail> lastFiveTrailDraws = new List<Trail>();
        public LoopingSound chargeSound;

        public ShaderWrapper possessedShader;
        public ShaderWrapper acidShader;
        public ShaderWrapper igShader;
        public ShaderWrapper oilShader;
        public ShaderWrapper infectedShader;
        public ShaderWrapper frozenCastleShader;
        public ShaderWrapper vaccineShader;
        public ShaderWrapper darkHoldShader;

        public float headshotRadius
        {
            get
            {
                return 6f;
            }
        }

        float damageSavings;
        float damageDebt;

        public bool stopCamUpdate = false;
        public Anim warpBeam;
        public float flattenedTime;
        public float wallKickCooldown;
        public float saberCooldown;

        public const float maxLastAttackerTime = 5;

        public float igFreezeProgress;
        public float freezeInvulnTime;
        public float stunInvulnTime;
        public float crystalizeInvulnTime;
        public float grabInvulnTime;
        public float darkHoldInvulnTime;

        public float limboRACheckCooldown;
        public RideArmor rideArmor;
        public RideChaser rideChaser;
        public Player lastGravityWellDamager;

        public Character(Player player, float x, float y, int xDir, bool isVisible, ushort? netId, bool ownedByLocalPlayer, bool isWarpIn = true, bool mk2VileOverride = false, bool mk5VileOverride = false)
            : base(null, new Point(x, y), netId, ownedByLocalPlayer, dontAddToLevel: true)
        {
            this.player = player;
            this.xDir = xDir;
            initNetCharState1();
            initNetCharState2();

            isDashing = false;
            splashable = true;

            CharState charState;

            if (player.isVile && isWarpIn)
            {
                if (mk5VileOverride) vileForm = 2;
                else if (mk2VileOverride) vileForm = 1;
                
                if (player.vileFormToRespawnAs == 2 || Global.quickStartVileMK5 == true)
                {
                    vileForm = 2;
                }
                else if (player.vileFormToRespawnAs == 1 || Global.quickStartVileMK2 == true)
                {
                    vileForm = 1;
                }
            }

            if (ownedByLocalPlayer)
            {
                if (player.maverick1v1 != null)
                {
                    charState = new WarpOut(true);
                }
                else if (isWarpIn) charState = new WarpIn();
                else charState = new Idle();
            }
            else
            {
                charState = new Idle();
            }

            spriteToCollider["roll"] = getDashingCollider();
            spriteToCollider["dash*"] = getDashingCollider();
            spriteToCollider["crouch*"] = getCrouchingCollider();
            spriteToCollider["ra_*"] = getRaCollider();
            spriteToCollider["rc_*"] = getRcCollider();
            spriteToCollider["head*"] = getSigmaHeadCollider();
            spriteToCollider["warp_beam"] = null;
            spriteToCollider["warp_out"] = null;
            spriteToCollider["warp_in"] = null;
            spriteToCollider["revive"] = null;
            spriteToCollider["revive_to5"] = null;
            spriteToCollider["die"] = null;
            spriteToCollider["block"] = getBlockCollider();

            changeState(charState);

            visible = isVisible;

            chargeTime = 0;
            chargeFlashTime = 0;
            useFrameProjs = true;

            chargeSound = new LoopingSound("charge_start", "charge_loop", this);
            iceGattlingSound = new LoopingSound("iceGattlingLoopStart", "iceGattlingLoopStop", "iceGattlingLoop", this);

            if (this.player != Global.level.mainPlayer)
            {
                zIndex = ++Global.level.autoIncCharZIndex;
            }
            else
            {
                zIndex = ZIndex.MainPlayer;
            }

            Global.level.addGameObject(this);

            chargeEffect = new ChargeEffect();

            xPaletteShader = Helpers.cloneShaderSafe("palette");
            invisibleShader = Helpers.cloneShaderSafe("invisible");
            zeroPaletteShader = Helpers.cloneShaderSafe("hyperzero");
            nightmareZeroShader = Helpers.cloneNightmareZeroPaletteShader("paletteNightmareZero");
            axlPaletteShader = Helpers.cloneShaderSafe("hyperaxl");
            viralSigmaShader = Helpers.cloneShaderSafe("viralsigma");
            sigmaShieldShader = Helpers.cloneGenericPaletteShader("paletteSigma3Shield");
            acidShader = Helpers.cloneShaderSafe("acid");
            oilShader = Helpers.cloneShaderSafe("oil");
            igShader = Helpers.cloneShaderSafe("igIce");
            infectedShader = Helpers.cloneShaderSafe("infected");
            frozenCastleShader = Helpers.cloneShaderSafe("frozenCastle");
            possessedShader = Helpers.cloneShaderSafe("possessed");
            vaccineShader = Helpers.cloneShaderSafe("vaccine");
            darkHoldShader = Helpers.cloneShaderSafe("darkhold");

            muzzleFlash = new Anim(new Point(), "axl_pistol_flash", xDir, null, false);
            muzzleFlash.visible = false;
        }

        public override void onStart()
        {
            base.onStart();
        }

        public float vaccineTime;
        public float vaccineHurtCooldown;
        public void addVaccineTime(float time)
        {
            if (!ownedByLocalPlayer) return;
            vaccineTime += time;
            if (vaccineTime > 8) vaccineTime = 8;
            if (charState is Frozen || charState is Crystalized || charState is Stunned)
            {
                changeToIdleOrFall();
            }
            burnTime = 0;
            acidTime = 0;
            oilTime = 0;
            player.possessedTime = 0;
        }
        public bool isVaccinated() { return vaccineTime > 0; }

        public float infectedTime;
        public Damager infectedDamager;
        public void addInfectedTime(Player attacker, float time)
        {
            if (!ownedByLocalPlayer) return;
            if (isInvulnerable()) return;
            if (isVaccinated()) return;

            Damager damager = new Damager(attacker, 0, 0, 0);
            if (infectedTime == 0 || infectedDamager == null)
            {
                infectedDamager = damager;
            }
            else if (infectedDamager.owner != damager.owner) return;
            infectedTime += time;
            if (infectedTime > 8) infectedTime = 8;
        }

        public void addDarkHoldTime(Player attacker, float time)
        {
            if (!ownedByLocalPlayer) return;
            if (isInvulnerable()) return;
            if (isVaccinated()) return;

            changeState(new DarkHoldState(this), true);
        }

        public float acidTime;
        public float acidHurtCooldown;
        public Damager acidDamager;
        public void addAcidTime(Player attacker, float time)
        {
            if (!ownedByLocalPlayer) return;
            if (chargedRollingShieldProj != null) return;
            if (isInvulnerable()) return;
            if (isVaccinated()) return;

            Damager damager = new Damager(attacker, 0, 0, 0);
            if (acidTime == 0 || acidDamager == null)
            {
                acidDamager = damager;
            }
            else if (acidDamager.owner != damager.owner) return;
            acidHurtCooldown = 0.5f;
            acidTime += time;
            oilTime = 0;
            if (acidTime > 8) acidTime = 8;
        }

        public float oilTime;
        public Damager oilDamager;
        public void addOilTime(Player attacker, float time)
        {
            if (!ownedByLocalPlayer) return;
            if (chargedRollingShieldProj != null) return;
            if (isInvulnerable()) return;
            if (isVaccinated()) return;

            Damager damager = new Damager(attacker, 0, 0, 0);
            if (oilTime == 0 || oilDamager == null)
            {
                oilDamager = damager;
            }
            else if (oilDamager.owner != damager.owner) return;
            oilTime += time;
            acidTime = 0;
            if (oilTime > 8) oilTime = 8;

            if (burnTime > 0)
            {
                float oldBurnTime = burnTime;
                burnTime = 0;
                addBurnTime(attacker, new FlameMOilWeapon(), oldBurnTime + 2);
                return;
            }
        }

        public float burnTime;
        public float burnEffectTime;
        public float burnHurtCooldown;
        public Damager burnDamager;
        public Weapon burnWeapon;
        public void addBurnTime(Player attacker, Weapon weapon, float time)
        {
            if (!ownedByLocalPlayer) return;
            if (chargedRollingShieldProj != null) return;
            if (isInvulnerable()) return;
            if (isVaccinated()) return;

            Damager damager = new Damager(attacker, 0, 0, 0);
            if (burnTime == 0 || burnDamager == null)
            {
                burnDamager = damager;
                burnWeapon = weapon;
            }
            else if (burnDamager.owner != damager.owner) return;
            burnHurtCooldown = 0.5f;
            burnTime += time;
            if (oilTime > 0)
            {
                playSound("flamemOilBurn", sendRpc: true);
                damager.applyDamage(this, false, weapon, this, (int)ProjIds.Burn, overrideDamage: 2, overrideFlinch: Global.defFlinch);
                burnTime += oilTime;
                oilTime = 0;
            }
            if (burnTime > 8) burnTime = 8;
        }

        float igFreezeRecoveryCooldown = 0;
        public void addIgFreezeProgress(float amount, int freezeTime)
        {
            if (freezeInvulnTime > 0) return;
            if (charState is Frozen) return;
            if (isCCImmune()) return;
            if (isInvulnerable()) return;
            if (isVaccinated()) return;

            igFreezeProgress += amount;
            igFreezeRecoveryCooldown = 0;
            if (igFreezeProgress >= 4)
            {
                igFreezeProgress = 0;
                freeze(freezeTime);
            }
        }

        public bool isCStingInvisible()
        {
            if (!player.isX) return false;
            if (isInvisibleBS.getValue() == false) return false;
            return true;
        }

        public bool isCStingInvisibleGraphics()
        {
            if (!player.isX) return false;
            if (hasUltimateArmorBS.getValue() == true) return false;
            if (isInvisibleBS.getValue() == false) return false;
            return true;
        }

        public override List<ShaderWrapper> getShaders()
        {
            var shaders = new List<ShaderWrapper>();
            ShaderWrapper palette = null;
            if (player.isX)
            {
                int index = player.weapon.index;
                if (index == (int)WeaponIds.GigaCrush || index == (int)WeaponIds.ItemTracer || index == (int)WeaponIds.AssassinBullet || index == (int)WeaponIds.Undisguise || index == (int)WeaponIds.UPParry) index = 0;
                if (index == (int)WeaponIds.HyperBuster && ownedByLocalPlayer)
                {
                    index = player.weapons[player.hyperChargeSlot].index;
                }
                if (player.hasGoldenArmor()) index = 25;
                if (hasUltimateArmorBS.getValue()) index = 0;
                palette = xPaletteShader;

                if (!isCStingInvisibleGraphics())
                {
                    palette?.SetUniform("palette", index);
                    palette?.SetUniform("paletteTexture", Global.textures["paletteTexture"]);
                }
                else
                {
                    palette?.SetUniform("palette", cStingPaletteIndex % 9);
                    palette?.SetUniform("paletteTexture", Global.textures["cStingPalette"]);
                }
            }
            else if (player.isZero)
            {
                int paletteNum = 0;
                if (blackZeroTime > 3) paletteNum = 1;
                else if (blackZeroTime > 0)
                {
                    int mod = MathF.Ceiling(blackZeroTime) * 2;
                    paletteNum = (Global.frameCount % (mod * 2)) < mod ? 0 : 1;
                }
                palette = zeroPaletteShader;
                palette?.SetUniform("palette", paletteNum);
                if (!player.isZBusterZero())
                {
                    palette?.SetUniform("paletteTexture", Global.textures["hyperZeroPalette"]);
                }
                else
                {
                    palette?.SetUniform("paletteTexture", Global.textures["hyperBusterZeroPalette"]);
                }
                if (isNightmareZeroBS.getValue())
                {
                    palette = nightmareZeroShader;
                }
            }
            else if (player.isAxl)
            {
                int paletteNum = 0;
                if (whiteAxlTime > 3) paletteNum = 1;
                else if (whiteAxlTime > 0)
                {
                    int mod = MathF.Ceiling(whiteAxlTime) * 2;
                    paletteNum = (Global.frameCount % (mod * 2)) < mod ? 0 : 1;
                }
                palette = axlPaletteShader;
                palette?.SetUniform("palette", paletteNum);
                palette?.SetUniform("paletteTexture", Global.textures["hyperAxlPalette"]);
            }
            else if (player.isViralSigma())
            {
                int paletteNum = 6 - MathF.Ceiling((player.health / player.maxHealth) * 6);
                if (sprite.name.Contains("_enter")) paletteNum = 0;
                palette = viralSigmaShader;
                palette?.SetUniform("palette", paletteNum);
                palette?.SetUniform("paletteTexture", Global.textures["paletteViralSigma"]);
            }
            else if (player.isSigma3())
            {
                if (Global.isOnFrameCycle(8)) palette = sigmaShieldShader;
            }

            if (palette != null) shaders.Add(palette);

            if (player.isPossessed())
            {
                possessedShader?.SetUniform("palette", 1);
                possessedShader?.SetUniform("paletteTexture", Global.textures["palettePossessed"]);
                shaders.Add(possessedShader);
            }

            if (isDarkHoldBS.getValue() && darkHoldShader != null)
            {
                // If we are not already being affected by a dark hold shader, apply it. Otherwise for a brief period,
                // victims will be double color inverted, appearing normal
                if (!Global.level.darkHoldProjs.Any(dhp => dhp.screenShader != null && dhp.inRange(this)))
                {
                    shaders.Add(darkHoldShader);
                }
            }

            if (darkHoldShader != null)
            {
                // Invert the zero who used a dark hold so he appears to be drawn normally on top of it
                var myDarkHold = Global.level.darkHoldProjs.FirstOrDefault(dhp => dhp.owner == player);
                if (myDarkHold != null && myDarkHold.inRange(this))
                {
                    shaders.Add(darkHoldShader);
                }
            }

            if (acidTime > 0 && acidShader != null)
            {
                acidShader?.SetUniform("acidFactor", 0.25f + (acidTime / 8f) * 0.75f);
                shaders.Add(acidShader);
            }
            if (oilTime > 0 && oilShader != null)
            {
                oilShader?.SetUniform("oilFactor", 0.25f + (oilTime / 8f) * 0.75f);
                shaders.Add(oilShader);
            }
            if (vaccineTime > 0 && vaccineShader != null)
            {
                vaccineShader?.SetUniform("vaccineFactor", vaccineTime / 8f);
                //vaccineShader?.SetUniform("vaccineFactor", 1f);
                shaders.Add(vaccineShader);
            }
            if (igFreezeProgress > 0 && !sprite.name.Contains("frozen") && igShader != null)
            {
                igShader?.SetUniform("igFreezeProgress", igFreezeProgress / 4);
                shaders.Add(igShader);
            }
            if (infectedTime > 0 && infectedShader != null)
            {
                infectedShader?.SetUniform("infectedFactor", infectedTime / 8f);
                shaders.Add(infectedShader);
            }
            else if (player.isVile && isFrozenCastleActiveBS.getValue())
            {
                shaders.Add(frozenCastleShader);
            }

            if (!isCStingInvisibleGraphics())
            {
                if (renderEffects.ContainsKey(RenderEffectType.Invisible) && alpha == 1)
                {
                    invisibleShader?.SetUniform("alpha", 0.33f);
                    shaders.Add(invisibleShader);
                }
                // alpha float doesn't work if one or more shaders exist. So need to use the invisible shader instead
                else if (alpha < 1 && shaders.Count > 0)
                {
                    invisibleShader?.SetUniform("alpha", alpha);
                    shaders.Add(invisibleShader);
                }
            }

            return shaders;
        }

        public bool isInvisibleEnemy()
        {
            return player.alliance != Global.level.mainPlayer.alliance;
        }

        public void splashLaserKnockback(Point splashDeltaPos)
        {
            if (charState.invincible) return;
            if (isImmuneToKnockback()) return;

            if (isClimbingLadder())
            {
                setFall();
            }
            else
            {
                float modifier = 1;
                if (grounded) modifier = 0.5f;
                if (charState is Crouch) modifier = 0.25f;
                var pushVel = splashDeltaPos.normalize().times(200 * modifier);
                xPushVel = pushVel.x;
                vel.y = pushVel.y;
            }
        }

        // Stuck in place and can't do any action but still can activate controls, etc.
        public bool isSoftLocked()
        {
            if (isAnyZoom() || sniperMissileProj != null) return true;
            if (isShootingLongshotGizmo) return true;
            if (charState is WarpOut) return true;
            if (player.currentMaverick != null) return true;
            if (isVileMK5 && player.weapon is MechMenuWeapon && vileStartRideArmor != null) return true;
            if (player.isVile && sprite.name.EndsWith("_idle_shoot") && sprite.frameTime < 0.1f) return true;
            //if (player.weapon is MaverickWeapon mw && mw.isMenuOpened) return true;
            return false;
        }

        public bool canTurn()
        {
            if (mk5RideArmorPlatform != null) return false;
            return !isShootingLongshotGizmo;
        }

        public bool canMove()
        {
            if (iceSled != null) return false;
            if (mk5RideArmorPlatform != null) return false;
            if (isAimLocked())
            {
                return false;
            }
            if (isSoftLocked())
            {
                return false;
            }
            return true;
        }

        public bool canDash()
        {
            if (mk5RideArmorPlatform != null) return false;
            if (charState is WallKick wallKick && wallKick.stateTime < 0.25f) return false;
            if (isAnyZoom() || sniperMissileProj != null) return false;
            if (isSoftLocked()) return false;
            return !isAttacking() && flag == null && !(player.isAxl && isRevving);
        }

        public bool canJump()
        {
            if (mk5RideArmorPlatform != null) return false;
            if (isSoftLocked()) return false;
            return true;
        }

        public bool canCrouch()
        {
            if (isSoftLocked()) return false;
            return true;
        }

        public bool canAirDash()
        {
            return dashedInAir == 0 || (dashedInAir == 1 && player.isX && player.hasChip(0));
        }

        public bool canAirJump()
        {
            return dashedInAir == 0 || (dashedInAir == 1 && player.isZero && isBlackZero2());
        }

        public bool canClimb()
        {
            if (charState is ZSaberProjSwingState || charState is ZeroDoubleBuster) return false;
            if (mk5RideArmorPlatform != null) return false;
            if (isSoftLocked()) return false;
            if (charState is VileHover)
            {
                return !player.input.isHeld(Control.Jump, player);
            }
            return true;
        }

        public bool canStartClimbLadder()
        {
            if (charState is ZSaberProjSwingState || charState is ZeroDoubleBuster) return false;
            return true;
        }

        public bool canClimbLadder()
        {
            if (mk5RideArmorPlatform != null) return false;
            return vileLadderShootCooldown == 0 && shootAnimTime == 0 && !isAttacking() && recoilTime == 0 && !hasBusterProj() && !isShootingRaySplasher && !isSoftLocked() && !isSigmaShooting();
        }

        public bool canCharge()
        {
            if (beeSwarm != null) return false;
            var weapon = player.weapon;
            //if (weapon.ammo <= 0) return false;
            if (weapon is RollingShield && chargedRollingShieldProj != null) return false;
            if (isInvisibleBS.getValue()) return false;
            if (flag != null) return false;
            if (player.weapons.Count == 0) return false;
            if (weapon is AbsorbWeapon) return false;
            return true;
        }

        public bool canShoot()
        {
            if (isInvulnerableAttack()) return false;
            if (chargedSpinningBlade != null) return false;
            if (isShootingRaySplasher) return false;
            if (chargedFrostShield != null) return false;
            if (chargedTunnelFang != null) return false;
            if (sniperMissileProj != null) return false;

            return true;
        }

        public bool canChangeWeapons()
        {
            if (strikeChainProj != null) return false;
            if (isShootingRaySplasher) return false;
            if (chargedSpinningBlade != null) return false;
            if (chargedFrostShield != null) return false;
            if (gaeaShield != null) return false;
            if (sniperMissileProj != null) return false;
            if (charState is GravityWellChargedState) return false;
            if (player.weapon is TriadThunder triadThunder && triadThunder.shootTime > 0.75f) return false;
            if (player.weapon is AssassinBullet && chargeTime > 0) return false;
            if (revTime > 0.5f) return false;
            if (isShootingLongshotGizmo) return false;
            if (charState is XRevive || charState is XReviveStart) return false;
            if (charState is ViralSigmaPossess) return false;
            if (charState is InRideChaser) return false;

            return true;
        }

        public bool canPickupFlag()
        {
            if (player.isPossessed()) return false;
            if (dropFlagCooldown > 0) return false;
            if (isInvulnerable()) return false;
            if (player.isDisguisedAxl) return false;
            if (isCCImmuneHyperMode()) return false;
            if (charState is Die || charState is VileRevive || charState is XReviveStart || charState is XRevive) return false;
            if (charState is WolfSigmaRevive || charState is WolfSigma || sprite.name.StartsWith("sigma_head")) return false;
            if (charState is ViralSigmaRevive || charState is ViralSigmaIdle || sprite.name.StartsWith("sigma2_viral")) return false;
            if (charState is KaiserSigmaRevive || Helpers.isOfClass(charState, typeof(KaiserSigmaBaseState)) || sprite.name.StartsWith("sigma3_kaiser")) return false;
            if (player.currentMaverick != null && player.isTagTeam()) return false;
            if (isWarpOut()) return false;
            return true;
        }

        public bool isSoundCentered()
        {
            if (charState is WarpOut) return false;
            if (isHyperSigma) return false;
            return true;
        }

        public bool isAimLocked()
        {
            if (!player.isAxl) return false;
            if (player.input.isPositionLocked(player) && Options.main.axlAimMode == 0)
            {
                return true;
            }
            if (Options.main.axlAimMode == 0 && !Options.main.moveInDiagAim && !isDashing &&
                (grounded || charState is Hover || player.input.isHeld(Control.Shoot, player) || player.input.isHeld(Control.Special1, player)) &&
                (player.input.isHeld(Control.Up, player) || player.input.isHeld(Control.Down, player)))
            {
                return true;
            }
            return false;
        }

        public float getRunSpeed(bool isAirDash = false)
        {
            float runSpeed;
            if (player.isX)
            {
                if (charState is XHover) runSpeed = 125;
                else runSpeed = 100;
            }
            else if (player.isVile && player.speedDevil)
            {
                runSpeed = speedDevilRunSpeed;
            }
            else if (isBlackZero() || isBlackZero2())
            {
                runSpeed = 115;
            }
            else if (player.isAxl && shootTime > 0)
            {
                runSpeed = 100 - getAimBackwardsAmount() * 25;
            }
            else
            {
                runSpeed = 100;
            }
            if (slowdownTime > 0) runSpeed *= 0.75f;
            if (igFreezeProgress == 1) runSpeed *= 0.75f;
            if (igFreezeProgress == 2) runSpeed *= 0.5f;
            if (igFreezeProgress == 3) runSpeed *= 0.25f;
            return runSpeed;
        }

        public float getDashSpeed()
        {
            if (flag != null) return 1;
            if (isDashing)
            {
                if (player.axlWeapon != null && player.axlWeapon.isTwoHanded(false)) return 1.75f;
                return 2f;
            }
            return 1;
        }

        public float getJumpPower()
        {
            float jp = Physics.jumpPower;
            jp += (chargedBubbles.Count / 6.0f) * 50;

            if (slowdownTime > 0) jp *= 0.75f;
            if (igFreezeProgress == 1) jp *= 0.75f;
            if (igFreezeProgress == 2) jp *= 0.5f;
            if (igFreezeProgress == 3) jp *= 0.25f;

            return jp;
        }

        public void hook(Projectile strikeChainProj)
        {
            bool isChargedStrikeChain = strikeChainProj is StrikeChainProj scp && scp.isCharged;
            bool flinch = isChargedStrikeChain || strikeChainProj is WSpongeSideChainProj || (player.isX && player.weapon is SpinWheel);
            changeState(new StrikeChainHooked(strikeChainProj, flinch), true);
        }

        public override Collider getGlobalCollider()
        {
            if (player.isKaiserSigma())
            {
                return getKaiserSigmaGlobalCollider();
            }
            var rect = new Rect(0, 0, 18, 34);
            if (player.isZero) rect.y2 = 40;
            if (player.isVile) rect.y2 = 43;
            if (player.isSigma) rect.y2 = sigmaHeight;
            if (sprite.name.Contains("_ra_")) rect.y2 = 20;
            return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
        }

        public Collider getDashingCollider()
        {
            var rect = new Rect(0, 0, 18, 22);
            if (player.isZero) rect.y2 = 27;
            if (player.isVile) rect.y2 = 30;
            if (player.isSigma) rect.y2 = 40;
            return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
        }

        public Collider getCrouchingCollider()
        {
            var rect = new Rect(0, 0, 18, 22);
            if (player.isZero) rect.y2 = 27;
            if (player.isVile) rect.y2 = 30;
            return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
        }

        public Collider getRaCollider()
        {
            var rect = new Rect(0, 0, 18, 15);
            if (player.isZero) rect.y2 = 20;
            if (player.isVile) rect.y2 = 23;
            if (player.isSigma) rect.y2 = 30;
            return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
        }

        public Collider getRcCollider()
        {
            var rect = new Rect(0, -20, 18, 0);
            if (player.isZero) rect.y1 = -21;
            if (player.isVile) rect.y1 = -21;
            if (player.isSigma) rect.y1 = -24;
            return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
        }

        public Collider getSigmaHeadCollider()
        {
            var rect = new Rect(0, 0, 14, 20);
            return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
        }

        public Collider getBlockCollider()
        {
            var rect = new Rect(0, 0, 18, 34);
            if (player.isZero) rect = Rect.createFromWH(0, 0, 16, 16);
            if (player.isSigma)
            {
                if (player.isSigma1()) rect = Rect.createFromWH(0, 0, 16, 35);
                if (player.isSigma2()) rect = Rect.createFromWH(0, 0, 18, 50);
                if (player.isSigma3()) rect = Rect.createFromWH(0, 0, 23, 55);
            }
            return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
        }

        public override void preUpdate()
        {
            base.preUpdate();
            if (player.isSigma)
            {
                preUpdateSigma();
            }
            insideCharacter = false;
            changedStateInFrame = false;
            pushedByTornadoInFrame = false;
            lastXDir = xDir;
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (other.myCollider?.flag == (int)HitboxFlag.Hitbox || other.myCollider?.flag == (int)HitboxFlag.None) return;

            var killZone = other.gameObject as KillZone;
            if (killZone != null)
            {
                if (charState is WolfSigmaRevive wsr)
                {
                    stopMoving();
                    useGravity = false;
                    wsr.groundStart = true;
                    return;
                }
                if (!killZone.killInvuln && player.isKaiserSigma()) return;
                if (!killZone.killInvuln && isInvisibleBS.getValue()) return;
                if (rideArmor != null && rideArmor.rideArmorState is RADropIn) return;
                killZone.applyDamage(this);
            }
            
            var character = other.gameObject as Character;
            if ((charState is Dash || charState is AirDash) && character != null && character.isCrystalized && character.player.alliance != player.alliance)
            {
                Damager.applyDamage(player, 3, 1f, Global.defFlinch, character, false, (int)WeaponIds.CrystalHunter, 20, player.character, (int)ProjIds.CrystalHunterDash);
            }

            var moveZone = other.gameObject as MoveZone;
            if (moveZone != null)
            {
                xPushVel = moveZone.moveVel.x;
            }
            if (ownedByLocalPlayer && other.gameObject is Flag flag && flag.alliance != player.alliance)
            {
                if (!Global.level.players.Any(p => p != player && p.character?.flag == flag))
                {
                    if (charState is SpeedBurnerCharState || charState is SigmaWallDashState || charState is FSplasherState)
                    {
                        changeState(new Fall(), true);
                    }
                }
            }
        }

        public List<Tuple<string, int>> lastDTInputs = new List<Tuple<string, int>>();
        public string holdingDTDash;
        const int doubleDashFrames = 20;
        public float slowdownTime;
        public float dropFlagProgress;
        public float dropFlagCooldown;
        public bool dropFlagUnlocked;
        long originalZIndex;
        bool viralOnce;
        public override void update()
        {
            if (charState is not InRideChaser)
            {
                camOffsetX = Helpers.lerp(camOffsetX, 0, Global.spf * 10);
                camOffsetX = MathF.Round(camOffsetX);
            }

            Helpers.decrementTime(ref limboRACheckCooldown);
            Helpers.decrementTime(ref dropFlagCooldown);
            Helpers.decrementTime(ref parryCooldown);

            if (ownedByLocalPlayer && player.possessedTime > 0)
            {
                player.possesseeUpdate();
            }

            if (flag != null)
            {
                if (MathF.Abs(xPushVel) > 75) xPushVel = 75 * MathF.Sign(xPushVel);
                if (MathF.Abs(xSwingVel) > 75) xSwingVel = 75 * MathF.Sign(xSwingVel);
                if (vel.y < -350) vel.y = -350;

                // Used to prevent holding dash before taking flag from activating which is bad player experience
                if (!player.input.isHeld(Control.Dash, player))
                {
                    dropFlagUnlocked = true;
                }

                if (!canPickupFlag())
                {
                    if (Global.isHost || Global.serverClient == null)
                    {
                        dropFlag();
                    }
                }
                else if (dropFlagUnlocked && dropFlagCooldown == 0 && player.input.isHeld(Control.Dash, player))
                {
                    dropFlagProgress += Global.spf;
                    if (dropFlagProgress > 1)
                    {
                        dropFlagProgress = 0;
                        dropFlagCooldown = 1;
                        if (Global.isHost || Global.serverClient == null)
                        {
                            dropFlag();
                        }
                        RPC.actorToggle.sendRpc(netId, RPCActorToggleType.DropFlagManual);
                    }
                }
                else
                {
                    dropFlagProgress = 0;
                }
            }
            else
            {
                dropFlagProgress = 0;
                dropFlagUnlocked = false;
            }

            if (Global.level.gameMode.isTeamMode)
            {
                int alliance = player.alliance;
                // If this is an enemy disguised Axl, change the alliance
                if (player.alliance != Global.level.mainPlayer.alliance && player.isDisguisedAxl)
                {
                    alliance = Global.level.mainPlayer.alliance;
                }

                removeRenderEffect(RenderEffectType.BlueShadow);
                removeRenderEffect(RenderEffectType.RedShadow);

                if (alliance == GameMode.blueAlliance)
                {
                    addRenderEffect(RenderEffectType.BlueShadow);
                }
                else
                {
                    addRenderEffect(RenderEffectType.RedShadow);
                }
            }

            if (isInvisibleBS.getValue() == true)
            {
                if (player.isAxl && Global.shaderWrappers.ContainsKey("stealthmode_blue") && Global.shaderWrappers.ContainsKey("stealthmode_red"))
                {
                    if (Global.level.gameMode.isTeamMode && player.alliance == GameMode.redAlliance) addRenderEffect(RenderEffectType.StealthModeRed);
                    else addRenderEffect(RenderEffectType.StealthModeBlue);
                    removeRenderEffect(RenderEffectType.BlueShadow);
                    removeRenderEffect(RenderEffectType.RedShadow);
                }
                else
                {
                    addRenderEffect(RenderEffectType.Invisible);
                }
            }
            else
            {
                if (player.isAxl && Global.shaderWrappers.ContainsKey("stealthmode_blue") && Global.shaderWrappers.ContainsKey("stealthmode_red"))
                {
                    if (Global.level.gameMode.isTeamMode && player.alliance == GameMode.redAlliance) removeRenderEffect(RenderEffectType.StealthModeRed);
                    else removeRenderEffect(RenderEffectType.StealthModeBlue);
                }
                else
                {
                    removeRenderEffect(RenderEffectType.Invisible);
                }
            }

            if (Global.level.mainPlayer.readyTextOver)
            {
                Helpers.decrementTime(ref invulnTime);
            }

            if (vaccineTime > 0)
            {
                oilTime = 0;
                burnTime = 0;
                acidTime = 0;
                infectedTime = 0;
                vaccineTime -= Global.spf;
                if (vaccineTime <= 0)
                {
                    vaccineTime = 0;
                }
            }

            if (infectedTime > 0)
            {
                infectedTime -= Global.spf;
                if (infectedTime <= 0)
                {
                    infectedTime = 0;
                }
            }

            if (oilTime > 0)
            {
                oilTime -= Global.spf;
                if (isUnderwater() || charState.invincible || isCCImmune())
                {
                    oilTime = 0;
                }
                if (oilTime <= 0)
                {
                    oilTime = 0;
                }
            }

            if (acidTime > 0)
            {
                acidTime -= Global.spf;
                acidHurtCooldown += Global.spf;
                if (acidHurtCooldown > 1)
                {
                    acidHurtCooldown = 0;
                    acidDamager?.applyDamage(this, player.weapon is TunnelFang, new AcidBurst(), this, (int)ProjIds.AcidBurstPoison, overrideDamage: 1f);
                    new Anim(getCenterPos().addxy(0, -20), "torpedo_smoke", 1, null, true) { vel = new Point(0, -50) };
                }
                if (isUnderwater() || charState.invincible || isCCImmune())
                {
                    acidTime = 0;
                }
                if (acidTime <= 0)
                {
                    removeAcid();
                }
            }

            if (burnTime > 0)
            {
                burnTime -= Global.spf;
                burnHurtCooldown += Global.spf;
                burnEffectTime += Global.spf;
                if (burnEffectTime > 0.1f)
                {
                    burnEffectTime = 0;

                    Point burnPos = pos.addxy(0, -10);
                    bool hiding = false;
                    if (charState is InRideArmor inRideArmor)
                    {
                        if (inRideArmor.isHiding)
                        {
                            burnPos = pos.addxy(0, 0);
                            hiding = true;
                        }
                    }

                    var f1 = new Anim(burnPos.addRand(5, 10), "burn_flame", 1, null, true, host: this);
                    if (hiding) f1.setzIndex(zIndex - 100);

                    if (burnTime > 2)
                    {
                        var f2 = new Anim(burnPos.addRand(5, 10), "burn_flame", 1, null, true, host: this);
                        if (hiding) f2.setzIndex(zIndex - 100);
                    }
                    if (burnTime > 4)
                    {
                        var f3 = new Anim(burnPos.addRand(5, 10), "burn_flame", 1, null, true, host: this);
                        if (hiding) f3.setzIndex(zIndex - 100);
                    }
                    if (burnTime > 6)
                    {
                        var f4 = new Anim(burnPos.addRand(5, 10), "burn_flame", 1, null, true, host: this);
                        if (hiding) f4.setzIndex(zIndex - 100);
                    }
                }
                if (burnHurtCooldown > 1)
                {
                    burnHurtCooldown = 0;
                    burnDamager?.applyDamage(this, false, burnWeapon, this, (int)ProjIds.Burn, overrideDamage: 1f);
                }
                if (isUnderwater() || charState.invincible || isCCImmune())
                {
                    burnTime = 0;
                }
                if (charState is Frozen)
                {
                    burnTime = 0;
                }
                if (burnTime <= 0)
                {
                    removeBurn();
                }
            }

            if (flattenedTime > 0 && !(charState is Die))
            {
                flattenedTime -= Global.spf;
                if (flattenedTime < 0) flattenedTime = 0;
            }

            if (isHyperSigmaBS.getValue() || isHyperXBS.getValue())
            {
                flattenedTime = 0;
            }

            if (awakenedZeroTime > 0)
            {
                updateAwakenedZero();
            }
            Helpers.decrementTime(ref blackZeroTime);
            Helpers.decrementTime(ref slowdownTime);

            if (whiteAxlTime > 0)
            {
                whiteAxlTime -= Global.spf;
                if (whiteAxlTime < 0)
                {
                    whiteAxlTime = 0;
                    if (ownedByLocalPlayer)
                    {
                        player.weapons[0] = new AxlBullet();
                    }
                }
            }

            if (!ownedByLocalPlayer)
            {
                if (charState is VileRevive)
                {
                    charState.update();
                }
            }

            if (!ownedByLocalPlayer)
            {
                if (isCharging())
                {
                    chargeSound.play();
                    addRenderEffect(RenderEffectType.Flash, 0.05f, 0.1f);
                    chargeEffect.update(getChargeLevel());
                }
                else
                {
                    stopCharge();
                }
            }

            updateProjectileCooldown();

            igFreezeRecoveryCooldown += Global.spf;
            if (igFreezeRecoveryCooldown > 0.2f)
            {
                igFreezeRecoveryCooldown = 0;
                igFreezeProgress--;
                if (igFreezeProgress < 0) igFreezeProgress = 0;
            }
            Helpers.decrementTime(ref freezeInvulnTime);
            Helpers.decrementTime(ref stunInvulnTime);
            Helpers.decrementTime(ref crystalizeInvulnTime);
            Helpers.decrementTime(ref grabInvulnTime);
            Helpers.decrementTime(ref darkHoldInvulnTime);
            Helpers.decrementTime(ref barrierCooldown);

            if (flag != null && flag.ownedByLocalPlayer)
            {
                flag.changePos(getCenterPos());
            }

            if (!Global.level.hasGameObject(vileStartRideArmor))
            {
                vileStartRideArmor = null;
            }

            if (transformAnim != null)
            {
                transformSmokeTime += Global.spf;
                if (transformSmokeTime > 0)
                {
                    int width = 15;
                    int height = 25;
                    transformSmokeTime = 0;
                    new Anim(getCenterPos().addxy(Helpers.randomRange(-width, width), Helpers.randomRange(-height, height)), "torpedo_smoke", xDir, null, true);
                    new Anim(getCenterPos().addxy(Helpers.randomRange(-width, width), Helpers.randomRange(-height, height)), "torpedo_smoke", xDir, null, true);
                    new Anim(getCenterPos().addxy(Helpers.randomRange(-width, width), Helpers.randomRange(-height, height)), "torpedo_smoke", xDir, null, true);
                    new Anim(getCenterPos().addxy(Helpers.randomRange(-width, width), Helpers.randomRange(-height, height)), "torpedo_smoke", xDir, null, true);
                }

                transformAnim.changePos(pos);
                if (transformAnim.destroyed)
                {
                    transformAnim = null;
                }
            }

            if (stockedCharge)
            {
                addRenderEffect(RenderEffectType.StockedCharge, 0.05f, 0.1f);
            }

            if (stockedXSaber)
            {
                addRenderEffect(RenderEffectType.StockedSaber, 0.05f, 0.1f);
            }
            
            /*
            if (!ownedByLocalPlayer || player.isAI)
            {
                if (isInvisibleBS.getValue() && player.alliance != Global.level.mainPlayer.alliance)
                {
                    alpha -= Global.spf * 4;
                    if (alpha < 0) alpha = 0;
                    removeRenderEffect(RenderEffectType.StockedCharge);
                    removeRenderEffect(RenderEffectType.StockedSaber);
                }
                else
                {
                    alpha += Global.spf * 4;
                    if (alpha > 1) alpha = 1;
                }
            }
            */

            if (player.isZero && !Global.level.is1v1())
            {
                if (isBlackZero())
                {
                    if (musicSource == null)
                    {
                        addMusicSource("blackzero", getCenterPos(), true);
                    }
                }
                else
                {
                    destroyMusicSource();
                }
            }

            if (cStingPaletteTime > 5)
            {
                cStingPaletteTime = 0;
                cStingPaletteIndex++;
            }
            cStingPaletteTime++;

            if (headbuttAirTime > 0)
            {
                headbuttAirTime += Global.spf;
            }

            // Cutoff point for things that run but aren't owned by the player
            if (!ownedByLocalPlayer)
            {
                base.update();
                Helpers.decrementTime(ref barrierTime);

                if (isNonOwnerRev)
                {
                    iceGattlingSound.play();
                    revTime += Global.spf;
                    if (revTime > 1) revTime = 1;
                }
                else
                {
                    if (!iceGattlingSound.destroyed)
                    {
                        iceGattlingSound.stopRev(revTime);
                    }
                    Helpers.decrementTime(ref revTime);
                }

                if (netNonOwnerScopeEndPos != null)
                {
                    var incPos = netNonOwnerScopeEndPos.Value.subtract(nonOwnerScopeEndPos).times(1f / Global.tickRate);
                    var framePos = nonOwnerScopeEndPos.add(incPos);
                    if (nonOwnerScopeEndPos.distanceTo(framePos) > 0.001f)
                    {
                        nonOwnerScopeEndPos = framePos;
                    }
                }

                if (isAwakenedZeroBS.getValue())
                {
                    updateAwakenedAura();
                }

                if (sprite.name.Contains("sigma2_viral"))
                {
                    if (!viralOnce)
                    {
                        viralOnce = true;
                        xScale = 0;
                        yScale = 0;
                        originalZIndex = zIndex;
                    }

                    if (sprite.name.Contains("sigma2_viral_possess"))
                    {
                        setzIndex(ZIndex.Actor);
                    }
                    else
                    {
                        setzIndex(originalZIndex);
                    }
                }

                return;
            }

            if (player.isVile && !player.isAI && !player.isDisguisedAxl && player.getVileWeightActive() > VileLoadout.maxWeight && charState is not WarpIn && charState is not Die)
            {
                applyDamage(null, null, Damager.envKillDamage, null);
                return;
            }

            updateParasite();
            updateBarrier();

            if (beeSwarm != null)
            {
                beeSwarm.update();
            }

            if (stingChargeTime > 0)
            {
                hadoukenCooldownTime = maxHadoukenCooldownTime;
                shoryukenCooldownTime = maxShoryukenCooldownTime;

                if (player.isX)
                {
                    stingChargeTime -= Global.spf;
                    
                    player.weapon.ammo -= (Global.spf * 3 * (player.hasChip(3) ? 0.5f : 1));
                    if (player.weapon.ammo < 0) player.weapon.ammo = 0;
                    stingChargeTime = player.weapon.ammo;
                }
                else
                {
                    stingChargeTime -= Global.spf;
                }
                if (stingChargeTime <= 0)
                {
                    player.delaySubtank();
                    stingChargeTime = 0;
                }
            }

            if (pos.y > Global.level.killY && !isWarpIn() && charState is not WarpOut)
            {
                if (charState is WolfSigmaRevive wsr)
                {
                    stopMoving();
                    useGravity = false;
                    wsr.groundStart = true;
                }
                else
                {
                    if (charState is not Die)
                    {
                        incPos(new Point(0, 25));
                    }
                    applyDamage(null, null, Damager.envKillDamage, null);
                }
            }

            if (player.health >= player.maxHealth)
            {
                healAmount = 0;
                usedSubtank = null;
            }
            if (healAmount > 0 && player.health > 0)
            {
                healTime += Global.spf;
                if (isHyperSigma) healTime += Global.spf;
                if (healTime > 0.05)
                {
                    healTime = 0;
                    healAmount--;
                    if (usedSubtank != null)
                    {
                        usedSubtank.health--;
                    }
                    player.health = Helpers.clampMax(player.health + player.getHealthModifier(), player.maxHealth);
                    if (acidTime > 0)
                    {
                        acidTime--;
                        if (acidTime < 0) removeAcid();
                    }
                    if (player == Global.level.mainPlayer || playHealSound)
                    {
                        playSound("heal", forcePlay: true, sendRpc: true);
                    }
                }
            }
            else
            {
                playHealSound = false;
            }

            if (usedSubtank != null && usedSubtank.health <= 0)
            {
                usedSubtank = null;
            }

            if (ai != null)
            {
                ai.update();
            }

            if (player.isZero)
            {
                updateZero();
            }

            if (player.isSigma)
            {
                updateSigma();
            }

            if (slideVel != 0)
            {
                slideVel = Helpers.toZero(slideVel, Global.spf * 350, Math.Sign(slideVel));
                move(new Point(slideVel, 0), true);
            }

            charState.update();
            base.update();

            if (player.isDisguisedAxl)
            {
                updateDisguisedAxl();
            }

            if (player.isX)
            {
                updateX();
            }
            else if (player.isVile)
            {
                updateVile();
            }
            else if (player.isAxl)
            {
                updateAxl();
            }
        }

        public void removeAcid()
        {
            acidTime = 0;
            acidHurtCooldown = 0;
        }

        public void removeBurn()
        {
            burnTime = 0;
            burnHurtCooldown = 0;
        }

        public bool canEnterRideArmor()
        {
            return charState is Fall fall && rideArmor == null && rideChaser == null && !isVileMK5 && fall.limboVehicleCheckTime == 0;
        }

        public bool canEnterRideChaser()
        {
            return charState is Fall fall && rideArmor == null && rideChaser == null && fall.limboVehicleCheckTime == 0;
        }

        public bool isSpawning()
        {
            return sprite.name.Contains("warp_in") || !visible || (player.isVile && isInvulnBS.getValue());
        }

        public Point getCharRideArmorPos()
        {
            if (rideArmor.currentFrame.POIs.Count == 0) return new Point();
            var charPos = rideArmor.currentFrame.POIs[0];
            charPos.x *= xDir;
            return charPos;
        }

        public Point getMK5RideArmorPos()
        {
            if (mk5RideArmorPlatform.currentFrame.POIs.Count == 0) return new Point();
            var charPos = mk5RideArmorPlatform.currentFrame.POIs[0];
            charPos.x *= xDir;
            return charPos;
        }

        public bool isSpriteDash(string spriteName)
        {
            return spriteName.Contains("dash") && !spriteName.Contains("up_dash");
        }

        public override void changeSprite(string spriteName, bool resetFrame)
        {
            cannonAimNum = 0;
            if (sprite != null && sprite.name != "zero_attack" && spriteName == "zero_attack")
            {
                zero3SwingComboStartTime = Global.time;
            }
            if (sprite != null && sprite.name != "zero_attack3" && spriteName == "zero_attack3")
            {
                zero3SwingComboEndTime = Global.time;
            }
            if (!isHeadbuttSprite(sprite?.name) && isHeadbuttSprite(spriteName))
            {
                headbuttAirTime = Global.spf;
            }
            if (isHeadbuttSprite(sprite?.name) && !isHeadbuttSprite(spriteName))
            {
                headbuttAirTime  = 0;
            }

            base.changeSprite(spriteName, resetFrame);
        }

        public bool isHeadbuttSprite(string sprite)
        {
            if (sprite == null) return false;
            return sprite.EndsWith("jump") || sprite.EndsWith("up_dash") || sprite.EndsWith("wall_kick");
        }

        public void unfreezeIfFrozen()
        {
            if (charState is Frozen)
            {
                changeState(new Idle(), true);
            }
        }

        public void freeze(int timeToFreeze = 5)
        {
            if (chargedRollingShieldProj != null) return;
            if (charState is SwordBlock) return;
            if (charState is Frozen) return;

            changeState(new Frozen(timeToFreeze), true);
        }

        public bool canCrystalize()
        {
            if (chargedRollingShieldProj != null) return false;
            if (charState is SwordBlock) return false;
            if (isCrystalized) return false;
            return true;
        }

        public void chargeLogic()
        {
            chargeEffect.stop();

            if (isCharging())
            {
                chargeSound.play();
                if (!sprite.name.Contains("ra_hide"))
                {
                    addRenderEffect(RenderEffectType.Flash, 0.05f, 0.1f);
                }
                chargeEffect.update(getChargeLevel());
            }
        }

        public bool isCCImmune()
        {
            //if (isAwakenedZeroBS.getValue() && isAwakenedGenmuZeroBS.getValue()) return true;
            //if (isHyperSigmaBS.getValue()) return true;
            //return false;
            return isCCImmuneHyperMode();
        }

        public bool isCCImmuneHyperMode()
        {
            // The former two hyper modes rely on a float time value sync.
            // The latter two hyper modes are boolean states so use the BoolState ("BS") system.
            return isAwakenedGenmuZeroBS.getValue() || (isInvisibleBS.getValue() && player.isAxl) || isHyperSigmaBS.getValue() || isHyperXBS.getValue();
        }

        public bool isToughGuyHyperMode()
        {
            return isBlackZero() || isWhiteAxl();
        }

        public bool isImmuneToKnockback()
        {
            return charState?.immuneToWind == true || immuneToKnockback || isCCImmune();
        }

        public bool isNonCCImmuneHyperMode()
        {
            return sprite.name.Contains("vilemk2") || player.hasGoldenArmor() || hasUltimateArmorBS.getValue();
        }

        // If factorHyperMode = true, then invuln frames in a hyper mode won't count as "invulnerable".
        // This is to allow the hyper mode start invulnerability to still be able to do things without being impeded
        // and should be set only by code that is checking to see if such things can be done.
        public bool isInvulnerable(bool ignoreRideArmorHide = false, bool factorHyperMode = false)
        {
            if (isWarpIn()) return true;
            if (!factorHyperMode && isInvulnBS.getValue()) return true;
            if (factorHyperMode && isInvulnBS.getValue() && !isCCImmuneHyperMode()) return true;
            if (!ignoreRideArmorHide && charState is InRideArmor && (charState as InRideArmor).isHiding) return true;
            if (!ignoreRideArmorHide && !string.IsNullOrEmpty(sprite?.name) && sprite.name.Contains("ra_hide")) return true;
            if (sprite.name == "axl_roll") return true;
            if (sprite.name.Contains("viral_exit")) return true;
            if (charState is WarpOut) return true;
            if (charState is WolfSigmaRevive || charState is ViralSigmaRevive || charState is KaiserSigmaRevive) return true;
            return false;
        }

        public bool isWarpIn()
        {
            return charState is WarpIn || sprite.name.EndsWith("warp_in");
        }

        public bool isWarpOut()
        {
            return charState is WarpOut || sprite.name.EndsWith("warp_beam");
        }

        public bool isInvulnerableAttack()
        {
            return isInvulnerable(factorHyperMode: true);
        }

        public bool isSpriteInvulnerable()
        {
            return sprite.name == "mmx_gigacrush" || sprite.name == "zero_hyper_start" || sprite.name == "axl_hyper_start" || sprite.name == "zero_rakuhouha" ||
                sprite.name == "zero_rekkoha" || sprite.name == "zero_cflasher" || sprite.name.Contains("vile_revive") || sprite.name.Contains("warp_out") || sprite.name.Contains("nova_strike");
        }

        public bool isCStingVulnerable(int projId)
        {
            return isInvisibleBS.getValue() && player.isX && Damager.isBoomerang(projId);
        }

        public bool canBeGrabbed()
        {
            return grabInvulnTime == 0 && !isCCImmune() && charState is not DarkHoldState;
        }

        public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId)
        {
            if (isInvulnerable()) return false;
            if (isDeathOrReviveSprite()) return false;
            if (Global.level.gameMode.setupTime > 0) return false;
            if (Global.level.isRace())
            {
                bool isAxlSelfDamage = player.isAxl && damagerAlliance == player.alliance;
                if (!isAxlSelfDamage) return false;
            }

            // Bommerang can go thru invisibility check
            if (player.alliance != damagerAlliance && projId != null && isCStingVulnerable(projId.Value))
            {
                return true;
            }

            if (isInvisibleBS.getValue()) return false;

            // Self damaging projIds can go thru alliance check
            bool isSelfDamaging = 
                projId == (int)ProjIds.GLauncherSplash || 
                projId == (int)ProjIds.ExplosionSplash || 
                projId == (int)ProjIds.NecroBurst || 
                projId == (int)ProjIds.SniperMissileBlast ||
                projId == (int)ProjIds.SpeedBurnerRecoil;

            if (isSelfDamaging && damagerPlayerId == player.id)
            {
                return true;
            }

            if (player.alliance == damagerAlliance) return false;

            return true;
        }

        public bool isDeathOrReviveSprite()
        {
            if (sprite.name == "sigma_head_intro") return true;
            if (sprite.name.EndsWith("die")) return true;
            if (sprite.name.Contains("revive")) return true;
            return false;
        }

        public bool isInvincible(Player attacker, int? projId)
        {
            if (ownedByLocalPlayer)
            {
                return charState.invincible || genmuImmune(attacker);
            }
            else
            {
                return isSpriteInvulnerable() || genmuImmune(attacker);
            }
        }

        public int getShootXDirSynced()
        {
            int xDir = this.xDir;
            if (sprite.name.Contains("_wall_slide")) xDir *= -1;
            return xDir;
        }

        public int getShootXDir()
        {
            int xDir = this.xDir;
            if (charState is WallSlide) xDir *= -1;
            return xDir;
        }

        public bool isStealthy(int alliance)
        {
            if (player.alliance == alliance) return false;
            if (isInvisibleBS.getValue()) return true;
            if (player.isDisguisedAxl) return true;
            return false;
        }


        public Point getDashSparkEffectPos(int xDir)
        {
            return getDashDustEffectPos(xDir).addxy(6 * xDir, 4);
        }

        public Point getDashDustEffectPos(int xDir)
        {
            float dashXPos = -24;
            if (player.isVile) dashXPos = -30;
            if (player.isSigma1AndSigma()) dashXPos = -35;
            if (player.isSigma2AndSigma()) dashXPos = -35;
            if (player.isSigma3AndSigma()) dashXPos = -35;
            return pos.addxy(dashXPos * xDir, -4);
        }

        public override Point getCenterPos()
        {
            if (player.isSigma)
            {
                if (player.isWolfSigma()) return pos.addxy(0, -7);
                else if (player.isViralSigma()) return pos.addxy(0, 0);
                else if (player.isKaiserSigma())
                {
                    if (sprite.name.StartsWith("sigma3_kaiser_virus")) return pos.addxy(0, -16);
                    else return pos.addxy(0, -60);
                }
                return pos.addxy(0, -32);
            }
            return pos.addxy(0, -18);
        }

        public Point getAimCenterPos()
        {
            if (sprite.name.Contains("_ra_"))
            {
                return pos.addxy(0, -10);
            }
            if (player.isZero) return pos.addxy(0, -21);
            if (player.isVile) return pos.addxy(0, -24);
            if (player.isSigma)
            {
                if (player.isKaiserSigma() && !player.isKaiserViralSigma())
                {
                    return pos.addxy(13 * xDir, -95);
                }
                return getCenterPos();
            }
            return pos.addxy(0, -18);
        }

        public Point getParasitePos()
        {
            float yOff = -18;
            if (sprite.name.Contains("_ra_"))
            {
                float hideY = 0;
                if (sprite.name.Contains("_ra_hide"))
                {
                    hideY = 22 * ((float)sprite.frameIndex / sprite.frames.Count);
                }
                yOff = -6 + hideY;
            }
            else if (player.isZero) yOff = -20;
            else if (player.isVile) yOff = -24;
            if (player.isSigma)
            {
                return getCenterPos();
            }
            else if (player.isAxl) yOff = -18;

            return pos.addxy(0, yOff);
        }

        public float camOffsetX;
        public Point getCamCenterPos(bool ignoreZoom = false)
        {
            if (player.isVile && mk5RideArmorPlatform != null)
            {
                return mk5RideArmorPlatform.pos.addxy(0, -70);
            }
            if (player.isSigma)
            {
                var maverick = player.currentMaverick;
                if (maverick != null && player.isTagTeam())
                {
                    if (maverick.state is MEnter me)
                    {
                        return me.getDestPos().addxy(camOffsetX, -24);
                    }
                    if (maverick.state is MorphMCHangState hangState)
                    {
                        return maverick.pos.addxy(camOffsetX, -24 + 17);
                    }
                    return maverick.pos.addxy(camOffsetX, -24);
                }

                if (player.isViralSigma())
                {
                    return pos.addxy(camOffsetX, 25);
                }

                if (player.isKaiserSigma())
                {
                    if (sprite.name.StartsWith("sigma3_kaiser_virus")) return pos.addxy(camOffsetX, -12);
                    return pos.addxy(camOffsetX, -55);
                }

                if (player.weapon is WolfSigmaHandWeapon handWeapon && handWeapon.hand.isControlling)
                {
                    var hand = handWeapon.hand;
                    Point camCenter = sigmaHeadGroundCamCenterPos.Value;
                    if (hand.pos.x > camCenter.x + Global.halfScreenW || hand.pos.x < camCenter.x - Global.halfScreenW || hand.pos.y > camCenter.y + Global.halfScreenH || hand.pos.y < camCenter.y - Global.halfScreenH)
                    {
                        float overFactorX = MathF.Abs(hand.pos.x - camCenter.x) - Global.halfScreenW;
                        if (overFactorX > 0)
                        {
                            float remainder = overFactorX - Global.halfScreenW;
                            int sign = MathF.Sign(hand.pos.x - camCenter.x);
                            camCenter.x += Math.Min(overFactorX, Global.halfScreenW) * sign * 2;
                            camCenter.x += Math.Max(remainder, 0) * sign;
                        }

                        float overFactorY = MathF.Abs(hand.pos.y - camCenter.y) - Global.halfScreenH;
                        if (overFactorY > 0)
                        {
                            float remainder = overFactorY - Global.halfScreenH;
                            int sign = MathF.Sign(hand.pos.y - camCenter.y);
                            camCenter.y += Math.Min(overFactorY, Global.halfScreenH) * sign * 2;
                            camCenter.y += Math.Max(remainder, 0) * sign;
                        }

                        return camCenter;
                    }
                }

                if (sigmaHeadGroundCamCenterPos != null)
                {
                    return sigmaHeadGroundCamCenterPos.Value;
                }
            }
            if (sniperMissileProj != null)
            {
                return sniperMissileProj.getCenterPos();
            }
            if (rideArmor != null)
            {
                if (ownedByLocalPlayer && rideArmor.rideArmorState is RADropIn)
                {
                    return (rideArmor.rideArmorState as RADropIn).spawnPos.addxy(0, -24);
                }
                return rideArmor.pos.addxy(camOffsetX, -24);
            }
            if (isZooming() && !ignoreZoom)
            {
                return player.axlScopeCursorWorldPos;
            }
            return pos.addxy(camOffsetX, -24);
        }

        public Point? getHeadPos()
        {
            if (currentFrame?.headPos == null) return null;
            return pos.addxy(currentFrame.headPos.Value.x * xDir, currentFrame.headPos.Value.y - 2);
        }

        public Rect getHeadRect()
        {
            Point headPos = getHeadPos().Value;
            float topY = float.MaxValue;
            float leftX = float.MaxValue;
            float rightX = float.MinValue;
            if (collider != null)
            {
                topY = collider.shape.getRect().y1 - 1;
                //leftX = collider.shape.getRect().x1 - 1;
                //rightX = collider.shape.getRect().x2 + 1;
            }

            return new Rect(
                Math.Min(leftX, headPos.x - headshotRadius),
                Math.Min(topY, headPos.y - headshotRadius),
                Math.Max(rightX, headPos.x + headshotRadius),
                headPos.y + headshotRadius
            );
        }

        public Actor abstractedActor()
        {
            if (rideArmor != null) return rideArmor;
            return this;
        }

        public void setFall()
        {
            changeState(new Fall());
        }

        public bool isClimbingLadder()
        {
            return charState is LadderClimb;
        }

        public void addAI()
        {
            ai = new AI(this);
        }

        public void drawCharge()
        {
        }

        public bool isCharging()
        {
            return chargeTime >= charge1Time;
        }

        public Point getShootPos()
        {
            var busterOffsetPos = currentFrame.getBusterOffset();
            if (busterOffsetPos == null)
            {
                return getCenterPos();
            }
            var busterOffset = (Point)busterOffsetPos;
            if (player.isX && player.armArmorNum == 3 && sprite.needsX3BusterCorrection())
            {
                if (busterOffset.x > 0) busterOffset.x += 4;
                else if (busterOffset.x < 0) busterOffset.x -= 4;
            }
            busterOffset.x *= xDir;
            if (player.weapon is RollingShield && charState is Dash) 
            {
                busterOffset.y -= 2;
            }
            return pos.add(busterOffset);
        }

        public void stopCharge()
        {
            if (chargeEffect == null) return;
            chargeEffect.reset();
            chargeTime = 0;
            chargeFlashTime = 0;
            chargeSound.stop();
            chargeSound.reset();
            chargeEffect.stop();
        }

        public string getSprite(string spriteName)
        {
            if (player.isAxl)
            {
                if (spriteName == "crystalized" || spriteName == "die" || spriteName == "hurt" || spriteName == "hyper_start" || spriteName == "hyper_start_air" || spriteName == "knocked_down" || spriteName == "roll" || spriteName == "warp_in" || spriteName == "win")
                {
                    if (player.axlBulletType == 1) spriteName += "_mc";
                    if (player.axlBulletType == 2) spriteName += "_bk";
                    if (player.axlBulletType == 3) spriteName += "_mb";
                    if (player.axlBulletType == 5) spriteName += "_rb";
                    if (player.axlBulletType == 6) spriteName += "_ag";
                }

                return "axl_" + spriteName;
            }
            else if (player.isVile)
            {
                if (isVileMK5) return "vilemk5_" + spriteName;
                if (isVileMK2) return "vilemk2_" + spriteName;
                return "vile_" + spriteName;
            }
            else if (player.isZero) return "zero_" + spriteName;
            else if (player.isSigma)
            {
                if (player.loadout.sigmaLoadout.sigmaForm == 0) return "sigma_" + spriteName;
                else if (player.loadout.sigmaLoadout.sigmaForm == 1) return "sigma2_" + spriteName;
                else return "sigma3_" + spriteName;
            }
            else return "mmx_" + spriteName;
        }

        public void changeSpriteFromName(string spriteName, bool resetFrame)
        {
            changeSprite(getSprite(spriteName), resetFrame);
        }

        public void changeSpriteFromNameIfDifferent(string spriteName, bool resetFrame)
        {
            string realSpriteName = getSprite(spriteName);
            if (sprite?.name != realSpriteName)
            {
                changeSprite(realSpriteName, resetFrame);
            }
        }

        public int getChargeLevel() 
        {
            bool clampTo3 = true;
            if (player.isZero) clampTo3 = !canUseDoubleBusterCombo();
            if (player.isVile) clampTo3 = !isVileMK5;
            if (player.isX) clampTo3 = !isHyperX;

            if (chargeTime < charge1Time)
            {
                return 0;
            }
            else if (chargeTime >= charge1Time && chargeTime < charge2Time)
            {
                return 1;
            }
            else if (chargeTime >= charge2Time && chargeTime < charge3Time)
            {
                return 2;
            }
            else if (chargeTime >= charge3Time && chargeTime < charge4Time)
            {
                return 3;
            }
            else if (chargeTime >= charge4Time)
            {
                return clampTo3 ? 3 : 4;
            }
            return -1;
        }

        public void changeToIdleOrFall()
        {
            if (grounded) changeState(new Idle(), true);
            else
            {
                if (charState.wasVileHovering && canVileHover()) changeState(new VileHover(), true);
                else changeState(new Fall(), true);
            }
        }

        public void changeState(CharState newState, bool forceChange = false)
        {
            if (charState != null && newState != null && charState.GetType() == newState.GetType()) return;
            if (changedStateInFrame && !forceChange) return;

            if (charState is InRideArmor && newState is Frozen)
            {
                (charState as InRideArmor).freeze((newState as Frozen).freezeTime);
                return;
            }
            else if (charState is InRideArmor && newState is Stunned)
            {
                (charState as InRideArmor).stun((newState as Stunned).stunTime);
                return;
            }
            else if (charState is InRideArmor && newState is Crystalized)
            {
                (charState as InRideArmor).crystalize((newState as Crystalized).crystalizedTime);
                return;
            }

            if (charState != null && !charState.canExit(this, newState)) return;
            if (newState != null && !newState.canEnter(this)) return;

            changedStateInFrame = true;
            newState.character = this;

            if (hasBusterProj() && string.IsNullOrEmpty(newState.shootSprite) && !(newState is Hurt))
            {
                destroyBusterProjs();
            }

            if (shootAnimTime == 0 || !newState.canShoot())
            {
                changeSprite(getSprite(newState.sprite), true);
            }
            else
            {
                changeSprite(getSprite(newState.shootSprite), true);
            }
            var oldState = charState;
            if (oldState != null) oldState.onExit(newState);
            charState = newState;
            newState.onEnter(oldState);
            
            if (gaeaShield != null && shouldDrawArm() == false)
            {
                gaeaShield.destroySelf();
                gaeaShield = null;
            }

            if (!newState.canShoot())
            {
                //this.shootTime = 0;
                //this.shootAnimTime = 0;
            }
        }

        float hyperChargeAnimTime;
        float hyperChargeAnimTime2 = 0.125f;
        const float maxHyperChargeAnimTime = 0.25f;

        // Get dist from y pos to pos at which to draw the first label
        public float getLabelOffY()
        {
            float offY = 42;
            if (player.isZero) offY = 45;
            if (player.isVile) offY = 50;
            if (player.isSigma) offY = 62;
            if (sprite.name.Contains("_ra_")) offY = 25;
            if (player.isMainPlayer && player.isTagTeam() && player.currentMaverick != null)
            {
                offY = player.currentMaverick.getLabelOffY();
            }
            if (player.isWolfSigma()) offY = 25;
            if (player.isViralSigma()) offY = 43;
            if (player.isKaiserSigma()) offY = 125;
            if (player.isKaiserViralSigma()) offY = 60;

            return offY;
        }

        public override void render(float x, float y)
        {
            currentLabelY = -getLabelOffY();

            if (isNightmareZeroBS.getValue()) addRenderEffect(RenderEffectType.Trail);
            else removeRenderEffect(RenderEffectType.Trail);

            if (player.isVile && isSpeedDevilActiveBS.getValue()) addRenderEffect(RenderEffectType.SpeedDevilTrail);
            else removeRenderEffect(RenderEffectType.SpeedDevilTrail);

            if (player.isZero && isAwakenedZeroBS.getValue() && visible)
            {
                float xOff = 0;
                int auraXDir = 1;
                float yOff = 5;
                string auraSprite = "zero_awakened_aura";
                if (sprite.name.Contains("dash"))
                {
                    auraSprite = "zero_awakened_aura2";
                    auraXDir = xDir;
                    yOff = 8;
                }
                var shaders = new List<ShaderWrapper>();
                if (isAwakenedGenmuZeroBS.getValue() && Global.frameCount % Global.normalizeFrames(6) > Global.normalizeFrames(3) && Global.shaderWrappers.ContainsKey("awakened"))
                {
                    shaders.Add(Global.shaderWrappers["awakened"]);
                }
                Global.sprites[auraSprite].draw(awakenedAuraFrame, pos.x + x + (xOff * auraXDir), pos.y + y + yOff, auraXDir, 1, null, 1, 1, 1, zIndex - 1, shaders: shaders);
            }

            if (player.isX && isHyperChargeActiveBS.getValue() && visible)
            {
                addRenderEffect(RenderEffectType.Flash, 0.05f, 0.1f);
                
                hyperChargeAnimTime += Global.spf;
                if (hyperChargeAnimTime >= maxHyperChargeAnimTime) hyperChargeAnimTime = 0;
                float sx = pos.x + x;
                float sy = pos.y + y - 18;

                var sprite1 = Global.sprites["hypercharge_part_1"];
                float distFromCenter = 12;
                float posOffset = hyperChargeAnimTime * 50;
                int hyperChargeAnimFrame = MathF.Floor((hyperChargeAnimTime / maxHyperChargeAnimTime) * sprite1.frames.Count);
                sprite1.draw(hyperChargeAnimFrame, sx + distFromCenter + posOffset, sy, 1, 1, null, 1, 1, 1, zIndex + 1);
                sprite1.draw(hyperChargeAnimFrame, sx - distFromCenter - posOffset, sy, 1, 1, null, 1, 1, 1, zIndex + 1);
                sprite1.draw(hyperChargeAnimFrame, sx, sy + distFromCenter + posOffset, 1, 1, null, 1, 1, 1, zIndex + 1);
                sprite1.draw(hyperChargeAnimFrame, sx, sy - distFromCenter - posOffset, 1, 1, null, 1, 1, 1, zIndex + 1);

                hyperChargeAnimTime2 += Global.spf;
                if (hyperChargeAnimTime2 >= maxHyperChargeAnimTime) hyperChargeAnimTime2 = 0;
                var sprite2 = Global.sprites["hypercharge_part_2"];
                float distFromCenter2 = 12;
                float posOffset2 = hyperChargeAnimTime2 * 50;
                int hyperChargeAnimFrame2 = MathF.Floor((hyperChargeAnimTime2 / maxHyperChargeAnimTime) * sprite2.frames.Count);
                float xOff = Helpers.cosd(45) * (distFromCenter2 + posOffset2);
                float yOff = Helpers.sind(45) * (distFromCenter2 + posOffset2);
                sprite2.draw(hyperChargeAnimFrame2, sx - xOff, sy + yOff, 1, 1, null, 1, 1, 1, zIndex + 1);
                sprite2.draw(hyperChargeAnimFrame2, sx + xOff, sy - yOff, 1, 1, null, 1, 1, 1, zIndex + 1);
                sprite2.draw(hyperChargeAnimFrame2, sx + xOff, sy - yOff, 1, 1, null, 1, 1, 1, zIndex + 1);
                sprite2.draw(hyperChargeAnimFrame2, sx - xOff, sy - yOff, 1, 1, null, 1, 1, 1, zIndex + 1);
            }

            if (player.isVile && currentFrame?.POIs?.Count > 0)
            {
                Sprite cannonSprite = getCannonSprite(out Point poiPos, out int zIndexDir);
                cannonSprite?.draw(cannonAimNum, poiPos.x, poiPos.y, getShootXDirSynced(), 1, getRenderEffectSet(), alpha, 1, 1, zIndex + zIndexDir, getShaders(), actor: this);
            }

            if (player.isSigma && visible)
            {
                string kaiserBodySprite = "";
                if (sprite.name.EndsWith("kaiser_idle")) kaiserBodySprite = sprite.name + "_body";
                if (sprite.name.EndsWith("kaiser_hover")) kaiserBodySprite = sprite.name + "_body";
                if (sprite.name.EndsWith("kaiser_fall")) kaiserBodySprite = sprite.name + "_body";
                if (sprite.name.EndsWith("kaiser_shoot")) kaiserBodySprite = sprite.name + "_body";
                if (sprite.name.EndsWith("kaiser_shoot2")) kaiserBodySprite = sprite.name + "_body";
                if (sprite.name.EndsWith("kaiser_taunt")) kaiserBodySprite = sprite.name + "_body";
                if (kaiserBodySprite != "")
                {
                    Global.sprites[kaiserBodySprite].draw(0, pos.x + x, pos.y + y, xDir, 1, null, 1, 1, 1, zIndex - 10);
                }
            }

            if (rideArmor == null && rideChaser == null && mk5RideArmorPlatform == null)
            {
                base.render(x, y);
            }
            else if (mk5RideArmorPlatform != null)
            {
                var rideArmorPos = mk5RideArmorPlatform.pos;
                var charPos = getMK5RideArmorPos();
                base.render(rideArmorPos.x + charPos.x - pos.x, rideArmorPos.y + charPos.y - pos.y);
            }
            else if (rideArmor != null)
            {
                var rideArmorPos = rideArmor.pos;
                var charPos = getCharRideArmorPos();
                base.render(rideArmorPos.x + charPos.x - pos.x, rideArmorPos.y + charPos.y - pos.y);
            }
            else if (rideChaser != null)
            {
                var rideChaserPos = rideChaser.pos;
                base.render(rideChaserPos.x - pos.x, rideChaserPos.y - pos.y);
            }

            if (charState != null)
            {
                charState.render(x, y);
            }

            if (chargeEffect != null)
            {
                chargeEffect.render(getParasitePos().add(new Point(x, y)));
            }

            if (player.isX && sprite.name.Contains("frozen"))
            {
                Global.sprites["frozen_block"].draw(0, pos.x + x - (xDir * 2), pos.y + y + 1, xDir, 1, null, 1, 1, 1, zIndex + 1);
            }

            if (isCrystalized)
            {
                float yOff = 0;
                if (sprite.name.Contains("ra_idle")) yOff = 12;
                if (player.isSigma) yOff = -7;
                Global.sprites["crystalhunter_crystal"].draw(0, pos.x + x, pos.y + y + yOff, xDir, 1, null, 1, 1, 1, zIndex + 1);
            }

            if (isShootingRaySplasher)
            {
                var shootPos = getShootPos();
                var muzzleFrameCount = Global.sprites["raysplasher_muzzle"].frames.Count;
                Global.sprites["raysplasher_muzzle"].draw(Global.frameCount % muzzleFrameCount, shootPos.x + x + (3 * xDir), shootPos.y + y, 1, 1, null, 1, 1, 1, zIndex);
            }

            List<Player> nonSpecPlayers = Global.level.nonSpecPlayers();
            bool drawCursorChar = player.isMainPlayer && (Global.level.is1v1() || Global.level.server.fixedCamera) && !isHyperSigmaBS.getValue();
            if (Global.level.mainPlayer.isSpectator && player == Global.level.specPlayer)
            {
                drawCursorChar = true;
            }
            if (Global.overrideDrawCursorChar) drawCursorChar = true;
             
            if (!isWarpIn() && drawCursorChar && player.currentMaverick == null)
            {
                Global.sprites["cursorchar"].draw(0, pos.x + x, pos.y + y + currentLabelY, 1, 1, null, 1, 1, 1, zIndex + 1);
                deductLabelY(labelCursorOffY);
            }

            bool shouldDrawName = false;
            bool shouldDrawHealthBar = false;
            string overrideName = null;
            Color? overrideColor = null;
            Color? overrideTextColor = null;

            if (!hideHealthAndName())
            {
                if (Global.level.mainPlayer.isSpectator)
                {
                    shouldDrawName = true;
                    shouldDrawHealthBar = true;
                }
                else if (Global.level.is1v1())
                {
                    if (!player.isMainPlayer && player.alliance == Global.level.mainPlayer.alliance)
                    {
                        shouldDrawName = true;
                    }
                }
                // Special case: puppeteer control, draw sigma health
                else if (player.isMainPlayer && player.isSigma && player.isPuppeteer() && player.currentMaverick != null)
                {
                    shouldDrawHealthBar = true;
                }
                // Special case: labeling the own player's disguised Axl
                else if (player.isMainPlayer && player.isDisguisedAxl && Global.level.gameMode.isTeamMode)
                {
                    overrideName = player.disguise.targetName;
                    overrideColor = player.alliance == GameMode.blueAlliance ? Helpers.DarkRed : Helpers.DarkBlue;
                    shouldDrawName = true;
                }
                // Special case: labeling an enemy player's disguised Axl
                else if (!player.isMainPlayer && player.isDisguisedAxl && Global.level.gameMode.isTeamMode && player.alliance != Global.level.mainPlayer.alliance)
                {
                    overrideName = player.disguise.targetName;
                    overrideColor = player.alliance == GameMode.blueAlliance ? Helpers.DarkRed : Helpers.DarkBlue;
                    shouldDrawName = true;
                    shouldDrawHealthBar = true;
                }
                // Special case: drawing enemy team name/health as disguised Axl
                else if (!player.isMainPlayer && Global.level.mainPlayer.isDisguisedAxl && Global.level.gameMode.isTeamMode && player.alliance != Global.level.mainPlayer.alliance && !isStealthy(Global.level.mainPlayer.alliance))
                {
                    shouldDrawName = true;
                    shouldDrawHealthBar = true;
                }
                // Basic case, drawing alliance of teammates in team modes
                else if (!player.isMainPlayer && player.alliance == Global.level.mainPlayer.alliance && Global.level.gameMode.isTeamMode)
                {
                    shouldDrawName = true;
                    shouldDrawHealthBar = true;
                }
                // X with scan
                else if (!player.isMainPlayer && Global.level.mainPlayer.isX && Global.level.mainPlayer.hasHelmetArmor(2) && player.scanned && !isStealthy(Global.level.mainPlayer.alliance))
                {
                    overrideTextColor = Color.Red;
                    overrideColor = Helpers.DarkBlue;
                    shouldDrawName = true;
                    shouldDrawHealthBar = true;
                }
                // Axl target
                else if (!player.isMainPlayer && Global.level.mainPlayer.isAxl && Global.level.mainPlayer.character?.axlCursorTarget == this && !isStealthy(Global.level.mainPlayer.alliance))
                {
                    shouldDrawHealthBar = true;
                }
            }

            if (shouldDrawHealthBar || Global.overrideDrawHealth)
            {
                drawHealthBar();
            }
            if (shouldDrawName || Global.overrideDrawName)
            {
                drawName(overrideName, overrideColor, overrideTextColor);
            }

            if (!hideNoShaderIcon())
            {
                float dummy = 0;
                getHealthNameOffsets(out bool shieldDrawn, ref dummy);
                if (player.isX && !Global.shaderWrappers.ContainsKey("palette") && player != Global.level.mainPlayer && !isWarpIn() && !(charState is Die) && player.weapon.index != 0)
                {
                    int overrideIndex = player.weapon.index;
                    if (player.weapon is NovaStrike)
                    {
                        overrideIndex = 95;
                    }
                    Global.sprites["hud_weapon_icon"].draw(overrideIndex, pos.x, pos.y - 8 + currentLabelY, 1, 1, null, 1, 1, 1, ZIndex.HUD);
                    deductLabelY(labelWeaponIconOffY);
                }
                else if (player.isZero && isBlackZero() && !Global.shaderWrappers.ContainsKey("hyperzero"))
                {
                    Global.sprites["hud_killfeed_weapon"].draw(125, pos.x, pos.y - 6 + currentLabelY, 1, 1, null, 1, 1, 1, ZIndex.HUD);
                    deductLabelY(labelKillFeedIconOffY);
                }
                else if (player.isZero && isNightmareZeroBS.getValue() && nightmareZeroShader == null)
                {
                    Global.sprites["hud_killfeed_weapon"].draw(174, pos.x, pos.y - 6 + currentLabelY, 1, 1, null, 1, 1, 1, ZIndex.HUD);
                    deductLabelY(labelKillFeedIconOffY);
                }
                else if (player.isAxl && isWhiteAxl() && !Global.shaderWrappers.ContainsKey("hyperaxl"))
                {
                    Global.sprites["hud_killfeed_weapon"].draw(123, pos.x, pos.y - 4 + currentLabelY, 1, 1, null, 1, 1, 1, ZIndex.HUD);
                    deductLabelY(labelKillFeedIconOffY);
                }
            }

            bool drewSubtankHealing = drawSubtankHealing();
            if (player.isMainPlayer && !player.isDead)
            {
                bool drewStatusProgress = drawStatusProgress();
                if (!drewStatusProgress && !drewSubtankHealing && player.isAxl)
                {
                    if (Options.main.aimKeyToggle)
                    {
                        if (player.input.isAimingBackwards(player))
                        {
                            Global.sprites["hud_axl_aim"].draw(0, pos.x, pos.y + currentLabelY, xDir, 1, null, 1, 1, 1, ZIndex.HUD);
                            deductLabelY(labelAxlAimModeIconOffY);
                        }
                        else if (player.input.isPositionLocked(player))
                        {
                            Global.sprites["hud_axl_aim"].draw(1, pos.x, pos.y + currentLabelY, 1, 1, null, 1, 1, 1, ZIndex.HUD);
                            deductLabelY(labelAxlAimModeIconOffY);
                        }
                    }
                    else if (Options.main.showRollCooldown && dodgeRollCooldown > 0)
                    {
                        drawSpinner(Helpers.progress(dodgeRollCooldown, maxDodgeRollCooldown));
                    }
                }

                if (!drewStatusProgress && !drewSubtankHealing && player.isZero)
                {
                    if (Options.main.showGigaAttackCooldown && player.zeroGigaAttackWeapon.shootTime > 0)
                    {
                        drawSpinner(Helpers.progress(player.zeroGigaAttackWeapon.shootTime, player.zeroGigaAttackWeapon.rateOfFire));
                    }
                }

                if (!drewStatusProgress && !drewSubtankHealing && player.isSigma && tagTeamSwapProgress > 0)
                {
                    float healthBarInnerWidth = 30;

                    float progress = 1 - (tagTeamSwapProgress / 1);
                    float width = progress * healthBarInnerWidth;

                    getHealthNameOffsets(out bool shieldDrawn, ref progress);

                    Point topLeft = new Point(pos.x - 16, pos.y - 5 + currentLabelY);
                    Point botRight = new Point(pos.x + 16, pos.y + currentLabelY);

                    DrawWrappers.DrawRect(topLeft.x, topLeft.y, botRight.x, botRight.y, true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White);
                    DrawWrappers.DrawRect(topLeft.x + 1, topLeft.y + 1, topLeft.x + 1 + width, botRight.y - 1, true, Color.Yellow, 0, ZIndex.HUD - 1);
                    
                    DrawWrappers.DrawText("Swapping...", pos.x, pos.y - 15 + currentLabelY, Alignment.Center, true, 0.75f, Color.White, Helpers.getAllianceColor(), Text.Styles.Regular, 1, true, ZIndex.HUD);
                    deductLabelY(labelCooldownOffY);
                }

                if (!drewStatusProgress && !drewSubtankHealing && player.isViralSigma() && charState is ViralSigmaPossessStart)
                {
                    float healthBarInnerWidth = 30;

                    float progress = (possessEnemyTime / maxPossessEnemyTime);
                    float width = progress * healthBarInnerWidth;

                    getHealthNameOffsets(out bool shieldDrawn, ref progress);

                    Point topLeft = new Point(pos.x - 16, pos.y - 5 + currentLabelY);
                    Point botRight = new Point(pos.x + 16, pos.y + currentLabelY);

                    DrawWrappers.DrawRect(topLeft.x, topLeft.y, botRight.x, botRight.y, true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White);
                    DrawWrappers.DrawRect(topLeft.x + 1, topLeft.y + 1, topLeft.x + 1 + width, botRight.y - 1, true, Color.Yellow, 0, ZIndex.HUD - 1);

                    DrawWrappers.DrawText("Possessing...", pos.x, pos.y - 15 + currentLabelY, Alignment.Center, true, 0.75f, Color.White, Helpers.getAllianceColor(), Text.Styles.Regular, 1, true, ZIndex.HUD);
                    deductLabelY(labelCooldownOffY);
                }

                if (!drewStatusProgress && !drewSubtankHealing && dropFlagProgress > 0)
                {
                    float healthBarInnerWidth = 30;

                    float progress = (dropFlagProgress);
                    float width = progress * healthBarInnerWidth;

                    getHealthNameOffsets(out bool shieldDrawn, ref progress);

                    Point topLeft = new Point(pos.x - 16, pos.y - 5 + currentLabelY);
                    Point botRight = new Point(pos.x + 16, pos.y + currentLabelY);

                    DrawWrappers.DrawRect(topLeft.x, topLeft.y, botRight.x, botRight.y, true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White);
                    DrawWrappers.DrawRect(topLeft.x + 1, topLeft.y + 1, topLeft.x + 1 + width, botRight.y - 1, true, Color.Yellow, 0, ZIndex.HUD - 1);

                    DrawWrappers.DrawText("Dropping...", pos.x + 5, pos.y - 15 + currentLabelY, Alignment.Center, true, 0.75f, Color.White, Helpers.getAllianceColor(), Text.Styles.Regular, 1, true, ZIndex.HUD);
                    deductLabelY(labelCooldownOffY);
                }

                if (!drewStatusProgress && !drewSubtankHealing && hyperProgress > 0)
                {
                    float healthBarInnerWidth = 30;

                    float progress = (hyperProgress);
                    float width = progress * healthBarInnerWidth;

                    getHealthNameOffsets(out bool shieldDrawn, ref progress);

                    Point topLeft = new Point(pos.x - 16, pos.y - 5 + currentLabelY - 2.5f);
                    Point botRight = new Point(pos.x + 16, pos.y + currentLabelY - 2.5f);

                    DrawWrappers.DrawRect(topLeft.x, topLeft.y, botRight.x, botRight.y, true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White);
                    DrawWrappers.DrawRect(topLeft.x + 1, topLeft.y + 1, topLeft.x + 1 + width, botRight.y - 1, true, Color.Yellow, 0, ZIndex.HUD - 1);

                    Global.sprites["hud_killfeed_weapon"].draw(125, pos.x, pos.y - 6 + currentLabelY, 1, 1, null, 1, 1, 1, ZIndex.HUD);
                    deductLabelY(labelCooldownOffY);
                }
            }

            if (player.isAxl)
            {
                renderAxl();
            }
            else if (player.isDisguisedAxl)
            {
                drawAxlCursor();
            }

            if (player.isKaiserSigma() && !player.isKaiserViralSigma())
            {
                renderDamageText(100);
            }
            else
            {
                renderDamageText(35);
            }

            if (player.isMainPlayer && isVileMK5 && vileHoverTime > 0 && charState is not HexaInvoluteState)
            {
                float healthPct = Helpers.clamp01((vileMaxHoverTime - vileHoverTime) / vileMaxHoverTime);
                float sy = -27;
                float sx = 20;
                if (xDir == -1) sx = 90 - 20;
                drawFuelMeter(healthPct, sx, sy);
            }

            if (player.isMainPlayer && player.isKaiserNonViralSigma() && kaiserHoverTime > 0)
            {
                float healthPct = Helpers.clamp01((kaiserMaxHoverTime - kaiserHoverTime) / kaiserMaxHoverTime);
                float sy = -70;
                float sx = 0;
                if (xDir == -1) sx = 90 - sx;
                drawFuelMeter(healthPct, sx, sy);
            }

            if (Global.showAIDebug)
            {
                float textPosX = pos.x;// (pos.x - Global.level.camX) / Global.viewSize;
                float textPosY = pos.y - 50;// (pos.y - 50 - Global.level.camY) / Global.viewSize;
                float fontSize = 0.75f;
                Color outlineColor = player.alliance == GameMode.blueAlliance ? Helpers.DarkBlue : Helpers.DarkRed;

                //DrawWrappers.DrawText("Possessing...", pos.x, pos.y - 15 + currentLabelY, Alignment.Center, true, 0.75f, Color.White, Helpers.getAllianceColor(), Text.Styles.Regular, 1, true, ZIndex.HUD);

                DrawWrappers.DrawText(player.name, textPosX, textPosY, Alignment.Center, true, fontSize, Color.White, outlineColor, Text.Styles.Regular, 1, true, ZIndex.HUD);
                if (ai != null)
                {
                    //DrawWrappers.DrawText("state:" + ai.aiState.GetType().Name, textPosX, textPosY -= 10, Alignment.Center, fontSize: fontSize, outlineColor: outlineColor);
                    var charTarget = ai.target as Character;
                    DrawWrappers.DrawText("dest:" + ai.aiState.getDestNodeName(), textPosX, textPosY -= 10, Alignment.Center, true, fontSize, Color.White, outlineColor, Text.Styles.Regular, 1, true, ZIndex.HUD);
                    DrawWrappers.DrawText("next:" + ai.aiState.getNextNodeName(), textPosX, textPosY -= 10, Alignment.Center, true, fontSize, Color.White, outlineColor, Text.Styles.Regular, 1, true, ZIndex.HUD);
                    DrawWrappers.DrawText("prev:" + ai.aiState.getPrevNodeName(), textPosX, textPosY -= 10, Alignment.Center, true, fontSize, Color.White, outlineColor, Text.Styles.Regular, 1, true, ZIndex.HUD);
                    if (charTarget != null) DrawWrappers.DrawText("target:" + charTarget?.name, textPosX, textPosY -= 10, Alignment.Center, true, fontSize, Color.White, outlineColor, Text.Styles.Regular, 1, true, ZIndex.HUD);
                    if (ai.aiState is FindPlayer fp) DrawWrappers.DrawText("stuck:" + fp.stuckTime, textPosX, textPosY -= 10, Alignment.Center, true, fontSize, Color.White, outlineColor, Text.Styles.Regular, 1, true, ZIndex.HUD);
                }
            }

            if (Global.showHitboxes)
            {
                Point? headPos = getHeadPos();
                if (headPos != null)
                {
                    //DrawWrappers.DrawCircle(headPos.Value.x, headPos.Value.y, headshotRadius, true, new Color(255, 0, 255, 128), 1, ZIndex.HUD);
                    var headRect = getHeadRect();
                    DrawWrappers.DrawRect(headRect.x1, headRect.y1, headRect.x2, headRect.y2, true, new Color(255, 0, 0, 128), 1, ZIndex.HUD);
                }
            }
        }

        public void drawSpinner(float progress)
        {
            float cx = pos.x;
            float cy = pos.y - 50;
            float ang = -90;
            float radius = 4f;
            float thickness = 1.5f;
            int count = Options.main.lowQualityParticles() ? 8 : 40;

            for (int i = 0; i < count; i++)
            {
                float angCopy = ang;
                DrawWrappers.deferredTextDraws.Add(() => DrawWrappers.DrawCircle(
                    (-Global.level.camX + cx + Helpers.cosd(angCopy) * radius) / Global.viewSize,
                    (-Global.level.camY + cy + Helpers.sind(angCopy) * radius) / Global.viewSize,
                    thickness / Global.viewSize, true, Color.Black, 1, ZIndex.HUD, isWorldPos: false));
                ang += (360f / count);
            }

            for (int i = 0; i < count * progress; i++)
            {
                float angCopy = ang;
                DrawWrappers.deferredTextDraws.Add(() => DrawWrappers.DrawCircle(
                    (-Global.level.camX + cx + Helpers.cosd(angCopy) * radius) / Global.viewSize,
                    (-Global.level.camY + cy + Helpers.sind(angCopy) * radius) / Global.viewSize,
                    (thickness - 0.5f) / Global.viewSize, true, Color.Yellow, 1, ZIndex.HUD, isWorldPos: false));
                ang += (360f / count);
            }
        }

        public bool drawSubtankHealing()
        {
            if (ownedByLocalPlayer)
            {
                if (usedSubtank != null)
                {
                    drawSubtankHealingInner(usedSubtank.health);
                    return true;
                }
            }
            else
            {
                if (netSubtankHealAmount > 0)
                {
                    drawSubtankHealingInner(netSubtankHealAmount);
                    netSubtankHealAmount -= Global.spf * 20;
                    if (netSubtankHealAmount <= 0) netSubtankHealAmount = 0;
                    return true;
                }
            }

            return false;
        }

        public void drawSubtankHealingInner(float health)
        {
            Point topLeft = new Point(pos.x - 8, pos.y - 15 + currentLabelY);
            Point topLeftBar = new Point(pos.x - 2, topLeft.y + 1);
            Point botRightBar = new Point(pos.x + 2, topLeft.y + 15);

            Global.sprites["menu_subtank"].draw(1, topLeft.x, topLeft.y, 1, 1, null, 1, 1, 1, ZIndex.HUD);
            Global.sprites["menu_subtank_bar"].draw(0, topLeftBar.x, topLeftBar.y, 1, 1, null, 1, 1, 1, ZIndex.HUD);
            float yPos = 14 * (health / SubTank.maxHealth);
            DrawWrappers.DrawRect(topLeftBar.x, topLeftBar.y, botRightBar.x, botRightBar.y - yPos, true, Color.Black, 1, ZIndex.HUD);
            
            deductLabelY(labelSubtankOffY);
        }

        public bool drawStatusProgress()
        {
            if (!Options.main.showMashProgress)
            {
                return false;
            }

            int statusIndex = 0;
            float statusProgress = 0;
            float totalMashTime = 1;

            if (charState is Frozen frozen)
            {
                statusIndex = 0;
                totalMashTime = frozen.startFreezeTime;
                statusProgress = frozen.freezeTime / totalMashTime;
            }
            else if (charState is Crystalized crystalized)
            {
                statusIndex = 1;
                totalMashTime = 2;
                statusProgress = crystalized.crystalizedTime / totalMashTime;
            }
            else if (charState is Stunned stunned)
            {
                statusIndex = 2;
                totalMashTime = 2;
                statusProgress = stunned.stunTime / totalMashTime;
            }
            else if (charState is VileMK2Grabbed grabbed)
            {
                statusIndex = 3;
                totalMashTime = VileMK2Grabbed.maxGrabTime;
                statusProgress = grabbed.grabTime / totalMashTime;
            }
            else if (parasiteTime > 0)
            {
                statusIndex = 4;
                totalMashTime = 2;
                statusProgress = 1 - (parasiteMashTime / 5);
            }
            else if (charState is UPGrabbed upGrabbed)
            {
                statusIndex = 5;
                totalMashTime = UPGrabbed.maxGrabTime;
                statusProgress = upGrabbed.grabTime / totalMashTime;
            }
            else if (charState is WhirlpoolGrabbed drained)
            {
                statusIndex = 6;
                totalMashTime = WhirlpoolGrabbed.maxGrabTime;
                statusProgress = drained.grabTime / totalMashTime;
            }
            else if (player.isPossessed())
            {
                statusIndex = 7;
                totalMashTime = Player.maxPossessedTime;
                statusProgress = player.possessedTime / totalMashTime;
            }
            else if (charState is WheelGGrabbed wheelgGrabbed)
            {
                statusIndex = 8;
                totalMashTime = WheelGGrabbed.maxGrabTime;
                statusProgress = wheelgGrabbed.grabTime / totalMashTime;
            }
            else if (charState is FStagGrabbed fstagGrabbed)
            {
                statusIndex = 9;
                totalMashTime = FStagGrabbed.maxGrabTime;
                statusProgress = fstagGrabbed.grabTime / totalMashTime;
            }
            else if (charState is MagnaCDrainGrabbed magnacGrabbed)
            {
                statusIndex = 10;
                totalMashTime = MagnaCDrainGrabbed.maxGrabTime;
                statusProgress = magnacGrabbed.grabTime / totalMashTime;
            }
            else if (charState is CrushCGrabbed crushcGrabbed)
            {
                statusIndex = 11;
                totalMashTime = CrushCGrabbed.maxGrabTime;
                statusProgress = crushcGrabbed.grabTime / totalMashTime;
            }
            else if (charState is BBuffaloDragged bbuffaloDragged)
            {
                statusIndex = 12;
                totalMashTime = BBuffaloDragged.maxGrabTime;
                statusProgress = bbuffaloDragged.grabTime / totalMashTime;
            }
            else if (charState is DarkHoldState darkHoldState)
            {
                statusIndex = 13;
                totalMashTime = DarkHoldState.totalStunTime;
                statusProgress = darkHoldState.stunTime / totalMashTime;
            }
            else
            {
                player.lastMashAmount = 0;
                return false;
            }

            float healthBarInnerWidth = 30;

            float width = Helpers.clampMax(MathF.Ceiling(healthBarInnerWidth * statusProgress), healthBarInnerWidth);
            float mashWidth = healthBarInnerWidth * (player.lastMashAmount / totalMashTime);

            getHealthNameOffsets(out bool shieldDrawn, ref statusProgress);

            Point topLeft = new Point(pos.x - 16, pos.y - 5 + currentLabelY);
            Point botRight = new Point(pos.x + 16, pos.y + currentLabelY);
            Global.sprites["hud_status_icon"].draw(statusIndex, pos.x, topLeft.y - 7, 1, 1, null, 1, 1, 1, ZIndex.HUD);
            
            DrawWrappers.DrawRect(topLeft.x, topLeft.y, botRight.x, botRight.y, true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White);
            DrawWrappers.DrawRect(topLeft.x + 1, topLeft.y + 1, topLeft.x + 1 + width, botRight.y - 1, true, Color.Yellow, 0, ZIndex.HUD - 1);
            DrawWrappers.DrawRect(topLeft.x + 1 + width, topLeft.y + 1, Math.Min(topLeft.x + 1 + width + mashWidth, botRight.x - 1), botRight.y - 1, true, Color.Red, 0, ZIndex.HUD - 1);

            deductLabelY(labelStatusOffY);

            return true;
        }

        public bool hideHealthAndName()
        {
            if (isWarpIn()) return true;
            if (sprite.name.EndsWith("warp_beam")) return true;
            if (!player.readyTextOver) return true;
            if (isDeathOrReviveSprite()) return true;
            if (Global.level.is1v1() && !Global.level.gameMode.isTeamMode && !Global.level.mainPlayer.isSpectator) return true;
            if (Global.showAIDebug) return true;
            return false;
        }

        // Used to show weapon icons, WA/BZ, etc for non-shader enabled players
        public bool hideNoShaderIcon()
        {
            if (isWarpIn()) return true;
            if (!player.readyTextOver) return true;
            if (isDeathOrReviveSprite()) return true;
            if (Global.showAIDebug) return true;
            if (isInvisibleBS.getValue()) return true;
            return false;
        }

        public void getHealthNameOffsets(out bool shieldDrawn, ref float healthPct)
        {        
            shieldDrawn = false;
            if (player.character != null && player.character.rideArmor != null)
            {
                shieldDrawn = true;
                healthPct = player.character.rideArmor.health / player.character.rideArmor.maxHealth;
            }
            else if (player.isX && chargedRollingShieldProj != null)
            {
                shieldDrawn = true;
                healthPct = player.weapon.ammo / player.weapon.maxAmmo;
            }
            /*
            else if (player.scanned && !player.isVile)
            {
                shieldDrawn = true;
                if (player.isZero) healthPct = player.rakuhouhaWeapon.ammo / player.rakuhouhaWeapon.maxAmmo;
                else healthPct = player.weapon.ammo / player.weapon.maxAmmo;
            }
            */
        }

        public void drawHealthBar()
        {
            float healthBarInnerWidth = 30;
            Color color = new Color();

            float healthPct = player.health / player.maxHealth;
            float width = Helpers.clampMax(MathF.Ceiling(healthBarInnerWidth * healthPct), healthBarInnerWidth);
            if (healthPct > 0.66) color = Color.Green;
            else if (healthPct <= 0.66 && healthPct >= 0.33) color = Color.Yellow;
            else if (healthPct < 0.33) color = Color.Red;

            getHealthNameOffsets(out bool shieldDrawn, ref healthPct);

            float botY = pos.y + currentLabelY;
            DrawWrappers.DrawRect(pos.x - 16, botY - 5, pos.x + 16, botY, true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White);
            DrawWrappers.DrawRect(pos.x - 15, botY - 4, pos.x - 15 + width, botY - 1, true, color, 0, ZIndex.HUD - 1);

            // Shield
            if (shieldDrawn)
            {
                width = Helpers.clampMax(MathF.Ceiling(healthBarInnerWidth * healthPct), healthBarInnerWidth);
                float shieldOffY = 4f;
                DrawWrappers.DrawRect(pos.x - 16, botY - 5 - shieldOffY, pos.x + 16, botY - shieldOffY, true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White);
                DrawWrappers.DrawRect(pos.x - 15, botY - 4 - shieldOffY, pos.x - 15 + width, botY - 1 - shieldOffY, true, Color.Blue, 0, ZIndex.HUD - 1);
                deductLabelY(labelHealthOffY + shieldOffY);
            }
            else
            {
                deductLabelY(labelHealthOffY);
            }
        }

        public void drawName(string overrideName = "", Color? overrideColor = null, Color? overrideTextColor = null)
        {
            float healthPct = 0;
            getHealthNameOffsets(out bool shieldDrawn, ref healthPct);

            string playerName = player.name;
            Color playerColor = Helpers.DarkBlue;
            if (Global.level.gameMode.isTeamMode)
            {
                playerColor = player.alliance == GameMode.blueAlliance ? Helpers.DarkBlue : Helpers.DarkRed;
            }

            if (!string.IsNullOrEmpty(overrideName)) playerName = overrideName;
            if (overrideColor != null) playerColor = overrideColor.Value;

            float textPosX = pos.x;
            float textPosY = pos.y + currentLabelY - 8;

            DrawWrappers.DrawText(playerName, textPosX, textPosY, Alignment.Center, true, 0.75f,
                overrideTextColor ?? Color.White, playerColor, style: overrideTextColor == null ? Text.Styles.Regular : Text.Styles.Italic, 1, true, ZIndex.HUD);

            deductLabelY(labelNameOffY);
        }

        public void applyDamage(Player attacker, int? weaponIndex, float damage, int? projId)
        {
            if (!ownedByLocalPlayer) return;
            if (attacker == player && isWhiteAxl())
            {
                damage = 0;
            }
            if (Global.level.isRace() && damage != Damager.envKillDamage && damage != Damager.switchKillDamage && attacker != player)
            {
                damage = 0;
            }

            bool isArmorPiercing = Damager.isArmorPiercing(projId);

            if (projId == (int)ProjIds.CrystalHunterDash)
            {
                var crystalizedState = charState as Crystalized;
                if (crystalizedState != null)
                {
                    if (damage > 0) crystalizedState.crystalizedTime = 0; //Dash to destroy crystal   
                }
            }

            var inRideArmor = charState as InRideArmor;
            if (inRideArmor != null && inRideArmor.crystalizeTime > 0)
            {
                if (weaponIndex == 20 && damage > 0) inRideArmor.crystalizeTime = 0;   //Dash to destroy crystal
                inRideArmor.checkCrystalizeTime();
            }

            // Damage increase/reduction section
            if (!isArmorPiercing)
            {
                if (charState is SwordBlock)
                {
                    if (player.isSigma)
                    {
                        if (player.isPuppeteer()) damageSavings += (damage * 0.25f);
                        else damageSavings += (damage * 0.5f);
                    }
                    else damageSavings += (damage * 0.25f);
                }

                if (acidTime > 0)
                {
                    float extraDamage = 0.25f + (0.25f * (acidTime / 8.0f));
                    damageDebt += (damage * extraDamage);
                }

                if (hasBarrier(false))
                {
                    damageSavings += (damage * 0.25f);
                }
                else if (hasBarrier(true))
                {
                    damageSavings += (damage * 0.5f);
                }

                if (player.isX && player.hasBodyArmor(1))
                {
                    damageSavings += damage / 8f;
                }

                if (player.isX && player.hasBodyArmor(2))
                {
                    damageSavings += damage / 8f;
                }

                if (player.isVile && hasFrozenCastleBarrier())
                {
                    damageSavings += damage * frozenCastlePercent;
                }

                while (damageSavings >= 1)
                {
                    damageSavings -= 1;
                    damage -= 1;
                }

                while (damageDebt >= 1)
                {
                    damageDebt -= 1;
                    damage += 1;
                }
            }

            if (damage < 0) damage = 0;

            player.health -= damage;

            if (player.showTrainingDps && player.health > 0 && damage > 0)
            {
                if (player.trainingDpsStartTime == 0)
                {
                    player.trainingDpsStartTime = Global.time;
                    Global.level.gameMode.dpsString = "";
                }
                player.trainingDpsTotalDamage += damage;
            }

            if (damage > 0)
            {
                noDamageTime = 0;
                rechargeHealthTime = 0;
            }

            if (damage > 0 && attacker != null)
            {
                if (projId != (int)ProjIds.Burn && projId != (int)ProjIds.AcidBurstPoison)
                {
                    player.delaySubtank();
                }
                addDamageTextHelper(attacker, damage, player.maxHealth, true);
            }

            if (player.health > 0 && damage > 0)
            {
                float modifier = player.maxHealth > 0 ? (16 / player.maxHealth) : 1;
                float gigaAmmoToAdd = 1 + (damage * 2 * modifier);
                if (player.isZero && ownedByLocalPlayer)
                {
                    player.zeroGigaAttackWeapon.addAmmo(gigaAmmoToAdd, player);
                }
                if (player.isX && ownedByLocalPlayer)
                {
                    var gigaCrush = player.weapons.FirstOrDefault(w => w is GigaCrush);
                    if (gigaCrush != null)
                    {
                        gigaCrush.addAmmo(gigaAmmoToAdd, player);
                    }
                    var hyperBuster = player.weapons.FirstOrDefault(w => w is HyperBuster);
                    if (hyperBuster != null)
                    {
                        hyperBuster.addAmmo(gigaAmmoToAdd, player);
                    }
                    var novaStrike = player.weapons.FirstOrDefault(w => w is NovaStrike);
                    if (novaStrike != null)
                    {
                        novaStrike.addAmmo(gigaAmmoToAdd, player);
                    }
                    //fgMoveAmmo += gigaAmmoToAdd;
                    //if (fgMoveAmmo > 32) fgMoveAmmo = 32;
                }
                if (player.isSigma && player.isSigma2() && !player.isViralSigma() && ownedByLocalPlayer)
                {
                    player.sigmaAmmo = Helpers.clampMax(player.sigmaAmmo + gigaAmmoToAdd, player.sigmaMaxAmmo);
                }
            }

            if (attacker != null && weaponIndex != null)
            {
                damageHistory.Add(new DamageEvent(attacker, weaponIndex.Value, projId, false, Global.time));
            }

            if (player.health <= 0)
            {
                if (player.showTrainingDps && player.trainingDpsStartTime > 0)
                {
                    float timeToKill = Global.time - player.trainingDpsStartTime;
                    float dps = player.trainingDpsTotalDamage / timeToKill;
                    Global.level.gameMode.dpsString = "DPS: " + dps.ToString("0.0");

                    player.trainingDpsTotalDamage = 0;
                    player.trainingDpsStartTime = 0;
                }
                killPlayer(attacker, null, weaponIndex, projId);
            }
            else
            {
                if (player.isX && player.hasBodyArmor(3) && damage > 0)
                {
                    addBarrier(charState is Hurt);
                }
            }
        }

        public void killPlayer(Player killer, Player assister, int? weaponIndex, int? projId)
        {
            player.health = 0;
            int? assisterProjId = null;
            int? assisterWeaponId = null;
            if (charState is not Die || !ownedByLocalPlayer)
            {
                player.lastDeathCanRevive = Global.anyQuickStart || Global.debug || Global.level.isTraining() || killer != null;
                changeState(new Die(), true);

                if (ownedByLocalPlayer)
                {
                    getKillerAndAssister(player, ref killer, ref assister, ref weaponIndex, ref assisterProjId, ref assisterWeaponId);
                }

                if (killer != null && killer != player)
                {
                    killer.addKill();
                    if (Global.level.gameMode is TeamDeathMatch)
                    {
                        if (Global.isHost)
                        {
                            if (player.alliance == GameMode.redAlliance) Global.level.gameMode.bluePoints++;
                            if (player.alliance == GameMode.blueAlliance) Global.level.gameMode.redPoints++;
                            Global.level.gameMode.syncTeamScores();
                        }
                    }

                    killer.awardScrap();
                }
                else if (Global.level.gameMode.level.is1v1())
                {
                    // In 1v1 the other player should always be considered a killer to prevent suicide
                    var otherPlayer = Global.level.nonSpecPlayers().Find(p => p.id != player.id);
                    if (otherPlayer != null)
                    {
                        otherPlayer.addKill();
                    }
                }

                if (assister != null && assister != player)
                {
                    assister.addAssist();
                    assister.addKill();

                    assister.awardScrap();
                }
                //bool isSuicide = killer == null || killer == player;
                player.addDeath(false);
                /*
                if (isSuicide && Global.isHost && Global.level.gameMode is TeamDeathMatch)
                {
                    if (player.alliance == GameMode.redAlliance) Global.level.gameMode.redPoints--;
                    if (player.alliance == GameMode.blueAlliance) Global.level.gameMode.bluePoints--;
                    if (Global.level.gameMode.bluePoints < 0) Global.level.gameMode.bluePoints = 0;
                    if (Global.level.gameMode.redPoints < 0) Global.level.gameMode.redPoints = 0;
                    Global.level.gameMode.syncTeamScores();
                }
                */
                
                Global.level.gameMode.addKillFeedEntry(new KillFeedEntry(killer, assister, player, weaponIndex));
                if (ownedByLocalPlayer && Global.level.isNon1v1Elimination() && player.deaths >= Global.level.gameMode.playingTo)
                {
                    Global.level.gameMode.addKillFeedEntry(new KillFeedEntry(player.name + " was eliminated.", GameMode.blueAlliance), sendRpc: true);
                }

                if (killer?.ownedByLocalPlayer == true && killer.copyShotDamageEvents.Any(c => c.character == this))
                {
                    killer.character?.addDNACore(this);
                }
                else if (assister?.ownedByLocalPlayer == true && assister.copyShotDamageEvents.Any(c => c.character == this))
                {
                    assister.character?.addDNACore(this);
                }

                if (ownedByLocalPlayer)
                {
                    var victimPlayerIdBytes = BitConverter.GetBytes((ushort)player.id);

                    if (weaponIndex != null && killer != null)
                    {
                        var bytes = new List<byte>()
                        {
                            (byte)1,
                            (byte)killer.id,
                            assister == null ? (byte)killer.id : (byte)assister.id,
                            victimPlayerIdBytes[0],
                            victimPlayerIdBytes[1],
                            (byte)weaponIndex
                        };

                        if (projId != null)
                        {
                            byte[] projIdBytes = BitConverter.GetBytes((ushort)projId.Value);
                            bytes.Add(projIdBytes[0]);
                            bytes.Add(projIdBytes[1]);
                        }
                        
                        Global.serverClient?.rpc(RPC.killPlayer, bytes.ToArray());
                    }
                    else
                    {
                        Global.serverClient?.rpc(RPC.killPlayer, (byte)0, (byte)0, (byte)0, victimPlayerIdBytes[0], victimPlayerIdBytes[1]);
                    }
                }
            }
        }

        public void addHealth(float amount, bool fillSubtank = true)
        {
            if (player.health >= player.maxHealth && fillSubtank)
            {
                player.fillSubtank(amount);
            }
            healAmount += amount;
        }

        public void fillHealthToMax()
        {
            healAmount += player.maxHealth;
        }

        public void addAmmo(float amount)
        {
            if (player.isX && player.weapon.ammo >= player.weapon.maxAmmo)
            {
                foreach (var weapon in player.weapons)
                {
                    if (weapon == player.weapon) continue;
                    if (weapon.ammo == weapon.maxAmmo) continue;
                    weapon.ammo = MathF.Clamp(weapon.ammo + amount, 0, weapon.maxAmmo);
                    break;
                }
                return;
            }

            weaponHealAmount += amount;
        }

        public void increaseCharge()
        {
            float factor = 1;
            if (player.isX && player.hasArmArmor(1)) factor = 1.5f;
            //if (player.isX && isHyperX) factor = 1.5f;
            if (isBlackZero2()) factor = 1.5f;
            //if (player.isZero && isAttacking()) factor = 0f;
            chargeTime += Global.spf * factor;
        }

        public void dropFlag()
        {
            if (flag != null)
            {
                flag.dropFlag();
                flag = null;
            }
        }

        public void onFlagPickup(Flag flag)
        {
            if (isCharging())
            {
                stopCharge();
            }
            dropFlagProgress = 0;
            stockedCharge = false;
            this.flag = flag;
            stingChargeTime = 0;
            if (beeSwarm != null)
            {
                beeSwarm.destroy();
            }
            if (chargedRollingShieldProj != null)
            {
                chargedRollingShieldProj.destroySelf();
            }
            popAllBubbles();
            if (player.isDisguisedAxl && player.ownedByLocalPlayer)
            {
                player.revertToAxl();
            }
        }

        public void popAllBubbles()
        {
            for (int i = chargedBubbles.Count - 1; i >= 0; i--)
            {
                chargedBubbles[i].destroySelf();
            }
        }

        public void setHurt(int dir, int flinchFrames, float miniFlinchTime, bool spiked)
        {
            // tough guy
            if (player.isSigma || isToughGuyHyperMode())
            {
                if (miniFlinchTime > 0) return;
                else
                {
                    flinchFrames = 0;
                    miniFlinchTime = 0.1f;
                }
            }
            if (!(charState is Die) && !(charState is InRideArmor) && !(charState is InRideChaser))
            {
                changeState(new Hurt(dir, flinchFrames, miniFlinchTime, spiked), true);
            }
        }

        public override void destroySelf(string spriteName = null, string fadeSound = null, bool rpc = false, bool doRpcEvenIfNotOwned = false, bool favorDefenderProjDestroy = false)
        {
            base.destroySelf(spriteName, fadeSound, rpc, doRpcEvenIfNotOwned);

            player.removeOwnedMines();
            player.removeOwnedTurrets();
            player.removeOwnedGrenades();
            player.removeOwnedIceStatues();
            player.removeOwnedMechaniloids();
            player.removeOwnedSeeds();

            chargeEffect?.destroy();
            chargeSound?.destroy();
            iceGattlingSound?.destroy();
            chargedRollingShieldProj?.destroySelfNoEffect();
            gaeaShield?.destroySelf();
            muzzleFlash?.destroySelf();
            strikeChainProj?.destroySelf();
            beeSwarm?.destroy();
            parasiteAnim?.destroySelf();
            barrierAnim?.destroySelf();
            sniperMissileProj?.destroySelf();
            kaiserExhaustL?.destroySelf();
            kaiserExhaustR?.destroySelf();
            destroyBusterProjs();
            setShootRaySplasher(false);

            if (player.isX && player.hasUltimateArmor()) player.setUltimateArmor(false);
            if (player.isX && player.hasGoldenArmor()) player.setGoldenArmor(false);

            head?.explode();
            leftHand?.destroySelf();
            rightHand?.destroySelf();

            // This ensures that the "onExit" charState function can do any cleanup it needs to do without having to copy-paste that code here, too
            charState?.onExit(null);
        }

        public void cleanupBeforeTransform()
        {
            parasiteAnim?.destroySelf();
            parasiteTime = 0;
            parasiteMashTime = 0;
            parasiteDamager = null;
            removeAcid();
            removeBurn();
        }

        public bool canBeHealed(int healerAlliance)
        {
            if (isHyperSigmaBS.getValue()) return false;
            return player.alliance == healerAlliance && player.health > 0 && player.health < player.maxHealth;
        }

        public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false)
        {
            if (!allowStacking && this.healAmount > 0) return;
            if (player.health < player.maxHealth)
            {
                playHealSound = true;
            }
            commonHealLogic(healer, healAmount, player.health, player.maxHealth, drawHealText);
            addHealth(healAmount, fillSubtank: false);
        }

        public void crystalizeStart()
        {
            isCrystalized = true;
            if (globalCollider != null) globalCollider.isClimbable = true;
            new Anim(getCenterPos(), "crystalhunter_activate", 1, null, true);
            playSound("crystalize");
        }

        public void crystalizeEnd()
        {
            isCrystalized = false;
            playSound("freezebreak2");
            for (int i = 0; i < 8; i++)
            {
                var anim = new Anim(getCenterPos().addxy(Helpers.randomRange(-20, 20), Helpers.randomRange(-20, 20)), "crystalhunter_piece", Helpers.randomRange(0, 1) == 0 ? -1 : 1, null, false);
                anim.frameIndex = Helpers.randomRange(0, 1);
                anim.frameSpeed = 0;
                anim.useGravity = true;
                anim.vel = new Point(Helpers.randomRange(-150, 150), Helpers.randomRange(-300, 25));
            }
        }

        // PARASITE SECTION

        public ParasiteAnim parasiteAnim;
        public bool hasParasite { get { return parasiteTime > 0; } }
        public float parasiteTime;
        public float parasiteMashTime;
        public Damager parasiteDamager;
        public BeeSwarm beeSwarm;

        public void addParasite(Player attacker)
        {
            if (!ownedByLocalPlayer) return;

            Damager damager = new Damager(attacker, 4, Global.defFlinch, 0);
            parasiteTime = Global.spf;
            parasiteDamager = damager;
            parasiteAnim = new ParasiteAnim(getCenterPos(), "parasitebomb_latch_start", player.getNextActorNetId(), true, true);
        }

        public void updateParasite()
        {
            if (parasiteTime <= 0) return;
            slowdownTime = Math.Max(slowdownTime, 0.05f);

            if (!(charState is ParasiteCarry) && parasiteTime > 1.5f)
            {
                foreach (var otherPlayer in Global.level.players)
                {
                    if (otherPlayer.character == null) continue;
                    if (otherPlayer == player) continue;
                    if (otherPlayer == parasiteDamager.owner) continue;
                    if (otherPlayer.character.isInvulnerable()) continue;
                    if (Global.level.gameMode.isTeamMode && otherPlayer.alliance != player.alliance) continue;
                    if (otherPlayer.character.getCenterPos().distanceTo(getCenterPos()) > ParasiticBomb.carryRange) continue;
                    Character target = otherPlayer.character;
                    changeState(new ParasiteCarry(target, true));
                    break;
                }
            }

            if (parasiteAnim != null)
            {
                if (parasiteAnim.sprite.name == "parasitebomb_latch_start" && parasiteAnim.isAnimOver())
                {
                    parasiteAnim.changeSprite("parasitebomb_latch", true);
                }
                parasiteAnim.changePos(getParasitePos());
            }

            parasiteTime += Global.spf;
            float mashValue = player.mashValue();
            if (mashValue > Global.spf)
            {
                parasiteMashTime += mashValue;
            }
            if (parasiteMashTime > 5)
            {
                removeParasite(true, false);
            }
            else if (parasiteTime > 2 && !(charState is ParasiteCarry))
            {
                removeParasite(false, false);
            }
        }

        public void removeParasite(bool ejected, bool carried)
        {
            if (!ownedByLocalPlayer) return;
            if (parasiteDamager == null) return;

            parasiteAnim?.destroySelf();
            if (ejected)
            {
                new Anim(getCenterPos(), "parasitebomb_latch", 1, player.getNextActorNetId(), true, sendRpc: true, ownedByLocalPlayer)
                {
                    vel = new Point(50 * xDir, -50),
                    useGravity = true
                };
            }
            else
            {
                new Anim(getCenterPos(), "explosion", 1, player.getNextActorNetId(), true, sendRpc: true, ownedByLocalPlayer);
                playSound("explosion", sendRpc: true);
                if (!carried) parasiteDamager.applyDamage(this, player.weapon is FrostShield, new ParasiticBomb(), this, (int)ProjIds.ParasiticBombExplode, overrideDamage: 4, overrideFlinch: Global.defFlinch);
            }

            parasiteTime = 0;
            parasiteMashTime = 0;
            parasiteDamager = null;
        }

        public bool isInvisible()
        {
            return stingChargeTime > 0 && (player.isX || stealthRevealTime == 0);
        }

        public bool genmuImmune(Player owner)
        {
            return false;
        }

        public override Dictionary<int, Func<Projectile>> getGlobalProjs()
        {
            var retProjs = new Dictionary<int, Func<Projectile>>();

            if (player.isZero && isAwakenedZeroBS.getValue() && globalCollider != null)
            {
                retProjs[(int)ProjIds.AwakenedAura] = () =>
                {
                    Point centerPoint = globalCollider.shape.getRect().center();
                    float damage = 2;
                    int flinch = 0;
                    if (isAwakenedGenmuZeroBS.getValue())
                    {
                        damage = 4;
                        flinch = Global.defFlinch;
                    }
                    Projectile proj = new GenericMeleeProj(player.awakenedAuraWeapon, centerPoint, ProjIds.AwakenedAura, player, damage, flinch, 0.5f);
                    proj.globalCollider = globalCollider.clone();
                    return proj;
                };
            }
            else if (canHeadbutt() && getHeadPos() != null)
            {
                retProjs[(int)ProjIds.Headbutt] = () =>
                {
                    Point centerPoint = getHeadPos().Value.addxy(0, -6);
                    float damage = 2;
                    int flinch = Global.halfFlinch;
                    if (sprite.name.Contains("up_dash"))
                    {
                        damage = 4;
                        flinch = Global.defFlinch;
                    }
                    Projectile proj = new GenericMeleeProj(player.headbuttWeapon, centerPoint, ProjIds.Headbutt, player, damage, flinch, 0.5f);
                    var rect = new Rect(0, 0, 14, 4);
                    proj.globalCollider = new Collider(rect.getPoints(), false, proj, false, false, 0, Point.zero);
                    return proj;
                };
            }
            else if (sprite.name.Contains("viral_tackle") && sprite.time > 0.15f)
            {
                retProjs[(int)ProjIds.Sigma2ViralTackle] = () =>
                {
                    var damageCollider = getAllColliders().FirstOrDefault(c => c.isAttack());
                    Point centerPoint = damageCollider.shape.getRect().center();
                    Projectile proj = new GenericMeleeProj(new ViralSigmaTackleWeapon(player), centerPoint, ProjIds.Sigma2ViralTackle, player);
                    proj.globalCollider = damageCollider.clone();
                    return proj;
                };
            }
            return retProjs;
        }

        public override void updateProjFromHitbox(Projectile proj)
        {
            if (proj.projId == (int)ProjIds.Sigma3KaiserStomp)
            {
                float damagePercent = getKaiserStompDamage();
                if (damagePercent > 0)
                {
                    proj.damager.damage = 12 * damagePercent;
                }
            }
            else if (proj.projId == (int)ProjIds.AwakenedAura)
            {
                if (isAwakenedGenmuZeroBS.getValue())
                {
                    proj.damager.damage = 4;
                    proj.damager.flinch = Global.defFlinch;
                }
            }
        }

        public float getKaiserStompDamage()
        {
            float damagePercent = 0.25f;
            if (deltaPos.y > 150 * Global.spf) damagePercent = 0.5f;
            if (deltaPos.y > 210 * Global.spf) damagePercent = 0.75f;
            if (deltaPos.y > 300 * Global.spf) damagePercent = 1;
            return damagePercent;
        }

        public bool canHeadbutt()
        {
            if (!player.isX) return false;
            if (!player.hasHelmetArmor(1)) return false;
            if (isInvisibleBS.getValue()) return false;
            if (isInvulnerableAttack()) return false;
            if (headbuttAirTime < 0.04f) return false;
            if (sprite.name.Contains("jump") && deltaPos.y < -100 * Global.spf) return true;
            if (sprite.name.Contains("up_dash") || sprite.name.Contains("wall_kick")) return true;
            if (charState is StrikeChainPullToWall scptw && scptw.isUp) return true;
            return false;
        }

        public bool hasHadoukenEquipped()
        {
            return !Global.level.is1v1() && player.hasArmArmor(1) && player.hasBootsArmor(1) && player.hasHelmetArmor(1) && player.hasBodyArmor(1) && player.weapons.Any(w => w is Buster);
        }

        public bool hasShoryukenEquipped()
        {
            return !Global.level.is1v1() && player.hasArmArmor(2) && player.hasBootsArmor(2) && player.hasHelmetArmor(2) && player.hasBodyArmor(2) && player.weapons.Any(w => w is Buster);
        }

        public bool hasFgMoveEquipped()
        {
            return hasHadoukenEquipped() || hasShoryukenEquipped();
        }

        public bool canAffordFgMove()
        {
            return player.scrap >= 3 || player.hasAllItems();
        }

        public bool canUseFgMove()
        {
            return !isInvulnerableAttack() && chargedRollingShieldProj == null && !isInvisibleBS.getValue() && canAffordFgMove() && hadoukenCooldownTime == 0 && player.weapon is Buster && player.fgMoveAmmo >= 32;
        }

        public bool shouldDrawFgCooldown()
        {
            return !isInvulnerableAttack() && chargedRollingShieldProj == null && !isInvisibleBS.getValue() && canAffordFgMove() && hadoukenCooldownTime == 0;
        }

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            if (player.isX) return getXProjFromHitbox(centerPoint);
            else if (player.isZero) return getZeroProjFromHitbox(hitbox, centerPoint);
            else if (player.isVile) return getVileProjFromHitbox(centerPoint);
            //else if (player.isAxl) return getAxlProjFromHitbox(centerPoint);
            else if (player.isSigma) return getSigmaProjFromHitbox(hitbox, centerPoint);
            return null;
        }

        public void releaseGrab(Actor grabber)
        {
            charState?.releaseGrab();
            if (!ownedByLocalPlayer)
            {
                RPC.commandGrabPlayer.sendRpc(grabber.netId, netId, CommandGrabScenario.Release, grabber.isDefenderFavored());
            }
        }

        public bool isAlwaysHeadshot()
        {
            return sprite?.name?.Contains("_ra_") == true || sprite?.name?.Contains("_rc_") == true;
        }

        public bool canEjectFromRideArmor()
        {
            var shape = globalCollider.shape;
            if (shape.minY < 0 && shape.maxY < 0)
            {
                shape = shape.clone(0, MathF.Abs(shape.maxY) + 1);
            }

            var collision = Global.level.checkCollisionShape(shape, new List<GameObject>() { rideArmor });
            if (collision?.gameObject is not Wall)
            {
                return true;
            }

            return false;
        }
    }

    public struct DamageEvent
    {
        public Player attacker;
        public int weapon;
        public int? projId;
        public float time;
        public bool envKillOnly;

        public DamageEvent(Player attacker, int weapon, int? projId, bool envKillOnly, float time)
        {
            this.attacker = attacker;
            this.weapon = weapon;
            this.projId = projId;
            this.envKillOnly = envKillOnly;
            this.time = time;
        }
    }
}
