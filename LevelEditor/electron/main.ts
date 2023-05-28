import 'reflect-metadata';
import { app, BrowserWindow, dialog, ipcMain, Menu, webFrame } from "electron"
import _ from "lodash";
import { fileName, getAssetPath } from "./../src/helpers";
import stripJsonTrailingCommas from 'strip-json-trailing-commas';

let mainWindow: BrowserWindow | null;

declare const MAIN_WINDOW_WEBPACK_ENTRY: string
declare const MAIN_WINDOW_PRELOAD_WEBPACK_ENTRY: string

import * as fs from 'fs';
import * as path from 'path';

const isProd = process.env.NODE_ENV === "production";
const basePath = isProd ? process.resourcesPath: app.getAppPath()
let isLevelEditor = process.argv.length >= 2 && process.argv[2] === "levelEditor";
let forceClose = false;

let configPath = "";
let assetPath = "assets";
let spriteFolderName = "sprites";
let spritesheetFolderName = "spritesheets";
let levelFolderName = "maps";
let isMapSpritePath = false;

const template = [
  {
    label: 'View',
    submenu: [
      { role: 'resetZoom' },
      { role: 'zoomIn' },
      { role: 'zoomOut' },
    ]
  }
];

function createWindow () {
  mainWindow = new BrowserWindow({
    icon: path.join(basePath, "favicon.png"),
    width: 1920,
    height: 1080,
    title: isLevelEditor ? "MMXOD Map Editor" : "MMXOD Sprite Editor",
    backgroundColor: "white",
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      preload: MAIN_WINDOW_PRELOAD_WEBPACK_ENTRY,
      webSecurity: isProd ? true : false
    }
  });

  mainWindow.maximize();
  mainWindow.loadURL(MAIN_WINDOW_WEBPACK_ENTRY);
  
  if (!isProd) {
    mainWindow.webContents.openDevTools();
  } else {
    //@ts-ignore
    let menu = Menu.buildFromTemplate(template);
    Menu.setApplicationMenu(menu);
  }

  mainWindow.on("close", function(e) {
    if (forceClose) {
      return;
    }
    mainWindow?.webContents?.send("requestDirtyState");
    e.preventDefault();
  });

  mainWindow.on("closed", (e) => {
    mainWindow = null;
  });
}

function saveSpriteHelper(req) {
  let name = req.name;
  delete req["name"];
  let jsonStr = JSON.stringify(req);
  let savePath = getSpritePath() + name + ".json";
  // console.log("Saving sprite to " + savePath);
  fs.writeFileSync(savePath, jsonStr);
}

function mapName(filepath: string) {
  if (!filepath) return filepath;
  let pieces = filepath.split("/");
  if (filepath.endsWith("mirrored.json")) { 
    return pieces[pieces.length - 2] + "_mirrored";
  }
  return pieces[pieces.length - 2];
}

function onConfigModFolderChange(config: any) {
  if (config.isInMapModFolder) {
    levelFolderName = "maps_custom";
  }
  else {
    levelFolderName = "maps";
  }

  if (config.isInSpriteModFolder && !isLevelEditor) {
    spriteFolderName = "sprites_visualmods";
  }
  else {
    spriteFolderName = "sprites";
  }
}

function getSpritePath() {
  return assetPath + `/${spriteFolderName}/`;
}

function getSpritesheetPath() {
  return assetPath + `/${spritesheetFolderName}/`;
}

function walkSync(dir, filelist) {
  let files = fs.readdirSync(dir);
  filelist = filelist || [];
  files.forEach(function(file) {
    if (fs.statSync(dir + file).isDirectory()) {
      filelist = walkSync(dir + file + '/', filelist);
    }
    else {
      filelist.push(dir + file);
    }
  });
  return filelist;
};

function getSpriteHelper(dirname: string, filename: string) {
  if (!filename.endsWith(".json")) return undefined;
  let content = fs.readFileSync(dirname + filename, "utf-8");
  let sprite = jsonParseHelper(content);
  sprite.name = filename.split(".")[0];
  sprite.spritesheetPath = fileName(sprite.spritesheetPath);
  if (dirname.includes("/maps_custom/")) {
    let customMapName = dirname.split("/maps_custom/")[1].split("/")[0];
    sprite.customMapName = customMapName;
  }
  return sprite;
}

function getSpritesHelper(dirname: string) {
  let sprites: any[] = [];
  let fileNames = fs.readdirSync(dirname);
  for (let filename of fileNames) {
    let sprite = getSpriteHelper(dirname, filename);
    if (sprite) {
      sprites.push(sprite);
    }
  }
  return sprites;
}

function getSpritesheetsHelper(dirname: string) {
  let paths: string[] = [];
  let fileNames = fs.readdirSync(dirname);
  for (let filename of fileNames) {
    if (filename.toLowerCase().endsWith(".png")) {
      paths.push(dirname + filename);
    }
  }
  return paths;
}

