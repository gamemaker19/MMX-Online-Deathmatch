import { Point } from "./Point";
import { Rect } from "./Rect";
import { Selectable } from "../selectable";
import { Exclude, Expose, Type } from "class-transformer";
import { global } from "../Global";

export class Hitbox implements Selectable {
  @Expose() width: number;
  @Expose() height: number;
  @Expose() flag: number;
  @Expose() name: string;
  @Expose() isTrigger: boolean = false;
  @Expose() @Type(() => Point) offset: Point;
  @Exclude() selectableId: number;

  constructor() {
    this.name = "";
    this.width = 20;
    this.height = 40;
    this.offset = new Point(0,0);
    this.selectableId = global.getNextSelectableId();
  }

  move(deltaX: number, deltaY: number) {
    this.offset.x += deltaX;
    this.offset.y += deltaY;
  }

  resizeCenter(w: number, h: number) {
    this.width += w;
    this.height += h;
  }

  getRect() {
    return new Rect(this.offset.x, this.offset.y, this.offset.x + this.width, this.offset.y + this.height);
  }

}