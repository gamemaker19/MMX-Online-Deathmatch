import { Type } from "class-transformer";
import { Point } from "./Point";
import * as Helpers from "./../helpers";

export class Line {
  @Type(() => Point)
  point1: Point;
  @Type(() => Point)
  point2: Point;
  constructor(point1: Point, point2: Point) {
    this.point1 = point1;
    this.point2 = point2;
  }
  
  // Given three colinear points p, q, r, the function checks if
  // point q lies on line segment 'pr'
  onSegment(p: Point, q: Point, r: Point)
  {
    if (q.x <= Math.max(p.x, r.x) && q.x >= Math.min(p.x, r.x) &&
        q.y <= Math.max(p.y, r.y) && q.y >= Math.min(p.y, r.y))
      return true;
    return false;
  }
 
  // To find orientation of ordered triplet (p, q, r).
  // The function returns following values
  // 0 --> p, q and r are colinear
  // 1 --> Clockwise
  // 2 --> Counterclockwise
  orientation(p: Point, q: Point, r: Point)
  {
    // See https://www.geeksforgeeks.org/orientation-3-ordered-points/
    // for details of below formula.
    let val = (q.y - p.y) * (r.x - q.x) -
              (q.x - p.x) * (r.y - q.y);

    if (val == 0) return 0;  // colinear
    return (val > 0)? 1: 2; // clock or counterclock wise
  }
  
  get x1() { return this.point1.x; }
  get y1() { return this.point1.y; }
  get x2() { return this.point2.x; }
  get y2() { return this.point2.y; }

  //@ts-ignore
  checkLineIntersection(line1StartX, line1StartY, line1EndX, line1EndY, line2StartX, line2StartY, line2EndX, line2EndY) {

    // if the lines intersect, the result contains the x and y of the intersection (treating the lines as infinite) and booleans for whether line segment 1 or line segment 2 contain the point
    let denominator, a, b, numerator1, numerator2, result = {
        //@ts-ignore
        x: null,
        //@ts-ignore
        y: null,
        onLine1: false,
        onLine2: false
    };
    denominator = ((line2EndY - line2StartY) * (line1EndX - line1StartX)) - ((line2EndX - line2StartX) * (line1EndY - line1StartY));
    if (denominator == 0) {
        return result;
    }
    a = line1StartY - line2StartY;
    b = line1StartX - line2StartX;
    numerator1 = ((line2EndX - line2StartX) * a) - ((line2EndY - line2StartY) * b);
    numerator2 = ((line1EndX - line1StartX) * a) - ((line1EndY - line1StartY) * b);
    a = numerator1 / denominator;
    b = numerator2 / denominator;

    // if we cast these lines infinitely in both directions, they intersect here:
    result.x = line1StartX + (a * (line1EndX - line1StartX));
    result.y = line1StartY + (a * (line1EndY - line1StartY));
    /*
    // it is worth noting that this should be the same as:
    x = line2StartX + (b * (line2EndX - line2StartX));
    y = line2StartX + (b * (line2EndY - line2StartY));
    */
    // if line1 is a segment and line2 is infinite, they intersect if:
    if (a > 0 && a < 1) {
        result.onLine1 = true;
    }
    // if line2 is a segment and line1 is infinite, they intersect if:
    if (b > 0 && b < 1) {
        result.onLine2 = true;
    }
    // if line1 and line2 are segments, they intersect if both of the above are true
    return result;
  }

