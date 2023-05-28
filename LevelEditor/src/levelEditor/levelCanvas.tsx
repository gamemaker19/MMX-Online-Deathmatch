import { LevelEditor } from "./levelEditor";
import * as Helpers from "../helpers";
import * as DrawWrappers from "../drawWrappers";
import { CanvasUI, KeyCode, MouseButton } from "../canvasUI";
import { Rect } from "../models/Rect";
import { Frame } from "../models/Frame";
import { global } from "../Global";
import { Sprite } from "../models/Sprite";
import { CreateInstanceTool, SelectTool } from "./tool";
import { Point } from "../models/Point";
import _ from "lodash";

export class LevelCanvas extends CanvasUI {
  levelEditor: LevelEditor;
  mouseLeftCanvas: boolean = false;
  mousedown: boolean = false;
  middlemousedown: boolean = false;
  rightmousedown: boolean = false;
  maxZoom: number = 4;
  backgroundCanvas: HTMLCanvasElement;
  backgroundCtx: CanvasRenderingContext2D;

  constructor(levelEditor: LevelEditor) {
    super("level-canvas", "rgba(0,0,0,0)");
    this.setOptimizedMode(levelEditor.isOptimizedMode());
    this.zoom = 1;
    this.levelEditor = levelEditor;
    this.isLevelEditor = true;

    this.backgroundCanvas = document.createElement('canvas') as HTMLCanvasElement;
    this.backgroundCtx = this.backgroundCanvas.getContext("2d") as CanvasRenderingContext2D;    
    this.backgroundCtx.imageSmoothingEnabled = false;

    this.wrapper.onscroll = () => {
      this.redraw();
    }
  }

  setOptimizedMode(isOptimizedMode: boolean) {
    this.isNoScrollZoom = isOptimizedMode;
    this.resetFastScroll();
    if (isOptimizedMode) {
      this.wrapper.style.overflow = "hidden";
      this.canvas.width = this.levelEditor.canvasWidth;
      this.canvas.height = this.levelEditor.canvasHeight;
    }
    else {
      this.wrapper.style.overflow = "auto";
      this.canvas.width = this.baseWidth * this.zoom;
      this.canvas.height = this.baseHeight * this.zoom;
    }
  }

  changeCanvasSize() {
    this.wrapper.style.width = `${this.levelEditor.canvasWidth}px`;
    this.wrapper.style.height = `${this.levelEditor.canvasHeight}px`;
    this.fastScrollX = 0;
    this.fastScrollY = 0;
    this.redraw();
  }

  resetFastScroll() {
    this.fastScrollX = 0;
    this.fastScrollY = 0;
  }

  getViewPort() {
    return Rect.CreateWH(this.getScrollLeft() / this.zoom, this.getScrollTop() / this.zoom, this.levelEditor.canvasWidth / this.zoom, this.levelEditor.canvasHeight / this.zoom);
  }

  fastScrollHelper(xDist: number, yDist: number) {
    this.fastScrollX += xDist;
    let maxFastScrollX = Math.max(this.baseWidth * this.zoom - this.levelEditor.canvasWidth, 0);
    if (this.fastScrollX < 0) this.fastScrollX = 0;
    else if (this.fastScrollX > maxFastScrollX) this.fastScrollX = maxFastScrollX;

    this.fastScrollY += yDist;
    let maxFastScrollY = Math.max(this.baseHeight * this.zoom - this.levelEditor.canvasHeight, 0);
    if (this.fastScrollY < 0) this.fastScrollY = 0;
    else if (this.fastScrollY > maxFastScrollY) this.fastScrollY = maxFastScrollY;
  }

  fastScroll(x: number, y: number) {
    this.fastScrollHelper(x * 100, y * 100);
  }

  fastScrollPage(x: number, y: number) {
    this.fastScrollHelper(x * this.levelEditor.canvasWidth, y * this.levelEditor.canvasHeight);
  }

  fastScrollStartEnd(x: number, y: number) {
    this.fastScrollHelper(x * 1000000, y * 1000000);
  }

