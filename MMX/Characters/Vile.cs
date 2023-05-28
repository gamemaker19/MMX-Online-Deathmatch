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
        public float vulcanLingerTime;
        public float calldownMechCooldown;
        public float grabCooldown;
        public const float maxCalldownMechCooldown = 2;
        public const int callNewMechScrapCost = 5;
        public bool alreadySummonedNewMech;
        float mechBusterCooldown;
        public bool usedAmmoLastFrame;
        public float vileLadderShootCooldown;
        public int buckshotDanceNum;
        public float vileAmmoRechargeCooldown;
        public bool isShootingLongshotGizmo;
        public int longshotGizmoCount;
        public float gizmoCooldown;
        public bool hasFrozenCastleBarrier()
        {
            return player.frozenCastle;
        }
        public bool summonedGoliath;
        public RideArmor vileStartRideArmor;
        public RideArmor mk5RideArmorPlatform;
        public int vileForm;
        public bool isVileMK2 { get { return vileForm == 1; } }
        public bool isVileMK5 { get { return vileForm == 2; } }
        public float vileHoverTime;
        public float vileMaxHoverTime = 6;

        public const float frozenCastlePercent = 0.125f;
        public const float speedDevilRunSpeed = 110;
        public const int frozenCastleCost = 3;
        public const int speedDevilCost = 3;
        public bool lastFrameWeaponLeftHeld;
        public bool lastFrameWeaponRightHeld;
        public int cannonAimNum;

        public Sprite getCannonSprite(out Point poiPos, out int zIndexDir)
        {
            poiPos = getCenterPos();
            zIndexDir = 0;

            string vilePrefix = "vile_";
            if (isVileMK2) vilePrefix = "vilemk2_";
            if (isVileMK5) vilePrefix = "vilemk5_";
            string cannonSprite = vilePrefix + "cannon";
            for (int i = 0; i < currentFrame.POIs.Count; i++)
            {
                var poi = currentFrame.POIs[i];
                var tag = currentFrame.POITags[i] ?? "";
                zIndexDir = tag.EndsWith("b") ? -1 : 1;
                int? frameIndexToDraw = null;
                if (tag.StartsWith("cannon1") && cannonAimNum == 0) frameIndexToDraw = 0;
                if (tag.StartsWith("cannon2") && cannonAimNum == 1) frameIndexToDraw = 1;
                if (tag.StartsWith("cannon3") && cannonAimNum == 2) frameIndexToDraw = 2;
                if (tag.StartsWith("cannon4") && cannonAimNum == 3) frameIndexToDraw = 3;
                if (tag.StartsWith("cannon5") && cannonAimNum == 4) frameIndexToDraw = 4;
                if (frameIndexToDraw != null)
                {
                    poiPos = new Point(pos.x + (poi.x * getShootXDirSynced()), pos.y + poi.y);
                    return Global.sprites[cannonSprite];
                }
            }
            return null;
        }

        public Point setCannonAim(Point shootDir)
        {
            float shootY = -shootDir.y;
            float shootX = MathF.Abs(shootDir.x);
            float ratio = shootY / shootX;
            if (ratio > 1.25f) cannonAimNum = 3;
            else if (ratio <= 1.25f && ratio > 0.75f) cannonAimNum = 2;
            else if (ratio <= 0.75f && ratio > 0.25f) cannonAimNum = 1;
            else if (ratio <= 0.25f && ratio > -0.25f) cannonAimNum = 0;
            else cannonAimNum = 4;

            var cannonSprite = getCannonSprite(out Point poiPos, out _);
            Point? nullablePos = cannonSprite?.frames?.ElementAtOrDefault(cannonAimNum)?.POIs?.FirstOrDefault();
            if (nullablePos == null)
            {
            }
            Point cannonSpritePOI = nullablePos ?? Point.zero;

            return poiPos.addxy(cannonSpritePOI.x * getShootXDir(), cannonSpritePOI.y);
        }

        public void updateVile()
        {
            if (mk5RideArmorPlatform != null && mk5RideArmorPlatform.destroyed)
            {
                mk5RideArmorPlatform = null;
            }
            if (mk5RideArmorPlatform != null)
            {
                changePos(mk5RideArmorPlatform.getMK5Pos());
                xDir = mk5RideArmorPlatform.xDir;
                grounded = true;
            }

            if ((grounded || charState is LadderClimb || charState is LadderEnd || charState is WallSlide) && vileHoverTime > 0)
            {
                vileHoverTime -= Global.spf * 6;
                if (vileHoverTime < 0) vileHoverTime = 0;
            }

            //bool isShootingVulcan = sprite.name.EndsWith("shoot") && player.weapon is Vulcan;
            bool isShootingVulcan = vulcanLingerTime <= 0.1;
            if (isShootingVulcan)
            {
                vileAmmoRechargeCooldown = 0.15f;
            }

            if (vileAmmoRechargeCooldown > 0)
            {
                Helpers.decrementTime(ref vileAmmoRechargeCooldown);
            }
            else if (usedAmmoLastFrame)
            {
                usedAmmoLastFrame = false;
            }
            else if (!isShootingLongshotGizmo && !isShootingVulcan)
            {
                player.vileAmmo += Global.spf * 15;
                if (player.vileAmmo > player.vileMaxAmmo)
                {
                    player.vileAmmo = player.vileMaxAmmo;
                }
            }

            if (player.vileAmmo >= player.vileMaxAmmo)
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
                    player.vileAmmo = Helpers.clampMax(player.vileAmmo + 1, player.vileMaxAmmo);
                    playSound("heal", forcePlay: true);
                }
            }

            if (vulcanLingerTime <= 0.1f && player.weapon.shootTime == 0f)
            {
                vulcanLingerTime += Global.spf;
                if (vulcanLingerTime > 0.1f && sprite.name.EndsWith("shoot"))
                {
                    changeSpriteFromName(charState.sprite, resetFrame: false);
                }
            }

            player.vileStunShotWeapon.update();
            player.vileMissileWeapon.update();
            player.vileRocketPunchWeapon.update();
            player.vileNapalmWeapon.update();
            player.vileBallWeapon.update();
            player.vileCutterWeapon.update();
            player.vileLaserWeapon.update();
            player.vileFlamethrowerWeapon.update();

            if (calldownMechCooldown > 0)
            {
                calldownMechCooldown -= Global.spf;
                if (calldownMechCooldown < 0) calldownMechCooldown = 0;
            }
            Helpers.decrementTime(ref grabCooldown);
            Helpers.decrementTime(ref vileLadderShootCooldown);
            Helpers.decrementTime(ref mechBusterCooldown);
            Helpers.decrementTime(ref gizmoCooldown);

            if (player.weapon is not AssassinBullet && (player.vileLaserWeapon.type > -1 || isVileMK5))
            {
                if (player.input.isHeld(Control.Special1, player) && charState is not Die && invulnTime == 0 && flag == null && player.vileAmmo >= player.vileLaserWeapon.getAmmoUsage(0))
                {
                    increaseCharge();
                }
                else
                {
                    if (isCharging() && getChargeLevel() >= 3)
                    {
                        if (getChargeLevel() >= 4 && isVileMK5)
                        {
                            changeState(new HexaInvoluteState(), true);
                        }
                        else
                        {
                            player.vileLaserWeapon.vileShoot(WeaponIds.VileLaser, this);
                        }
                    }
                    stopCharge();
                }
                chargeLogic();
            }

            var raState = charState as InRideArmor;
            if (rideArmor != null && raState != null && !raState.isHiding)
            {
                if (rideArmor.rideArmorState is RAIdle || rideArmor.rideArmorState is RAJump || rideArmor.rideArmorState is RAFall || rideArmor.rideArmorState is RADash)
                {
                    bool stunShotPressed = player.input.isPressed(Control.Special1, player);
                    bool goliathShotPressed = player.input.isPressed(Control.WeaponLeft, player) || player.input.isPressed(Control.WeaponRight, player);

                    if (rideArmor.raNum == 4 && Options.main.swapGoliathInputs)
                    {
                        bool oldStunShotPressed = stunShotPressed;
                        stunShotPressed = goliathShotPressed;
                        goliathShotPressed = oldStunShotPressed;
                    }

                    if (stunShotPressed && !player.input.isHeld(Control.Down, player) && invulnTime == 0)
                    {
                        if (player.vileMissileWeapon.type == 1 || player.vileMissileWeapon.type == 2)
                        {
                            if (tryUseVileAmmo(player.vileMissileWeapon.vileAmmo))
                            {
                                player.vileMissileWeapon.vileShoot(WeaponIds.StunShot, this);
                            }
                        }
                        else if (player.vileStunShotWeapon.type == -1 || player.vileStunShotWeapon.type == 0)
                        {
                            if (tryUseVileAmmo(player.vileMissileWeapon.vileAmmo))
                            {
                                player.vileStunShotWeapon.vileShoot(WeaponIds.StunShot, this);
                            }
                        }
                    }

                    if (goliathShotPressed)
                    {
                        if (rideArmor.raNum == 4 && !rideArmor.isAttacking() && mechBusterCooldown == 0)
                        {
                            rideArmor.changeState(new RAGoliathShoot(rideArmor.grounded), true);
                            mechBusterCooldown = 1;
                        }
                    }
                }
                player.gridModeHeld = false;
                player.gridModePos = new Point();
                return;
            }

            if (charState is InRideChaser)
            {
                return;
            }

            player.changeWeaponControls();
            if (player.weapons.Count == 1 && player.weapon is MechMenuWeapon mmw2 && mmw2.isMenuOpened)
            {
                if (player.input.isPressed(Control.WeaponLeft, player) || player.input.isPressed(Control.WeaponRight, player))
                {
                    mmw2.isMenuOpened = false;
                }
            }

            bool wL = player.input.isHeld(Control.WeaponLeft, player);
            bool wR = player.input.isHeld(Control.WeaponRight, player);
            if (isVileMK5 && vileStartRideArmor != null && Options.main.mk5PuppeteerHoldOrToggle && player.weapon is MechMenuWeapon && !wL && !wR)
            {
                if (lastFrameWeaponRightHeld)
                {
                    player.weaponSlot--;
                    if (player.weaponSlot < 0)
                    {
                        player.weaponSlot = player.weapons.Count - 1;
                    }
                }
                else
                {
                    player.weaponSlot++;
                    if (player.weaponSlot >= player.weapons.Count)
                    {
                        player.weaponSlot = 0;
                    }
                }
            }
            lastFrameWeaponLeftHeld = wL;
            lastFrameWeaponRightHeld = wR;

            var mmw = player.weapon as MechMenuWeapon;
            if (!isVileMK5 || vileStartRideArmor == null)
            {
                if (player.input.isPressed(Control.Shoot, player) && mmw != null && calldownMechCooldown == 0)
                {
                    onMechSlotSelect(mmw);
                    return;
                }
            }
            else if (mmw != null)
            {
                if (player.input.isPressed(Control.Up, player))
                {
                    onMechSlotSelect(mmw);
                    player.changeWeaponSlot(player.prevWeaponSlot);
                    return;
                }
            }

            if (isVileMK5 && vileStartRideArmor != null)
            {
                if (canLinkMK5())
                {
                    if (vileStartRideArmor.character == null)
                    {
                        vileStartRideArmor.linkMK5(this);
                    }
                }
                else
                {
                    if (vileStartRideArmor.character != null)
                    {
                        vileStartRideArmor.unlinkMK5();
                    }
                }
            }

            if (isVileMK5 && vileStartRideArmor != null && mmw != null && vileStartRideArmor.rideArmorState is RADeactive)
            {
                vileStartRideArmor.changeState(new RAIdle("ridearmor_activating"), true);
                return;
            }

            if (isVileMK5 && vileStartRideArmor != null && mmw != null && grounded && vileStartRideArmor.grounded && player.input.isPressed(Control.Down, player))
            {
                if (vileStartRideArmor.rideArmorState is not RADeactive)
                {
                    vileStartRideArmor.changeState(new RADeactive(), true);
                    player.changeWeaponSlot(player.prevWeaponSlot);
                    Global.level.gameMode.setHUDErrorMessage(player, "Deactivated Ride Armor.", playSound: false, resetCooldown: true);
                    return;
                }
            }

            if (isInvulnerableAttack()) return;
            if (!player.canControl) return;

            // GMTODO consider a better way here instead of a hard-coded deny list
            if (charState is Die || charState is Hurt || charState is VileRevive || charState is VileMK2Grabbed || charState is DeadLiftGrabbed || charState is WhirlpoolGrabbed || charState is UPGrabbed || charState is Taunt || 
                charState is DarkHoldState || charState is HexaInvoluteState || charState is CallDownMech || charState is NapalmAttack) return;

            if (charState is Dash || charState is AirDash)
            {
                if (isVileMK2 && (player.input.isPressed(Control.Special1, player)))
                {
                    charState.isGrabbing = true;
                    charState.superArmor = true;
                    changeSpriteFromName("dash_grab", true);
                }
            }

            if (isShootingLongshotGizmo && player.weapon is VileCannon)
            {
                player.weapon.vileShoot(WeaponIds.FrontRunner, this);
            }
            else if (player.input.isPressed(Control.Special1, player))
            {
                if (charState is Crouch)
                {
                    if (player.vileNapalmWeapon.type == (int)NapalmType.NoneBall)
                    {
                        player.vileBallWeapon.vileShoot(WeaponIds.Napalm, this);
                    }
                    else if (player.vileNapalmWeapon.type == (int)NapalmType.NoneFlamethrower)
                    {
                        player.vileFlamethrowerWeapon.vileShoot(WeaponIds.Napalm, this);
                    }
                    else
                    {
                        player.vileNapalmWeapon.vileShoot(WeaponIds.Napalm, this);
                    }
                }
                else if (charState is Jump || charState is Fall || charState is VileHover)
                {
                    if (!player.input.isHeld(Control.Down, player))
                    {
                        if (player.vileBallWeapon.type == (int)VileBallType.NoneNapalm)
                        {
                            player.vileNapalmWeapon.vileShoot(WeaponIds.VileBomb, this);
                        }
                        else if (player.vileBallWeapon.type == (int)VileBallType.NoneFlamethrower)
                        {
                            player.vileFlamethrowerWeapon.vileShoot(WeaponIds.VileBomb, this);
                        }
                        else
                        {
                            player.vileBallWeapon.vileShoot(WeaponIds.VileBomb, this);
                        }
                    }
                    else
                    {
                        if (player.vileFlamethrowerWeapon.type == (int)VileFlamethrowerType.NoneNapalm)
                        {
                            player.vileNapalmWeapon.vileShoot(WeaponIds.VileFlamethrower, this);
                        }
                        else if (player.vileFlamethrowerWeapon.type == (int)VileFlamethrowerType.NoneBall)
                        {
                            player.vileBallWeapon.vileShoot(WeaponIds.VileFlamethrower, this);
                        }
                        else
                        {
                            player.vileFlamethrowerWeapon.vileShoot(WeaponIds.VileFlamethrower, this);
                        }
                    }
                }
                else if (charState is Idle || charState is Dash || charState is Run || charState is RocketPunchAttack)
                {
                    if ((player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player)) && !player.input.isHeld(Control.Up, player))
                    {
                        if (player.vileRocketPunchWeapon.type > -1)
                        {
                            player.vileRocketPunchWeapon.vileShoot(WeaponIds.RocketPunch, this);
                        }
                    }
                    else if (charState is not RocketPunchAttack)
                    {
                        if (!player.input.isHeld(Control.Up, player) || player.vileCutterWeapon.type == -1)
                        {
                            if (player.vileMissileWeapon.type > -1)
                            {
                                player.vileMissileWeapon.vileShoot(WeaponIds.StunShot, this);
                            }
                        }
                        else
                        {
                            player.vileCutterWeapon.vileShoot(WeaponIds.VileCutter, this);
                        }
                    }
                }
            }
            else if (player.input.isHeld(Control.Shoot, player))
            {
                if (player.vileCutterWeapon.shootTime < player.vileCutterWeapon.rateOfFire * 0.75f)
                {
                    player.weapon.vileShoot(0, this);
                }
            }
        }

        public bool canLandOnRideArmor()
        {
            if (charState is Fall) return true;
            if (charState is VileHover vh && vh.fallY > 0) return true;
            return false;
        }

        public bool canLinkMK5()
        {
            if (vileStartRideArmor == null) return false;
            if (vileStartRideArmor.rideArmorState is RADeactive) return false;
            if (vileStartRideArmor.pos.distanceTo(pos) > Global.screenW * 0.75f) return false;
            return charState is not Die && charState is not VileRevive && charState is not CallDownMech && charState is not HexaInvoluteState;
        }

        public bool isVileMK5Linked()
        {
            return isVileMK5 && vileStartRideArmor?.character == this;
        }

        public void getOffMK5Platform()
        {
            if (mk5RideArmorPlatform != null)
            {
                mk5RideArmorPlatform.character = null;
                mk5RideArmorPlatform = null;
            }
        }

        public bool canVileHover()
        {
            return isVileMK5 && player.vileAmmo > 0 && flag == null;
        }

        public void onMechSlotSelect(MechMenuWeapon mmw)
        {
            if (vileStartRideArmor == null)
            {
                if (!mmw.isMenuOpened)
                {
                    mmw.isMenuOpened = true;
                    return;
                }
            }

            if (player.isAI) calldownMechCooldown = maxCalldownMechCooldown;
            if (vileStartRideArmor == null)
            {
                if (alreadySummonedNewMech)
                {
                    Global.level.gameMode.setHUDErrorMessage(player, "Can only summon a mech once per life");
                }
                else if (canAffordRideArmor())
                {
                    if (!(charState is Idle || charState is Run || charState is Crouch)) return;
                    if (player.selectedRAIndex == 4 && player.scrap < 10)
                    {
                        if (isVileMK2) Global.level.gameMode.setHUDErrorMessage(player, "Goliath armor requires 10 scrap");
                        else Global.level.gameMode.setHUDErrorMessage(player, "Devil Bear armor requires 10 scrap");
                    }
                    else
                    {
                        alreadySummonedNewMech = true;
                        if (vileStartRideArmor != null) vileStartRideArmor.selfDestructTime = 1000;
                        buyRideArmor();
                        mmw.isMenuOpened = false;
                        int raIndex = player.selectedRAIndex;
                        if (isVileMK5 && raIndex == 4) raIndex++;
                        vileStartRideArmor = new RideArmor(player, pos, raIndex, 0, player.getNextActorNetId(), true, sendRpc: true);
                        if (vileStartRideArmor.raNum == 4) summonedGoliath = true;
                        if (isVileMK5)
                        {
                            vileStartRideArmor.ownedByMK5 = true;
                            vileStartRideArmor.zIndex = zIndex - 1;
                            player.weaponSlot = 0;
                            if (player.weapon is MechMenuWeapon) player.weaponSlot = 1;
                        }
                        changeState(new CallDownMech(vileStartRideArmor, true), true);
                    }
                }
                else
                {
                    if (player.selectedRAIndex == 4 && player.scrap < 10)
                    {
                        if (isVileMK2) Global.level.gameMode.setHUDErrorMessage(player, "Goliath armor requires 10 scrap");
                        else Global.level.gameMode.setHUDErrorMessage(player, "Devil Bear armor requires 10 scrap");
                    }
                    else
                    {
                        cantAffordRideArmorMessage();
                    }
                }
            }
            else
            {
                if (!(charState is Idle || charState is Run || charState is Crouch)) return;
                changeState(new CallDownMech(vileStartRideArmor, false), true);
            }
        }

        public bool tryUseVileAmmo(float ammo)
        {
            if (player.weapon is Vulcan)
            {
                usedAmmoLastFrame = true;
            }
            if (player.vileAmmo > ammo - 0.1f)
            {
                usedAmmoLastFrame = true;
                if (weaponHealAmount == 0)
                {
                    player.vileAmmo -= ammo;
                    if (player.vileAmmo < 0) player.vileAmmo = 0;
                }
                return true;
            }
            return false;
        }

        private void buyRideArmor()
        {
            if (Global.level.is1v1()) player.health -= (player.maxHealth / 2);
            else player.scrap -= callNewMechScrapCost * (player.selectedRAIndex == 4 ? 2 : 1);
        }

        private void cantAffordRideArmorMessage()
        {
            if (Global.level.is1v1()) Global.level.gameMode.setHUDErrorMessage(player, "Ride Armor requires 16 HP");
            else Global.level.gameMode.setHUDErrorMessage(player, "Ride Armor requires " + callNewMechScrapCost + " scrap");
        }

        private bool canAffordRideArmor()
        {
            if (Global.level.is1v1()) return player.health > (player.maxHealth / 2);
            return player.scrap >= callNewMechScrapCost;
        }

        public Point getVileShootVel(bool aimable)
        {
            Point vel = new Point(1, 0);
            if (!aimable)
            {
                return vel;
            }

            if (rideArmor != null)
            {
                if (player.input.isHeld(Control.Up, player))
                {
                    vel = new Point(1, -0.5f);
                }
                else
                {
                    vel = new Point(1, 0.5f);
                }
            }
            else if (charState is VileMK2GrabState)
            {
                vel = new Point(1, -0.75f);
            }
            else if (player.input.isHeld(Control.Up, player))
            {
                if (!canVileAim60Degrees() || (player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player)))
                {
                    vel = new Point(1, -0.75f);
                }
                else
                {
                    vel = new Point(1, -3);
                }
            }
            else if (player.input.isHeld(Control.Down, player) && player.character.charState is not Crouch && charState is not MissileAttack)
            {
                vel = new Point(1, 0.5f);
            }
            else if (player.input.isHeld(Control.Down, player) && player.input.isLeftOrRightHeld(player) && player.character.charState is Crouch)
            {
                vel = new Point(1, 0.5f);
            }

            if (charState is RisingSpecterState)
            {
                vel = new Point(1, -0.75f);
            }

            /*
            if (charState is CutterAttackState)
            {
                vel = new Point(1, -3);
            }
            */

            return vel;
        }

        public bool canVileAim60Degrees()
        {
            return charState is MissileAttack || charState is Idle || charState is CannonAttack;
        }

        public Point? getVileMK2StunShotPos()
        {
            if (charState is InRideArmor)
            {
                return pos.addxy(xDir * -8, -12);
            }

            var headPos = getHeadPos();
            if (headPos == null) return null;
            return headPos.Value.addxy(-xDir * 5, 3);
        }

        public void setVileShootTime(Weapon weapon, float modifier = 1f, Weapon targetCooldownWeapon = null)
        {
            targetCooldownWeapon = targetCooldownWeapon ?? weapon;
            if (isVileMK2)
            {
                float innerModifier = 1f;
                if (weapon is VileMissile) innerModifier = 0.33f;
                weapon.shootTime = targetCooldownWeapon.rateOfFire * innerModifier * modifier;
            }
            else
            {
                weapon.shootTime = targetCooldownWeapon.rateOfFire * modifier;
            }
        }

        public Projectile getVileProjFromHitbox(Point centerPoint)
        {
            Projectile proj = null;
            if (sprite.name.Contains("dash_grab"))
            {
                proj = new GenericMeleeProj(new VileMK2Grab(), centerPoint, ProjIds.VileMK2Grab, player, 0, 0, 0);
            }
            return proj;
        }
    }

    public class CallDownMech : CharState
    {
        RideArmor rideArmor;
        bool isNew;
        public CallDownMech(RideArmor rideArmor, bool isNew, string transitionSprite = "") : base("call_down_mech", "", "", transitionSprite)
        {
            this.rideArmor = rideArmor;
            this.isNew = isNew;
            superArmor = true;
        }

        public override void update()
        {
            base.update();
            if (rideArmor == null || rideArmor.destroyed || stateTime > 4)
            {
                character.changeState(new Idle(), true);
                return;
            }

            if (rideArmor.rideArmorState is not RACalldown)
            {
                /*
                if (character.isVileMK5)
                {
                    if (stateTime > 0.75f)
                    {
                        character.changeState(new Idle(), true);
                    }
                    return;
                }
                */

                if (!character.isVileMK5 && MathF.Abs(character.pos.x - rideArmor.pos.x) < 10)
                {
                    rideArmor.putCharInRideArmor(character);
                }
                else
                {
                    character.changeState(new Idle(), true);
                }
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            rideArmor.changeState(new RACalldown(character.pos, isNew), true);
            rideArmor.xDir = character.xDir;
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
        }
    }

    public class VileRevive : CharState
    {
        public float radius = 200;
        Anim drDopplerAnim;
        bool isMK5;
        public VileRevive(bool isMK5) : base(isMK5 ? "revive_to5" : "revive")
        {
            invincible = true;
            this.isMK5 = isMK5;
        }

        public override void update()
        {
            base.update();
            if (radius >= 0)
            {
                radius -= Global.spf * 150;
            }
            if (character.frameIndex < 2)
            {
                if (Global.frameCount % 4 < 2)
                {
                    character.addRenderEffect(RenderEffectType.Flash);
                }
                else
                {
                    character.removeRenderEffect(RenderEffectType.Flash);
                }
            }
            else
            {
                character.removeRenderEffect(RenderEffectType.Flash);
            }
            if (character.frameIndex == 7 && !once)
            {
                character.playSound("ching");
                player.health = 1;
                character.addHealth(player.maxHealth);
                once = true;
            }
            if (character.ownedByLocalPlayer)
            {
                if (character.isAnimOver())
                {
                    setFlags();
                    character.changeState(new Fall(), true);
                }
            }
            else if (character?.sprite?.name != null)
            {
                if (!character.sprite.name.EndsWith("_revive") && !character.sprite.name.EndsWith("_revive_to5") && radius <= 0)
                {
                    setFlags();
                    character.changeState(new Fall(), true);
                }
            }
        }

        public void setFlags()
        {
            if (!isMK5)
            {
                character.vileForm = 1;
            }
            else
            {
                character.vileForm = 2;
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            //character.setzIndex(ZIndex.Foreground);
            character.playSound("revive");
            character.addMusicSource("drdoppler", character.getCenterPos(), false);
            if (!isMK5)
            {
                drDopplerAnim = new Anim(character.pos.addxy(30 * character.xDir, -15), "drdoppler", -character.xDir, null, false);
                drDopplerAnim.fadeIn = true;
                drDopplerAnim.blink = true;
            }
            else
            {
                if (character.vileStartRideArmor != null)
                {
                    character.vileStartRideArmor.ownedByMK5 = true;
                }
            }
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.useGravity = true;
            setFlags();
            character.removeRenderEffect(RenderEffectType.Flash);
            Global.level.delayedActions.Add(new DelayedAction(() => { character.destroyMusicSource(); }, 0.75f));
            
            drDopplerAnim?.destroySelf();
            if (character != null)
            {
                character.invulnTime = 0.5f;
            }
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            if (!character.ownedByLocalPlayer) return;

            if (radius <= 0) return;
            Point pos = character.getCenterPos();
            DrawWrappers.DrawCircle(pos.x + x, pos.y + y, radius, false, Color.White, 5, character.zIndex + 1, true, Color.White);
        }
    }

    public class VileHover : CharState
    {
        public Point flyVel;
        float flyVelAcc = 500;
        float flyVelMaxSpeed = 200;
        public float fallY;
        public VileHover(string transitionSprite = "") : base("hover", "hover_shoot", "", transitionSprite)
        {
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (character.flag != null)
            {
                character.changeToIdleOrFall();
                return;
            }

            if (character.vileHoverTime > character.vileMaxHoverTime)
            {
                character.vileHoverTime = character.vileMaxHoverTime;
                character.changeToIdleOrFall();
                return;
            }

            airCode();

            if (character.charState is not VileHover) return;

            if (Global.level.checkCollisionActor(character, 0, -character.getYMod()) != null && character.vel.y * character.getYMod() < 0)
            {
                character.vel.y = 0;
            }

            Point move = getHoverMove();

            if (!character.sprite.name.EndsWith("cannon_air") || character.sprite.time > 0.1f)
            {
                if (MathF.Abs(move.x) > 75 && !character.isUnderwater())
                {
                    sprite = "hover_forward";
                    character.changeSpriteFromNameIfDifferent("hover_forward", false);
                }
                else
                {
                    sprite = "hover";
                    character.changeSpriteFromNameIfDifferent("hover", false);
                }
            }

            if (move.magnitude > 0)
            {
                character.move(move);
            }

            if (character.isUnderwater())
            {
                character.frameIndex = 0;
                character.frameSpeed = 0;
            }
        }

        public Point getHoverMove()
        {
            bool isSoftLocked = character.isSoftLocked();
            bool isJumpHeld = !isSoftLocked && player.input.isHeld(Control.Jump, player) && character.pos.y > -5;

            var inputDir = isSoftLocked ? Point.zero : player.input.getInputDir(player);
            inputDir.y = isJumpHeld ? -1 : 0;

            if (inputDir.x > 0) character.xDir = 1;
            if (inputDir.x < 0) character.xDir = -1;

            if (inputDir.y == 0 || character.gravityWellModifier > 1)
            {
                if (character.frameIndex >= character.sprite.loopStartFrame)
                {
                    character.frameIndex = character.sprite.loopStartFrame;
                    character.frameSpeed = 0;
                }
                character.addGravity(ref fallY);
            }
            else
            {
                character.frameSpeed = 1;
                fallY = Helpers.lerp(fallY, 0, Global.spf * 10);
                character.vileHoverTime += Global.spf;
            }

            if (inputDir.isZero())
            {
                flyVel = Point.lerp(flyVel, Point.zero, Global.spf * 5f);
            }
            else
            {
                float ang = flyVel.angleWith(inputDir);
                float modifier = MathF.Clamp(ang / 90f, 1, 2);

                flyVel.inc(inputDir.times(Global.spf * flyVelAcc * modifier));
                if (flyVel.magnitude > flyVelMaxSpeed)
                {
                    flyVel = flyVel.normalize().times(flyVelMaxSpeed);
                }
            }

            var hit = character.checkCollision(flyVel.x * Global.spf, flyVel.y * Global.spf);
            if (hit != null && !hit.isGroundHit())
            {
                flyVel = flyVel.subtract(flyVel.project(hit.getNormalSafe()));
            }

            return flyVel.addxy(0, fallY);
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.useGravity = false;
            if (player.speedDevil)
            {
                flyVelMaxSpeed *= 1.1f;
                flyVelAcc *= 1.1f;
            }

            float flyVelX = 0;
            if (character.isDashing && character.deltaPos.x != 0)
            {
                flyVelX = character.xDir * character.getDashSpeed() * character.getRunSpeed() * 0.5f;
            }
            else if (character.deltaPos.x != 0)
            {
                flyVelX = character.xDir * character.getRunSpeed() * 0.5f;
            }

            float flyVelY = 0;
            if (character.vel.y < 0)
            {
                flyVelY = character.vel.y;
            }

            flyVel = new Point(flyVelX, flyVelY);
            if (flyVel.magnitude > flyVelMaxSpeed) flyVel = flyVel.normalize().times(flyVelMaxSpeed);

            if (character.vel.y > 0)
            {
                fallY = character.vel.y;
            }

            character.isDashing = false;
            character.stopMoving();
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.useGravity = true;
            character.sprite.restart();
            character.stopMoving();
        }
    }
}
