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
import { global } from "../Global";
import * as LevelEditorJsx from "./levelEditorJsx";
import { classToPlain, plainToClass, serialize } from 'class-transformer';
import { Rect } from "../models/Rect";
import { Obj } from "../models/Obj";
import { LevelCanvas } from "./levelCanvas";
import { CreateInstanceTool, CreateTool, SelectTool, Tool } from "./tool";
import { LevelEditorInput } from "./levelEditorInput";
import { Level } from "../models/Level";
import { Instance } from "../models/Instance";
import { Config } from "../config";
import { BaseEditor } from "../baseEditor";
import { NavMeshNeighbor, NavMeshNode } from "../models/NavMeshNode";
import { get2DArrayFromImage } from "../pixelClump";

export class LevelEditorState {
  levelFilter: string = "";
  selectedFilterMode: string = "contains";
  newLevelName: string = "";
  newLevelActive: boolean = false;
  modifiedLevels: Level[] = [];
  selectedLevelIndex: number = -1;
  lastObjIndex: number = -1;
  selectedObjectIndex: number = -1;
  selectedInstanceIds: number[] = [];
  hideGizmos: boolean = false;
  showInstanceLabels: boolean = true;
  toggleShowCamBounds: boolean = false;
  showBackground: boolean = true;
  showBackwall: boolean = true;
  showForeground: boolean = true;
  showParallaxes: boolean = true;
  snapCollision: boolean = false;
  showWallPaths: boolean = false;

  get selectedInstances(): Instance[] {
    let instances: Instance[] = [];
    if (!this.selectedLevel?.instances) {
      return instances;
    }
    for (let instance of this.selectedLevel.instances) {
      if (this.selectedInstanceIds.indexOf(instance.id) !== -1) {
        instances.push(instance);
      }
    }
    return instances;
  }

  isLevelDirty(level: Level): boolean {
    return _.some(this.modifiedLevels, s => s.path === level.path && !s.isMirrorJson() && s.isDirty);
  }

  isSelectedLevelDirty(): boolean {
    return this.isLevelDirty(this.selectedLevel);
  }

  setLevelDirty(level: Level, isDirty: boolean) {
    let isLevelDirty = this.isLevelDirty(level);
    if (isDirty && !isLevelDirty) {
      let modifiedLevel = _.find(this.modifiedLevels, s => s.path === level.path);
      if (modifiedLevel) {
        modifiedLevel.isDirty = true;
      } else {
        let clonedLevel = _.cloneDeep(level);
        clonedLevel.isDirty = true;
        this.modifiedLevels.push(clonedLevel);
      }
    }
    else if (!isDirty && isLevelDirty) {
      let modifiedLevel = _.find(this.modifiedLevels, s => s.path === level.path);
      if (modifiedLevel) {
        modifiedLevel.isDirty = false;
      }
    }
  }

  setSelectedLevelDirty(isDirty: boolean) {
    this.setLevelDirty(this.selectedLevel, isDirty);
  }

  get selectedLevel(): Level {
    if (this.selectedLevelIndex === -1) return undefined;
    let globalLevel = global.levels[this.selectedLevelIndex];

    let modifiedLevel = _.find(this.modifiedLevels, s => s.path === globalLevel.path);
    if (!modifiedLevel) {
      let clonedLevel = _.cloneDeep(globalLevel);
      this.modifiedLevels.push(clonedLevel);
      return clonedLevel;
    }

    return modifiedLevel;
  }

  get selectedObject(): Obj {
    if (this.selectedObjectIndex === -1) return undefined;
    return global.objects[this.selectedObjectIndex];
  }

  get lastObj(): Obj {
    if (this.lastObjIndex === -1) return undefined;
    return global.objects[this.lastObjIndex];
  }
};

export class LevelEditor extends BaseEditor<LevelEditorState> {
  
  levelCanvas: LevelCanvas;
  tool: Tool;
  input: LevelEditorInput;
  sprites: Sprite[] = [];

  canvasWidth = 1100;
  canvasHeight = 606;

  hiddenCanvas: HTMLCanvasElement;
  hiddenCtx: CanvasRenderingContext2D;

  optimizedMode: boolean;
  consoleCommand: string = "";
  consoleCommandResult: string = "";

  constructor(props: {}) {
    super(props);
    this.data = new LevelEditorState();
    this.tool = new SelectTool(this);

    window.onerror = function(error) {
      //window.Main.showDialog("Error", error.toString());
    };
  }

