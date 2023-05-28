import * as Helpers from "../helpers";
import { Rect } from "./Rect";
import * as _ from "lodash";
import { Spritesheet } from "./Spritesheet";
import { Line } from "./Line";
import { Exclude, Expose, Transform, Type } from "class-transformer";
import { Instance } from "./Instance";
import { Point } from "./Point";
import { NavMeshNode } from "./NavMeshNode";
import { Parallax } from "./Parallax";
import { MirrorEnabled } from "../enums";
import { Obj } from "./Obj";
import { global } from "../Global";
import { Shape } from "./Shape";
// @ts-ignore
import * as PolyBool from "polybooljs";

function getMirroredName(name: string) {
  let matches = name.match(/\d+$/);
  let num = "";
  if (matches) {
    num = matches[0];
    name = name.substring(0, name.length - num.length);
  }
  return name + " Mirrored" + num;
}

function isMirroredName(name: string) {
  return name.includes(" Mirrored");
}

function getNonMirroredName(name: string) {
  return name.replace(" Mirrored", "");
}

export class Level {
  @Expose() name: string;
  @Expose() path: string;   // Only covers part after assets folder
  @Expose() backgroundPath: string;
  @Expose() backwallPath: string;
  @Expose() foregroundPath: string;
  @Expose() width: number = 0;
  @Expose() height: number = 0;
  @Expose() killY: number = undefined;
  @Expose() maxPlayers: number = undefined;
  @Expose() supportsLargeCam: boolean = false;
  @Expose() bgColorHex: string = undefined;
  @Expose() waterY: number = undefined;
  @Expose() @Transform(({ value }) => value ?? 0, { toClassOnly: true }) mirrorX: number;
  @Expose() @Transform(({ value }) => value ?? false, { toClassOnly: true }) mirroredOnly: boolean;
  @Expose() shortName: string;
  @Expose() displayName: string;
  @Expose() @Type(() => Instance) instances: Instance[] = [];
  @Expose() @Type(() => Parallax) @Transform(({ value }) => value ?? [], { toClassOnly: true }) parallaxes: Parallax[] = [];
  @Expose() mapSpritePaths: string[] = [];
  @Expose() mergedWalls: number[][][] = [];
  @Expose() supportsCTF: boolean;
  @Expose() supportsCP: boolean;
  @Expose() supportsKOTH: boolean;
  @Expose() supportsRace: boolean;
  @Expose() raceOnly: boolean;
  @Expose() customMapUrl: string;
  @Expose() defaultLargeCam: boolean;
  @Expose() mirrorMapImages: boolean = true;
  @Exclude() isDirty: boolean = false;
  @Expose() parallaxShader: string;
  @Expose() parallaxShaderImage: string;
  @Expose() backgroundShader: string;
  @Expose() backgroundShaderImage: string;
  @Expose() supportsVehicles: boolean;

  constructor(name: string, width: number, height: number) {
    this.name = name;
    this.width = width;
    this.height = height;
  }
  
  addInstance(instance: Instance) {
		let insertIndex = _.findIndex(this.instances, i => i.obj.zIndex > instance.obj.zIndex);
		if (insertIndex < 0) insertIndex = this.instances.length;
		this.instances.splice(insertIndex, 0, instance);
  }

  getNewInstanceName(obj: Obj) {
    let newInstanceNum = this.getNewInstanceNum(obj);
    let endStr = (newInstanceNum === undefined ? "" : newInstanceNum);
    return obj.name + endStr;
  }
  
  getNewInstanceNum(obj: Obj) {
    let existingInstances = _.filter(this.instances, instance => instance.objectName === obj.name);
    existingInstances.sort((a, b) => a.name.localeCompare(b.name, "en", {numeric: true}));
    if (existingInstances.length === 0) {
      return 1;
    }
  
    let lastNum = existingInstances[0].getNum();
    for (let i = 1; i < existingInstances.length; i++) {
      let curNum = existingInstances[i].getNum();
      if (curNum > lastNum + 1) {
        return lastNum + 1;
      }
      lastNum = curNum;
    }
    return lastNum + 1;
  }

