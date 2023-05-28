// Put stuff in this file that has minimal dependencies so the main.ts can use it

import * as _ from "lodash";

export function inCircle(x: number, y: number, circleX: number, circleY: number, r: number): boolean {
  if(Math.sqrt(Math.pow(x - circleX, 2) + Math.pow(y - circleY, 2)) <= r) {
      return true;
  }
  return false;
}

export function toZero(num: number, inc: number, dir: number) {
  if(dir === 1) {
    num -= inc;
    if(num < 0) num = 0;
    return num;
  }
  else if(dir === -1) {
    num += inc;
    if(num > 0) num = 0;
    return num;
  }
  else {
    throw "Must pass in -1 or 1 for dir";
  }
}

export function incrementRange(num: number, min: number, max: number) {
  num++;
  if(num >= max) num = min;
  return num;
}

export function decrementRange(num: number, min: number, max: number) {
  num--;
  if(num < min) num = max - 1;
  return num;
}

export function clamp01(num: number) {
  if(num < 0) num = 0;
  if(num > 1) num = 1;
  return num;
}

//Inclusive
export function randomRange(start: number, end: number) {
  /*
  end++;
  let dist = end - start;
  return Math.floor(Math.random() * dist) + start;
  */
  //@ts-ignore
  return _.random(start, end);
}

export function clampMax(num: number, max: number) {
  return num < max ? num : max;
}

export function clampMin(num: number, min: number) {
  return num > min ? num : min;
}

export function clampMin0(num: number) {
  return clampMin(num, 0);
}

export function clamp(num: number, min: number, max: number) {
  if(num < min) return min;
  if(num > max) return max;
  return num;
}

export function sin(degrees: number) {
  let rads = degrees * Math.PI / 180;
  return Math.sin(rads);
}

export function cos(degrees: number) {
  let rads = degrees * Math.PI / 180;
  return Math.cos(rads);
}

export function atan(value: number) {
  return Math.atan(value) * 180 / Math.PI;
}

export function moveTo(num: number, dest: number, inc: number) {
  inc *= Math.sign(dest - num);
  num += inc;
  return num;
}

export function lerp(num: number, dest: number, timeScale: number) {
  num = num + (dest - num)*timeScale;
  return num;
}

export function lerpNoOver(num: number, dest: number, timeScale: number) {
  num = num + (dest - num)*timeScale;
  if(Math.abs(num - dest) < 1) num = dest;
  return num;
}

//Expects angle and destAngle to be > 0 and < 360
export function lerpAngle(angle: number, destAngle: number, timeScale: number) {
  let dir = 1;
  if(Math.abs(destAngle - angle) > 180) {
    dir = -1;
  }
  angle = angle + dir*(destAngle - angle) * timeScale;
  return to360(angle);
}

export function to360(angle: number) {
  if(angle < 0) angle += 360;
  if(angle > 360) angle -= 360;
  return angle;
}

export function getHex(r: number, g: number, b: number, a: number) {
  return "#" + r.toString(16) + g.toString(16) + b.toString(16) + a.toString(16);
}

export function roundEpsilon(num: number) {
  let numRound = Math.round(num);
  let diff = Math.abs(numRound - num);
  if(diff < 0.0001) {
    return numRound;
  }
  return num;
}

let autoInc = 0;
export function getAutoIncId() {
  autoInc++;
  return autoInc;
}

export function stringReplace(str: string, pattern: string, replacement: string) {
  return str.replace(new RegExp(pattern, 'g'), replacement);
}

