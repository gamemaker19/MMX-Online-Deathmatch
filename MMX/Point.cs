using System;
using System.Collections.Generic;
using System.Text;

namespace MMXOnline
{
    public struct Point
    {
        public float x;
        public float y;
        public Point(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static Point zero
        {
            get
            {
                return new Point(0, 0);
            }
        }

        public float ix
        {
            get { return MathF.Round(x); }
        }
        public float iy
        {
            get { return MathF.Round(y); }
        }

        public Point addxy(float x, float y)
        {
            var point = new Point(this.x + x, this.y + y);
            return point;
        }

        //Avoid calls to this, if called a lot may be bottleneck
        public Point normalize()
        {
            x = Helpers.RoundEpsilon(x);
            y = Helpers.RoundEpsilon(y);
            if (x == 0 && y == 0) return new Point(0, 0);
            var mag = magnitude;
            var point = new Point(x / mag, y / mag);
            if (float.IsNaN(point.x) || float.IsNaN(point.y))
            {
                throw new Exception("NAN!");
            }
            point.x = Helpers.RoundEpsilon(point.x);
            point.y = Helpers.RoundEpsilon(point.y);
            return point;
        }

        public static Point lerp(Point a, Point b, float t)
        {
            return a.add(b.subtract(a).times(t));
        }

        public static Point moveTo(Point a, Point b, float amount)
        {
            if (a.distanceTo(b) <= amount * 2)
            {
                return b;
            }
            Point dirTo = a.directionToNorm(b);
            return a.add(dirTo.times(amount));
        }

        public float dotProduct(Point other)
        {
            return (x * other.x) + (y * other.y);
        }

        public Point project(Point other)
        {
            var dp = dotProduct(other);
            return new Point((dp / (other.x * other.x + other.y * other.y)) * other.x, (dp / (other.x * other.x + other.y * other.y)) * other.y);
        }

        public Point leftOrRightNormal(int dir)
        {
            if (dir == 1) return rightNormal();
            else return leftNormal();
        }

        public Point rightNormal()
        {
            return new Point(-y, x);
        }

        public Point leftNormal()
        {
            return new Point(y, -x);
        }

        public float perProduct(Point other)
        {
            return dotProduct(other.rightNormal());
        }

        //Returns new point
        public Point add(Point other)
        {
            var point = new Point(x + other.x, y + other.y);
            return point;
        }

        //Mutates this point
        public void inc(Point other) 
        {
            x += other.x;
            y += other.y;
        }

        //Returns new point
        public Point times(float num)
        {
            var point = new Point(x * num, y * num);
            return point;
        }

        //Mutates this point
        public Point multiply(float num)
        {
            x *= num;
            y *= num;
            return this;
        }

        public Point unitInc(float num)
        {
            return add(normalize().times(num));
        }

        public float angle
        {
            get
            {
                var ang = MathF.Atan2(y, x);
                ang *= 180 / MathF.PI;
                if (ang < 0) ang += 360;
                return ang;
            }
        }

        public static Point createFromAngle(float angle)
        {
            float x = Helpers.cosd(angle);
            float y = Helpers.sind(angle);
            return new Point(x, y);
        }

        public float angleWith(Point other)
        {
            var ang = MathF.Atan2(other.y, other.x) - MathF.Atan2(y, x);
            ang *= 180 / MathF.PI;
            if (ang < 0) ang += 360;
            if (ang > 180) ang = 360 - ang;
            return ang;
        }

        public float magnitude
        {
            get
            {
                if (x == 0) return MathF.Abs(y);
                if (y == 0) return MathF.Abs(x);
                var root = x * x + y * y;
                if (root < 0) root = 0;
                var result = MathF.Sqrt(root);
                if (float.IsNaN(result)) throw new Exception("NAN!");
                return result;
            }
        }

        public Point clone()
        {
            return new Point(x, y);
        }
        
        public float distanceTo(Point other)
        {
            return MathF.Sqrt(MathF.Pow(other.x - x, 2) + MathF.Pow(other.y - y, 2));
        }

        public bool isZero()
        {
            return x == 0 && y == 0;
        }

        public bool isCloseToZero(float epsilon = 0.1f)
        {
            return magnitude < epsilon;
        }

        public Point subtract(Point other)
        {
            return new Point(x - other.x, y - other.y);
        }

        public Point directionTo(Point other)
        {
            return new Point(other.x - x, other.y - y);
        }

        public Point directionToNorm(Point other)
        {
            return (new Point(other.x - x, other.y - y)).normalize();
        }

        public bool isAngled()
        {
            return x != 0 && y != 0;
        }

        public override string ToString()
        {
            return x.ToString("0.0") + "," + y.ToString("0.0");
        }

        public static Point random(float xStart, float xEnd, float yStart, float yEnd)
        {
            return new Point(Helpers.randomRange(xStart, xEnd), Helpers.randomRange(yStart, yEnd));
        }

        public Point addRand(int xRange, int yRange)
        {
            return addxy(Helpers.randomRange(-xRange, xRange), Helpers.randomRange(-yRange, yRange));
        }

        public static Point average(List<Point> points)
        {
            if (points.Count == 0) return new Point();
            Point sum = new Point();
            foreach (var point in points)
            {
                sum.inc(point);
            }
            return sum.multiply(1.0f / points.Count);
        }

        public static float minX(List<Point> points)
        {
            float minX = float.MaxValue;
            foreach (var point in points)
            {
                if (point.x < minX) minX = point.x;
            }
            return minX;
        }

        public static float maxX(List<Point> points)
        {
            float maxX = float.MinValue;
            foreach (var point in points)
            {
                if (point.x > maxX) maxX = point.x;
            }
            return maxX;
        }

        public static float minY(List<Point> points)
        {
            float minY = float.MaxValue;
            foreach (var point in points)
            {
                if (point.y < minY) minY = point.y;
            }
            return minY;
        }

        public static float maxY(List<Point> points)
        {
            float maxY = float.MinValue;
            foreach (var point in points)
            {
                if (point.y > maxY) maxY = point.y;
            }
            return maxY;
        }

        public bool isSideways()
        {
            return MathF.Abs(x) > MathF.Abs(y);
        }

        public bool isGroundNormal()
        {
            return y < 0 && MathF.Abs(y) > MathF.Abs(x);
        }

        public bool isCeilingNormal()
        {
            return y > 0 && MathF.Abs(y) > MathF.Abs(x);
        }

        public bool equals(Point other)
        {
            return x == other.x && y == other.y;
        }

        public Point closestPoint(List<Point> points)
        {
            if (points.Count == 0) return this;
            if (points.Count == 1) return points[0];

            Point bestPoint = this;
            float bestDist = float.MaxValue;
            foreach (var point in points)
            {
                float dist = distanceTo(point);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestPoint = point;
                }
            }

            return bestPoint;
        }
    }
}
