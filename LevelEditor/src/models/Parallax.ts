import * as _ from "lodash";
import { Exclude, Expose, Transform, Type } from "class-transformer";

export class Parallax {
  @Expose() path: string;
  @Expose() isLargeCamOverride: boolean = false;
  @Expose() @Transform(({ value }) => value ?? 0, { toClassOnly: true }) mirrorX: number = 0;
  @Expose() @Transform(({ value }) => value ?? 0, { toClassOnly: true }) startX: number = 0;
  @Expose() @Transform(({ value }) => value ?? 0, { toClassOnly: true }) startY: number = 0;
  @Expose() @Transform(({ value }) => value ?? 0.5, { toClassOnly: true }) speedX: number = 0.5;
  @Expose() @Transform(({ value }) => value ?? 0.5, { toClassOnly: true }) speedY: number = 0.5;
  @Expose() @Transform(({ value }) => value ?? 0, { toClassOnly: true }) scrollSpeedX: number = 0;
  @Expose() @Transform(({ value }) => value ?? 0, { toClassOnly: true }) scrollSpeedY: number = 0;

  constructor() {
  }

}