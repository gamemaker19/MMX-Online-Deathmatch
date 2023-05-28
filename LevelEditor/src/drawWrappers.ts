import { Rect } from "./models/Rect";
import { Shape } from "./models/Shape";
import * as _ from "lodash";;

export function noCanvasSmoothing(c: CanvasRenderingContext2D) {
  //c.webkitImageSmoothingEnabled = false;
  //c.mozImageSmoothingEnabled = false;
  c.imageSmoothingEnabled = false; /// future
}

let helperCanvas = document.createElement("canvas");
let helperCtx = helperCanvas.getContext("2d") as CanvasRenderingContext2D;
noCanvasSmoothing(helperCtx);

let helperCanvas2 = document.createElement("canvas");
let helperCtx2 = helperCanvas2.getContext("2d") as CanvasRenderingContext2D;
noCanvasSmoothing(helperCtx2);

let helperCanvas3 = document.createElement("canvas");
let helperCtx3 = helperCanvas3.getContext("2d") as CanvasRenderingContext2D;
noCanvasSmoothing(helperCtx3);

export function drawImage(ctx: CanvasRenderingContext2D, imgEl: HTMLImageElement, sX: number, sY: number, sW?: number, sH?: number, 
  x?: number, y?: number, flipX?: number, flipY?: number, options?: string, alpha?: number, scaleX?: number, scaleY?: number): void {
  
  if(!sW || !sH) {
    ctx.drawImage(imgEl, sX, sY);
    return;
  }

  ctx.globalAlpha = (alpha === undefined || alpha === undefined) ? 1 : alpha;

  helperCanvas.width = sW;
  helperCanvas.height = sH;
  
  helperCtx.save();
  scaleX = scaleX || 1;
  scaleY = scaleY || 1;
  flipX = (flipX || 1);
  flipY = (flipY || 1);
  helperCtx.scale(flipX * scaleX, flipY * scaleY);

  helperCtx.clearRect(0, 0, helperCanvas.width, helperCanvas.height);
  helperCtx.drawImage(
    imgEl,
    sX, //source x
    sY, //source y
    sW, //source width
    sH, //source height
    0,  //dest x
    0, //dest y
    flipX * sW, //dest width
    flipY * sH  //dest height
  );

  ctx.drawImage(helperCanvas, x || 0, y || 0);
  
  ctx.globalAlpha = 1;
  helperCtx.restore();
}

/*
export function createAndDrawRect(container: PIXI.Container, rect: Rect, fillColor?: number, strokeColor?: number, strokeWidth?: number, fillAlpha?: number): PIXI.Graphics {
  let rectangle = new PIXI.Graphics();
  if(fillAlpha === undefined) fillAlpha = 1;
  //if(!fillColor) fillColor = 0x00FF00;

  if(strokeColor) {
    rectangle.lineStyle(strokeWidth, strokeColor, fillAlpha);
  }

  if(fillColor !== undefined) 
    rectangle.beginFill(fillColor, fillAlpha);
  
  rectangle.drawRect(rect.x1, rect.y1, rect.w, rect.h);
  if(fillColor !== undefined)
    rectangle.endFill();
  
  container.addChild(rectangle);
  return rectangle;
}
*/

export function drawRect(ctx: CanvasRenderingContext2D, rect: Rect, fillColor?: string, strokeColor?: string, strokeWidth?: number, fillAlpha?: number): void {
  let rx: number = Math.round(rect.x1);
  let ry: number = Math.round(rect.y1);
  let rx2: number = Math.round(rect.x2);
  let ry2: number = Math.round(rect.y2);

  ctx.beginPath();
  ctx.rect(rx, ry, rx2 - rx, ry2 - ry);

  if(fillAlpha) {
    ctx.globalAlpha = fillAlpha;
  }

  if(strokeColor) {
    strokeWidth = strokeWidth ? strokeWidth : 1;
    ctx.lineWidth = strokeWidth;
    ctx.strokeStyle = strokeColor;
    ctx.stroke();
  }

  if(fillColor) {
    ctx.fillStyle = fillColor;
    ctx.fill();
  }

  ctx.globalAlpha = 1;
}

export function drawPolygon(ctx: CanvasRenderingContext2D, shape: Shape, closed: boolean, fillColor?: string, lineColor?: string, lineThickness?: number, fillAlpha?: number): void {

  let vertices = shape.points;

  if(fillAlpha) {
    ctx.globalAlpha = fillAlpha;
  }

  ctx.beginPath();
  ctx.moveTo(vertices[0].x, vertices[0].y);

  for(let i: number = 1; i < vertices.length; i++) {
      ctx.lineTo(vertices[i].x, vertices[i].y);
  }

  if(closed) {
      ctx.closePath();

      if(fillColor) {
          ctx.fillStyle = fillColor;
          ctx.fill();
      }
  }

  if(lineColor) {
      ctx.lineWidth = lineThickness || 0;
      ctx.strokeStyle = lineColor;
      ctx.stroke();
  }

  ctx.globalAlpha = 1;
}

export function drawText(ctx: CanvasRenderingContext2D, text: string, x: number, y: number, fillColor: string = undefined, outlineColor: string = undefined, size: number = undefined, hAlign: string = undefined, vAlign: string = undefined, font: string = undefined) {
  ctx.save();
  fillColor = fillColor || "black";
  size = size || 14;
  hAlign = hAlign || "center";  //start,end,left,center,right
  vAlign = vAlign || "middle";  //Top,Bottom,Middle,Alphabetic,Hanging
  font = font || "Arial";
  ctx.font = size + "px " + font;
  ctx.textAlign = hAlign as CanvasTextAlign;
  ctx.textBaseline = vAlign as CanvasTextBaseline;
  if(outlineColor) {
    ctx.lineWidth = size / 7;
    ctx.strokeStyle = outlineColor;
    ctx.strokeText(text,x,y);
  }
  ctx.fillStyle = fillColor;
  ctx.fillText(text,x,y);
  ctx.restore();
}

export function drawCircle(ctx: CanvasRenderingContext2D, x: number, y: number, r: number, fillColor?: string, lineColor?: string, lineThickness?: number) {
  ctx.beginPath();
  ctx.arc(x, y, r, 0, 2*Math.PI, false);
  
  if(fillColor) {
      ctx.fillStyle = fillColor;
      ctx.fill();
  }
  
  if(lineColor) {
      ctx.lineWidth = lineThickness || 0;
      ctx.strokeStyle = lineColor;
      ctx.stroke();
  }

}

export function drawLine(ctx: CanvasRenderingContext2D, x: number, y: number, x2: number, y2: number, color?: string, thickness?: number) {

  if(!thickness) thickness = 1;
  if(!color) color = 'black';

  ctx.beginPath();
  ctx.moveTo(x, y);
  ctx.lineTo(x2, y2);
  ctx.lineWidth = thickness;
  ctx.strokeStyle = color;
  ctx.stroke();
}
