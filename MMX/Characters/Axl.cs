using SFML.Graphics;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SFML.Window.Keyboard;

namespace MMXOnline
{
    public class RaycastHitData
    {
        public List<IDamagable> hitGos = new List<IDamagable>();
        public Point hitPos;
        public bool isHeadshot;
    }

    public partial class Character
    {
        public bool aiming;
        public IDamagable axlCursorTarget = null;
        public Character axlHeadshotTarget = null;
        public Anim muzzleFlash;
        public Anim transformAnim;
        public float recoilTime;
        public float axlSwapTime;
        public float axlAltSwapTime;
        public float switchTime;
        public float altSwitchTime;
        public int lastXDir;
        public float netArmAngle;
        public float assassinTime;
        public bool isQuickAssassinate;
        float targetSoundCooldown;
        public Point nonOwnerAxlBulletPos;
        public float stealthRevealTime;
        float transformSmokeTime;

        public bool aimBackwardsToggle;
        public bool positionLockToggle;
        public bool cursorLockToggle;
        public void resetToggle()
        {
            aimBackwardsToggle = false;
            positionLockToggle = false;
            cursorLockToggle = false;
        }

        public bool isNonOwnerZoom;
        public Point nonOwnerScopeStartPos;
        public Point nonOwnerScopeEndPos;
        public Point? netNonOwnerScopeEndPos;
        private bool _zoom;
        public bool isZoomingIn;
        public bool isZoomingOut;
        public bool isZoomOutPhase1Done;
        public float zoomCharge;
        public float savedCamX;
        public float savedCamY;
        public bool hyperAxlStillZoomed;

        public float revTime;
        public float revIndex;
        public bool aimingBackwards;
        public LoopingSound iceGattlingLoop;
        public bool isRevving;
        public bool isNonOwnerRev;
        public SniperMissileProj sniperMissileProj;
        public LoopingSound iceGattlingSound;
        public float whiteAxlTime;
        public float dodgeRollCooldown;
        public const float maxDodgeRollCooldown = 1.5f;
        public bool disguiseCoverBlown;
        public bool hyperAxlUsed;
        public ShaderWrapper axlPaletteShader;
        public float maxHyperAxlTime = 30;
        public List<int> ammoUsages = new List<int>();

        // Used to be 0.5, 100
        public const float maxStealthRevealTime = 0.25f;
        public const float stealthRevealPingDenom = 200;    // The ping divided by this number indicates stealth reveal time in online

        public void zoomIn()
        {
            if (isZoomingIn) return;
            if (_zoom) return;

            _zoom = true;
            if (isWhiteAxl()) hyperAxlStillZoomed = true;
            player.axlCursorPos.x = Helpers.clamp(player.axlCursorPos.x, 0, Global.viewScreenW);
            player.axlCursorPos.y = Helpers.clamp(player.axlCursorPos.y, 0, Global.viewScreenH);
            savedCamX = Global.level.camX;
            savedCamY = Global.level.camY;
            player.axlScopeCursorWorldPos = player.character.getCamCenterPos(ignoreZoom: true);
            player.axlScopeCursorWorldLerpPos = player.axlCursorWorldPos;
            isZoomingIn = true;
            isZoomingOut = false;
        }

        public void zoomOut()
        {
            if (isZoomingOut) return;
            if (!_zoom) return;

            zoomCharge = 0;
            player.axlZoomOutCursorDestPos = player.character.getCamCenterPos(ignoreZoom: true);
            player.axlCursorPos = getAxlBulletPos().add(getAxlBulletDir().times(50)).addxy(-savedCamX, -savedCamY);

            isZoomingOut = true;
            isZoomingIn = false;
        }

        public bool isZooming()
        {
            return _zoom && player.isAxl;
        }

        public bool isAnyZoom()
        {
            return isZooming() || isZoomingOut || isZoomingIn;
        }

        public bool hasScopedTarget()
        {
            if (isZoomingOut || isZoomingIn) return false;
            if (axlCursorTarget == null && axlHeadshotTarget == null) return false;
            var hitData = getFirstHitPos(player.adjustedZoomRange, ignoreDamagables: true);
            if (hitData.hitGos.Contains(axlCursorTarget) || hitData.hitGos.Contains(axlHeadshotTarget))
            {
                return true;
            }
            return false;
        }

        public RaycastHitData getFirstHitPos(float range, float backOffDist = 0, bool ignoreDamagables = false)
        {
            var retData = new RaycastHitData();
            Point bulletPos = getAxlBulletPos();
            Point bulletDir = getAxlBulletDir();

            Point maxPos = bulletPos.add(bulletDir.times(range));

            List<CollideData> hits = Global.level.raycastAll(bulletPos, maxPos, new List<Type>() { typeof(Actor), typeof(Wall) });

            CollideData hit = null;

            foreach (var p in Global.level.players)
            {
                if (p.character == null || p.character.getHeadPos() == null) continue;
                Rect headRect = p.character.getHeadRect();

                Point startTestPoint = bulletPos.add(bulletDir.times(-range * 2));
                Point endTestPoint = bulletPos.add(bulletDir.times(range * 2));
                Line testLine = new Line(startTestPoint, endTestPoint);
                Shape headShape = headRect.getShape();
                List<CollideData> lineIntersections = headShape.getLineIntersectCollisions(testLine);
                if (lineIntersections.Count > 0)
                {
                    hits.Add(new CollideData(null, p.character.globalCollider, bulletDir, false, p.character, new HitData(null, new List<Point>() { lineIntersections[0].getHitPointSafe() })));
                }
            }
            
            hits.Sort((cd1, cd2) =>
            {
                float d1 = bulletPos.distanceTo(cd1.getHitPointSafe());
                float d2 = bulletPos.distanceTo(cd2.getHitPointSafe());
                if (d1 < d2) return -1;
                else if (d1 > d2) return 1;
                else return 0;
            });

            foreach (var h in hits)
            {
                if (h.gameObject is Wall)
                {
                    hit = h;
                    break;
                }
                if (h.gameObject is IDamagable damagable && damagable.canBeDamaged(player.alliance, player.id, null))
                {
                    retData.hitGos.Add(damagable);
                    if (h.gameObject is Character c)
                    {
                        if (c.isAlwaysHeadshot())
                        {
                            retData.isHeadshot = true;
                        }
                        // Detect headshots
                        else if (h?.hitData?.hitPoint != null && c.getHeadPos() != null)
                        {
                            Point headPos = c.getHeadPos().Value;
                            Rect headRect = c.getHeadRect();

                            Point hitPoint = h.hitData.hitPoint.Value;
                            // Bullet position inside head rect
                            if (headRect.containsPoint(bulletPos))
                            {
                                hitPoint = bulletPos;
                            }

                            float xLeeway = c.headshotRadius * 5f;
                            float yLeeway = c.headshotRadius * 2.5f;

                            float xDist = MathF.Abs(hitPoint.x - headPos.x);
                            float yDist = MathF.Abs(hitPoint.y - headPos.y);

                            if (xDist < xLeeway && yDist < yLeeway)
                            {
                                Point startTestPoint = bulletPos.add(bulletDir.times(-range * 2));
                                Point endTestPoint = bulletPos.add(bulletDir.times(range * 2));
                                Line testLine = new Line(startTestPoint, endTestPoint);
                                Shape headShape = headRect.getShape();
                                List<CollideData> lineIntersections = headShape.getLineIntersectCollisions(testLine);
                                if (lineIntersections.Count > 0)
                                {
                                    retData.isHeadshot = true;
                                }
                            }
                        }
                    }
                    if (ignoreDamagables == false)
                    {
                        hit = h;
                        break;
                    }
                }
            }

            Point targetPos = hit?.hitData?.hitPoint ?? maxPos;
            if (backOffDist > 0)
            {
                retData.hitPos = bulletPos.add(bulletPos.directionTo(targetPos).unitInc(-backOffDist));
            }
            else
            {
                retData.hitPos = targetPos;
            }

            return retData;
        }

        public Point getDoubleBulletArmPos()
        {
            if (sprite.name == "axl_dash")
            {
                return new Point(-7, -2);
            }
            if (sprite.name == "axl_run")
            {
                return new Point(-7, 1);
            }
            if (sprite.name == "axl_jump" || sprite.name == "axl_fall_start" || sprite.name == "axl_fall" || sprite.name == "axl_hover")
            {
                return new Point(-7, 0);
            }
            return new Point(-5, 2);
        }

        public int axlXDir
        {
            get
            {
                if (sprite.name.Contains("wall_slide")) return -xDir;
                return xDir;
            }
        }

