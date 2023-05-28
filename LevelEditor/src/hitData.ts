import { Point } from "./models/Point";

export class HitData {
  normal: Point | undefined;
  hitPoint: Point | undefined;
  constructor(normal: Point | undefined, hitPoint: Point | undefined) {
    this.normal = normal;
    this.hitPoint = hitPoint;
  }
}