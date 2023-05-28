import * as _ from "lodash";
import { Point } from "./Point";

export class Obj {
  name: string;
  isShape: boolean;
  color: string;
  imgEl: HTMLImageElement;
  zIndex: number;
  exclusiveMap: string;
  constructor(name: string, isShape: boolean, color: string, imageFileName: string, zIndex: number) {
    this.name = name;
    this.isShape = isShape;
    this.color = color;
    this.zIndex = zIndex;
    if (imageFileName) {
      this.imgEl = document.createElement("img");
      this.imgEl.src = "file:///" + imageFileName;
      this.imgEl.onload = () => {
        //Make sure this loads before app is ready? Might cause issues if not
      };
    }
  }
}