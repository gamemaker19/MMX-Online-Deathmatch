import * as DrawWrappers from "../drawWrappers";
import { CanvasUI, KeyCode } from "../canvasUI";
import { SpriteEditor } from "./spriteEditor";
import { Rect } from "../models/Rect";
import { Frame } from "../models/Frame";
import { Point } from "../models/Point";
import { getPixelClumpRect } from "../pixelClump";

export class SpritesheetCanvas extends CanvasUI {

  spriteEditor: SpriteEditor;

  constructor(spriteEditor: SpriteEditor) {
    super("canvas2");
    this.spriteEditor = spriteEditor;
  }
  
  setSize(width: number, height: number) {
    this.canvas.width = width;
    this.canvas.height = height;
    this.baseWidth = width;
    this.baseHeight = height;
  }

  onLeftMouseDown() {
    if (!this.spriteEditor.data.selectedSprite) return;

    for (let i = 0; i < this.spriteEditor.data.selectedSprite.frames.length; i++) {
      let frame = this.spriteEditor.data.selectedSprite.frames[i];
      if (frame.rect.inRect(this.mouseX,this.mouseY)) {
        this.spriteEditor.selectFrame(i);
        return;
      }
    }

    if (this.spriteEditor.data.selectedSpritesheetPath === null) return;

    let rect = getPixelClumpRect(this.mouseX, this.mouseY, this.spriteEditor.selectedSpritesheet.imgArr);
    if (rect) {
      this.spriteEditor.data.pendingFrame = new Frame(rect as Rect, 0.066, new Point(0,0));
      this.spriteEditor.changeState();
    }

    this.spriteEditor.spriteCanvas?.redraw();
  }
  
  onLeftMouseUp() {
    let area = (Math.abs(this.dragBotY - this.dragTopY) * Math.abs(this.dragRightX - this.dragLeftX));
    if (area > 10) {
      this.spriteEditor.getSelectedPixels();
    }
  }
  
  onMouseMove() {
    if (this.mousedown) {
      this.redraw();
    }
  }
  
  onMouseLeave() {
    if (this.mousedown) {
      this.onLeftMouseUp();
      this.mousedown = false;
    }
    this.redraw();
  }
  
  onMouseWheel(e: WheelEvent) {
    if (this.isHeld(KeyCode.CONTROL)) {
      let delta = -(e.deltaY/180);
      this.zoom += delta;
      if (this.zoom < 1) this.zoom = 1;
      if (this.zoom > 5) this.zoom = 5;
      this.redraw();
      e.preventDefault();
      return false;
    }
  }

  onKeyDown(keyCode: KeyCode, firstFrame: boolean) {
    
    if (this.spriteEditor.data.pendingFrame || this.spriteEditor.data.selectedFrame) {
      if(keyCode === KeyCode.F) {
        this.spriteEditor.addPendingFrame();
      }
      else if(keyCode === KeyCode.R) {
        this.spriteEditor.addPendingFrame(this.spriteEditor.data.selectedFrameIndex);
      }
    }

    this.redraw();
  }

  redraw() {
    super.redraw();
    this.ctx.imageSmoothingEnabled = false;
    this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
    
    if(this.spriteEditor.selectedSpritesheet && this.spriteEditor.selectedSpritesheet.imgEl) {
      this.ctx.drawImage(this.spriteEditor.selectedSpritesheet.imgEl, 0, 0);
    }
  
    if (this.mousedown) {
      DrawWrappers.drawRect(this.ctx, new Rect(this.dragLeftX, this.dragTopY, this.dragRightX, this.dragBotY), "", "blue", 1);
    }
  
    if(this.spriteEditor.data.selectedSprite) {
      let i = 0;
      for(let frame of this.spriteEditor.data.selectedSprite.frames) {
        DrawWrappers.drawRect(this.ctx,frame.rect, "", "blue", 1);
        DrawWrappers.drawText(this.ctx,String(i),frame.rect.x1, frame.rect.y1, "red", "", 12, "left", "Top", "Arial");
        i++;
      }
    }
    
    if(this.spriteEditor.data.pendingFrame) {
      DrawWrappers.drawRect(this.ctx, this.spriteEditor.data.pendingFrame.rect, "", "green", 2);
    }
    else if(this.spriteEditor.data.selectedFrame) {
      DrawWrappers.drawRect(this.ctx, this.spriteEditor.data.selectedFrame.rect, "", "green", 2);
    }

  }

}