        public bool canChangeDir()
        {
            return !(charState is InRideArmor) && !(charState is Die) && !(charState is Frozen) && !(charState is Stunned);
        }

        public void updateDisguisedAxl()
        {
            if (player.weapon is AssassinBullet)
            {
                player.assassinHitPos = player.character.getFirstHitPos(AssassinBulletProj.range);
            }

            if (!player.isAxl)
            {
                if (Options.main.axlAimMode == 2)
                {
                    updateAxlCursorPos();
                }
                else
                {
                    updateAxlDirectionalAim();
                }
            }

            if (player.isZero)
            {
                player.changeWeaponControls();
            }

            if (player.weapon is UndisguiseWeapon)
            {
                bool shootPressed = player.input.isPressed(Control.Shoot, player);
                bool altShootPressed = player.input.isPressed(Control.Special1, player);
                if ((shootPressed || altShootPressed) && !isCCImmuneHyperMode())
                {
                    undisguiseTime = 0.33f;
                    player.revertToAxl();
                    player.character.undisguiseTime = 0.33f;
                    if (altShootPressed && player.scrap >= 2)
                    {
                        player.scrap -= 2;
                        player.lastDNACore.hyperMode = DNACoreHyperMode.None;
                        // Turn ultimate and golden armor into naked X
                        if (player.lastDNACore.armorFlag >= byte.MaxValue - 1)
                        {
                            player.lastDNACore.armorFlag = 0;
                        }
                        // Turn ancient gun into regular axl bullet
                        if (player.lastDNACore.weapons.Count > 0 && player.lastDNACore.weapons[0] is AxlBullet ab && ab.type == (int)AxlBulletWeaponType.AncientGun)
                        {
                            player.lastDNACore.weapons[0] = player.getAxlBulletWeapon(0);
                        }
                        player.weapons.Insert(player.lastDNACoreIndex, player.lastDNACore);
                    }
                    return;
                }
            }
            
            if (player.weapon is AssassinBullet)
            {
                if (player.input.isPressed(Control.Special1, player) && !isCharging())
                {
                    if (player.scrap >= 2)
                    {
                        player.scrap -= 2;
                        shootAssassinShot(isAltFire: true);
                        return;
                    }
                    else
                    {
                        Global.level.gameMode.setHUDErrorMessage(player, "Quick assassinate requires 2 scrap");
                    }
                }
            }

            if (player.weapon is AssassinBullet && (player.isVile || player.isSigma))
            {
                if (player.input.isHeld(Control.Shoot, player))
                {
                    increaseCharge();
                }
                else
                {
                    if (isCharging())
                    {
                        shootAssassinShot();
                    }
                    stopCharge();
                }
                chargeLogic();
            }

            /*
            if (player.weapon is AssassinBullet && chargeTime > 7)
            {
                shootAssassinShot();
                stopCharge();
            }
            */
        }

        public void shootAssassinShot(bool isAltFire = false)
        {
            if (getChargeLevel() >= 3 || isAltFire)
            {
                player.revertToAxl();
                assassinTime = 0.5f;
                player.character.assassinTime = 0.5f;
                player.character.useGravity = false;
                player.character.vel = new Point();
                player.character.isQuickAssassinate = isAltFire;
                player.character.changeState(new Assassinate(grounded), true);
            }
            else
            {
                stopCharge();
            }
        }