  isMirrored() {
    return !!this.mirrorX;
  }

  isMirrorJson() {
    return this.path.includes("mirrored.json");
  }

  getFlippedX(x: number) {
    let halfWidth = this.width / 2;
    return halfWidth + (halfWidth - x);
  }

  flipMapX() {
    for (let instance of this.instances) {
      if (instance.points) {
        for (let point of instance.points) {
          point.x = this.getFlippedX(point.x);
        }
      }
      else if (instance.pos) {
        instance.pos.x = this.getFlippedX(instance.pos.x);
      }
    }
  }

  mirrorEnabledCheck(instance: Instance) {
    if (instance.mirrorEnabled === MirrorEnabled.NonMirroredOnly && this.isMirrorJson()) {
      return false;
    }
    if (instance.mirrorEnabled === MirrorEnabled.MirroredOnly && !this.isMirrorJson()) {
      return false;
    }
    return true;
  }

  // Validation
  getValidationErrors() {
    let errors = "";

    if (_.filter(this.instances, i => i.objectName === "Spawn Point").length < 4) {
      errors += "-Not enough neutral spawn points (minimum 4)\n";
    }
    if (this.width > 35000 || this.height > 35000) {
      errors += "-Map is too large (max 35000x35000)\n";
    }
    if (this.instances.some(i => i.objectName === "Map Sprite" && !i.properties.spriteName)) {
      errors += "-Some map sprites are missing sprites\n";
    }
    if ((this.name.endsWith("_md") || this.name.endsWith("_1v1")) && this.instances.some(i => i.objectName === "Red Flag" || i.objectName === "Blue Flag" || i.objectName === "Control Point")) {
      errors += "-Can't have flags or control points in 1v1 or medium maps\n";
    }
    if (this.instances.some(i => i.objectName === "Goal") && !this.instances.some(i => i.objectName === "Gate")) {
      errors += "-Maps with a goal must also have a gate\n";
    }
    if (this.instances.some(i => i.objectName === "Goal") && this.instances.filter(i => i.objectName === "Spawn Point" && i.properties?.raceStartSpawn === true).length < 10) {
      errors += "-Maps with a goal must also have 10 spawn points with Race Spawn property set\n";
    }
    /*
    if (this.instances.filter(i => i.objectName === "Goal").length > 1) {
      errors += "-Map may only have one goal\n";
    }
    */

    let movingPlatforms = this.instances.filter(i => i.objectName === "Moving Platform");
    for (let movingPlatform of movingPlatforms) {
      let error = this.getMovingPlatformError(movingPlatform);
      if (error) {
        errors += "Moving platform \"" + movingPlatform.name + "\" " + error;
        break;
      }
    }

    return errors;
  }

  getMovingPlatformError(movingPlatform: Instance) {
    if (!movingPlatform.properties.spriteName) {
      return "missing sprite name";
    }

    let moveSpeed: number = movingPlatform.properties.moveSpeed ?? 50;
    if (moveSpeed < 1) {
      return "move speed must be at least 1";
    }

    let timeOffset: number = movingPlatform.properties.timeOffset ?? 0;
    if (timeOffset < 0) {
      return "time offset must not be negative";
    }

    if (!movingPlatform.properties.moveData) {
      return "missing moveData";
    }

    let lines = movingPlatform.properties.moveData.split("\n");
    if (lines.length < 2) {
      return "must have at least 2 lines";
    }

    for (let i = 0; i < lines.length; i++) {
      let pieces = lines[i].split(",");
      if (pieces.length !== 3) {
        return "has misformatted moveData";
      }
      if (pieces.length === 3) {
        let x = Number(pieces[0]);
        let y = Number(pieces[1]);
        let t = Number(pieces[2]);
        
        if (isNaN(x) || isNaN(y) || isNaN(t) || t < 0) {
          return "has misformatted moveData";
        }

        if (i === 0 && !(x === 0 && y === 0)) {
          return "must have first line in moveData with coords 0,0";
        }
      }
    }
    return "";
  }