  redraw() {
    let le = this.levelEditor;
    let data = this.levelEditor.data;
    let sl = data.selectedLevel;
    let canvas = this.canvas;
    let ctx = this.ctx;
    if (!data.selectedLevel) return;

    if (sl) {
      this.baseWidth = sl.width;
      this.baseHeight = sl.height;
      if (!this.isNoScrollZoom) {
        canvas.width = this.baseWidth * this.zoom;
        canvas.height = this.baseHeight * this.zoom;
      }
    }

    ctx.save();
    ctx.imageSmoothingEnabled = false;

    let viewRect = Rect.CreateWH(0, 0, canvas.width, canvas.height);
    ctx.clearRect(viewRect.topLeftPoint.x, viewRect.topLeftPoint.y, viewRect.w, viewRect.h);
    DrawWrappers.drawRect(ctx, viewRect, "white", "", null);
  
    ctx.scale(this.zoom, this.zoom);
    ctx.translate(-this.fastScrollX, -this.fastScrollY);
    
    let viewPort = this.getViewPort();
    ctx.drawImage(
      this.backgroundCanvas,
      viewPort.x1,  // source x
      viewPort.y1, // source y
      viewPort.w,  // source w
      viewPort.h,  // source h
      viewPort.x1,  // dest x
      viewPort.y1,  // dest y
      viewPort.w,  // dest w
      viewPort.h,  // dest h
    );
    
    for (var instance of data.selectedLevel.instances) {
      if (!instance.hidden && instance.getRect().overlaps(viewPort)) {
        instance.draw(ctx, data, viewPort);
      }
    }

    if (this.levelEditor.tool) {
      this.levelEditor.tool.draw();
    }
  
    if (data.toggleShowCamBounds) {
      DrawWrappers.drawRect(ctx, new Rect(this.mouseX-149,this.mouseY-112,this.mouseX+149,this.mouseY+112), "", "green", null);
    }

    if (data.selectedLevel && data.selectedLevel.mirrorX > 0) {
      DrawWrappers.drawLine(ctx, data.selectedLevel.mirrorX, 0, data.selectedLevel.mirrorX, data.selectedLevel.height, "yellow", 2);
    }
    
    if (data.showWallPaths && data.selectedLevel.mergedWalls) {
      for (let mergedShape of data.selectedLevel.mergedWalls) {
        for (let i = 0; i < mergedShape.length; i++) {
          let current = mergedShape[i];
          let next = i + 1 < mergedShape.length ? mergedShape[i + 1] : mergedShape[0];
          DrawWrappers.drawLine(ctx, current[0], current[1], next[0], next[1], "red", 1);
        }
      }
    }

    ctx.restore();
  }