        float assassinSmokeTime;
        float undisguiseTime;
        float lastAltShootPressedTime;
        float voltTornadoTime;
        public void updateAxl()
        {
            isRevving = false;
            bool altRayGunHeld = false;
            bool altPlasmaGunHeld = false;

            if (isStealthMode())
            {
                updateStealthMode();
            }

            if (isZooming() && deltaPos.magnitude > 1)
            {
                zoomOut();
            }

            if (isZooming() && !isZoomingIn && !isZoomingOut)
            {
                zoomCharge += Global.spf * 0.5f;
                if (isWhiteAxl()) zoomCharge = 1;
                if (zoomCharge > 1) zoomCharge = 1;
            }

            if (assassinTime > 0)
            {
                assassinSmokeTime += Global.spf;
                if (assassinSmokeTime > 0.06f)
                {
                    assassinSmokeTime = 0;
                    // new Anim(getAxlBulletPos(0), "torpedo_smoke", 1, player.getNextActorNetId(), false, true, true) { vel = new Point(0, -100) };
                }
                assassinTime -= Global.spf;
                if (assassinTime < 0)
                {
                    assassinTime = 0;
                    useGravity = true;
                }
                return;
            }
            if (targetSoundCooldown > 0) targetSoundCooldown += Global.spf;
            if (targetSoundCooldown >= 1) targetSoundCooldown = 0;
            
            Helpers.decrementTime(ref dodgeRollCooldown);
            Helpers.decrementTime(ref undisguiseTime);
            Helpers.decrementTime(ref axlSwapTime);
            Helpers.decrementTime(ref axlAltSwapTime);
            Helpers.decrementTime(ref switchTime);
            Helpers.decrementTime(ref altSwitchTime);
            Helpers.decrementTime(ref revTime);
            Helpers.decrementTime(ref voltTornadoTime);
            Helpers.decrementTime(ref stealthRevealTime);

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

            player.changeWeaponControls();
            
            updateAxlAim();

            sprite.reversed = false;
            if (player.axlWeapon != null && (player.axlWeapon.isTwoHanded(false) || isZooming()) && canChangeDir() && charState is not WallSlide)
            {
                int newXDir = (pos.x > player.axlGenericCursorWorldPos.x ? -1 : 1);
                if (charState is Run && xDir != newXDir)
                {
                    sprite.reversed = true;
                }
                xDir = newXDir;
            }

            var axlBullet = player.weapons.FirstOrDefault(w => w is AxlBullet) as AxlBullet;

            bool shootPressed = player.input.isPressed(Control.Shoot, player);
            bool shootHeld = player.input.isHeld(Control.Shoot, player);
            bool altShootPressed = player.input.isPressed(Control.Special1, player);
            bool altShootHeld = player.input.isHeld(Control.Special1, player);
            bool altShootRecentlyPressed = false;

            if (!player.isAI)
            {
                shootPressed = shootPressed || Input.isMousePressed(Mouse.Button.Left, player.canControl);
                shootHeld = shootHeld || Input.isMouseHeld(Mouse.Button.Left, player.canControl);
                altShootPressed = altShootPressed || Input.isMousePressed(Mouse.Button.Right, player.canControl);
                altShootHeld = altShootHeld || Input.isMouseHeld(Mouse.Button.Right, player.canControl);
            }

            if (altShootPressed)
            {
                lastAltShootPressedTime = Global.time;
            }
            else
            {
                altShootRecentlyPressed = Global.time - lastAltShootPressedTime < 0.1f;
            }

            if (isInvulnerableAttack() || isWarpIn())
            {
                if (charState is not DodgeRoll)
                {
                    shootPressed = false;
                    shootHeld = false;
                    altShootPressed = false;
                    altShootHeld = false;
                    altShootRecentlyPressed = false;
                }
            }

            if (axlSwapTime > 0)
            {
                shootPressed = false;
                shootHeld = false;
            }
            if (axlAltSwapTime > 0)
            {
                altShootPressed = false;
                altShootHeld = false;
                altShootRecentlyPressed = false;
            }

            bool bothHeld = shootHeld && altShootHeld;

            if (player.weapon is AxlBullet || player.weapon is DoubleBullet)
            {
                (player.weapon as AxlWeapon)?.rechargeAxlBulletAmmo(player, this, shootHeld, 1);
            }
            else
            {
                foreach (var weapon in player.weapons)
                {
                    if (weapon is AxlBullet || weapon is DoubleBullet)
                    {
                        (weapon as AxlWeapon)?.rechargeAxlBulletAmmo(player, this, shootHeld, 2);
                    }
                }
            }

            if (player.weapons.Count > 0 && player.weapons[0].type > 0)
            {
                player.axlBulletTypeLastAmmo[player.weapons[0].type] = player.weapons[0].ammo;
            }

            if (player.weapon is not AssassinBullet)
            {
                if (altShootHeld && !bothHeld && (player.weapon is AxlBullet || player.weapon is DoubleBullet) && invulnTime == 0 && flag == null)
                {
                    increaseCharge();
                }
                else
                {
                    if (isCharging() && getChargeLevel() >= 3 && player.scrap >= 10 && !isWhiteAxl() && !hyperAxlUsed && (player.axlHyperMode > 0 || player.axlBulletType == 0))
                    {
                        if (player.axlHyperMode == 0)
                        {
                            changeState(new HyperAxlStart(grounded), true);
                        }
                        else
                        {
                            if (!hyperAxlUsed)
                            {
                                hyperAxlUsed = true;
                                //addHealth(player.maxHealth);
                                foreach (var weapon in player.weapons)
                                {
                                    weapon.ammo = weapon.maxAmmo;
                                }
                                stingChargeTime = 12;
                                playSound("stingCharge", sendRpc: true);
                            }
                        }
                    }
                    else if (isCharging() && getChargeLevel() >= 3 && isStealthMode())
                    {
                        stingChargeTime = 0;
                        playSound("stingCharge", sendRpc: true);
                    }
                    else if (isCharging())
                    {
                        if (player.weapon is AxlBullet || player.weapon is DoubleBullet)
                        {
                            recoilTime = 0.2f;
                            if (!isWhiteAxl())
                            {
                                player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
                            }
                            else
                            {
                                player.axlWeapon.axlShoot(player, AxlBulletType.WhiteAxlCopyShot2);
                                player.axlWeapon.axlShoot(player, AxlBulletType.WhiteAxlCopyShot2);
                            }
                        }
                    }
                    stopCharge();
                }
            }
            else
            {
                if (shootHeld)
                {
                    increaseCharge();
                }
                else
                {
                    if (isCharging())
                    {
                        shootAssassinShot();
                    }
                    stopCharge();
                }
            }
            chargeLogic();

            bool canShoot = (undisguiseTime == 0 && assassinTime == 0);
            if (canShoot)
            {
                // Axl bullet
                if (!isCharging())
                {
                    if (player.weapon is AxlBullet && charState.canShoot() && !player.weapon.noAmmo())
                    {
                        if (shootHeld && shootTime == 0 && player.weapon.altShootTime == 0)
                        {
                            recoilTime = 0.2f;
                            player.axlWeapon.axlShoot(player);
                        }
                        else if ((altShootPressed || altShootRecentlyPressed) && shootTime == 0 && player.weapon.altShootTime == 0 && player.weapon.ammo >= 4)
                        {
                            recoilTime = 0.2f;
                            player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
                        }
                    }

                    // Double bullet
                    if (player.weapon is DoubleBullet && charState.canShoot() && !(charState is LadderClimb) && !player.weapon.noAmmo())
                    {
                        if (shootHeld && shootTime == 0)
                        {
                            recoilTime = 0.2f;
                            player.axlWeapon.axlShoot(player);
                            if (bothHeld) player.axlWeapon.shootTime *= 2f;
                        }
                        if (bothHeld && player.weapon.altShootTime == 0)
                        {
                            recoilTime = 0.2f;
                            player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
                            if (bothHeld) player.axlWeapon.altShootTime *= 2f;
                        }
                        else if ((altShootPressed || altShootRecentlyPressed) && shootTime == 0 && player.weapon.altShootTime == 0 && player.weapon.ammo >= 4)
                        {
                            recoilTime = 0.2f;
                            player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
                        }
                    }

                    if (player.weapon is GLauncher && charState.canShoot() && !(charState is LadderClimb))
                    {
                        if (shootHeld && shootTime == 0 && player.weapon.ammo >= 1)
                        {
                            recoilTime = 0.2f;
                            player.axlWeapon.axlShoot(player);
                        }

                        if (player.axlLoadout.blastLauncherAlt == 0)
                        {
                            if (altShootPressed && shootTime == 0 && player.weapon.altShootTime == 0 && player.weapon.ammo >= 1)
                            {
                                recoilTime = 0.2f;
                                player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
                            }
                        }
                        else
                        {
                            if (altShootPressed && player.grenades.Count > 0)
                            {
                                foreach (var grenade in player.grenades)
                                {
                                    grenade.detonate();
                                }
                                player.grenades.Clear();
                            }
                        }
                    }

                    if (player.weapon is RayGun && charState.canShoot() && !player.weapon.noAmmo())
                    {
                        if (shootHeld && shootTime == 0)
                        {
                            recoilTime = 0.2f;
                            player.axlWeapon.axlShoot(player);
                        }
                        else if (altShootHeld)
                        {
                            if (shootTime == 0)
                            {
                                recoilTime = 0.2f;
                                player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
                            }
                            altRayGunHeld = player.axlWeapon.ammo > 0;

                            if (player.axlLoadout.rayGunAlt == 0)
                            {
                                Point bulletDir = getAxlBulletDir();
                                float whiteAxlMod = isWhiteAxl() ? 2 : 1;
                                player.character.move(bulletDir.times(-50 * whiteAxlMod));
                            }
                        }
                    }

                    if (player.weapon is BlackArrow && charState.canShoot() && !player.weapon.noAmmo())
                    {
                        if (shootHeld && shootTime == 0)
                        {
                            recoilTime = 0.2f;
                            player.axlWeapon.axlShoot(player);
                        }
                        else if (altShootHeld && shootTime == 0 && player.weapon.altShootTime == 0)
                        {
                            recoilTime = 0.2f;
                            player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
                        }
                    }

                    if (player.weapon is SpiralMagnum && charState.canShoot())
                    {
                        if (shootHeld && shootTime == 0)
                        {
                            if (!player.weapon.noAmmo())
                            {
                                recoilTime = 0.2f;
                                player.axlWeapon.axlShoot(player);
                            }
                        }
                        else
                        {
                            if (player.axlLoadout.spiralMagnumAlt == 0)
                            {
                                if (altShootPressed && player.axlWeapon.ammo > 0 && shootTime == 0 && player.weapon.altShootTime == 0)
                                {
                                    recoilTime = 0.2f;
                                    player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
                                }
                            }
                            else
                            {
                                if (altShootPressed && (charState is Idle || charState is Crouch))
                                {
                                    if (!_zoom)
                                    {
                                        zoomIn();
                                    }
                                    else if (!isZoomingIn && !isZoomingOut)
                                    {
                                        zoomOut();
                                    }
                                }
                            }
                        }
                    }

                    if (player.weapon is BoundBlaster && charState.canShoot() && !player.weapon.noAmmo())
                    {
                        if (shootHeld && shootTime == 0)
                        {
                            recoilTime = 0.2f;
                            player.axlWeapon.axlShoot(player);
                        }
                        else if (altShootHeld && shootTime == 0 && player.weapon.altShootTime == 0)
                        {
                            recoilTime = 0.2f;
                            player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
                        }
                    }

                    if (player.weapon is PlasmaGun && charState.canShoot() && !player.weapon.noAmmo())
                    {
                        if (shootHeld && shootTime == 0)
                        {
                            recoilTime = 0.2f;
                            player.axlWeapon.altShootTime = player.axlWeapon.altFireCooldown;
                            player.axlWeapon.axlShoot(player);
                        }
                        else if (altShootHeld)
                        {
                            if (player.axlLoadout.plasmaGunAlt == 0)
                            {
                                if (player.axlWeapon.altShootTime == 0 && grounded)
                                {
                                    recoilTime = 0.2f;
                                    voltTornadoTime = 0.2f;
                                    player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
                                }
                            }
                            else
                            {
                                if (player.axlWeapon.altShootTime == 0)
                                {
                                    recoilTime = 0.2f;
                                    player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
                                }
                                altPlasmaGunHeld = player.axlWeapon.ammo > 0;
                            }
                        }
                    }

                    if (player.weapon is IceGattling && charState.canShoot() && !(charState is LadderClimb) && player.weapon.ammo > 0)
                    {
                        if (altShootPressed && player.axlLoadout.iceGattlingAlt == 0 && gaeaShield == null)
                        {
                            recoilTime = 0.2f;
                            player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
                        }

                        bool isAltRev = (altShootHeld && player.axlLoadout.iceGattlingAlt == 1);
                        if (shootHeld || isAltRev)
                        {
                            isRevving = true;
                            revTime += Global.spf * 2 * (isWhiteAxl() ? 10 : (isAltRev ? 2 : 1));
                            if (revTime > 1)
                            {
                                revTime = 1;
                            }
                        }

                        if (shootHeld && shootTime == 0 && revTime >= 1)
                        {
                            recoilTime = 0.2f;
                            player.axlWeapon.axlShoot(player);
                        }
                    }

                    if (player.weapon is FlameBurner && charState.canShoot() && !(charState is LadderClimb) && player.weapon.ammo > 0)
                    {
                        if (shootHeld && shootTime == 0)
                        {
                            recoilTime = 0.05f;
                            player.axlWeapon.axlShoot(player);
                        }

                        if (player.axlLoadout.flameBurnerAlt == 0)
                        {
                            if (altShootHeld && shootTime == 0 && player.weapon.altShootTime == 0)
                            {
                                recoilTime = 0.2f;
                                player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
                                player.axlWeapon.shootTime = 0.5f;
                            }
                        }
                        else
                        {
                            if (altShootHeld)
                            {
                                if (shootTime == 0 && player.weapon.altShootTime == 0)
                                {
                                    recoilTime = 0.2f;
                                    player.axlWeapon.axlShoot(player, AxlBulletType.AltFire);
                                }
                            }
                        }
                    }

                    // DNA Core
                    if (player.weapon is DNACore && charState.canShoot())
                    {
                        AxlWeapon realWeapon = player.weapons[player.weaponSlot] as AxlWeapon;
                        if (realWeapon != null)
                        {
                            if (shootPressed && shootTime == 0)
                            {
                                if (flag != null)
                                {
                                    Global.level.gameMode.setHUDErrorMessage(player, "Cannot transform with flag");
                                }
                                else if (player.scrap < 1)
                                {
                                    Global.level.gameMode.setHUDErrorMessage(player, "Transformation requires 1 scrap");
                                }
                                else if (isWhiteAxl() || isStealthMode())
                                {
                                    Global.level.gameMode.setHUDErrorMessage(player, "Cannot transform as Hyper Axl");
                                }
                                else
                                {
                                    player.scrap--;
                                    realWeapon.axlShoot(player);
                                }
                            }
                        }
                    }
                }
            }

            Helpers.decrementTime(ref recoilTime);

            if (!isRevving && !iceGattlingSound.destroyed)
            {
                iceGattlingSound.stopRev(revTime);
            }
            else
            {
                iceGattlingSound.play();
            }

            if (player.axlWeapon is IceGattling)
            {
                if (isRevving)
                {
                    RPC.playerToggle.sendRpc(player.id, RPCToggleType.StartRev);
                }
                else
                {
                    RPC.playerToggle.sendRpc(player.id, RPCToggleType.StopRev);
                }
            }
            else
            {
                if (revTime > 0)
                {
                    RPC.playerToggle.sendRpc(player.id, RPCToggleType.StopRev);
                }
            }

            if (!altRayGunHeld)
            {
                rayGunAltProj?.destroySelf();
                rayGunAltProj = null;
            }

            if (!altPlasmaGunHeld)
            {
                plasmaGunAltProj?.destroySelf();
                plasmaGunAltProj = null;
            }
        }

