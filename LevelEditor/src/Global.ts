import { Level } from "./models/Level";
import { Obj } from "./models/Obj";
import { Sprite } from "./models/Sprite";
import { Spritesheet } from "./models/Spritesheet";

class Global {
  sprites: Sprite[] = [];
  levels: Level[] = [];
  spritesheetMap: { [path: string]: Spritesheet } = {};
  backgroundMap: { [path: string]: Spritesheet } = {};
  imageMap: { [fileName: string]: HTMLImageElement } = {};
  nextSelectableId: number = 0;
  objects: Obj[] = [
    
    new Obj("Collision Shape", true, "blue", "", 1),
    
    new Obj("Water Zone", true, "lightblue", "", -1),
    new Obj("Backwall Zone", true, "gray", "", 0),
    
    new Obj("Brake Zone", true, "aqua", "", 1),
    new Obj("Turn Zone", true, "olive", "", 1.5),

    new Obj("No Scroll", true, "magenta", "", 2),
    new Obj("Kill Zone", true, "red", "", 3),
    new Obj("Jump Zone", true, "green", "", 4),
    new Obj("Move Zone", true, "orange", "", 5),
    
    new Obj("Ladder", true, "yellow", "", 6),

    new Obj("Spawn Point", false, "", "images/spawnPoint.png", 7),
    new Obj("Red Spawn", false, "", "images/redSpawnPoint.png" , 8),
    new Obj("Blue Spawn", false, "", "images/blueSpawnPoint.png" , 9),
    
    new Obj("Red Flag", false, "", "images/redFlag.png", 10),
    new Obj("Blue Flag", false, "", "images/blueFlag.png", 11),
    new Obj("Control Point", false, "", "images/controlPoint.png", 12),
    
    new Obj("Large Health", false, "", "images/largeHealth.png", 13),
    new Obj("Small Health", false, "", "images/smallHealth.png", 14),
    new Obj("Large Ammo", false, "", "images/largeAmmo.png", 15),
    new Obj("Small Ammo", false, "", "images/smallAmmo.png", 16),

    new Obj("Ride Armor", false, "", "images/rideArmor.png", 17),
    new Obj("Ride Chaser", false, "", "images/rideChaser.png", 18),

    new Obj("Node", false, "", "images/graph_node.png", 19),

    new Obj("Map Sprite", false, "", "images/mapSpritePlaceholder.png", 19),
    new Obj("Moving Platform", false, "", "images/mapSpritePlaceholder.png", 19.5),
    new Obj("Music Source", false, "", "images/musicSource.png", 19.75),

    new Obj("Jape Memorial", false, "", "images/japeMemorial.png", 20),

    new Obj("Goal", false, "", "images/victoryPoint.png", 21),
    new Obj("Gate", true, "purple", "", 22),

  ];
  alignments: string[] = [ "topleft", "topmid", "topright", "midleft", "center", "midright", "botleft", "botmid", "botright"];
  wrapModes: string[] = [ "loop", "once" ];
  hitboxFlags: string[] = [ "hitbox", "hurtbox", "hit+hurt", "none" ];

  constructor() {
  }

  getObjectByName(name: string): Obj {
    for (let obj of this.objects) {
      if (obj.name === name) {
        return obj;
      }
    }
    return undefined;
  }

  get spritesheets (): Spritesheet[] {
    let retSpritesheets: Spritesheet[] = [];
    for (let spritesheetPath in this.spritesheetMap) {
      retSpritesheets.push(this.spritesheetMap[spritesheetPath]);
    }
    return retSpritesheets;
  }

  get backgrounds (): Spritesheet[] {
    let retBackgrounds: Spritesheet[] = [];
    for (let backgroundPath in this.backgroundMap) {
      retBackgrounds.push(this.backgroundMap[backgroundPath]);
    }
    return retBackgrounds;
  }

  getNextSelectableId() {
    return this.nextSelectableId++;
  }

}

let global = new Global();
export {global};