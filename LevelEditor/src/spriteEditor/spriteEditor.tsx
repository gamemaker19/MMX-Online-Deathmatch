import * as _ from "lodash";
import { Sprite } from "../models/Sprite";
import { Frame } from "../models/Frame";
import { Spritesheet } from "../models/Spritesheet";
import * as Helpers from "../helpers";
import { Hitbox } from "../models/Hitbox";
import { Point } from "../models/Point";
import { Selectable } from "../selectable";
import { POI } from "../models/POI";
import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { SpriteCanvas } from "./spriteCanvas";
import { SpritesheetCanvas } from "./spriteSheetCanvas";
import { global } from "../Global";
import { SpriteEditorInput } from "./spriteEditorInput";
import * as SpriteEditorJsx from "./spriteEditorJsx";
import { classToPlain, plainToClass, serialize } from 'class-transformer';
import { Rect } from "../models/Rect";
import { Config } from "../config";
import { BaseEditor } from "../baseEditor";
import { get2DArrayFromImage, getPixelClumpRect, getSelectedPixelRect } from "../pixelClump";

export class Ghost {
  sprite: Sprite;
  frame: Frame;
  constructor(sprite: Sprite, frame: Frame) {
    this.sprite = sprite;
    this.frame = frame;
  }
}

export class SpriteEditorState {
  modifiedSprites: Sprite[] = [];
  selectedSpritesheetPath: string = "";
  isAnimPlaying: boolean = false;
  offsetX: number = 0;
  offsetY: number = 0;
  hideGizmos: boolean = false;
  moveChildren: boolean = false;
  moveToTopOrBottom: boolean = false;
  bulkDuration: number = 0;
  newSpriteName: string = "";
  newSpriteActive: boolean = false;
  newSpriteSpritesheetPath = "";
  ghost: Ghost | undefined = undefined;
  lastSelectedFrameIndex: number = 0;
  spriteFilter: string = "";
  selectedFilterMode: string = "contains";
  pendingFrame : Frame | undefined = undefined;
  selectedSpriteIndex: number = -1;
  selectedFrameIndex: number = -1;
  _selectionId: number = -1;

  constructor() {
  }

  get selectionId(): number {
    return this._selectionId;
  }

  set selectionId(value: number) {
    this._selectionId = value;
  }

  isSpriteDirty(sprite: Sprite): boolean {
    return _.some(this.modifiedSprites, s => s.name === sprite.name && s.isDirty);
  }

  isSelectedSpriteDirty(): boolean {
    return this.isSpriteDirty(this.selectedSprite);
  }

  setSpriteDirty(sprite: Sprite, isDirty: boolean) {
    let isSpriteDirty = this.isSpriteDirty(sprite);
    if (isDirty && !isSpriteDirty) {
      let modifiedSprite = _.find(this.modifiedSprites, s => s.name === sprite.name);
      if (modifiedSprite) {
        modifiedSprite.isDirty = true;
      } else {
        let clonedSprite = _.cloneDeep(sprite);
        clonedSprite.isDirty = true;
        this.modifiedSprites.push(clonedSprite);
      }
    }
    else if (!isDirty && isSpriteDirty) {
      let modifiedSprite = _.find(this.modifiedSprites, s => s.name === sprite.name);
      if (modifiedSprite) {
        modifiedSprite.isDirty = false;
      }
    }
  }

  setSelectedSpriteDirty(isDirty: boolean) {
    this.setSpriteDirty(this.selectedSprite, isDirty);
  }

  get selection(): Selectable | undefined {
    if (this.selectionId === -1) return undefined;
    let selectables = this.getSelectables();
    return _.find(selectables, s => s.selectableId === this.selectionId);
  }

  get selectedSprite() : Sprite | undefined {
    if (this.selectedSpriteIndex === -1) return undefined;
    let globalSprite = global.sprites[this.selectedSpriteIndex];
    
    let modifiedSprite = _.find(this.modifiedSprites, s => s.name === globalSprite.name);
    if (!modifiedSprite) {
      let clonedSprite = _.cloneDeep(globalSprite);
      this.modifiedSprites.push(clonedSprite);
      return clonedSprite;
    }

    return modifiedSprite;
  }