        public float getAimBackwardsAmount()
        {
            Point bulletDir = getAxlBulletDir();

            float forwardAngle = getShootXDir() == 1 ? 0 : 180;
            float bulletAngle = bulletDir.angle;
            if (bulletAngle > 180) bulletAngle = 360 - bulletAngle;

            float dist = MathF.Abs(forwardAngle - bulletAngle);
            dist = Helpers.clampMin0(dist - 90);
            return Helpers.clamp01(dist / 90f);
        }

        public void updateAxlAim()
        {
            if (Global.level.gameMode.isOver || isAnyZoom() || sniperMissileProj != null)
            {
                resetToggle();
            }

            if (!Global.level.gameMode.isOver)
            {
                if (isZooming())
                {
                    if (!isZoomingIn && !isZoomingOut)
                    {
                        updateAxlScopePos();
                    }
                    else if (isZoomingIn)
                    {
                        player.axlScopeCursorWorldPos = Point.lerp(player.axlScopeCursorWorldPos, player.axlScopeCursorWorldLerpPos, Global.spf * 15);
                        if (player.axlScopeCursorWorldPos.distanceTo(player.axlScopeCursorWorldLerpPos) < 1)
                        {
                            player.axlScopeCursorWorldPos = player.axlScopeCursorWorldLerpPos;
                            isZoomingIn = false;
                        }
                    }
                    else if (isZoomingOut)
                    {
                        Point destPos = player.axlZoomOutCursorDestPos;
                        player.axlScopeCursorWorldPos = Point.lerp(player.axlScopeCursorWorldPos, destPos, Global.spf * 15);
                        float dist = player.axlScopeCursorWorldPos.distanceTo(destPos);
                        if (dist < 50 && !isZoomOutPhase1Done)
                        {
                            //player.axlCursorPos = player.axlScopeCursorWorldPos.addxy(-Global.level.camX, -Global.level.camY);
                            isZoomOutPhase1Done = true;
                        }
                        if (dist < 1)
                        {
                            player.axlScopeCursorWorldPos = destPos;
                            isZoomingOut = false;
                            isZoomOutPhase1Done = false;
                            _zoom = false;
                            hyperAxlStillZoomed = false;
                        }
                    }
                    return;
                }

                if (player.isAI)
                {
                    updateAxlCursorPos();
                }
                else if (Options.main.axlAimMode == 2)
                {
                    updateAxlCursorPos();
                }
                else
                {
                    updateAxlDirectionalAim();
                }
            }
        }

        public void updateAxlDirectionalAim()
        {
            if (player.input.isCursorLocked(player))
            {
                Point worldCursorPos = pos.add(lastDirToCursor);
                player.axlCursorPos = worldCursorPos.addxy(-Global.level.camX, -Global.level.camY);
                lockOn(out _);
                return;
            }

            if (charState is Assassinate)
            {
                return;
            }

            Point aimDir = new Point(0, 0);

            if (Options.main.aimAnalog)
            {
                aimDir.x = Input.aimX;
                aimDir.y = Input.aimY;
            }

            bool aimLeft = player.input.isHeld(Control.AimLeft, player);
            bool aimRight = player.input.isHeld(Control.AimRight, player);
            bool aimUp = player.input.isHeld(Control.AimUp, player);
            bool aimDown = player.input.isHeld(Control.AimDown, player);

            if (aimDir.magnitude < 10)
            {
                if (aimLeft)
                {
                    aimDir.x = -100;
                }
                else if (aimRight)
                {
                    aimDir.x = 100;
                }
                if (aimUp)
                {
                    aimDir.y = -100;
                }
                else if (aimDown)
                {
                    aimDir.y = 100;
                }
            }

            aimingBackwards = player.input.isAimingBackwards(player);
            
            int aimBackwardsMod = 1;
            if (aimingBackwards && charState is not LadderClimb)
            {
                if (player.axlWeapon?.isTwoHanded(false) != true)
                {
                    if (Math.Sign(aimDir.x) == Math.Sign(xDir))
                    {
                        aimDir.x *= -1;
                    }
                    aimBackwardsMod = -1;
                }
                else
                {
                    // By design, aiming backwards with 2-handed weapons does not actually cause Axl to aim backwards like with 1-handed weapons as this would look really weird.
                    // Instead, it locks Axl's aim forward and allows him to backpedal without changing direction.
                    xDir = lastXDir;
                    if (Math.Sign(aimDir.x) != Math.Sign(xDir))
                    {
                        aimDir.x *= -1;
                    }
                }
            }

            if (aimDir.magnitude < 10)
            {
                aimDir = new Point(xDir * 100 * aimBackwardsMod, 0);
            }

            if (charState is WallSlide)
            {
                if (xDir == -1)
                {
                    if (aimDir.x < 0) aimDir.x *= -1;
                }
                if (xDir == 1)
                {
                    if (aimDir.x > 0) aimDir.x *= -1;
                }
            }

            float xOff = 0;
            float yOff = -24;
            if (charState is Crouch) yOff = -16;

            //player.axlCursorPos = pos.addxy(xOff * xDir, yOff).addxy(aimDir.x, aimDir.y).addxy(-Global.level.camX, -Global.level.camY);
            Point destCursorPos = pos.addxy(xOff * xDir, yOff).addxy(aimDir.x, aimDir.y).addxy(-Global.level.camX, -Global.level.camY);

            if (charState is Dash || charState is AirDash)
            {
                destCursorPos = destCursorPos.addxy(15 * xDir, 0);
            }

            // Try to see if where cursor will go to has auto-aim target. If it does, make that the dest, not the actual dest
            Point oldCursorPos = player.axlCursorPos;
            player.axlCursorPos = destCursorPos;
            lockOn(out Point? lockOnPoint);
            if (lockOnPoint != null)
            {
                destCursorPos = lockOnPoint.Value;
            }
            player.axlCursorPos = oldCursorPos;

            // Lerp to the new target
            //player.axlCursorPos = Point.moveTo(player.axlCursorPos, destCursorPos, Global.spf * 1000);
            if (!Options.main.aimAnalog)
            {
                player.axlCursorPos = Point.lerp(player.axlCursorPos, destCursorPos, Global.spf * 15);
            }
            else
            {
                player.axlCursorPos = destCursorPos;
            }

            lastDirToCursor = pos.directionTo(player.axlCursorWorldPos);
        }

        Point lastDirToCursor;