  // "Change State Selected Level Dirty"
  cssld() : void {
    this.data.setSelectedLevelDirty(true);
    this.changeState();
  }

  componentDidMount() {

    this.levelCanvas = new LevelCanvas(this);
    this.input = new LevelEditorInput(this);
    
    window.Main.on("requestDirtyState", () => {
      window.Main.sendHasDirty(this.isAnyLevelDirty());
    });

    this.hiddenCanvas = document.createElement('canvas') as HTMLCanvasElement;
    this.hiddenCtx = this.hiddenCanvas.getContext("2d") as CanvasRenderingContext2D;    
    this.hiddenCtx.imageSmoothingEnabled = false;

    this.isLoading = true;
    this.fetchAllData();
  }

  async fetchAllData() {
    try {
      let configData = await window.Main.getConfig();
      
      this.config = plainToClass(Config, configData);
      this.data.levelFilter = this.config.lastLevelFilter || "";
      this.data.selectedFilterMode = this.config.lastLevelFilterMode || "contains";
      this.canvasWidth = this.config.mapCanvasWidth || 1100;
      this.canvasHeight = this.config.mapCanvasHeight || 606;
      this.setZoom(this.config.lastMapZoom || 1);
      this.setOptimizedMode(this.config.optimizedMode, true);

      let spritesheetsData = await window.Main.getSpritesheets();

      _.each(spritesheetsData, (spritesheet: string) => {
        let spritesheetObj = new Spritesheet(spritesheet);
        spritesheetObj.loadImage();
        global.spritesheetMap[Helpers.getNormalizedSpritesheetName(undefined, spritesheet)] = spritesheetObj;
      });

      let spritesData = await window.Main.getSprites();

      for (let spriteData of spritesData) {
        // @ts-ignore
        let sprite: Sprite = plainToClass(Sprite, spriteData, { excludeExtraneousValues: true });
        global.sprites.push(sprite);
      }

      let backgrounds = await window.Main.getBackgrounds();
      _.each(backgrounds, (background: string) => {
        global.backgroundMap[Helpers.getAssetPath(background)] = new Spritesheet(background);
      });

      let levelsData = await window.Main.getLevels() as Level[];
      let levels = plainToClass(Level, levelsData, { excludeExtraneousValues: true });
      
      global.levels = levels;

      this.isLoading = false;
      this.changeState();
    }
    catch (error) {
      window.Main.showError("Error", error.toString());
      this.isLoading = false;
      this.forceUpdate();
    }
  }

  runConsoleCommand() {
    let pieces = this.consoleCommand.split(' ');
    if (pieces.length === 0) return;
    if (pieces[0] === "moveall" && pieces[1] && pieces[2]) {
      let x = Number(pieces[1]);
      let y = Number(pieces[2]);
      for (let instance of this.data?.selectedLevel?.instances ?? []) {
        instance.move(x, y);
      }
      this.consoleCommandResult = "Successfully ran command.";
      this.cssld();
      return;  
    }
    this.consoleCommandResult = "Invalid command.";
    this.forceUpdate();
  }

  unhideAll() {
    for (let instance of this.data.selectedLevel.instances) {
      instance.hidden = false;
    }
    this.changeState();
  }

  setOptimizedMode(val: boolean, dontSave: boolean = false) {
    this.levelCanvas.setOptimizedMode(val);
    this.optimizedMode = val;
    this.forceUpdate();
    this.redraw();
    if (dontSave === false) {
      this.config.optimizedMode = val;
      this.saveConfig();
    }
  }

  isOptimizedMode() {
    return this.optimizedMode;
  }

  setShowWallPaths(checked: boolean) {
    if (checked) {
      this.data.selectedLevel.computeWallPaths();
    }
    this.data.showWallPaths = checked;
    this.cssld();
  }

  refreshShowWallPaths() {
    this.data.selectedLevel.computeWallPaths();
    this.cssld();
  }

  getMapSpriteOptions() {
    let mapSpriteOptions; 
    if (this.config.isInMapModFolder) {
      mapSpriteOptions = global.sprites.filter(s => s.name.startsWith(this.data.selectedLevel.name + ":")).map(s => s.name);
    }
    else {
      mapSpriteOptions = global.sprites.filter(s => s.name.startsWith("ms_")).map(s => s.name);
    }
    mapSpriteOptions.unshift("");
    return mapSpriteOptions;
  }

  render(): JSX.Element {
    return LevelEditorJsx.render(this);
  }
  
  fastScroll(x: number, y: number): void {
    this.levelCanvas.fastScroll(x, y);
    this.redraw();
  }

