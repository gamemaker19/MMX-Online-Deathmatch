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
        public const float sigmaHeight = 50;
        public float sigmaSaberMaxCooldown = 1f;
        public float noBlockTime = 0;
        public bool isHyperSigma;
        public float leapSlashCooldown;
        public const float maxLeapSlashCooldown = 2;
        public float tagTeamSwapProgress;
        public int tagTeamSwapCase;
        public float sigmaAmmoRechargeCooldown = 0.5f;
        public float sigmaAmmoRechargeTime;
        public float sigmaUpSlashCooldown;
        public float sigmaDownSlashCooldown;
        public float sigma3FireballCooldown;
        public float maxSigma3FireballCooldown = 0.39f;
        public float sigma3ShieldCooldown;
        public float maxSigma3ShieldCooldown = 1.125f;
        public float sigmaHeadBeamRechargePeriod = 0.05f;
        public float sigmaHeadBeamTimeBeforeRecharge = 0.33f;

        public float viralSigmaTackleCooldown;
        public float viralSigmaTackleMaxCooldown = 1;
        public string lastHyperSigmaSprite;
        public int lastHyperSigmaFrameIndex;
        public int lastHyperSigmaXDir;
        public float lastViralSigmaAngle;
        public float viralSigmaAngle;
        public ShaderWrapper viralSigmaShader;
        public ShaderWrapper sigmaShieldShader;
        public float viralSigmaBeamLength;
        public int lastViralSigmaXDir = 1;
        public Character possessTarget;
        public float possessEnemyTime;
        public float maxPossessEnemyTime;
        public int numPossesses;
        public float kaiserHoverTime;
        public float kaiserMaxHoverTime = 4;

        public WolfSigmaHead head;
        public WolfSigmaHand leftHand;
        public WolfSigmaHand rightHand;

        public void getViralSigmaPossessTarget()
        {
            var collideDatas = Global.level.getTriggerList(this, 0, 0);
            foreach (var collideData in collideDatas)
            {
                if (collideData?.gameObject is Character chr && chr.canBeDamaged(player.alliance, player.id, (int)ProjIds.Sigma2ViralPossess) && chr.player.canBePossessed())
                {
                    possessTarget = chr;
                    maxPossessEnemyTime = 2 + (Helpers.clampMax(numPossesses, 4) * 0.5f); //2 - Helpers.progress(chr.player.health, chr.player.maxHealth);
                    return;
                }
            }
        }

        public bool canPossess(Character target)
        {
            if (target == null || target.destroyed) return false;
            if (!target.player.canBePossessed()) return false;
            var collideDatas = Global.level.getTriggerList(this, 0, 0);
            foreach (var collideData in collideDatas)
            {
                if (collideData.gameObject is Character chr && chr.canBeDamaged(player.alliance, player.id, (int)ProjIds.Sigma2ViralPossess))
                {
                    if (target == chr)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool isSigmaShooting()
        {
            return sprite.name.Contains("_shoot_") || sprite.name.EndsWith("_shoot");
        }

        public void preUpdateSigma()
        {
            bool isSummoner = player.isSummoner();
            bool isPuppeteer = player.isPuppeteer();
            bool isStriker = player.isStriker();
            bool isTagTeam = player.isTagTeam();

            if (isPuppeteer && Options.main.puppeteerHoldOrToggle && !player.input.isHeld(Control.WeaponLeft, player) && !player.input.isHeld(Control.WeaponRight, player))
            {
                player.changeToSigmaSlot();
            }

            player.changeWeaponControls();

            if (invulnTime > 0) return;

            bool shootPressed = player.input.isPressed(Control.Shoot, player);
            bool spcPressed = player.input.isPressed(Control.Special1, player);
            if (flag != null)
            {
                shootPressed = false;
                spcPressed = false;
            }

            if (isPuppeteer)
            {
                if (player.weapon is MaverickWeapon mw2 && mw2.maverick != null)
                {
                    if (mw2.maverick.aiBehavior != MaverickAIBehavior.Control && mw2.maverick.state is not MExit)
                    {
                        becomeMaverick(mw2.maverick);
                    }
                }
                else if (player.currentMaverick != null)
                {
                    becomeSigma(pos, xDir);
                }
            }

            // "Global" command prototype
            if (player.weapon is SigmaMenuWeapon && player.currentMaverick == null && player.mavericks.Count > 0 && grounded && player.input.isHeld(Control.Down, player) && (isPuppeteer || isSummoner) && charState is not IssueGlobalCommand)
            {
                if (player.input.isCommandButtonPressed(player))
                {
                    Global.level.gameMode.hudErrorMsgTime = 0;
                    if (player.currentMaverickCommand == MaverickAIBehavior.Defend)
                    {
                        player.currentMaverickCommand = MaverickAIBehavior.Follow;
                        Global.level.gameMode.setHUDErrorMessage(player, "Issued follow command.", playSound: false);
                    }
                    else
                    {
                        player.currentMaverickCommand = MaverickAIBehavior.Defend;
                        Global.level.gameMode.setHUDErrorMessage(player, "Issued hold position command.", playSound: false);
                    }

                    foreach (var maverick in player.mavericks)
                    {
                        maverick.aiBehavior = player.currentMaverickCommand;
                    }

                    changeState(new IssueGlobalCommand(), true);
                }
            }
            else if (player.weapon is SigmaMenuWeapon && player.currentMaverick == null && player.mavericks.Count > 0 && grounded && player.input.isHeld(Control.Up, player) && (isPuppeteer || isSummoner) && charState is not IssueGlobalCommand)
            {
                if (player.input.isCommandButtonPressed(player))
                {
                    foreach (var maverick in player.mavericks)
                    {
                        maverick.changeState(new MExit(maverick.pos, true), ignoreCooldown: true);
                    }
                    changeState(new IssueGlobalCommand(), true);
                }
            }

            if (player.weapon is SigmaMenuWeapon && player.currentMaverick == null && player.mavericks.Count > 0 && grounded && (player.input.isHeld(Control.Right, player) || player.input.isHeld(Control.Left, player)) && isSummoner
                && charState is not IssueGlobalCommand && charState is not Dash)
            {
                if (player.input.isCommandButtonPressed(player))
                {
                    Global.level.gameMode.hudErrorMsgTime = 0;

                    player.currentMaverickCommand = MaverickAIBehavior.Attack;
                    Global.level.gameMode.setHUDErrorMessage(player, "Issued attack-move command.", playSound: false);

                    foreach (var maverick in player.mavericks)
                    {
                        maverick.aiBehavior = player.currentMaverickCommand;
                        maverick.attackDir = xDir;
                    }

                    changeState(new IssueGlobalCommand(), true);
                }
            }

            if (player.currentMaverick == null && !isTagTeam)
            {
                if (player.weapon is MaverickWeapon mw && (!isStriker || mw.cooldown == 0) && (shootPressed || spcPressed))
                {
                    if (mw.maverick == null)
                    {
                        if (canAffordMaverick(mw))
                        {
                            if (!(charState is Idle || charState is Run || charState is Crouch)) return;
                            if (isStriker && player.mavericks.Count > 0) return;
                            buyMaverick(mw);
                            var maverick = player.maverickWeapon.summon(player, pos.addxy(0, -112), pos, xDir);
                            if (isStriker)
                            {
                                mw.maverick.health = mw.lastHealth;
                                if (player.input.isPressed(Control.Shoot, player))
                                {
                                    maverick.startMoveControl = Control.Shoot;
                                }
                                else if (player.input.isPressed(Control.Special1, player))
                                {
                                    maverick.startMoveControl = Control.Special1;
                                }
                            }
                            /*
                            else if (isSummoner)
                            {
                                mw.shootTime = MaverickWeapon.summonerCooldown;
                                if (player.input.isPressed(Control.Shoot, player))
                                {
                                    maverick.startMoveControl = Control.Shoot;
                                }
                                else if (player.input.isPressed(Control.Special1, player))
                                {
                                    maverick.startMoveControl = Control.Special1;
                                }
                            }
                            */

                            changeState(new CallDownMaverick(maverick, true, false), true);

                            if (isSummoner)
                            {
                                maverick.aiCooldown = 1f;
                            }

                            if (!isPuppeteer)
                            {
                                player.changeToSigmaSlot();
                            }
                        }
                        else
                        {
                            cantAffordMaverickMessage();
                        }
                    }
                    else if (isSummoner)
                    {
                        if (shootPressed && mw.shootTime == 0)
                        {
                            mw.shootTime = MaverickWeapon.summonerCooldown;
                            changeState(new CallDownMaverick(mw.maverick, false, false), true);
                            player.changeToSigmaSlot();
                        }
                    }
                    return;
                }
            }

            bool isMaverickIdle = player.currentMaverick?.state is MIdle mIdle;
            if (player.currentMaverick is MagnaCentipede ms && ms.reversedGravity) isMaverickIdle = false;

            bool isSigmaIdle = charState is Idle;
            if (isTagTeam && shootPressed)
            {
                if (isMaverickIdle && player.weapon is SigmaMenuWeapon sw && sw.shootTime == 0 && charState is not Die && tagTeamSwapProgress == 0)
                {
                    tagTeamSwapProgress = Global.spf;
                    tagTeamSwapCase = 0;
                }
                else if (player.weapon is MaverickWeapon mw && (mw.maverick == null || mw.maverick != player.currentMaverick) && mw.cooldown == 0 && (isSigmaIdle || isMaverickIdle))
                {
                    if (canAffordMaverick(mw))
                    {
                        tagTeamSwapProgress = Global.spf;
                        tagTeamSwapCase = 1;
                    }
                    else
                    {
                        cantAffordMaverickMessage();
                    }
                }
            }

            if (player.currentMaverick != null)
            {
                if (!isMaverickIdle || !player.currentMaverick.grounded)
                {
                    tagTeamSwapProgress = 0;
                }
            }
            else
            {
                if (!isSigmaIdle || !grounded)
                {
                    tagTeamSwapProgress = 0;
                }
            }

            if (tagTeamSwapProgress > 0)
            {
                tagTeamSwapProgress += Global.spf * 2;
                if (tagTeamSwapProgress > 1)
                {
                    tagTeamSwapProgress = 0;
                    if (tagTeamSwapCase == 0)
                    {
                        var sw = player.weapons.FirstOrDefault(w => w is SigmaMenuWeapon);
                        sw.shootTime = sw.rateOfFire;
                        player.currentMaverick.changeState(new MExit(player.currentMaverick.pos, true));
                        becomeSigma(player.currentMaverick.pos, player.currentMaverick.xDir);
                    }
                    else
                    {
                        if (player.weapon is MaverickWeapon mw && mw.maverick == null)
                        {
                            buyMaverick(mw);

                            Point currentPos = pos;
                            if (player.currentMaverick == null)
                            {
                                changeState(new WarpOut());
                            }
                            else
                            {
                                currentPos = player.currentMaverick.pos;
                                player.currentMaverick.changeState(new MExit(currentPos, true));
                            }

                            mw.summon(player, currentPos.addxy(0, -112), currentPos, xDir);
                            mw.maverick.health = mw.lastHealth;
                            becomeMaverick(mw.maverick);
                        }
                    }
                }
            }
        }

        public void updateSigma()
        {
            sigmaSaberMaxCooldown = (player.isSigma1() ? 1 : 0.5f);

            if (dashAttackCooldown > 0) dashAttackCooldown = Helpers.clampMin0(dashAttackCooldown - Global.spf);
            if (airAttackCooldown > 0) airAttackCooldown = Helpers.clampMin0(airAttackCooldown - Global.spf);
            Helpers.decrementTime(ref wallKickCooldown);
            Helpers.decrementTime(ref saberCooldown);
            Helpers.decrementTime(ref xSaberCooldown);
            Helpers.decrementTime(ref genmuCooldown);
            Helpers.decrementTime(ref noBlockTime);
            Helpers.decrementTime(ref leapSlashCooldown);
            Helpers.decrementTime(ref viralSigmaTackleCooldown);
            Helpers.decrementTime(ref sigmaUpSlashCooldown);
            Helpers.decrementTime(ref sigmaDownSlashCooldown);
            Helpers.decrementTime(ref sigma3FireballCooldown);
            Helpers.decrementTime(ref sigma3ShieldCooldown);
            player.sigmaFireWeapon.update();
            if (viralSigmaBeamLength < 1 && charState is not ViralSigmaBeamState)
            {
                viralSigmaBeamLength += Global.spf * 0.1f;
                if (viralSigmaBeamLength > 1) viralSigmaBeamLength = 1;
            }

            if (player.sigmaAmmo >= player.sigmaMaxAmmo)
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
                    player.sigmaAmmo = Helpers.clampMax(player.sigmaAmmo + 1, player.sigmaMaxAmmo);
                    playSound("heal", forcePlay: true);
                }
            }

            if (player.maverick1v1 != null && player.readyTextOver && !player.maverick1v1Spawned && player.respawnTime <= 0 && player.weapons.Count > 0)
            {
                player.maverick1v1Spawned = true;
                var mw = player.weapons[0] as MaverickWeapon;
                if (mw != null)
                {
                    mw.summon(player, pos.addxy(0, -112), pos, xDir);
                    mw.maverick.health = mw.lastHealth;
                    becomeMaverick(mw.maverick);
                }
            }

            if (player.isSigma1())
            {
                Helpers.decrementTime(ref sigmaAmmoRechargeCooldown);
                if (sigmaAmmoRechargeCooldown == 0)
                {
                    Helpers.decrementTime(ref sigmaAmmoRechargeTime);
                    if (sigmaAmmoRechargeTime == 0)
                    {
                        player.sigmaAmmo = Helpers.clampMax(player.sigmaAmmo + 1, player.sigmaMaxAmmo);
                        sigmaAmmoRechargeTime = sigmaHeadBeamRechargePeriod;
                    }
                }
            }

            if (isHyperSigmaBS.getValue() && player.isSigma3() && charState is not Die)
            {
                lastHyperSigmaSprite = sprite?.name;
                lastHyperSigmaFrameIndex = frameIndex;
                lastHyperSigmaXDir = xDir;
            }

            if (isHyperSigmaBS.getValue() && player.isSigma2() && charState is not Die)
            {
                lastHyperSigmaSprite = sprite?.name;
                lastHyperSigmaFrameIndex = frameIndex;
                lastViralSigmaAngle = angle ?? 0;

                var inputDir = player.input.getInputDir(player);
                if (inputDir.x != 0) lastViralSigmaXDir = MathF.Sign(inputDir.x);

                possessTarget = null;
                if (charState is ViralSigmaIdle)
                {
                    getViralSigmaPossessTarget();
                }

                if (charState is not ViralSigmaRevive)
                {
                    angle = Helpers.moveAngle(angle ?? 0, viralSigmaAngle, Global.spf * 500, snap: true);
                }
                if (player.weapons.Count >= 3)
                {
                    if (isWading())
                    {
                        if (player.weapons[2] is MechaniloidWeapon meW && meW.mechaniloidType != MechaniloidType.Fish) player.weapons[2] = new MechaniloidWeapon(player, MechaniloidType.Fish);
                    }
                    else
                    {
                        if (player.weapons[2] is MechaniloidWeapon meW && meW.mechaniloidType != MechaniloidType.Bird) player.weapons[2] = new MechaniloidWeapon(player, MechaniloidType.Bird);
                    }
                }
            }

            if (invulnTime > 0) return;

            if ((charState is Die || charState is WarpOut) && player.currentMaverick != null && !visible)
            {
                changePos(player.currentMaverick.pos);
            }
            if (charState is WarpOut) return;
            if (player.currentMaverick != null)
            {
                return;
            }

            if (player.weapon is MaverickWeapon && (player.input.isHeld(Control.Shoot, player) || player.input.isHeld(Control.Special1, player)))
            {
                return;
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

            bool lenientAttackPressed = (attackPressed || framesSinceLastAttack < 5);

            if (player.isDisguisedAxl && player.axlWeapon is UndisguiseWeapon)
            {
                lenientAttackPressed = false;
            }

            if (player.isSigma3())
            {
                if (!string.IsNullOrEmpty(charState?.shootSprite) && sprite?.name?.EndsWith(charState.shootSprite) == true)
                {
                    if (isAnimOver() && charState is not Sigma3Shoot)
                    {
                        changeSpriteFromName(charState.sprite, true);
                    }
                    else
                    {
                        var shootPOI = getFirstPOI();
                        if (shootPOI != null && player.sigmaFireWeapon.shootTime == 0)
                        {
                            player.sigmaFireWeapon.shootTime = 0.15f;
                            int upDownDir = MathF.Sign(player.input.getInputDir(player).y);
                            float ang = getShootXDir() == 1 ? 0 : 180;
                            if (charState.shootSprite.EndsWith("jump_shoot_downdiag")) ang = getShootXDir() == 1 ? 45 : 135;
                            if (charState.shootSprite.EndsWith("jump_shoot_down")) ang = 90;
                            if (ang != 0 && ang != 180) upDownDir = 0;
                            playSound("sigma3shoot", sendRpc: true);
                            new Sigma3FireProj(player.sigmaFireWeapon, shootPOI.Value, ang, upDownDir, player, player.getNextActorNetId(), sendRpc: true);
                        }
                    }
                }
            }

            if (charState?.canAttack() == true && lenientAttackPressed && player.weapon is not MaverickWeapon)
            {
                if (!isAttacking())
                {
                    if (player.isSigma2())
                    {
                        if (player.input.isHeld(Control.Up, player) && flag == null && grounded)
                        {
                            if (sigmaUpSlashCooldown == 0)
                            {
                                sigmaUpSlashCooldown = 0.75f;
                                changeState(new SigmaUpDownSlashState(true), true);
                            }
                            return;
                        }
                        else if (player.input.isHeld(Control.Down, player) && !grounded && getDistFromGround() > 25)
                        {
                            if (sigmaDownSlashCooldown == 0)
                            {
                                sigmaUpSlashCooldown += 0.5f;
                                sigmaDownSlashCooldown = 1f;
                                changeState(new SigmaUpDownSlashState(false), true);
                            }
                            return;
                        }
                    }

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

                    if (player.isSigma3())
                    {
                        if (!string.IsNullOrEmpty(charState.shootSprite) && player.sigmaFireWeapon.shootTime == 0 && !isSigmaShooting() && sigma3FireballCooldown == 0)
                        {
                            if (charState is Fall || charState is Jump || charState is WallKick)
                            {
                                changeState(new Sigma3ShootAir(player.input.getInputDir(player)), true);
                            }
                            else if (charState is Idle || charState is Run || charState is Dash || charState is SwordBlock)
                            {
                                changeState(new Sigma3Shoot(), true);
                            }
                            sigma3FireballCooldown = maxSigma3FireballCooldown;
                            changeSpriteFromName(charState.shootSprite, true);
                            return;
                        }
                    }

                    var attackSprite = charState.attackSprite;

                    saberCooldown = sigmaSaberMaxCooldown;
                    if (charState is Run || charState is Dash || charState is Idle || charState is Jump || charState is Fall || charState is AirDash)
                    {
                        if (player.loadout.sigmaLoadout.sigmaForm == 0)
                        {
                            changeState(new SigmaSlashState(charState), true);
                        }
                        else if (player.loadout.sigmaLoadout.sigmaForm == 1)
                        {
                            changeState(new SigmaClawState(charState, !grounded), true);
                        }
                        return;
                    }

                    changeSprite(getSprite(attackSprite), true);
                    if (player.isSigma1()) playSound("saberShot", sendRpc: true);
                    if (player.isSigma2()) playSound("sigma2slash", sendRpc: true);
                }
            }
            else if (!isAttacking() && !isInvulnerableAttack() && (charState is Idle || charState is Run))
            {
                if (player.isSigma1() && player.input.isHeld(Control.Special1, player) && player.sigmaAmmo > 0)
                {
                    sigmaAmmoRechargeCooldown = 0.5f;
                    changeState(new SigmaBallShoot(), true);
                    return;
                }
                else if (player.isSigma2() && player.input.isPressed(Control.Special1, player) && player.sigmaAmmo >= 16 && flag == null)
                {
                    if (player.sigmaAmmo < 32)
                    {
                        player.sigmaAmmo -= 16;
                        changeState(new SigmaElectricBallState(), true);
                        return;
                    }
                    else
                    {
                        player.sigmaAmmo = 0;
                        changeState(new SigmaElectricBall2State(), true);
                        return;
                    }
                }
                else if (player.isSigma3() && player.input.isPressed(Control.Special1, player) && charState is not SigmaThrowShieldState && sigma3ShieldCooldown == 0)
                {
                    sigma3ShieldCooldown = maxSigma3ShieldCooldown;
                    changeState(new SigmaThrowShieldState(), true);
                }
            }

            /*
            if (charState.canAttack() && player.input.isHeld(Control.Shoot, player) && player.weapon is not MaverickWeapon && !isAttacking() && player.isSigma2() && saberCooldown == 0)
            {
                saberCooldown = 0.2f;
                changeState(new SigmaClawState(charState, !grounded), true);
                playSound("sigma2slash", sendRpc: true);
                return;
            }
            */

            if (isAttacking())
            {
                if (player.isSigma1())
                {
                    if (isAnimOver() && charState != null && charState is not SigmaSlashState)
                    {
                        changeSprite(getSprite(charState.defaultSprite), true);
                        if (charState is WallSlide && sprite != null)
                        {
                            frameIndex = sprite.frames.Count - 1;
                        }
                    }
                    else if (grounded && sprite?.name != "sigma_attack")
                    {
                        changeSprite("sigma_attack", false);
                    }
                }
                else if (player.isSigma2())
                {
                    if (isAnimOver() && charState != null && charState is not SigmaClawState)
                    {
                        changeSprite(getSprite(charState.defaultSprite), true);
                        if (charState is WallSlide && sprite != null)
                        {
                            frameIndex = sprite.frames.Count - 1;
                        }
                    }
                    else if (grounded && sprite?.name != "sigma2_attack" && sprite?.name != "sigma2_attack2")
                    {
                        changeSprite("sigma2_attack", false);
                    }
                }
            }
        }

        // This can run on both owners and non-owners. So data used must be in sync
        public Projectile getSigmaProjFromHitbox(Collider collider, Point centerPoint)
        {
            Projectile proj = null;
            if (sprite.name.Contains("sigma3_kaiser_fall") && collider.isAttack())
            {
                return new GenericMeleeProj(new KaiserStompWeapon(player), centerPoint, ProjIds.Sigma3KaiserStomp, player, damage: 12 * getKaiserStompDamage(), flinch: Global.defFlinch, hitCooldown: 1f);
            }
            else if (sprite.name.StartsWith("sigma3_kaiser_") && collider.name == "body")
            {
                return new GenericMeleeProj(new Weapon(), centerPoint, ProjIds.Sigma3KaiserSuit, player, damage: 0, flinch: 0, hitCooldown: 1, isShield: true);
            }
            else if (sprite.name.StartsWith("sigma3_") && collider.name == "shield")
            {
                return new GenericMeleeProj(new Weapon(), centerPoint, ProjIds.Sigma3ShieldBlock, player, damage: 0, flinch: 0, hitCooldown: 1, isDeflectShield: true, isShield: true);
            }
            else if (sprite.name == "sigma_ladder_attack") proj = new GenericMeleeProj(player.sigmaSlashWeapon, centerPoint, ProjIds.SigmaSlash, player, 3, 0, 0.25f);
            else if (sprite.name == "sigma_wall_slide_attack") proj = new GenericMeleeProj(player.sigmaSlashWeapon, centerPoint, ProjIds.SigmaSlash, player, 3, 0, 0.25f);
            else if (sprite.name.Contains("sigma_block") && !collider.isHurtBox())
            {
                proj = new GenericMeleeProj(player.sigmaSlashWeapon, centerPoint, ProjIds.SigmaSwordBlock, player, 0, 0, 0, isDeflectShield: true);
            }
            else if (sprite.name == "sigma2_attack") proj = new GenericMeleeProj(player.sigmaClawWeapon, centerPoint, ProjIds.Sigma2Claw, player, 2, 0, 0.2f);
            else if (sprite.name == "sigma2_attack2") proj = new GenericMeleeProj(player.sigmaClawWeapon, centerPoint, ProjIds.Sigma2Claw2, player, 2, Global.halfFlinch, 0.5f);
            else if (sprite.name == "sigma2_attack_air") proj = new GenericMeleeProj(player.sigmaClawWeapon, centerPoint, ProjIds.Sigma2Claw, player, 3, 0, 0.375f);
            else if (sprite.name == "sigma2_attack_dash") proj = new GenericMeleeProj(player.sigmaClawWeapon, centerPoint, ProjIds.Sigma2Claw, player, 3, 0, 0.375f);
            else if (sprite.name == "sigma2_upslash" || sprite.name == "sigma2_downslash") proj = new GenericMeleeProj(player.sigmaClawWeapon, centerPoint, ProjIds.Sigma2UpDownClaw, player, 3, Global.defFlinch, 0.5f);
            else if (sprite.name == "sigma2_ladder_attack") proj = new GenericMeleeProj(player.sigmaSlashWeapon, centerPoint, ProjIds.Sigma2Claw, player, 3, 0, 0.25f);
            else if (sprite.name == "sigma2_wall_slide_attack") proj = new GenericMeleeProj(player.sigmaSlashWeapon, centerPoint, ProjIds.Sigma2Claw, player, 3, 0, 0.25f);
            else if (sprite.name == "sigma2_shoot2") proj = new GenericMeleeProj(new SigmaElectricBall2Weapon(), centerPoint, ProjIds.Sigma2Ball2, player, 6, Global.defFlinch, 1f);
            return proj;
        }

        public void becomeSigma(Point pos, int xDir)
        {
            var prevCamPos = getCamCenterPos();
            if (player.isPuppeteer())
            {
                resetMaverickBehavior();
                //stopCamUpdate = true;
                //Global.level.snapCamPos(getCamCenterPos());
                return;
            }

            resetMaverickBehavior();
            stopCamUpdate = true;
            Point raycastPos = pos.addxy(0, -5);
            Point? warpInPos = Global.level.getGroundPosNoKillzone(raycastPos, Global.screenH);

            if (warpInPos == null)
            {
                var nearestSpawnPoint = Global.level.getClosestSpawnPoint(pos);
                warpInPos = Global.level.getGroundPos(nearestSpawnPoint.pos);
            }

            changePos(warpInPos.Value);
            this.xDir = xDir;
            changeState(new WarpIn(false), true);
            Global.level.snapCamPos(getCamCenterPos(), prevCamPos);
        }

        public void becomeMaverick(Maverick maverick)
        {
            resetMaverickBehavior();
            maverick.aiBehavior = MaverickAIBehavior.Control;
            //stopCamUpdate = true;
            //Global.level.snapCamPos(getCamCenterPos());
            if (maverick.state is not MEnter && maverick.state is not MorphMHatchState && maverick.state is not MFly)
            {
                //To bring back puppeteer cancel, uncomment this
                if (Options.main.puppeteerCancel)
                {
                    maverick.changeToIdleFallOrFly();
                }
            }
        }

        public void resetMaverickBehavior()
        {
            foreach (var weapon in player.weapons)
            {
                if (weapon is MaverickWeapon mw)
                {
                    if (mw.maverick != null && mw.maverick.aiBehavior == MaverickAIBehavior.Control)
                    {
                        mw.maverick.aiBehavior = player.currentMaverickCommand;
                    }
                    if (mw.isMenuOpened)
                    {
                        mw.isMenuOpened = false;
                    }
                }
            }
        }

        private void buyMaverick(MaverickWeapon mw)
        {
            //if (Global.level.is1v1()) player.health -= (player.maxHealth / 2);
            if (player.isStriker()) return;
            if (player.isRefundableMode() && mw.summonedOnce) return;
            else player.scrap -= getMaverickCost();
        }

        private void cantAffordMaverickMessage()
        {
            //if (Global.level.is1v1()) Global.level.gameMode.setHUDErrorMessage(player, "Maverick requires 16 HP");
            Global.level.gameMode.setHUDErrorMessage(player, "Maverick requires " + getMaverickCost() + " scrap");
        }

        private bool canAffordMaverick(MaverickWeapon mw)
        {
            //if (Global.level.is1v1()) return player.health > (player.maxHealth / 2);
            if (player.isStriker()) return true;
            if (player.isRefundableMode() && mw.summonedOnce) return true;

            return player.scrap >= getMaverickCost();
        }

        public int getMaverickCost()
        {
            if (player.isSummoner()) return 3;
            if (player.isPuppeteer()) return 3;
            if (player.isStriker()) return 0;
            if (player.isTagTeam()) return 5;
            return 3;
        }
    }

    public class IssueGlobalCommand : CharState
    {
        public IssueGlobalCommand(string transitionSprite = "") :
            base("summon_maverick", "", "", transitionSprite)
        {
            superArmor = true;
        }

        public override void update()
        {
            base.update();

            if (character.isAnimOver())
            {
                character.changeState(new Idle(), true);
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
        }
    }

    public class CallDownMaverick : CharState
    {
        Maverick maverick;
        bool isNew;
        bool isRecall;
        int frame;
        public CallDownMaverick(Maverick maverick, bool isNew, bool isRecall, string transitionSprite = "") : 
            base("summon_maverick", "", "", transitionSprite)
        {
            this.maverick = maverick;
            this.isNew = isNew;
            this.isRecall = isRecall;
            superArmor = true;
        }

        public override void update()
        {
            base.update();

            frame++;

            if (frame > 0 && frame < 10 && (player.isStriker() || player.isSummoner()))
            {
                if (player.input.isPressed(Control.Shoot, player) && maverick.startMoveControl == Control.Special1)
                {
                    maverick.startMoveControl = Control.Dash;
                }
                else if (player.input.isPressed(Control.Special1, player) && maverick.startMoveControl == Control.Shoot)
                {
                    maverick.startMoveControl = Control.Dash;
                }
            }

            if (character.isAnimOver())
            {
                character.changeState(new Idle(), true);
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            if (!isNew)
            {
                if (maverick.state is not MExit) maverick.changeState(new MExit(character.pos, isRecall));
                else maverick.changeState(new MEnter(character.pos));
            }
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
        }
    }

    public class SigmaSlashWeapon : Weapon
    {
        public SigmaSlashWeapon() : base()
        {
            index = (int)WeaponIds.SigmaSlash;
            killFeedIndex = 9;
        }
    }

    public class SigmaSlashState : CharState
    {
        CharState prevCharState;
        int attackFrame = 2;
        bool fired;
        public SigmaSlashState(CharState prevCharState) : base(prevCharState.attackSprite, "", "", "")
        {
            this.prevCharState = prevCharState;
            if (prevCharState is Dash || prevCharState is AirDash)
            {
                attackFrame = 1;
            }
        }

        public override void update()
        {
            base.update();

            if (!character.grounded)
            {
                airCode();
                landSprite = "attack";
            }

            if (prevCharState is Dash)
            {
                if (character.frameIndex < attackFrame)
                {
                    character.move(new Point(character.getDashSpeed() * character.getRunSpeed() * character.xDir, 0));
                }
            }

            if (character.frameIndex >= attackFrame && !fired)
            {
                fired = true;
                character.playSound("saberShot", sendRpc: true);

                Point off = new Point(30, -20);
                if (character.sprite.name == "sigma_attack_air")
                {
                    off = new Point(20, -30);
                }

                float damage = character.grounded ? 4 : 3;
                int flinch = character.grounded ? Global.defFlinch : 13;
                new SigmaSlashProj(player.sigmaSlashWeapon, character.pos.addxy(off.x * character.xDir, off.y), character.xDir, player, player.getNextActorNetId(), damage: damage, flinch: flinch, rpc: true);
            }

            if (character.isAnimOver())
            {
                if (character.grounded) character.changeState(new Idle(), true);
                else character.changeState(new Fall(), true);
            }
        }
    }

    public class SigmaSlashProj : Projectile
    {
        public SigmaSlashProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, float damage = 6, int flinch = Global.defFlinch, bool rpc = false) :
            base(weapon, pos, xDir, 0, damage, player, "sigma_proj_slash", flinch, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            reflectable = false;
            destroyOnHit = false;
            shouldShieldBlock = false;
            setIndestructableProperties();
            maxTime = 0.1f;
            projId = (int)ProjIds.SigmaSlash;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void postUpdate()
        {
            base.postUpdate();
            if (owner?.character != null)
            {
                incPos(owner.character.deltaPos);
            }
        }
    }

    public class SigmaBallWeapon : Weapon
    {
        public SigmaBallWeapon() : base()
        {
            index = (int)WeaponIds.SigmaBall;
            killFeedIndex = 103;
        }
    }

    public class SigmaBallProj : Projectile
    {
        public SigmaBallProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, Point? vel = null, bool rpc = false) :
            base(weapon, pos, xDir, 400, 2, player, "sigma_proj_ball", 0, 0.2f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.SigmaBall;
            maxTime = 0.5f;
            destroyOnHit = true;
            if (vel != null)
            {
                this.vel = vel.Value.times(speed);
            }

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }

    public class SigmaBallShoot : CharState
    {
        bool shot;
        public SigmaBallShoot(string transitionSprite = "") : base("shoot", "", "", transitionSprite)
        {
        }

        public override void update()
        {
            base.update();

            if (character.sprite.loopCount > 0 && !player.input.isHeld(Control.Special1, player))
            {
                character.changeState(new Idle(), true);
                return;
            }

            Point vel = new Point(0, 0.2f);
            bool lHeld = player.input.isHeld(Control.Left, player) && !player.isAI;
            bool rHeld = player.input.isHeld(Control.Right, player) && !player.isAI;
            bool uHeld = player.input.isHeld(Control.Up, player) || player.isAI;
            bool dHeld = player.input.isHeld(Control.Down, player) && !player.isAI;

            if (lHeld)
            {
                character.xDir = -1;
                vel.x = -2;
            }
            else if (rHeld)
            {
                character.xDir = 1;
                vel.x = 2;
            }

            if (uHeld)
            {
                vel.y = -1;
                if (vel.x == 0) vel.x = character.xDir * 0.5f;
            }
            else if (dHeld)
            {
                vel.y = 1;
                if (vel.x == 0) vel.x = character.xDir * 0.5f;
            }
            else vel.x = character.xDir;

            if (!uHeld && !dHeld && (lHeld || rHeld))
            {
                vel.y = 0;
                vel.x = character.xDir;
            }

            if (character.sprite.frameIndex == 0)
            {
                shot = false;
            }
            if (character.sprite.frameIndex == 1 && !shot)
            {
                shot = true;
                Point poi = character.getFirstPOI() ?? character.getCenterPos();

                player.sigmaAmmo -= 7;
                if (player.sigmaAmmo < 0) player.sigmaAmmo = 0;
                character.sigmaAmmoRechargeCooldown = character.sigmaHeadBeamTimeBeforeRecharge;
                character.playSound("energyBall", sendRpc: true);
                new SigmaBallProj(player.sigmaBallWeapon, poi, character.xDir, player, player.getNextActorNetId(), vel.normalize(), rpc: true);
                new Anim(poi, "sigma_proj_ball_muzzle", character.xDir, player.getNextActorNetId(), true, sendRpc: true);
            }

            if (character.sprite.loopCount > 5 || player.sigmaAmmo <= 0)
            {
                character.changeState(new Idle(), true);
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.vel = new Point();
        }
    }

    public class SigmaWallDashState : CharState
    {
        bool fired;
        int yDir;
        Point vel;
        bool fromGround;
        public SigmaWallDashState(int yDir, bool fromGround) : base("wall_dash", "", "", "")
        {
            this.yDir = yDir;
            this.fromGround = fromGround;
            superArmor = true;
        }

        public override bool canEnter(Character character)
        {
            if (!base.canEnter(character)) return false;
            return character?.player?.isSigma1() == true;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            float xSpeed = 350;
            if (!fromGround)
            {
                character.xDir *= -1;
            }
            else
            {
                character.unstickFromGround();
                character.incPos(new Point(0, -5));
            }
            character.isDashing = true;
            character.dashedInAir++;
            character.stopMoving();
            vel = new Point(character.xDir * xSpeed, yDir * 100);
            character.useGravity = false;
        }

        public override void onExit(CharState newState)
        {
            character.useGravity = true;
            character.leapSlashCooldown = Character.maxLeapSlashCooldown;
            base.onExit(newState);
        }

        public override void update()
        {
            base.update();

            var collideData = Global.level.checkCollisionActor(character, vel.x * Global.spf, vel.y * Global.spf);
            if (collideData?.gameObject is Wall wall)
            {
                var collideData2 = Global.level.checkCollisionActor(character, vel.x * Global.spf, 0);
                if (collideData2?.gameObject is Wall wall2 && wall2.collider.isClimbable)
                {
                    character.changeState(new WallSlide(character.xDir, wall2.collider), true);
                }
                else
                {
                    if (vel.y > 0) character.changeState(new Idle(), true);
                    else
                    {
                        //vel.y *= -1;
                        character.isDashing = true;
                        character.changeState(new Fall(), true);
                    }
                }
            }

            character.move(vel);

            if (stateTime > 0.7f)
            {
                character.changeState(new Fall(), true);
            }

            if (player.input.isPressed(Control.Shoot, player) && !fired && character.saberCooldown == 0 && character.invulnTime == 0)
            {
                if (yDir == 0)
                {
                    character.changeState(new SigmaSlashState(new Dash(Control.Dash)), true);
                    return;
                }

                fired = true;
                character.saberCooldown = character.sigmaSaberMaxCooldown;

                character.playSound("saberShot", sendRpc: true);
                character.changeSpriteFromName("wall_dash_attack", true);

                Point off = new Point(30, -20);
                new SigmaSlashProj(player.sigmaSlashWeapon, character.pos.addxy(off.x * character.xDir, off.y), character.xDir, player, player.getNextActorNetId(), damage: 4, rpc: true);
            }
        }
    }
}