        public bool canUpdateAimAngle()
        {
            if (shootTime > 0) return true;
            return !(charState is LadderClimb) && !(charState is LadderEnd) && canChangeDir();
        }

        public Character getLockOnTarget()
        {
            Character newTarget = null;
            foreach (var enemy in Global.level.players)
            {
                if (enemy.character != null && enemy.character.canBeDamaged(player.alliance, player.id, null) && enemy.character.pos.distanceTo(pos) < 150 && !enemy.character.isStealthy(player.alliance))
                {
                    float distPercent = 1 - (enemy.character.pos.distanceTo(pos) / 150);
                    var dirToEnemy = getAxlBulletPos().directionTo(enemy.character.getAimCenterPos());
                    var dirToCursor = getAxlBulletPos().directionTo(player.axlGenericCursorWorldPos);

                    float angle = dirToEnemy.angleWith(dirToCursor);

                    float leeway = 22.5f;
                    if (angle < leeway + (distPercent * (90 - leeway)))
                    {
                        newTarget = enemy.character;
                        break;
                    }
                }
            }

            return newTarget;
        }

        public void lockOn(out Point? lockOnPoint)
        {
            // Check for lock on targets
            lockOnPoint = null;
            var prevTarget = axlCursorTarget;
            axlCursorTarget = null;
            axlHeadshotTarget = null;
            player.assassinCursorPos = null;

            if (!Options.main.lockOnSound) return;
            if (player.isDisguisedAxl && !player.isAxl && player.axlWeapon is not AssassinBullet) return;
            if (player.isDisguisedAxl && player.axlWeapon is UndisguiseWeapon) return;
            if (player.input.isCursorLocked(player)) return;

            axlCursorTarget = getLockOnTarget();

            if (axlCursorTarget != null && prevTarget == null && player.isMainPlayer && targetSoundCooldown == 0)
            {
                Global.playSound("axlTarget", false);
                targetSoundCooldown = Global.spf;
            }

            if (axlCursorTarget != null)
            {
                player.axlLockOnCursorPos = (axlCursorTarget as Character).getAimCenterPos();
                lockOnPoint = player.axlLockOnCursorPos.addxy(-Global.level.camX, -Global.level.camY);
                // player.axlCursorPos = (axlCursorTarget as Character).getAimCenterPos().addxy(-Global.level.camX, -Global.level.camY);

                if (player.axlWeapon is AssassinBullet)
                {
                    player.assassinCursorPos = lockOnPoint;
                }
            }
        }

        public void updateAxlScopePos()
        {
            float aimThreshold = 5;
            bool axisXMoved = false;
            bool axisYMoved = false;
            // Options.main.aimSensitivity is a float from 0 to 1.
            float distFromNormal = Options.main.aimSensitivity - 0.5f;
            float sensitivity = 1;
            if (distFromNormal > 0)
            {
                sensitivity += distFromNormal * 7.5f;
            }
            else
            {
                sensitivity += distFromNormal * 1.75f;
            }

            // Controller joystick axis move section
            if (Input.aimX > aimThreshold && Input.aimX >= Input.lastAimX)
            {
                player.axlScopeCursorWorldPos.x += Global.spf * Global.screenW * (Input.aimX / 100f) * sensitivity;
                axisXMoved = true;
            }
            else if (Input.aimX < -aimThreshold && Input.aimX <= Input.lastAimX)
            {
                player.axlScopeCursorWorldPos.x -= Global.spf * Global.screenW * (MathF.Abs(Input.aimX) / 100f) * sensitivity;
                axisXMoved = true;
            }
            if (Input.aimY > aimThreshold && Input.aimY >= Input.lastAimY)
            {
                player.axlScopeCursorWorldPos.y += Global.spf * Global.screenW * (Input.aimY / 100f) * sensitivity;
                axisYMoved = true;
            }
            else if (Input.aimY < -aimThreshold && Input.aimY <= Input.lastAimY)
            {
                player.axlScopeCursorWorldPos.y -= Global.spf * Global.screenW * (MathF.Abs(Input.aimY) / 100f) * sensitivity;
                axisYMoved = true;
            }

            // Controller or keyboard button based aim section
            if (!axisXMoved)
            {
                if (player.input.isHeld(Control.AimLeft, player))
                {
                    player.axlScopeCursorWorldPos.x -= Global.spf * 200 * sensitivity;
                    axisXMoved = true;
                }
                else if (player.input.isHeld(Control.AimRight, player))
                {
                    player.axlScopeCursorWorldPos.x += Global.spf * 200 * sensitivity;
                    axisXMoved = true;
                }
            }
            if (!axisYMoved)
            {
                if (player.input.isHeld(Control.AimUp, player))
                {
                    player.axlScopeCursorWorldPos.y -= Global.spf * 200 * sensitivity;
                    axisYMoved = true;
                }
                else if (player.input.isHeld(Control.AimDown, player))
                {
                    player.axlScopeCursorWorldPos.y += Global.spf * 200 * sensitivity;
                    axisYMoved = true;
                }
            }

            // Mouse based aim
            if (!Menu.inMenu && !player.isAI)
            {
                if (Options.main.useMouseAim)
                {
                    player.axlScopeCursorWorldPos.x += Input.mouseDeltaX * 0.125f * sensitivity;
                    player.axlScopeCursorWorldPos.y += Input.mouseDeltaY * 0.125f * sensitivity;
                }
            }

            // Aim assist
            if (!Options.main.useMouseAim && Options.main.lockOnSound)// && !axisXMoved && !axisYMoved)
            {
                Character target = null;
                float bestDist = float.MaxValue;
                foreach (var enemy in Global.level.players)
                {
                    if (enemy.character != null && enemy.character.canBeDamaged(player.alliance, player.id, null) && !enemy.character.isStealthy(player.alliance))
                    {
                        float cursorDist = enemy.character.getAimCenterPos().distanceTo(player.axlScopeCursorWorldPos);
                        if (cursorDist < bestDist)
                        {
                            bestDist = cursorDist;
                            target = enemy.character;
                        }
                    }
                }
                const float aimAssistRange = 25;
                if (target != null && bestDist < aimAssistRange)
                {
                    //float aimAssistPower = (float)Math.Pow(1 - (bestDist / aimAssistRange), 2);
                    player.axlScopeCursorWorldPos = Point.lerp(player.axlScopeCursorWorldPos, target.getAimCenterPos(), Global.spf * 5);
                    //player.axlScopeCursorWorldPos = target.getAimCenterPos();
                }
            }

            // Aimbot
            if (!player.isAI)
            {
                //var target = Global.level.getClosestTarget(player.axlScopeCursorWorldPos, player.alliance, true);
                //if (target != null && target.pos.distanceTo(player.axlScopeCursorWorldPos) < 100) player.axlScopeCursorWorldPos = target.getAimCenterPos();
            }

            Point centerPos = getAimCenterPos();
            if (player.axlScopeCursorWorldPos.distanceTo(centerPos) > player.zoomRange)
            {
                player.axlScopeCursorWorldPos = centerPos.add(centerPos.directionTo(player.axlScopeCursorWorldPos).normalize().times(player.zoomRange));
            }

            getMouseTargets();
        }

        public void getMouseTargets()
        {
            axlCursorTarget = null;
            axlHeadshotTarget = null;
            
            int cursorSize = 1;
            var shape = new Rect(player.axlGenericCursorWorldPos.x - cursorSize, player.axlGenericCursorWorldPos.y - cursorSize, player.axlGenericCursorWorldPos.x + cursorSize, player.axlGenericCursorWorldPos.y + cursorSize).getShape();
            var hit = Global.level.checkCollisionsShape(shape, new List<GameObject>() { this }).FirstOrDefault(c => c.gameObject is IDamagable);
            if (hit != null)
            {
                var target = hit.gameObject as IDamagable;
                if (target.canBeDamaged(player.alliance, player.id, null))
                {
                    axlCursorTarget = target;
                }
            }
            foreach (var enemy in Global.level.players)
            {
                if (enemy.character != null && enemy.character.canBeDamaged(player.alliance, player.id, null) && enemy.character.getHeadPos() != null)
                {
                    if (player.axlGenericCursorWorldPos.distanceTo(enemy.character.getHeadPos().Value) < headshotRadius)
                    {
                        axlCursorTarget = enemy.character as IDamagable;
                        axlHeadshotTarget = enemy.character;
                    }
                }
            }
        }