  get selectedFrame() : Frame | undefined {
    if (this.selectedFrameIndex === -1) return undefined;
    return this.selectedSprite.frames[this.selectedFrameIndex];
  }

  getSelectables(): Selectable[] {
    let selectables: Selectable[] = [];
    for(let hitbox of this.selectedSprite?.hitboxes ?? []) {
      selectables.push(hitbox);
    }
    for(let hitbox of this.selectedFrame?.hitboxes ?? []) {
      selectables.push(hitbox);
    }
    for(let poi of this.selectedFrame?.POIs ?? []) {
      selectables.push(poi);
    }
    return selectables;
  }
}

export class SpriteEditor extends BaseEditor<SpriteEditorState> {
  
  spriteCanvas: SpriteCanvas | undefined;
  spritesheetCanvas: SpritesheetCanvas | undefined;
  animTime: number = 0;
  animFrameIndex: number = 0;
  input: SpriteEditorInput;
  customMapContext: string;
  isMapSpriteEditor: boolean;

  addRectMode: number = 0;
  addRectPoint1: Point | undefined = undefined;
  addRectPoint2: Point | undefined = undefined;
  addRectPoint1Raw: Point | undefined = undefined;
  addPOIMode: boolean = false;
  defaultPOIFlag: string = "";

  hiddenCanvas: HTMLCanvasElement;
  hiddenCtx: CanvasRenderingContext2D;

  constructor(props: {}) {
    super(props);
    this.data = new SpriteEditorState();
  }

  // "Change State Selected Sprite Dirty"
  csssd() : void {
    this.data.setSelectedSpriteDirty(true);
    this.changeState();
  }

  debug(): void {
    for (let sprite of global.sprites) {
      let changed = false;
      for (let frame of sprite.frames) {
        if (frame.POIs.length > 0) {
          for (let poi of frame.POIs) {
            if (poi.tags === "h") {
              poi.y += 4;
              changed = true;
            }
          }
        }
      }
      if (changed) {
        this.data.setSpriteDirty(sprite, true);
      }
    }
  }

  componentDidMount() {
    this.spriteCanvas = new SpriteCanvas(this);
    this.spritesheetCanvas = new SpritesheetCanvas(this);
    this.input = new SpriteEditorInput(this);

    window.Main.on("requestDirtyState", () => {
      window.Main.sendHasDirty(this.isAnySpriteDirty());
    });

    this.hiddenCanvas = document.createElement('canvas') as HTMLCanvasElement;
    this.hiddenCtx = this.hiddenCanvas.getContext("2d") as CanvasRenderingContext2D;    
    this.hiddenCtx.imageSmoothingEnabled = false;

    this.fetchAllData();

    setInterval(() => this.mainLoop(this), 1000 / 60);
  }

  async fetchAllData() {
    
    try {
      let configData = await window.Main.getConfig();
      
      this.config = plainToClass(Config, configData);

      this.customMapContext = await window.Main.getCustomMapContext();
      console.log("customMapContext: " + this.customMapContext);
      this.data.spriteFilter = this.config.lastSpriteFilter;
      this.data.selectedFilterMode = this.config.lastSpriteFilterMode;
      this.setZoom(this.config.lastZoom);

      if (!this.config.isProd) {
        window.onerror = function(error) {
          window.Main.showDialog("Error (debug mode only)", error.toString());
        };
      }

      let spritesheetsData = await window.Main.getSpritesheets();

      _.each(spritesheetsData, (spritesheet: string) => {
        global.spritesheetMap[Helpers.getNormalizedSpritesheetName(undefined, spritesheet)] = new Spritesheet(spritesheet);
      });

      let spritesData = await window.Main.getSprites();

      for (let spriteData of spritesData) {
        // @ts-ignore
        let sprite: Sprite = plainToClass(Sprite, spriteData, { excludeExtraneousValues: true });
        global.sprites.push(sprite);
      }

      this.isMapSpriteEditor = await window.Main.getIsMapSpriteEditor();

      this.isLoading = false;
      this.changeState();

      if (!this.config.isProd) {
        this.getNoHeadshotSprites();
      }
    }
    catch (error) {
      window.Main.showError("Error", error.toString());
      this.isLoading = false;
      this.forceUpdate();
    }
  }