  fastScrollPage(x: number, y: number): void {
    this.levelCanvas.fastScrollPage(x, y);
    this.redraw();
  }

  fastScrollStartEnd(x: number, y: number): void {
    this.levelCanvas.fastScrollStartEnd(x, y);
    this.redraw();
  }

  changeLevelFilter(value: string) {
    this.data.levelFilter = value;
    this.config.lastLevelFilter = value;
    this.saveConfig();
    this.changeState();
  }

  changeLevelFilterMode(value: string) {
    this.data.selectedFilterMode = value;
    this.config.lastLevelFilterMode = value;
    this.saveConfig();
    this.changeState();
  }

  getFilteredLevels(): Level[] {
    let filters = this.data.levelFilter.split(",");
    if (filters[0] === "") return global.levels;
    return _.filter(global.levels, (level: Level) => {
      if (this.data.selectedFilterMode === "exactmatch") {
        return filters.indexOf(level.name) >= 0;
      }
      else if (this.data.selectedFilterMode === "contains") {
        for (let filter of filters) {
          if (level.name.toLowerCase().includes(filter.toLowerCase())) {
            return true;
          }
        }
      }
      else if (this.data.selectedFilterMode === "startswith") {
        for (let filter of filters) {
          if (level.name.startsWith(filter)) {
            return true;
          }
        }
      }
      else if(this.data.selectedFilterMode === "endswith") {
        for (let filter of filters) {
          if (level.name.endsWith(filter)) {
            return true;
          }
        }
      }
      return false;
    });
  }
  
  onSwapFolderClick() {
    if (!this.config) return;
    if (this.config.isInMapModFolder) {
      this.config.isInMapModFolder = false;
      this.saveConfigAndReload();
    }
    else {
      this.config.isInMapModFolder = true;
      this.saveConfigAndReload();
    }
  }
  getSwapFolderButtonText() {
    if (!this.config) return "Loading...";
    if (this.config.isInMapModFolder) {
      return "Change to maps folder";
    }
    return "Change to maps_custom folder";
  }

  saveConfig() {
    let jsonStr = JSON.stringify(classToPlain(this.config), null, 2);
    window.Main.saveConfig(jsonStr);
  }

  saveConfigAndReload() {
    let jsonStr = JSON.stringify(classToPlain(this.config), null, 2);
    window.Main.saveConfigAndReload({ config: jsonStr, isDirty: this.isAnyLevelDirty() });
  }

  reload() {
    window.Main.reload({ isDirty: this.isAnyLevelDirty() });
  }

  async setupMapSpriteEditor() {
    try {
      await window.Main.setupMapSpriteEditor({ isDirty: this.isAnyLevelDirty(), path: this.data.selectedLevel.path });
    }
    catch (error) {
      window.Main.showError("Error", error.toString());
    }
  }

  isAnyLevelDirty(): boolean {
    return this.data.modifiedLevels.some(l => l.isDirty && !l.isMirrorJson());
  }

  get availableBackgrounds() {
    return _.filter(global.backgroundMap, (val, key) => {
      return val.path.includes("/" + this.data.selectedLevel.name + "/") || val.path.includes("maps_shared");
    });
  }

  get selectedBackground() {
    if (!this.data.selectedLevel?.backgroundPath) return undefined;
    return global.backgroundMap[this.data.selectedLevel.backgroundPath];
  }

  get selectedBackground2() {
    if (!this.data.selectedLevel?.backwallPath) return undefined;
    return global.backgroundMap[this.data.selectedLevel.backwallPath];
  }

  get selectedForeground() {
    if (!this.data.selectedLevel?.foregroundPath) return undefined;
    return global.backgroundMap[this.data.selectedLevel.foregroundPath];
  }

  selectedParallax(index: number) {
    if (!this.data.selectedLevel?.parallaxes[index]?.path) return undefined;
    return global.backgroundMap[this.data.selectedLevel.parallaxes[index].path];
  }

  getZoom(): number {
    return this.levelCanvas.zoom;
  }

  setZoom(zoomLevel: number) {
    if (isNaN(zoomLevel)) return;
    if (zoomLevel < 1) zoomLevel = 1;
    if (zoomLevel > this.levelCanvas.maxZoom) zoomLevel = this.levelCanvas.maxZoom;
    this.levelCanvas.setZoom(zoomLevel);
    this.forceUpdate();
    this.config.lastMapZoom = zoomLevel;
    this.saveConfig();
  }

