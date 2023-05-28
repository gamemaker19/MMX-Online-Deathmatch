import { Exclude, Expose } from "class-transformer";
import * as Helpers from "../helpers";

export class Point {
  @Expose() x: number;
  @Expose() y: number;

  @Exclude() perc_from_left: number = 0;
  @Exclude() perc_from_right: number = 0;
  @Exclude() perc_from_top: number = 0;
  @Exclude() perc_from_bottom: number = 0;

  @Exclude() shapeAngle: number = 0;

  constructor(x: number, y: number) {
    this.x = x;
    this.y = y;
  }
  get ix() {
    return Math.round(this.x);
  }
  get iy() {
    return Math.round(this.y);
  }

  addxy(x: number, y: number) {
    let point = new Point(this.x + x, this.y + y);
    return point;
  }

  //Avoid calls to this, if called a lot may be bottleneck
  normalize() {
    this.x = Helpers.roundEpsilon(this.x);
    this.y = Helpers.roundEpsilon(this.y);
    if(this.x === 0 && this.y === 0) return new Point(0, 0);
    let mag = this.magnitude;
    let point = new Point(this.x / mag, this.y / mag);
    if(isNaN(point.x) || isNaN(point.y)) 
    {
      throw "NAN!";
    }
    point.x = Helpers.roundEpsilon(point.x);
    point.y = Helpers.roundEpsilon(point.y);
    return point;
  }

  dotProduct(other: Point) {
    return (this.x * other.x) + (this.y * other.y);
  }

  project(other: Point) {
    let dp = this.dotProduct(other);
    return new Point((dp / (other.x * other.x + other.y * other.y)) * other.x, (dp / (other.x * other.x + other.y * other.y)) * other.y);
  }

  rightNormal() {
    return new Point(-this.y, this.x);
  }

  leftNormal() {
    return new Point(this.y, -this.x);
  }

  perProduct(other: Point) {
    return this.dotProduct(other.rightNormal());
  }

  //Returns new point
  add(other: Point) {
    let point = new Point(this.x + other.x, this.y + other.y);
    return point;
  }
  
  //Mutates this point
  inc(other: Point): void {
    this.x += other.x;
    this.y += other.y;
  }
  
  //Returns new point
  times(num: number) {
    let point = new Point(this.x * num, this.y * num);
    return point;
  }

  //Mutates this point
  multiply(num: number) {
    this.x *= num;
    this.y *= num;
    return this;
  }

  unitInc(num: number) {
    return this.add(this.normalize().times(num));
  }

  get angle() {
    let ang = Math.atan2(this.y, this.x);
    ang *= 180/Math.PI;
    if(ang < 0) ang += 360;
    return ang;
  }

  angleWith(other: Point) {
    let ang = Math.atan2(other.y, other.x) - Math.atan2(this.y, this.x);
    ang *= 180/Math.PI;
    if(ang < 0) ang += 360;
    if(ang > 180) ang = 360 - ang;
    return ang;
  }

  get magnitude() {
    let root = this.x * this.x + this.y * this.y;
    if(root < 0) root = 0;
    let result = Math.sqrt(root);
    if(isNaN(result)) throw "NAN!";
    return result;
  }
  clone() {
    return new Point(this.x, this.y);
  }
  distanceTo(other: Point) {
    return Math.sqrt(Math.pow(other.x - this.x, 2) + Math.pow(other.y - this.y, 2));
  }
  subtract(other: Point) {
    return new Point(this.x - other.x, this.y - other.y);
  }
  directionTo(other: Point) {
    return new Point(other.x - this.x, other.y - this.y);
  }
  directionToNorm(other: Point) {
    return (new Point(other.x - this.x, other.y - this.y)).normalize();
  }
  isAngled() {
    return this.x !== 0 && this.y !== 0;
  }
  equals(other: Point) {
    return this.x === other.x && this.y === other.y;
  }
  
  static getInclinePushDir(inclineNormal: Point, pushDir: Point) {
    let bisectingPoint = inclineNormal.normalize().add(pushDir.normalize());
    bisectingPoint = bisectingPoint.normalize();
    //Snap to the nearest axis
    if(Math.abs(bisectingPoint.x) >= Math.abs(bisectingPoint.y)) {
      bisectingPoint.y = 0;
    }
    else {
      bisectingPoint.x = 0;
    }
    return bisectingPoint.normalize();
  }
}