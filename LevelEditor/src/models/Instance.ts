import * as Helpers from "../helpers";
import * as DrawWrappers from "../drawWrappers";
import { Point } from "./Point";
import { Sprite } from "./Sprite";
import { Obj } from "./Obj";
import * as _ from "lodash";
import { Rect } from "./Rect";
import { Exclude, Expose, Transform, Type } from "class-transformer";
import { LevelEditorState } from "../levelEditor/levelEditor";
import { Shape } from "./Shape";
import { global } from "../Global";
import { Level } from "./Level";
import { NavMeshNode } from "./NavMeshNode";
import { MirrorEnabled } from "../enums";
import { Line } from "./Line";

const ICON_WIDTH = 20;

export class Instance {
  // Common
  @Expose() name: string;
  @Expose() properties: any;
  @Expose() objectName: string = "";
  @Expose() @Transform(({ value }) => value ?? MirrorEnabled.Both, { toClassOnly: true }) mirrorEnabled: MirrorEnabled;
  
  @Exclude() propertiesString: string = "";
  @Exclude() _id: number;
  @Exclude() hidden: boolean = false;

  // Object Instance
  @Expose() @Type(() => Point) pos: Point;
  // Shape Instance
  @Expose() @Type(() => Point) points: Point[];

  get id () {
    if (!this._id) {
      this._id = global.getNextSelectableId();
    }
    return this._id;
  }

  get isShape() {
    return this.points !== undefined;
  }

  constructor(name: string) {
    this.name = name;
    this.properties = {};
  }

  rename(newName: string, level: Level) {
    if (!newName) return;
    if (level.instances.some(i => i.name === newName)) return;
    this.name = newName;
  }

  clone() {
    let clonedInstance = _.cloneDeep(this);
    clonedInstance._id = global.getNextSelectableId();
    return clonedInstance;
  }

  shouldMirror(level: Level) {
    return this.pos && this.pos.x === level.mirrorX;
  }

  static CreateShapeInstance(name: string, properties: any, objectName: string, points: Point[]) {
    let instance = new Instance(name);
    instance.properties = properties || {};
    instance.objectName = objectName;
    instance.points = points;
    return instance;
  }

  static CreateObjectInstance(name: string, properties: any, objectName: string, pos: Point) {
    let instance = new Instance(name);
    instance.properties = properties || {};
    instance.objectName = objectName;
    instance.pos = pos;
    return instance;
  }

  get obj(): Obj {
    return global.getObjectByName(this.objectName);
  }
  
  getListItemColor(levelEditorState: LevelEditorState) {
    if (levelEditorState.selectedInstances.indexOf(this) !== -1) return "lightblue";
    if (this.isOutOfBounds(levelEditorState.selectedLevel)) return "red";
    if (this.hidden) return "lightgray";
    return undefined;
  }

  getNum(): number {
    let num = (this.name.match(/\d+$/) || []).pop();
    let numInt = parseInt(num);
    if (!numInt || isNaN(numInt)) {
      return 0;
    }
    return numInt;
  }

  // Must be called when updating mutating properties json
  onUpdatePropertiesJson() {
    this.propertiesString = this.getPropertiesJson() || "{}";
  }

  getPropertiesString() {
    if (!this.propertiesString) {
      this.propertiesString = this.getPropertiesJson() || "{}";
    }
    return this.propertiesString;
  }

  getPropertiesJson() {
    return JSON.stringify(this.properties, null, 1);
  }

  setPropertiesJson(json: string) {
    try {
      let parsedJson = JSON.parse(json);
      this.propertiesString = json;
      this.properties = parsedJson;
    }
    catch {
      console.error("Invalid properties json! ignoring...");
      this.propertiesString = this.getPropertiesJson();
    }
  }

  addProperty(name: string, value: any) {
    if (!this.properties) {
      this.properties = {};
    }
    this.properties[name] = value;
    this.propertiesString = this.getPropertiesJson();
  }

  removeProperty(name: string) {
    if (!this.properties) {
      this.properties = {};
    }
    delete this.properties[name];
    this.propertiesString = this.getPropertiesJson();
  }

  getArrowPoints(l1: number, l2: number, x1: number, y1: number, x2: number, y2: number, angle: number) {
    angle = angle * (Math.PI / 180);
    let x3 = x2 + (l2/l1) * (((x1 - x2) * Math.cos(angle)) + ((y1 - y2) * Math.sin(angle)));
    let y3 = y2 + (l2/l1) * (((y1 - y2) * Math.cos(angle)) - ((x1 - x2) * Math.sin(angle)));
    let x4 = x2 + (l2/l1) * (((x1 - x2) * Math.cos(angle)) - ((y1 - y2) * Math.sin(angle)));
    let y4 = y2 + (l2/l1) * (((y1 - y2) * Math.cos(angle)) + ((x1 - x2) * Math.sin(angle)));
    return [new Point(x3, y3), new Point(x4, y4)];
  }