export function keyCodeToString(charCode: number) {

  if(charCode === 0) return "left mouse";
  if(charCode === 1) return "middle mouse";
  if(charCode === 2) return "right mouse";
  if(charCode === 3) return "wheel up";
  if(charCode === 4) return "wheel down";

  if (charCode === 8) return "backspace"; //  backspace
  if (charCode === 9) return "tab"; //  tab
  if (charCode === 13) return "enter"; //  enter
  if (charCode === 16) return "shift"; //  shift
  if (charCode === 17) return "ctrl"; //  ctrl
  if (charCode === 18) return "alt"; //  alt
  if (charCode === 19) return "pause/break"; //  pause/break
  if (charCode === 20) return "caps lock"; //  caps lock
  if (charCode === 27) return "escape"; //  escape
  if (charCode === 33) return "page up"; // page up, to avoid displaying alternate character and confusing people	         
  if (charCode === 34) return "page down"; // page down
  if (charCode === 35) return "end"; // end
  if (charCode === 36) return "home"; // home
  if (charCode === 37) return "left arrow"; // left arrow
  if (charCode === 38) return "up arrow"; // up arrow
  if (charCode === 39) return "right arrow"; // right arrow
  if (charCode === 40) return "down arrow"; // down arrow
  if (charCode === 45) return "insert"; // insert
  if (charCode === 46) return "delete"; // delete
  if (charCode === 91) return "left window"; // left window
  if (charCode === 92) return "right window"; // right window
  if (charCode === 93) return "select key"; // select key
  if (charCode === 96) return "numpad 0"; // numpad 0
  if (charCode === 97) return "numpad 1"; // numpad 1
  if (charCode === 98) return "numpad 2"; // numpad 2
  if (charCode === 99) return "numpad 3"; // numpad 3
  if (charCode === 100) return "numpad 4"; // numpad 4
  if (charCode === 101) return "numpad 5"; // numpad 5
  if (charCode === 102) return "numpad 6"; // numpad 6
  if (charCode === 103) return "numpad 7"; // numpad 7
  if (charCode === 104) return "numpad 8"; // numpad 8
  if (charCode === 105) return "numpad 9"; // numpad 9
  if (charCode === 106) return "multiply"; // multiply
  if (charCode === 107) return "add"; // add
  if (charCode === 109) return "subtract"; // subtract
  if (charCode === 110) return "decimal point"; // decimal point
  if (charCode === 111) return "divide"; // divide
  if (charCode === 112) return "F1"; // F1
  if (charCode === 113) return "F2"; // F2
  if (charCode === 114) return "F3"; // F3
  if (charCode === 115) return "F4"; // F4
  if (charCode === 116) return "F5"; // F5
  if (charCode === 117) return "F6"; // F6
  if (charCode === 118) return "F7"; // F7
  if (charCode === 119) return "F8"; // F8
  if (charCode === 120) return "F9"; // F9
  if (charCode === 121) return "F10"; // F10
  if (charCode === 122) return "F11"; // F11
  if (charCode === 123) return "F12"; // F12
  if (charCode === 144) return "num lock"; // num lock
  if (charCode === 145) return "scroll lock"; // scroll lock
  if (charCode === 186) return ";"; // semi-colon
  if (charCode === 187) return "="; // equal-sign
  if (charCode === 188) return ","; // comma
  if (charCode === 189) return "-"; // dash
  if (charCode === 190) return "."; // period
  if (charCode === 191) return "/"; // forward slash
  if (charCode === 192) return "`"; // grave accent
  if (charCode === 219) return "["; // open bracket
  if (charCode === 220) return "\\"; // back slash
  if (charCode === 221) return "]"; // close bracket
  if (charCode === 222) return "'"; // single quote
  if (charCode === 32) return "space";
  return String.fromCharCode(charCode);
}

export function fileName(filepath: string) {
  if (!filepath) return filepath;
  return filepath.split(/[\\/]/).pop();
}

export function getNormalizedSpritesheetName(customMapName: string, rawSpritesheetPath: string) {
  let spritesheetBaseName = baseName(rawSpritesheetPath);
  if (customMapName) {
    spritesheetBaseName = customMapName + ":" + spritesheetBaseName;
  }
  else if (rawSpritesheetPath.includes("/maps_custom/")) {
    customMapName = rawSpritesheetPath.split("/maps_custom/")[1].split("/")[0];
    spritesheetBaseName = customMapName + ":" + spritesheetBaseName;
  }
  return spritesheetBaseName;
}

export function baseName(filepath: string) {
  if (!filepath) return filepath;
   var base = new String(filepath).substring(filepath.lastIndexOf('/') + 1); 
    if(base.lastIndexOf(".") != -1)       
        base = base.substring(0, base.lastIndexOf("."));
   return base;
}

export function getAssetPath(filepath: string) {
  if (!filepath) return filepath;
  let pieces = filepath.split("/assets/");
  return pieces[pieces.length - 1];
}

export function getFolderPath(filepath: string) {
  if (!filepath) return filepath;
  let pieces = filepath.split("/");
  pieces.pop();
  return pieces.join('/');
}

export function make2DArray(w: number, h: number, val: any) : any {
  var arr: any[][] = [];
  for(let i = 0; i < h; i++) {
      arr[i] = [];
      for(let j = 0; j < w; j++) {
          arr[i][j] = val;
      }
  }
  return arr;
}

export function endsInNumber(str: string) {
  let num = (str.match(/\d+$/) || []).pop();
  let numInt = parseInt(num);
  if (isNaN(numInt)) {
    return false;
  }
  return true;
}

export function isAlphaNumeric(str: string) {
  var code, i, len;

  for (i = 0, len = str.length; i < len; i++) {
    code = str.charCodeAt(i);
    if (!(code > 47 && code < 58) && // numeric (0-9)
        !(code > 64 && code < 91) && // upper alpha (A-Z)
        !(code > 96 && code < 123)) // lower alpha (a-z)
    {
      return false;
    }
  }
  return true;
};

export function rangesOverlap(range1Start: number, range1End: number, range2Start: number, range2End: number) {
  let x1 = Math.min(range1Start, range1End);
  let x2 = Math.max(range1Start, range1End);
  let y1 = Math.min(range2Start, range2End);
  let y2 = Math.max(range2Start, range2End);
  return x1 <= y2 && y1 <= x2;
}

export function rangesDistance(range1Start: number, range1End: number, range2Start: number, range2End: number) {

  if (rangesOverlap(range1Start, range1End, range2Start, range2End)) {
    return 0;
  }

  let min = Math.min(range1Start, range1End);
  let max = Math.max(range1Start, range1End);
  let min2 = Math.min(range2Start, range2End);
  let max2 = Math.max(range2Start, range2End);
  
  if (min2 > max) {
    return min2 - max;
  }
  else {
    return min - max2;
  }
}