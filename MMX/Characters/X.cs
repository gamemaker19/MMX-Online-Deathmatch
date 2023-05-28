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
        public float shotgunIceChargeTime = 0;
        public float shotgunIceChargeCooldown = 0;
        public int shotgunIceChargeMod = 0;
        public bool fgMotion;
        public bool isHyperX;

        public const int headArmorCost = 1;
        public const int bodyArmorCost = 3;
        public const int armArmorCost = 4;
        public const int bootsArmorCost = 2;

        public RollingShieldProjCharged chargedRollingShieldProj;
        public ShotgunIceProjSled iceSled;
        public List<BubbleSplashProjCharged> chargedBubbles = new List<BubbleSplashProjCharged>();
        public StrikeChainProj strikeChainProj;
        public GravityWellProjCharged chargedGravityWell;
        public RayGunAltProj rayGunAltProj;
        public PlasmaGunAltProj plasmaGunAltProj;
        public SpinningBladeProjCharged chargedSpinningBlade;
        public FrostShieldProjCharged chargedFrostShield;
        public TunnelFangProjCharged chargedTunnelFang;
        public GaeaShieldProj gaeaShield;
        public GravityWellProj gravityWell;
        public int totalChipHealAmount;
        public const int maxTotalChipHealAmount = 32;
        public int unpoShotCount;

        public float hadoukenCooldownTime;
        public float maxHadoukenCooldownTime = 1f;
        public float shoryukenCooldownTime;
        public float maxShoryukenCooldownTime = 1f;
        public ShaderWrapper xPaletteShader;

        public float streamCooldown;
        int lastShootPressed;
        int lastShootReleased;
        float noDamageTime;
        float rechargeHealthTime;
        public float scannerCooldown;
        float UPDamageCooldown;
        public float unpoDamageMaxCooldown = 2;
        float unpoTime;

        public int cStingPaletteIndex;
        public float cStingPaletteTime;
        bool lastFrameSpecialHeld;
        bool lastShotWasSpecialBuster;
        public float upPunchCooldown;
        public Projectile unpoAbsorbedProj;

        public bool canShootSpecialBuster()
        {
            if (isHyperX && (charState is Dash || charState is AirDash)) return false;
            return player.isSpecialBuster() && !Buster.isNormalBuster(player.weapon) && !isInvisibleBS.getValue() && player.armorFlag == 0 && streamCooldown == 0;
        }

        public bool canShootSpecialBusterOnBuster()
        {
            return player.isSpecialBuster() && !isInvisibleBS.getValue() && player.armorFlag == 0;
        }

        public void refillUnpoBuster()
        {
            if (player.weapons.Count > 0) player.weapons[0].ammo = 32;
        }

        public void updateX()
        {
            if (hasFgMoveEquipped())
            {
                player.fgMoveAmmo += Global.spf;
                if (player.fgMoveAmmo > 32) player.fgMoveAmmo = 32;
            }
            Helpers.decrementTime(ref xSaberCooldown);
            Helpers.decrementTime(ref scannerCooldown);
            Helpers.decrementTime(ref hadoukenCooldownTime);
            Helpers.decrementTime(ref shoryukenCooldownTime);
            Helpers.decrementTime(ref streamCooldown);
            if (player.weapon.ammo >= player.weapon.maxAmmo)
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
                    player.weapon.ammo = Helpers.clampMax(player.weapon.ammo + 1, player.weapon.maxAmmo);
                    playSound("heal", forcePlay: true);
                }
            }

            if (shootAnimTime > 0 && strikeChainProj == null && !charState.isGrabbing)
            {
                shootAnimTime -= Global.spf;
                if (shootAnimTime <= 0)
                {
                    shootAnimTime = 0;
                    changeSpriteFromName(charState.sprite, false);
                    if (charState is WallSlide)
                    {
                        frameIndex = sprite.frames.Count - 1;
                    }
                }
            }

            if (player.hasChip(2) && !isInvisible() && totalChipHealAmount < maxTotalChipHealAmount)
            {
                noDamageTime += Global.spf;
                if ((player.health < player.maxHealth || player.hasSubtankCapacity()) && noDamageTime > 4)
                {
                    rechargeHealthTime -= Global.spf;
                    if (rechargeHealthTime <= 0)
                    {
                        rechargeHealthTime = 1;
                        addHealth(1);
                        totalChipHealAmount++;
                    }
                }
            }

            Point inputDir = player.input.getInputDir(player);

            if (!isHyperX && canShoot() && charState is not Die && charState is not Hurt && charState?.canShoot() == true)
            {
                if (Global.level.is1v1() && player.weapons.Count == 10)
                {
                    if (player.weaponSlot != 9)
                    {
                        player.weapons[9].update();
                    }

                    if (player.input.isPressed(Control.Special1, player) && chargeTime == 0)
                    {
                        int oldSlot = player.weaponSlot;
                        player.changeWeaponSlot(9);
                        if (shootTime <= 0)
                        {
                            shoot(false);
                        }
                        player.weaponSlot = oldSlot;
                        player.changeWeaponSlot(oldSlot);
                    }
                }

                if (Options.main.gigaCrushSpecial && player.input.isPressed(Control.Special1, player) && player.input.isHeld(Control.Down, player) && player.weapons.Any(w => w is GigaCrush))
                {
                    int oldSlot = player.weaponSlot;
                    int gCrushSlot = player.weapons.FindIndex(w => w is GigaCrush);
                    player.changeWeaponSlot(gCrushSlot);
                    shoot(false);
                    player.weaponSlot = oldSlot;
                    player.changeWeaponSlot(oldSlot);
                }
                else if (Options.main.novaStrikeSpecial && player.input.isPressed(Control.Special1, player) && player.weapons.Any(w => w is NovaStrike) && !inputDir.isZero())
                {
                    int oldSlot = player.weaponSlot;
                    int novaStrikeSlot = player.weapons.FindIndex(w => w is NovaStrike);
                    player.changeWeaponSlot(novaStrikeSlot);
                    if (player.weapon.shootTime <= 0)
                    {
                        shoot(false);
                    }
                    player.weaponSlot = oldSlot;
                    player.changeWeaponSlot(oldSlot);
                }
            }

            if (charState is not Die && player.input.isPressed(Control.Special1, player) && player.hasAllX3Armor() && !player.hasGoldenArmor() && !player.hasUltimateArmor() && player.hasAnyChip())
            {
                if (player.input.isHeld(Control.Down, player))
                {
                    player.setChipNum(0, false);
                    Global.level.gameMode.setHUDErrorMessage(player, "Equipped foot chip.", playSound: false, resetCooldown: true);
                }
                else if (player.input.isHeld(Control.Up, player))
                {
                    player.setChipNum(2, false);
                    Global.level.gameMode.setHUDErrorMessage(player, "Equipped head chip.", playSound: false, resetCooldown: true);
                }
                else if (player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player))
                {
                    player.setChipNum(3, false);
                    Global.level.gameMode.setHUDErrorMessage(player, "Equipped arm chip.", playSound: false, resetCooldown: true);
                }
                else
                {
                    player.setChipNum(1, false);
                    Global.level.gameMode.setHUDErrorMessage(player, "Equipped body chip.", playSound: false, resetCooldown: true);
                }
            }

            Helpers.decrementTime(ref upPunchCooldown);

            if (isHyperX && !isInvulnerableAttack())
            {
                if (player.input.isPressed(Control.Shoot, player) &&
                    (charState is Idle || charState is Run || charState is Fall || charState is Jump || charState is Dash))
                {
                    if (unpoShotCount <= 0)
                    {
                        upPunchCooldown = 0.5f;
                        changeState(new XUPPunchState(grounded), true);
                        return;
                    }
                }
                else if (player.input.isPressed(Control.Special1, player) && !isInvisible() &&
                    (charState is Dash || charState is AirDash))
                {
                    charState.isGrabbing = true;
                    changeSpriteFromName("unpo_grab_dash", true);
                }
                else if (player.input.isPressed(Control.Special1, player) &&
                    (charState is Idle || charState is Run || charState is Fall || charState is Jump))
                {
                    upPunchCooldown = 0.5f;
                    changeState(new XUPPunchState(grounded), true);
                    return;
                }
                else if 
                (
                    player.input.isWeaponLeftOrRightPressed(player) && parryCooldown == 0 &&
                    (charState is Idle || charState is Run || charState is Fall || charState is Jump || charState is XUPPunchState || charState is XUPGrabState)
                )
                {
                    if (unpoAbsorbedProj != null)
                    {
                        changeState(new XUPParryProjState(unpoAbsorbedProj, true, false), true);
                        unpoAbsorbedProj = null;
                        return;
                    }
                    else
                    {
                        changeState(new XUPParryStartState(), true);
                    }
                }
            }

            if (player.isSpecialSaber() && canShoot() && canChangeWeapons() && player.armorFlag == 0 && player.input.isPressed(Control.Special1, player) && !isAttacking() && !isInvisible() && !charState.isGrabbing && !isHyperX &&
                (charState is Idle || charState is Run || charState is Fall || charState is Jump || charState is Dash))
            {
                if (xSaberCooldown == 0)
                {
                    xSaberCooldown = 1f;
                    changeState(new X6SaberState(grounded), true);
                    return;
                }
            }

            if (isHyperX)
            {
                if (unpoTime > 12) unpoDamageMaxCooldown = 1;
                if (unpoTime > 24) unpoDamageMaxCooldown = 0.75f;
                if (unpoTime > 36) unpoDamageMaxCooldown = 0.5f;
                if (unpoTime > 48) unpoDamageMaxCooldown = 0.25f;

                if (charState is not XUPGrabState && charState is not XUPParryMeleeState && charState is not XUPParryProjState)
                {
                    unpoTime += Global.spf;
                    UPDamageCooldown += Global.spf;
                    if (UPDamageCooldown > unpoDamageMaxCooldown)
                    {
                        UPDamageCooldown = 0;
                        applyDamage(null, null, 1, null);
                    }
                }
            }

            if (player.hasHelmetArmor(2) && scannerCooldown == 0 && canScan() && inputDir.isZero())
            {
                Point scanPos;
                Point? headPos = getHeadPos();
                if (headPos != null)
                {
                    scanPos = headPos.Value.addxy(xDir * 10, 0);
                }
                else
                {
                    scanPos = getCenterPos().addxy(0, -10);
                }
                CollideData hit = Global.level.raycast(scanPos, scanPos.addxy(getShootXDir() * 150, 0), new List<Type>() { typeof(Actor) });
                if (hit?.gameObject is Character chr && chr.player.alliance != player.alliance && !chr.player.scanned && !chr.isStealthy(player.alliance))
                {
                    new ItemTracer().getProjectile(scanPos, getShootXDir(), player, 0, player.getNextActorNetId());
                }
                else if (player.input.isPressed(Control.Special1, player))
                {
                    new ItemTracer().getProjectile(scanPos, getShootXDir(), player, 0, player.getNextActorNetId());
                }
            }

            player.busterWeapon.update();

            var oldWeapon = player.weapon;
            if (canShootSpecialBuster())
            {
                if (player.input.isHeld(Control.Special1, player))
                {
                    if (!lastFrameSpecialHeld)
                    {
                        lastFrameSpecialHeld = true;
                        if (isCharging())
                        {
                            stopCharge();
                        }
                    }
                }
                else
                {
                    if (lastFrameSpecialHeld)
                    {
                        player.isShootingSpecialBuster = true;
                    }
                    lastFrameSpecialHeld = false;
                }
            }
            else
            {
                lastFrameSpecialHeld = false;
                lastShotWasSpecialBuster = false;
                player.isShootingSpecialBuster = false;
            }

            if (charState.canShoot())
            {
                bool specialBusterOnBuster = Buster.isNormalBuster(player.weapon) && player.input.isPressed(Control.Special1, player) && canShootSpecialBusterOnBuster();
                var shootPressed = player.input.isPressed(Control.Shoot, player) || specialBusterOnBuster;
                if (shootPressed)
                {
                    lastShotWasSpecialBuster = false;
                    if (lastFrameSpecialHeld)
                    {
                        lastFrameSpecialHeld = false;
                        stopCharge();
                    }
                }
                else
                {
                    if (canShootSpecialBuster())
                    {
                        shootPressed = player.input.isPressed(Control.Special1, player);
                        if (shootPressed)
                        {
                            lastShotWasSpecialBuster = true;
                        }
                    }
                }

                if (shootPressed)
                {
                    lastShootPressed = Global.frameCount;
                }

                int framesSinceLastShootPressed = Global.frameCount - lastShootPressed;
                int framesSinceLastShootReleased = Global.frameCount - lastShootReleased;
                var shootHeld = player.input.isHeld(Control.Shoot, player);

                if (lastShotWasSpecialBuster) player.isShootingSpecialBuster = true;

                bool offCooldown = oldWeapon.shootTime == 0 && shootTime == 0;
                if (player.isShootingSpecialBuster)
                {
                    offCooldown = oldWeapon.shootTime < oldWeapon.rateOfFire * 0.5f && shootTime == 0;
                }

                bool shootCondition =
                    shootPressed ||
                    (framesSinceLastShootPressed < Global.normalizeFrames(6) && framesSinceLastShootReleased > Global.normalizeFrames(30)) ||
                    (shootHeld && player.weapon.isStream && chargeTime < charge1Time);
                if (!fgMotion && offCooldown && shootCondition)
                {
                    shoot(false);
                }

                if (!isHyperX)
                {
                    chargeControls();
                }
                else
                {
                    unpoChargeControls();
                }
            }
            else if (charState is Dash || charState is AirDash || charState is XUPParryMeleeState || charState is XUPParryProjState || charState is XUPParryStartState || charState is XUPGrabState)
            {
                if (isHyperX)
                {
                    unpoChargeControls();
                }
            }

            player.isShootingSpecialBuster = false;

            if (chargedSpinningBlade != null || chargedFrostShield != null || chargedTunnelFang != null)
            {
                changeSprite("mmx_" + charState.shootSprite, true);
            }

            if (!isHyperX)
            {
                player.changeWeaponControls();
            }

            chargeLogic();

            if (charState is Hurt || charState is Die)
            {
                shotgunIceChargeTime = 0;
            }

            if (string.IsNullOrEmpty(charState.shootSprite))
            {
                shotgunIceChargeTime = 0;
            }

            if (shotgunIceChargeTime > 0 && ownedByLocalPlayer)
            {
                changeSprite("mmx_" + charState.shootSprite, true);
                shotgunIceChargeTime -= Global.spf;
                var busterPos = getShootPos().addxy(xDir * 10, 0);
                if (shotgunIceChargeCooldown == 0)
                {
                    new ShotgunIceProjCharged(player.weapon, busterPos, xDir, player, shotgunIceChargeMod % 2, false, player.getNextActorNetId(), rpc: true);
                    shotgunIceChargeMod++;
                }
                shotgunIceChargeCooldown += Global.spf;
                if (shotgunIceChargeCooldown > 0.1)
                {
                    shotgunIceChargeCooldown = 0;
                }
                if (shotgunIceChargeTime < 0)
                {
                    shotgunIceChargeTime = 0;
                    shotgunIceChargeCooldown = 0;
                    changeSprite("mmx_" + charState.defaultSprite, true);
                }
            }

            if (isShootingRaySplasher && ownedByLocalPlayer)
            {
                changeSprite("mmx_" + charState.shootSprite, true);

                if (raySplasherCooldown > 0)
                {
                    raySplasherCooldown += Global.spf;
                    if (raySplasherCooldown >= 0.03f)
                    {
                        raySplasherCooldown = 0;
                    }
                }
                else
                {
                    var busterPos = getShootPos();
                    if (raySplasherCooldown2 == 0)
                    {
                        player.weapon.addAmmo(-0.15f, player);
                        raySplasherCooldown2 = 0.03f;
                        new RaySplasherProj(player.weapon, busterPos, getShootXDir(), raySplasherMod % 3, (raySplasherMod / 3) % 3, false, player, player.getNextActorNetId(), rpc: true);
                        raySplasherMod++;
                        if (raySplasherMod % 3 == 0)
                        {
                            if (raySplasherMod >= 21)
                            {
                                setShootRaySplasher(false);
                                changeSprite("mmx_" + charState.defaultSprite, true);
                            }
                            else
                            {
                                raySplasherCooldown = Global.spf;
                            }
                        }
                    }
                    else
                    {
                        raySplasherCooldown2 -= Global.spf;
                        if (raySplasherCooldown2 <= 0)
                        {
                            raySplasherCooldown2 = 0;
                        }
                    }
                }
            }

            if (iceSled != null && !Global.level.gameObjects.Contains(iceSled))
            {
                iceSled = null;
            }
            if (iceSled != null)
            {
                changePos(iceSled.pos.addxy(0, -16.1f));
            }
        }

        public void chargeControls()
        {
            if (player.chargeButtonHeld() && canCharge())
            {
                increaseCharge();

                if (player.weapon is ParasiticBomb && getChargeLevel() == 3)
                {
                    shoot(true);
                }
            }
            else
            {
                if (isCharging())
                {
                    if (player.weapon is AssassinBullet)
                    {
                        shootAssassinShot();
                    }
                    else
                    {
                        if (shootTime == 0)
                        {
                            shoot(true);
                        }
                        stopCharge();
                        lastShootReleased = Global.frameCount;
                    }
                }
                else if (!(charState is Hurt))
                {
                    stopCharge();
                }
            }
        }

        public void unpoChargeControls()
        {
            if (player.chargeButtonHeld() && canCharge())
            {
                increaseCharge();
                if (getChargeLevel() == 1) unpoShotCount = Math.Max(unpoShotCount, 1);
                if (getChargeLevel() == 2) unpoShotCount = Math.Max(unpoShotCount, 2);
                if (getChargeLevel() == 3) unpoShotCount = Math.Max(unpoShotCount, 3);
                if (getChargeLevel() == 4) unpoShotCount = Math.Max(unpoShotCount, 4);
            }
            else
            {
                if (isCharging())
                {
                    stopCharge();
                }
                else if (!(charState is Hurt))
                {
                    stopCharge();
                }
            }
        }

        public void shoot(bool doCharge)
        {
            int chargeLevel = getChargeLevel();
            if (!doCharge && chargeLevel == 3) return;

            if (isHyperX && unpoShotCount <= 0) return;

            if (!player.weapon.canShoot(chargeLevel, player))
            {
                return;
            }
            if (player.weapon is AssassinBullet || player.weapon is UndisguiseWeapon)
            {
                return;
            }
            if (player.weapon is HyperBuster hb)
            {
                var hyperChargeWeapon = player.weapons[player.hyperChargeSlot];
                shootTime = hb.getRateOfFire(player);
                if (hyperChargeWeapon is Sting || hyperChargeWeapon is RollingShield || hyperChargeWeapon is BubbleSplash || hyperChargeWeapon is ParasiticBomb)
                {
                    doCharge = true;
                    chargeLevel = 3;
                    hb.shootTime = hb.getRateOfFire(player);
                    hb.addAmmo(-hb.getAmmoUsage(3), player);
                    player.changeWeaponSlot(player.hyperChargeSlot);
                    if (hyperChargeWeapon is BubbleSplash bs)
                    {
                        bs.hyperChargeDelay = 0.25f;
                    }
                }
            }
            else
            {
                shootTime = player.weapon.rateOfFire;
            }

            if (chargeLevel == 2 || chargeLevel == 3)
            {
                var hbWep = player.weapons.FirstOrDefault(w => w is HyperBuster) as HyperBuster;
                if (hbWep != null)
                {
                    hbWep.shootTime = hbWep.getRateOfFire(player);
                }
            }

            if (stockedXSaber)
            {
                if (xSaberCooldown == 0)
                {
                    stockSaber(false);
                    changeState(new XSaberState(grounded), true);
                }
                return;
            }

            bool hasShootSprite = !string.IsNullOrEmpty(charState.shootSprite);
            if (shootAnimTime == 0)
            {
                if (hasShootSprite) changeSprite(getSprite(charState.shootSprite), false);
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
            if (charState is XUPGrabState)
            {
                changeToIdleOrFall();
            }

            //Sometimes transitions cause the shoot sprite not to be played immediately, so force it here
            if (currentFrame.getBusterOffset() == null)
            {
                if (hasShootSprite) changeSprite(getSprite(charState.shootSprite), false);
            }

            if (hasShootSprite) shootAnimTime = 0.3f;
            int xDir = getShootXDir();

            int cl = doCharge ? chargeLevel : 0;
            if (player.weapon is GigaCrush)
            {
                if (player.weapon.ammo < 16) cl = 0;
                else if (player.weapon.ammo >= 16 && player.weapon.ammo < 24) cl = 1;
                else if (player.weapon.ammo >= 24 && player.weapon.ammo < 32) cl = 2;
                else cl = 3;
            }
            if (Buster.isWeaponUnpoBuster(player.weapon))
            {
                cl = 2;
            }

            shootRpc(getShootPos(), player.weapon.index, xDir, cl, player.getNextActorNetId(), true);

            if (chargeLevel == 3 && player.hasGoldenArmor() && player.weapon is Buster)
            {
                stockSaber(true);
                xSaberCooldown = 0.66f;
            }

            if (chargeLevel == 3 && player.hasArmArmor(2))
            {
                stockedCharge = true;
                if (player.weapon is Buster)
                {
                    shootTime = hasUltimateArmorBS.getValue() ? 0.5f : 0.25f;
                }
                else shootTime = 0.5f;
                Global.serverClient?.rpc(RPC.playerToggle, (byte)player.id, (int)RPCToggleType.StockCharge);
            }
            else if (stockedCharge)
            {
                stockedCharge = false;
                Global.serverClient?.rpc(RPC.playerToggle, (byte)player.id, (int)RPCToggleType.UnstockCharge);
            }

            if (!player.weapon.isStream)
            {
                chargeTime = 0;
            }
            else
            {
                streamCooldown = 0.25f;
            }

            if (isHyperX)
            {
                unpoShotCount--;
                if (unpoShotCount < 0) unpoShotCount = 0;
            }
        }

        public void shootRpc(Point pos, int weaponIndex, int xDir, int chargeLevel, ushort netProjId, bool sendRpc)
        {
            // Right before we shoot, change to the current weapon.
            // This ensures that the shoot RPC sent reflects the current weapon used
            if (!player.isAI)
            {
                player.changeWeaponFromWi(weaponIndex);
            }

            Weapon weapon = player.weapon;
            float oldWeaponAmmo = 0;
            bool nonBusterHyperCharge = false;
            if (ownedByLocalPlayer && weaponIndex == (int)WeaponIds.HyperBuster)
            {
                Weapon hyperChargeWeapon = player.weapons[player.hyperChargeSlot];
                if (hyperChargeWeapon is not Buster || player.hasUltimateArmor())
                {
                    player.weapon.addAmmo(-HyperBuster.ammoUsage, player);
                    weapon = hyperChargeWeapon;
                    if (hyperChargeWeapon is not Buster)
                    {
                        nonBusterHyperCharge = true;
                        weapon.addAmmo(-HyperBuster.weaponAmmoUsage, player);
                        oldWeaponAmmo = weapon.ammo;
                    }
                }

                chargeLevel = 3;
            }

            weapon.shoot(pos, xDir, player, chargeLevel, netProjId);

            if (ownedByLocalPlayer && nonBusterHyperCharge)
            {
                weapon.ammo = oldWeaponAmmo;
            }

            if (ownedByLocalPlayer && sendRpc)
            {
                var playerIdByte = (byte)player.id;
                var xDirByte = (byte)(xDir + 128);
                var chargeLevelByte = (byte)chargeLevel;
                var netProjIdBytes = BitConverter.GetBytes(netProjId);
                var xBytes = BitConverter.GetBytes((short)pos.x);
                var yBytes = BitConverter.GetBytes((short)pos.y);
                var weaponIndexByte = (byte)weapon.index;

                RPC shootRpc = RPC.shoot;
                if (weapon is FireWave)
                {
                    // Optimize firewave shoot RPCs since they churn out at a fast rate
                    shootRpc = RPC.shootFast;
                }

                Global.serverClient?.rpc(shootRpc, playerIdByte, xBytes[0], xBytes[1], yBytes[0], yBytes[1], xDirByte, chargeLevelByte, netProjIdBytes[0], netProjIdBytes[1], weaponIndexByte);
            }
        }

        public bool canScan()
        {
            if (player.isAI) return false;
            if (invulnTime > 0) return false;
            if (!(charState is Idle || charState is Run || charState is Jump || charState is Fall || charState is WallKick || charState is WallSlide || charState is Dash || charState is Crouch || charState is AirDash)) return false;
            if (Global.level.is1v1()) return false;
            return true;
        }

        public bool isHyperXOrReviving()
        {
            if (sprite.name.Contains("revive")) return true;
            if (isHyperXBS.getValue()) return true;
            return false;
        }

        // BARRIER SECTION

        public Anim barrierAnim;
        public float barrierTime;
        public bool barrierFlinch;
        public float barrierDuration { get { return barrierFlinch ? 1.5f : 0.75f; } }
        public float barrierCooldown;
        public float maxBarrierCooldown { get { return 0; } }
        public bool hasBarrier(bool isOrange)
        {
            bool hasBarrier = barrierTime > 0 && barrierTime < barrierDuration - 0.12f;
            if (!isOrange) return hasBarrier && !player.hasChip(1);
            else return hasBarrier && player.hasChip(1);
        }
        public void updateBarrier()
        {
            if (barrierTime <= 0) return;

            if (barrierAnim != null)
            {
                if (barrierAnim.sprite.name == "barrier_start" && barrierAnim.isAnimOver())
                {
                    string spriteName = player.hasChip(1) ? "barrier2" : "barrier";
                    barrierAnim.changeSprite(spriteName, true);
                }
                barrierAnim.changePos(getCenterPos());
            }

            barrierTime -= Global.spf;
            if (barrierTime <= 0)
            {
                removeBarrier();
                barrierCooldown = maxBarrierCooldown;
            }
        }
        public void addBarrier(bool isFlinch)
        {
            if (!ownedByLocalPlayer) return;
            if (barrierAnim != null) return;
            if (barrierTime > 0) return;
            if (barrierCooldown > 0) return;
            barrierFlinch = isFlinch;
            barrierAnim = new Anim(getCenterPos(), "barrier_start", 1, player.getNextActorNetId(), false, true, true);
            barrierTime = barrierDuration;
            RPC.playerToggle.sendRpc(player.id, RPCToggleType.StartBarrier);
        }
        public void removeBarrier()
        {
            if (!ownedByLocalPlayer) return;
            barrierTime = 0;
            barrierAnim?.destroySelf();
            barrierAnim = null;
            RPC.playerToggle.sendRpc(player.id, RPCToggleType.StopBarrier);
        }

        // RAY SPLASHER SECTION

        public bool isShootingRaySplasher;
        public float raySplasherCooldown;
        public int raySplasherMod;
        public float raySplasherCooldown2;

        public void setShootRaySplasher(bool isShooting)
        {
            isShootingRaySplasher = isShooting;
            if (!isShooting)
            {
                raySplasherCooldown = 0;
                raySplasherMod = 0;
                raySplasherCooldown2 = 0;
                if (ownedByLocalPlayer) RPC.playerToggle.sendRpc(player.id, RPCToggleType.StopRaySplasher);
            }
        }

        public void removeBusterProjs()
        {
            chargedSpinningBlade = null;
            chargedFrostShield = null;
            chargedTunnelFang = null;
            changeSprite("mmx_" + charState.sprite, true);
        }

        public bool hasBusterProj()
        {
            return chargedSpinningBlade != null || chargedFrostShield != null || chargedTunnelFang != null;
        }

        public void destroyBusterProjs()
        {
            chargedSpinningBlade?.destroySelf();
            chargedFrostShield?.destroySelf();
            chargedTunnelFang?.destroySelf();
        }

        public bool checkMaverickWeakness(ProjIds projId)
        {
            if (player.weapon is Torpedo)
            {
                return projId == ProjIds.ArmoredARoll;
            }
            else if (player.weapon is Sting)
            {
                return projId == ProjIds.BoomerKBoomerang;
            }
            else if (player.weapon is RollingShield)
            {
                return projId == ProjIds.SparkMSpark;
            }
            else if (player.weapon is FireWave)
            {
                return projId == ProjIds.StormETornado;
            }
            else if (player.weapon is Tornado)
            {
                return projId == ProjIds.StingCSting;
            }
            else if (player.weapon is ElectricSpark)
            {
                return projId == ProjIds.ChillPIcePenguin || projId == ProjIds.ChillPIceShot;
            }
            else if (player.weapon is Boomerang)
            {
                return projId == ProjIds.LaunchOMissle || projId == ProjIds.LaunchOTorpedo;
            }
            else if (player.weapon is ShotgunIce)
            {
                return projId == ProjIds.FlameMFireball || projId == ProjIds.FlameMOilFire;
            }
            else if (player.weapon is CrystalHunter)
            {
                return projId == ProjIds.MagnaCMagnetMine;
            }
            else if (player.weapon is BubbleSplash)
            {
                return projId == ProjIds.WheelGSpinWheel;
            }
            else if (player.weapon is SilkShot)
            {
                return projId == ProjIds.FStagFireball || projId == ProjIds.FStagDash || projId == ProjIds.FStagDashCharge || projId == ProjIds.FStagDashTrail;
            }
            else if (player.weapon is SpinWheel)
            {
                return projId == ProjIds.WSpongeChain || projId == ProjIds.WSpongeUpChain;
            }
            else if (player.weapon is SonicSlicer)
            {
                return projId == ProjIds.CSnailCrystalHunter;
            }
            else if (player.weapon is StrikeChain)
            {
                return projId == ProjIds.OverdriveOSonicSlicer || projId == ProjIds.OverdriveOSonicSlicerUp;
            }
            else if (player.weapon is MagnetMine)
            {
                return projId == ProjIds.MorphMCScrap || projId == ProjIds.MorphMBeam;
            }
            else if (player.weapon is SpeedBurner)
            {
                return projId == ProjIds.BCrabBubbleSplash;
            }
            else if (player.weapon is AcidBurst)
            {
                return projId == ProjIds.BBuffaloIceProj || projId == ProjIds.BBuffaloIceProjGround;
            }
            else if (player.weapon is ParasiticBomb)
            {
                return projId == ProjIds.GBeetleGravityWell || projId == ProjIds.GBeetleBall;
            }
            else if (player.weapon is TriadThunder)
            {
                return projId == ProjIds.TunnelRTornadoFang || projId == ProjIds.TunnelRTornadoFang2 || projId == ProjIds.TunnelRTornadoFangDiag;
            }
            else if (player.weapon is SpinningBlade)
            {
                return projId == ProjIds.VoltCBall || projId == ProjIds.VoltCTriadThunder || projId == ProjIds.VoltCUpBeam || projId == ProjIds.VoltCUpBeam2;
            }
            else if (player.weapon is RaySplasher)
            {
                return projId == ProjIds.CrushCArmProj;
            }
            else if (player.weapon is GravityWell)
            {
                return projId == ProjIds.NeonTRaySplasher;
            }
            else if (player.weapon is FrostShield)
            {
                return projId == ProjIds.BHornetBee || projId == ProjIds.BHornetHomingBee;
            }
            else if (player.weapon is TunnelFang)
            {
                return projId == ProjIds.TSeahorseAcid1 || projId == ProjIds.TSeahorseAcid2;
            }

            return false;
        }

        public Projectile getXProjFromHitbox(Point centerPoint)
        {
            Projectile proj = null;

            if (sprite.name.Contains("beam_saber") && sprite.name.Contains("2"))
            {
                float overrideDamage = 3;
                if (!grounded) overrideDamage = 2;
                proj = new GenericMeleeProj(new XSaber(player), centerPoint, ProjIds.X6Saber, player, damage: overrideDamage, flinch: 0);
            }
            else if (sprite.name.Contains("beam_saber"))
            {
                proj = new GenericMeleeProj(new XSaber(player), centerPoint, ProjIds.XSaber, player);
            }
            else if (sprite.name.Contains("nova_strike"))
            {
                proj = new GenericMeleeProj(new NovaStrike(player), centerPoint, ProjIds.NovaStrike, player);
            }
            else if (sprite.name.Contains("speedburner"))
            {
                proj = new GenericMeleeProj(new SpeedBurner(player), centerPoint, ProjIds.SpeedBurnerCharged, player);
            }
            else if (sprite.name.Contains("shoryuken"))
            {
                proj = new GenericMeleeProj(new ShoryukenWeapon(player), centerPoint, ProjIds.Shoryuken, player);
            }
            else if (sprite.name.Contains("unpo_grab_dash"))
            {
                proj = new GenericMeleeProj(new XUPGrab(), centerPoint, ProjIds.UPGrab, player, 0, 0, 0);
            }
            else if (sprite.name.Contains("unpo_punch"))
            {
                proj = new GenericMeleeProj(new XUPPunch(player), centerPoint, ProjIds.UPPunch, player);
            }
            else if (sprite.name.Contains("unpo_air_punch"))
            {
                proj = new GenericMeleeProj(new XUPPunch(player), centerPoint, ProjIds.UPPunch, player, damage: 3, flinch: Global.halfFlinch);
            }
            else if (sprite.name.Contains("unpo_parry_start"))
            {
                proj = new GenericMeleeProj(new XUPParry(), centerPoint, ProjIds.UPParryBlock, player, 0, 0, 1);
            }

            return proj;
        }
    }

    public class XHover : CharState
    {
        float hoverTime;
        int startXDir;
        public XHover() : base("hover", "hover_shoot", "", "")
        {
        }

        public override void update()
        {
            base.update();

            airCode();
            character.xDir = startXDir;
            Point inputDir = player.input.getInputDir(player);

            if (inputDir.x == character.xDir)
            {
                if (!sprite.StartsWith("hover_forward"))
                {
                    sprite = "hover_forward";
                    shootSprite = sprite + "_shoot";
                    character.changeSpriteFromName(sprite, true);
                }
            }
            else if (inputDir.x == -character.xDir)
            {
                if (player.input.isHeld(Control.Jump, player))
                {
                    if (!sprite.StartsWith("hover_backward"))
                    {
                        sprite = "hover_backward";
                        shootSprite = sprite + "_shoot";
                        character.changeSpriteFromName(sprite, true);
                    }
                }
                else
                {
                    character.xDir = -character.xDir;
                    startXDir = character.xDir;
                    if (!sprite.StartsWith("hover_forward"))
                    {
                        sprite = "hover_forward";
                        shootSprite = sprite + "_shoot";
                        character.changeSpriteFromName(sprite, true);
                    }
                }
            }
            else
            {
                if (sprite != "hover")
                {
                    sprite = "hover";
                    shootSprite = sprite + "_shoot";
                    character.changeSpriteFromName(sprite, true);
                }
            }

            if (character.vel.y < 0)
            {
                character.vel.y += Global.spf * Global.level.gravity;
                if (character.vel.y > 0) character.vel.y = 0;
            }

            if (character.gravityWellModifier > 1)
            {
                character.vel.y = 53;
            }

            hoverTime += Global.spf;
            if (hoverTime > 2 || character.player.input.isPressed(Control.Jump, character.player))
            {
                character.changeState(new Fall(), true);
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.useGravity = false;
            character.vel = new Point();
            startXDir = character.xDir;
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.useGravity = true;
        }
    }
}
