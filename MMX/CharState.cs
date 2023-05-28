using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MMXOnline
{
    public class CharState
    {
        public string sprite;
        public string defaultSprite;
        public string attackSprite;
        public string shootSprite;
        public string transitionSprite;
        public string landSprite;
        public Point busterOffset;
        public Character character;
        public Collider lastLeftWallCollider;
        public Collider lastRightWallCollider;
        public Wall lastLeftWall;
        public Wall lastRightWall;
        public float stateTime;
        public string enterSound;
        public float framesJumpNotHeld = 0;
        public bool once;
        public bool useGravity = true;

        public bool invincible;
        public bool superArmor;
        public bool immuneToWind;
        public int accuracy;
        public bool isGrabbedState;

        public bool wasVileHovering;

        // For grab states (I am grabber)
        public bool isGrabbing;

        // For grabbed states (I am the grabbed)
        public float grabTime = 4;

        public virtual void releaseGrab()
        {
            grabTime = 0;
        }

        public CharState(string sprite, string shootSprite = null, string attackSprite = null, string transitionSprite = null)
        {
            this.sprite = string.IsNullOrEmpty(transitionSprite) ? sprite : transitionSprite;
            this.transitionSprite = transitionSprite;
            defaultSprite = sprite;
            this.shootSprite = shootSprite;
            this.attackSprite = attackSprite;
            stateTime = 0;
        }

        public bool canShoot()
        {
            return !string.IsNullOrEmpty(shootSprite) && character.canShoot();
        }

        public bool canAttack()
        {
            return !string.IsNullOrEmpty(attackSprite) && !character.isInvulnerableAttack() && character.saberCooldown == 0;
        }

        public Player player
        {
            get
            {
                return character.player;
            }
        }

        public virtual void onExit(CharState newState)
        {
            if (!useGravity)
            {
                character.useGravity = true;
            }
            //Stop the dash speed on transition to any frame except jump/fall (dash lingers in air) or dash itself
            if (!(newState is Jump) && !(newState is Fall) && !(newState is WallKick) && !(newState is Dash) && !(newState is X2ChargeShot) && !(newState is VulcanCharState) && !(newState is StrikeChainPullToWall)
                && !(newState is SigmaSlashState) && !(newState is SigmaClawState) && !(newState is SigmaWallDashState) && !(newState is X6SaberState) && !(newState is Sigma3ShootAir) && !(newState is XUPPunchState) && !(newState is VileHover))
            {
                if (character.isDashing && newState is AirDash) character.dashedInAir++;
                if (character.isDashing && newState is UpDash) character.dashedInAir++;
                character.isDashing = false;
            }
            if (newState is Hurt || newState is Die || newState is Frozen || newState is Crystalized || newState is Stunned)
            {
                character.strikeChainProj?.destroySelf();
                if (newState is not Hurt hurtState)
                {
                    character.beeSwarm?.destroy();
                }
                else
                {
                    character.beeSwarm?.reset(hurtState.isMiniFlinch());
                }
            }
            if (!(newState is Idle || newState is Run || newState is Crouch || newState is Taunt || newState is GigaCrushCharState || newState is XSaberState || newState is X6SaberState ||
                newState is X2ChargeShot || newState is X3ChargeShot || newState is GravityWellChargedState || newState is RaySplasherChargedState || newState is TriadThunderChargedState))
            {
                character.iceSled = null;
            }
            if (newState is Hurt || newState is Die || newState is CallDownMech || newState?.isGrabbedState == true)
            {
                character.mk5RideArmorPlatform = null;
            }
            if (string.IsNullOrEmpty(newState?.shootSprite))
            {
                character.setShootRaySplasher(false);
            }
            if (character.isZooming())
            {
                character.zoomOut();
            }
            if (invincible)
            {
                player.delaySubtank();
            }
        }

        public virtual void onEnter(CharState oldState)
        {
            if (!string.IsNullOrEmpty(enterSound))
            {
                character.playSound(enterSound, sendRpc: true);
            }
            if (oldState is VileHover)
            {
                wasVileHovering = true;
            }
            if (!useGravity)
            {
                character.useGravity = false;
                character.stopMoving();
            }
        }

        public virtual bool canEnter(Character character) 
        {
            if (character.charState is InRideArmor && 
                !(this is Die || this is Idle || this is Jump || this is Fall || this is StrikeChainHooked || this is ParasiteCarry || this is VileMK2Grabbed || this is DarkHoldState ||
                  this is NecroBurstAttack || this is UPGrabbed || this is WhirlpoolGrabbed || this is DeadLiftGrabbed || Helpers.isOfClass(this, typeof(GenericGrabbedState))))
            {
                return false;
            }
            if (character.charState is DarkHoldState dhs && dhs.stunTime > 0)
            {
                if (this is not Die && this is not Hurt)
                {
                    return false;
                }
            }
            if (character.player.isViralSigma())
            {
                return this is ViralSigmaBeamState || this is ViralSigmaIdle || this is ViralSigmaTaunt || this is ViralSigmaShoot || this is ViralSigmaTackle || this is ViralSigmaPossessStart || this is ViralSigmaPossess || this is Die;
            }
            if (character.player.isKaiserSigma())
            {
                return this is KaiserSigmaIdleState || this is KaiserSigmaHoverState || this is KaiserSigmaFallState || this is KaiserSigmaTauntState || this is KaiserSigmaVirusState || this is KaiserSigmaBeamState || this is Die;
            }
            if (character.charState is WarpOut && this is not WarpIn)
            {
                return false;
            }
            return true;
        }

        public virtual bool canExit(Character character, CharState newState)
        {
            if (character.charState is Die && newState is not VileRevive && newState is not WolfSigmaRevive && newState is not ViralSigmaRevive && newState is not KaiserSigmaRevive && newState is not XReviveStart) return false;
            return true;
        }

        public bool inTransition()
        {
            return !string.IsNullOrEmpty(transitionSprite) && sprite == transitionSprite && character?.sprite?.name != null && character.sprite.name.Contains(transitionSprite);
        }

        public virtual void render(float x, float y)
        {
        }

        public virtual void update()
        {
            if (inTransition())
            {
                character.frameSpeed = 1;
                if (character.isAnimOver() && !Global.level.gameMode.isOver)
                {
                    sprite = defaultSprite;
                    character.changeSpriteFromName(sprite, true);
                }
            }

            stateTime += Global.spf;

            var lastLeftWallData = character.getHitWall(-Global.spf * 60, 0);
            lastLeftWallCollider = lastLeftWallData != null ? lastLeftWallData.otherCollider : null;
            if (lastLeftWallCollider != null && !lastLeftWallCollider.isClimbable) lastLeftWallCollider = null;
            lastLeftWall = lastLeftWallData?.gameObject as Wall;

            var lastRightWallData = character.getHitWall(Global.spf * 60, 0);
            lastRightWallCollider = lastRightWallData != null ? lastRightWallData.otherCollider : null;
            if (lastRightWallCollider != null && !lastRightWallCollider.isClimbable) lastRightWallCollider = null;
            lastRightWall = lastRightWallData?.gameObject as Wall;

            // Moving platforms detection
            CollideData leftWallPlat = character.getHitWall(-Global.spf * 300, 0);
            if (leftWallPlat?.gameObject is Wall leftWall && leftWall.isMoving)
            {
                character.move(leftWall.deltaMove, useDeltaTime: true);
                lastLeftWallCollider = leftWall.collider;
            }
            else if (leftWallPlat?.gameObject is Actor leftActor && leftActor.isPlatform && leftActor.pos.x < character.pos.x)
            {
                lastLeftWallCollider = leftActor.collider;
            }

            CollideData rightWallPlat = character.getHitWall(Global.spf * 300, 0);
            if (rightWallPlat?.gameObject is Wall rightWall && rightWall.isMoving)
            {
                character.move(rightWall.deltaMove, useDeltaTime: true);
                lastRightWallCollider = rightWall.collider;
            }
            else if (rightWallPlat?.gameObject is Actor rightActor && rightActor.isPlatform && rightActor.pos.x > character.pos.x)
            {
                lastRightWallCollider = rightActor.collider;
            }

            if (character.grounded && !string.IsNullOrEmpty(landSprite) && sprite != landSprite)
            {
                sprite = landSprite;
                int oldFrameIndex = character.sprite.frameIndex;
                float oldFrameTime = character.sprite.frameTime;
                character.changeSpriteFromName(sprite, false);
                character.sprite.frameIndex = oldFrameIndex;
                character.sprite.frameTime = oldFrameTime;
            }
        }

        public void landingCode()
        {
            character.playSound("land", sendRpc: true);
            string ts = "land";
            if (character.sprite != null && character.sprite.name.EndsWith(shootSprite))
            {
                ts = "";
            }
            if (character.sprite.name.Contains("hyouretsuzan"))
            {
                ts = "hyouretsuzan_land";
                if (!character.sprite.name.Contains("_start") || character.frameIndex > 0)
                {
                    character.breakFreeze(player, character.pos.addxy(character.xDir * 5, 0), sendRpc: true);
                }
            }
            if (character.sprite.name.Contains("rakukojin"))
            {
                ts = "rakukojin_land";
                if (!character.sprite.name.Contains("_start") || character.frameIndex > 0)
                {
                    character.playSound("swordthud", sendRpc: true);
                }
            }
            if (character.sprite.name.Contains("quakeblazer") && character.charState is Hyouretsuzan h)
            {
                ts = "quakeblazer_land";
                h.quakeBlazerExplode(true);
            }
            if (character.sprite.name.Contains("dropkick") && character.charState is DropKickState d)
            {
                ts = "dropkick_land";
            }

            //character.player.vileBallWeapon.shootTime = 0;
            character.quakeBlazerBounces = 0;
            character.dashedInAir = 0;
            changeToIdle(ts);
            if (character.ai != null) character.ai.jumpTime = 0;
        }

        float lastJumpPressedTime;
        public void airCode()
        {
            if (character.grounded)
            {
                landingCode();
                return;
            }

            if (character.sprite.name.EndsWith("cannon_air") && character.isAnimOver())
            {
                character.changeSpriteFromName("fall", true);
            }

            if (character.canVileHover() && player.input.isPressed(Control.Jump, player) && character.charState is not VileHover)
            {
                character.changeState(new VileHover(), true);
            }

            if (character.vel.y < 0 && Global.level.checkCollisionActor(character, 0, -1) != null)
            {
                if (character.gravityWellModifier < 0 && character.vel.y < -300)
                {
                    Damager.applyDamage(character.lastGravityWellDamager, 4, 0.5f, Global.halfFlinch, character, false, (int)WeaponIds.GravityWell, 45, character, (int)ProjIds.GravityWellCharged);
                }
                character.vel.y = 0;
                if (this is Hyouretsuzan || this is DropKickState) character.changeState(new Fall(), true);
            }

            if (this is Hyouretsuzan || this is DropKickState || this is SigmaUpDownSlashState) return;

            if (player.dashPressed(out string dashControl) && character.canAirDash() && character.canDash())
            {
                CharState dashState;
                if (player.input.isHeld(Control.Up, player) && player.isX && player.hasBootsArmor(3))
                {
                    dashState = new UpDash(Control.Dash);
                }
                else dashState = new AirDash(dashControl);
                if (!character.isDashing)
                {
                    character.changeState(dashState);
                    return;
                }
                else if (character.player.isX)
                {
                    if (!character.player.hasChip(0) && dashState is AirDash && character.lastAirDashWasSide)
                    {
                    }
                    else if (!character.player.hasChip(0) && dashState is AirDash && character.isDashing)
                    {
                    }
                    else if ((!character.player.hasChip(0) && dashState is UpDash && !character.lastAirDashWasSide))
                    {
                    }
                    else if (character.player.hasChip(0))
                    {
                        character.changeState(dashState);
                        return;
                    }
                }
            }

            if (player.input.isPressed(Control.Jump, player) && character.canJump())
            {
                lastJumpPressedTime = Global.time;
            }

            if (player.isZero && Global.time - lastJumpPressedTime < 0.1f && !character.isDashing && character.canAirJump() && character.wallKickCooldown == 0 && character.flag == null && !character.sprite.name.Contains("kick_air"))
            {
                character.dashedInAir++;
                character.vel.y = -character.getJumpPower();
                character.changeState(new Jump());
                return;
            }

            if (player.isX && player.input.isPressed(Control.Jump, player) && character.canJump() && character.isUnderwater() && character.chargedBubbles.Count > 0 && character.flag == null)
            {
                character.vel.y = -character.getJumpPower();
                character.changeState(new Jump());
                return;
            }

            if (player.isAxl && !player.isAI && player.input.isPressed(Control.Jump, player) && character.canJump() && !character.isDashing && character.canAirDash() && character.flag == null)
            {
                character.dashedInAir++;
                character.changeState(new Hover(), true);
            }

            if (player.isX && !player.isAI && player.hasUltimateArmor() && player.input.isPressed(Control.Jump, player) && character.canJump() && !character.isDashing && character.canAirDash() && character.flag == null)
            {
                character.dashedInAir++;
                character.changeState(new XHover(), true);
            }

            if (character.gravityWellModifier > 0)
            {
                if (!player.input.isHeld(Control.Jump, player) && character.vel.y < 0)
                {
                    framesJumpNotHeld++;
                    if (framesJumpNotHeld > 3)
                    {
                        framesJumpNotHeld = 0;
                        character.vel.y = 0;
                    }
                }

                if (player.input.isHeld(Control.Jump, player))
                {
                    framesJumpNotHeld = 0;
                }
            }

            if (character.canStartClimbLadder())
            {
                checkLadder(false);
            }

            var move = new Point(0, 0);

            /*
            if (character.chargedBubbles.Count > 0 && character.player.input.isHeld(Control.Jump, player) && character.isUnderwater())
            {
                float newVelY = -this.character.getJumpPower() * (character.chargedBubbles.Count / 6f);
                character.vel.y = Math.Min(character.vel.y, newVelY);
                character.changeState(new Jump(), true);
            }
            */

            //Cast from base to derived
            var wallKick = this as WallKick;

            if (this is not VileHover)
            {
                if (player.input.isHeld(Control.Left, player))
                {
                    if (wallKick == null || wallKick.kickSpeed <= 0)
                    {
                        if (player.character.canMove()) move.x = -character.getRunSpeed() * character.getDashSpeed();
                        if (player.character.canTurn()) character.xDir = -1;
                    }
                }
                else if (player.input.isHeld(Control.Right, player))
                {
                    if (wallKick == null || wallKick.kickSpeed >= 0)
                    {
                        if (player.character.canMove()) move.x = character.getRunSpeed() * character.getDashSpeed();
                        if (player.character.canTurn()) character.xDir = 1;
                    }
                }
            }

            if (move.magnitude > 0)
            {
                character.move(move);
            }

            if (character.canClimb())
            {
                bool velYRequirementMet = character.vel.y > 0 || (this is VileHover vh && vh.fallY > 0);
                //This logic can be abit confusing, but we are trying to mirror the actual Mega man X wall climb physics
                //In the actual game, X will not initiate a climb if you directly hugging a wall, jump and push in its direction UNTIL you start falling OR you move away and jump into it
                if ((player.input.isPressed(Control.Left, player) && !player.isAI) || (player.input.isHeld(Control.Left, player) && (velYRequirementMet || lastLeftWallCollider == null)))
                {
                    if (lastLeftWallCollider != null)
                    {
                        player.character.changeState(new WallSlide(-1, lastLeftWallCollider));
                        return;
                    }
                }
                else if ((player.input.isPressed(Control.Right, player) && !player.isAI) || (player.input.isHeld(Control.Right, player) && (velYRequirementMet || lastRightWallCollider == null)))
                {
                    if (lastRightWallCollider != null)
                    {
                        player.character.changeState(new WallSlide(1, lastRightWallCollider));
                        return;
                    }
                }
            }
        }
        
        public void groundCode()
        {
            if (!character.grounded)
            {
                character.changeState(new Fall());
                return;
            }
            
            if (character.isAttacking())
            {
                return;
            }

            if (!Options.main.disableDoubleDash && player.input.isPressed(Control.Dash, player) && player.input.checkDoubleTap(Control.Dash) && character.canDash() && character.dodgeRollCooldown == 0 && player.isAxl && player.canControl)
            {
                character.changeState(new DodgeRoll(), true);
                return;
            }

            if (player.isX && character.hasHadoukenEquipped() && character.canUseFgMove())
            {
                if (player.input.checkQCF(player, Control.Shoot))
                {
                    if (!player.hasAllItems()) player.scrap -= 3;
                    player.fgMoveAmmo = 0;
                    character.changeState(new Hadouken(), true);
                    return;
                }
            }
            if (player.isX && character.hasShoryukenEquipped() && character.canUseFgMove())
            {
                if (player.input.checkDragonPunch(player, character.xDir, Control.Shoot))
                {
                    if (!player.hasAllItems()) player.scrap -= 3;
                    player.fgMoveAmmo = 0;
                    character.changeState(new Shoryuken(character.isUnderwater()), true);
                    return;
                }
            }

            if (player.input.isPressed(Control.Jump, player) && player.input.isHeld(Control.Up, player) && character.mk5RideArmorPlatform != null && character.canEjectFromRideArmor())
            {
                character.getOffMK5Platform();
            }

            if (player.input.isPressed(Control.Jump, player) && character.canJump())
            {
                bool hasIceSled = (character.iceSled != null);
                character.vel.y = -character.getJumpPower();
                character.isDashing = character.isDashing || player.dashPressed(out string dashControl);
                character.changeState(new Jump(), hasIceSled);
                return;
            }
            else if (player.dashPressed(out string dashControl))
            {
                if (character.canDash() && !(this is Crouch && player.isAxl)) character.changeState(new Dash(dashControl), true);
                return;
            }
            else if (player.isZero && !player.isZBusterZero() && !character.isDashing && !player.hasKnuckle() && (player.input.isHeld(Control.WeaponLeft, player) || player.input.isHeld(Control.WeaponRight, player)) &&
                (!player.isDisguisedAxl || player.input.isHeld(Control.Down, player)))
            {
                character.changeState(new SwordBlock());
            }
            else if (player.isZero && !player.isZBusterZero() && !character.isDashing && (player.input.isPressed(Control.WeaponLeft, player) || player.input.isPressed(Control.WeaponRight, player)) &&
                (!player.isDisguisedAxl || player.input.isHeld(Control.Down, player)))
            {
                if (!player.hasKnuckle())
                {
                    character.changeState(new SwordBlock());
                }
                else if (character.parryCooldown == 0)
                {
                    character.changeState(new KKnuckleParryStartState());
                }
            }
            else if (player.input.isPressed(Control.Down, player) && character.canClimb())
            {
                character.checkLadderDown = true;
                var ladders = Global.level.getTriggerList(character, 0, 1, null, typeof(Ladder));
                if (ladders.Count > 0)
                {
                    var rect = ladders[0].otherCollider.shape.getRect();
                    var snapX = (rect.x1 + rect.x2) / 2;
                    float xDist = snapX - character.pos.x;
                    if (MathF.Abs(xDist) < 10 && Global.level.checkCollisionActor(character, xDist, 30) == null)
                    {
                        var midX = ladders[0].otherCollider.shape.getRect().center().x;
                        character.changeState(new LadderClimb(ladders[0].gameObject as Ladder, midX));
                        character.move(new Point(0, 30), false);
                        character.stopCamUpdate = true;
                    }
                }
                character.checkLadderDown = false;
            }
            /*
            else if (player.input.isPressed(Control.Special1, player))
            {
                this.character.changeState(new Hadouken());
            }
            */
            else if (player.isCrouchHeld() && !character.isDashing)
            {
                if (!player.isSigma)
                {
                    character.changeState(new Crouch());
                }
                else if (!character.isAttacking() && character.noBlockTime == 0)
                {
                    character.changeState(new SwordBlock());
                }
            }
            else if (player.input.isPressed(Control.Taunt, player) && !character.isAnyZoom() && character.sniperMissileProj == null)
            {
                character.changeState(new Taunt());
            }
            else if (character.canClimb())
            {
                checkLadder(true);
            }
        }

        public void groundCodeWithMove()
        {
            groundCode();
            if (player.character.canTurn())
            {
                if (player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player))
                {
                    if (player.input.isHeld(Control.Left, player)) character.xDir = -1;
                    if (player.input.isHeld(Control.Right, player)) character.xDir = 1;
                    if (player.character.canMove()) character.changeState(new Run());
                }
            }
        }

        public void changeToIdle(string ts = "")
        {
            if (string.IsNullOrEmpty(ts) && (player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player)))
            {
                character.changeState(new Run());
            }
            else
            {
                character.changeState(new Idle(ts));
            }
        }

        public void checkLadder(bool isGround)
        {
            if (player.input.isHeld(Control.Up, player))
            {
                var ladders = Global.level.getTriggerList(character, 0, 0, null, typeof(Ladder));
                if (ladders.Count > 0)
                {
                    var midX = ladders[0].otherCollider.shape.getRect().center().x;
                    if (Math.Abs(character.pos.x - midX) < 12)
                    {
                        var rect = ladders[0].otherCollider.shape.getRect();
                        var snapX = (rect.x1 + rect.x2) / 2;
                        if (Global.level.checkCollisionActor(character, snapX - character.pos.x, 0) == null)
                        {
                            float? incY = null;
                            if (isGround) incY = -10;
                            character.changeState(new LadderClimb(ladders[0].gameObject as Ladder, midX, incY));
                        }
                    }
                }
            }
        }

        public void clampViralSigmaPos()
        {
            float w = 25;
            float h = 35;
            if (character.pos.y < h)
            {
                Point destPos = new Point(character.pos.x, h);
                Point lerpPos = Point.lerp(character.pos, destPos, Global.spf * 10);
                character.changePos(lerpPos);
            }
            if (character.pos.x < w)
            {
                Point destPos = new Point(w, character.pos.y);
                Point lerpPos = Point.lerp(character.pos, destPos, Global.spf * 10);
                character.changePos(lerpPos);
            }

            float rightBounds = Global.level.width - w;
            if (character.pos.x > rightBounds)
            {
                Point destPos = new Point(rightBounds, character.pos.y);
                Point lerpPos = Point.lerp(character.pos, destPos, Global.spf * 10);
                character.changePos(lerpPos);
            }
        }
    }

    public class WarpIn : CharState
    {
        public bool warpSoundPlayed;
        public float destY;
        public float startY;
        public Anim warpAnim;
        bool warpAnimOnce;

        // Sigma-specific
        public bool isSigma { get { return player.isSigma; } }
        public int sigmaRounds;
        public const float yOffset = 200;
        public bool landOnce;
        public bool decloaked;
        public bool addInvulnFrames;
        public bool sigma2Once;

        public WarpIn(bool addInvulnFrames = true) : base("warp_in")
        {
            this.addInvulnFrames = addInvulnFrames;
        }

        public override void update()
        {
            if (!character.ownedByLocalPlayer) return;
            if (!Global.level.mainPlayer.readyTextOver) return;

            if (warpAnim == null && !warpAnimOnce)
            {
                warpAnimOnce = true;
                warpAnim = new Anim(character.pos.addxy(0, -yOffset), character.getSprite("warp_beam"), character.xDir, player.getNextActorNetId(), false, sendRpc: true);
                warpAnim.splashable = false;
            }

            if (warpAnim == null)
            {
                character.visible = true;
                character.frameSpeed = 1;
                if (isSigma && player.isSigma1() && character.sprite.frameIndex >= 2 && !decloaked)
                {
                    decloaked = true;
                    var cloakAnim = new Anim(character.getFirstPOI() ?? character.getCenterPos(), "sigma_cloak", character.xDir, player.getNextActorNetId(), true);
                    cloakAnim.vel = new Point(-25 * character.xDir, -10);
                    cloakAnim.fadeOut = true;
                    cloakAnim.setzIndex(character.zIndex - 1);
                }

                if (isSigma && player.isSigma2() && character.sprite.frameIndex >= 4 && !sigma2Once)
                {
                    sigma2Once = true;
                    character.playSound("sigma2start", sendRpc: true);
                }

                if (character.isAnimOver())
                {
                    character.changeState(new Idle());
                }
                return;
            }

            if (character.player == Global.level.mainPlayer && !warpSoundPlayed)
            {
                warpSoundPlayed = true;
                character.playSound("warpIn", sendRpc: true);
            }

            float yInc = Global.spf * 450 * getSigmaRoundsMod(sigmaRounds);
            warpAnim.incPos(new Point(0, yInc));

            if (isSigma && !landOnce && warpAnim.pos.y >= destY - 1)
            {
                landOnce = true;
                warpAnim.changePos(new Point(warpAnim.pos.x, destY - 1));
            }

            if (warpAnim.pos.y >= destY)
            {
                if (!isSigma || sigmaRounds > 6)
                {
                    warpAnim.destroySelf();
                    warpAnim = null;
                }
                else
                {
                    sigmaRounds++;
                    landOnce = false;
                    warpAnim.changePos(new Point(warpAnim.pos.x, destY - getSigmaYOffset(sigmaRounds)));
                }
            }
        }

        float getSigmaRoundsMod(int aSigmaRounds)
        {
            if (!isSigma) return 1;
            return 2;
        }

        float getSigmaYOffset(int aSigmaRounds)
        {
            if (aSigmaRounds == 0) return yOffset;
            if (aSigmaRounds == 1) return yOffset;
            if (aSigmaRounds == 2) return yOffset;
            if (aSigmaRounds == 3) return yOffset * 0.75f;
            if (aSigmaRounds == 4) return yOffset * 0.5f;
            return yOffset * 0.25f;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.stopMoving();
            character.useGravity = false;
            character.visible = false;
            character.frameSpeed = 0;
            destY = character.pos.y;
            startY = character.pos.y;

            if (player.warpedInOnce || Global.debug)
            {
                sigmaRounds = 10;
            }
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.visible = true;
            character.useGravity = true;
            character.splashable = true;
            if (warpAnim != null)
            {
                warpAnim.destroySelf();
            }
            if (addInvulnFrames && character.ownedByLocalPlayer)
            {
                character.invulnTime = (player.warpedInOnce || Global.level.joinedLate) ? 2 : 0;
            }
            player.warpedInOnce = true;
        }
    }

    public class WarpOut : CharState
    {
        public bool warpSoundPlayed;
        public float destY;
        public float startY;
        public Anim warpAnim;
        public const float yOffset = 200;
        public bool isSigma { get { return player.isSigma; } }
        public bool is1v1MaverickStart;

        public WarpOut(bool is1v1MaverickStart = false) : base("warp_beam")
        {
            this.is1v1MaverickStart = is1v1MaverickStart;
        }

        public override void update()
        {
            if (warpAnim == null)
            {
                return;
            }
            if (is1v1MaverickStart)
            {
                return;
            }

            if (character.player == Global.level.mainPlayer && !warpSoundPlayed)
            {
                warpSoundPlayed = true;
                character.playSound("warpIn");
            }

            warpAnim.pos.y -= Global.spf * 1000;

            if (character.pos.y <= destY)
            {
                warpAnim.destroySelf();
                warpAnim = null;
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.stopMoving();
            character.useGravity = false;
            character.visible = false;
            destY = character.pos.y - yOffset;
            startY = character.pos.y;
            if (!is1v1MaverickStart)
            {
                warpAnim = new Anim(character.pos, character.getSprite("warp_beam"), character.xDir, player.getNextActorNetId(), false, sendRpc: true);
                warpAnim.splashable = false;
            }
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            if (warpAnim != null)
            {
                warpAnim.destroySelf();
            }
        }
    }

    public class Idle : CharState
    {
        public Idle(string transitionSprite = "") : base("idle", "shoot", "attack", transitionSprite)
        {
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            if (player.isX && character.isHyperX)
            {
                sprite = "weak";
                character.changeSpriteFromName("weak", true);
            }
            character.dashedInAir = 0;
        }

        public override void update()
        {
            base.update();

            if (player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player))
            {
                if (!character.isAttacking() && !character.isSoftLocked() && character.canTurn())
                {
                    if (player.input.isHeld(Control.Left, player)) character.xDir = -1;
                    if (player.input.isHeld(Control.Right, player)) character.xDir = 1;
                    if (player.character.canMove()) character.changeState(new Run());
                }
            }
            groundCode();

            if (Global.level.gameMode.isOver)
            {
                if (Global.level.gameMode.playerWon(player))
                {
                    if (!character.sprite.name.Contains("_win"))
                    {
                        character.changeSpriteFromName("win", true);
                    }
                }
                else
                {
                    if (!character.sprite.name.Contains("lose"))
                    {
                        string loseSprite = "lose";
                        if (player.isX && character.player.hasArmor(2)) loseSprite = "mmx_lose_x2";
                        if (player.isX && character.player.hasArmor(3)) loseSprite = "mmx_lose_x3";
                        character.changeSpriteFromName(loseSprite, true);
                    }
                }
            }
        }
    }

    public class Run : CharState
    {
        public Run() : base("run", "run_shoot", "attack")
        {
            accuracy = 5;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
        }

        public override void update()
        {
            base.update();
            var move = new Point(0, 0);
            if (player.input.isHeld(Control.Left, player))
            {
                character.xDir = -1;
                if (player.character.canMove()) move.x = -character.getRunSpeed();
            }
            else if (player.input.isHeld(Control.Right, player))
            {
                character.xDir = 1;
                if (player.character.canMove()) move.x = character.getRunSpeed();
            }
            if (move.magnitude > 0)
            {
                character.move(move);
            }
            else
            {
                character.changeState(new Idle());
            }
            groundCode();
        }
    }

    public class Crouch : CharState
    {
        public Crouch(string transitionSprite = "crouch_start") : base("crouch", "crouch_shoot", "attack_crouch", transitionSprite)
        {
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.globalCollider = character.getCrouchingCollider();
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
        }

        public override void update()
        {
            base.update();
            groundCode();

            if (player.input.isHeld(Control.Right, player))
            {
                character.xDir = 1;
            }
            else if (player.input.isHeld(Control.Left, player))
            {
                character.xDir = -1;
            }

            if (!player.isCrouchHeld() && !(player.isZero && character.isAttacking()))
            {
                character.changeState(new Idle(transitionSprite: "crouch_start"));
                return;
            }

            if (player.isAxl)
            {
                if (player.input.isHeld(Control.Dash, player) && character.canDash() && character.dodgeRollCooldown == 0)
                {
                    character.changeState(new DodgeRoll());
                }
            }

            if (Global.level.gameMode.isOver)
            {
                if (Global.level.gameMode.playerWon(player))
                {
                    if (!character.sprite.name.Contains("_win"))
                    {
                        character.changeSpriteFromName("win", true);
                    }
                }
                else
                {
                    if (!character.sprite.name.Contains("lose"))
                    {
                        character.changeSpriteFromName("lose", true);
                    }
                }
            }
        }
    }

    public class SwordBlock : CharState
    {
        public SwordBlock() : base("block")
        {
            immuneToWind = true;
            superArmor = true;
        }

        public override void update()
        {
            base.update();
            groundCode();

            bool isHoldingGuard;
            if (!player.isSigma)
            {
                isHoldingGuard = player.input.isHeld(Control.WeaponLeft, player) || player.input.isHeld(Control.WeaponRight, player);
            }
            else
            {
                isHoldingGuard = player.isCrouchHeld();
            }

            if (!player.isControllingPuppet())
            {
                bool leftGuard = player.input.isHeld(Control.Left, player);
                bool rightGuard = player.input.isHeld(Control.Right, player);

                if (leftGuard) character.xDir = -1;
                else if (rightGuard) character.xDir = 1;
            }

            if (!isHoldingGuard)
            {
                character.changeState(new Idle());
                return;
            }

            if (player.input.isPressed(Control.Shoot, player) && character.saberCooldown == 0 && !player.isControllingPuppet())
            {
                character.noBlockTime = 0.25f;
                character.changeState(new Idle());
                return;
            }

            if (Global.level.gameMode.isOver)
            {
                if (Global.level.gameMode.playerWon(player))
                {
                    if (!character.sprite.name.Contains("_win"))
                    {
                        character.changeSpriteFromName("win", true);
                    }
                }
                else
                {
                    if (!character.sprite.name.Contains("lose"))
                    {
                        character.changeSpriteFromName("lose", true);
                    }
                }
            }
        }
    }

    public class ZeroClang : CharState
    {
        public int hurtDir;
        public float hurtSpeed;

        public ZeroClang(int dir) : base("clang")
        {
            hurtDir = dir;
            hurtSpeed = dir * 100;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
        }

        public override void update()
        {
            base.update();
            if (hurtSpeed != 0)
            {
                hurtSpeed = Helpers.toZero(hurtSpeed, 400 * Global.spf, hurtDir);
                character.move(new Point(hurtSpeed, 0));
            }
            /*
            if (this.character.isAnimOver())
            {
                this.character.changeState(new Idle());
            }
            */
            if (hurtSpeed == 0)
            {
                character.changeState(new Idle());
            }
        }
    }

    public class Jump : CharState
    {
        public Jump() : base("jump", "jump_shoot", Options.main.getAirAttack())
        {
            accuracy = 5;
            enterSound = "jump";
        }

        public override void update()
        {
            base.update();
            character.iceSled = null;
            airCode();
            if (character.vel.y > 0)
            {
                if (character.sprite?.name?.EndsWith("cannon_air") == false)
                {
                    character.changeState(new Fall());
                }
                return;
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

    public class Fall : CharState
    {
        public float limboVehicleCheckTime;
        public Actor limboVehicle;

        public Fall() : base("fall", "fall_shoot", Options.main.getAirAttack(), "fall_start")
        {
            accuracy = 5;
        }

        public override void update()
        {
            base.update();
            if (limboVehicleCheckTime > 0)
            {
                limboVehicleCheckTime -= Global.spf;
                if (limboVehicle.destroyed || limboVehicleCheckTime <= 0)
                {
                    limboVehicleCheckTime = 0;
                    character.useGravity = true;
                    character.limboRACheckCooldown = 1;
                }
            }
            else
            {
                airCode();
            }
        }

        public void setLimboVehicleCheck(Actor limboVehicle)
        {
            if (limboVehicleCheckTime == 0 && character.limboRACheckCooldown == 0)
            {
                this.limboVehicle = limboVehicle;
                limboVehicleCheckTime = 1;
                character.stopMoving();
                character.useGravity = false;
                if (limboVehicle is RideArmor ra)
                {
                    RPC.checkRAEnter.sendRpc(player.id, ra.netId, ra.neutralId, ra.raNum);
                }
                else if (limboVehicle is RideChaser rc)
                {
                    RPC.checkRCEnter.sendRpc(player.id, rc.netId, rc.neutralId);
                }
            }
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.useGravity = true;
        }
    }

    public class Dash : CharState
    {
        public float dashTime = 0;
        public string initialDashButton;
        public int initialDashDir;

        public Dash(string initialDashButton) : base("dash", "dash_shoot", "attack_dash")
        {
            enterSound = "dash";
            this.initialDashButton = initialDashButton;
            accuracy = 10;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);

            initialDashDir = character.xDir;
            if (player.isAxl && player.axlWeapon?.isTwoHanded(false) == true)
            {
                if (player.input.isHeld(Control.Left, player)) initialDashDir = -1;
                else if (player.input.isHeld(Control.Right, player)) initialDashDir = 1;
            }

            character.isDashing = true;
            character.globalCollider = character.getDashingCollider();
            new Anim(character.getDashSparkEffectPos(initialDashDir), "dash_sparks", initialDashDir, player.getNextActorNetId(), true, sendRpc: true);
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
        }

        public static void dashBackwardsCode(Character character, int initialDashDir)
        {
            if (character.player.isAxl)
            {
                if (character.xDir != initialDashDir)
                {
                    if (!character.sprite.name.EndsWith("backwards"))
                    {
                        character.changeSpriteFromName("dash_backwards", false);
                    }
                }
                else
                {
                    if (character.sprite.name.EndsWith("backwards"))
                    {
                        character.changeSpriteFromName("dash", false);
                    }
                }
            }
        }

        public override void update()
        {
            dashBackwardsCode(character, initialDashDir);

            base.update();
            groundCode();

            if (!player.input.isHeld(initialDashButton, player))
            {
                changeToIdle();
                return;
            }

            if (player.isSigma && player.input.isPressed(Control.Special1, player) && character.flag == null && character.leapSlashCooldown == 0)
            {
                character.changeState(new SigmaWallDashState(-1, true), true);
                return;
            }

            dashTime += Global.spf;
            float modifier = 1;
            if (dashTime > 0.6 * modifier)
            {
                changeToIdle();
                return;
            }
            var move = new Point(0, 0);
            if (player.isX && player.hasBootsArmor(1)) modifier = 1.15f;
            if (player.character.sprite.name.EndsWith("unpo_grab_dash")) modifier = 1.25f;
            move.x = character.getRunSpeed() * character.getDashSpeed() * initialDashDir * modifier;
            character.move(move);
            if (stateTime > 0.1)
            {
                stateTime = 0;
                new Anim(character.getDashDustEffectPos(initialDashDir), "dust", initialDashDir, player.getNextActorNetId(), true, sendRpc: true);
            }
        }
    }

    public class AirDash : CharState
    {
        public float dashTime = 0;
        public string initialDashButton;
        public int initialDashDir;

        public AirDash(string initialDashButton) : base("dash", "dash_shoot")
        {
            enterSound = "dash";
            this.initialDashButton = initialDashButton;
            accuracy = 10;
        }

        public override void update()
        {
            Dash.dashBackwardsCode(character, initialDashDir);

            base.update();

            if (!player.input.isHeld(initialDashButton, player))
            {
                character.changeState(new Fall());
                return;
            }

            float modifier = 1;
            if (player.isX && player.hasBootsArmor(2)) modifier = 1.15f;
            if (player.character.sprite.name.EndsWith("unpo_grab_dash")) modifier = 1.25f;
            dashTime += Global.spf;
            if (dashTime > 0.6 * modifier)
            {
                character.changeState(new Fall());
                return;
            }

            var move = new Point(0, 0);
            move.x = character.getRunSpeed(true) * character.getDashSpeed() * initialDashDir * modifier;
            character.move(move);
            if (stateTime > 0.1)
            {
                stateTime = 0;
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);

            initialDashDir = character.xDir;

            if (player.isAxl && player.axlWeapon?.isTwoHanded(false) == true)
            {
                if (player.input.isHeld(Control.Left, player)) initialDashDir = -1;
                else if (player.input.isHeld(Control.Right, player)) initialDashDir = 1;
            }

            character.isDashing = true;
            character.useGravity = false;
            character.vel = new Point(0, 0);
            character.dashedInAir++;
            character.globalCollider = character.getDashingCollider();
            character.lastAirDashWasSide = true;
            new Anim(character.getDashSparkEffectPos(initialDashDir), "dash_sparks", initialDashDir, null, true);
        }

        public override void onExit(CharState newState)
        {
            character.useGravity = true;
            base.onExit(newState);
        }
    }

    public class UpDash : CharState
    {
        public float dashTime = 0;
        public string initialDashButton;

        public UpDash(string initialDashButton) : base("up_dash", "up_dash_shoot")
        {
            this.initialDashButton = initialDashButton;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.isDashing = true;
            character.useGravity = false;
            character.vel = new Point(0, -4);
            character.dashedInAir++;
            character.lastAirDashWasSide = false;
            character.frameSpeed = 2;
        }

        public override void onExit(CharState newState)
        {
            character.useGravity = true;
            character.vel.y = 0;
            base.onExit(newState);
        }

        public override void update()
        {
            base.update();
            if (!player.input.isHeld(initialDashButton, player))
            {
                character.changeState(new Fall());
                return;
            }

            if (!once)
            {
                once = true;
                character.vel = new Point(0, -250);
                new Anim(character.pos.addxy(0, -10), "dash_sparks_up", character.xDir, player.getNextActorNetId(), true, sendRpc: true);
                character.playSound("dash", sendRpc: true);
            }

            dashTime += Global.spf;
            float maxDashTime = 0.4f;
            if (character.isUnderwater()) maxDashTime *= 1.5f;
            if (dashTime > maxDashTime)
            {
                character.changeState(new Fall());
                return;
            }
        }
    }

    public class WallSlide : CharState
    {
        public int wallDir;
        public float dustTime;
        public Collider wallCollider;
        public WallSlide(int wallDir, Collider wallCollider) : base("wall_slide", "wall_slide_shoot", "wall_slide_attack")
        {
            enterSound = "wallslide";
            this.wallDir = wallDir;
            this.wallCollider = wallCollider;
            accuracy = 2;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.dashedInAir = 0;
            character.quakeBlazerBounces = 0;
            if (player.isAI)
            {
                character.ai.jumpTime = 0;
            }
        }

        public override void update()
        {
            base.update();
            if (character.grounded)
            {
                character.changeState(new Idle());
                return;
            }
            if (player.input.isPressed(Control.Jump, player))
            {
                if (player.input.isHeld(Control.Dash, player))
                {
                    character.isDashing = true;
                }
                character.vel.y = -character.getJumpPower();
                character.changeState(new WallKick(wallDir * -1));
                return;
            }
            if (player.isSigma && player.input.isPressed(Control.Special1, player) && character.flag == null)
            {
                int yDir = player.input.isHeld(Control.Down, player) ? 1 : -1;
                character.changeState(new SigmaWallDashState(yDir, false), true);
                return;
            }

            character.useGravity = false;
            character.vel.y = 0;

            /*
            if (wallDir == -1 && wallCollider?.actor?.isPlatform == true)
            {
                float charWidth = character.collider?.shape.getRect().w() ?? 0;
                character.changePos(new Point(wallCollider.shape.getRect().x2 + 1 + charWidth / 2, character.pos.y));
            }

            if (wallDir == 1 && wallCollider?.actor?.isPlatform == true)
            {
                float charWidth = character.collider?.shape.getRect().w() ?? 0;
                character.changePos(new Point(wallCollider.shape.getRect().x1 - 1 - charWidth / 2, character.pos.y));
            }
            */

            if (stateTime > 0.15)
            {
                if (character.strikeChainProj == null)
                {
                    var dirHeld = wallDir == -1 ? player.input.isHeld(Control.Left, player) : player.input.isHeld(Control.Right, player);
                    var hit = character.getHitWall(wallDir, 0);
                    var hitWall = hit?.gameObject as Wall;

                    if (!dirHeld)
                    {
                        player.character.changeState(new Fall());
                    }
                    else if (hitWall == null || !hitWall.collider.isClimbable)
                    {
                        var hitActor = hit?.gameObject as Actor;
                        if (hitActor == null || !hitActor.isPlatform)
                        {
                            player.character.changeState(new Fall());
                        }
                    }
                }
                character.move(new Point(0, 100));
            }

            dustTime += Global.spf;
            if (stateTime > 0.2 && dustTime > 0.1)
            {
                dustTime = 0;

                var animPoint = character.pos.addxy(12 * character.xDir, 0);
                var rect = new Rect(animPoint.addxy(-3, -3), animPoint.addxy(3, 3));
                if (Global.level.checkCollisionShape(rect.getShape(), null) != null)
                {
                    new Anim(animPoint, "dust", character.xDir, player.getNextActorNetId(), true, sendRpc: true);
                }
            }

        }

        public override void onExit(CharState newState)
        {
            character.useGravity = true;
            base.onExit(newState);
        }
    }

    public class WallKick : CharState
    {
        public int kickDir;
        public float kickSpeed;
        public WallKick(int kickDir) : base("wall_kick", "wall_kick_shoot")
        {
            enterSound = "wallkick";
            this.kickDir = kickDir;
            kickSpeed = kickDir * 150;
            accuracy = 5;
        }

        public override void update()
        {
            base.update();
            if (kickSpeed != 0)
            {
                kickSpeed = Helpers.toZero(kickSpeed, 800 * Global.spf, kickDir);
                bool stopMove = false;
                if (player.input.isHeld(Control.Left, player) && !player.input.isHeld(Control.Right, player) && kickSpeed < 0) stopMove = true;
                if (player.input.isHeld(Control.Right, player) && !player.input.isHeld(Control.Left, player) && kickSpeed > 0) stopMove = true;
                if (!stopMove) character.move(new Point(kickSpeed, 0));
            }
            airCode();
            if (character.vel.y > 0)
            {
                character.changeState(new Fall());
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);

            var wallSparkPoint = character.pos.addxy(12 * character.xDir, 0);
            var rect = new Rect(wallSparkPoint.addxy(-2, -2), wallSparkPoint.addxy(2, 2));
            if (Global.level.checkCollisionShape(rect.getShape(), null) != null)
            {
                new Anim(wallSparkPoint, "wall_sparks", character.xDir, player.getNextActorNetId(), true, sendRpc: true);
            }
            //character.isDashing = false;

            character.wallKickCooldown = 0.35f;

            if (character.isDashing)
            {
                kickSpeed *= 1.25f;
            }
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
        }
    }

    public class LadderClimb : CharState
    {
        public Ladder ladder;
        public float snapX;
        public float? incY;
        public LadderClimb(Ladder ladder, float snapX, float? incY = null) : base("ladder_climb", "ladder_shoot", "ladder_attack", "ladder_start")
        {
            this.ladder = ladder;
            this.snapX = MathF.Round(snapX);
            this.incY = incY;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.changePos(new Point(snapX, character.pos.y));
            
            if (incY != null)
            {
                character.incPos(new Point(0, (float)incY));
            }

            if (character.player == Global.level.mainPlayer)
            {
                Global.level.lerpCamTime = 0.25f;
            }
            character.stopMoving();
            character.useGravity = false;
            character.dashedInAir = 0;
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.frameSpeed = 1;
            character.useGravity = true;
        }

        public override void update()
        {
            base.update();
            character.changePos(new Point(snapX, character.pos.y));
            character.xPushVel = 0;
            character.vel.x = 0;
            character.vel.y = 0;
            if (inTransition())
            {
                return;
            }

            if (player.isVile && character.vileLadderShootCooldown == 0)
            {
                character.changeSpriteFromName(sprite, true);
            }

            if (character.isAttacking() || character.isSigmaShooting())
            {
                character.frameSpeed = 1;
            }
            else
            {
                character.frameSpeed = 0;
            }
            if (character.canClimbLadder())
            {
                if (player.input.isHeld(Control.Up, player))
                {
                    character.move(new Point(0, -75));
                    character.frameSpeed = 1;
                }
                else if (player.input.isHeld(Control.Down, player))
                {
                    character.move(new Point(0, 75));
                    character.frameSpeed = 1;
                }
            }

            var ladderTop = ladder.collider.shape.getRect().y1;
            var yDist = character.physicsCollider.shape.getRect().y2 - ladderTop;
            if (!ladder.collider.isCollidingWith(character.physicsCollider) || MathF.Abs(yDist) < 12)
            {
                if (player.input.isHeld(Control.Up, player))
                {
                    var targetY = ladderTop - 1;
                    if (Global.level.checkCollisionActor(character, 0, targetY - character.pos.y) == null && MathF.Abs(targetY - character.pos.y) < 20)
                    {
                        character.changeState(new LadderEnd(targetY));
                    }
                }
                else
                {
                    character.changeState(new Fall());
                }
            }
            else if (!player.isAI && player.input.isPressed(Control.Jump, player))
            {
                if (!character.isAttacking())
                {
                    dropFromLadder();
                }
            }

            if (character.grounded)
            {
                character.changeState(new Idle());
            }
        }

        // AI should call this manually when they want to drop from a ladder
        public void dropFromLadder()
        {
            character.changeState(new Fall());
        }
    }

    public class LadderEnd : CharState
    {
        public float targetY;
        public LadderEnd(float targetY) : base("ladder_end")
        {
            this.targetY = targetY;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.useGravity = false;
            character.stopMoving();
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.useGravity = true;
        }

        public override void update()
        {
            base.update();
            character.xPushVel = 0;
            character.vel.x = 0;
            character.vel.y = 0;
            if (character.isAnimOver())
            {
                if (character.player == Global.level.mainPlayer)
                {
                    Global.level.lerpCamTime = 0.25f;
                }
                //this.character.pos.y = this.targetY;
                character.incPos(new Point(0, targetY - character.pos.y));
                character.stopCamUpdate = true;
                character.changeState(new Idle());
            }
        }
    }

    public class Taunt : CharState
    {
        float tauntTime = 1;
        public Taunt() : base("win")
        {
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            if (player.charNum == 0) tauntTime = 0.75f;
            if (player.charNum == 1) tauntTime = 0.7f;
            if (player.charNum == 3) tauntTime = 0.75f;
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
        }

        public override void update()
        {
            base.update();

            if (player.charNum == 2)
            {
                if (character.isAnimOver())
                {
                    character.changeState(new Idle());
                }
            }
            else if (stateTime >= tauntTime)
            {
                character.changeState(new Idle());
            }
        }
    }

    public class KnockedDown : CharState
    {
        public int hurtDir;
        public float hurtSpeed;
        public float flinchTime;
        public KnockedDown(int dir) : base("knocked_down")
        {
            hurtDir = dir;
            hurtSpeed = dir * 100;
            flinchTime = 0.5f;
        }

        public override bool canEnter(Character character)
        {
            if (character.isCCImmune()) return false;
            if (character.charState.superArmor || character.charState.invincible) return false;
            if (character.isInvulnerable()) return false;
            if (character.vaccineTime > 0) return false;
            return base.canEnter(character);
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.vel.y = -100;
        }

        public override void update()
        {
            base.update();
            if (hurtSpeed != 0)
            {
                hurtSpeed = Helpers.toZero(hurtSpeed, 400 * Global.spf, hurtDir);
                character.move(new Point(hurtSpeed, 0));
            }

            if (player.character.canCharge() && player.input.isHeld(Control.Shoot, player))
            {
                player.character.increaseCharge();
            }

            if (stateTime >= flinchTime)
            {
                character.changeState(new Idle());
            }
        }
    }

    public class Hurt : CharState
    {
        public int hurtDir;
        public float hurtSpeed;
        public float flinchTime;
        public float miniFlinchTime;
        public bool spiked;
        public Hurt(int dir, int flinchFrames, float miniFlinchTime, bool spiked = false) : base("hurt")
        {
            this.miniFlinchTime = miniFlinchTime;
            if (miniFlinchTime == 0)
            {
                hurtDir = dir;
                hurtSpeed = dir * 100;
                flinchTime = flinchFrames * (1 / 60f);
            }
            else
            {
                flinchTime = miniFlinchTime;
            }
            this.spiked = spiked;
        }

        public bool isMiniFlinch()
        {
            return miniFlinchTime > 0;
        }

        public override bool canEnter(Character character)
        {
            if (character.isCCImmune()) return false;
            if (character.vaccineTime > 0) return false;
            if (character.mk5RideArmorPlatform != null) return false;
            return base.canEnter(character);
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            if (miniFlinchTime == 0)
            {
                if (!spiked) character.vel.y = -100;
            }
            if (player.isX && player.hasBodyArmor(1))
            {
                flinchTime *= 0.75f;
                sprite = "hurt2";
                character.changeSpriteFromName("hurt2", true);
            }
        }

        public override void update()
        {
            base.update();
            if (hurtSpeed != 0)
            {
                hurtSpeed = Helpers.toZero(hurtSpeed, 400 * Global.spf, hurtDir);
                character.move(new Point(hurtSpeed, 0));
            }

            if (miniFlinchTime > 0)
            {
                character.frameSpeed = 0;
                if (Global.frameCount % 2 == 0)
                {
                    if (player.charNum == 0) character.frameIndex = 3;
                    if (player.charNum == 1) character.frameIndex = 3;
                    if (player.charNum == 2) character.frameIndex = 0;
                    if (player.charNum == 3) character.frameIndex = 3;
                }
                else
                {
                    if (player.charNum == 0) character.frameIndex = 2;
                    if (player.charNum == 1) character.frameIndex = 2;
                    if (player.charNum == 2) character.frameIndex = 1;
                    if (player.charNum == 3) character.frameIndex = 2;
                }
            }

            if (player.character.canCharge() && player.input.isHeld(Control.Shoot, player))
            {
                player.character.increaseCharge();
            }

            if (stateTime >= flinchTime)
            {
                character.changeState(new Idle());
            }
        }
    }

    public class GoliathDragged : CharState
    {
        public RideArmor goliath;
        public GoliathDragged(RideArmor goliath) : base("hurt")
        {
            this.goliath = goliath;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.useGravity = false;
            character.vel.y = 0;
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.useGravity = true;
        }

        public override void update()
        {
            base.update();

            var goliathDash = goliath.rideArmorState as RADash;
            if (goliathDash == null || !goliath.isAttacking())
            {
                if (character.grounded) character.changeState(new Idle(), true);
                else character.changeState(new Fall(), true);
                return;
            }

            character.move(goliathDash.getDashVel());
        }
    }

    public class Frozen : CharState
    {
        public float startFreezeTime;
        public float freezeTime;
        public Frozen(float freezeTime) : base("frozen")
        {
            this.startFreezeTime = freezeTime;
            this.freezeTime = freezeTime;
        }

        public override bool canEnter(Character character)
        {
            if (!base.canEnter(character)) return false;
            if (character.freezeInvulnTime > 0) return false;
            if (character.isInvulnerable()) return false;
            if (character.isVaccinated()) return false;
            return !character.isCCImmune() && !character.charState.invincible;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            if (character.vel.y < 0) character.vel.y = 0;
            character.playSound("igFreeze");
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.breakFreeze(player, sendRpc: true);
            character.freezeInvulnTime = 2;
        }

        public override void update()
        {
            base.update();

            freezeTime -= player.mashValue();
            if (freezeTime <= 0)
            {
                freezeTime = 0;
                character.changeState(new Idle(), true);
            }
        }
    }

    public class Stunned : CharState
    {
        public float stunTime = 2;
        public Anim stunAnim;
        public Stunned() : base("lose")
        {
        }

        public override bool canEnter(Character character)
        {
            if (!base.canEnter(character)) return false;
            if (character.stunInvulnTime > 0) return false;
            if (character.isInvulnerable()) return false;
            if (character.charState is SwordBlock) return false;
            if (character.grabInvulnTime > 0) return false;
            if (character.isVaccinated()) return false;
            return !(character.charState is Frozen) && !(character.charState is VileMK2Grabbed) && !character.isCCImmune() && character.chargedRollingShieldProj == null && !character.charState.invincible;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            if (!character.ownedByLocalPlayer) return;
            stunAnim = new Anim(character.getCenterPos(), "vile_stun_static", 1, character.player.getNextActorNetId(), false, sendRpc: true);
            stunAnim.setzIndex(character.zIndex + 100);
            if (character.vel.y < 0) character.vel.y = 0;
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            if (!character.ownedByLocalPlayer) return;
            stunAnim?.destroySelf();
            character.stunInvulnTime = 2;
        }

        public override void update()
        {
            base.update();

            if (player.isX && character.player.hasArmor(2)) character.changeSprite("mmx_lose_x2", true);
            if (player.isX && character.player.hasArmor(3)) character.changeSprite("mmx_lose_x3", true);

            if (!character.ownedByLocalPlayer) return;
            if (stunAnim != null) stunAnim.pos = character.getCenterPos();

            stunTime -= player.mashValue();
            if (stunTime <= 0)
            {
                stunTime = 0;
                character.changeState(new Idle(), true);
            }
        }
    }

    public class Crystalized : CharState
    {
        public float crystalizedTime;
        public Crystalized(int crystalizedTime) : base("idle")
        {
            this.crystalizedTime = crystalizedTime;
        }

        public override bool canEnter(Character character)
        {
            if (!base.canEnter(character)) return false;
            if (character.crystalizeInvulnTime > 0) return false;
            if (!character.ownedByLocalPlayer) return false;
            if (character.isInvulnerable()) return false;
            if (character.isVaccinated()) return false;
            return !character.isCCImmune() && !character.charState.invincible;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.crystalizeStart();
            character.frameSpeed = 0;
            Global.serverClient?.rpc(RPC.playerToggle, (byte)character.player.id, (byte)RPCToggleType.StartCrystalize);
            if (character.player.isAxl) character.changeSprite("axl_crystalized", true);
        }

        public override void onExit(CharState newState)
        {
            character.crystalizeInvulnTime = 2;
            character.crystalizeEnd();
            character.frameSpeed = 1;
            Global.serverClient?.rpc(RPC.playerToggle, (byte)character.player.id, (byte)RPCToggleType.StopCrystalize);
            base.onExit(newState);
        }

        public override void update()
        {
            base.update();
            crystalizedTime -= player.mashValue();
            if (crystalizedTime <= 0)
            {
                crystalizedTime = 0;
                character.changeState(new Idle(), true);
            }
        }
    }

    public class Die : CharState
    {
        bool sigmaHasMavericks;
        public Die() : base("die")
        {
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.useGravity = false;
            character.stopMoving();
            character.stopCharge();
            new Anim(character.pos.addxy(0, -12), "die_sparks", 1, null, true);
            character.stingChargeTime = 0;
            character.removeBarrier();
            character.sniperMissileProj?.destroySelf();
            character.sniperMissileProj = null;
            if (character.ownedByLocalPlayer && character.player.isDisguisedAxl)
            {
                character.player.revertToAxlDeath();
                character.changeSpriteFromName("die", true);
            }
            player.lastDeathWasVileMK2 = character.isVileMK2;
            player.lastDeathWasVileMK5 = character.isVileMK5;
            player.lastDeathWasSigmaHyper = character.isHyperSigma;
            player.lastDeathWasXHyper = character.isHyperX;
            player.lastDeathPos = character.getCenterPos();
            if (player.isAI) player.selectedRAIndex = Helpers.randomRange(0, 3);
            sigmaHasMavericks = player.isSigma && player.mavericks.Count > 0;

            if (player.isSigma && character.ownedByLocalPlayer && character.isHyperSigma)
            {
                player.destroyCharacter();
                Global.serverClient?.rpc(RPC.destroyCharacter, (byte)player.id);
                if (player.isSigma1())
                {
                    var anim = new Anim(character.pos, "sigma_wolf_head_drop", 1, player.getNextActorNetId(), false, sendRpc: true);
                    anim.useGravity = true;
                    anim.ttl = 3;
                    anim.blink = true;
                    anim.collider.wallOnly = true;
                    var ede = new ExplodeDieEffect(player, character.pos, character.pos, "empty", 1, character.zIndex, false, 20, 3, false);
                    ede.host = anim;
                    Global.level.addEffect(ede);
                }
                else if (player.isSigma2())
                {
                    var anim = new Anim(character.pos, character.lastHyperSigmaSprite, 1, player.getNextActorNetId(), false, sendRpc: true);
                    anim.ttl = 3;
                    anim.blink = true;
                    anim.frameIndex = character.lastHyperSigmaFrameIndex;
                    anim.frameSpeed = 0;
                    anim.angle = character.lastViralSigmaAngle;
                    var ede = new ExplodeDieEffect(player, character.pos, character.pos, "empty", 1, character.zIndex, false, 20, 3, false);
                    ede.host = anim;
                    Global.level.addEffect(ede);
                }
                else if (player.isSigma3())
                {
                    string deathSprite = "";
                    if (character.lastHyperSigmaSprite.StartsWith("sigma3_kaiser_virus"))
                    {
                        deathSprite = character.lastHyperSigmaSprite;
                        Point explodeCenterPos = character.pos.addxy(0, -16);
                        var ede = new ExplodeDieEffect(player, explodeCenterPos, explodeCenterPos, "empty", 1, character.zIndex, false, 16, 3, false);
                        Global.level.addEffect(ede);
                    }
                    else
                    {
                        deathSprite = character.lastHyperSigmaSprite + "_body";
                        if (!Global.sprites.ContainsKey(deathSprite))
                        {
                            deathSprite = "sigma3_kaiser_idle";
                        }
                        Point explodeCenterPos = character.pos.addxy(0, -55);
                        var ede = new ExplodeDieEffect(player, explodeCenterPos, explodeCenterPos, "empty", 1, character.zIndex, false, 60, 3, false);
                        Global.level.addEffect(ede);

                        var headAnim = new Anim(character.pos, character.lastHyperSigmaSprite, 1, player.getNextActorNetId(), false, sendRpc: true);
                        headAnim.ttl = 3;
                        headAnim.blink = true;
                        headAnim.setFrameIndexSafe(character.lastHyperSigmaFrameIndex);
                        headAnim.xDir = character.lastHyperSigmaXDir;
                        headAnim.frameSpeed = 0;
                    }

                    var anim = new Anim(character.pos, deathSprite, 1, player.getNextActorNetId(), false, sendRpc: true, zIndex: ZIndex.Background + 1000);
                    anim.ttl = 3;
                    anim.blink = true;
                    anim.setFrameIndexSafe(character.lastHyperSigmaFrameIndex);
                    anim.xDir = character.lastHyperSigmaXDir;
                    anim.frameSpeed = 0;
                }
            }
        }

        public override void onExit(CharState newState)
        {
        }

        public override void update()
        {
            character.xPushVel = 0;
            character.vel.x = 0;
            character.vel.y = 0;
            base.update();

            if (sigmaHasMavericks)
            {
                if (stateTime > 0.75f && !once)
                {
                    once = true;
                    player.destroySigmaEffect();
                    character.visible = false;
                }

                if (once)
                {
                    /*
                    // This code would allow controlling Mavs after death, but would result in camera issues and spectate issues that need to be resolved first
                    if (player.currentMaverick == null)
                    {
                        if (player.isPuppeteer())
                        {
                            foreach (var weapon in new List<Weapon>(player.weapons))
                            {
                                if (weapon is MaverickWeapon mw && mw.maverick != null)
                                {
                                    player.weapons.RemoveAll(w => w is SigmaMenuWeapon);
                                    character.becomeMaverick(mw.maverick);
                                    player.weaponSlot = player.weapons.IndexOf(weapon);
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        return;
                    }
                    */

                    if (!player.isTagTeam())
                    {
                        foreach (var weapon in new List<Weapon>(player.weapons))
                        {
                            if (weapon is MaverickWeapon mw && mw.maverick != null)
                            {
                                mw.maverick.changeState(new MExit(mw.maverick.pos, true), true);
                            }
                        }
                    }

                    player.destroySigma();
                }
            }
            else if (player.isVile || player.isSigma)
            {
                if (stateTime > 0.75f && !once)
                {
                    once = true;
                    character.visible = false;
                    player.explodeDieStart();
                }

                if (stateTime > 2.25f)
                {
                    destroyRideArmor();
                    player.explodeDieEnd();
                }
            }
            else
            {
                if (stateTime > 0.75f)
                {
                    destroyRideArmor();
                    player.destroyCharacter();
                    Global.serverClient?.rpc(RPC.destroyCharacter, (byte)player.id);
                }
            }
        }

        public void destroyRideArmor()
        {
            if (character.vileStartRideArmor != null)
            {
                character.vileStartRideArmor.selfDestructTime = Global.spf;
                RPC.actorToggle.sendRpc(character.vileStartRideArmor.netId, RPCActorToggleType.StartMechSelfDestruct);
            }
        }
    }

    public class GenericGrabbedState : CharState
    {
        public Actor grabber;
        public long savedZIndex;
        public string grabSpriteSuffix;
        public bool reverseZIndex;
        public bool freeOnHitWall;
        public bool lerp;
        public bool freeOnGrabberLeave;
        public string additionalGrabSprite;
        public float notGrabbedTime;
        public float maxNotGrabbedTime;
        public bool customUpdate;
        public GenericGrabbedState(Actor grabber, float maxGrabTime, string grabSpriteSuffix, 
            bool reverseZIndex = false, bool freeOnHitWall = true, bool lerp = true, string additionalGrabSprite = null, float maxNotGrabbedTime = 0.5f) : base("grabbed")
        {
            this.isGrabbedState = true;
            this.grabber = grabber;
            grabTime = maxGrabTime;
            this.grabSpriteSuffix = grabSpriteSuffix;
            this.reverseZIndex = reverseZIndex;
            //this.freeOnHitWall = freeOnHitWall;   //Don't use this unless absolutely needed, it causes issues with octopus grab in FTD
            this.lerp = lerp;
            this.additionalGrabSprite = additionalGrabSprite;
            this.maxNotGrabbedTime = maxNotGrabbedTime;
        }

        public override void update()
        {
            base.update();
            if (customUpdate) return;

            if (grabber.sprite?.name.EndsWith(grabSpriteSuffix) == true || 
                (!string.IsNullOrEmpty(additionalGrabSprite) && grabber.sprite?.name.EndsWith(additionalGrabSprite) == true))
            {
                if (!trySnapToGrabPoint(lerp) && freeOnHitWall)
                {
                    character.changeToIdleOrFall();
                    return;
                }
            }
            else
            {
                notGrabbedTime += Global.spf;
                if (notGrabbedTime > maxNotGrabbedTime)
                {
                    character.changeToIdleOrFall();
                    return;
                }
            }

            grabTime -= player.mashValue();
            if (grabTime <= 0)
            {
                character.changeToIdleOrFall();
            }
        }

        public bool trySnapToGrabPoint(bool lerp)
        {
            Point grabberGrabPoint = grabber.getFirstPOIOrDefault("g");
            Point victimGrabOffset = character.pos.subtract(character.getFirstPOIOrDefault("g", 0));

            Point destPos = grabberGrabPoint.add(victimGrabOffset);
            if (character.pos.distanceTo(destPos) > 25) lerp = true;
            Point lerpPos = lerp ? Point.lerp(character.pos, destPos, 0.25f) : destPos;

            var hit = Global.level.checkCollisionActor(character, lerpPos.x - character.pos.x, lerpPos.y - character.pos.y);
            if (hit?.gameObject is Wall)
            {
                return false;
            }

            character.changePos(lerpPos);
            return true;
        }

        public override bool canEnter(Character character)
        {
            if (!base.canEnter(character)) return false;
            return !character.isInvulnerable() && !character.charState.invincible;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.stopMoving();
            character.stopCharge();
            character.useGravity = false;
            character.grounded = false;
            savedZIndex = character.zIndex;
            if (!reverseZIndex) character.setzIndex(grabber.zIndex - 100);
            else character.setzIndex(grabber.zIndex + 100);
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.grabInvulnTime = 2;
            character.useGravity = true;
            character.setzIndex(savedZIndex);
        }
    }
}