  getMovingPlatformPoints(moveData: string) {
    let points: Point[] = [];
    let lines = moveData.split("\n");
    for (let line of lines) {
      let pieces = line.split(",");
      if (pieces.length === 3) {
        let x = Number(pieces[0]);
        let y = Number(pieces[1]);
        if (!isNaN(x) && !isNaN(y)) {
          points.push(new Point(this.pos.x + x, this.pos.y + y));
        }
      }
    }
    points.push(this.pos);
    return points;
  }

  draw(ctx: CanvasRenderingContext2D, data: LevelEditorState, viewPort: Rect) {
    if (this.isShape) {
      this.drawShapeInstance(ctx);
    }
    else if (this.objectName === "Map Sprite" || this.objectName === "Moving Platform") {
      this.drawMapSpriteInstance(ctx, viewPort);      
    }
    else {
      this.drawNonShapeInstance(ctx);
    }
    this.drawInstanceGizmos(ctx, data);
  }

  drawShapeInstance(ctx: CanvasRenderingContext2D) {
    DrawWrappers.drawPolygon(ctx, new Shape(this.points), true, this.obj.color, "", undefined, 0.5);
  }

  drawMapSpriteInstance(ctx: CanvasRenderingContext2D, viewPort: Rect) {
    let sprite = global.sprites.find(s => s.name === this.properties.spriteName);
    if (!sprite) return;
    
    let repeatX: number = this.properties?.repeatX ?? 1;
    let repeatXPadding: number = this.properties?.repeatXPadding ?? 0;
    let repeatY: number = this.properties?.repeatY ?? 1;
    let repeatYPadding: number = this.properties?.repeatYPadding ?? 0;
    
    let size = this.getRect();
    let xDir = this.properties.flipX ? -1 : 1;
    let yDir = this.properties.flipY ? -1 : 1;
    for (let i = 0; i < repeatY; i++) {
      for (let j = 0; j < repeatX; j++) {
        let x = this.pos.x + (j * xDir * (size.w + repeatXPadding));
        let y = this.pos.y + (i * yDir * (size.h + repeatYPadding));
        if (viewPort.inRect(x, y)) {
          sprite.draw(ctx, 0, x, y, xDir, yDir);
        }
      }
    }
  }

  drawNonShapeInstance(ctx: CanvasRenderingContext2D) {
    let xOff = 0;
    let yOff = 0;
    if (this.objectName.endsWith(" Flag")) {
      xOff = 10;
    }
    ctx.drawImage(
      this.obj.imgEl,
      Math.round(0), //source x
      Math.round(0), //source y
      Math.round(this.obj.imgEl.width), //source width
      Math.round(this.obj.imgEl.height), //source height
      Math.round(this.pos.x + xOff - ICON_WIDTH / 2),  //dest x
      Math.round(this.pos.y + yOff - ICON_WIDTH / 2),  //dest y
      Math.round(ICON_WIDTH), //dest width
      Math.round(ICON_WIDTH)  //dest height
    );
  }

