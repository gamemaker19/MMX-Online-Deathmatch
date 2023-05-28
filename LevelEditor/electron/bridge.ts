import { contextBridge, ipcRenderer, webFrame } from "electron"

let callbackMap: { [path: string]: any } = {};
export function on(callbackName: string, callbackFunc: Function) {
  let wrapperFunc = (_: any, data: any) => callbackFunc(data);
  if (callbackMap[callbackName]) {
    ipcRenderer.off(callbackName, callbackMap[callbackName]);
  }
  callbackMap[callbackName] = wrapperFunc;
  ipcRenderer.on(callbackName, wrapperFunc);
}

function send(name: string, ...args: any[]) {
  return new Promise(function (resolve: (value: any) => void, reject: (value: any) => void) {
    on(name, resolve);
    on(name + "Error", (value: any) => {
      reject(value);
    });
    ipcRenderer.send(name, ...args);
  });
}

export const api = {
  /**
   * Here you can expose functions to the renderer process
   * so they can interact with the main (electron) side
   * without security problems.
   *
   * The function below can accessed using `window.Main.sayHello`
   */
  
  getIsMapSpriteEditor: async () => {
    return await send("getIsMapSpriteEditor");
  },

  getEditorType: async () => {
    return await send("getEditorType");
  },

  getCustomMapContext: async() => {
    return await send("getCustomMapContext");
  },

  getConfig: async () => {
    return await send("getConfig");
  },

  saveConfig: async (message: string) => {
    return await send("saveConfig", message);
  },

  saveConfigAndReload: async (message: any) => {
    return await send("saveConfigAndReload", message);
  },

  reload: async (message: any) => {
    return await send("reload", message);
  },

  getSprites: async () => { 
    return await send("getSprites");
  },

  getSpritesheets: async () => { 
    return await send("getSpritesheets");
  },

  saveSprite: async (json: any) => {
    return await send("saveSprite", json);
  },

  saveSprites: async (jsonList: any[]) => {
    return await send("saveSprites", jsonList);
  },

  getBackgrounds: async () => {
    return await send("getBackgrounds");
  },

  getLevels: async () => {
    return await send("getLevels");
  },

  saveLevel: async (json: any) => {
    return await send("saveLevel", json);
  },

  saveCanvas: async (json: any) => {
    return await send("saveCanvas", json);
  },
 
  sendHasDirty: async (hasDirty: boolean) => {
    return await send("sendHasDirty", hasDirty);
  },

  addLevel: async (json: any) => {
    return await send("addLevel", json);
  },

  setZoomLevel: (zoomLevel: number) => {
    webFrame.setZoomFactor(zoomLevel);
  },

  showDialog: async (title: string, message: string, buttons: string[] = undefined, isError: boolean = false, isCrash: boolean = false) => {
    if (!buttons) {
      buttons = ["Ok"];
    }
    let result = await send("showDialog", title, message, buttons, isError, isCrash);
    return result;
  },

  showError: async (title: string, message: string) => {
    let result = await send("showDialog", title, message, ["Ok"], true, false);
    return result;
  },

  setupMapSpriteEditor: async (json: any) => {
    return await send("setupMapSpriteEditor", json);
  },

  /**
   * Provide an easier way to listen to events
   */
  on: (channel: string, callback: Function) => {
    ipcRenderer.on(channel, (_, data) => callback(data))
  },
}

contextBridge.exposeInMainWorld("Main", api);
