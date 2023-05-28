import { Rect } from "./Rect";
import { Point } from "./Point";
import { Hitbox } from "./Hitbox";
import { POI } from "./POI";
import { Exclude, Expose, Type } from "class-transformer";

let autoIncFrameId = 0;

export class Frame {
  @Expose() duration: number;
  @Expose() xDir: number = 1;
  @Expose() yDir: number = 1;
  @Expose() tags: string = "";
  @Exclude() frameId: number;
  @Expose() @Type(() => Hitbox) hitboxes: Hitbox[];
  @Expose() @Type(() => POI) POIs: POI[];
  @Expose() @Type(() => Rect) rect: Rect;
  @Expose() @Type(() => Point) offset: Point;

  constructor(rect: Rect, duration: number, offset: Point) {
    this.rect = rect;
    this.duration = duration;
    this.offset = offset;
    this.hitboxes = [];
    this.POIs = [];
    this.autoIncId();
  }

  getDurationInFrames() {
    return Math.round(this.duration * 60);
  }

  setDurationFromFrames(frames: number) {
    frames = Math.round(frames);
    if (frames < 1) frames = 1;
    let d = (frames / 60) - 0.001;
    this.duration = +(d.toFixed(3));
  }

  autoIncId() {
    this.frameId = autoIncFrameId++;
  }

}