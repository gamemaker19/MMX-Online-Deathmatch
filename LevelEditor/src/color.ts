export class Color {

  //
  r: number;
  g: number;
  b: number;
  a: number;

  constructor(r:number, g: number, b: number, a: number) {
    if(r === undefined || g === undefined || b === undefined || a === undefined) throw "Bad color";
    this.r = r;
    this.g = g;
    this.b = b;
    this.a = a;
  }

  get hex() {
    return "#" + this.r.toString(16) + this.g.toString(16) + this.b.toString(16) + this.a.toString(16);
  }

  get number() {
    let rString = this.r.toString(16);
    let gString = this.g.toString(16);
    let bString = this.b.toString(16);
    if(rString.length === 1) rString = "0" + rString;
    if(gString.length === 1) gString = "0" + gString;
    if(bString.length === 1) bString = "0" + bString;
    let hex = "0x" + rString + gString + bString;
    return parseInt(hex, 16);
  }

}