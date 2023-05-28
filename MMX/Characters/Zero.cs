using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public partial class Character
    {
        public float rakuhouhaCooldown { get { return player.zeroGigaAttackWeapon.shootTime; } }
        public float ryuenjinCooldown { get { return Math.Max(player.zeroUppercutWeaponA.shootTime, player.zeroUppercutWeaponS.shootTime); } }
        public float hyouretsuzanCooldown { get { return Math.Max(player.zeroDownThrustWeaponA.shootTime, player.zeroDownThrustWeaponS.shootTime); } }

        public float dashAttackCooldown;
        public float maxDashAttackCooldown = 0.75f;
        public float airAttackCooldown;
        public float maxAirAttackCooldown = 0.5f;
        public float genmuCooldown;

        public float zSaberShotCooldown;
        public float maxZSaberShotCooldown = 0.33f;
        public float knuckleSoundCooldown;

        public float maxHyperZeroTime = 12;
        public float blackZeroTime;
        public float awakenedZeroTime;
        public bool hyperZeroUsed;
        public bool isNightmareZero;
        public ShaderWrapper zeroPaletteShader;
        public ShaderWrapper nightmareZeroShader;
        public int quakeBlazerBounces;
        public float zero3SwingComboStartTime;
        public float zero3SwingComboEndTime;
        public float hyorogaCooldown = 0;
        public const float maxHyorogaCooldown = 1f;
        public float zeroLemonCooldown;
        public bool doubleBusterDone;

        public bool isAttacking()
        {
            return sprite.name.Contains("attack") ||
                   sprite.name.Contains("zero_hyouretsuzan") ||
                   sprite.name.Contains("zero_raijingeki") ||
                   sprite.name.Contains("zero_ryuenjin") ||
                   sprite.name.Contains("zero_rakukojin") ||
                   sprite.name.Contains("zero_raijingeki2") ||
                   sprite.name.Contains("zero_eblade") ||
                   sprite.name.Contains("zero_rising") ||
                   sprite.name.Contains("zero_quakeblazer") ||
                   sprite.name.Contains("zero_genmu") ||
                   sprite.name.Contains("zero_projswing") || 
                   sprite.name.Contains("zero_tbreaker") ||
                   sprite.name.Contains("zero_spear") ||
                   sprite.name.Contains("punch") ||
                   sprite.name.Contains("zero_kick_air") ||
                   sprite.name.Contains("zero_dropkick");
        }

        int framesSinceLastAttack = 1000;

        public void updateZero()
        {
            player.raijingekiWeapon.update();
            player.zeroAirSpecialWeapon.update();
            player.zeroUppercutWeaponA.update();
            player.zeroUppercutWeaponS.update();
            player.zeroDownThrustWeaponA.update();
            player.zeroDownThrustWeaponS.update();
            player.zeroGigaAttackWeapon.update();

            if (dashAttackCooldown > 0) dashAttackCooldown = Helpers.clampMin0(dashAttackCooldown - Global.spf);
            if (airAttackCooldown > 0) airAttackCooldown = Helpers.clampMin0(airAttackCooldown - Global.spf);
            Helpers.decrementTime(ref wallKickCooldown);
            Helpers.decrementTime(ref saberCooldown);
            Helpers.decrementTime(ref xSaberCooldown);
            Helpers.decrementTime(ref genmuCooldown);
            Helpers.decrementTime(ref zSaberShotCooldown);
            Helpers.decrementTime(ref knuckleSoundCooldown);
            Helpers.decrementTime(ref hyorogaCooldown);

            if (shootAnimTime > 0)
            {
                shootAnimTime -= Global.spf;
                if (shootAnimTime <= 0)
                {
                    shootAnimTime = 0;
                    changeSpriteFromName(charState.defaultSprite, false);
                    if (charState is WallSlide)
                    {
                        frameIndex = sprite.frames.Count - 1;
                    }
                }
            }

            Weapon gigaWeapon = player.zeroGigaAttackWeapon;
            if (gigaWeapon.ammo >= gigaWeapon.maxAmmo)
            {
                weaponHealAmount = 0;
            }
            if (weaponHealAmount > 0 && player.health > 0)
            {
                weaponHealTime += Global.spf;
                if (weaponHealTime > 0.05)
                {
                    weaponHealTime = 0;
                    weaponHealAmount--;
                    gigaWeapon.ammo = Helpers.clampMax(gigaWeapon.ammo + 1, gigaWeapon.maxAmmo);
                    playSound("heal", forcePlay: true);
                }
            }

            bool attackPressed = false;
            if (player.weapon is not AssassinBullet)
            {
                if (player.input.isPressed(Control.Shoot, player))
                {
                    attackPressed = true;
                    framesSinceLastAttack = 0;
                }
                else
                {
                    framesSinceLastAttack++;
                }
            }

            if (player.chargeButtonHeld() && (player.scrap > 0 || player.isZBusterZero() || player.weapon is AssassinBullet) && flag == null && rideChaser == null && rideArmor == null)
            {
                if (!stockedXSaber && !isInvulnerableAttack())
                {
                    increaseCharge();
                }
            }
            else
            {
                int chargeLevel = getChargeLevel();
                if (isCharging())
                {
                    if (player.weapon is AssassinBullet)
                    {
                        shootAssassinShot();
                    }
                    else if (chargeLevel == 1)
                    {
                        zeroShoot(1);
                    }
                    else if (chargeLevel == 2)
                    {
                        zeroShoot(2);
                    }
                    else if (chargeLevel >= 3)
                    {
                        if (player.scrap >= 10 && !hyperZeroUsed && flag == null && !player.isZBusterZero())
                        {
                            changeState(new HyperZeroStart(player.zeroHyperMode), true);
                        }
                        else
                        {
                            zeroShoot(chargeLevel);
                        }
                    }
                }
                stopCharge();
            }
            chargeLogic();

            Helpers.decrementTime(ref zeroLemonCooldown);
            if (player.isZBusterZero())
            {
                if (charState.canShoot() && !isCharging())
                {
                    var shootPressed = player.input.isPressed(Control.Shoot, player);
                    if (shootPressed)
                    {
                        lastShootPressed = Global.frameCount;
                    }

                    int framesSinceLastShootPressed = Global.frameCount - lastShootPressed;
                    int framesSinceLastShootReleased = Global.frameCount - lastShootReleased;
                    var shootHeld = player.input.isHeld(Control.Shoot, player);

                    if (shootPressed || (framesSinceLastShootPressed < Global.normalizeFrames(6) && framesSinceLastShootReleased > Global.normalizeFrames(30)))
                    {
                        if (stockedXSaber)
                        {
                            if (!doubleBusterDone)
                            {
                                changeState(new ZeroDoubleBuster(true, false), true);
                            }
                            else if (xSaberCooldown == 0)
                            {
                                swingStockedSaber();
                            }
                            return;
                        }

                        if (zeroLemonCooldown == 0)
                        {
                            zeroLemonCooldown = 0.15f;
                            zeroShoot(0);
                            return;
                        }
                    }
                }

                if (player.input.isPressed(Control.Special1, player))
                {
                    if (charState.canAttack())
                    {
                        if (xSaberCooldown == 0)
                        {
                            xSaberCooldown = 1;
                            if (stockedXSaber)
                            {
                                swingStockedSaber();
                            }
                            else
                            {
                                changeState(new ZSaberProjSwingState(grounded, false), true);
                            }
                        }
                    }
                }

                if (player.input.isWeaponLeftOrRightHeld(player) && player.scrap >= 10 && !isBlackZero2() && charState is not HyperZeroStart && invulnTime == 0 && rideChaser == null && rideArmor == null && charState is not WarpIn)
                {
                    hyperProgress += Global.spf;
                }
                else
                {
                    hyperProgress = 0;
                }

                if (hyperProgress >= 1 && player.scrap >= 10 && !isBlackZero2())
                {
                    hyperProgress = 0;
                    changeState(new HyperZeroStart(0), true);
                }

                return;
            }

            // Cutoff point for non-zero buster loadouts

            bool lenientAttackPressed = (attackPressed || framesSinceLastAttack < 5);
            bool spcPressed = player.input.isPressed(Control.Special1, player);
            bool spcActivated = false;

            if (hyorogaCooldown > 0 && charState is HyorogaState)
            {
                lenientAttackPressed = false;
            }

            if (lenientAttackPressed && charState is HyorogaState)
            {
                hyorogaCooldown = maxHyorogaCooldown;
            }

            if (player.isDisguisedAxl && player.axlWeapon is UndisguiseWeapon)
            {
                lenientAttackPressed = false;
                spcPressed = false;
            }

            bool isMidairRising = ((lenientAttackPressed && player.zeroUppercutWeaponA is RisingWeapon) || (spcPressed && player.zeroUppercutWeaponS is RisingWeapon)) && canAirDash() && flag == null;

            bool notUpLogic = !player.input.isHeld(Control.Up, player) || !isMidairRising;
            if (player.zeroAirSpecialWeapon.type != (int)AirSpecialType.Kuuenbu && spcPressed && !player.input.isHeld(Control.Down, player) && !isAttacking() && !player.hasKnuckle() && 
                (charState is Jump || charState is Fall || charState is WallKick) && !isInvulnerableAttack() &&
                (player.zeroUppercutWeaponS.type != (int)RyuenjinType.Rising || !player.input.isHeld(Control.Up, player)))
            {
                player.zeroAirSpecialWeapon.attack(this);
            }
            else if (lenientAttackPressed && !player.hasKnuckle() && charState.canAttack() && !isAttacking() && notUpLogic && !player.input.isHeld(Control.Down, player) &&
                (charState is Idle || charState is Crouch || charState is Run || charState is Dash || charState is AirDash || charState is Jump || charState is Fall))
            {
                if (stockedXSaber)
                {
                    if (xSaberCooldown == 0)
                    {
                        xSaberCooldown = 1;
                        stockSaber(false);
                        changeState(new ZSaberProjSwingState(grounded, true), true);
                    }
                    return;
                }
                else if ((charState is Idle || charState is Crouch || charState is Run || charState is Dash) && isAwakenedGenmuZero())
                {
                    if (genmuCooldown == 0 && xSaberCooldown < 0.5f)
                    {
                        genmuCooldown = 2;
                        changeState(new GenmuState(), true);
                    }
                    return;
                }
                else if (isAwakenedZero())
                {
                    if (xSaberCooldown == 0 && genmuCooldown < 1)
                    {
                        xSaberCooldown = 1f;
                        changeState(new ZSaberProjSwingState(grounded, true), true);
                    }
                    return;
                }
            }

            if (charState.canAttack() && charState is Idle && !player.hasKnuckle() && ((sprite.name == "zero_attack" && framePercent > 0.7f) || (sprite.name == "zero_attack2" && framePercent > 0.55f)) && spcPressed &&
                !player.input.isHeld(Control.Up, player) && !player.input.isHeld(Control.Down, player))
            {
                player.raijingekiWeapon.attack2(this);
            }
            else if (charState.canAttack() && charState is Idle && player.hasKnuckle() && ((sprite.name == "zero_punch" && framePercent > 0.6f) || (sprite.name == "zero_punch2" && framePercent > 0.6f)) && spcPressed &&
                !player.input.isHeld(Control.Up, player) && !player.input.isHeld(Control.Down, player))
            {
                player.raijingekiWeapon.attack2(this);
            }
            else if (charState.canAttack() && (spcPressed || lenientAttackPressed) && !isAttacking())
            {
                if (charState is Idle || charState is Run || charState is Crouch)
                {
                    if (player.input.isHeld(Control.Up, player))
                    {
                        spcActivated = true;
                        if (ryuenjinCooldown <= 0)
                        {
                            changeState(new Ryuenjin(lenientAttackPressed ? player.zeroUppercutWeaponA : player.zeroUppercutWeaponS, isUnderwater()), true);
                        }
                    }
                    else if (player.input.isHeld(Control.Down, player))
                    {
                        if (spcPressed && rakuhouhaCooldown == 0 && flag == null)
                        {
                            spcActivated = true;

                            float gigaAmmoUsage = gigaWeapon.getAmmoUsage(0);
                            if (gigaWeapon.ammo >= gigaAmmoUsage)
                            {
                                player.zeroGigaAttackWeapon.addAmmo(-gigaAmmoUsage, player);
                                if (player.zeroGigaAttackWeapon is RekkohaWeapon)
                                {
                                    changeState(new Rekkoha(player.zeroGigaAttackWeapon), true);
                                }
                                else
                                {
                                    changeState(new Rakuhouha(player.zeroGigaAttackWeapon), true);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!lenientAttackPressed)
                        {
                            player.raijingekiWeapon.attack(this);
                        }
                    }
                }
                else if ((charState is Jump || charState is Fall || charState is WallKick))
                {
                    if (player.input.isHeld(Control.Up, player) && isMidairRising)
                    {
                        spcActivated = true;
                        if (ryuenjinCooldown <= 0)
                        {
                            changeState(new Ryuenjin(player.zeroUppercutWeaponA is RisingWeapon ? player.zeroUppercutWeaponA : player.zeroUppercutWeaponS, isUnderwater()), true);
                        }
                    }
                    else if (player.input.isHeld(Control.Down, player))
                    {
                        spcActivated = true;
                        if (hyouretsuzanCooldown <= 0)
                        {
                            if (!player.hasKnuckle()) changeState(new Hyouretsuzan(lenientAttackPressed ? player.zeroDownThrustWeaponA : player.zeroDownThrustWeaponS), true);
                            else changeState(new DropKickState(), true);
                        }
                    }
                    else if ((Options.main.swapAirAttacks || airAttackCooldown == 0) && !lenientAttackPressed && !player.hasKnuckle())
                    {
                        if (player.zeroAirSpecialWeapon.type == (int)AirSpecialType.Kuuenbu || !spcPressed)
                        {
                            playSound("saber1", sendRpc: true);
                            changeState(new Fall(), true);
                            changeSprite(Options.main.getSpecialAirAttack(), true);
                            if (!Options.main.swapAirAttacks) airAttackCooldown = maxAirAttackCooldown;
                        }
                    }

                }
                else if (charState is Dash && !lenientAttackPressed)
                {
                    if (!player.hasKnuckle())
                    {
                        if (dashAttackCooldown > 0) return;
                        dashAttackCooldown = maxDashAttackCooldown;
                        slideVel = xDir * getDashSpeed() * getRunSpeed();
                        changeState(new Idle(), true);
                        playSound("saber1", sendRpc: true);
                        changeSprite("zero_attack_dash2", true);
                    }
                    else
                    {
                        if (dashAttackCooldown > 0) return;
                        changeState(new ZeroSpinKickState(), true);
                        return;
                    }
                }
            }

            if (charState.canAttack() && !spcActivated && lenientAttackPressed)
            {
                if (!isAttacking())
                {
                    if (charState is LadderClimb)
                    {
                        if (player.input.isHeld(Control.Left, player))
                        {
                            xDir = -1;
                        }
                        else if (player.input.isHeld(Control.Right, player))
                        {
                            xDir = 1;
                        }
                    }
                    var attackSprite = charState.attackSprite;
                    if (player.hasKnuckle())
                    {
                        attackSprite = attackSprite.Replace("attack", "punch");
                        string attackSound = "punch1";
                        if (charState is Jump || charState is Fall || charState is WallKick) attackSprite = "kick_air";
                        if (charState is Dash)
                        {
                            if (dashAttackCooldown > 0) return;
                            changeState(new ZeroSpinKickState(), true);
                            return;
                        }
                        if (charState is Crouch)
                        {
                            return;
                        }
                        if (Global.spriteNames.Contains(getSprite(attackSprite)))
                        {
                            playSound(attackSound, sendRpc: true);
                        }
                        if (charState is Run) changeState(new Idle(), true);
                        else if (charState is Jump) changeState(new Fall(), true);
                    }
                    else
                    {
                        if (charState is Run) changeState(new Idle(), true);
                        else if (charState is Jump) changeState(new Fall(), true);
                        else if (charState is Dash)
                        {
                            if (dashAttackCooldown > 0) return;
                            dashAttackCooldown = maxDashAttackCooldown;
                            slideVel = xDir * getDashSpeed() * getRunSpeed();
                            changeState(new Idle(), true);
                        }
                        if (charState is Fall)
                        {
                            if (Options.main.swapAirAttacks)
                            {
                                if (airAttackCooldown > 0) return;
                                airAttackCooldown = maxAirAttackCooldown;
                            }
                        }
                    }
                    changeSprite(getSprite(attackSprite), true);

                    if (!player.hasKnuckle())
                    {
                        if (stockedXSaber || isAwakenedZero())
                        {
                            stockSaber(false);
                            playSound("saberShot", sendRpc: true);
                            if (zSaberShotCooldown == 0)
                            {
                                zSaberShotCooldown = maxZSaberShotCooldown;
                                Global.level.delayedActions.Add(new DelayedAction(() =>
                                {
                                    new ZSaberProj(new ZSaber(player), pos.addxy(30 * getShootXDir(), -20), getShootXDir(), player, player.getNextActorNetId(), rpc: true);
                                }, 0.1f));
                            }
                        }
                        else
                        {
                            playSound("saber1", sendRpc: true);
                        }
                    }
                }
                else if (charState is Idle && sprite.name == "zero_attack" && framePercent > 0.4f)
                {
                    playSound("saber2", sendRpc: true);
                    changeSprite("zero_attack2", true);
                    turnToInput(player.input, player);
                }
                else if (charState is Idle && sprite.name == "zero_attack2" && framePercent > 0.4f)
                {
                    playSound("saber3", sendRpc: true);
                    changeSprite("zero_attack3", true);
                    turnToInput(player.input, player);
                }
                else if (charState is Idle && sprite.name == "zero_punch" && framePercent > 0.4f)
                {
                    changeSprite("zero_punch2", true);
                    playSound("punch2");
                    turnToInput(player.input, player);
                }
            }

            if (isAttacking())
            {
                if (isAnimOver() && charState is not ZSaberProjSwingState)
                {
                    changeSprite(getSprite(charState.defaultSprite), true);
                    if (charState is WallSlide)
                    {
                        frameIndex = sprite.frames.Count - 1;
                    }
                }
            }
        }

        public void swingStockedSaber()
        {
            xSaberCooldown = 1;
            doubleBusterDone = false;
            stockSaber(false);
            changeState(new ZSaberProjSwingState(grounded, true), true);
        }

        // This can run on both owners and non-owners. So data used must be in sync
        public Projectile getZeroProjFromHitbox(Collider collider, Point centerPoint)
        {
            Projectile proj = null;

            if (sprite.name == "zero_attack") proj = new GenericMeleeProj(player.zSaberWeapon, centerPoint, ProjIds.ZSaber1, player, 2, 0, 0.25f, isReflectShield: true);
            else if (sprite.name == "zero_attack2") proj = new GenericMeleeProj(player.zSaberWeapon, centerPoint, ProjIds.ZSaber2, player, 2, 0, 0.25f, isReflectShield: true);
            else if (sprite.name == "zero_attack3")
            {
                float timeSinceStart = zero3SwingComboEndTime - zero3SwingComboStartTime;
                float overrideDamage = 4;
                int overrideFlinch = Global.defFlinch;
                if (timeSinceStart < 0.4f)
                {
                    overrideDamage = 2;
                    overrideFlinch = Global.halfFlinch;
                }
                else if (timeSinceStart < 0.5f)
                {
                    overrideDamage = 3;
                    overrideFlinch = Global.defFlinch;
                }
                proj = new GenericMeleeProj(player.zSaberWeapon, centerPoint, ProjIds.ZSaber3, player, overrideDamage, overrideFlinch, 0.25f, isReflectShield: true);
            }
            else if (sprite.name == "zero_punch") proj = new GenericMeleeProj(player.kKnuckleWeapon, centerPoint, ProjIds.KKnuckle, player, 2, 0, 0.25f);
            else if (sprite.name == "zero_punch2") proj = new GenericMeleeProj(player.kKnuckleWeapon, centerPoint, ProjIds.KKnuckle2, player, 2, Global.halfFlinch, 0.25f);
            else if (sprite.name == "zero_spinkick") proj = new GenericMeleeProj(player.kKnuckleWeapon, centerPoint, ProjIds.KKnuckle2, player, 2, Global.halfFlinch, 0.5f);
            else if (sprite.name == "zero_kick_air") proj = new GenericMeleeProj(player.kKnuckleWeapon, centerPoint, ProjIds.KKnuckleAirKick, player, 3, 0, 0.25f);
            else if (sprite.name == "zero_parry_start") proj = new GenericMeleeProj(new KKnuckleParry(), centerPoint, ProjIds.KKnuckleParryStart, player, 0, Global.defFlinch, 0.25f);
            else if (sprite.name == "zero_parry")
            {
                proj = new GenericMeleeProj(new KKnuckleParry(), centerPoint, ProjIds.KKnuckleParry, player, 4, Global.defFlinch, 0.25f);
            }
            else if (sprite.name == "zero_shoryuken") proj = new GenericMeleeProj(player.zeroUppercutWeaponA, centerPoint, ProjIds.KKnuckleShoryuken, player, 4, Global.defFlinch, 0.25f);
            else if (sprite.name == "zero_megapunch") proj = new GenericMeleeProj(player.raijingekiWeapon, centerPoint, ProjIds.KKnuckleMegaPunch, player, 6, Global.defFlinch, 0.25f);
            else if (sprite.name == "zero_dropkick") proj = new GenericMeleeProj(player.zeroDownThrustWeaponA, centerPoint, ProjIds.KKnuckleDropKick, player, 4, Global.halfFlinch, 0.25f);
            else if (sprite.name == "zero_hyoroga_attack") proj = new GenericMeleeProj(player.zeroAirSpecialWeapon, centerPoint, ProjIds.HyorogaSwing, player, 4, 0, 0.25f);
            else if (sprite.name == "zero_attack_dash") proj = new GenericMeleeProj(player.zSaberWeapon, centerPoint, ProjIds.ZSaberdash, player, 2, 0, 0.25f, isReflectShield: true);
            else if (sprite.name == "zero_attack_dash2") proj = new GenericMeleeProj(player.zSaberWeapon, centerPoint, ProjIds.Shippuuga, player, 2, Global.halfFlinch, 0.25f);
            else if (sprite.name == "zero_attack_air") proj = new GenericMeleeProj(player.zSaberWeapon, centerPoint, ProjIds.ZSaberair, player, 2, 0, 0.25f, isReflectShield: true);
            else if (sprite.name == "zero_attack_air2") proj = new GenericMeleeProj(player.zSaberWeapon, centerPoint, ProjIds.ZSaberair, player, 1, 0, 0.125f, isDeflectShield: true);
            else if (sprite.name == "zero_ladder_attack") proj = new GenericMeleeProj(player.zSaberWeapon, centerPoint, ProjIds.ZSaberladder, player, 3, 0, 0.25f, isReflectShield: true);
            else if (sprite.name == "zero_wall_slide_attack") proj = new GenericMeleeProj(player.zSaberWeapon, centerPoint, ProjIds.ZSaberslide, player, 3, 0, 0.25f, isReflectShield: true);
            else if (sprite.name == "zero_attack_crouch") proj = new GenericMeleeProj(player.zSaberWeapon, centerPoint, ProjIds.ZSabercrouch, player, 3, 0, 0.25f, isReflectShield: true);
            else if (sprite.name == "zero_raijingeki") proj = new GenericMeleeProj(player.raijingekiWeapon, centerPoint, ProjIds.Raijingeki, player, 2, Global.defFlinch, 0.06f);
            else if (sprite.name == "zero_raijingeki2") proj = new GenericMeleeProj(player.raijingeki2Weapon, centerPoint, ProjIds.Raijingeki2, player, 2, Global.defFlinch, 0.06f);
            else if (sprite.name == "zero_tbreaker") proj = new GenericMeleeProj(player.raijingekiWeapon, centerPoint, ProjIds.TBreaker, player, 6, Global.defFlinch, 0.5f);
            else if (sprite.name == "zero_ryuenjin") proj = new GenericMeleeProj(new RyuenjinWeapon(player), centerPoint, ProjIds.Ryuenjin, player, 4, 0, 0.2f);
            else if (sprite.name == "zero_eblade") proj = new GenericMeleeProj(new EBladeWeapon(player), centerPoint, ProjIds.EBlade, player, 3, Global.defFlinch, 0.1f);
            else if (sprite.name == "zero_rising")
            {
                //float overrideDamage = sprite.time > 0.1f ? 2 : 1;
                proj = new GenericMeleeProj(new RisingWeapon(player), centerPoint, ProjIds.Rising, player, 1, 0, 0.15f);
            }
            else if (sprite.name.Contains("hyouretsuzan")) proj = new GenericMeleeProj(new HyouretsuzanWeapon(player), centerPoint, ProjIds.Hyouretsuzan2, player, 4, 12, 0.5f);
            else if (sprite.name.Contains("rakukojin"))
            {
                float damage = 3 + Helpers.clamp(MathF.Floor(deltaPos.y * 0.8f), 0, 10);
                proj = new GenericMeleeProj(new RakukojinWeapon(player), centerPoint, ProjIds.Rakukojin, player, damage, 12, 0.5f);
            }
            else if (sprite.name.Contains("quakeblazer"))
            {
                proj = new GenericMeleeProj(new QuakeBlazerWeapon(player), centerPoint, ProjIds.QuakeBlazer, player, 2, 0, 0.5f);
            }
            else if (sprite.name.Contains("zero_projswing")) proj = new GenericMeleeProj(player.zSaberProjSwingWeapon, centerPoint, ProjIds.ZSaberProjSwing, player, isBlackZero2() ? 4 : 3, Global.defFlinch, 0.5f, isReflectShield: true);
            else if (sprite.name.Contains("zero_block") && !collider.isHurtBox())
            {
                proj = new GenericMeleeProj(player.zSaberWeapon, centerPoint, ProjIds.SwordBlock, player, 0, 0, 0, isDeflectShield: true);
            }

            return proj;
        }

        public List<BusterProj> zeroLemonsOnField = new List<BusterProj>();
        private void zeroShoot(int chargeLevel)
        {
            if (!player.isZBusterZero() && player.scrap <= 0) return;

            if (player.isZBusterZero() && chargeLevel == 0)
            {
                for (int i = zeroLemonsOnField.Count - 1; i >= 0; i--)
                {
                    if (zeroLemonsOnField[i].destroyed)
                    {
                        zeroLemonsOnField.RemoveAt(i);
                        continue;
                    }
                }
                if (zeroLemonsOnField.Count >= 3) return;
            }

            string zeroShootSprite = getSprite(charState.shootSprite);
            if (!Global.sprites.ContainsKey(zeroShootSprite))
            {
                if (grounded) zeroShootSprite = "zero_shoot";
                else zeroShootSprite = "zero_fall_shoot";
            }
            bool hasShootSprite = !string.IsNullOrEmpty(charState.shootSprite);
            if (shootAnimTime == 0)
            {
                if (hasShootSprite) changeSprite(zeroShootSprite, false);
                else
                {
                    if (!(charState is Crouch))
                    {
                        return;
                    }
                }
            }
            else if (charState is Idle)
            {
                frameIndex = 0;
                frameTime = 0;
            }
            if (charState is LadderClimb)
            {
                if (player.input.isHeld(Control.Left, player))
                {
                    this.xDir = -1;
                }
                else if (player.input.isHeld(Control.Right, player))
                {
                    this.xDir = 1;
                }
            }

            //Sometimes transitions cause the shoot sprite not to be played immediately, so force it here
            if (currentFrame.getBusterOffset() == null)
            {
                if (hasShootSprite) changeSprite(zeroShootSprite, false);
            }

            if (hasShootSprite) shootAnimTime = 0.3f;
            Point shootPos = getShootPos();
            int xDir = getShootXDir();

            if (isAwakenedZero())
            {
                var proj = new ShingetsurinProj(new Shingetsurin(player), shootPos, xDir, 0, player, player.getNextActorNetId(), rpc: true);
                playSound("saber3", sendRpc: true);
                player.scrap -= 1;
                if (player.scrap < 0) player.scrap = 0;
                if (chargeLevel >= 2)
                {
                    Global.level.delayedActions.Add(new DelayedAction(() => 
                    {
                        var proj = new ShingetsurinProj(new Shingetsurin(player), getShootPos(), xDir, 0.15f, player, player.getNextActorNetId(), rpc: true);
                        playSound("saber3", sendRpc: true);
                    }, 0.15f));
                }
                if (chargeLevel >= 3)
                {
                    Global.level.delayedActions.Add(new DelayedAction(() =>
                    {
                        var proj = new ShingetsurinProj(new Shingetsurin(player), getShootPos(), xDir, 0.3f, player, player.getNextActorNetId(), rpc: true);
                        playSound("saber3", sendRpc: true);
                    }, 0.3f));
                }
            }
            else
            {
                int type = player.isZBusterZero() ? 1 : 0;

                if (stockedCharge)
                {
                    changeState(new ZeroDoubleBuster(true, true), true);
                }
                else if (chargeLevel == 0)
                {
                    playSound("buster", sendRpc: true);
                    zeroLemonCooldown = 0.15f;
                    var lemon = new BusterProj(player.zeroBusterWeapon, shootPos, xDir, 1, player, player.getNextActorNetId(), rpc: true);
                    zeroLemonsOnField.Add(lemon);
                }
                else if (chargeLevel == 1)
                {
                    if (type == 0) player.scrap -= 1;
                    playSound("zbuster2", sendRpc: true);
                    zeroLemonCooldown = 0.375f;
                    new ZBuster2Proj(player.zeroBusterWeapon, shootPos, xDir, type, player, player.getNextActorNetId(), rpc: true);
                }
                else if (chargeLevel == 2)
                {
                    if (type == 0) player.scrap -= 1;
                    zeroLemonCooldown = 0.375f;
                    if (!player.isZBusterZero())
                    {
                        playSound("zbuster3", sendRpc: true);
                        new ZBuster3Proj(player.zeroBusterWeapon, shootPos, xDir, type, player, player.getNextActorNetId(), rpc: true);
                    }
                    else
                    {
                        playSound("zbuster3", sendRpc: true);
                        new ZBuster4Proj(player.zeroBusterWeapon, shootPos, xDir, type, player, player.getNextActorNetId(), rpc: true);
                    }
                }
                else if (chargeLevel == 3 || chargeLevel == 4)
                {
                    if (type == 0) player.scrap -= 1;
                    if (chargeLevel == 3 && player.isZBusterZero())
                    {
                        changeState(new ZeroDoubleBuster(false, true), true);
                        //playSound("zbuster2", sendRpc: true);
                        //new ZBuster2Proj(player.zeroBusterWeapon, shootPos, xDir, type, player, player.getNextActorNetId(), rpc: true);
                        //stockedCharge = true;
                    }
                    else if (chargeLevel == 4 && canUseDoubleBusterCombo())
                    {
                        //if (!isBlackZero2()) player.scrap -= 1;
                        changeState(new ZeroDoubleBuster(false, false), true);
                    }
                    else
                    {
                        playSound("zbuster4", sendRpc: true);
                        zeroLemonCooldown = 0.375f;
                        new ZBuster4Proj(player.zeroBusterWeapon, shootPos, xDir, type, player, player.getNextActorNetId(), rpc: true);
                    }
                }
            }

            chargeTime = 0;
            //saberCooldown = 0.5f;
        }

        public bool canUseDoubleBusterCombo()
        {
            //if (isBlackZero()) return true;
            if (player.isZBusterZero()) return true;
            return false;
        }

        public bool isCrouchSlashing()
        {
            return charState is Crouch && isAttacking();
        }

        public bool isHyperZero()
        {
            return isBlackZero() || isAwakenedZeroBS.getValue();
        }

        // isBlackZero below can be used by non-owners as these times are sync'd
        public bool isBlackZero()
        {
            return player.isZero && blackZeroTime > 0 && !player.isZBusterZero();
        }

        public bool isBlackZero2()
        {
            return player.isZero && blackZeroTime > 0 && player.isZBusterZero();
        }

        // These methods below can't be used by non-owners of the character since the times aren't sync'd. Use the BS states instead
        public bool isAwakenedZero()
        {
            return player.isZero && awakenedZeroTime > 0;
        }

        public bool isAwakenedGenmuZero()
        {
            return player.isZero && awakenedZeroTime > 30;
        }

        float awakenedScrapTime;
        float awakenedAuraAnimTime;
        int awakenedAuraFrame;
        public void updateAwakenedZero()
        {
            awakenedZeroTime += Global.spf;
            awakenedScrapTime += Global.spf;
            int scrapDeductTime = 2;
            if (isAwakenedGenmuZero()) scrapDeductTime = 1;

            if (awakenedScrapTime > scrapDeductTime)
            {
                awakenedScrapTime = 0;
                player.scrap--;
                if (player.scrap <= 0)
                {
                    player.scrap = 0;
                    awakenedZeroTime = 0;
                }
            }

            updateAwakenedAura();
        }

        int lastAwakenedAuraFrameUpdate;
        public void updateAwakenedAura()
        {
            if (lastAwakenedAuraFrameUpdate == Global.frameCount) return;
            lastAwakenedAuraFrameUpdate = Global.frameCount;
            awakenedAuraAnimTime += Global.spf;
            if (awakenedAuraAnimTime > 0.06f)
            {
                awakenedAuraAnimTime = 0;
                awakenedAuraFrame++;
                if (awakenedAuraFrame > 3) awakenedAuraFrame = 0;
            }
        }
    }

    public class HyperZeroStart : CharState
    {
        public float radius = 200;
        public float time;
        Anim drWilyAnim;
        public HyperZeroStart(int type) : base(type == 1 ? "hyper_start2" : "hyper_start", "", "", "")
        {
            invincible = true;
        }

        public override void update()
        {
            base.update();
            if (time == 0)
            {
                if (radius >= 0)
                {
                    radius -= Global.spf * 200;
                }
                else
                {
                    time = Global.spf;
                    radius = 0;
                    if (player.isZBusterZero())
                    {
                        character.blackZeroTime = 9999;
                        RPC.actorToggle.sendRpc(character.netId, RPCActorToggleType.ActivateBlackZero2);
                    }
                    else if (player.zeroHyperMode == 0)
                    {
                        character.blackZeroTime = character.maxHyperZeroTime + 1;
                        RPC.setHyperZeroTime.sendRpc(character.player.id, character.blackZeroTime, 0);
                    }
                    else if (player.zeroHyperMode == 1)
                    {
                        character.awakenedZeroTime = Global.spf;
                        RPC.setHyperZeroTime.sendRpc(character.player.id, character.awakenedZeroTime, 2);
                    }
                    else if (player.zeroHyperMode == 2)
                    {
                        character.isNightmareZero = true;
                    }
                    character.playSound("ching");
                    character.fillHealthToMax();
                }
            }
            else
            {
                time += Global.spf;
                if (time > 1)
                {
                    character.changeState(new Idle(), true);
                }
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            if (!character.hyperZeroUsed)
            {
                if (player.isZBusterZero())
                {
                    character.player.scrap -= 10;
                }
                else if (player.zeroHyperMode == 0)
                {
                    character.player.scrap -= 10;
                }
                else if (player.zeroHyperMode == 1)
                {
                    drWilyAnim = new Anim(character.pos.addxy(30 * character.xDir, -30), "drwily", -character.xDir, player.getNextActorNetId(), false, sendRpc: true);
                    drWilyAnim.fadeIn = true;
                    drWilyAnim.blink = true;
                    character.player.awakenedScrapEnd = (character.player.scrap - 10);
                }
                else if (player.zeroHyperMode == 2)
                {
                    drWilyAnim = new Anim(character.pos.addxy(30 * character.xDir, -30), "gate", -character.xDir, player.getNextActorNetId(), false, sendRpc: true);
                    drWilyAnim.fadeIn = true;
                    drWilyAnim.blink = true;
                    character.player.awakenedScrapEnd = (character.player.scrap - 10);
                }
                character.hyperZeroUsed = true;
            }
            character.useGravity = false;
            character.vel = new Point();
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.useGravity = true;
            drWilyAnim?.destroySelf();
            if (character != null)
            {
                character.invulnTime = 0.5f;
            }
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            Point pos = character.getCenterPos();
            DrawWrappers.DrawCircle(pos.x + x, pos.y + y, radius, false, Color.White, 5, character.zIndex + 1, true, Color.White);
        }
    }

    public class KKnuckleParry : Weapon
    {
        public KKnuckleParry() : base()
        {
            rateOfFire = 0.75f;
            index = (int)WeaponIds.KKnuckleParry;
            killFeedIndex = 172;
        }
    }

    public class KKnuckleParryStartState : CharState
    {
        public KKnuckleParryStartState() : base("parry_start", "", "", "")
        {
            superArmor = true;
        }

        public override void update()
        {
            base.update();

            if (stateTime < 0.1f)
            {
                character.turnToInput(player.input, player);
            }

            if (character.isAnimOver())
            {
                character.changeToIdleOrFall();
            }
        }

        public void counterAttack(Player damagingPlayer, Actor damagingActor, float damage)
        {
            Actor counterAttackTarget = null;
            if (damagingActor is GenericMeleeProj gmp)
            {
                counterAttackTarget = gmp.owningActor;
            }

            if (counterAttackTarget == null)
            {
                counterAttackTarget = damagingPlayer?.character ?? damagingActor;
            }

            var proj = damagingActor as Projectile;
            bool stunnableParry = proj != null && proj.canBeParried();
            if (counterAttackTarget != null && character.pos.distanceTo(counterAttackTarget.pos) < 75 && counterAttackTarget is Character chr && stunnableParry)
            {
                if (!chr.ownedByLocalPlayer)
                {
                    RPC.actorToggle.sendRpc(chr.netId, RPCActorToggleType.ChangeToParriedState);
                }
                else
                {
                    chr.changeState(new ParriedState(), true);
                }
            }

            character.playSound("zeroParry", sendRpc: true);
            character.changeState(new KKnuckleParryMeleeState(counterAttackTarget), true);
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.parryCooldown = character.maxParryCooldown;
        }

        public bool canParry(Actor damagingActor)
        {
            var proj = damagingActor as Projectile;
            if (proj == null)
            {
                return false;
            }
            return character.frameIndex == 0;
        }
    }

    public class KKnuckleParryMeleeState : CharState
    {
        Actor counterAttackTarget;
        Point counterAttackPos;
        public KKnuckleParryMeleeState(Actor counterAttackTarget) : base("parry", "", "", "")
        {
            invincible = true;
            this.counterAttackTarget = counterAttackTarget;
        }

        public override void update()
        {
            base.update();

            if (counterAttackTarget != null)
            {
                character.turnToPos(counterAttackPos);
                float dist = character.pos.distanceTo(counterAttackPos);
                if (dist < 150)
                {
                    if (character.frameIndex >= 1 && !once)
                    {
                        if (dist > 5)
                        {
                            var destPos = Point.lerp(character.pos, counterAttackPos, Global.spf * 5);
                            character.changePos(destPos);
                        }
                    }
                }
            }

            if (character.isAnimOver())
            {
                character.changeToIdleOrFall();
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            if (counterAttackTarget != null)
            {
                counterAttackPos = counterAttackTarget.pos.addxy(character.xDir * 30, 0);
            }
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.parryCooldown = character.maxParryCooldown;
        }
    }
}
