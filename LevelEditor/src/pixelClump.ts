import { Color } from "./color";
import { Rect } from "./models/Rect";
import * as _ from "lodash";

class PixelData {
  x: number;
  y: number;
  rgb: Color;
  neighbors: (PixelData | undefined)[];
  constructor(x: number, y: number, rgb: Color, neighbors: (PixelData | undefined)[]) {
    this.x = x;
    this.y = y;
    this.rgb = rgb;
    this.neighbors = neighbors;
  }
}

export function get2DArrayFromImage(imageData: ImageData) {
  let data = imageData.data;
  let arr = [];
  let row = [];
  for (let i=0; i<data.length; i+=4) {
    
    if (i % (imageData.width*4) === 0) {
      if (i > 0) {
        arr.push(row);
      }
      row = [];
    }

    let red = data[i];
    let green = data[i+1];
    let blue = data[i+2];
    let alpha = data[i+3];
    
    row.push(new PixelData(-1, -1, new Color(red, green, blue, alpha), []));

    if (i === data.length - 4) {
      arr.push(row);
    }
  }

  for (let i = 0; i < arr.length; i++) {
    for (let j = 0; j < arr[i].length; j++) {
      arr[i][j].x = j;
      arr[i][j].y = i;
    }
  }

  for (let i = 0; i < arr.length; i++) {
    for (let j = 0; j < arr[i].length; j++) {
      arr[i][j].neighbors.push(get2DArrayEl(arr, i-1, j-1));
      arr[i][j].neighbors.push(get2DArrayEl(arr, i-1, j));
      arr[i][j].neighbors.push(get2DArrayEl(arr, i-1, j+1));
      arr[i][j].neighbors.push(get2DArrayEl(arr, i, j-1));
      arr[i][j].neighbors.push(get2DArrayEl(arr, i, j));
      arr[i][j].neighbors.push(get2DArrayEl(arr, i, j+1));
      arr[i][j].neighbors.push(get2DArrayEl(arr, i+1, j-1));
      arr[i][j].neighbors.push(get2DArrayEl(arr, i+1, j));
      arr[i][j].neighbors.push(get2DArrayEl(arr, i+1, j+1));
      _.pull(arr[i][j].neighbors, undefined);
    }
  }

  return arr;
}

export function getPixelClumpRect(x: number, y: number, imageArr: PixelData[][]) {
  x = Math.round(x);
  y = Math.round(y);
  var selectedNode = imageArr[y]?.[x];
  if (!selectedNode) {
    return undefined;
  }
  if (selectedNode.rgb.a === 0) {
    console.log("Clicked transparent pixel");
    return undefined;
  }

  var queue = [];
  queue.push(selectedNode);

  var minX = Infinity;
  var minY = Infinity;
  var maxX = -1;
  var maxY = -1;

  var num  = 0;
  var visitedNodes = new Set();
  while (queue.length > 0) {
    var node = queue.shift();
    if (!node) continue;
    num++;
    if (node.x < minX) minX = node.x;
    if (node.y < minY) minY = node.y;
    if (node.x > maxX) maxX = node.x;
    if (node.y > maxY) maxY = node.y;

    for (var neighbor of node.neighbors) {
      if (visitedNodes.has(neighbor)) continue;
      if (queue.indexOf(neighbor) === -1) {
        queue.push(neighbor);
      }
    }
    visitedNodes.add(node);
  }
  //console.log(num);
  return new Rect(Math.round(minX), Math.round(minY), Math.round(maxX+1), Math.round(maxY+1));

}

export function getSelectedPixelRect(x: number, y: number, endX: number, endY: number, imageArr: PixelData[][]) {
  
  x = Math.round(x);
  y = Math.round(y);

  var minX = Infinity;
  var minY = Infinity;
  var maxX = -1;
  var maxY = -1;

  for (var i = y; i <= endY; i++) {
    for (var j = x; j <= endX; j++) {
      if (!imageArr[i] || !imageArr[i][j]) continue;
      if (imageArr[i][j].rgb.a !== 0) {
        if (i < minY) minY = i;
        if (i > maxY) maxY = i;
        if (j < minX) minX = j;
        if (j > maxX) maxX = j;
      }
    }
  }

  if (!isFinite(minX) || !isFinite(minY) || maxX === -1 || maxY === -1) return;

  return new Rect(Math.round(minX), Math.round(minY), Math.round(maxX+1), Math.round(maxY+1));
}

export function get2DArrayEl(arr: PixelData[][], i: number, j: number) {
  if (i < 0 || i >= arr.length) return undefined;
  if (j < 0 || j >= arr[0].length) return undefined;
  if (arr[i][j].rgb.a === 0) return undefined;
  return arr[i][j];
}