  changeCanvasWidth(width: number) {
    this.canvasWidth = width;
    this.levelCanvas.changeCanvasSize();
    this.forceUpdate();
    this.config.mapCanvasWidth = width;
    this.saveConfig();
    setTimeout(() => this.redraw(), 100);
  }

  changeCanvasHeight(height: number) {
    this.canvasHeight = height;
    this.levelCanvas.changeCanvasSize();
    this.forceUpdate();
    this.config.mapCanvasHeight = height;
    this.saveConfig();
    setTimeout(() => this.redraw(), 100);
  }

  switchTool(newTool: Tool) {
    this.tool = newTool;
    this.levelCanvas.canvas.style.cursor = newTool.cursor;
    this.redraw();
  }

  sortInstances() {
    this.data.selectedLevel.instances.sort(function(a, b) {
      if (a.obj.zIndex < b.obj.zIndex) return -1;
      if (a.obj.zIndex > b.obj.zIndex) return 1;

      var compare = a.name.localeCompare(b.name, "en", { numeric: true });
      if(compare < 0) return -1;
      if(compare > 0) return 1;
      if(compare === 0) return 0;
    });

    this.cssld();
  }

  changeProperties (value: string) {
    if(this.data.selectedInstances.length === 1) {
      this.data.setSelectedLevelDirty(true);
      this.data.selectedInstances[0].setPropertiesJson(value);
      this.changeState();
      this.forceUpdate();
    }
  }

  setProperty (key: string, value: any) {
    if(this.data.selectedInstances.length !== 1) return;
    this.data.setSelectedLevelDirty(true);
    this.data.selectedInstances[0].addProperty(key, value);
    this.changeState();
  }

  removeProperty (key: string) {
    if(this.data.selectedInstances.length !== 1) return;
    this.data.setSelectedLevelDirty(true);
    this.data.selectedInstances[0].removeProperty(key);
    this.changeState();
  }

  getPropertyValue(key: string) {
    return this.data.selectedInstances[0].properties?.[key];
  }
  
  propertyExists (key: string) {
    return this.data.selectedInstances[0].properties?.[key] !== undefined;
  }

  getPropertyButtonStyle (key: string) {
    if (this.propertyExists(key)) {
      return "selected-button";
    }
    return "";
  }

  getNeighborButtonStyle (neighbor: any, key: string) {
    if (neighbor[key] !== undefined) {
      return "selected-button";
    }
    return "";
  }

  removeNeighbor (instance: Instance, neighborName: string) {
    let navMeshNode1 = (instance.properties || {}) as NavMeshNode;
    navMeshNode1.neighbors = navMeshNode1.neighbors || [];
    _.remove(navMeshNode1.neighbors, n => n.nodeName === neighborName);
    instance.onUpdatePropertiesJson();
    this.cssld();
  }

  setNeighborProperty (neighbor: any, key: string, value: any, deleteIfExists: boolean) {
    if(this.data.selectedInstances.length !== 1) return;
    
    let selectedInstance = this.data.selectedInstances[0];
    if (neighbor[key] !== value) {
      neighbor[key] = value;
    }
    else if (deleteIfExists) {
      delete neighbor[key];
    }
    selectedInstance.onUpdatePropertiesJson();

    this.cssld();
  }
  
  changeWidth(width: number) {
    this.data.selectedLevel.width = width;
    this.cssld();
  }

  changeHeight(height: number) {
    this.data.selectedLevel.height = height;
    this.cssld();
  }

  onBackgroundChange(newBackgroundPath: string) {
    
    this.data.selectedLevel.backgroundPath = newBackgroundPath;

    let newBackground = global.backgroundMap[newBackgroundPath];
    if (!newBackground) {
      // window.Main.showDialog("Error", "Map " + this.data.selectedLevel.name + " is missing background.png image file.");
      return;
    }

    if (newBackground.imgEl) {
      this.changeState();
      this.redraw(true);
      return;
    }

    this.isLoading = true;
    let backgroundImg = document.createElement("img");
    backgroundImg.onload = () => {
      this.data.selectedLevel.width = backgroundImg.width;
      this.data.selectedLevel.height = backgroundImg.height;
      newBackground.imgEl = backgroundImg;
      this.isLoading = false;
      this.changeState();
      this.forceUpdate();
      this.redraw(true);
    };

    backgroundImg.onerror = () => this.onImageError(newBackground);
    backgroundImg.src = "file:///" + newBackground.path;
  }

