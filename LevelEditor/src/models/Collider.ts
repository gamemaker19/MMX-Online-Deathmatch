import { Type } from "class-transformer";
import { Point } from "./Point";
import { Shape } from "./Shape";

export class Collider {

  isTrigger: boolean;
  wallOnly: boolean = false;
  isClimbable: boolean = true;
  isStatic: boolean = false;
  flag: number = 0;
  
  @Type(() => Point)
  offset: Point;
  @Type(() => Shape)
  _shape: Shape;

  constructor(points: Point[], isTrigger: boolean, isClimbable: boolean, isStatic: boolean, flag: number, offset: Point) {
    this._shape = new Shape(points);
    this.isTrigger = isTrigger;
    //this.gameObject = gameObject;
    this.isClimbable = isClimbable;
    this.isStatic = isStatic;
    this.flag = flag;
    this.offset = offset;
  }

  get shape() {
    let offset = new Point(0, 0);
    return this._shape.clone(offset.x, offset.y);
  }
}