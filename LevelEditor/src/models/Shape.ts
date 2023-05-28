import { Point } from "./Point";
import { Line } from "./Line";
import { Rect } from "./Rect";
import { HitData } from "../hitData";
import { Type } from "class-transformer";
import _ from "lodash";

export class Shape {

  @Type(() => Point)
  points: Point[];
  @Type(() => Point)
  normals: Point[];

  constructor(points: Point[], normals?: Point[]) {
    this.points = points;
    let isNormalsSet = true;
    if(!normals) {
      normals = [];
      isNormalsSet = false;
    }
    for (let i = 0; i < this.points.length; i++) {
      let p1 = this.points[i];
      let p2 = (i == this.points.length - 1 ? this.points[0] : this.points[i + 1]);
      
      if(!isNormalsSet) {
        let v = new Point(p2.x - p1.x, p2.y - p1.y);
        normals.push(v.leftNormal().normalize());
      }
    }
    this.normals = normals;
  }

  //Called a lot
  getRect(): Rect | undefined {
    if(this.points.length !== 4) return undefined;
    if(this.points[0].x === this.points[3].x && this.points[1].x === this.points[2].x && this.points[0].y === this.points[1].y && this.points[2].y === this.points[3].y) {
      return Rect.Create(this.points[0], this.points[2]);
    }
    return undefined;
  }

  getBoundingRect(): Rect {
    let minX = _.minBy(this.points, p => p.x).x;
    let minY = _.minBy(this.points, p => p.y).y;
    let maxX = _.maxBy(this.points, p => p.x).x;
    let maxY = _.maxBy(this.points, p => p.y).y;
    return new Rect(minX, minY, maxX, maxY);
  }

  getLines(): Line[] {
    let lines: Line[] = [];
    for(let i = 0; i < this.points.length; i++) {
      let next = i+1;
      if(next >= this.points.length) next = 0;
      lines.push(new Line(this.points[i], this.points[next]));
    }
    return lines;
  }

  getNormals(): Point[] {
    return this.normals;
  }

  intersectsLine(line: Line) {
    let lines = this.getLines();
    for(let myLine of lines) {
      if(myLine.getIntersectPoint(line)) {
        return true;
      }
    }
    return false;
  }

  getLineIntersectCollisions(line: Line): HitData[] {
    let collideDatas = [];
    let lines = this.getLines();
    let normals = this.getNormals();
    for(let i = 0; i < lines.length; i++) {
      let myLine = lines[i];
      let point = myLine.getIntersectPoint(line);
      if(point) {
        let normal = normals[i];
        let collideData = new HitData(normal, point);
        collideDatas.push(collideData);
      }
    }
    return collideDatas;
  }

  static inShape(x: number, y: number, points: Point[]): boolean {
    let shape = new Shape(points);
    return shape.containsPoint(new Point(x, y));
  }

  //IMPORTANT NOTE: When determining normals, it is always off "other".
  intersectsShape(other: Shape, vel?: Point): HitData | undefined {
    let pointOutside = false;
    for(let point of this.points) {
      if(!other.containsPoint(point)) {
        pointOutside = true;
        break;
      }
    }
    let pointOutside2 = false;
    for(let point of other.points) {
      if(!this.containsPoint(point)) {
        pointOutside2 = true;
        break;
      }
    }
    if(!pointOutside || !pointOutside2) {
      //console.log("INSIDE");
      return new HitData(undefined, undefined);
    }
      
    let lines1 = this.getLines();
    let lines2 = other.getLines();
    let hitNormals = [];
    for(let line1 of lines1) {
      let normals = other.getNormals();
      for(let i = 0; i < lines2.length; i++) {
        let line2 = lines2[i];
        if(line1.getIntersectPoint(line2)) {
          if(!vel) {
            return new HitData(normals[i], undefined);
          }
          else {
            hitNormals.push(normals[i]);
          }
        }
      }
    }
    if(hitNormals.length === 0) {
      return undefined;
    }

    if (vel) {
      for(let normal of hitNormals) {
        let ang = vel.times(-1).angleWith(normal);
        if(ang < 90) {
          return new HitData(normal, undefined);
        }
      }
    }

    if(hitNormals.length > 0) {
      return new HitData(hitNormals[0], undefined);
    }

    return undefined;
  }

  containsPoint(point: Point): boolean {
    let x = point.x;
    let y = point.y;
    let vertices = this.points;
    // ray-casting algorithm based on
    // http://www.ecse.rpi.edu/Homepages/wrf/Research/Short_Notes/pnpoly.html
    let inside: boolean = false;
    for (let i:number = 0, j:number = vertices.length - 1; i < vertices.length; j = i++) {
        let xi:number = vertices[i].x, yi:number = vertices[i].y;
        let xj:number = vertices[j].x, yj:number = vertices[j].y;

        let intersect: boolean = ((yi > y) !== (yj > y))
            && (x < (xj - xi) * (y - yi) / (yj - yi) + xi);
        if (intersect) inside = !inside;
    }
    return inside;
  }