  render(): JSX.Element {
    return SpriteEditorJsx.render(this);
  }

  isAnySpriteDirty(): boolean {
    return this.data.modifiedSprites.some(s => s.isDirty);
  }

  getFilteredSprites(): Sprite[] {
    let filters = this.data.spriteFilter.split(",");
    if(filters[0] === "") return global.sprites;
    return _.filter(global.sprites, (sprite: Sprite) => {
      if(this.data.selectedFilterMode === "exactmatch") {
        return filters.indexOf(sprite.name) >= 0;
      }
      else if(this.data.selectedFilterMode === "contains") {
        for(let filter of filters) {
          if(sprite.name.toLowerCase().includes(filter.toLowerCase())) {
            return true;
          }
        }
      }
      else if(this.data.selectedFilterMode === "startswith") {
        for(let filter of filters) {
          if(sprite.name.startsWith(filter)) {
            return true;
          }
        }
      }
      else if(this.data.selectedFilterMode === "endswith") {
        for(let filter of filters) {
          if(sprite.name.endsWith(filter)) {
            return true;
          }
        }
      }
      return false;
    });
  }

  onSpritesheetChange(newSheetPath: string, changeStateIfSame: boolean = false) {
    if (this.data.selectedSpritesheetPath === newSheetPath) {
      if (changeStateIfSame) {
        this.changeState();
      }
      return;
    }
    if (newSheetPath === "") {
      window.Main.showDialog("Error", "should not be able to clear out spritesheet");
      if (changeStateIfSame) {
        this.changeState();
      }
      return;
    }

    this.data.selectedSpritesheetPath = newSheetPath;
    this.data.selectedSprite.spritesheetPath = newSheetPath;

    let newSheet = global.spritesheetMap[Helpers.getNormalizedSpritesheetName(this.customMapContext, newSheetPath)];
    if (newSheet.imgEl) {
      this.spritesheetCanvas.setSize(newSheet.imgEl.width, newSheet.imgEl.height);
      this.spritesheetCanvas.ctx.drawImage(newSheet.imgEl, 0, 0);      
      this.changeState();
      this.redraw();
      return;
    }

    this.isLoading = true;
    this.changeState();

    let spritesheetImg = document.createElement("img");
    spritesheetImg.onload = () => {
      this.spritesheetCanvas.setSize(spritesheetImg.width, spritesheetImg.height);
      this.spritesheetCanvas.ctx.drawImage(spritesheetImg, 0, 0);      
      let imageData = this.spritesheetCanvas.ctx.getImageData(0,0,this.spritesheetCanvas.canvas.width,this.spritesheetCanvas.canvas.height);
      newSheet.imgArr = get2DArrayFromImage(imageData);
      newSheet.imgEl = spritesheetImg;
      this.isLoading = false;
      this.redraw();
      this.forceUpdate();
    };
    spritesheetImg.onerror = (e) => {
      console.log("Error loading image " + newSheet.path);
      window.Main.showDialog("Error", "Error loading image " + newSheet.path);
      this.isLoading = false;
      this.forceUpdate();
    }
    spritesheetImg.src = "file:///" + newSheet.path;
  }

  getSpriteDisplayName(sprite: Sprite) {
    return sprite.name + (this.data.isSpriteDirty(sprite) ? '*' : '');
  }

  newSprite() {
    if (!this.isAllowedAction()) return;
    this.data.newSpriteActive = true;
    //this.data.newSpriteSpritesheetPath = this.data.newSpriteSpritesheetPath || global.spritesheets[0].path;
    this.data.newSpriteSpritesheetPath = this.data.selectedSpritesheetPath;
    this.changeState();
  }

