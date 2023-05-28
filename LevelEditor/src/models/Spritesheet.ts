export class Spritesheet {

  path: string = "";
  imgEl: HTMLImageElement | undefined;
  imgArr: any;
  lazyLoadImgArr: Function;

  constructor(path: string) {
    this.path = path;
  }

  loadImage() {
    if (!this.imgEl) {
      let spritesheetImg = document.createElement("img");
      spritesheetImg.onload = () => {
      };
      spritesheetImg.onerror = (e) => {
        window.Main.showError("Error", "Error loading image " + this.path);
      }
      spritesheetImg.src = "file:///" + this.path;
      this.imgEl = spritesheetImg;
    }
  }
}