  drawInstanceGizmos(ctx: CanvasRenderingContext2D, data: LevelEditorState) {
    let navMeshNode = this.properties as NavMeshNode;
    if (this.objectName === "Node" && navMeshNode?.neighbors) {
      for (let neighbor of navMeshNode.neighbors) {
        let node = _.find(data.selectedLevel.instances, (instance) => {
          return instance.name === neighbor.nodeName;
        });
        if (node) {
          let angle = this.pos.directionTo(node.pos).angle;
          let xOff = Helpers.cos(angle) * 10;
          let yOff = Helpers.sin(angle) * 10;
          let startPos = new Point(this.pos.x + xOff, this.pos.y + yOff); 
          let destPos = new Point(node.pos.x - xOff, node.pos.y - yOff);

          DrawWrappers.drawLine(ctx, startPos.x, startPos.y, destPos.x, destPos.y, "green", 2);
          let dist2 = startPos.distanceTo(destPos);
          let arrowPoints = this.getArrowPoints(dist2, 10, startPos.x, startPos.y, destPos.x, destPos.y, 30);
          DrawWrappers.drawLine(ctx, arrowPoints[0].x, arrowPoints[0].y, destPos.x, destPos.y, "green", 2);
          DrawWrappers.drawLine(ctx, arrowPoints[1].x, arrowPoints[1].y, destPos.x, destPos.y, "green", 2);
        }
      }
    }

    if (this.objectName == "Moving Platform") {
      let moveData: string = this.properties?.moveData ?? "";
      let points: Point[] = this.getMovingPlatformPoints(moveData);
      for (let i = 0; i < points.length - 1; i++) {
        DrawWrappers.drawLine(ctx, points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, "red", 2);
      }
    }
    
    if (data.showInstanceLabels && this.objectName === "Ladder") {
      let rect = this.getRect();
      let num = this.getNum();
      DrawWrappers.drawText(ctx, num === 0 ? "" : num.toString(), rect.x2, (rect.y1 + rect.y2)/2, "black", "white", 18);
    }

    if (data.showInstanceLabels && !this.isShape) {
      let num = this.getNum();
      DrawWrappers.drawText(ctx, num === 0 ? "" : num.toString(), this.pos.x, this.pos.y, "black", "white", 18);
    }
  }

  normalizePoints() {
    if (!this.isShape) {
      this.pos.x = Math.round(this.pos.x);
      this.pos.y = Math.round(this.pos.y);
    }
    else {
      let sumX = 0, sumY = 0;
      for (let point of this.points) {
        point.x = Math.round(point.x);
        point.y = Math.round(point.y);
        sumX += point.x;
        sumY += point.y;
      }
      let center = new Point(sumX / this.points.length, sumY / this.points.length);
      for (let point of this.points) {
        let p = center.directionTo(point);
        p.y *= -1;
        point.shapeAngle = Helpers.to360(p.angle);
        // console.log(point.shapeAngle);
      }
      this.points.sort((a, b) => {
        let aQuadRank = 5;
        if (a.shapeAngle < 180 && a.shapeAngle >= 90) aQuadRank = 1;
        else if (a.shapeAngle < 90 && a.shapeAngle >= 0) aQuadRank = 2;
        else if (a.shapeAngle < 360 && a.shapeAngle >= 270) aQuadRank = 3;
        else if (a.shapeAngle < 270 && a.shapeAngle >= 180) aQuadRank = 4;

        let bQuadRank = 5;
        if (b.shapeAngle < 180 && b.shapeAngle >= 90) bQuadRank = 1;
        else if (b.shapeAngle < 90 && b.shapeAngle >= 0) bQuadRank = 2;
        else if (b.shapeAngle < 360 && b.shapeAngle >= 270) bQuadRank = 3;
        else if (b.shapeAngle < 270 && b.shapeAngle >= 180) bQuadRank = 4;

        if (aQuadRank < bQuadRank) return -1;
        else if (aQuadRank > bQuadRank) return 1;
        else {
          return b.shapeAngle - a.shapeAngle;
        }
      });
    }
  }

  snapCollisionShape(instances: Instance[]) {
    if (this.objectName !== "Collision Shape") return;
    for (let other of instances) {
      if (other.id === this.id) continue;
      if (other.objectName !== "Collision Shape") return;
      
      for (let i = 0; i < this.points.length; i++) {
        for (let j = 0; j < other.points.length; j++) {
          let point = this.points[i];
          let otherPoint = other.points[j];
          let dist = point.distanceTo(otherPoint);
          if (dist < 5 && dist > 0) {
            let oldX = point.x;
            let oldY = point.y;
            point.x = otherPoint.x;
            point.y = otherPoint.y;
            for (let point2 of this.points) {
              if (point2.x === oldX) {
                point2.x = point.x;
              }
              if (point2.y === oldY) {
                point2.y = point.y;
              }
            }
          }
        }
      }
      
      let shape = new Shape(this.points);
      let otherShape = new Shape(other.points);
      let lines = shape.getLines();
      for (let line of lines) {
        let otherLines = otherShape.getLines();
        let concatLines: Line[] = [];
        for (let i = otherLines.length - 1; i >= 0; i--) {
          if (otherLines[i].getHorizontalOrVertical() === "angled") {
            concatLines = concatLines.concat(otherLines[i].getAngledLineRectLines());
            otherLines.splice(i, 1);
          }
        }
        otherLines = otherLines.concat(concatLines);
        for (let otherLine of otherLines) {
          line.snapTo(otherLine);
        }
      }
      for (let i = 0; i < lines.length; i++) {
        this.points[i].x = lines[i].x1;
        this.points[i].y = lines[i].y1;
      }
    }
  }