  redrawBackgrounds() {
    let le = this.levelEditor;
    let data = this.levelEditor.data;
    let sl = data.selectedLevel;
    let canvas = this.backgroundCanvas;
    let ctx = this.backgroundCtx;
    
    if (sl) {
      this.baseWidth = sl.width;
      this.baseHeight = sl.height;
      canvas.width = this.baseWidth;
      canvas.height = this.baseHeight;
    }

    ctx.save();
    ctx.imageSmoothingEnabled = false;

    let viewRect = Rect.CreateWH(0, 0, canvas.width, canvas.height);
    ctx.clearRect(viewRect.topLeftPoint.x, viewRect.topLeftPoint.y, viewRect.w, viewRect.h);
    DrawWrappers.drawRect(ctx, viewRect, "white", "", null);
  
    if (!data.selectedLevel.isMirrorJson() || !data.selectedLevel.mirrorMapImages) {
      for (let i = 0; i < data.selectedLevel.parallaxes.length; i++) {
        let selectedParallaxSpritesheet = le.selectedParallax(i);
        let selectedParallax = data.selectedLevel.parallaxes[i];
        if (selectedParallax.isLargeCamOverride) continue;
        let pX = selectedParallax.startX ?? 0;
        let pY = selectedParallax.startY ?? 0;
        if (data.selectedLevel && data.toggleShowCamBounds) {
          pX += Helpers.clamp(this.mouseX - 149, 0, canvas.width) * selectedParallax.speedX;
          pY += Helpers.clamp(this.mouseY - 112, 0, canvas.height) * selectedParallax.speedY;
        }

        if (selectedParallaxSpritesheet?.imgEl && data.showParallaxes) {
          DrawWrappers.drawImage(ctx, selectedParallaxSpritesheet.imgEl, 0, 0, selectedParallaxSpritesheet.imgEl.width, selectedParallaxSpritesheet.imgEl.height, pX, pY);
        }
      }
      
      if (le.selectedBackground2?.imgEl && data.showBackwall) {
        DrawWrappers.drawImage(ctx, le.selectedBackground2.imgEl, 0, 0);
      }
      if (le.selectedBackground?.imgEl && data.showBackground) {
        DrawWrappers.drawImage(ctx, le.selectedBackground.imgEl, 0, 0);
      }
      if (le.selectedForeground?.imgEl && data.showForeground) {
        DrawWrappers.drawImage(ctx, le.selectedForeground.imgEl, 0, 0);
      }
    }
    else {
      for (let i = 0; i < data.selectedLevel.parallaxes.length; i++) {
        let selectedParallaxSpritesheet = le.selectedParallax(i);
        let selectedParallax = data.selectedLevel.parallaxes[i];
        if (selectedParallax.isLargeCamOverride) continue;
        let pX = selectedParallax.startX ?? 0;
        let pY = selectedParallax.startY ?? 0;
        if (data.selectedLevel && data.toggleShowCamBounds) {
          pX += Helpers.clamp(this.mouseX - 149, 0, canvas.width) * selectedParallax.speedX;
          pY += Helpers.clamp(this.mouseY - 112, 0, canvas.height) * selectedParallax.speedY;
        }

        if (selectedParallaxSpritesheet?.imgEl && data.showParallaxes) {
          DrawWrappers.drawImage(ctx, selectedParallaxSpritesheet.imgEl, 0, 0, selectedParallax.mirrorX, selectedParallaxSpritesheet.imgEl.height, pX, pY);
          DrawWrappers.drawImage(ctx, selectedParallaxSpritesheet.imgEl, 0, 0, selectedParallax.mirrorX, selectedParallaxSpritesheet.imgEl.height, selectedParallax.mirrorX + pX, pY, -1);
        }
      }

      if (le.selectedBackground2?.imgEl && data.showBackwall) {
        DrawWrappers.drawImage(ctx, le.selectedBackground2.imgEl, 0, 0, sl.mirrorX, le.selectedBackground2.imgEl.height, 0, 0);
        DrawWrappers.drawImage(ctx, le.selectedBackground2.imgEl, 0, 0, sl.mirrorX, le.selectedBackground2.imgEl.height, sl.mirrorX, 0, -1);
      }
      if (le.selectedBackground?.imgEl && data.showBackground) {
        DrawWrappers.drawImage(ctx, le.selectedBackground.imgEl, 0, 0, sl.mirrorX, le.selectedBackground.imgEl.height, 0, 0);
        DrawWrappers.drawImage(ctx, le.selectedBackground.imgEl, 0, 0, sl.mirrorX, le.selectedBackground.imgEl.height, sl.mirrorX, 0, -1);
      }
      if (le.selectedForeground?.imgEl && data.showForeground) {
        DrawWrappers.drawImage(ctx, le.selectedForeground.imgEl, 0, 0, sl.mirrorX, le.selectedForeground.imgEl.height, 0, 0);
        DrawWrappers.drawImage(ctx, le.selectedForeground.imgEl, 0, 0, sl.mirrorX, le.selectedForeground.imgEl.height, sl.mirrorX, 0, -1);
      }
    }

    ctx.restore();
  }

  onMouseWheel(e: WheelEvent) {
    if (this.isHeld(KeyCode.CONTROL)) {
      let delta = -(e.deltaY/360);
      this.zoom += delta;
      if (this.zoom < 1) this.zoom = 1;
      if (this.zoom > this.maxZoom) this.zoom = this.maxZoom;
      this.zoom = Math.round(this.zoom);
      this.setZoom(this.zoom);
      this.levelEditor.forceUpdate();
      e.preventDefault();
      return false;
    }
  }

