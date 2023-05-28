using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MMXOnline
{
    public enum HitboxFlag
    {
        Hitbox,
        Hurtbox,
        HitAndHurt,
        None,
    }

    public class Collider
    {
        public Shape _shape;
        public bool isTrigger;
        public bool wallOnly = true;
        public bool isClimbable = true;
        public Actor actor;
        public Point offset;
        public bool isStatic = false;
        public bool disabled;   //NOTE only affects triggers
        public string name;
        public string originalSprite;

        public int flag = 0;
        public bool isAttack() { return flag == (int)HitboxFlag.Hitbox || flag == (int)HitboxFlag.HitAndHurt; }
        public bool isHurtBox() { return flag == (int)HitboxFlag.Hurtbox || flag == (int)HitboxFlag.HitAndHurt; }

        public Collider(List<Point> points, bool isTrigger, Actor actor, bool isClimbable, bool isStatic, HitboxFlag flag, Point offset)
        {
            _shape = new Shape(points);
            this.isTrigger = isTrigger;
            this.actor = actor;
            this.isClimbable = isClimbable;
            this.isStatic = isStatic;
            this.flag = (int)flag;
            this.offset = offset;
        }

        public Shape shape
        {
            get
            {
                var offset = new Point(0, 0);
                if (actor != null)
                {
                    var rect = _shape.getRect();
                    offset = actor.sprite.getAlignOffsetHelper(rect, this.offset, actor.xDir, actor.yDir);
                    offset.x += actor.pos.x;
                    offset.y += actor.pos.y;
                }

                var retVal = _shape.clone(offset.x, offset.y);

                return retVal;
            }
        }

        public bool isCollidingWith(Collider other)
        {
            if (other == null) return false;
            return shape.intersectsShape(other.shape) != null;
        }

        public Collider clone()
        {
            var clonedCollider = (Collider)MemberwiseClone();
            clonedCollider._shape = _shape.clone(0, 0);
            return clonedCollider;
        }
    }

    public class CollideData
    {
        public Collider myCollider; //My own collider that collided the otherCollider with
        public Collider otherCollider; //The other thing that was collided with
        public GameObject gameObject; //Gameobject of otherCollider
        public Point? vel; //The velocity at which we collided with the other thing above
        public bool isTrigger;
        public HitData hitData;

        public CollideData(Collider myCollider, Collider otherCollider, Point? vel, bool isTrigger, GameObject gameObject, HitData hitData)
        {
            this.myCollider = myCollider;
            this.otherCollider = otherCollider;
            this.vel = vel;
            this.isTrigger = isTrigger;
            this.gameObject = gameObject;
            this.hitData = hitData;
        }

        public Point getHitPointSafe()
        {
            if (hitData?.hitPoint != null)
            {
                return hitData.hitPoint.Value;
            }
            if (gameObject is Actor actor)
            {
                return actor.pos;
            }
            return new Point();
        }

        public Point getNormalSafe()
        {
            if (hitData?.normal != null)
            {
                return hitData.normal.Value;
            }
            return new Point(0, -1);
        }

        public bool isSideWallHit()
        {
            return gameObject is Wall && hitData?.normal != null && hitData.normal.Value.isSideways();
        }

        public bool isCeilingHit()
        {
            return gameObject is Wall && hitData?.normal != null && hitData.normal.Value.isCeilingNormal();
        }

        public bool isGroundHit()
        {
            return gameObject is Wall && hitData?.normal != null && hitData.normal.Value.isGroundNormal();
        }

        public bool isMovingPlatformHit()
        {
            return gameObject is Wall wall && wall.isMoving;
        }

        public void drawNormal()
        {
            var origin = getHitPointSafe();
            var normal = getNormalSafe();
            Global.level.debugDrawCalls.Add(() =>
            {
                DrawWrappers.DrawDebugDot(origin.x, origin.y, Color.Red);
                DrawWrappers.DrawLine(origin.x, origin.y, origin.x + normal.x * 10, origin.y + normal.y * 10, Color.Red, 1, ZIndex.HUD);
            });
        }
    }

    public class HitData
    {
        public Point? normal;
        public List<Point> hitPoints;
        public List<Line> hitLines;
        public Point? hitPoint
        {
            get
            {
                if (hitPoints.Count == 0) return null;
                else if (hitPoints.Count == 1) return hitPoints[0];
                else return Point.average(hitPoints);
            }
        }

        public List<Line> distinctHitLines
        {
            get
            {
                var retLines = new List<Line>();
                for (int i = 0; i < hitLines.Count; i++)
                {
                    bool dontAdd = false;
                    for (int j = i - 1; j >= 0; j--)
                    {
                        if (hitLines[i].equals(hitLines[j]))
                        {
                            dontAdd = true;
                            break;
                        }
                    }
                    if (!dontAdd)
                    {
                        retLines.Add(hitLines[i]);
                    }
                }

                return retLines;
            }
        }

        public HitData(Point? normal, List<Point> hitPoints, List<Line> hitLines = null)
        {
            this.normal = normal;
            this.hitPoints = hitPoints ?? new List<Point>();
            this.hitLines = hitLines ?? new List<Line>();
        }
    }
}