  addSprite() {
    if (!this.data.newSpriteName || !this.data.newSpriteSpritesheetPath) {
      window.Main.showDialog("Error", "Must provide a sprite name and spritesheet path", undefined, true);
      return;
    }
    if (_.some(global.sprites, s => s.name.toLowerCase() === this.data.newSpriteName.toLowerCase())) {
      window.Main.showDialog("Error", "Sprite name already exists", undefined, true);
      return;
    }
    this.data.newSpriteActive = false;
    let newSprite = new Sprite(this.data.newSpriteName, this.data.newSpriteSpritesheetPath, this.customMapContext);
    this.data.newSpriteName = "";
    //this.data.newSpriteSpritesheetPath = "";
    this.data.setSpriteDirty(newSprite, true);
    this.data.selectedFrameIndex = -1;
    this.data.selectionId = -1;
    global.sprites.unshift(newSprite);
    this.changeSprite(0, true);
    document.getElementsByClassName("sprite-list-scroll")[0].scrollTop = 0;
  }

  changeSprite(newSpriteIndex: number, isNew: boolean) {
    this.data.selectedSpriteIndex = newSpriteIndex;
    this.data.selectionId = -1;
    this.data.selectedFrameIndex = 0;
    this.data.lastSelectedFrameIndex = 0;
    this.data.isAnimPlaying = false;
    this.data.pendingFrame = undefined;
    this.onSpritesheetChange(this.data.selectedSprite.spritesheetPath, true);
  }

  changeSpriteFilter(value: string) {
    this.data.spriteFilter = value;
    this.config.lastSpriteFilter = value;
    this.saveConfig();
    this.changeState();
  }

  changeSpriteFilterMode(value: string) {
    this.data.selectedFilterMode = value;
    this.config.lastSpriteFilterMode = value;
    this.saveConfig();
    this.changeState();
  }

  onSwapFolderClick() {
    if (!this.config) return;
    if (this.config.isInSpriteModFolder) {
      this.config.isInSpriteModFolder = false;
      this.saveConfigAndReload();
    }
    else {
      this.config.isInSpriteModFolder = true;
      this.saveConfigAndReload();
    }
  }
  getSwapFolderButtonText() {
    if (!this.config) return "Loading...";
    if (this.config.isInSpriteModFolder) {
      return "Change to sprites folder";
    }
    return "Change to sprites_visualmods folder";
  }

  saveConfig() {
    let jsonStr = JSON.stringify(classToPlain(this.config), null, 2);
    window.Main.saveConfig(jsonStr);
  }

  saveConfigAndReload() {
    let jsonStr = JSON.stringify(classToPlain(this.config), null, 2);
    window.Main.saveConfigAndReload({ config: jsonStr, isDirty: this.isAnySpriteDirty() });
  }

  reload() {
    window.Main.reload({ isDirty: this.isAnySpriteDirty() });
  }

  addHitboxToSprite(placeCoords: boolean = false) {
    this.data.setSelectedSpriteDirty(true);
    if (!placeCoords) {
      let hitbox = new Hitbox();
      hitbox.width = this.data.selectedFrame.rect.w;
      hitbox.height = this.data.selectedFrame.rect.h;
      hitbox.isTrigger = true;
      this.data.selectedSprite.hitboxes.push(hitbox);
      this.data.selectionId = hitbox.selectableId;
      this.changeState();
    }
    else {
      this.spriteCanvas.canvas.style.cursor = "crosshair";
      this.addRectMode = 1;
      this.redraw();
      this.forceUpdate();
    }
  }

  addHitboxToFrame(placeCoords: boolean = false) {
    this.data.setSelectedSpriteDirty(true);
    if (!placeCoords) {
      let hitbox = new Hitbox();
      hitbox.width = this.data.selectedFrame.rect.w;
      hitbox.height = this.data.selectedFrame.rect.h;
      hitbox.isTrigger = true;
      this.data.selectedFrame.hitboxes.push(hitbox);
      this.data.selectionId = hitbox.selectableId;
      this.changeState();
    }
    else {
      this.spriteCanvas.canvas.style.cursor = "crosshair";
      this.addRectMode = 2;
      this.forceUpdate();
    }
  }

  clearRectMode() {
    this.addRectMode = 0;
    this.addRectPoint1 = undefined;
    this.addRectPoint2 = undefined;
    this.addRectPoint1Raw = undefined;
    this.spriteCanvas.canvas.style.cursor = "auto";
    this.forceUpdate();
  }