  onBeforeSave() {

    this.computeWallPaths();

    this.shortName = this.shortName || this.name.substring(0, 14);
    this.displayName = this.displayName || this.name.substring(0, 25);

    let redFlag: boolean;
    let blueFlag: boolean;
    let cp1: boolean;
    let cp2: boolean;
    let hill: boolean;
    let goal: boolean;
    let rideArmor: boolean;
    let rideChaser: boolean;

    for (let i = this.instances.length - 1; i >= 0; i--) {
      let instance = this.instances[i];
      let shouldRemove = false;
      if (instance.mirrorEnabled === MirrorEnabled.NonMirroredOnly && this.isMirrorJson()) shouldRemove = true;
      if (shouldRemove) {
        this.instances.splice(i, 1);
        continue;
      }

      instance.normalizePoints();
      
      let additionalCheck = this.mirrorEnabledCheck(instance);
      redFlag = redFlag || (instance.objectName === "Red Flag" && additionalCheck);
      blueFlag = blueFlag || (instance.objectName === "Blue Flag" && additionalCheck);
      cp1 = cp1 || (instance.objectName === "Control Point" && instance.properties?.num === 1 && additionalCheck);
      cp2 = cp2 || (instance.objectName === "Control Point" && instance.properties?.num === 2 && additionalCheck);
      hill = hill || (instance.objectName === "Control Point" && instance.properties?.hill === true && additionalCheck);
      goal = goal || (instance.objectName === "Goal" && additionalCheck);
      rideArmor = rideArmor || (instance.objectName === "Ride Armor" && additionalCheck);
      rideChaser = rideChaser || (instance.objectName === "Ride Chaser" && additionalCheck);
    }
    
    this.supportsCTF = (redFlag && blueFlag);
    this.supportsCP = (cp1 && cp2);
    this.supportsKOTH = hill;
    this.supportsRace = goal;
    this.supportsVehicles = (rideArmor || rideChaser);
  }

