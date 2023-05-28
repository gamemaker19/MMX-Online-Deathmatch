import { Rect } from "./models/Rect";

export interface Selectable {
  move(deltaX: number, deltaY: number): void;
  resizeCenter(w: number, h: number): void;
  getRect(): Rect;
  selectableId: number;
}