  getRect() {
    if (!this.isShape) {
      if (this.objectName.startsWith("Map Sprite")) {
        let spriteName = this.properties.spriteName;
        let sprite = global.sprites.find(s => s.name === spriteName);
        if (!sprite?.frames || sprite.frames.length === 0) {
          return new Rect(this.pos.x - ICON_WIDTH/2, this.pos.y - ICON_WIDTH/2, this.pos.x + ICON_WIDTH/2, this.pos.y + ICON_WIDTH/2);
        }
        let rect = sprite.frames[0].rect;
        let offset = sprite.getAlignOffset(sprite.frames[0]);
        return Rect.CreateWH(this.pos.x + offset.x, this.pos.y + offset.y, rect.w, rect.h);
      }
      else {
        return new Rect(this.pos.x - ICON_WIDTH/2, this.pos.y - ICON_WIDTH/2, this.pos.x + ICON_WIDTH/2, this.pos.y + ICON_WIDTH/2)
      }
    }
    else {
      var minX = _.minBy(this.points, (point) => { return point.x; }).x;
      var minY = _.minBy(this.points, (point) => { return point.y; }).y;
      var maxX = _.maxBy(this.points, (point) => { return point.x; }).x;
      var maxY = _.maxBy(this.points, (point) => { return point.y; }).y;
      return Rect.Create(new Point(minX, minY), new Point(maxX, maxY));
    }
  }

  isOutOfBounds(level: Level) {
    let rect = this.getRect();
    if ((rect.x1 < 0 && rect.x2 < 0) || (rect.y1 < 0 && rect.y2 < 0)) return true;
    if ((rect.x1 > level.width && rect.x2 > level.width) && (rect.y1 > level.height && rect.y2 > level.height)) return true;
    return false;
  }

  move(deltaX: number, deltaY: number) {
    if (!this.isShape) {
      this.pos.x += deltaX;
      this.pos.y += deltaY;
    }
    else {
      for(var point of this.points) {
        point.x += deltaX;
        point.y += deltaY;
      }
    }
  }

  clearPointPercents() {
    var rect = this.getRect();
    var selectionLeft = rect.x1;
    var selectionRight = rect.x2;
    var selectionTop = rect.y1;
    var selectionBottom = rect.y2;
    var selectionWidth = rect.w;
    var selectionHeight = rect.h;
    if (this.points) {
      for(var point of this.points) {
        point.perc_from_left = Math.abs(point.x - selectionLeft) / selectionWidth;
        point.perc_from_right = Math.abs(point.x - selectionRight) / selectionWidth;
        point.perc_from_top = Math.abs(point.y - selectionTop) / selectionHeight;
        point.perc_from_bottom = Math.abs(point.y - selectionBottom) / selectionHeight;
      }
    }
  }

  //resizeDir can be: ["nw-resize", "n-resize", "ne-resize", "e-resize", "se-resize", "s-resize", "sw-resize", "w-resize"];
  resize(deltaX: number, deltaY: number, resizeDir: string) {
    if (!this.isShape) return;
    
		for(let i = 0; i < this.points.length; i++) {

      var point = this.points[i];
			var perc_from_ox = 0;
			var perc_from_oy = 0;	

			if(resizeDir === "nw-resize") {
				perc_from_ox = this.points[i].perc_from_right;
				perc_from_oy = this.points[i].perc_from_bottom;
			}
			else if(resizeDir === "n-resize") {
				perc_from_oy = this.points[i].perc_from_bottom;
			}
			else if(resizeDir === "ne-resize") {
				perc_from_ox = this.points[i].perc_from_left;
				perc_from_oy = this.points[i].perc_from_bottom;
			}
			else if(resizeDir === "e-resize") {
				perc_from_ox = this.points[i].perc_from_left;
			}
			else if(resizeDir === "se-resize") {
				perc_from_ox = this.points[i].perc_from_left;
				perc_from_oy = this.points[i].perc_from_top;
			}
			else if(resizeDir === "s-resize") {
				perc_from_oy = this.points[i].perc_from_top;
			}
			else if(resizeDir === "sw-resize") {
				perc_from_ox = this.points[i].perc_from_right;
				perc_from_oy = this.points[i].perc_from_top;
			}
			else if(resizeDir === "w-resize") {
				perc_from_ox = this.points[i].perc_from_right;
			}

			this.points[i].x += (deltaX * perc_from_ox);
			this.points[i].y += (deltaY * perc_from_oy);
		}

  }
}