        public void updateAxlCursorPos()
        {
            float aimThreshold = 5;
            bool axisXMoved = false;
            bool axisYMoved = false;
            // Options.main.aimSensitivity is a float from 0 to 1.
            float distFromNormal = Options.main.aimSensitivity - 0.5f;
            float sensitivity = 1;
            if (distFromNormal > 0)
            {
                sensitivity += distFromNormal * 7.5f;
            }
            else
            {
                sensitivity += distFromNormal * 1.75f;
            }

            // Controller joystick axis move section
            if (Input.aimX > aimThreshold && Input.aimX >= Input.lastAimX)
            {
                player.axlCursorPos.x += Global.spf * Global.screenW * (Input.aimX / 100f) * sensitivity;
                axisXMoved = true;
            }
            else if (Input.aimX < -aimThreshold && Input.aimX <= Input.lastAimX)
            {
                player.axlCursorPos.x -= Global.spf * Global.screenW * (MathF.Abs(Input.aimX) / 100f) * sensitivity;
                axisXMoved = true;
            }
            if (Input.aimY > aimThreshold && Input.aimY >= Input.lastAimY)
            {
                player.axlCursorPos.y += Global.spf * Global.screenW * (Input.aimY / 100f) * sensitivity;
                axisYMoved = true;
            }
            else if (Input.aimY < -aimThreshold && Input.aimY <= Input.lastAimY)
            {
                player.axlCursorPos.y -= Global.spf * Global.screenW * (MathF.Abs(Input.aimY) / 100f) * sensitivity;
                axisYMoved = true;
            }

            // Controller or keyboard button based aim section
            if (!axisXMoved)
            {
                if (player.input.isHeld(Control.AimLeft, player))
                {
                    player.axlCursorPos.x -= Global.spf * 200 * sensitivity;
                }
                else if (player.input.isHeld(Control.AimRight, player))
                {
                    player.axlCursorPos.x += Global.spf * 200 * sensitivity;
                }
            }
            if (!axisYMoved)
            {
                if (player.input.isHeld(Control.AimUp, player))
                {
                    player.axlCursorPos.y -= Global.spf * 200 * sensitivity;
                }
                else if (player.input.isHeld(Control.AimDown, player))
                {
                    player.axlCursorPos.y += Global.spf * 200 * sensitivity;
                }
            }

            // Mouse based aim
            if (!Menu.inMenu && !player.isAI)
            {
                if (Options.main.useMouseAim)
                {
                    player.axlCursorPos.x += Input.mouseDeltaX * 0.125f * sensitivity;
                    player.axlCursorPos.y += Input.mouseDeltaY * 0.125f * sensitivity;
                }
                player.axlCursorPos.x = Helpers.clamp(player.axlCursorPos.x, 0, Global.viewScreenW);
                player.axlCursorPos.y = Helpers.clamp(player.axlCursorPos.y, 0, Global.viewScreenH);
            }

            if (isWarpIn())
            {
                player.axlCursorPos = getCenterPos().addxy(-Global.level.camX + 50 * xDir, -Global.level.camY);
            }

            // aimbot
            if (!player.isAI)
            {
                //var target = Global.level.getClosestTarget(pos, player.alliance, true);
                //if (target != null) player.axlCursorPos = target.pos.addxy(-Global.level.camX, -Global.level.camY - (target.charState is InRideArmor ? 0 : 16));
            }

            getMouseTargets();
        }

        public bool isAxlLadderShooting()
        {
            if (player.weapon is AssassinBullet) return false;
            if (recoilTime > 0) return true;
            bool canShoot = charState.canShoot() && !player.weapon.noAmmo() && player.axlWeapon != null && !player.axlWeapon.isTwoHanded(true) && shootTime == 0;
            if (player.input.isHeld(Control.Shoot, player) && canShoot)
            {
                return true;
            }
            return false;
        }

        public void renderAxl()
        {
            if (!ownedByLocalPlayer)
            {
                if (shouldDrawArmBS.getValue())
                {
                    drawArm(netArmAngle);
                }

                if (isNonOwnerZoom)
                {
                    Color laserColor = new Color(255, 0, 0, 160);
                    DrawWrappers.DrawLine(nonOwnerScopeStartPos.x, nonOwnerScopeStartPos.y, nonOwnerScopeEndPos.x, nonOwnerScopeEndPos.y, laserColor, 2, ZIndex.HUD);
                    DrawWrappers.DrawCircle(nonOwnerScopeEndPos.x, nonOwnerScopeEndPos.y, 2f, true, laserColor, 1, ZIndex.HUD);
                }

                return;
            }

            drawAxlCursor();
            
            if (player.axlWeapon != null) netArmAngle = getShootAngle();

            float angleOffset = 0;
            if (recoilTime > 0.1f) angleOffset = (0.2f - recoilTime) * 50;
            else if (recoilTime < 0.1f && recoilTime > 0) angleOffset = (0.1f - (0.1f - recoilTime)) * 50;
            angleOffset *= -axlXDir;
            netArmAngle += angleOffset;

            if (charState is DarkHoldState dhs)
            {
                netArmAngle = dhs.lastArmAngle;
            }

            if (charState is LadderClimb)
            {
                if (isAxlLadderShooting())
                {
                    xDir = (pos.x > player.axlGenericCursorWorldPos.x ? -1 : 1);
                    changeSprite("axl_ladder_shoot", true);
                }
                else
                {
                    changeSprite("axl_ladder_climb", true);
                }
            }

            if (shootTime > 0 && !muzzleFlash.isAnimOver())
            {
                muzzleFlash.xDir = axlXDir;
                muzzleFlash.visible = true;
                muzzleFlash.angle = netArmAngle;
                muzzleFlash.pos = getAxlBulletPos();
                if (muzzleFlash.sprite.name.StartsWith("axl_raygun_flash"))
                {
                    muzzleFlash.xScale = 0.75f;
                    muzzleFlash.yScale = 0.75f;
                    muzzleFlash.setzIndex(zIndex - 2);
                }
                else
                {
                    muzzleFlash.xScale = 1f;
                    muzzleFlash.yScale = 1f;
                    muzzleFlash.setzIndex(zIndex + 200);
                }
            }
            else
            {
                muzzleFlash.visible = false;
            }

            if (shouldDrawArm())
            {
                drawArm(netArmAngle);
            }

            if (Global.showHitboxes)
            {
                Point bulletPos = getAxlBulletPos();
                DrawWrappers.DrawLine(bulletPos.x, bulletPos.y, player.axlGenericCursorWorldPos.x, player.axlGenericCursorWorldPos.y, Color.Magenta, 1, ZIndex.Default + 1);
            }

            //DEBUG CODE
            /*
            if (Keyboard.IsKeyPressed(Key.I)) player.axlWeapon.renderAngleOffset++;
            else if (Keyboard.IsKeyPressed(Key.J)) player.axlWeapon.renderAngleOffset--;
            Global.debugString1 = player.axlWeapon.renderAngleOffset.ToString();
            if (Keyboard.IsKeyPressed(Key.K)) player.axlWeapon.shootAngleOffset++;
            else if (Keyboard.IsKeyPressed(Key.L)) player.axlWeapon.shootAngleOffset--;
            Global.debugString2 = player.axlWeapon.shootAngleOffset.ToString();
            */
        }

