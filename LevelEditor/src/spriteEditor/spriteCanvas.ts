import * as Helpers from "../helpers";
import * as DrawWrappers from "../drawWrappers";
import { CanvasUI, KeyCode, MouseButton } from "../canvasUI";
import { Ghost, SpriteEditor } from "./spriteEditor";
import { Rect } from "../models/Rect";
import { Frame } from "../models/Frame";
import { global } from "../Global";
import { Sprite } from "../models/Sprite";
import { Hitbox } from "../models/Hitbox";
import { Point } from "../models/Point";
import { POI } from "../models/POI";

export class SpriteCanvas extends CanvasUI {

  spriteEditor: SpriteEditor;
  lastTopRectPoint: Point | undefined;

  constructor(spriteEditor: SpriteEditor) {
    super("canvas1", "lightgray");
    this.isNoScrollZoom = true;
    this.zoom = 5;
    this.spriteEditor = spriteEditor;
  }

  setSize(width: number, height: number) {
    this.canvas.width = width;
    this.canvas.height = height;
    this.baseWidth = width;
    this.baseHeight = height;
  }

  redraw() {
    let state = this.spriteEditor.data;
    super.redraw();
    if (!state.selectedSprite) return;
    let frame: Frame;

    if (!state.isAnimPlaying) {
      if (state.selectedFrame && state.selectedSpritesheetPath && this.spriteEditor.selectedSpritesheet.imgEl) {
        frame = state.selectedFrame;
      }
      else {
        return;
      }
    }
    else {
      frame = state.selectedSprite.frames[this.spriteEditor.animFrameIndex];
    }

    let cX = this.canvas.width/2;
    let cY = this.canvas.height/2;

    let drewVileCannon = [];
    for (let poi of frame.POIs) {
      if (poi.isHidden) {
        drewVileCannon.push(false);
        continue;
      }
      let x = cX + (state.selectedSprite.alignOffX || 0);
      let y = cY + (state.selectedSprite.alignOffY || 0);
      drewVileCannon.push(this.drawVileCannon(poi, x, y, true));
    }

    let frameIndex = state.selectedSprite.frames.indexOf(frame);

    if (frameIndex < 0) {
      state.selectedSprite.drawFrame(this.ctx, frame, cX, cY, frame.xDir, frame.yDir);
    }
    else {
      state.selectedSprite.draw(this.ctx, frameIndex, cX, cY, frame.xDir, frame.yDir);
    }

    if (state.ghost) {
      state.ghost.sprite.draw(this.ctx, state.ghost.sprite.frames.indexOf(state.ghost.frame), cX, cY, frame.xDir, frame.yDir, "", 0.5);  
    }

    if (!state.hideGizmos) {
      for (let hitbox of this.spriteEditor.getVisibleHitboxes()) {

        let hx: number = 0; let hy: number = 0;
        let halfW = hitbox.width * 0.5;
        let halfH = hitbox.height * 0.5;
        let w = halfW * 2;
        let h = halfH * 2;
        if(state.selectedSprite.alignment === "topleft") {
          hx = cX; hy = cY;
        }
        else if(state.selectedSprite.alignment === "topmid") {
          hx = cX - halfW; hy = cY;
        }
        else if(state.selectedSprite.alignment === "topright") {
          hx = cX - w; hy = cY;
        }
        else if(state.selectedSprite.alignment === "midleft") {
          hx = cX; hy = cY - halfH;
        }
        else if(state.selectedSprite.alignment === "center") {
          hx = cX - halfW; hy = cY - halfH;
        }
        else if(state.selectedSprite.alignment === "midright") {
          hx = cX - w; hy = cY - halfH;
        }
        else if(state.selectedSprite.alignment === "botleft") {
          hx = cX; hy = cY - h;
        }
        else if(state.selectedSprite.alignment === "botmid") {
          hx = cX - halfW; hy = cY - h;
        }
        else if(state.selectedSprite.alignment === "botright") {
          hx = cX - w; hy = cY - h;
        }

        let offsetRect = new Rect(
          hx + hitbox.offset.x, hy + hitbox.offset.y, hx + hitbox.width + hitbox.offset.x, hy + hitbox.height + hitbox.offset.y
        );

        let hitboxColor = "red";
        if (hitbox.flag === 1) hitboxColor = "blue";
        if (hitbox.flag === 2) hitboxColor = "purple";
        if (hitbox.flag === 3) hitboxColor = "yellow";

        DrawWrappers.drawRect(this.ctx, offsetRect, hitboxColor, undefined, undefined, 0.25);
        if (state.selection === hitbox) {
          DrawWrappers.drawRect(this.ctx, offsetRect, undefined, "green", 2 / this.zoom, 1);
        }
      }
      
      let len = 1000;
      DrawWrappers.drawLine(this.ctx, cX, cY - len, cX, cY + len, "red", 1);
      DrawWrappers.drawLine(this.ctx, cX - len, cY, cX + len, cY, "red", 1);
      DrawWrappers.drawCircle(this.ctx, cX, cY, 1, "red");
      //drawStroked(c1, "+", cX, cY);
      
      for (let i = 0; i < frame.POIs.length; i++) {
        let poi = frame.POIs[i];
        if (poi.isHidden) continue;
        let x = cX + (state.selectedSprite.alignOffX || 0);
        let y = cY + (state.selectedSprite.alignOffY || 0);
        if (!drewVileCannon[i] && !this.drawVileCannon(poi, x, y, false)) {
          DrawWrappers.drawCircle(this.ctx, poi.x + x, poi.y + y, 1, "green");
        }
      }

      if (state.selectedSprite.alignOffX !== 0 || state.selectedSprite.alignOffY !== 0) {
        DrawWrappers.drawCircle(this.ctx, cX + state.selectedSprite.alignOffX, cY + state.selectedSprite.alignOffY, 1, "blue");
      }

      /*
      if (state.addRectMode > 0 && state.addRectPoint1Raw) {
        let rect = new Rect(state.addRectPoint1Raw.x, state.addRectPoint1Raw.y, this.mouseX, this.mouseY);
        DrawWrappers.drawRect(this.ctx, rect, "blue", undefined, undefined, 0.5);
      }
      */
    }
  }

