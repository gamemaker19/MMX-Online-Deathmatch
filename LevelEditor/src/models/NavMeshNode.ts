export interface NavMeshNode {
  neighbors: NavMeshNeighbor[];
  isRedFlagNode: boolean;
  isBlueFlagNode: boolean;
  controlPointNodeNum: number;
  connectToSelfIfMirrored: boolean;
}

export function addNeighbor (navMeshNode: NavMeshNode, neighborName: string) {
  if (!navMeshNode.neighbors.some(n => n.nodeName === neighborName)) {
    navMeshNode.neighbors.push({
      nodeName: neighborName,
    });
  }
}

export interface NavMeshNeighbor {
  nodeName: string;
  ladderDir?: string;
  wallDir?: string;
  platformJumpDir?: string;
  platformJumpDirDist?: number;
  dropFromLadder?: boolean;
  includeJumpZones?: string;
  movingPlatXDist?: number;
  isDestNodeInAir?: boolean;
  dash?: boolean;
}