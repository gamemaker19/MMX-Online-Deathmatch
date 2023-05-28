import { Point } from "./Point";
import { runInThisContext } from "vm";
import { Selectable } from "../selectable";
import { Rect } from "./Rect";
import { Exclude, Expose } from "class-transformer";
import { global } from "../Global";

export class POI implements Selectable {
  @Expose() tags: string;
  @Expose() x: number;
  @Expose() y: number;
  @Exclude() selectableId: number;
  @Exclude() isHidden: boolean;

  constructor(tags: string, x: number, y: number) {
    this.tags = tags;
    this.x = x;
    this.y = y;
    this.selectableId = global.getNextSelectableId();
  }
  move(deltaX: number, deltaY: number): void {
    this.x += deltaX;
    this.y += deltaY;
  }
  resizeCenter(w: number, h: number): void {
  }
  getRect(): Rect {
    return new Rect(this.x - 2, this.y - 2, this.x + 2, this.y + 2);
  }
}