import { Expose, Type } from "class-transformer";
import { Point } from "./Point";
import { Shape } from "./Shape";

export class Rect {

  @Expose() @Type(() => Point) topLeftPoint: Point;
  @Expose() @Type(() => Point) botRightPoint: Point;

  static Create(topLeftPoint: Point, botRightPoint: Point) {
    return new Rect(topLeftPoint.x, topLeftPoint.y, botRightPoint.x, botRightPoint.y);
  }

  static CreateWH(x: number, y: number, w: number, h: number) {
    return new Rect(x, y, x + w, y + h);
  }

  static CreateFromStringKey(key: string) {
    let pieces = key.split('_');
    return new Rect(Number(pieces[0]), Number(pieces[1]), Number(pieces[2]), Number(pieces[3]));
  }

  constructor(x1: number, y1: number, x2: number, y2: number) {    
    this.topLeftPoint = new Point(x1, y1);
    this.botRightPoint = new Point(x2, y2);
  }

  inRect(x: number, y: number) {
    let rx:number = this.x1;
    let ry:number = this.y1;
    let rx2:number = this.x2;
    let ry2:number = this.y2;
    return x >= rx && x <= rx2 && y >= ry && y <= ry2;
  }

  getShape(): Shape {
    return new Shape([this.topLeftPoint, new Point(this.x2, this.y1), this.botRightPoint, new Point(this.x1, this.y2)]);
  }

  get midX(): number {
    return (this.topLeftPoint.x + this.botRightPoint.x) * 0.5;
  }
  get x1(): number {
    return this.topLeftPoint.x;
  }
  get y1(): number {
    return this.topLeftPoint.y;
  }
  get x2(): number {
    return this.botRightPoint.x;
  }
  get y2(): number {
    return this.botRightPoint.y;
  }

  get w(): number {
    return this.botRightPoint.x - this.topLeftPoint.x;
  }

  get h(): number {
    return this.botRightPoint.y - this.topLeftPoint.y;
  }

  get area(): number {
    return this.w * this.h;
  }

  getPoints(): Point[] {
    return [
      new Point(this.topLeftPoint.x, this.topLeftPoint.y),
      new Point(this.botRightPoint.x, this.topLeftPoint.y),
      new Point(this.botRightPoint.x, this.botRightPoint.y),
      new Point(this.topLeftPoint.x, this.botRightPoint.y),
    ];
  }

  overlaps(other: Rect) {
    // If one rectangle is on left side of other
    if (this.x1 > other.x2 || other.x1 > this.x2)
      return false;
    // If one rectangle is above other
    if (this.y1 > other.y2 || other.y1 > this.y2)
      return false;
    return true;
  }

  equals(other: Rect) {
    return this.x1 === other.x1 && this.x2 === other.x2 && this.y1 === other.y1 && this.y2 === other.y2;
  }

  clone(x: number, y: number) {
    return new Rect(this.x1 + x, this.y1 + y, this.x2 + x, this.y2 + y);
  }
  
  toString() {
    return this.x1 + "_" + this.y1 + "_" + this.x2 + "_" + this.y2;
  }

  static isRectangle(points: Point[]): boolean {
    let xVals = new Set<Number>();
    let yVals = new Set<Number>();
    for (let point of points) {
      xVals.add(point.x);
      yVals.add(point.y);
    }
    return xVals.size === 2 && yVals.size === 2;
  }

}