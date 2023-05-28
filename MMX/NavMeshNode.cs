using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MMXOnline
{
    public class NavMeshNode
    {
        public string name;
        public Point pos;
        public dynamic neighborJson;
        public List<NavMeshNeighbor> neighbors = new List<NavMeshNeighbor>();
        public MovingPlatform movingPlatform;
        public bool isMovingPlatform { get { return movingPlatform != null; } }

        public NavMeshNode(string name, Point pos, dynamic neighborJson)
        {
            this.name = name;
            this.pos = pos;
            this.neighborJson = neighborJson;
        }

        public void setNeighbors(List<NavMeshNode> nodeList, List<GameObject> gameObjects)
        {
            var properties = neighborJson;
            if (properties?.neighbors == null) return;

            foreach (var jsonNeighbor in properties.neighbors)
            {
                var node = nodeList.Where((iterNode) => 
                {
                    return iterNode.name == (string)jsonNeighbor.nodeName;
                }).FirstOrDefault();

                var ladder = gameObjects.Where((gameobject) => 
                {
                    return (gameobject is Ladder) && gameobject.name == (string)jsonNeighbor.ladderName;
                }).FirstOrDefault();

                var navMeshNeighbor = new NavMeshNeighbor(this, node, jsonNeighbor);
                neighbors.Add(navMeshNeighbor);
            }
        }

        public NavMeshNeighbor getNeighbor(NavMeshNode neighborNode)
        {
            var node = neighbors.Where((neighbor) =>
            {
                return neighbor.neighborNode == neighborNode;
            }).FirstOrDefault();
            return node;
        }

        public List<NavMeshNode> getNodePath(NavMeshNode destNode)
        {
            if (this == destNode)
            {
                return new List<NavMeshNode>() { destNode };
            }
            var found = false;
            var pathToNode = new List<NavMeshNode>();
            var foundPath = new List<NavMeshNode>();

            var visited = new HashSet<NavMeshNode>();

            getNextNodeDfs(this, destNode, ref found, ref pathToNode, ref foundPath, ref visited);
            if (foundPath.Count > 0)
            {
                return foundPath;
            }
            return new List<NavMeshNode>() { destNode };
        }

        void getNextNodeDfs(NavMeshNode curNode, NavMeshNode destNode, ref bool found, ref List<NavMeshNode> pathToNode, ref List<NavMeshNode> foundPath, ref HashSet<NavMeshNode> visited)
        {
            if (found) return;
            if (visited.Contains(curNode)) return;
            visited.Add(curNode);

            if (!found && curNode == destNode)
            {
                found = true;
                foundPath = new List<NavMeshNode>(pathToNode);
                return;
            }

            var neighbors = curNode.neighbors.Shuffle();
            foreach (var neighbor in neighbors)
            {
                pathToNode.Add(neighbor.neighborNode);
                getNextNodeDfs(neighbor.neighborNode, destNode, ref found, ref pathToNode, ref foundPath, ref visited);
                pathToNode.Pop();
            }
        }
    }

    public class NavMeshNeighbor
    {
        public NavMeshNode baseNode;
        public NavMeshNode neighborNode;
        public int ladderDir;
        public int wallDir;
        public int platformJumpDir;
        public int platformJumpDirDist;
        public bool dropFromLadder;
        public string includeJumpZones;
        public bool isDestNodeInAir;
        public bool dash;
        public float movingPlatXDist;

        public NavMeshNeighbor(NavMeshNode baseNode, NavMeshNode neighborNode, dynamic neighborJson)
        {
            this.baseNode = baseNode;
            this.neighborNode = neighborNode;
            ladderDir = Helpers.convertDynamicToDir(neighborJson.ladderDir);
            wallDir = Helpers.convertDynamicToDir(neighborJson.wallDir);
            platformJumpDir = Helpers.convertDynamicToDir(neighborJson.platformJumpDir);
            platformJumpDirDist = neighborJson.platformJumpDirDist ?? 30;
            dropFromLadder = neighborJson.dropFromLadder ?? false;
            includeJumpZones = neighborJson.includeJumpZones ?? null;
            isDestNodeInAir = neighborJson.isDestNodeInAir ?? false;
            movingPlatXDist = neighborJson.movingPlatXDist ?? 60;
            dash = neighborJson.dash ?? false;
        }

        public bool isJumpZoneExcluded(string jumpZoneName)
        {
            // Empty string means exclude all. null means include all
            if (includeJumpZones == null) return false;
            if (includeJumpZones == "") return true;

            var includeJumpZonePieces = includeJumpZones.Split(',');
            foreach (var includeJumpZonePiece in includeJumpZonePieces)
            {
                if (jumpZoneName.EndsWith(includeJumpZonePiece)) return false;
            }
            return true;
        }

        public List<NodeTransitionPhase> getNodeTransitionPhases(FindPlayer findPlayer)
        {
            var phases = new List<NodeTransitionPhase>();
            if (findPlayer?.player == null || findPlayer.player.character == null)
            {
                return phases;
            }

            if (baseNode.isMovingPlatform || neighborNode.isMovingPlatform)
            {
                phases.Add(new JumpToMovingPlatformNTP(findPlayer, movingPlatXDist));
            }
            else if (ladderDir != 0)
            {
                phases.Add(new EnterLadderNTP(findPlayer, ladderDir));
                phases.Add(new ClimbLadderNTP(findPlayer, ladderDir));
            }
            else if (wallDir != 0)
            {
                phases.Add(new ClimbWallNTP(findPlayer, wallDir));
            }
            else if (platformJumpDir != 0)
            {
                phases.Add(new JumpToPlatformNTP(findPlayer, platformJumpDir, platformJumpDirDist));
            }
            else if (dropFromLadder)
            {
                phases.Add(new DropFromLadderNTP(findPlayer));
            }
            else if (dash)
            {
                phases.Add(new DashNTP(findPlayer));
            }

            return phases;
        }
    }
}
