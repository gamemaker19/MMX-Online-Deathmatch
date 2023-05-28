import { Frame } from "./Frame";
import { Point } from "./Point";
import { Rect } from "./Rect";
import * as Helpers from "../helpers";
import * as DrawWrappers from "../drawWrappers";
import { Hitbox } from "./Hitbox";
import * as _ from "lodash";
import { Spritesheet } from "./Spritesheet";
import { global } from "../Global";
import { Exclude, Expose, Type } from "class-transformer";

export class Sprite {
  @Expose() name: string = "";
  @Expose() loopStartFrame: number = 0;
  @Expose() alignment: string = "center";
  @Expose() wrapMode: string = "once"; //Can be "once", "loop" or "pingpong"
  @Expose() spritesheetPath: string;
  @Expose() customMapName: string;
  @Expose() alignOffX: number;
  @Expose() alignOffY: number;
  @Expose() @Type(() => Hitbox) hitboxes: Hitbox[] = [];
  @Expose() @Type(() => Frame) frames: Frame[] = [];
  @Exclude() isDirty: boolean;
  
  constructor(name: string, spritesheetPath: string, customMapName: string) {
    this.name = name;
    this.customMapName = customMapName;
    this.spritesheetPath = Helpers.fileName(spritesheetPath);
  }
  
  getSpritesheet() : Spritesheet {
    return global.spritesheetMap[Helpers.getNormalizedSpritesheetName(this.customMapName, this.spritesheetPath)];
  }

  //Given the sprite's alignment, get the offset x and y on where to actually draw the sprite
  getAnchor(): Point {
    let x = 0, y = 0;
    if(this.alignment === "topleft") {
      x = 0; y = 0;
    }
    else if(this.alignment === "topmid") {
      x = 0.5; y = 0;
    }
    else if(this.alignment === "topright") {
      x = 1; y = 0;
    }
    else if(this.alignment === "midleft") {
      x = 0; y = 0.5;
    }
    else if(this.alignment === "center") {
      x = 0.5; y = 0.5;
    }
    else if(this.alignment === "midright") {
      x = 1; y = 0.5;
    }
    else if(this.alignment === "botleft") {
      x = 0; y = 1;
    }
    else if(this.alignment === "botmid") {
      x = 0.5; y = 1;
    }
    else if(this.alignment === "botright") {
      x = 1; y = 1;
    }
    return new Point(x, y);
  }

  draw(ctx: CanvasRenderingContext2D, frameIndex: number, x: number, y: number, flipX?: number, flipY?: number, options?: string, alpha?: number, scaleX?: number, scaleY?: number) {
    flipX = flipX || 1;
    flipY = flipY || 1;
    let frame = this.frames[frameIndex];
    let rect = frame.rect;
    let offset = this.getAlignOffset(frame, flipX, flipY);
    let imgEl = this.getSpritesheet().imgEl;
    if (imgEl) {
      DrawWrappers.drawImage(ctx, imgEl, rect.x1, rect.y1, rect.w, rect.h, x + offset.x + frame.offset.x, y + offset.y + frame.offset.y, flipX, flipY, options, alpha, scaleX, scaleY);
    }
  }

  drawFrame(ctx: CanvasRenderingContext2D, frame: Frame, x: number, y: number, flipX?: number, flipY?: number, options?: string, alpha?: number, scaleX?: number, scaleY?: number) {
    flipX = flipX || 1;
    flipY = flipY || 1;
    let rect = frame.rect;
    let offset = this.getAlignOffset(frame, flipX, flipY);
    let imgEl = this.getSpritesheet().imgEl;
    if (imgEl) {
      DrawWrappers.drawImage(ctx, imgEl, rect.x1, rect.y1, rect.w, rect.h, x + offset.x + frame.offset.x, y + offset.y + frame.offset.y, flipX, flipY, options, alpha, scaleX, scaleY);
    }
  }

  //Returns actual width and heights, not 0-1 number
  getAlignOffset(frame: Frame, flipX?: number, flipY?: number): Point {
    let rect = frame.rect;
    flipX = flipX || 1;
    flipY = flipY || 1;

    let w = rect.w;
    let h = rect.h;

    let halfW = w * 0.5;
    let halfH = h * 0.5;

    if(flipX > 0) halfW = Math.floor(halfW);
    else halfW = Math.ceil(halfW);
    if(flipY > 0) halfH = Math.floor(halfH);
    else halfH = Math.ceil(halfH);

    let x; let y;

    if(this.alignment === "topleft") {
      x = 0; y = 0;
    }
    else if(this.alignment === "topmid") {
      x = -halfW; y = 0;
    }
    else if(this.alignment === "topright") {
      x = -w; y = 0;
    }
    else if(this.alignment === "midleft") {
      x = flipX === -1 ? -w : 0; y = -halfH;
    }
    else if(this.alignment === "center") {
      x = -halfW; y = -halfH;
    }
    else if(this.alignment === "midright") {
      x = flipX === -1 ? 0 : -w; y = -halfH;
    }
    else if(this.alignment === "botleft") {
      x = 0; y = -h;
    }
    else if(this.alignment === "botmid") {
      x = -halfW; y = -h;
    }
    else if(this.alignment === "botright") {
      x = -w; y = -h;
    }
    else {
      throw "No alignment provided";
    }
    return new Point(x, y);
  }

  getParentFrames() {
    let frames = [];
    for(let frame of this.frames) {
      frames.push(frame);
    }
    return frames;
  }

  getChildFrames(parentFrameIndex: number) {
    let frames = [];
    for(let frame of this.frames) {
      frames.push(frame);
    }
    return frames;
  }

}