        float axlCursorAngle;
        public void drawAxlCursor()
        {
            if (player.isAI) return;
            if (!ownedByLocalPlayer) return;
            if (Global.level.gameMode.isOver) return;
            if (isZooming() && !isZoomOutPhase1Done) return;
            // if (isWarpIn()) return;

            if (Options.main.useMouseAim || Global.showHitboxes)
            {
                drawBloom();
                Global.sprites["axl_cursor"].draw(0, player.axlCursorWorldPos.x, player.axlCursorWorldPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
                if (player.assassinHitPos?.isHeadshot == true && player.weapon is AssassinBullet && Global.level.isTraining())
                {
                    Global.sprites["hud_kill"].draw(0, player.axlCursorWorldPos.x, player.axlCursorWorldPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
                }
            }
            if (!Options.main.useMouseAim)
            {
                if (player.axlWeapon != null && (player.axlWeapon is AssassinBullet || player.input.isCursorLocked(player)))
                {
                    Point bulletPos = getAxlBulletPos();
                    float radius = 120;
                    float ang = getShootAngle();
                    float x = Helpers.cosd(ang) * radius * getShootXDir();
                    float y = Helpers.sind(ang) * radius * getShootXDir();
                    DrawWrappers.DrawLine(bulletPos.x, bulletPos.y, bulletPos.x + x, bulletPos.y + y, new Color(255, 0, 0, 128), 2, ZIndex.HUD, true);
                    if (axlCursorTarget != null && player.assassinHitPos?.isHeadshot == true && player.weapon is AssassinBullet && Global.level.isTraining())
                    {
                        Global.sprites["hud_kill"].draw(0, player.axlLockOnCursorPos.x, player.axlLockOnCursorPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
                    }
                }
                if (axlCursorTarget != null && !isAnyZoom())
                {
                    axlCursorAngle += Global.spf * 360;
                    if (axlCursorAngle > 360) axlCursorAngle -= 360;
                    Global.sprites["axl_cursor_x7"].draw(0, player.axlLockOnCursorPos.x, player.axlLockOnCursorPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1, angle: axlCursorAngle);
                    //drawBloom();
                }
            }

            /*
            if (player.weapon.ammo <= 0)
            {
                if (player.weapon.rechargeCooldown > 0)
                {
                    float textPosX = player.axlCursorPos.x;
                    float textPosY = player.axlCursorPos.y - 20;
                    if (!Options.main.useMouseAim)
                    {
                        textPosX = pos.x - Global.level.camX / Global.viewSize;
                        textPosY = (pos.y - 50 - Global.level.camY) / Global.viewSize;
                    }
                    DrawWrappers.DeferTextDraw(() =>
                    {
                        Helpers.drawTextStd("Reload:" + player.weapon.rechargeCooldown.ToString("0.0"), textPosX, textPosY, Alignment.Center, fontSize: 20, outlineColor: Helpers.getAllianceColor());
                    });
                }
            }
            */
        }

        public void drawBloom()
        {
            Global.sprites["axl_cursor_top"].draw(0, player.axlCursorWorldPos.x, player.axlCursorWorldPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
            Global.sprites["axl_cursor_bottom"].draw(0, player.axlCursorWorldPos.x, player.axlCursorWorldPos.y + 1, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
            Global.sprites["axl_cursor_left"].draw(0, player.axlCursorWorldPos.x, player.axlCursorWorldPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
            Global.sprites["axl_cursor_right"].draw(0, player.axlCursorWorldPos.x + 1, player.axlCursorWorldPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
            Global.sprites["axl_cursor_dot"].draw(0, player.axlCursorWorldPos.x, player.axlCursorWorldPos.y, 1, 1, null, 1, 1, 1, ZIndex.Default + 1);
        }

        float mcFrameTime;
        float mcMaxFrameTime = 0.03f;
        int mcFrameIndex;
        public void drawArm(float angle)
        {
            long zIndex = this.zIndex - 1;
            Point gunArmOrigin;
            if (charState is Assassinate assasinate)
            {
                gunArmOrigin = getAxlGunArmOrigin();
                getAxlArmSprite().draw(0, gunArmOrigin.x, gunArmOrigin.y, axlXDir, 1, getRenderEffectSet(), 1, 1, 1, zIndex, angle: angle, shaders: getShaders());
                return;
            }

            if (player.axlWeapon != null && player.axlWeapon.isTwoHanded(false))
            {
                zIndex = this.zIndex + 1;
            }
            gunArmOrigin = getAxlGunArmOrigin();

            if (player.axlWeapon is DoubleBullet)
            {
                var armPos = getDoubleBulletArmPos();
                if (shouldDraw())
                {
                    Global.sprites["axl_arm_pistol2"].draw(0, gunArmOrigin.x + armPos.x * axlXDir, gunArmOrigin.y + armPos.y, axlXDir, 1, getRenderEffectSet(), 1, 1, 1, this.zIndex + 100, angle: angle, shaders: getShaders(), actor: this);
                }
            }

            if (shouldDraw())
            {
                int frameIndex = 0;
                if (player.axlWeapon is IceGattling)
                {
                    revIndex += revTime * Global.spf * 60;
                    if (revIndex >= 4)
                    {
                        revIndex = 0;
                    }

                    if (revTime > 0)
                    {
                        frameIndex = (int)revIndex;
                    }
                }
                if (player.axlWeapon is AxlBullet ab && ab.type == (int)AxlBulletWeaponType.MetteurCrash)
                {
                    if (shootTime > 0)
                    {
                        mcFrameTime += Global.spf;
                        if (mcFrameTime > mcMaxFrameTime)
                        {
                            mcFrameTime = 0;
                            mcFrameIndex++;
                            if (mcFrameIndex > 3) mcFrameIndex = 0;
                        }
                    }
                    frameIndex = mcFrameIndex;
                }
                getAxlArmSprite().draw(frameIndex, gunArmOrigin.x, gunArmOrigin.y, axlXDir, 1, getRenderEffectSet(), 1, 1, 1, zIndex, angle: angle, shaders: getShaders(), actor: this);
            }
        }

        public bool shouldDrawArm()
        {
            if (charState is DarkHoldState dhs)
            {
                return dhs.shouldDrawAxlArm;
            }

            bool ladderClimb = false;
            if (charState is LadderClimb)
            {
                if (!isAxlLadderShooting())
                {
                    ladderClimb = true;
                }
            }
            else if (charState is LadderEnd) ladderClimb = true;

            return !(charState is HyperAxlStart || isWarpIn() || charState is Hurt || charState is Die || charState is Frozen || charState is InRideArmor || charState is DodgeRoll || charState is Crystalized || charState is VileMK2Grabbed || charState is KnockedDown 
                || sprite.name.Contains("win") || sprite.name.Contains("lose") || ladderClimb || charState is DeadLiftGrabbed || charState is UPGrabbed || charState is WhirlpoolGrabbed || charState is InRideChaser);
        }

        public Point getAxlBulletDir()
        {
            Point origin = player.character.getAxlBulletPos();
            Point cursorPos = getCorrectedCursorPos();
            return origin.directionTo(cursorPos).normalize();
        }

        public ushort netAxlArmSpriteIndex;
        public string getAxlArmSpriteName()
        {
            if (gaeaShield != null)
            {
                return "axl_arm_icegattling2";
            }

            return player.axlWeapon.sprite;
        }

        public Sprite getAxlArmSprite()
        {
            if (!ownedByLocalPlayer)
            {
                return Global.sprites[Global.spriteNames[netAxlArmSpriteIndex]];
            }

            return Global.sprites[getAxlArmSpriteName()];
        }

        public Point getCorrectedCursorPos()
        {
            if (player.axlWeapon == null) return new Point();
            Point cursorPos = player.axlGenericCursorWorldPos;
            Point gunArmOrigin = getAxlGunArmOrigin();

            Sprite sprite = getAxlArmSprite();
            float minimumAimRange = sprite.frames[0].POIs[0].magnitude + 5;

            if (gunArmOrigin.distanceTo(cursorPos) < minimumAimRange)
            {
                Point angleDir = Point.createFromAngle(getShootAngle(true));
                cursorPos = cursorPos.add(angleDir.times(minimumAimRange));
            }
            return cursorPos;
        }

        public Point getAxlHitscanPoint(float maxRange)
        {
            Point bulletPos = getAxlBulletPos();
            Point bulletDir = getAxlBulletDir();
            return bulletPos.add(bulletDir.times(maxRange));
        }

        public Point getMuzzleOffset(float angle)
        {
            if (player.axlWeapon == null) return new Point();
            Sprite sprite = getAxlArmSprite();
            Point muzzlePOI = sprite.frames[0].POIs[0];

            float horizontalOffX = 0;// Helpers.cosd(angle) * muzzlePOI.x;
            float horizontalOffY = 0;// Helpers.sind(angle) * muzzlePOI.x;

            float verticalOffX = -axlXDir * Helpers.sind(angle) * muzzlePOI.y;
            float verticalOffY = axlXDir * Helpers.cosd(angle) * muzzlePOI.y;

            return new Point(horizontalOffX + verticalOffX, horizontalOffY + verticalOffY);
        }

        public Point getAxlBulletPos(int poiIndex = 0)
        {
            if (player.axlWeapon == null) return new Point();

            Point gunArmOrigin = getAxlGunArmOrigin();

            var doubleBullet = player.weapon as DoubleBullet;
            if (doubleBullet != null && doubleBullet.isSecondShot)
            {
                Point dbArmPos = getDoubleBulletArmPos();
                gunArmOrigin = gunArmOrigin.addxy(dbArmPos.x * getAxlXDir(), dbArmPos.y);
            }

            Sprite sprite = getAxlArmSprite();
            float angle = getShootAngle(ignoreXDir: true) + sprite.frames[0].POIs[poiIndex].angle * axlXDir;
            Point angleDir = Point.createFromAngle(angle).times(sprite.frames[0].POIs[poiIndex].magnitude);
            
            return gunArmOrigin.addxy(angleDir.x, angleDir.y);
        }

        public Point getAxlScopePos()
        {
            if (player.axlWeapon == null) return new Point();
            Point gunArmOrigin = getAxlGunArmOrigin();
            Sprite sprite = getAxlArmSprite();
            if (sprite.frames[0].POIs.Count < 2) return new Point();
            float angle = getShootAngle(ignoreXDir: true) + sprite.frames[0].POIs[1].angle * axlXDir;
            Point angleDir = Point.createFromAngle(angle).times(sprite.frames[0].POIs[1].magnitude);
            return gunArmOrigin.addxy(angleDir.x, angleDir.y);
        }

        public float getShootAngle(bool ignoreXDir = false)
        {
            if (voltTornadoTime > 0)
            {
                return -90 * getAxlXDir();
            }

            Point gunArmOrigin = getAxlGunArmOrigin();
            Point cursorPos = player.axlGenericCursorWorldPos;
            float angle = gunArmOrigin.directionTo(cursorPos).angle;

            Point adjustedOrigin = gunArmOrigin.add(getMuzzleOffset(angle));
            float adjustedAngle = adjustedOrigin.directionTo(cursorPos).angle;

            // DEBUG CODE
            //Global.debugString1 = angle.ToString();
            //Global.debugString2 = adjustedAngle.ToString();
            //DrawWrappers.DrawPixel(adjustedOrigin.x, adjustedOrigin.y, Color.Red, ZIndex.Default + 1);
            //DrawWrappers.DrawPixel(gunArmOrigin.x, gunArmOrigin.y, Color.Red, ZIndex.Default + 1);
            //Point angleLine = Point.createFromAngle(angle).times(100);
            //DrawWrappers.DrawLine(gunArmOrigin.x, gunArmOrigin.y, gunArmOrigin.x + angleLine.x, gunArmOrigin.y + angleLine.y, Color.Magenta, 1, ZIndex.Default + 1);
            //Point angleLine2 = Point.createFromAngle(angleWithOffset).times(100);
            //DrawWrappers.DrawLine(gunArmOrigin.x, gunArmOrigin.y, gunArmOrigin.x + angleLine2.x, gunArmOrigin.y + angleLine2.y, Color.Red, 1, ZIndex.Default + 1);
            // END DEBUG CODE

            if (axlXDir == -1 && !ignoreXDir) adjustedAngle += 180;
                
            return adjustedAngle;
        }

        public Point getTwoHandedOffset()
        {
            if (player.axlWeapon == null) return new Point();
            if (player.axlWeapon.isTwoHanded(false))
            {
                if (player.axlWeapon is FlameBurner) return new Point(-6, 1);
                else if (player.axlWeapon is IceGattling) return new Point(-6, 1);
                else return new Point(-6, 1);
            }
            return new Point();
        }

        public Point getAxlGunArmOrigin()
        {
            Point retPoint;
            var pois = sprite.getCurrentFrame().POIs;
            Point offset = getTwoHandedOffset();
            if (pois.Count > 0)
            {
                retPoint = pos.addxy((offset.x + pois[0].x) * axlXDir, pois[0].y + offset.y);
            }
            else retPoint = pos.addxy((offset.x + 3) * axlXDir, -21 + offset.y);

            return retPoint;
        }

        public void addDNACore(Character hitChar)
        {
            if (!player.ownedByLocalPlayer) return;
            if (!player.isAxl) return;
            if (player.isAxl && player.isDisguisedAxl) return;
            if (Global.level.is1v1()) return;

            if (player.weapons.Count < 8 || Global.level.isTraining())
            {
                int loopCount = 1;
                if (Global.debug && Global.debugDNACores && Global.level.isTraining()) loopCount = 4;
                for (int i = 0; i < loopCount; i++)
                {
                    var dnaCoreWeapon = new DNACore(hitChar);
                    dnaCoreWeapon.index = (int)WeaponIds.DNACore - player.weapons.Count;
                    player.weapons.Add(dnaCoreWeapon);
                    player.savedDNACoreWeapons.Add(dnaCoreWeapon);
                }
            }
        }

        public int getAxlXDir()
        {
            if (player.axlWeapon != null && (player.axlWeapon.isTwoHanded(false)))
            {
                return pos.x < player.axlGenericCursorWorldPos.x ? 1 : -1;
            }
            return xDir;
        }

        public void addTransformAnim()
        {
            transformAnim = new Anim(pos, "axl_transform", xDir, null, true);
            playSound("transform");
            if (ownedByLocalPlayer)
            {
                Global.serverClient?.rpc(RPC.playerToggle, (byte)player.id, (byte)RPCToggleType.AddTransformEffect);
            }
        }

        public bool isWhiteAxl()
        {
            return player.isAxl && whiteAxlTime > 0;
        }

        public bool isStealthMode()
        {
            return player.isAxl && isInvisible();
        }

        public bool isStealthModeSynced()
        {
            return player.isAxl && isInvisibleBS.getValue() == true;
        }

        float stealthScrapTime;

        public void updateStealthMode()
        {
            stealthScrapTime += Global.spf;
            stingChargeTime = 8;
            if (stealthScrapTime > 1)
            {
                stealthScrapTime = 0;
                player.scrap--;
                if (player.scrap <= 0)
                {
                    player.scrap = 0;
                    stingChargeTime = 0;
                }
            }

            updateAwakenedAura();
        }
    }

    public class HyperAxlStart : CharState
    {
        public float radius = 200;
        public float time;
        public HyperAxlStart(bool isGrounded) : base(isGrounded ? "hyper_start" : "hyper_start_air", "", "", "")
        {
            invincible = true;
        }

        public override void update()
        {
            base.update();

            foreach (var weapon in player.weapons)
            {
                for (int i = 0; i < 10; i++) weapon.rechargeAmmo(0.1f);
            }

            if (character.loopCount > 8)
            {
                character.whiteAxlTime = character.maxHyperAxlTime;
                RPC.setHyperZeroTime.sendRpc(character.player.id, character.whiteAxlTime, 1);
                character.playSound("ching");
                if (player.input.isHeld(Control.Jump, player)) character.changeState(new Hover(), true);
                else character.changeState(new Idle(), true);
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            if (!character.hyperAxlUsed)
            {
                character.hyperAxlUsed = true;
                character.player.scrap -= 10;
            }
            character.useGravity = false;
            character.vel = new Point();
            character.fillHealthToMax();
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.useGravity = true;
            if (character != null)
            {
                character.invulnTime = 0.5f;
            }
        }
    }

    public class Hover : CharState
    {
        float hoverTime;
        Anim hoverExhaust;
        public Hover() : base("hover", "hover", "hover", "hover")
        {
        }

        public override void update()
        {
            base.update();

            accuracy = 0;
            Point prevPos = character.pos;
            airCode();
            if (character.pos.x != prevPos.x)
            {
                accuracy = 5;
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
            hoverExhaust.changePos(exhaustPos());
            hoverExhaust.xDir = character.getAxlXDir();
            if ((hoverTime > 2 && !character.isWhiteAxl()) || !character.player.input.isHeld(Control.Jump, character.player))
            {
                character.changeState(new Fall(), true);
            }
        }

        public Point exhaustPos()
        {
            if (character.currentFrame.POIs.Count == 0) return character.pos;
            Point exhaustPOI = character.currentFrame.POIs.Last();
            return character.pos.addxy(exhaustPOI.x * character.getAxlXDir(), exhaustPOI.y);
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.useGravity = false;
            character.vel = new Point();
            hoverExhaust = new Anim(exhaustPos(), "hover_exhaust", character.getAxlXDir(), player.getNextActorNetId(), false, sendRpc: true);
            hoverExhaust.setzIndex(ZIndex.Character - 1);
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.useGravity = true;
            hoverExhaust.destroySelf();
        }
    }

    public class DodgeRoll : CharState
    {
        public float dashTime = 0;
        public int initialDashDir;

        public DodgeRoll() : base("roll", "", "")
        {
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.isDashing = true;
            character.burnTime -= 2;
            if (character.burnTime < 0)
            {
                character.burnTime = 0;
            }

            initialDashDir = character.xDir;
            if (player.input.isHeld(Control.Left, player)) initialDashDir = -1;
            else if (player.input.isHeld(Control.Right, player)) initialDashDir = 1;
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.dodgeRollCooldown = Character.maxDodgeRollCooldown;
        }

        public override void update()
        {
            base.update();
            groundCode();

            if (character.isAnimOver())
            {
                character.changeState(new Idle(), true);
                return;
            }

            if (character.frameIndex >= 4) return;

            dashTime += Global.spf;

            var move = new Point(0, 0);
            move.x = character.getRunSpeed() * character.getDashSpeed() * initialDashDir;
            character.move(move);
            if (stateTime > 0.1)
            {
                stateTime = 0;
                //new Anim(this.character.pos.addxy(0, -4), "dust", this.character.xDir, null, true);
            }
        }
    }
}