  changeAddPOIMode(trueOrFalse: boolean, defaultPOIFlag: string = ""): void {
    this.addPOIMode = trueOrFalse;
    this.defaultPOIFlag = defaultPOIFlag;
    this.spriteCanvas.canvas.style.cursor = this.addPOIMode ? "crosshair" : "auto";
    this.forceUpdate();
  }

  selectHitbox(hitbox: Hitbox) {
    this.data.selectionId = hitbox.selectableId;
    this.changeState();
    this.spriteCanvas?.wrapper.focus();
  }

  deleteHitbox(hitboxArr: Hitbox[], hitbox: Hitbox) {
    _.pull(hitboxArr, hitbox);
    this.data.setSelectedSpriteDirty(true);
    this.changeState();
  }

  isSelectedFrameAdded() {
    if (!this.data.selectedSprite) return false;
    return _.includes(this.data.selectedSprite.frames, this.data.selectedFrame);
  }

  addPendingFrame(index: number | undefined = undefined) {
    if (index === undefined && !this.isAllowedAction()) return;
    this.data.setSelectedSpriteDirty(true);
    if (index === undefined) {
      let pendingFrame = _.cloneDeep(this.data.pendingFrame) ?? _.cloneDeep(this.data.selectedFrame);
      pendingFrame.autoIncId();
      this.data.selectedSprite.frames.push(pendingFrame);
      index = this.data.selectedSprite.frames.length - 1;
    }
    else {
      if (!this.data.selectedFrame || !this.data.pendingFrame) return;
      let pendingFrame = _.cloneDeep(this.data.selectedFrame);
      pendingFrame.autoIncId();
      pendingFrame.rect = _.cloneDeep(this.data.pendingFrame.rect);
      this.data.selectedSprite.frames[index] = pendingFrame;
    }
    this.data.lastSelectedFrameIndex = this.data.selectedFrameIndex;
    this.data.selectedFrameIndex = index;
    this.changeState();
  }

  selectFrame(index: number) {
    this.data.pendingFrame = undefined;
    this.data.lastSelectedFrameIndex = this.data.selectedFrameIndex;
    this.data.selectedFrameIndex = index;
    this.changeState();
  }

  copyFrame(index: number, dir: number) {
    if (!this.isAllowedAction()) return;
    this.data.setSelectedSpriteDirty(true); 
    let frame = this.data.selectedSprite.frames[index];
    if(dir === -1) dir = 0;
    let clonedFrame = _.cloneDeep(frame);
    clonedFrame.autoIncId();
    if (this.data.moveToTopOrBottom) {
      if (dir === 1) this.data.selectedSprite.frames.push(clonedFrame);
      else this.data.selectedSprite.frames.unshift(clonedFrame);
    }
    else {
      this.data.selectedSprite.frames.splice(index + dir, 0, clonedFrame);
    }
    
    this.changeState();
  }

  moveFrame(index: number, dir: number) {
    if (!this.isAllowedAction()) return;
    this.data.setSelectedSpriteDirty(true); 
    if (index + dir < 0 || index + dir >= this.data.selectedSprite.frames.length) return;
    let temp = this.data.selectedSprite.frames[index];
    let newIndex = index + dir;

    if (this.data.moveToTopOrBottom) {
      if (dir === -1) {
        newIndex = 0;
        let currentFrame = this.data.selectedSprite.frames[index];
        this.data.selectedSprite.frames.splice(index, 1);
        this.data.selectedSprite.frames.unshift(currentFrame);
      }
      else {
        newIndex = this.data.selectedSprite.frames.length - 1;
        let currentFrame = this.data.selectedSprite.frames[index];
        this.data.selectedSprite.frames.splice(index, 1);
        this.data.selectedSprite.frames.push(currentFrame);
      }
    }
    else {
      this.data.selectedSprite.frames[index] = this.data.selectedSprite.frames[newIndex];
      this.data.selectedSprite.frames[newIndex] = temp;
    }
    this.data.lastSelectedFrameIndex = this.data.selectedFrameIndex;
    this.data.selectedFrameIndex = newIndex;
    this.changeState();
  }