  setZoom(zoom: number) {

    this.zoom = zoom;
    this.redraw();

    let pos: Point;
    let zoomInPlace: boolean = false;
    let instance = this.levelEditor.data.selectedInstances[0];
    if (instance) {
      pos = instance.pos ?? instance.points[0];
    } else {
      //pos = new Point(this.mouseX + (this.wrapper.scrollLeft * this.zoom), this.mouseY + (this.wrapper.scrollTop * this.zoom));
      if (!this.isNoScrollZoom) {
        pos = new Point(this.lastClickX, this.lastClickY);
      }
      else {
        zoomInPlace = true;
      }
    }

    if (!this.isNoScrollZoom) {
      this.wrapper.scrollLeft = (pos.x * this.zoom) - (this.levelEditor.canvasWidth * 0.5);
      this.wrapper.scrollTop = (pos.y * this.zoom) - (this.levelEditor.canvasHeight * 0.5);
    }
    else {
      if (!zoomInPlace) {
        this.fastScrollX = (pos.x) - (this.levelEditor.canvasWidth * 0.5 / this.zoom);
        this.fastScrollY = (pos.y) - (this.levelEditor.canvasHeight * 0.5 / this.zoom);
      }
      else {
      }
      this.fastScrollHelper(0, 0);
    }

    this.redraw();
  }
  
  onMouseMove(deltaX: number, deltaY: number) {
    if (this.levelEditor.tool) {
      this.levelEditor.tool.onMouseMove(deltaX, deltaY);
    }
  }
  
  onMouseLeave() {
    if (this.levelEditor.tool) {
      this.levelEditor.tool.onMouseLeaveCanvas();
    }
  }

  onLeftMouseDown() {
    if (this.levelEditor.tool) {
      this.levelEditor.tool.onMouseDown();
    }
    this.levelEditor.forceUpdate();
  }

  onRightMouseDown() {
    if (this.levelEditor.tool) {
      this.levelEditor.tool.onRightMouseDown();
    }
    this.levelEditor.forceUpdate();
  }

  onLeftMouseUp() {
    if (this.levelEditor.tool) {
      this.levelEditor.tool.onMouseUp();
    }
  }

  onKeyDown(keyCode: KeyCode, firstFrame: boolean) {

    let data = this.levelEditor.data;
    if (!data.selectedLevel) return;
    
    if (keyCode === KeyCode.E) {
      for (let instance of data.selectedInstances) {
        instance.snapCollisionShape(data.selectedLevel.instances);
      }
      data.setSelectedLevelDirty(true);
    }

    if (keyCode === KeyCode.Q) {
      for (let instance of data.selectedInstances) {
        instance.normalizePoints();
      }
      data.setSelectedLevelDirty(true);
    }

    if (keyCode === KeyCode.B) {
      data.toggleShowCamBounds = !data.toggleShowCamBounds;
    }

    if (keyCode === KeyCode.SPACE) {
      if (!data.lastObj) return;
      data.selectedObjectIndex = -1;
      data.selectedInstanceIds = [];
      this.levelEditor.switchTool(new SelectTool(this.levelEditor));
      this.levelEditor.changeObject(data.lastObj);
    }

    if (keyCode === KeyCode.C) {
      var copiedInstances = [];
      for (var instance of data.selectedInstances) {
        var clonedInstance = instance.clone();
        let newName = data.selectedLevel.getNewInstanceName(clonedInstance.obj);
        clonedInstance.name = newName;
        clonedInstance.move(10, 10);
        copiedInstances.push(clonedInstance);
        data.selectedLevel.addInstance(clonedInstance);
      }
      data.selectedObjectIndex = -1;
      data.selectedInstanceIds = copiedInstances.map(ci => ci.id);
      this.levelEditor.cssld();
      this.levelEditor.switchTool(new SelectTool(this.levelEditor));
    }

    if ((keyCode === KeyCode.SPACE || keyCode === KeyCode.TAB || keyCode === KeyCode.CONTROL || keyCode === KeyCode.ALT)) {
      // e.preventDefault();
    }

    this.levelEditor.redraw();
  }
}