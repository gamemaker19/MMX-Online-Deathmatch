export class Config {

  isProd: boolean;
  lastSpriteFilter: string = "";
  lastSpriteFilterMode: string = "";
  lastLevelFilter: string = "";
  lastLevelFilterMode: string = "";
  assetPath: string = "";
  isInMapModFolder: boolean;
  isInSpriteModFolder: boolean;
  version: string = "";
  lastZoom: number;
  mapCanvasWidth: number;
  mapCanvasHeight: number;
  lastMapZoom: number;
  optimizedMode: boolean;

  constructor() {
  }

}