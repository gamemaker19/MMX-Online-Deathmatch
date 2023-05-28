using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class NodeTransition
    {
        public List<NodeTransitionPhase> phases;
        public int currentPhaseIndex;
        public bool completed;
        public bool failed;
        public NodeTransitionPhase currentPhase
        {
            get
            {
                return phases[currentPhaseIndex];
            }
        }

        public NodeTransition(List<NodeTransitionPhase> phases)
        {
            this.phases = phases;
        }

        public void update()
        {
            currentPhase.update();
            if (currentPhase.time > currentPhase.maxTimeBeforeAbort)
            {
                failed = true;
            }
            else if (currentPhase.exitCondition())
            {
                if (currentPhaseIndex < phases.Count - 1)
                {
                    currentPhaseIndex++;
                }
                else
                {
                    completed = true;
                }
            }
        }
    }

    public abstract class NodeTransitionPhase
    {
        public float maxTimeBeforeAbort = 2;
        public float time;
        public AI ai;
        public Player player;
        public Character character;
        public FindPlayer findPlayer;

        public NodeTransitionPhase(FindPlayer findPlayer)
        {
            ai = findPlayer.ai;
            player = findPlayer.player;
            character = player.character;
            this.findPlayer = findPlayer;
        }

        public virtual void update()
        {
            time += Global.spf;
        }

        // Condition under which to transition to the next phase
        public virtual bool exitCondition()
        {
            return true;
        }
    }

    public class EnterLadderNTP : NodeTransitionPhase
    {
        int yDir;

        public EnterLadderNTP(FindPlayer findPlayer, int yDir) : base(findPlayer)
        {
            this.yDir = yDir;
        }

        public override void update()
        {
            base.update();
            if (yDir == -1)
            {
                player.press(Control.Up);
                ai.doJump(1);
            }
            else
            {
                player.press(Control.Down);
            }
        }

        public override bool exitCondition()
        {
            return character?.charState is LadderClimb;
        }
    }

    public class ClimbLadderNTP : NodeTransitionPhase
    {
        int yDir;

        public ClimbLadderNTP(FindPlayer findPlayer, int yDir) : base(findPlayer)
        {
            this.yDir = yDir;
            maxTimeBeforeAbort = 30;
        }

        public override void update()
        {
            base.update();
            if (yDir == -1)
            {
                player.press(Control.Up);
            }
            else
            {
                player.press(Control.Down);
            }
        }

        public override bool exitCondition()
        {
            if (character == null) return false;
            if (findPlayer.nodePath.Count >= 1)
            {
                var nextNode = findPlayer.nextNode;
                if (nextNode.neighbors.Any(n => n.dropFromLadder))
                {
                    return true;
                }
            }
            return character.charState is not LadderClimb && !character.charState.inTransition();
        }
    }

    public class ClimbWallNTP : NodeTransitionPhase
    {
        int xDir;

        public ClimbWallNTP(FindPlayer findPlayer, int xDir) : base(findPlayer)
        {
            this.xDir = xDir;
            maxTimeBeforeAbort = 30;
        }

        public override void update()
        {
            base.update();
            if (character.pos.y > findPlayer.nextNode.pos.y)
            {
                ai.doJump();
            }
            if (xDir == -1)
            {
                player.press(Control.Left);
            }
            else
            {
                player.press(Control.Right);
            }
        }
    }

    public class JumpToPlatformNTP : NodeTransitionPhase
    {
        float platformJumpDir;
        int state = 0;
        int platformJumpDirDist;

        public JumpToPlatformNTP(FindPlayer findPlayer, float platformJumpDir, int platformJumpDirDist) : base(findPlayer)
        {
            this.platformJumpDir = platformJumpDir;
            this.platformJumpDirDist = platformJumpDirDist;
        }

        public override void update()
        {
            base.update();
            if (character == null) return;
            if (state == 0)
            {
                ai.doJump(1);
                if (platformJumpDir >= 1)
                {
                    var rightWall = Global.level.checkCollisionActor(character, platformJumpDirDist, 0);
                    if (rightWall != null && rightWall.gameObject is Wall)
                    {
                        if (ai.jumpTime > 0.5f)
                        {
                            state = 1;
                        }
                    }
                }
                else if (platformJumpDir <= -1)
                {
                    var leftWall = Global.level.checkCollisionActor(character, -platformJumpDirDist, 0);
                    if (leftWall != null && leftWall.gameObject is Wall)
                    {
                        if (ai.jumpTime > 0.5f)
                        {
                            state = 1;
                        }
                    }
                }
            }
            else if (state == 1)
            {
                if (!character.grounded) ai.doJump();
                if (platformJumpDir == -1) player.press(Control.Left);
                else if (platformJumpDir == 1) player.press(Control.Right);
            }
        }

        public override bool exitCondition()
        {
            return character.pos.y < findPlayer.nextNode.pos.y + 15 && character.grounded;
        }
    }

    public class DropFromLadderNTP : NodeTransitionPhase
    {
        public DropFromLadderNTP(FindPlayer findPlayer) : base(findPlayer)
        {
        }

        public override void update()
        {
            base.update();
            (character.charState as LadderClimb)?.dropFromLadder();
        }

        public override bool exitCondition()
        {
            return character.charState is not LadderClimb;
            
        }
    }

    public class JumpToMovingPlatformNTP : NodeTransitionPhase
    {
        int currentPlatformMoveDir;
        float xDistThreshold;
        public JumpToMovingPlatformNTP(FindPlayer findPlayer, float movingPlatXDist) : base(findPlayer)
        {
            xDistThreshold = movingPlatXDist;
        }

        public int yDir { get { return findPlayer.nextNode.pos.y < character.pos.y ? -1 : 1; } }

        public override void update()
        {
            base.update();
            NavMeshNode nextNode = findPlayer.nextNode;
            float distToNextNode = nextNode.pos.x - character.pos.x;
            float distThreshold;
            if (yDir == -1)
            {
                distThreshold = xDistThreshold;
            }
            else
            {
                distThreshold = (currentPlatformMoveDir == 0 ? xDistThreshold : xDistThreshold * 1.5f);
            }
            if (MathF.Abs(distToNextNode) < distThreshold)
            {
                ai.doJump(1);
                if (distToNextNode > 0 && currentPlatformMoveDir >= 0)
                {
                    player.press(Control.Right);
                }
                else if (distToNextNode < 0 && currentPlatformMoveDir <= 0)
                {
                    player.press(Control.Left);
                }
            }
            else
            {
                currentPlatformMoveDir = MathF.Sign(character.deltaPos.x);
            }
        }

        public override bool exitCondition()
        {
            if (!character.grounded) return false;
            if (findPlayer.nextNode.movingPlatform != null)
            {
                return character.checkCollision(0, 1)?.gameObject == findPlayer.nextNode.movingPlatform;
            }
            else
            {
                return character.checkCollision(0, 1)?.gameObject is not MovingPlatform;
            }
        }
    }

    public class DashNTP : NodeTransitionPhase
    {
        public DashNTP(FindPlayer findPlayer) : base(findPlayer)
        {
        }

        public override void update()
        {
            base.update();
            player.press(Control.Dash);
        }

        public override bool exitCondition()
        {
            return true;
        }
    }

}