  getMirrored() {
    let clonedLevel = _.cloneDeep(this);
    let pathPieces = clonedLevel.path.split("/");
    pathPieces[pathPieces.length - 1] = "mirrored.json";
    clonedLevel.path = pathPieces.join("/");
    clonedLevel.width = clonedLevel.mirrorX * 2;
    clonedLevel.name += "_mirrored";

    // Filter instances
    clonedLevel.instances = clonedLevel.instances.filter((instance) => {
      if (instance.mirrorEnabled === MirrorEnabled.NonMirroredOnly) return false;
      if (instance.points) {
        if (_.every(instance.points, p => p.x >= clonedLevel.mirrorX)) return false;
        return true;
      }
      else {
        if (instance.pos.x > clonedLevel.mirrorX) {
          // Special case: you want to support CP in a mirrored map. As CP is an asymmetrical mode, control points on the right
          // side of mirror axis will not be deleted if flagged with MirroredOnly.
          if (instance.objectName === "Control Point" && instance.mirrorEnabled === MirrorEnabled.MirroredOnly) {
            return true;
          }
          if (instance.properties?.doNotMirror === true) {
            return true;
          }
          return false;
        }
        if (instance.objectName === "Red Spawn") return false;
        if (instance.objectName === "Red Flag") return false;
        return true;
      }
    });

    let instancesNotToMirror = new Set();

    // Apply changes to existing nodes
    for (let instance of clonedLevel.instances) {
      if (instance.properties?.doNotMirror === true) {
        instancesNotToMirror.add(instance);
      }
      if (instance.points) {
        if (Rect.isRectangle(instance.points)) {
          // If a rectangular shape instance goes beyond the mirror X axis, increase its width so it covers both sides equally.
          // Then flag it to not be mirrored as it would be redundant
          let minX = _.minBy(instance.points, point => point.x).x;
          for (let point of instance.points) {
            if (point.x > clonedLevel.mirrorX) {
              point.x = clonedLevel.mirrorX + (clonedLevel.mirrorX - minX);
              instancesNotToMirror.add(instance);
            }
          }
        }
        else {
          for (let point of instance.points) {
            if (point.x > clonedLevel.mirrorX) {
              point.x = clonedLevel.mirrorX;
            }
          }
        }
      }
      else {
        let navMeshNode = instance.properties as NavMeshNode;
        if (navMeshNode?.isRedFlagNode === true) {
          navMeshNode.isRedFlagNode = false;
        }
        // Control points should not be mirrored, as by design the mode is asymmetrical
        if (instance.objectName === "Control Point") {
          instancesNotToMirror.add(instance);
        }
        if (instance.properties?.raceStartSpawn === true) {
          instancesNotToMirror.add(instance);
        }
      }
    }

    // Generate cloned mirror instances
    let clonedInstances = [];
    for (let instance of clonedLevel.instances) {
      if (instancesNotToMirror.has(instance)) continue;

      let clonedInstance: Instance = instance.clone();
      clonedInstance.name = getMirroredName(clonedInstance.name);

      if (instance.points) {
        for (let i = 0; i < clonedInstance.points.length; i++) {
          clonedInstance.points[i] = new Point(clonedLevel.mirrorX + (clonedLevel.mirrorX - clonedInstance.points[i].x), clonedInstance.points[i].y);
        }

        clonedInstance.points = _.reverse(clonedInstance.points);

        // Cloned no scroll: invert the free direction
        if (clonedInstance.objectName === "No Scroll") {
          if (clonedInstance.properties?.freeDir === "right") {
            clonedInstance.properties.freeDir = "left";
          }
          else if (clonedInstance.properties?.freeDir === "left") {
            clonedInstance.properties.freeDir = "right";
          }
        }
      }
      else {
        if (instance.pos.x >= clonedLevel.mirrorX) continue;
        
        clonedInstance.pos = new Point(clonedLevel.mirrorX + (clonedLevel.mirrorX - instance.pos.x), instance.pos.y);
        
        // Cloned node
        let navMeshNode = clonedInstance.properties as NavMeshNode;
        if (navMeshNode) {
          if (navMeshNode.isBlueFlagNode) {
            navMeshNode.isBlueFlagNode = false;
            navMeshNode.isRedFlagNode = true;
          }
          else if (navMeshNode.isRedFlagNode) {
            navMeshNode.isBlueFlagNode = true;
            navMeshNode.isRedFlagNode = false;
          }
          for (let neighbor of navMeshNode.neighbors ?? []) {
            if (neighbor.platformJumpDir === "left") {
              neighbor.platformJumpDir = "right";
            }
            else if (neighbor.platformJumpDir === "right") {
              neighbor.platformJumpDir = "left";
            }

            if (neighbor.wallDir === "left") {
              neighbor.wallDir = "right";
            }
            else if (neighbor.wallDir === "right") {
              neighbor.wallDir = "left";
            }
          }
        }

        // Cloned flag: invert the flag color
        if (clonedInstance.objectName === "Red Flag") {
          clonedInstance.objectName = "Blue Flag";
        }
        else if (clonedInstance.objectName === "Blue Flag") {
          clonedInstance.objectName = "Red Flag";
        }

        // Cloned spawn point: invert the spawn team
        if (clonedInstance.objectName === "Red Spawn") {
          clonedInstance.objectName = "Blue Spawn";
        }
        else if (clonedInstance.objectName === "Blue Spawn") {
          clonedInstance.objectName = "Red Spawn";
        }

        // Cloned map sprite: flip x dir
        if (clonedInstance.objectName === "Map Sprite" || clonedInstance.objectName === "Ride Armor" || clonedInstance.objectName === "Ride Chaser") {
          if (clonedInstance.properties.flipX === undefined) {
            clonedInstance.properties.flipX = true;
          }
          else {
            clonedInstance.properties.flipX = !clonedInstance.properties.flipX;
          }
        }

        // Cloned moving platform
        if (clonedInstance.objectName === "Moving Platform") {
          let moveData: string = clonedInstance.properties.moveData;
          let newString = "";
          let lines = moveData.split("\n");
          for (let line of lines) {
            if (newString) newString += "\n";
            let pieces = line.split(",");
            if (pieces.length === 3) {
              let x = Number(pieces[0]);
              x *= -1;
              newString += String(x) + "," + pieces[1] + "," + pieces[2];
            }
          }
          clonedInstance.properties.moveData = newString;
          
          let nodeName: string = clonedInstance.properties.nodeName ?? "";
          if (nodeName) {
            nodeName = nodeName.replace("Node", "Node Mirrored");
            clonedInstance.properties.nodeName = nodeName;
          }
        }

        if (clonedInstance.properties.forceDir === "left") {
          clonedInstance.properties.forceDir = "right";
        }
        else if (clonedInstance.properties.forceDir === "right") {
          clonedInstance.properties.forceDir = "left";
        }
      }

      clonedInstances.push(clonedInstance);
    }

    clonedLevel.instances = clonedLevel.instances.concat(clonedInstances);

    _.remove(clonedLevel.instances, i => i.properties?.mirroredGoal === true && i.pos.x < this.mirrorX);

    // Nav mesh linking cleanup
    for (let instance of clonedLevel.instances) {
      let navMeshNode = instance.properties as NavMeshNode;
      if (!navMeshNode?.neighbors) continue;
      
      // Remove neighbor links that don't exist
      for (let i = navMeshNode.neighbors.length - 1; i >= 0; i--) {
        let neighbor = navMeshNode.neighbors[i];
         let nodeInstance = _.find(clonedLevel.instances, instance => instance.name === neighbor.nodeName);
         if (!nodeInstance) {
           navMeshNode.neighbors.splice(i, 1);
         }
      }

      // For all mirrored nodes, change their neighbors to mirrored ones EXCEPT connections to nodes on mirror axis
      if (isMirroredName(instance.name)) {
        for (let neighbor of navMeshNode.neighbors) {
          let nodeInstance = _.find(clonedLevel.instances, instance => instance.name === neighbor.nodeName);
          if (nodeInstance.pos.x !== clonedLevel.mirrorX) {
            neighbor.nodeName = getMirroredName(neighbor.nodeName);
          }
        }
      }

      // Node on mirror axis: duplicate neighbors for mirrored nodes as well
      if (instance.pos && instance.pos.x === clonedLevel.mirrorX) {
        let nodesToAdd = [];
        for (let neighbor of navMeshNode.neighbors) {
          let clonedNeighbor = _.cloneDeep(neighbor);
          clonedNeighbor.nodeName = getMirroredName(clonedNeighbor.nodeName);
          nodesToAdd.push(clonedNeighbor);
        }
        navMeshNode.neighbors = navMeshNode.neighbors.concat(nodesToAdd);
      }
      
      // Node that should connect to its "_mirrored" equivalent (a "border" node)
      if (navMeshNode.connectToSelfIfMirrored) {
        let nameMatch = isMirroredName(instance.name) ? getNonMirroredName(instance.name) : getMirroredName(instance.name);
        let match = _.find(clonedLevel.instances, i => i.name === nameMatch);
        if (match) {
          navMeshNode.neighbors.push({
            nodeName: nameMatch
          });
        }
      }
    }

    return clonedLevel;
  }

  computeWallPaths() {
    this.mergedWalls = [];
    let polygons: any[] = [];
    for (let instance of this.instances ?? []) {
      if (instance.isShape && instance.objectName === "Collision Shape" && !instance.properties?.unclimbable && !instance.properties?.topWall && !instance.properties?.boundary) {
        let shapeArray: number[][] = [];
        for (let point of instance.points) {
          shapeArray.push([point.x, point.y]);
        }
        polygons.push({
          regions: [ shapeArray ],
          inverted: false,
        });
      }
    }

    if (polygons.length === 0) return;

    var segments = PolyBool.segments(polygons[0]);
    for (var i = 1; i < polygons.length; i++) {
      var seg2 = PolyBool.segments(polygons[i]);
      var comb = PolyBool.combine(segments, seg2);
      segments = PolyBool.selectUnion(comb);
    }
    let result = PolyBool.polygon(segments);
    //console.log(result);

    for (let region of result.regions ?? []) {
      let points: number[][] = [];
      for (let regionPoint of region) {
        points.push([Number(regionPoint[0]), Number(regionPoint[1])]);
      }
      this.mergedWalls.push(points);
    }
  }
}