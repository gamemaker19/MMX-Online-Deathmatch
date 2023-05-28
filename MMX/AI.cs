using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MMXOnline
{
    public enum AITrainingBehavior
    {
        Default,
        Idle,
        Attack,
        Jump,
        Crouch,
        Guard
    }

    public class AI
    {
        public Character character;
        public AIState aiState;
        public Actor target;
        public float shootTime;
        public float dashTime = 0;
        public float jumpTime = 0;
        public float weaponTime = 0;
        public float maxChargeTime = 0;
        public int framesChargeHeld = 0;
        public float jumpZoneTime = 0;
        public bool flagger = false; //Will this ai aggressively capture the flag?
        public static AITrainingBehavior trainingBehavior;
        public int axlAccuracy;
        public int mashType; //0=no mash, 1 = light, 2 = heavy

        public Player player { get { return character.player; } }

        public AI(Character character)
        {
            this.character = character;
            aiState = new AimAtPlayer(this.character);
            if (Global.level.flaggerCount < 2)
            {
                flagger = true;
                Global.level.flaggerCount++;
            }
            axlAccuracy = Helpers.randomRange(10, 30);
            mashType = Helpers.randomRange(0, 2);
            if (Global.level.isTraining()) mashType = 0;
        }

        public void doJump(float jumpTime = 0.75f)
        {
            if (this.jumpTime == 0)
            {
                //this.player.release(Control.Jump);
                player.press(Control.Jump);
                this.jumpTime = jumpTime;
            }
        }

        public RideChaser raceAiSetupRc;

        public RideChaser getRaceAIChaser()
        {
            var rideChasers = new List<RideChaser>();
            foreach (var go in Global.level.gameObjects)
            {
                if (go is RideChaser rc && !rc.destroyed && rc.character == null)
                {
                    rideChasers.Add(rc);
                }
            }
            rideChasers = rideChasers.OrderBy(rc => rc.pos.distanceTo(character.pos)).ToList();
            var rideChaser = rideChasers.FirstOrDefault();
            return rideChaser;
        }

        public void raceChaserAI()
        {
            if (character == null || character.charState is WarpIn)
            {
                return;
            }

            if (character.rideChaser == null)
            {
                var bestAIRideChaser = getRaceAIChaser();
                if (bestAIRideChaser != null)
                {
                    raceAiSetupRc = bestAIRideChaser;
                }
                else if (raceAiSetupRc != null && ((raceAiSetupRc.character != null && raceAiSetupRc.character != character) || raceAiSetupRc.destroyed))
                {
                    raceAiSetupRc = null;
                }
                
                if (raceAiSetupRc == null) return;

                bool movedLastFrame = false;
                if (character.pos.x - raceAiSetupRc.pos.x > 5)
                {
                    player.press(Control.Left);
                    movedLastFrame = true;
                }
                else if (character.pos.x - raceAiSetupRc.pos.x < -5)
                {
                    player.press(Control.Right);
                    movedLastFrame = true;
                }

                if (!movedLastFrame)
                {
                    player.press(Control.Jump);
                }
                else
                {
                    player.release(Control.Jump);
                }
            }
            else
            {
                bool shouldShoot = false;
                var hits = Global.level.raycastAll(character.getCenterPos(), character.getCenterPos().addxy(character.xDir * 100, 0), new List<Type>() { typeof(RideChaser), typeof(Character) });
                foreach (var hit in hits)
                {
                    if (hit?.gameObject is RideChaser rc)
                    {
                        if (rc.character != null && rc.character != character)
                        {
                            shouldShoot = true;
                            break;
                        }
                    }
                    else if (hit?.gameObject is Character hitChar)
                    {
                        if (hitChar != character)
                        {
                            shouldShoot = true;
                            break;
                        }
                    }
                }
                if (shouldShoot)
                {
                    player.press(Control.Shoot);
                }
                else
                {
                    player.release(Control.Shoot);
                }

                var brakeZones = Global.level.getTriggerList(character.abstractedActor(), 0, 0, null, typeof(BrakeZone));
                if ((Global.level.gameMode as Race).getPlace(character.player) > 1)
                {
                    dashTime = 100;
                }
                else
                {
                    dashTime = 0;
                }
                
                if ((dashTime > 0 || jumpTime > 0) && brakeZones.Count == 0)
                {
                    player.press(Control.Dash);
                    dashTime -= Global.spf;
                    if (dashTime < 0) dashTime = 0;
                }

                var turnZones = Global.level.getTriggerList(character.abstractedActor(), 0, 0, null, typeof(TurnZone));
                var turnZone = turnZones.FirstOrDefault()?.gameObject as TurnZone;
                if (turnZone != null && turnZone.xDir != character.xDir)
                {
                    if (turnZone.xDir == -1)
                    {
                        player.release(Control.Left);
                        player.press(Control.Left);
                    }
                    else
                    {
                        player.release(Control.Right);
                        player.press(Control.Right);
                    }
                }

                if (jumpTime == 0)
                {
                    var jumpZones = Global.level.getTriggerList(character.abstractedActor(), 0, 0, null, typeof(JumpZone));
                    int jumpTurnZoneCount = turnZones.Count(turnZone => turnZone.gameObject is TurnZone tz && tz.jumpAfterTurn && tz.xDir == character.xDir);

                    if (jumpZones.Count + jumpTurnZoneCount > 0 && character.rideChaser?.grounded == true)
                    {
                        jumpTime = (jumpZones.FirstOrDefault()?.gameObject as JumpZone)?.jumpTime ?? 0.5f;
                    }
                    else if (Helpers.randomRange(0, 300) < 1)
                    {
                        jumpTime = 0.5f;
                    }
                }
                else
                {
                    player.release(Control.Jump);
                    player.press(Control.Jump);
                    jumpTime -= Global.spf;
                    if (jumpTime <= 0)
                    {
                        jumpTime = 0;
                    }
                }
            }
        }
    
        public virtual void update()
        {
            if (Global.level.isRace() && Global.level.supportsRideChasers && Global.level.levelData.raceOnly)
            {
                raceChaserAI();
                return;
            }

            if (Global.debug || Global.level.isTraining())
            {
                if (trainingBehavior == AITrainingBehavior.Idle)
                {
                    player.release(Control.Shoot);
                    player.release(Control.Jump);
                    return;
                }
                if (trainingBehavior == AITrainingBehavior.Attack)
                {
                    player.release(Control.Jump);
                    player.press(Control.Shoot);
                    return;
                }
                if (trainingBehavior == AITrainingBehavior.Jump)
                {
                    player.release(Control.Shoot);
                    player.press(Control.Jump);
                    return;
                }
                if (trainingBehavior == AITrainingBehavior.Guard)
                {
                    player.press(Control.WeaponLeft);
                    return;
                }
                if (trainingBehavior == AITrainingBehavior.Crouch)
                {
                    if (player.isSigma)
                    {
                        character?.changeState(new SwordBlock(), true);
                        player.press(Control.Down);
                    }
                    else
                    {
                        player.press(Control.Down);
                    }
                    return;
                }
            }

            if (Global.level.gameMode.isOver) return;

            var gameMode = Global.level.gameMode;
            if (!player.isMainPlayer && player.isX && player.aiArmorUpgradeIndex < player.aiArmorUpgradeOrder.Count && !Global.level.is1v1())
            {
                var upgradeNumber = player.aiArmorUpgradeOrder[player.aiArmorUpgradeIndex];
                if (upgradeNumber == 0 && player.scrap >= Character.bootsArmorCost)
                {
                    UpgradeArmorMenu.upgradeBootsArmor(player, player.aiArmorPath);
                    player.aiArmorUpgradeIndex++;
                }
                else if (upgradeNumber == 1 && player.scrap >= Character.bodyArmorCost)
                {
                    UpgradeArmorMenu.upgradeBodyArmor(player, player.aiArmorPath);
                    player.aiArmorUpgradeIndex++;
                }
                else if (upgradeNumber == 2 && player.scrap >= Character.headArmorCost)
                {
                    UpgradeArmorMenu.upgradeHelmetArmor(player, player.aiArmorPath);
                    player.aiArmorUpgradeIndex++;
                }
                else if (upgradeNumber == 3 && player.scrap >= Character.armArmorCost)
                {
                    UpgradeArmorMenu.upgradeArmArmor(player, player.aiArmorPath);
                    player.aiArmorUpgradeIndex++;
                }
            }

            if (framesChargeHeld > 0)
            {
                if (character.chargeTime < maxChargeTime)
                {
                    //console.log("HOLD");
                    player.press(Control.Shoot);
                }
                else
                {
                    //this.player.release(control.Shoot.key);
                }
            }

            if (!Global.level.gameObjects.Contains(target))
            {
                target = null;
            }

            target = Global.level.getClosestTarget(character.pos, player.alliance, true, isRequesterAI: true);

            if (character.isHyperSigma)
            {
                int attack = Helpers.randomRange(0, 1);
                if (attack == 0)
                {
                    player.release(Control.Special1);
                    player.press(Control.Special1);
                }
                else if (attack == 1)
                {
                    player.release(Control.Shoot);
                    player.press(Control.Shoot);
                }
                if (Helpers.randomRange(0, 60) < 5)
                {
                    player.changeWeaponSlot(Helpers.randomRange(0, 2));
                }
                return;
            }

            if (aiState is not InJumpZone)
            {
                var jumpZones = Global.level.getTriggerList(character.abstractedActor(), 0, 0, null, typeof(JumpZone));
                var neighbor = (aiState as FindPlayer)?.neighbor;
                if (neighbor != null)
                {
                    jumpZones = jumpZones.FindAll(j => !neighbor.isJumpZoneExcluded(j.gameObject.name));
                }
                if (jumpZones.Count > 0)
                {
                    var jumpZone = jumpZones[0].gameObject as JumpZone;
                    var jumpZoneDir = jumpZone.forceDir != 0 ? jumpZone.forceDir : character.xDir;
                    if (jumpZoneDir == 0) jumpZoneDir = -1;

                    if (jumpZone.targetNode == null || jumpZone.targetNode == aiState.getNextNodeName())
                    {
                        if (aiState is not FindPlayer)
                        {
                            changeState(new InJumpZone(character, jumpZone, jumpZoneDir));
                        }
                        else
                        {
                            if (jumpZone.forceDir == -1)
                            {
                                player.press(Control.Left);
                            }
                            else if (jumpZone.forceDir == 1)
                            {
                                player.press(Control.Right);
                            }

                            if (character.charState is not LadderClimb)
                            {
                                doJump();
                                jumpZoneTime += Global.spf;
                                if (jumpZoneTime > 2 && character.player.isVile)
                                {
                                    jumpZoneTime = 0;
                                    player.press(Control.Up);
                                }
                            }
                        }
                    }
                    else
                    {
                    }
                }
                else
                {
                    jumpZoneTime = 0;
                }
            }

            if (character.flag != null)
            {
                target = null;
            }
            else if (Global.level.gameMode is CTF)
            {
                /*
                foreach (var player in Global.level.players)
                {
                    if (player.character != null && player.alliance != character.player.alliance && player.character.flag != null)
                    {
                        target = player.character;
                        break;
                    }
                }
                */
            }

            float stuckTime = (aiState as FindPlayer)?.stuckTime ?? 0;
            bool inNodeTransition = (aiState as FindPlayer)?.nodeTransition != null;

            if (aiState is not InJumpZone)
            {
                if (target == null)
                {
                    if (aiState is not FindPlayer)
                    {
                        changeState(new FindPlayer(character));
                    }
                }
                else
                {
                    if (aiState is FindPlayer)
                    {
                        changeState(new AimAtPlayer(character));
                    }
                }

                if (target != null)
                {
                    if (character.charState is LadderClimb)
                    {
                        doJump();
                    }
                    var xDist = target.pos.x - character.pos.x;
                    if (Math.Abs(xDist) > getMaxDist())
                    {
                        changeState(new MoveTowardsTarget(character));
                    }
                }
            }

            if (aiState.facePlayer && target != null)
            {
                if (character.pos.x > target.pos.x)
                {
                    if (character.xDir != -1)
                    {
                        player.press(Control.Left);
                    }
                }
                else
                {
                    if (character.xDir != 1)
                    {
                        player.press(Control.Right);
                    }
                }
                if (player.isAxl)
                {
                    player.axlCursorPos = target.pos
                        .addxy(-Global.level.camX, -Global.level.camY)
                        .addxy(Helpers.randomRange(-axlAccuracy, axlAccuracy), Helpers.randomRange(-axlAccuracy, axlAccuracy));
                }
            }
            if (aiState.shouldAttack && target != null)
            {
                if (shootTime == 0)
                {
                    bool isTargetInAir = target.pos.y < character.pos.y - 50;
                    if (target is Character chr && chr.player.isKaiserNonViralSigma()) isTargetInAir = true;

                    if (character.isFacing(target))
                    {
                        if (framesChargeHeld > 0)
                        {
                            if (character.chargeTime >= maxChargeTime)
                            {
                                player.release(Control.Shoot);
                                framesChargeHeld = 0;
                            }
                        }
                        else
                        {
                            if (player.isZero && character.charState is not LadderClimb)
                            {
                                int attack = Helpers.randomRange(0, 10);
                                if (isTargetInAir) attack = 1;

                                if (attack == 0) player.press(Control.Special1);
                                else if (attack == 1)
                                {
                                    player.press(Control.Special1);
                                    player.press(Control.Up);
                                }
                                else if (attack == 2)
                                {
                                    if (!character.grounded || player.zeroGigaAttackWeapon.ammo >= 16)
                                    {
                                        player.press(Control.Special1);
                                        player.press(Control.Down);
                                    }
                                    else
                                    {
                                        player.press(Control.Shoot);
                                    }
                                }
                                else
                                {
                                    player.press(Control.Shoot);
                                }
                            }
                            if (player.isSigma && player.currentMaverick != null)
                            {
                                if (isTargetInAir && player.maverick1v1 != null)
                                {
                                    doJump(1);
                                }
                                else
                                {
                                    int attack = Helpers.randomRange(0, 1);
                                    if (attack == 0) player.press(Control.Special1);
                                    else if (attack == 1) player.press(Control.Shoot);
                                }
                            }
                            else
                            {
                                if (isTargetInAir && (player.isVile || player.isSigma))
                                {
                                    if (player.isVile)
                                    {
                                        int cannonSlot = player.weapons.FindIndex(c => c is VileCannon);
                                        if (cannonSlot != -1)
                                        {
                                            player.changeWeaponSlot(cannonSlot);
                                        }
                                        player.press(Control.Up);
                                        player.press(Control.Shoot);
                                    }
                                    else if (player.isSigma1AndSigma())
                                    {
                                        if (character.grounded) player.press(Control.Special1);
                                        else player.press(Control.Shoot);
                                    }
                                    else if (player.isSigma2AndSigma())
                                    {
                                        player.press(Control.Shoot);
                                        player.press(Control.Up);
                                    }
                                    else if (player.isSigma3AndSigma())
                                    {
                                        player.press(Control.Special1);
                                    }
                                }
                                else
                                {
                                    player.press(Control.Shoot);
                                }
                            }
                        }
                    }
                }
                shootTime += Global.spf;
                if (shootTime > 0.1)
                {
                    shootTime = 0;
                }
            }
            if (aiState.shouldDodge)
            {
                foreach (var proj in Global.level.gameObjects)
                {
                    if (proj is Projectile && proj is not BusterProj)
                    {
                        var projProj = proj as Projectile;
                        if (projProj.isFacing(character) && character.withinX(projProj, 100) && character.withinY(projProj, 30) && projProj.damager.owner.alliance != player.alliance)
                        {
                            doJump();
                        }
                    }
                }
            }
            if (aiState.randomlyChargeWeapon && player.isX && framesChargeHeld == 0 && player.character.canCharge())
            {
                if (Helpers.randomRange(0, 300) < 1)
                {
                    if (player.weapon is Buster)
                    {
                        maxChargeTime = Helpers.randomRange(0.75f, 3);
                    }
                    else
                    {
                        maxChargeTime = 3.5f;
                    }
                    framesChargeHeld = 1;
                    player.press(Control.Shoot);
                }
            }
            if (aiState.randomlyChangeState)
            {
                if (Helpers.randomRange(0, 60) < 5)
                {
                    var randAmount = Helpers.randomRange(-100, 100);
                    changeState(new MoveToPos(character, character.pos.addxy(randAmount, 0)));
                    return;
                }
            }
            if (aiState.randomlyDash && !(character.charState is WallKick) && !inNodeTransition && stuckTime == 0)
            {
                if (Helpers.randomRange(0, 150) < 5)
                {
                    dashTime = Helpers.randomRange(0.2f, 0.5f);
                }
                if (dashTime > 0)
                {
                    player.press(Control.Dash);
                    dashTime -= Global.spf;
                    if (dashTime < 0) dashTime = 0;
                }
            }
            if (aiState.randomlyJump && !inNodeTransition && stuckTime == 0)
            {
                int max = player.isX ? 150 : 600;
                if (Helpers.randomRange(0, max) < 5)
                {
                    jumpTime = Helpers.randomRange(0.25f, 0.75f);
                }
            }
            if (aiState.randomlyChangeWeapon && (player.isX || player.isAxl || player.isVile) && !player.lockWeapon && !character.isInvisibleBS.getValue() && character.chargedRollingShieldProj == null)
            {
                weaponTime += Global.spf;
                if (weaponTime > 5)
                {
                    weaponTime = 0;
                    var wasBuster = (player.weapon is Buster);
                    player.changeWeaponSlot(getRandomWeaponIndex());
                    if (wasBuster && maxChargeTime > 0)
                    {
                        maxChargeTime = 3.5f;
                    }
                }
            }
            if (player.weapon != null && player.weapon.ammo <= 0 && !(player.weapon is Buster))
            {
                player.changeWeaponSlot(getRandomWeaponIndex());
            }

            aiState.update();

            if (jumpTime > 0)
            {
                jumpTime -= Global.spf;
                if (jumpTime < 0)
                {
                    jumpTime = 0;
                }
            }
        }

        public int getRandomWeaponIndex()
        {
            if (player.weapons.Count == 0) return 0;
            var weapons = player.weapons.FindAll(w => !(w is DoubleBullet) && !(w is DNACore)).ToList();
            return weapons.IndexOf(weapons.GetRandomItem());
        }

        public void changeState(AIState newState, bool forceChange = false)
        {
            if (aiState is FindPlayer && newState is not FindPlayer && character.flag != null)
            {
                return;
            }
            if (flagger && aiState is FindPlayer && newState is not FindPlayer && Global.level.gameMode is CTF)
            {
                return;
            }
            if (aiState is FindPlayer && newState is not FindPlayer && Global.level.gameMode is Race)
            {
                return;
            }
            if (forceChange || newState.canChangeTo())
            {
                aiState = newState;
            }
        }

        public float getMaxDist()
        {
            var maxDist = Global.screenW / 2;
            if (player.isZero || player.isSigma) return 70;
            int? raNum = player.character?.rideArmor?.raNum;
            if (raNum != null && raNum != 2) maxDist = 35;
            return maxDist;
        }
    }

    public class AIState
    {
        public bool facePlayer;
        public Character character;
        public bool shouldAttack;
        public bool shouldDodge;
        public bool randomlyChangeState;
        public bool randomlyDash;
        public bool randomlyJump;
        public bool randomlyChangeWeapon;
        public bool randomlyChargeWeapon;

        public Player player
        {
            get
            {
                return character.player;
            }
        }

        public AI ai
        {
            get
            {
                if (player.character != null)
                {
                    return player.character.ai;
                }
                else if (player.limboChar != null)
                {
                    return player.limboChar.ai;
                }
                else
                {
                    return new AI(character);
                }
            }
        }

        public Actor target
        {
            get
            {
                return ai?.target;
            }
        }

        public string getPrevNodeName()
        {
            if (this is FindPlayer)
            {
                return (this as FindPlayer).prevNode?.name;
            }
            return "";
        }

        public string getNextNodeName()
        {
            if (this is FindPlayer)
            {
                return (this as FindPlayer).nextNode?.name;
            }
            return "";
        }

        public string getDestNodeName()
        {
            if (this is FindPlayer)
            {
                return (this as FindPlayer).destNode?.name;
            }
            return "";
        }

        public bool canChangeTo()
        {
            return character.charState is not LadderClimb && character.charState is not LadderEnd;
        }

        public AIState(Character character)
        {
            this.character = character;
            shouldAttack = true;
            facePlayer = true;
            shouldDodge = true;
            randomlyChangeState = true;
            randomlyDash = true;
            randomlyJump = true;
            randomlyChangeWeapon = true;
            randomlyChargeWeapon = true;
        }

        public virtual void update()
        {
            if (character.charState is LadderClimb && this is not FindPlayer)
            {
                player.press(Control.Down);
                player.press(Control.Jump);
            }
        }
    }

    public class MoveTowardsTarget : AIState
    {
        public MoveTowardsTarget(Character character) : base(character)
        {
            facePlayer = false;
            shouldAttack = false;
            shouldDodge = false;
            randomlyChangeState = false;
            randomlyDash = true;
            randomlyJump = false;
            randomlyChangeWeapon = false;
            randomlyChargeWeapon = true;
        }

        public override void update()
        {
            base.update();
            if (ai.target == null)
            {
                ai.changeState(new FindPlayer(character));
                return;
            }

            if (character.pos.x - ai.target.pos.x > ai.getMaxDist())
            {
                player.press(Control.Left);
            }
            else if (character.pos.x - ai.target.pos.x < -ai.getMaxDist())
            {
                player.press(Control.Right);
            }
            else
            {
                ai.changeState(new AimAtPlayer(character));
            }
        }
    }

    public class FindPlayer : AIState
    {
        public NavMeshNode destNode;
        public NavMeshNode nextNode;
        public NavMeshNode prevNode;
        public NavMeshNeighbor neighbor;
        public NodeTransition nodeTransition;
        public List<NavMeshNode> nodePath;
        public float stuckTime;
        public float lastX;
        public float runIntoWallTime;
        public FindPlayer(Character character) : base(character)
        {
            facePlayer = false;
            shouldAttack = false;
            shouldDodge = false;
            randomlyChangeState = false;
            randomlyDash = true;
            randomlyJump = false;
            randomlyChangeWeapon = false;
            randomlyChargeWeapon = true;

            setDestNodePos();
        }

        public override void update()
        {
            base.update();

            if (nextNode == null)
            {
                ai.changeState(new FindPlayer(character));
                return;
            }

            if (nodeTransition != null)
            {
                nodeTransition.update();
                if (nodeTransition.failed)
                {
                    ai.changeState(new FindPlayer(character));
                    return;
                }
                else if (!nodeTransition.completed)
                {
                    return;
                }
            }

            float xDist = character.pos.x - nextNode.pos.x;
            if (MathF.Abs(xDist) > 2.5f)
            {
                if (xDist < 0)
                {
                    player.press(Control.Right);
                }
                else if (xDist > 0)
                {
                    player.press(Control.Left);
                }
                if (character.pos.x == lastX && character.grounded)
                {
                    runIntoWallTime += Global.spf;
                    if (runIntoWallTime > 2)
                    {
                        setDestNodePos();
                    }
                }
                lastX = character.pos.x;
            }
            else
            {
                // States where it's possible to move to the next node. As more special situations are added this may need to grow
                bool isValidTransitionState = character.grounded || neighbor?.isDestNodeInAir == true || character.charState is LadderClimb;

                if (Math.Abs(character.abstractedActor().pos.y - nextNode.pos.y) < 30 && isValidTransitionState)
                {
                    goToNextNode();
                }
                else
                {
                    stuckTime += Global.spf;
                    if (stuckTime > 2)
                    {
                        setDestNodePos();
                    }
                }
            }
        }
        public void goToNextNode()
        {
            if (nextNode == destNode)
            {
                setDestNodePos();
            }
            else
            {
                prevNode = nextNode;
                nextNode = nodePath.PopFirst();
            }

            neighbor = prevNode?.getNeighbor(nextNode);
            if (neighbor != null)
            {
                var phases = neighbor.getNodeTransitionPhases(this);
                if (phases.Count > 0)
                {
                    nodeTransition = new NodeTransition(phases);
                }
                else
                {
                    nodeTransition = null;
                }
            }
        }

        public void setDestNodePos()
        {
            runIntoWallTime = 0;
            stuckTime = 0;
            if (Global.level.gameMode is Race)
            {
                destNode = Global.level.goalNode;
            }
            else if (Global.level.gameMode is CTF)
            {
                if (character.flag == null)
                {
                    Flag targetFlag = null;
                    if (player.alliance == GameMode.redAlliance) targetFlag = Global.level.blueFlag;
                    else if (player.alliance == GameMode.blueAlliance) targetFlag = Global.level.redFlag;
                    destNode = Global.level.getClosestNodeInSight(targetFlag.pos);
                    if (destNode == null)
                    {
                        destNode = Global.level.getRandomNode();
                    }
                }
                else
                {
                    if (player.alliance == GameMode.blueAlliance) destNode = Global.level.blueFlagNode;
                    else if (player.alliance == GameMode.redAlliance) destNode = Global.level.redFlagNode;
                }
            }
            else if (Global.level.gameMode is ControlPoints)
            {
                var cp = Global.level.getCurrentControlPoint();
                if (cp == null)
                {
                    destNode = Global.level.getRandomNode();
                }
                else
                {
                    destNode = cp.navMeshNode;
                }
            }
            else if (Global.level.gameMode is KingOfTheHill)
            {
                var cp = Global.level.hill;
                destNode = cp.navMeshNode;
            }
            else
            {
                destNode = Global.level.getRandomNode();
            }
            if (Global.level.navMeshNodes.Count == 2)
            {
                nextNode = destNode;
            }
            else
            {
                nextNode = Global.level.getClosestNodeInSight(character.getCenterPos());
            }
            prevNode = null;

            if (nextNode != null)
            {
                nodePath = nextNode.getNodePath(destNode);
                nodePath.Remove(nextNode);
            }
        }
    }

    public class MoveToPos : AIState
    {
        public Point dest;
        public MoveToPos(Character character, Point dest) : base(character)
        {
            this.dest = dest;
            facePlayer = false;
            randomlyChangeState = false;
            randomlyChargeWeapon = true;
        }

        public override void update()
        {
            base.update();
            var dir = 0;
            if (character.pos.x - dest.x > 5)
            {
                dir = -1;
                player.press(Control.Left);
            }
            else if (character.pos.x - dest.x < -5)
            {
                dir = 1;
                player.press(Control.Right);
            }
            else
            {
                ai.changeState(new AimAtPlayer(character));
            }

            if (character.sweepTest(new Point(dir * 5, 0)) != null)
            {
                ai.changeState(new AimAtPlayer(character));
            }
        }
    }

    public class AimAtPlayer : AIState
    {
        public float jumpDelay = 0;
        public AimAtPlayer(Character character) : base(character)
        {
        }

        public override void update() 
        {
            base.update();
            if (character.grounded && jumpDelay > 0.3)
            {
                jumpDelay = 0;
            }

            if (target != null && character.pos.y > target.pos.y && character.pos.y < target.pos.y + 80)
            {
                jumpDelay += Global.spf;
                if (jumpDelay > 0.3)
                {
                    ai.doJump();
                }
            }
            else
            {
                //this.changeState(new JumpToWall());
            }
        }
    }

    public class InJumpZone : AIState
    {
        public JumpZone jumpZone;
        public int jumpZoneDir;
        public float time = 0.25f;

        public InJumpZone(Character character, JumpZone jumpZone, int jumpZoneDir) : base(character)
        {
            this.jumpZone = jumpZone;
            this.jumpZoneDir = jumpZoneDir;
            facePlayer = false;
            shouldAttack = false;
            shouldDodge = false;
            randomlyChangeState = false;
            randomlyDash = true;
            randomlyJump = false;
            randomlyChangeWeapon = false;
            randomlyChargeWeapon = true;
        }

        public override void update() 
        {
            base.update();
            time += Global.spf;
            ai.doJump();
            ai.jumpZoneTime += Global.spf;

            if (jumpZoneDir == -1)
            {
                player.press(Control.Left);
            }
            else if (jumpZoneDir == 1)
            {
                player.press(Control.Right);
            }

            //Check if out of zone
            if (character != null && character.abstractedActor().collider != null)
            {
                if (!character.abstractedActor().collider.isCollidingWith(jumpZone.collider))
                {
                    ai.changeState(new FindPlayer(character));
                }
            }
        }
    }
}
