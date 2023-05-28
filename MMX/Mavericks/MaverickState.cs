using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class MaverickStateCooldown
    {
        public readonly bool isGlobal;  // "Global" states no longer shares the global cooldown but sets all states at their max
        public readonly bool startOnEnter;
        public readonly float maxCooldown;
        public float cooldown;

        public MaverickStateCooldown(bool isGlobal, bool startOnEnter, float maxCooldown)
        {
            this.isGlobal = isGlobal;
            this.startOnEnter = startOnEnter;
            this.maxCooldown = maxCooldown;
        }
    }

    public class MaverickStateConsecutiveData
    {
        public int consecutiveCount;
        public int maxConsecutiveCount;
        public float consecutiveDelay;

        public MaverickStateConsecutiveData(int consecutiveCount, int maxConsecutiveCount, float consecutiveDelay)
        {
            this.consecutiveCount = consecutiveCount;
            this.maxConsecutiveCount = maxConsecutiveCount;
            this.consecutiveDelay = consecutiveDelay;
        }

        public bool isOver()
        {
            return consecutiveCount >= maxConsecutiveCount - 1;
        }
    }

    public class MaverickState
    {
        public string sprite;
        public string defaultSprite;
        public string transitionSprite;
        public float stateTime;
        public int stateFrame;
        public float framesJumpNotHeld = 0;
        public float flySoundTime;

        public bool once;
        public string enterSound;
        public bool stopMoving;
        public bool canEnterSelf = true;
        public bool useGravity = true;
        public bool superArmor;
        public float consecutiveWaitTime;
        public bool stopMovingOnEnter;
        public bool exitOnAnimEnd;
        public bool wasFlying;

        public Collider lastLeftWallCollider;
        public Collider lastRightWallCollider;
        public Wall lastLeftWall;
        public Wall lastRightWall;

        public Maverick maverick;
        public Player player { get { return maverick?.player; } }
        public Input input { get { return maverick?.input; } }

        public MaverickStateConsecutiveData consecutiveData;

        public MaverickState(string sprite, string transitionSprite = null)
        {
            this.sprite = string.IsNullOrEmpty(transitionSprite) ? sprite : transitionSprite;
            this.transitionSprite = transitionSprite;
            defaultSprite = sprite;
            stateTime = 0;
        }

        public virtual void update()
        {
            if (inTransition())
            {
                maverick.frameSpeed = 1;
                if (maverick.isAnimOver() && !Global.level.gameMode.isOver)
                {
                    sprite = defaultSprite;
                    maverick.changeSpriteFromName(sprite, true);
                }
            }

            stateTime += Global.spf;
            stateFrame++;

            var lastLeftWallData = maverick.getHitWall(-Global.spf * 60, 0);
            lastLeftWallCollider = lastLeftWallData != null ? lastLeftWallData.otherCollider : null;
            if (lastLeftWallCollider != null && !lastLeftWallCollider.isClimbable) lastLeftWallCollider = null;
            lastLeftWall = lastLeftWallData?.gameObject as Wall;

            var lastRightWallData = maverick.getHitWall(Global.spf * 60, 0);
            lastRightWallCollider = lastRightWallData != null ? lastRightWallData.otherCollider : null;
            if (lastRightWallCollider != null && !lastRightWallCollider.isClimbable) lastRightWallCollider = null;
            lastRightWall = lastRightWallData?.gameObject as Wall;

            // Moving platforms detection
            CollideData leftWallPlat = maverick.getHitWall(-Global.spf * 300, 0);
            if (leftWallPlat?.gameObject is Wall leftWall && leftWall.isMoving)
            {
                maverick.move(leftWall.deltaMove, useDeltaTime: true);
                lastLeftWallCollider = leftWall.collider;
            }
            else if (leftWallPlat?.gameObject is Actor leftActor && leftActor.isPlatform && leftActor.pos.x < maverick.pos.x)
            {
                lastLeftWallCollider = leftActor.collider;
            }

            CollideData rightWallPlat = maverick.getHitWall(Global.spf * 300, 0);
            if (rightWallPlat?.gameObject is Wall rightWall && rightWall.isMoving)
            {
                maverick.move(rightWall.deltaMove, useDeltaTime: true);
                lastRightWallCollider = rightWall.collider;
            }
            else if (rightWallPlat?.gameObject is Actor rightActor && rightActor.isPlatform && rightActor.pos.x > maverick.pos.x)
            {
                lastRightWallCollider = rightActor.collider;
            }

            if (exitOnAnimEnd)
            {
                if (maverick.isAnimOver())
                {
                    maverick.changeToIdleOrFall();
                }
            }
        }

        public virtual bool canEnter(Maverick maverick)
        {
            if (!canEnterSelf && maverick.state.GetType() == GetType()) return false;
            return true;
        }

        public virtual void onEnter(MaverickState oldState)
        {
            if (oldState is MFly) wasFlying = true;
            if (stopMoving) maverick.stopMoving();
            if (!string.IsNullOrEmpty(enterSound)) maverick.playSound(enterSound, sendRpc: true);
            if (!useGravity) maverick.useGravity = false;
            if (stopMovingOnEnter) maverick.stopMoving();
        }

        public virtual void onExit(MaverickState newState)
        {
            if (maverick is NeonTiger nt)
            {
                if (newState is not MJumpStart && newState is not MJump && newState is not MFall && newState is not NeonTPounceState && newState is not NeonTAirClawState)
                {
                    nt.isDashing = false;
                }
            }

            if (this is not MLand && this is not DrDopplerUncoatState && this is not MEnter && (newState is MIdle || newState is MFall))
            {
                maverick.aiCooldown = maverick.maxAICooldown;
                if (player.isStriker())
                {
                    maverick.autoExit = true;
                }
            }
            if (!useGravity) maverick.useGravity = true;
        }

        public bool inTransition()
        {
            return !string.IsNullOrEmpty(transitionSprite) && sprite == transitionSprite && maverick?.sprite?.name != null && maverick.sprite.name.Contains(transitionSprite);
        }

        public int jumpFramesHeld;
        public const int maxJumpFrames = 10;

        public float getJumpModifier()
        {
            if (jumpFramesHeld == 1) return 1f;
            if (jumpFramesHeld == 2) return 1f;
            if (jumpFramesHeld == 3) return 1.01f;
            if (jumpFramesHeld == 4) return 1.015f;
            if (jumpFramesHeld == 5) return 1.02f;
            if (jumpFramesHeld == 6) return 1.025f;
            if (jumpFramesHeld == 7) return 1.05f;
            if (jumpFramesHeld == 8) return 1.1f;
            if (jumpFramesHeld == 9) return 1.25f;
            if (jumpFramesHeld >= 10) return 1.5f;
            return 0;
        }

        public bool isHoldStateOver(float minTime, float maxTime, float aiTime, string control)
        {
            if (isAI)
            {
                return stateTime > aiTime;
            }
            else
            {
                return stateTime > maxTime || (!input.isHeld(control, player) && stateTime > minTime);
            }
        }

        // Useful for non-jump states
        public void genericJumpCode()
        {
            if (maverick.grounded)
            {
                bool jumpHeld = input.isHeld(Control.Jump, player);
                if (jumpHeld)
                {
                    jumpFramesHeld++;
                    if (jumpFramesHeld > maxJumpFrames)
                    {
                        jumpHeld = false;
                    }
                }
                if (!jumpHeld)
                {
                    if (jumpFramesHeld > 0)
                    {
                        maverick.vel.y = -maverick.getJumpPower() * getJumpModifier();
                        jumpFramesHeld = 0;
                    }
                }
            }
        }

        public bool isAI
        {
            get { return maverick.aiBehavior != MaverickAIBehavior.Control; }
        }

        public void landingCode()
        {
            if (maverick.isHeavy)
            {
                maverick.shakeCamera(sendRpc: true);
                maverick.playSound("crash", sendRpc: true);
            }
            if (maverick is FlameMammoth fm)
            {
                new FlameMStompShockwave(fm.stompWeapon, fm.pos, fm.xDir, player, player.getNextActorNetId(), rpc: true);
            }
            if (maverick is VoltCatfish vc)
            {
                if (!vc.bouncedOnce)
                {
                    vc.bouncedOnce = true;
                    maverick.changeState(new VoltCBounce(), true);
                    return;
                }
                else
                {
                    vc.bouncedOnce = false;
                }
            }
            maverick.dashSpeed = 1;
            maverick.changeState(new MLand(maverick.landingVelY));
        }

        public void airCode(bool canMove = true)
        {
            if (player == null || maverick == null) return;

            if (maverick.grounded)
            {
                landingCode();
                return;
            }

            if (Global.level.checkCollisionActor(maverick, 0, -maverick.getYMod()) != null && maverick.vel.y * maverick.getYMod() < 0)
            {
                maverick.vel.y = 0;
            }

            var move = new Point(0, 0);
            if (canMove)
            {
                var wallKick = this as MWallKick;
                if (input.isHeld(Control.Left, player))
                {
                    if (wallKick == null || wallKick.kickSpeed <= 0)
                    {
                        move.x = -maverick.getRunSpeed() * maverick.getDashSpeed() * maverick.getAirSpeed();
                        maverick.xDir = -1;
                    }
                }
                else if (input.isHeld(Control.Right, player))
                {
                    if (wallKick == null || wallKick.kickSpeed >= 0)
                    {
                        move.x = maverick.getRunSpeed() * maverick.getDashSpeed() * maverick.getAirSpeed();
                        maverick.xDir = 1;
                    }
                }
            }

            if (maverick.canFly && (input.isPressed(Control.Up, player) || (input.isPressed(Control.Jump, player) && !isAI)))
            {
                maverick.changeState(new MFly());
                return;
            }

            if (maverick.canClimb)
            {
                climbIfCheckClimbTrue();
            }

            if (move.magnitude > 0)
            {
                maverick.move(move);
            }

            wallClimbCode();
        }

        public void climbIfCheckClimbTrue()
        {
            if (input.isHeld(Control.Up, player) && this is not StingCClimb && checkClimb())
            {
                if (maverick.grounded)
                {
                    maverick.incPos(new Point(0, -7.5f));
                }
                maverick.changeState(new StingCClimb());
            }
        }

        public bool hitLadder;
        public bool checkClimb()
        {
            var rect = maverick.collider.shape.getRect();
            rect.x1 += 10;
            rect.x2 -= 10;
            rect.y1 += 15;
            rect.y2 -= 15;
            var shape = rect.getShape();
            var ladders = Global.level.getTriggerList(maverick, 0, 0, null, typeof(Ladder));
            var backwallZones = maverick is StingChameleon ? Global.level.getTriggerList(shape, typeof(BackwallZone)) : new List<CollideData>();
            if (ladders.Count > 0 || (backwallZones.Count > 0 && !backwallZones.Any(bw => (bw.gameObject as BackwallZone).isExclusion)))
            {
                if (ladders.Count > 0) hitLadder = true;
                return true;
            }
            return false;
        }

        public void wallClimbCode()
        {
            //This logic can be abit confusing, but we are trying to mirror the actual Mega man X wall climb physics
            //In the actual game, X will not initiate a climb if you directly hugging a wall, jump and push in its direction UNTIL you start falling OR you move away and jump into it
            if ((input.isPressed(Control.Left, player) && !player.isAI) || (input.isHeld(Control.Left, player) && (maverick.vel.y > -150 || lastLeftWallCollider == null)))
            {
                if (maverick.canClimbWall)
                {
                    if (lastLeftWallCollider != null)
                    {
                        maverick.changeState(new MWallSlide(-1, lastLeftWall));
                        return;
                    }
                }
                else
                {
                    if (lastLeftWallCollider != null && input.isPressed(Control.Jump, player) && Global.maverickWallClimb)
                    {
                        maverick.vel.y = -maverick.getJumpPower() * maverick.getYMod();
                        maverick.changeState(new MWallKick(1, "jump"));
                    }
                }
            }
            else if ((input.isPressed(Control.Right, player) && !player.isAI) || (input.isHeld(Control.Right, player) && (maverick.vel.y > -150 || lastRightWallCollider == null)))
            {
                if (maverick.canClimbWall)
                {
                    if (lastRightWallCollider != null)
                    {
                        maverick.changeState(new MWallSlide(1, lastRightWall));
                        return;
                    }
                }
                else
                {
                    if (lastRightWallCollider != null && input.isPressed(Control.Jump, player) && Global.maverickWallClimb)
                    {
                        maverick.vel.y = -maverick.getJumpPower() * maverick.getYMod();
                        maverick.changeState(new MWallKick(-1, "jump"));
                    }
                }
            }
        }

        public float shootHeldTime;

        public void commonGroundCode()
        {
            if (!maverick.grounded)
            {
                maverick.changeState(new MFall());
                return;
            }
        }

        public void groundCode()
        {
            commonGroundCode();
            if (maverick.isAttacking())
            {
                return;
            }

            if (input.isPressed(Control.Jump, player))
            {
                maverick.changeState(new MJumpStart());
                return;
            }
            else if (input.isPressed(Control.Up, player) && maverick.canFly)
            {
                maverick.stopMoving();
                maverick.incPos(new Point(0, -10));
                maverick.changeState(new MFly());
                return;
            }
            else if (input.isPressed(Control.Taunt, player))
            {
                maverick.changeState(new MTaunt());
            }
            else if (player.input.isPressed(Control.Down, player) && !maverick.canClimb)
            {
                maverick.checkLadderDown = true;
                var ladders = Global.level.getTriggerList(maverick, 0, 1, null, typeof(Ladder));
                if (ladders.Count > 0)
                {
                    var rect = ladders[0].otherCollider.shape.getRect();
                    var snapX = (rect.x1 + rect.x2) / 2;
                    float xDist = snapX - maverick.pos.x;
                    if (MathF.Abs(xDist) < 10 && Global.level.checkCollisionActor(maverick, xDist, 30) == null)
                    {
                        maverick.move(new Point(xDist, 1), false);
                    }
                }
                maverick.checkLadderDown = false;
            }
            else if (player.input.isPressed(Control.Down, player) && maverick.canClimb)
            {
                maverick.checkLadderDown = true;
                var ladders = Global.level.getTriggerList(maverick, 0, 1, null, typeof(Ladder));
                if (ladders.Count > 0)
                {
                    var rect = ladders[0].otherCollider.shape.getRect();
                    var snapX = (rect.x1 + rect.x2) / 2;
                    float xDist = snapX - maverick.pos.x;
                    if (MathF.Abs(xDist) < 10 && Global.level.checkCollisionActor(maverick, xDist, 30) == null)
                    {
                        maverick.changeState(new StingCClimb());
                        maverick.move(new Point(0, 30), false);
                        player.character.stopCamUpdate = true;
                        maverick.changePos(new Point(snapX, maverick.pos.y));
                        if (maverick.player == Global.level.mainPlayer)
                        {
                            Global.level.lerpCamTime = 0.25f;
                        }
                    }
                }
                maverick.checkLadderDown = false;
            }
            else if (maverick.canClimb)
            {
                climbIfCheckClimbTrue();
            }
        }

        public void morphMothBeam(Point shootPos, bool isGround)
        {
            maverick.playSound("morphmBeam", sendRpc: true);
            Point shootDir;
            var inputDir = input.getInputDir(player);
            if (inputDir.isZero()) shootDir = isGround ? new Point(maverick.xDir, 0) : new Point(0, 1);
            else if (inputDir.x == 0 && inputDir.y == -1) shootDir = new Point(maverick.xDir, -1);
            else shootDir = inputDir;

            if (shootDir.x != 0) shootDir.x = maverick.xDir;

            new MorphMBeamProj(maverick.weapon, shootPos, shootPos.add(shootDir.normalize().times(150)), maverick.xDir, player, player.getNextActorNetId(), rpc: true);
        }

        public CollideData checkCollision(float x, float y, bool autoVel = false)
        {
            return Global.level.checkCollisionActor(maverick, x, y, autoVel: autoVel);
        }

        // Use this for code that slides the maverick across the ground and needs to check if a side wall was hit.
        // Be sure to pass in y = -2 (or -2 offset).
        // This will handle inclines properly, for example sliding from an incline to another inline, or to flat ground.
        public CollideData checkCollisionSlide(float x, float y)
        {
            var hitWall = Global.level.checkCollisionActor(maverick, x, y, autoVel: true);
            if (maverick.deltaPos.isCloseToZero(1) && stateFrame > 1)
            {
                return hitWall;
            }
            return null;
        }

        // Use this for code that needs to check for an accurate normal especially when hitting corners.
        public CollideData checkCollisionNormal(float x, float y)
        {
            var hitWall = checkCollisionSlide(x, y);
            if (hitWall == null) return null;

            var hitPoint = hitWall.getHitPointSafe();
            var maverickColliderPoints = maverick.collider.shape.getRect().getPoints().OrderBy(p => p.distanceTo(hitPoint));

            var raycastDir = new Point(x, y).normalize().times(25);
            // Raycast from each of the 4 corners of the maverick collision box, ordered by ones closest to the first hitWall check.
            // The hitData from the first raycast that hits the same wall will give the most accurate normal when hitting a wall corner.
            foreach (var origin in maverickColliderPoints)
            {
                var hitWall2 = Global.level.raycast(origin, origin.add(raycastDir), new List<Type>() { typeof(Wall) });
                if (hitWall2?.gameObject == hitWall.gameObject)
                {
                    hitWall.hitData = hitWall2.hitData;
                    break;
                }
            }

            return hitWall;
        }

        public virtual bool trySetGrabVictim(Character grabbed)
        {
            return false;
        }
    }

    public class MIdle : MaverickState
    {
        public MIdle(string transitionSprite = "") : base("idle", transitionSprite)
        {
        }

        float attackCooldown = 0;
        public override void update()
        {
            base.update();

            if (maverick == null) return;

            Helpers.decrementTime(ref attackCooldown);

            bool dashCondition = input.isHeld(Control.Left, player) || input.isHeld(Control.Right, player);
            // if (maverick is FakeZero) dashCondition = input.isPressed(Control.Left, player) || input.isPressed(Control.Right, player);

            if (dashCondition)
            {
                if (!maverick.isAttacking() && (maverick.aiBehavior != MaverickAIBehavior.Control || maverick is not BoomerKuwanger))
                {
                    if (input.isHeld(Control.Left, player)) maverick.xDir = -1;
                    if (input.isHeld(Control.Right, player)) maverick.xDir = 1;

                    bool changeToRun = true;
                    if (maverick is OverdriveOstrich)
                    {
                        Global.breakpoint = true;
                        var hit = Global.level.checkCollisionActor(maverick, maverick.xDir, -2, vel: new Point(maverick.xDir, 0));
                        Global.breakpoint = false;
                        if (hit?.isSideWallHit() == true)
                        {
                            changeToRun = false;
                        }
                    }

                    if (changeToRun)
                    {
                        maverick.changeState(new MRun());
                    }
                }
            }
            groundCode();

            if (Global.level.gameMode.isOver && player != null && maverick != null)
            {
                if (Global.level.gameMode.playerWon(player))
                {
                    maverick.changeState(new MTaunt());
                }
            }
        }
    }

    public class MEnter : MaverickState
    {
        public float destY;
        public MEnter(Point destPos) : base("enter", "")
        {
            destY = destPos.y;
        }

        public override void update()
        {
            base.update();
            maverick.alpha = Helpers.clamp01(stateTime * 2);
            maverick.incPos(new Point(0, maverick.vel.y * Global.spf));
            maverick.vel.y += Physics.gravity * Global.spf;
            if (maverick.pos.y >= destY)
            {
                maverick.changePos(new Point(maverick.pos.x, destY));
                if (maverick is DrDoppler)
                {
                    maverick.changeState(new DrDopplerUncoatState(), true);
                }
                else
                {
                    landingCode();
                }
            }
        }

        public Point getDestPos()
        {
            return new Point(maverick.pos.x, destY);
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.useGravity = false;
            maverick.alpha = 0;
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            maverick.useGravity = true;
            maverick.alpha = 1;
        }
    }

    public class MExit : MaverickState
    {
        public float destY;
        public Point destPos;
        bool isRecall;
        public const float yPos = 164;
        public MExit(Point destPos, bool isRecall) : base("exit", "")
        {
            this.destPos = destPos;
            this.isRecall = isRecall;
        }

        public override void update()
        {
            base.update();
            maverick.alpha = Helpers.clamp01(1 - stateTime * 2);
            maverick.incPos(new Point(0, maverick.vel.y * Global.spf));
            maverick.vel.y += Physics.gravity * Global.spf * maverick.getYMod();
            if ((maverick.getYMod() == 1 && maverick.pos.y < destY) || (maverick.getYMod() == -1 && maverick.pos.y > destY))
            {
                maverick.changePos(destPos.addxy(0, -yPos * maverick.getYMod()));
                if (!isRecall)
                {
                    maverick.changeState(new MEnter(destPos));
                }
                else
                {
                    maverick.destroySelf();
                }
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.useGravity = false;
            maverick.vel.x = 0;
            maverick.vel.y = -400 * maverick.getYMod();
            destY = maverick.pos.y - (yPos * maverick.getYMod());
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            maverick.alpha = 1;
            maverick.useGravity = true;
            maverick.vel = new Point();
        }
    }

    public class MTaunt : MaverickState
    {
        public MTaunt() : base("taunt", "")
        {
        }

        public override void update()
        {
            base.update();
            if (maverick.isAnimOver() && !Global.level.gameMode.playerWon(player))
            {
                maverick.changeState(new MIdle());
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            if (maverick is FlameMammoth)
            {
                maverick.playSound("flamemTaunt", sendRpc: true);
            }
            else if (maverick is Velguarder)
            {
                maverick.playSound("velgHowl", sendRpc: true);
            }
            else if (maverick is OverdriveOstrich)
            {
                maverick.playSound("overdriveoTaunt", sendRpc: true);
            }
        }
    }

    public class MRun : MaverickState
    {
        float dustTime;
        float runSoundTime;

        public MRun() : base("run", "")
        {
        }

        public override void update()
        {
            base.update();

            var oo = maverick as OverdriveOstrich;
            if (oo != null)
            {
                Helpers.decrementTime(ref dustTime);
                Helpers.decrementTime(ref runSoundTime);
                if (dustTime == 0 && oo.getRunSpeed() >= oo.dustSpeed)
                {
                    new Anim(oo.pos.addxy(-oo.xDir * 15, 0), "dust", oo.xDir, player.getNextActorNetId(), true, sendRpc: true) { frameSpeed = 1.5f };
                    dustTime = 0.05f;
                }
                if (runSoundTime == 0 && oo.getRunSpeed() >= oo.dustSpeed)
                {
                    oo.playSound("overdriveoRun", sendRpc: true);
                    runSoundTime = 0.175f;
                }

                if (input.isHeld(Control.Left, player) && oo.xDir == 1 && oo.getRunSpeed() >= oo.skidSpeed)
                {
                    oo.changeState(new OverdriveOSkidState(), true);
                    return;
                }
                else if (input.isHeld(Control.Right, player) && oo.xDir == -1 && oo.getRunSpeed() >= oo.skidSpeed)
                {
                    oo.changeState(new OverdriveOSkidState(), true);
                    return;
                }
            }
            
            var move = new Point(0, 0);
            if (input.isHeld(Control.Left, player))
            {
                maverick.xDir = -1;
                move.x = -maverick.getRunSpeed();
            }
            else if (input.isHeld(Control.Right, player))
            {
                maverick.xDir = 1;
                move.x = maverick.getRunSpeed();
            }
            if (move.magnitude > 0)
            {
                maverick.move(move);
            }
            else
            {
                if (oo != null && oo.getRunSpeed() >= 100)
                {
                    oo.accSpeed -= Global.spf * 500;
                    maverick.move(new Point(oo.getRunSpeed() * oo.xDir, 0));
                    // oo.changeState(new OverdriveOSkidState(), true);
                    // return;
                }
                else
                {
                    maverick.changeState(new MIdle());
                }
            }
            groundCode();

            if (maverick is OverdriveOstrich oo2 && move.x != 0)
            {
                bool overWallSkidSpeed = oo.getRunSpeed() >= oo.wallSkidSpeed;
                var hit = checkCollisionSlide(MathF.Sign(move.x), -2);
                if (hit?.isSideWallHit() == true && maverick.deltaPos.isZero() && stateFrame > 1)
                {
                    if (overWallSkidSpeed)
                    {
                        oo2.changeState(new OverdriveOSkidState(), true);
                    }
                    else
                    {
                        maverick.changeState(new MIdle());
                    }
                }
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            flySoundTime = 0;
        }
    }

    public class MJumpStart : MaverickState
    {
        new float jumpFramesHeld;
        float preJumpFramesHeld;
        const float maxPreJumpFrames = 4;
        new const float maxJumpFrames = 2;
        float additionalJumpPower;
        public MJumpStart(float additionalJumpPower = 1) : base("jump_start", "")
        {
            this.additionalJumpPower = additionalJumpPower;
        }

        public override void update()
        {
            base.update();

            if (maverick is BoomerKuwanger ||
                (maverick is OverdriveOstrich oo && oo.deltaPos.magnitude > 100 * Global.spf) || 
                (maverick is FakeZero fz))
            {
                maverick.vel.y = -maverick.getJumpPower() * getJumpModifier() * maverick.getYMod();
                maverick.changeState(new MJump());
                return;
            }

            if (maverick is NeonTiger nt && nt.isDashing)
            {
                maverick.vel.y = -maverick.getJumpPower() * getJumpModifier() * maverick.getYMod() * 0.7f;
                var jumpState = new NeonTPounceState();
                maverick.changeState(jumpState);
                return;
            }

            bool jumpHeld = player.input.isHeld(Control.Jump, player);
            if (maverick.aiBehavior != MaverickAIBehavior.Control)
            {
                jumpHeld = true;
            }

            if (jumpHeld)
            {
                preJumpFramesHeld++;
                if (preJumpFramesHeld > maxPreJumpFrames)
                {
                    jumpFramesHeld++;
                }
            }
            else
            {
                maverick.vel.y = -maverick.getJumpPower() * getJumpModifier() * maverick.getYMod() * additionalJumpPower;
                maverick.changeState(new MJump());
                return;
            }

            if (maverick.isAnimOver())
            {
                maverick.vel.y = -maverick.getJumpPower() * getJumpModifier() * maverick.getYMod() * additionalJumpPower;
                maverick.changeState(new MJump());
            }
        }

        public new float getJumpModifier()
        {
            float minHeight = 1f;
            float maxHeight = 1.25f;

            return minHeight + (maxHeight - minHeight) * Helpers.clamp01((jumpFramesHeld + preJumpFramesHeld) / (maxPreJumpFrames + maxJumpFrames));
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            if ((player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player)))
            {
                maverick.dashSpeed = 1.5f;
                if (jumpFramesHeld >= 5) maverick.dashSpeed = 2;
            }
        }
    }

    public class MJump : MaverickState
    {
        new int jumpFramesHeld = 0;
        public bool fromCling;
        public MaverickState followUpAiState;
        public MJump(MaverickState followUpAiState = null) : base("jump", "")
        {
            this.followUpAiState = followUpAiState;
        }

        public override void update()
        {
            base.update();

            if (!player.input.isHeld(Control.Jump, player))
            {
                jumpFramesHeld = 6;
            }

            bool jumpHeld = player.input.isHeld(Control.Jump, player);
            if (jumpHeld && jumpFramesHeld < 6)
            {
                jumpFramesHeld++;
                maverick.vel.y -= Global.spf * 1250 * maverick.getYMod();
            }

            if (maverick.vel.y * maverick.getYMod() > 0)
            {
                maverick.changeState(followUpAiState ?? new MFall());
                return;
            }
            airCode();
        }
    }

    public class MFall : MaverickState
    {
        public MFall(string transitionSprite = "") : base("fall", transitionSprite)
        {
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            airCode();
        }
    }

    public class MFly : MaverickState
    {
        public MFly(string transitionSprite = "") : base("fly", transitionSprite)
        {
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (Global.level.checkCollisionActor(maverick, 0, -maverick.getYMod()) != null && maverick.vel.y * maverick.getYMod() < 0)
            {
                maverick.vel.y = 0;
            }

            Point move;
            if (maverick is BlastHornet)
            {
                move = getHornetMove();
            }
            else
            {
                move = getMove();
            }

            if (move.magnitude > 0)
            {
                maverick.move(move);
            }

            if (input.isPressed(Control.Jump, player))
            {
                maverick.changeToIdleOrFall();
            }
            else if (maverick.grounded)
            {
                landingCode();
            }
        }

        public Point flyVel;
        float flyVelAcc = 500;
        float flyVelMaxSpeed = 175;
        public Point getHornetMove()
        {
            var inputDir = input.getInputDir(player);
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

            Helpers.decrementTime(ref maverick.ammo);
            if (maverick.ammo <= 0 || maverick.gravityWellModifier > 1)
            {
                flyVel.y = 100;
            }

            maverick.turnToInput(input, player);

            var hit = maverick.checkCollision(flyVel.x * Global.spf, flyVel.y * Global.spf);
            if (hit != null && !hit.isGroundHit())
            {
                flyVel = flyVel.subtract(flyVel.project(hit.getNormalSafe()));
            }

            return flyVel;
        }

        public Point getMove()
        {
            Point move = new Point();

            if (input.isHeld(Control.Left, player))
            {
                move.x = -maverick.getRunSpeed() * maverick.getDashSpeed() * maverick.getAirSpeed();
                maverick.xDir = -1;
            }
            else if (input.isHeld(Control.Right, player))
            {
                move.x = maverick.getRunSpeed() * maverick.getDashSpeed() * maverick.getAirSpeed();
                maverick.xDir = 1;
            }

            Helpers.decrementTime(ref maverick.ammo);
            if (maverick.ammo > 0 && maverick.gravityWellModifier <= 1)
            {
                if (input.isHeld(Control.Up, player))
                {
                    if (maverick.pos.y > -5)
                    {
                        move.y = -maverick.getRunSpeed() * maverick.getDashSpeed();
                    }
                }
                else if (input.isHeld(Control.Down, player))
                {
                    move.y = maverick.getRunSpeed() * maverick.getDashSpeed();
                }
            }
            else
            {
                move.y = maverick.getRunSpeed() * maverick.getDashSpeed();
            }

            if (!maverick.isUnderwater())
            {
                move.x *= 1.25f;
            }
            else
            {
                move.x *= 0.75f;
                move.y *= 0.75f;
                maverick.frameSpeed = 0.75f;
            }

            if (move.y != 0)
            {
                if (sprite != "fly_fall")
                {
                    sprite = "fly_fall";
                    maverick.changeSpriteFromName(sprite, false);
                }
            }
            else
            {
                if (sprite != "fly")
                {
                    sprite = "fly";
                    maverick.changeSpriteFromName(sprite, false);
                }
            }

            return move;
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.useGravity = false;

            float flyVelX = 0;
            if (maverick.dashSpeed > 1)
            {
                flyVelX = maverick.deltaPos.normalize().times(flyVelMaxSpeed).x;
            }
            float flyVelY = maverick.vel.y;
            flyVel = new Point(flyVelX, flyVelY);
            if (flyVel.magnitude > flyVelMaxSpeed) flyVel = flyVel.normalize().times(flyVelMaxSpeed);

            maverick.dashSpeed = 1;
            maverick.stopMoving();
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            maverick.useGravity = true;
            maverick.stopMoving();
        }
    }

    public class MLand : MaverickState
    {
        float landingVelY;
        bool jumpHeldOnce;
        public MLand(float landingVelY) : base("land", "")
        {
            this.landingVelY = landingVelY;
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            if (landingVelY < 360) maverick.frameSpeed = 1.5f;
            if (landingVelY < 315) maverick.frameSpeed = 2f;
        }

        public override void update()
        {
            base.update();
            if (player == null) return;
            if (maverick is OverdriveOstrich oo)
            {
                if (oo.getRunSpeed() > 100) maverick.frameSpeed = 2;
                if (oo.getRunSpeed() > 200) maverick.frameSpeed = 3;
                if (maverick.isAnimOver() || oo.getRunSpeed() > 300)
                {
                    if (input.isHeld(Control.Left, player) || input.isHeld(Control.Right, player))
                    {
                        maverick.changeState(new MRun());
                    }
                    else
                    {
                        maverick.changeState(new MIdle());
                    }
                }
            }
            else if (maverick is BubbleCrab bc && bc.shield != null)
            {
                jumpHeldOnce = jumpHeldOnce || input.isHeld(Control.Jump, player);
                if (!once)
                {
                    once = true;
                    bc.shield.startShieldBounceY();
                    bc.playSound("bcrabBounce", sendRpc: true);
                }
                if (bc.isAnimOver() || bc.shield.shieldBounceTimeY > bc.shield.halfShieldBounceMaxTime)
                {
                    if (jumpHeldOnce)
                    {
                        float additionalJumpPower = 1 + (landingVelY / 400) * 0.25f;
                        bc.changeState(new MJumpStart(additionalJumpPower));
                    }
                    else
                    {
                        maverick.changeState(new MIdle());
                    }
                }
            }
            else
            {
                if (maverick.isAnimOver())
                {
                    maverick.changeState(new MIdle());
                }
            }
        }
    }

    public class MHurt : MaverickState
    {
        public int hurtDir;
        public float flinchTime;
        public float hurtSpeed;
        public MHurt(int dir, int flinchFrames) : base("hurt")
        {
            hurtDir = dir;
            hurtSpeed = dir * 200;
            flinchTime = flinchFrames * (1 / 60f);
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            if (oldState is MFly)
            {
                wasFlying = true;
                maverick.useGravity = false;
            }
            else if (oldState.wasFlying && 
                (oldState is StormEDiveState || oldState is StormEAirShootState || oldState is StormEEggState ||
                oldState is MorphMSweepState || oldState is MorphMShootAir ||
                oldState is BHornetShootState || oldState is BHornetShoot2State || oldState is BHornetStingState))
            {
                wasFlying = true;
                maverick.useGravity = false;
            }

            if (shouldKnockUp())
            {
                maverick.vel.y = -100;
            }
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            maverick.useGravity = true;
        }

        public bool noMove()
        {
            return maverick is LaunchOctopus || maverick is StormEagle || maverick is FlameMammoth || maverick is BlizzardBuffalo || maverick is GravityBeetle || maverick is TunnelRhino || maverick is WheelGator;
        }

        public bool shouldKnockUp()
        {
            return maverick is StingChameleon || maverick is Velguarder || maverick is WireSponge || maverick is ChillPenguin;
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (hurtSpeed != 0 && !noMove())
            {
                hurtSpeed = Helpers.toZero(hurtSpeed, 600 * Global.spf, hurtDir);
                //hurtSpeed = Helpers.lerp(hurtSpeed, 0, Global.spf * 10);
                maverick.move(new Point(hurtSpeed, 0));
            }

            if (stateTime >= flinchTime)
            {
                maverick.changeToIdleFallOrFly();
            }
        }
    }

    public class MDie : MaverickState
    {
        bool isEnvDeath;
        Point deathPos;
        public MDie(bool isEnvDeath) : base("die")
        {
            this.isEnvDeath = isEnvDeath;
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.useGravity = false;
            maverick.stopMoving();
            maverick.globalCollider = null;
            deathPos = maverick.pos;
            if (maverick is Velguarder)
            {
                maverick.visible = false;
                new VelGDeathAnim(deathPos, maverick.xDir, player, player.getNextActorNetId(), sendRpc: true);
            }
            else if (maverick is FakeZero)
            {
                maverick.visible = false;
                Anim.createGibEffect("fakezero_piece", maverick.getCenterPos(), player, gibPattern: GibPattern.SemiCircle, sendRpc: true);
                maverick.playSound("explosion", sendRpc: true);
            }
            if (isEnvDeath)
            {
                maverick.lastGroundedPos = null;
            }
        }

        public override void update()
        {
            base.update();
            if (player == null) return;
            if (maverick.destroyed) return;

            float maxTime = 0.75f;
            if (maverick is Velguarder)
            {
                maxTime = 0.5f;
            }
            else if (maverick is FakeZero)
            {
                maxTime = 0;
            }

            if (stateTime > maxTime && !once)
            {
                once = true;

                if (maverick is not Velguarder && maverick is not FakeZero)
                {
                    var dieEffect = ExplodeDieEffect.createFromActor(maverick.player, maverick, 20, 5.5f, true, overrideCenterPos: maverick.getCenterPos());
                    Global.level.addEffect(dieEffect);
                }
                
                if (player.maverick1v1 != null)
                {
                    player.maverick1v1Kill();
                }
                else if (player.currentMaverick == maverick)
                {
                    // If sigma is not dead, become sigma
                    if (player.character != null && player.character.charState is not Die)
                    {
                        Point spawnPos;
                        Point closestSpawnPoint = Global.level.getClosestSpawnPoint(deathPos).pos;
                        if (isEnvDeath)
                        {
                            if (maverick.lastGroundedPos == null)
                            {
                                spawnPos = closestSpawnPoint;
                            }
                            else
                            {
                                spawnPos = deathPos.distanceTo(closestSpawnPoint) < deathPos.distanceTo(maverick.lastGroundedPos.Value) ? 
                                    closestSpawnPoint : 
                                    maverick.lastGroundedPos.Value;
                            }
                        }
                        else if (maverick.lastGroundedPos != null)
                        {
                            spawnPos = deathPos.distanceTo(closestSpawnPoint) < deathPos.distanceTo(maverick.lastGroundedPos.Value) ?
                                    closestSpawnPoint :
                                    maverick.lastGroundedPos.Value;
                        }
                        else
                        {
                            spawnPos = closestSpawnPoint;
                        }

                        player.character.becomeSigma(spawnPos, maverick.xDir);
                        player.removeWeaponSlot(player.weapons.FindIndex(w => w is MaverickWeapon mw && mw.maverick == maverick));
                        player.changeWeaponSlot(player.weapons.FindIndex(w => w is SigmaMenuWeapon));
                    }
                    /*
                    // If sigma is dead, become the next maverick if available
                    else if (player.character != null)
                    {
                        var firstAvailableMaverick = player.weapons.FirstOrDefault(w => w is MaverickWeapon mw && mw.maverick != null && mw.maverick != maverick) as MaverickWeapon;
                        if (firstAvailableMaverick != null)
                        {
                            player.character.becomeMaverick(firstAvailableMaverick.maverick);
                        }

                        player.weapons.RemoveAll(w => w is MaverickWeapon mw && mw.maverick == maverick);

                        if (firstAvailableMaverick != null)
                        {
                            player.weaponSlot = player.weapons.IndexOf(firstAvailableMaverick);
                        }
                        else
                        {
                            player.weaponSlot = 0;
                        }
                    }
                    */
                }
                else
                {
                    player.weapons.RemoveAll(w => w is MaverickWeapon mw && mw.maverick == maverick);
                    player.changeToSigmaSlot();
                }

                maverick.destroySelf();
            }
        }
    }

    public class MWallSlide : MaverickState
    {
        public int wallDir;
        public float dustTime;
        public Wall wall;
        public bool leftOff;
        public MWallSlide(int wallDir, Wall wall) : base("wall_slide")
        {
            this.wallDir = wallDir;
            this.wall = wall;
        }

        public override void update()
        {
            base.update();
            if (maverick.grounded)
            {
                maverick.changeState(new MIdle());
                return;
            }
            if (input.isPressed(Control.Jump, player))
            {
                maverick.vel.y = -maverick.getJumpPower();
                maverick.changeState(new MWallKick(wallDir * -1));
                return;
            }
            else if (input.isPressed(Control.Dash, player))
            {
                if (maverick is Velguarder)
                {
                    maverick.changeState(new VelGPounceState());
                    maverick.vel.y = -maverick.getJumpPower() * 0.75f;
                    maverick.xDir *= -1;
                }
                else if (maverick is FlameStag)
                {
                    maverick.changeState(new FStagWallDashState());
                    if (!input.isHeld(Control.Down, player)) maverick.vel.y = -maverick.getJumpPower() * 1.5f;
                    else maverick.incPos(new Point(-wallDir * 5, 0));
                    maverick.xDir *= -1;
                }
                else if (maverick is NeonTiger nt)
                {
                    nt.isDashing = true;
                    maverick.changeState(new NeonTPounceState());
                    maverick.vel.y = -100;
                    maverick.xDir *= -1;
                }
                return;
            }

            maverick.useGravity = false;
            maverick.vel.y = 0;

            if (stateTime > 0.15)
            {
                var dirHeld = wallDir == -1 ? input.isHeld(Control.Left, player) : input.isHeld(Control.Right, player);
                if (!dirHeld || Global.level.checkCollisionActor(maverick, wallDir, 0) == null)
                {
                    maverick.changeState(new MFall());
                }
                if (maverick is not NeonTiger)
                {
                    maverick.move(new Point(0, 100));
                }
                else
                {
                    maverick.stopMoving();
                }
            }

            if (maverick is not NeonTiger)
            {
                dustTime += Global.spf;
                if (stateTime > 0.2 && dustTime > 0.1)
                {
                    dustTime = 0;
                    new Anim(maverick.pos.addxy(maverick.xDir * 12, 0), "dust", maverick.xDir, null, true);
                }
            }
        }

        public MWallSlide cloneLeaveOff()
        {
            return new MWallSlide(wallDir, wall)
            {
                leftOff = true,
            };
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.stopMoving();
            if (leftOff)
            {
                maverick.frameIndex = maverick.sprite.frames.Count - 1;
                maverick.useGravity = false;
            }
        }

        public override void onExit(MaverickState newState)
        {
            maverick.useGravity = true;
            base.onExit(newState);
        }
    }

    public class MWallKick : MaverickState
    {
        public int kickDir;
        public float kickSpeed;
        public MWallKick(int kickDir, string overrideSprite = null) : base(overrideSprite ?? "wall_kick")
        {
            this.kickDir = kickDir;
            kickSpeed = kickDir * 150;
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
                if (!stopMove) maverick.move(new Point(kickSpeed, 0));
            }
            airCode();
            if (maverick.vel.y > 0)
            {
                maverick.changeState(new MFall());
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            new Anim(maverick.pos.addxy(12 * maverick.xDir, 0), "wall_sparks", maverick.xDir, null, true);
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
        }
    }

    // A generic shoot projectile state that any Maverick can use
    public class MShoot : MaverickState
    {
        bool shotOnce;
        string shootSound;
        public Action<Point, int> getProjectile;
        public int shootFramesHeld;
        bool shootReleased;
        public MShoot(Action<Point, int> getProjectile, string shootSound) : base("shoot", "")
        {
            this.getProjectile = getProjectile;
            this.shootSound = shootSound;
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (input.isHeld(Control.Shoot, player))
            {
                if (!shootReleased)
                {
                    shootFramesHeld++;
                }
            }
            else
            {
                shootReleased = true;
            }

            Point? shootPos = maverick.getFirstPOI();
            if (!shotOnce && shootPos != null)
            {
                shotOnce = true;
                if (!string.IsNullOrEmpty(shootSound)) maverick.playSound(shootSound, sendRpc: true);
                getProjectile(shootPos.Value, maverick.xDir);
            }

            if (consecutiveWaitTime > 0)
            {
                consecutiveWaitTime += Global.spf;
                if (consecutiveWaitTime >= consecutiveData.consecutiveDelay)
                {
                    consecutiveData.consecutiveCount++;
                    var newState = new MShoot(getProjectile, shootSound);
                    newState.consecutiveData = consecutiveData;
                    maverick.changeState(newState, ignoreCooldown: true);
                }
            }

            if (maverick.isAnimOver())
            {
                if (consecutiveData?.isOver() == false)
                {
                    if (consecutiveWaitTime == 0)
                    {
                        maverick.changeSpriteFromName("idle", true);
                        consecutiveWaitTime = Global.spf;
                    }
                }
                else
                {
                    maverick.changeState(new MIdle());
                }
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
        }
    }
}