  commonBackgroundChange(newBackgroundPath: string, loadPixelClumpArray: boolean = false) {
    if (!newBackgroundPath) {
      this.changeState();
      this.redraw(true);
      return;
    }

    let newBackground = global.backgroundMap[newBackgroundPath];
    if (!newBackground) return;

    if (newBackground.imgEl) {
      this.changeState();
      this.redraw(true);
      return;
    }

    this.isLoading = true;
    let backgroundImg = document.createElement("img");
    backgroundImg.onload = () => {
      newBackground.imgEl = backgroundImg;

      if (loadPixelClumpArray) {
        this.hiddenCanvas.width = backgroundImg.width;
        this.hiddenCanvas.height =  backgroundImg.height;
        this.hiddenCtx.drawImage(backgroundImg, 0, 0);
        let that = this;

        newBackground.lazyLoadImgArr = () => {
          let imageData = that.hiddenCtx.getImageData(0, 0, that.hiddenCanvas.width, that.hiddenCanvas.height);
          newBackground.imgArr = get2DArrayFromImage(imageData);
        };
      }

      this.isLoading = false;
      this.changeState();
      this.forceUpdate();
      this.redraw(true);
    };

    backgroundImg.onerror = () => this.onImageError(newBackground);
    backgroundImg.src = "file:///" + newBackground.path;
  }

  onBackwallChange(newBackgroundPath: string) {
    this.data.selectedLevel.backwallPath = newBackgroundPath;
    this.commonBackgroundChange(newBackgroundPath, true);
  }

  onForegroundChange(newBackgroundPath: string) {
    this.data.selectedLevel.foregroundPath = newBackgroundPath;
    this.commonBackgroundChange(newBackgroundPath);
  }

  onParallaxChange(index: number, newBackgroundPath: string) {
    this.data.selectedLevel.parallaxes[index].path  = newBackgroundPath;
    this.commonBackgroundChange(newBackgroundPath);
  }

  moveParallax(index: number, dir: number) {
    if (this.data.selectedLevel.parallaxes.length <= 1) return;
    if (index + dir >= this.data.selectedLevel.parallaxes.length) return;
    if (index + dir < 0) return;

    let temp = this.data.selectedLevel.parallaxes[index];
    this.data.selectedLevel.parallaxes[index] = this.data.selectedLevel.parallaxes[index + dir];
    this.data.selectedLevel.parallaxes[index + dir] = temp;
    this.cssld();
  }

  onImageError(newBackground: Spritesheet) {
    console.log("Error loading image " + newBackground.path);
    window.Main.showDialog("Error", "Error loading image " + newBackground.path);
    this.isLoading = false;
    this.forceUpdate();
  }

  getLevelDisplayName(level: Level) {
    return level.name + (this.data.isLevelDirty(level) ? '*' : '');
  }

  newLevel() {
    this.data.newLevelActive = true;
    this.changeState();
  }

  async addLevel() {
    // Validation
    if (!this.data.newLevelName) {
      window.Main.showDialog("Error", "Must provide a Level name", undefined, true);
      return;
    }
    if (_.some(global.levels, s => s.name.toLowerCase() === this.data.newLevelName.toLowerCase())) {
      window.Main.showDialog("Error", "Level name already exists", undefined, true);
      return;
    }
    if (this.data.newLevelName.toLowerCase() === "sprites") {
      window.Main.showDialog("Error", "Level name can't be 'sprites'", undefined, true);
      return;
    }
    if (this.data.newLevelName.toLowerCase().includes("mirrored")) {
      window.Main.showDialog("Error", "Level name can't include 'mirrored'", undefined, true);
      return;
    }
    if (this.data.newLevelName.toLowerCase().includes("inverted")) {
      window.Main.showDialog("Error", "Level name can't include 'inverted'", undefined, true);
      return;
    }
    if (!Helpers.isAlphaNumeric(this.data.newLevelName.replaceAll('_', ''))) {
      window.Main.showDialog("Error", "Level name must be alphanumeric'", undefined, true);
      return;
    }

    this.data.newLevelActive = false;
    let newLevel = new Level(this.data.newLevelName, 1000, 500);
    this.data.newLevelName = "";
    
    try {
      let plainLevel = classToPlain(newLevel);
      let data = await window.Main.addLevel(plainLevel);
      console.log("Successfully added level " + data.name);
      // @ts-ignore
      newLevel = plainToClass(Level, data, { excludeExtraneousValues: true });
      console.log(newLevel);
      global.levels.unshift(newLevel);
      this.changeLevel(newLevel);
      document.getElementsByClassName("sprite-list-scroll")[0].scrollTop = 0;
    }
    catch (error) {
      window.Main.showDialog("Error", error.toString(), undefined, true);
    }
  }