  getIntersectPoint(point: Point, dir: Point) {
    if(this.containsPoint(point)) {
      return point;
    }
    let intersections: Point[] = [];
    let pointLine = new Line(point, point.add(dir));
    for(let line of this.getLines()) {
      let intersectPoint = line.getIntersectPoint(pointLine);
      if(intersectPoint) {
        intersections.push(intersectPoint);
      }
    }
    if(intersections.length === 0) return undefined;
    
    //@ts-ignore
    return _.minBy(intersections, (intersectPoint) => {
      return intersectPoint.distanceTo(point);
    });

  }

  getClosestPointOnBounds(point: Point) {

  }

  // project vectors on to normal and return min/max value
  minMaxDotProd(normal: Point) {
    let min: number | undefined = undefined,
        max: number | undefined = undefined;
    for (let point of this.points) {
      let dp = point.dotProduct(normal);
      if (min === undefined || dp < min) min = dp;
      if (max === undefined || dp > max) max = dp;
    }
    return [min, max];
  }
  
  checkNormal(other: Shape, normal: Point) {
    let aMinMax = this.minMaxDotProd(normal);
    let bMinMax = other.minMaxDotProd(normal);

    if (aMinMax[0] === undefined || aMinMax[1] == undefined || bMinMax[0] == undefined || bMinMax[1] == undefined) {
      return;
    }

    //Containment
    let overlap = 0;
    if(aMinMax[0] > bMinMax[0] && aMinMax[1] < bMinMax[1]) {
      overlap = aMinMax[1] - aMinMax[0];
    }
    if(bMinMax[0] > aMinMax[0] && bMinMax[1] < aMinMax[1]) {
      overlap = bMinMax[1] - bMinMax[0];
    }
    if(overlap > 0) {
      let mins = Math.abs(aMinMax[0] - bMinMax[0]);
      let maxs = Math.abs(aMinMax[1] - bMinMax[1]);
      // NOTE: depending on which is smaller you may need to
      // negate the separating axis!!
      if (mins < maxs) {
        overlap += mins;
      } else {
        overlap += maxs;
      }
      let correction = normal.times(overlap);
      return correction;
    }

    if (aMinMax[0] <= bMinMax[1] && aMinMax[1] >= bMinMax[0]) {
      let correction = normal.times(bMinMax[1] - aMinMax[0]);
      return correction;
    }
    return undefined;
  }

  //Get the min trans vector to get this shape out of shape b.
  getMinTransVector(b: Shape/*, dir?: Point*/): Point | undefined {
    let correctionVectors = [];
    let thisNormals: Point[];
    let bNormals: Point[];
    let dir = undefined;
    if(dir) {
      thisNormals = [dir];
      bNormals = [dir];
    }
    else {
      thisNormals = this.getNormals();
      bNormals = b.getNormals();
    }
    for (let normal of thisNormals) {
      let result = this.checkNormal(b, normal);
      if (result) correctionVectors.push(result);
      //else return undefined;
    }
    for (let normal of bNormals) {
      let result = this.checkNormal(b, normal);
      if (result) correctionVectors.push(result);
      //else return undefined;
    }
    if (correctionVectors.length > 0) {
      //@ts-ignore
      return _.minBy(correctionVectors, (correctionVector) => {
        return correctionVector.magnitude;
      });
    }
    return undefined;
  }

  getMinTransVectorDir(b: Shape, dir: Point) {
    dir = dir.normalize();
    let mag = 0;
    let maxMag = 0;
    for(let point of this.points) {
      let line = new Line(point, point.add(dir.times(10000)));
      for(let bLine of b.getLines()) {
        let intersectPoint = bLine.getIntersectPoint(line);
        if(intersectPoint) {
          mag = point.distanceTo(intersectPoint);
          if(mag > maxMag) {
            maxMag = mag;
          }
        }
      }
    }
    for(let point of b.points) {
      let line = new Line(point, point.add(dir.times(-10000)));
      for(let myLine of this.getLines()) {
        let intersectPoint = myLine.getIntersectPoint(line);
        if(intersectPoint) {
          mag = point.distanceTo(intersectPoint);
          if(mag > maxMag) {
            maxMag = mag;
          }
        }
      }
    }
    if(maxMag === 0) {
      return undefined;
    }
    return dir.times(maxMag);
  }

  //Get the min trans vector to get this shape into shape b.
  getSnapVector(b: Shape, dir: Point) {
    let mag = 0;
    let minMag = Infinity;
    for(let point of this.points) {
      let line = new Line(point, point.add(dir.times(10000)));
      for(let bLine of b.getLines()) {
        let intersectPoint = bLine.getIntersectPoint(line);
        if(intersectPoint) {
          mag = point.distanceTo(intersectPoint);
          if(mag < minMag) {
            minMag = mag;
          }
        }
      }
    }
    if(mag === 0) {
      return undefined;
    }
    return dir.times(minMag);
  }

  clone(x: number, y: number) {
    let points: Point[] = [];
    for(let i = 0; i < this.points.length; i++) {
      let point = this.points[i];
      points.push(new Point(point.x + x, point.y + y));
    }
    return new Shape(points, this.normals);
  }

}