  deleteFrame(index: number) {
    if (!this.isAllowedAction()) return;
    this.data.setSelectedSpriteDirty(true); 
    this.data.selectedSprite.frames.splice(index, 1);
    this.data.selectedFrameIndex = 0;
    this.data.lastSelectedFrameIndex = 0;
    this.changeState();
  }

  selectNextFrame() {
    if (this.data.selectedSprite.frames.length < 2) return;
    this.data.selectionId = -1;
    if (this.data.selectedFrameIndex < this.data.selectedSprite.frames.length - 1) {
      this.data.selectedFrameIndex++;
    }
    else {
      this.data.selectedFrameIndex = 0;
    }
    this.changeState();
  }

  selectPrevFrame() {
    if (this.data.selectedSprite.frames.length < 2) return;
    this.data.selectionId = -1;
    if (this.data.selectedFrameIndex > 0) {
      this.data.selectedFrameIndex--;
    }
    else {
      this.data.selectedFrameIndex = this.data.selectedSprite.frames.length - 1;
    }
    this.changeState();
  }

  playAnim() {
    this.data.isAnimPlaying = !this.data.isAnimPlaying;
    this.changeState();
    if(!this.data.isAnimPlaying) {
      this.animFrameIndex = 0;
    }
  }

  async saveSprite() {

    if (this.isMapSpriteEditor && (this.selectedSpritesheet.imgEl.width > 1024 || this.selectedSpritesheet.imgEl.height > 1024)) {
      window.Main.showError("WARNING", "Spritesheet is larger than 1024x1024 pixels.\nSprite will still be saved, but please reduce spritesheet size.\nOtherwise lower-end PCs will not be able to play the map, and the map editor may lag more.");
    }

    let plainSprite = classToPlain(this.data.selectedSprite);
    try {
      await window.Main.saveSprite(plainSprite);
      console.log("Successfully saved sprite");
      this.data.setSelectedSpriteDirty(false);
      for (let i = 0; i < global.sprites.length; i++) {
        if (global.sprites[i].name === this.data.selectedSprite.name) {
          global.sprites[i] = _.cloneDeep(this.data.selectedSprite);
          break;
        }
      }
      this.changeState();
    }
    catch (error) {
      window.Main.showError("Error saving sprites", error.toString());
    }
  }

  async saveSprites() {
    let changedSprites = _.filter(this.data.modifiedSprites, s => s.isDirty);
    let plainSprites = _.map(changedSprites, s => classToPlain(s));
    try {
      await window.Main.saveSprites(plainSprites);
      console.log("Successfully saved sprites");
      for (let i = 0; i < global.sprites.length; i++) {
        for (let modifiedSprite of this.data.modifiedSprites) {
          if (global.sprites[i].name === modifiedSprite.name) {
            modifiedSprite.isDirty = false;
            global.sprites[i] = modifiedSprite;
          }
        }
      }
      this.data.modifiedSprites = [];
      this.changeState();
    }
    catch (error) {
      window.Main.showError("Error saving sprites", error.toString());
    }

  }

  forceAllDirty() {
    for (let sprite of global.sprites) {
      this.data.setSpriteDirty(sprite, true);
    }
  }

  redraw(redrawBackgrounds: boolean = false) {
    this.spriteCanvas?.redraw();
    this.spritesheetCanvas?.redraw();
  }

  onBulkDurationChange(bulkDuration: number) {
    this.data.setSelectedSpriteDirty(true);
    for (let frame of this.data.selectedSprite.frames) {
      frame.setDurationFromFrames(bulkDuration);
    }
    this.data.bulkDuration = bulkDuration;
    this.changeState();
  }

  reverseFrames() {
    if (!this.isAllowedAction()) return;
    this.data.setSelectedSpriteDirty(true);
    _.reverse(this.data.selectedSprite.frames);
    this.changeState();
  }