  getIntersectPoint(other: Line): Point | undefined {
    //https://www.geeksforgeeks.org/check-if-two-given-line-segments-intersect/
    let doesIntersect = false;
    let coincidePoint: Point | undefined;
    let p1 = this.point1;
    let q1 = this.point2;
    let p2 = other.point1;
    let q2 = other.point2;
    // Find the four orientations needed for general and
    // special cases
    let o1 = this.orientation(p1, q1, p2);
    let o2 = this.orientation(p1, q1, q2);
    let o3 = this.orientation(p2, q2, p1);
    let o4 = this.orientation(p2, q2, q1);

    // General case
    if (o1 != o2 && o3 != o4) {
      doesIntersect = true;
    }

    // Special Cases
    // p1, q1 and p2 are colinear and p2 lies on segment p1q1
    if (o1 == 0 && this.onSegment(p1, p2, q1)) {
      coincidePoint = p2;
    }
    // p1, q1 and q2 are colinear and q2 lies on segment p1q1
    else if (o2 == 0 && this.onSegment(p1, q2, q1)) {
      coincidePoint = q2;
    }
    // p2, q2 and p1 are colinear and p1 lies on segment p2q2
    else if (o3 == 0 && this.onSegment(p2, p1, q2)) {
      coincidePoint = p1;
    }
    // p2, q2 and q1 are colinear and q1 lies on segment p2q2
    else if (o4 == 0 && this.onSegment(p2, q1, q2)) {
      coincidePoint = q1;
    }
    
    if(coincidePoint) doesIntersect = true;
    if(!doesIntersect) return undefined;

    if(coincidePoint) return coincidePoint;
    let intersection = this.checkLineIntersection(this.x1, this.y1, this.x2, this.y2, other.x1, other.y1, other.x2, other.y2);
    if(intersection.x !== null && intersection.y !== null)
      return new Point(intersection.x, intersection.y);
    return new Point((this.x1 + this.x2) / 2, (this.y1 + this.y2) / 2);
  }

  get slope() {
    if (this.x1 == this.x2) return NaN;
    return (this.y1 - this.y2) / (this.x1 - this.x2);
  }
  
  get yInt() {
    if (this.x1 === this.x2) return this.y1 === 0 ? 0 : NaN;
    if (this.y1 === this.y2) return this.y1;
    return this.y1 - this.slope * this.x1;
  }

  get xInt() {
    let slope;
    if (this.y1 === this.y2) return this.x1 == 0 ? 0 : NaN;
    if (this.x1 === this.x2) return this.x1;
    return (-1 * ((slope = this.slope * this.x1 - this.y1)) / this.slope);
  }

  getHorizontalOrVertical() {
    if (this.x1 === this.x2) return "vertical";
    if (this.y1 === this.y2) return "horizontal";
    return "angled";
  }

  snapTo(other: Line) {
    const threshold = 5;
    let hov = this.getHorizontalOrVertical();
    let otherHov = other.getHorizontalOrVertical();
    if (hov === "angled" || hov !== otherHov) return;
    if (hov === "vertical") {
      if (Helpers.rangesDistance(this.y1, this.y2, other.y1, other.y2) < threshold && Math.abs(this.x1 - other.x1) < threshold) {
        this.point1.x = other.x1;
        this.point2.x = other.x1;
      }
    }
    else if (hov === "horizontal") {
      if (Helpers.rangesDistance(this.x1, this.x2, other.x1, other.x2) < threshold && Math.abs(this.y1 - other.y1) < threshold) {
        this.point1.y = other.y1;
        this.point2.y = other.y1;
      }
    }
  }

  getAngledLineRectLines(): Line[] {
    return [
      new Line(new Point(this.x1, this.y1), new Point(this.x2, this.y1)),
      new Line(new Point(this.x1, this.y1), new Point(this.x1, this.y2)),
      new Line(new Point(this.x2, this.y1), new Point(this.x2, this.y2)),
      new Line(new Point(this.x1, this.y2), new Point(this.x2, this.y2)),
    ];
  }

  static inLine(mouseX: number, mouseY: number, x0: number, y0: number, x1: number, y1: number, threshold: number): boolean {

    let small_x: number = Math.min(x0,x1);
    let big_x: number = Math.max(x0,x1);
  
    if(mouseX < small_x - (threshold*0.5) || mouseX > big_x + (threshold*0.5)){
      return false;
    }
  
    let linepoint: Point = linepointNearestMouse(x0, y0, x1, y1, mouseX, mouseY);
    let dx: number = mouseX - linepoint.x;
    let dy: number = mouseY - linepoint.y;
    let distance: number = Math.abs(Math.sqrt(dx*dx+dy*dy));
    if(distance < threshold) {
      return true;
    }
    else {
      return false;
    }
  }
}

function linepointNearestMouse(x0: number, y0: number, x1: number, y1: number, x: number, y: number): Point {
  function lerp(a: number,b: number,x: number):number{ return(a+x*(b-a)); };
  let dx: number=x1-x0;
  let dy: number=y1-y0;
  let t: number = ((x-x0)*dx+(y-y0)*dy)/(dx*dx+dy*dy);
  let lineX: number = lerp(x0, x1, t);
  let lineY: number = lerp(y0, y1, t);
  return new Point(lineX,lineY);
}