  drawVileCannon(poi: POI, x: number, y: number, isBehind: boolean) {
    if (!poi?.tags) return false;
    let selectedSprite = this.spriteEditor.data.selectedSprite;
    if (!isBehind && poi.tags.endsWith("b")) return false;
    if (isBehind && !poi.tags.endsWith("b")) return false;
    let spritePrefix = "vile_";
    if (selectedSprite.name.startsWith("vilemk2")) spritePrefix = "vilemk2_";
    if (selectedSprite.name.startsWith("vilemk5")) spritePrefix = "vilemk5_";
    let spriteNameToDraw = spritePrefix + "cannon";
    let spriteIndexToDraw = 0;
    if (poi.tags.startsWith("cannon1")) {
      spriteIndexToDraw = 0;
    }
    else if (poi.tags.startsWith("cannon2")) {
      spriteIndexToDraw = 1;
    }
    else if (poi.tags.startsWith("cannon3")) {
      spriteIndexToDraw = 2;
    }
    else if (poi.tags.startsWith("cannon4")) {
      spriteIndexToDraw = 3;
    }
    else if (poi.tags.startsWith("cannon5")) {
      spriteIndexToDraw = 4;
    }
    else {
      return false;
    }
    if (spriteNameToDraw) {
      let spriteToDraw = global.sprites.find(s => s.name === spriteNameToDraw);
      if (spriteToDraw) {
        let flipX = 1;
        let xOff = 0;
        if (selectedSprite.name.endsWith("wall_slide")) {
          flipX = -1;
          xOff = spriteToDraw.frames[spriteIndexToDraw].rect.w;
        }
        spriteToDraw.draw(this.ctx, spriteIndexToDraw, x - xOff + (poi.x * flipX), y + poi.y, flipX);
        return true;
      }
    }
    return false;
  }

  onMouseUp(whichMouse: MouseButton) {
  }