  recomputeFrame(frame: Frame, shouldChangeState: boolean = true) {
    var cr = frame.rect;

    let centerX = (cr.x2 + cr.x1) / 2;
    let centerY = (cr.y2 + cr.y1) / 2;
    let bestX = cr.x1;
    let bestY = cr.y1;
    let bestDist = 1000000;
    for (let x = cr.x1; x < cr.x2; x++) {
      for (let y = cr.y1; y < cr.y2; y++) {
        if (this.selectedSpritesheet.imgArr[y][x].rgb.a > 0) {
          let dist = Math.abs(centerX - x) + Math.abs(centerY - y);
          if (dist < bestDist) {
            bestDist = dist;
            bestX = x;
            bestY = y;
          }
        }
      }
    }

    var rect = getPixelClumpRect(bestX, bestY, this.selectedSpritesheet.imgArr);
    if (rect) {
      frame.rect = rect;
      if (shouldChangeState) {
        this.csssd();
      }
    }
    return;

  }

  recomputeAllFrames() {
    for (let frame of this.data.selectedSprite.frames) {
      this.recomputeFrame(frame, false);
    }
    this.csssd();
  }

  addPOI(frame: Frame, x: number, y: number) {
    var poi = new POI(this.defaultPOIFlag, Math.round(x), Math.round(y));
    frame.POIs.push(poi);
    this.csssd();
    this.selectPOI(poi);
  }

  movePOI(frame: Frame, index: number, dir: number) {
    if (!this.isAllowedAction()) return;
    this.data.setSelectedSpriteDirty(true); 
    if (index + dir < 0 || index + dir >= frame.POIs.length) return;
    let temp = frame.POIs[index];
    let newIndex = index + dir;
    frame.POIs[index] = frame.POIs[newIndex];
    frame.POIs[newIndex] = temp;
    this.changeState();
  }

  selectPOI(poi: POI) {
    this.data.selectionId = poi.selectableId;
    this.changeState();
  }

  deletePOI(poi: POI) {
    if (!this.data.selectedFrame) return;
    _.pull(this.data.selectedFrame.POIs, poi);
    if (this.data.selectionId === poi.selectableId) {
      this.data.selectionId = -1;
    }
    this.csssd();
  }

  applyPOIToAllFrames(poi: POI) {
    for (let frame of this.data.selectedSprite.frames) {
      if (frame === this.data.selectedFrame) continue;
      var clonedPOI = _.cloneDeep(poi);
      frame.POIs.push(clonedPOI);
    }
    this.csssd();
  }

  applyHitboxToNextFrame(hitbox: Hitbox) {
    let nextFrame = this.data.selectedSprite.frames[this.data.selectedFrameIndex + 1];
    if (nextFrame) {
      nextFrame.hitboxes.push(_.cloneDeep(hitbox));
    }
    this.csssd();
  }

  isAllowedAction() {
    if (this.config?.isInSpriteModFolder === true && !this.isMapSpriteEditor) {
      window.Main.showError("Unsupported Action", "Non-visual modifications not allowed in sprites_visualmods folder.");
      return false;
    }
    return true;
  }

  get selectedSpritesheet() {
    return global.spritesheetMap[Helpers.getNormalizedSpritesheetName(this.customMapContext, this.data.selectedSpritesheetPath)];
  }

  getZoom(): number {
    return this.spriteCanvas.zoom;
  }

  setZoom(zoomLevel: number) {
    if (isNaN(zoomLevel)) return;
    if (zoomLevel < 1) zoomLevel = 1;
    if (zoomLevel > 10) zoomLevel = 10;
    this.spriteCanvas.zoom = zoomLevel;
    this.config.lastZoom = zoomLevel;
    this.saveConfig();
    this.redraw();
    this.forceUpdate();
  }

  getSelectedPixels() {
    if(!this.selectedSpritesheet || !this.spritesheetCanvas) return;
    let rect = getSelectedPixelRect(this.spritesheetCanvas.dragLeftX, this.spritesheetCanvas.dragTopY, this.spritesheetCanvas.dragRightX, this.spritesheetCanvas.dragBotY, this.selectedSpritesheet.imgArr);
    if(rect) {
      this.data.pendingFrame = new Frame(rect as Rect, 0.066, new Point(0,0));
      this.changeState();
    }
  }