  async saveLevel() {

    this.data.selectedLevel.onBeforeSave();
    this.data.setSelectedLevelDirty(true);
    let validationErrors = this.data.selectedLevel.getValidationErrors();
    if (validationErrors) {
      window.Main.showDialog("Validation Errors", "Please fix the following errors:\n\n" + validationErrors);
      return;
    }

    if (this.data.selectedLevel.isMirrored()) {
      await this.saveMirroredLevel();
    }

    try {
      let plainLevel = classToPlain(this.data.selectedLevel);
      await window.Main.saveLevel(plainLevel);
      console.log("Successfully saved level " + this.data.selectedLevel.name);
      this.data.setSelectedLevelDirty(false);
      for (let i = 0; i < global.levels.length; i++) {
        if (global.levels[i].path === this.data.selectedLevel.path) {
          global.levels[i] = _.cloneDeep(this.data.selectedLevel);
          break;
        }
      }
      this.changeState();
    }
    catch (error) {
      window.Main.showError("Error", error.toString());
    }
  }

  async saveMirroredLevel() {
    let mirroredLevel: Level = this.data.selectedLevel.getMirrored();
    mirroredLevel.onBeforeSave();
    let plainLevel = classToPlain(mirroredLevel);
    try {
      await window.Main.saveLevel(plainLevel);
      console.log("Successfully saved mirrored level " + this.data.selectedLevel.name);
      let found = false;
      for (let i = 0; i < global.levels.length; i++) {
        if (global.levels[i].path === mirroredLevel.path) {
          this.data.setLevelDirty(global.levels[i], false);
          _.remove(this.data.modifiedLevels, level => level.path === mirroredLevel.path);
          global.levels[i] = mirroredLevel;
          found = true;
          break;
        }
      }
      if (!found) {
        let insertIndex = global.levels.findIndex(l => l.name === this.data.selectedLevel.name) + 1;
        global.levels.splice(insertIndex, 0, mirroredLevel);
      }
      this.changeState();
      this.forceUpdate();
    }
    catch (error) {
      window.Main.showError("Error", error.toString());
    }
  }

  changeLevel(newLevel: Level) {
    this.levelCanvas.resetFastScroll();
    this.data.selectedLevelIndex = global.levels.indexOf(newLevel);
    this.data.selectedObjectIndex = -1;
    this.data.selectedInstanceIds = [];

    this.onBackgroundChange(newLevel.backgroundPath);
    this.onBackwallChange(newLevel.backwallPath);
    this.onForegroundChange(newLevel.foregroundPath);
    for (let i = 0; i < newLevel.parallaxes.length; i++) {
      this.onParallaxChange(i, newLevel.parallaxes[i].path);
    }
    
    for (let instance of newLevel.instances) {
      instance.properties = instance.properties || {};
    }
  }

  async saveAllLevels() {
    for (let i = 0; i < global.levels.length; i++) {
      this.data.selectedLevelIndex = i;
      if (this.data.selectedLevel.isMirrorJson()) continue;
      await this.saveLevel();
    }
  }

  flipLevelX() {
    this.data.selectedLevel.flipMapX();
    this.changeState();
  }

  offsetAll() {
    var coords = prompt("Enter offset x,offset y", "0,0");
    if (coords !== null) {
      var x = Number(coords.split(',')[0]);
      var y = Number(coords.split(',')[1]);
      for (var instance of this.data.selectedLevel.instances) {
          if(instance.pos) {
            instance.pos.x += x;
            instance.pos.y += y;
          }
          else instance.move(x, y);
      }
      this.redraw();
    }
  }
  
  redraw(redrawBackgrounds: boolean = false) {
    if (redrawBackgrounds) {
      this.levelCanvas.redrawBackgrounds();
    }
    this.levelCanvas.redraw();
  }

  changeObject(newObj: Obj) {
    this.data.lastObjIndex = global.objects.indexOf(newObj);
    this.data.selectedInstanceIds = [];
    this.data.selectedObjectIndex = this.data.lastObjIndex;
    if (newObj.isShape) {
      this.tool = new CreateTool(this, newObj);
    }
    else {
      this.tool = new CreateInstanceTool(this);
    }
    this.forceUpdate();
  }

  onInstanceClick(instance: Instance) {
    this.data.selectedInstanceIds = [instance.id];
    this.changeState();
  }
}