  onMouseWheel(e: WheelEvent) {
    if(this.isHeld(KeyCode.CONTROL)) {
      let delta = -(e.deltaY/180);
      this.zoom += delta;
      if(this.zoom < 1) this.zoom = 1;
      if(this.zoom > 5) this.zoom = 5;
      this.spriteEditor.setZoom(this.zoom);
      this.redraw();
      e.preventDefault();
      return false;
    }
  }

  onMouseMove(deltaX: number, deltaY: number) {
    let state = this.spriteEditor.data;
    if (state.selection && this.mousedown) {
      state.selection.move(deltaX, deltaY);
      this.redraw();
    }
    if (this.spriteEditor.addRectMode > 0) {
      this.redraw();
    }
  }

  onLeftMouseDown() {
    let state = this.spriteEditor.data;
    let cX = (this.canvas.width)/2;
    let cY = (this.canvas.height)/2;
    let relMouseX = Math.round((this.mouseX - cX)/this.zoom);
    let relMouseY = Math.round((this.mouseY - cY)/this.zoom);

    if (this.spriteEditor.addPOIMode) {
      if (state.selectedFrame) {
        this.spriteEditor.data.setSelectedSpriteDirty(true);
        this.spriteEditor.addPOI(state.selectedFrame, relMouseX, relMouseY);
        this.spriteEditor.changeAddPOIMode(false);
        this.spriteEditor.changeState();
      }
      return;
    }

    if (this.spriteEditor.addRectMode === 1 || this.spriteEditor.addRectMode === 2) {
      if (!this.spriteEditor.addRectPoint1) {
        this.spriteEditor.addRectPoint1 = new Point(relMouseX, relMouseY);
        this.spriteEditor.addRectPoint1Raw = new Point(this.mouseX, this.mouseY);
      }
      else if (!this.spriteEditor.addRectPoint2) {
        this.spriteEditor.addRectPoint2 = new Point(relMouseX, relMouseY);

        let anchor = state.selectedSprite.getAnchor();
        let w = Math.abs(this.spriteEditor.addRectPoint2.x - this.spriteEditor.addRectPoint1.x);
        let h = Math.abs(this.spriteEditor.addRectPoint2.y - this.spriteEditor.addRectPoint1.y);
        let offX = Math.min(this.spriteEditor.addRectPoint2.x, this.spriteEditor.addRectPoint1.x) + Math.round(anchor.x * w);
        let offY = Math.min(this.spriteEditor.addRectPoint2.y, this.spriteEditor.addRectPoint1.y) + Math.round(anchor.y * h);

        let hitbox = new Hitbox();
        hitbox.offset.x = offX;
        hitbox.offset.y = offY;
        hitbox.width = w;
        hitbox.height = h;
        hitbox.isTrigger = true;

        //hitbox.name = "shield";

        if (this.spriteEditor.addRectMode === 1) {
          this.spriteEditor.data.selectedSprite.hitboxes.push(hitbox);
        }
        else if (this.spriteEditor.addRectMode === 2) {
          this.spriteEditor.data.selectedFrame.hitboxes.push(hitbox);
        }
        this.spriteEditor.data.selectionId = hitbox.selectableId;
        this.spriteEditor.clearRectMode();
        this.spriteEditor.changeState();
      }

      this.redraw();
      return;
    }

    let found = false;
    let selectables = state.getSelectables();
    for (let selectable of selectables) {
      let rect = selectable.getRect().clone(cX/this.zoom, cY/this.zoom);
      if (rect.inRect((this.mouseX - cX)/this.zoom, (this.mouseY - cY)/this.zoom)) {
        if (state.selectionId !== selectable.selectableId) {
          state.selectionId = selectable.selectableId;
          this.spriteEditor.changeState();
        }
        found = true;
      }
    }
    if (!found && state.selectionId !== -1) {
      state.selectionId = -1;
      this.spriteEditor.changeState();
    }
    this.redraw();
  }

  onKeyDown(keyCode: KeyCode, firstFrame: boolean) {
  }
}