  mainLoop(spriteEditor: SpriteEditor) {
    let state = spriteEditor.data;
    if (state.isAnimPlaying && state.selectedSprite) {
      this.animTime += 1000 / 60;
      let frames = state.selectedSprite.frames;
      if(this.animTime >= frames[this.animFrameIndex].duration * 1000) {
        this.animFrameIndex++;
        if(this.animFrameIndex >= frames.length) {
          this.animFrameIndex = 0;
        }
        this.animTime = 0;
      }
      this.spriteCanvas?.redraw();
    }
  }

  getVisibleHitboxes() {
    let hitboxes: Hitbox[] = [];
    if(this.data.selectedSprite) {
      hitboxes = hitboxes.concat(this.data.selectedSprite.hitboxes);
    }
    if(this.data.selectedFrame) {
      hitboxes = hitboxes.concat(this.data.selectedFrame.hitboxes);
    }
    return hitboxes;
  }

  getNoHeadshotSprites() {
    let ignoredSprites = [
      "axl_hyper_start",
      "axl_hyper_start_air",
      "axl_rocket",
      "axl_rocket_explosion",
      "axl_rocket_explosion_hyper",
      "axl_roll",
      "axl_transform",
      "menu_axl_hidden",
      "mmx_gigacrush",
      "vile_bomb_air",
      "vile_bomb_ground",
      "vile_ebomb_proj",
      "vile_ebomb_start",
      "vile_mk2_proj",
      "vile_mk2_proj_fade",
      "vile_revive",
      "vile_revive_end",
      "vile_stun_shot",
      "vile_stun_shot2",
      "vile_stun_shot_fade",
      "vile_stun_static",
      "zero_hyper_start",
      "zero_rakuhouha",
      "zero_rekkoha",
      "vilemk2_win2",
      "hud_axl_aim",
      "zero_cflasher",
      "zero_awakened_",
      "sigma_cloak",
    ];
    let missingSprites = [];
    for (let sprite of global.sprites) {
      if (!sprite.name.includes("zero_") && !sprite.name.includes("mmx_") && !sprite.name.includes("vile_") && !sprite.name.includes("axl_") && !sprite.name.includes("vilemk2_") && !sprite.name.includes("sigma_") && !sprite.name.includes("sigma2_") && !sprite.name.includes("sigma3_") && !sprite.name.includes("vilemk5_")) {
        continue;
      }
      if (sprite.name.includes("axl_arm") || sprite.name.includes("_ra_") || sprite.name.includes("axl_bullet") || sprite.name.includes("axl_cursor") || sprite.name.includes("_die")
      || sprite.name.includes("axl_pistol") || sprite.name.includes("raygun") || sprite.name.includes("grenade") || sprite.name.includes("warp_") || sprite.name.includes("zero_awakened")
      || sprite.name.includes("mmx_revive") || sprite.name.includes("_proj") || sprite.name.includes("nova_strike") || sprite.name.includes("axl_hyper_start") || sprite.name.includes("axl_roll")
      || sprite.name.includes("genmu") || sprite.name.includes("sigma_head") || sprite.name.includes("sigma_wolf") || sprite.name.includes("sigma2_viral") || sprite.name.includes("fakezero")
      || sprite.name.includes("_rc_") || sprite.name.includes("hud") || sprite.name.includes("sigma2_revive") || sprite.name.includes("zero_hyper_start2") || sprite.name.includes("kaiser")
      || sprite.name.includes("sigma2_tank") || sprite.name.includes("sigma2_hopper") || sprite.name.includes("sigma2_bird") || sprite.name.includes("sigma2_fish") || sprite.name.includes("sigma2_ball")
      || sprite.name.includes("revive_to5") || sprite.name.includes("sigma3_revive")
      || ignoredSprites.includes(sprite.name)) {
        continue;
      }
      for (let frame of sprite.frames) {
        if (!_.some(frame.POIs, p => p.tags == 'h')) {
          missingSprites.push(sprite.name);
          break;
        }
      }
    }
    console.log(missingSprites);
  }

}