function jsonParseHelper(jsonStr: string) {
  jsonStr = stripJsonTrailingCommas(jsonStr);
  const data = JSON.parse(jsonStr);
  return data;
}

let api = {
  sendHasDirty: (isDirty: boolean) => {
    if (!mainWindow) return;
    if (isDirty) {
      let choice = dialog.showMessageBoxSync(mainWindow, {
        type: 'question',
        buttons: ['Yes', 'No'],
        title: 'Confirm Quit',
        message: 'You have unsaved changes. Are you sure you want to quit?\nAny unsaved changes will be lost.'
      });
      if (choice === 0) {
        forceClose = true;
        mainWindow.close();
      }
    } else {
      forceClose = true;
      mainWindow.close();
    }
  },

  showDialog: (title: string, message: string, buttons: string[], isError: boolean, isCrash: boolean) => {
    if (!mainWindow) return;
    let selectedButtonIndex = dialog.showMessageBoxSync(mainWindow, {
      buttons: buttons,
      message: message,
      title: title,
    });
    return selectedButtonIndex;
  },

  getEditorType: () => {
    let type = 0;
    if (isLevelEditor) {
      type = 1;
    }
    return type;
  },

  getCustomMapContext: () => {
    if (isMapSpritePath) {
      return mapName(spriteFolderName);
    }
    return "";
  },

  getConfig: () => {
    if (!mainWindow) return;
    configPath = "config.json";
    let documentsPath = app.getPath("documents").replace(/\\/g, "/");
    let documentsConfigPath = documentsPath + "/MMXOD Editor";
    if (fs.existsSync(documentsConfigPath)) {
      configPath = documentsConfigPath + "/" + configPath;
    }

    let config: any = {
      assetPath: "",
      allowUnsupportedActions: isProd === false,
      lastSpriteFilterMode: "contains",
      isInMapModFolder: true,
      isInSpriteModFolder: true,
    };

    if (!fs.existsSync(configPath)) {
      fs.writeFileSync(configPath, JSON.stringify(config, null, 2));
    }
    else {
      config = jsonParseHelper(fs.readFileSync(configPath, "utf-8"));
    }

    if (!isMapSpritePath) {
      onConfigModFolderChange(config);
    }

    if (config.assetPath) {
      assetPath = config.assetPath;
    }
    else {
      dialog.showMessageBoxSync(mainWindow, {
        message: "You will be prompted to select an assets folder that contains the sprite and map folders.",
        title: "First time setup required",
      });
      var paths = dialog.showOpenDialogSync({
        title: "Please select an assets folder.",
        properties: ['openDirectory'],
      });
      if (paths && paths[0]) {
        assetPath = paths[0].replace(/\\/g, "/");
      } else {
        assetPath = "";
      }

      if (!assetPath) {
        throw "Asset path not selected. Cannot proceed.";
      }

      if (!assetPath.endsWith("/assets")) {
        throw "The path doesn't end with /assets.";
      }

      config.assetPath = assetPath;
      fs.writeFileSync(configPath, JSON.stringify(config, null, 2));
    }

    config.isProd = isProd;
    config.version = app.getVersion();
    return config;
  },

  saveConfig: (config: any) => {
    fs.writeFileSync(configPath, config);
  },

  saveConfigAndReload: (message: any) => {
    if (!mainWindow) return;
    let selectedButtonIndex = 0;
    if (message.isDirty) {
      selectedButtonIndex = dialog.showMessageBoxSync(mainWindow, {
        buttons: ["OK", "Cancel"],
        message: "You will lose any unsaved changes.",
        title: "Are you sure?",
      });
    }
    
    if (selectedButtonIndex === 0) {
      fs.writeFileSync(configPath, message.config);
      onConfigModFolderChange(message.config);
      mainWindow.reload();
    }
  },

  zoom: (zoom: number) => {
    webFrame.setZoomFactor(zoom);
  },

  reload: (message: any) => {
    if (!mainWindow) return;
    let selectedButtonIndex = 0;
    if (message.isDirty) {
      selectedButtonIndex = dialog.showMessageBoxSync(mainWindow, {
        buttons: ["OK", "Cancel"],
        message: "You will lose any unsaved changes.",
        title: "Are you sure?",
      });
    }
    if (selectedButtonIndex === 0) {
      if (isMapSpritePath) {
        isMapSpritePath = false;
        isLevelEditor = true;
      }
      mainWindow.reload();
    }
  },

  getIsMapSpriteEditor: () => {
    return isMapSpritePath;
  },

  setupMapSpriteEditor: (message: any) => {
    if (!mainWindow) return;
    let selectedButtonIndex = 0;
    if (message.isDirty) {
      selectedButtonIndex = dialog.showMessageBoxSync(mainWindow, {
        buttons: ["OK", "Cancel"],
        message: "You will lose any unsaved changes.",
        title: "Are you sure?",
      });
    }
    let mapFolderPath = path.dirname(message.path) + "/sprites";
    let spritesheetPath: string = assetPath + "/" + mapFolderPath + "/spritesheet.png";
    if (!fs.existsSync(spritesheetPath)) {
      throw `Sprites folder "${spritesheetPath}" does not exist with a spritesheet.png file.`;
    }
    if (selectedButtonIndex === 0) {
      isLevelEditor = false;
      isMapSpritePath = true;
      spriteFolderName = mapFolderPath;
      spritesheetFolderName = mapFolderPath;
      mainWindow.reload();
    }
  },

  getSprites: () => {
    let sprites = getSpritesHelper(getSpritePath());

    // Get custom map sprites
    if (isLevelEditor && levelFolderName === "maps_custom") {
      sprites = [];
      let dirname = assetPath + `/${levelFolderName}/`;
      let filePaths: string[] = walkSync(dirname, []);
      for (let filepath of filePaths) {
        if (!filepath.includes("/sprites/")) continue;
        if (!filepath.endsWith(".json")) continue;
        let mapSprite = getSpriteHelper(path.dirname(filepath) + "/", path.basename(filepath));
        if (mapSprite) {
          let pieces = filepath.split('/');
          let mapName = pieces[pieces.length - 3]
          mapSprite.name = mapName + ":" + mapSprite.name;
          sprites.push(mapSprite);
        }
      }
    }

    return sprites;
  },

  getSpritesheets: () => {
    let spritesheets = getSpritesheetsHelper(getSpritesheetPath());

    // Get custom map spritesheets
    if (isLevelEditor && levelFolderName === "maps_custom") {
      spritesheets = [];
      let dirname = assetPath + `/${levelFolderName}/`;
      let filePaths: string[] = walkSync(dirname, []);
      for (let filepath of filePaths) {
        if (!filepath.includes("/sprites/")) continue;
        if (filepath.toLowerCase().endsWith(".png")) {
          spritesheets.push(filepath);
        }
      }
    }

    return spritesheets;
  },

  saveSprite: (message: any) => { 
    saveSpriteHelper(message);
  },

  saveSprites: (message: any) => { 
    for (var sprite of message) {
      saveSpriteHelper(sprite);
    }
  },

  getLevels: () => {
    let dirname = assetPath + `/${levelFolderName}/`;
    let levels: any[] = [];
    let filePaths: string[] = walkSync(dirname, []);

    for (let filepath of filePaths) {
      if (!filepath.endsWith(".json")) continue;
      if (filepath.includes("/sprites/")) continue;
      let content = fs.readFileSync(filepath, 'utf-8');
      let level = jsonParseHelper(content);
      level.name = mapName(filepath);
      level.path = getAssetPath(filepath);
      levels.push(level);
    }
    return levels;
  },

  getBackgrounds: () => {
    var paths: string[] = [];
    let filePaths = walkSync(assetPath + `/${levelFolderName}/`, []);
    for (let filepath of filePaths) {
      if (!filepath.endsWith(".png")) continue;
      if (filepath.includes("thumbnails")) continue;
      paths.push(filepath);
    }
    filePaths = walkSync(assetPath + "/maps_shared/", []);
    for (let filename of filePaths) {
      if (!filename.endsWith(".png")) continue;
      if (filename.includes("thumbnails")) continue;
      paths.push(filename);
    }
    return paths;
  },

  saveLevel: (message) => {
    let jsonStr = JSON.stringify(message, null, 2);
    let finalPath = assetPath + "/" + message.path;
    fs.writeFileSync(finalPath, jsonStr);
  },

  saveMirroredLevel: (message) => {
    let jsonStr = JSON.stringify(message, null, 2);
    let finalPath = assetPath + "/" + message.path;
    fs.writeFileSync(finalPath, jsonStr);
    return message;
  },

  addLevel: (message) => {
    let folderPath = assetPath + `/${levelFolderName}/` + message.name;
    if (!fs.existsSync(folderPath)) {
      fs.mkdirSync(folderPath);
    }
    let finalPath = folderPath + "/map.json";
    message.path = `${levelFolderName}/${message.name}/map.json`;
    let jsonStr = JSON.stringify(message, null, 2);
    fs.writeFileSync(finalPath, jsonStr);
    return message;
  },
}

async function registerListeners () {
  for (let key of Object.keys(api)) {
    let func = api[key] as Function;
    ipcMain.on(key, (_, ...args: any[]) => {
      try {
        let result = func(...args);
        mainWindow?.webContents?.send(key, result);
      }
      catch (error) {
        mainWindow?.webContents?.send(key + "Error", error);
      }
    });
  }
}

app.on('ready', createWindow)
  .whenReady()
  .then(registerListeners)
  .catch(e => console.error(e));

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
})

app.on('activate', () => {
  if (BrowserWindow.getAllWindows().length === 0) {
    createWindow();
  }
})