using System;
using System.Collections.Generic;
using System.Text;

namespace MMXOnline
{
    public struct Rect
    {
        public float x1;
        public float y1;
        public float x2;
        public float y2;

        public Rect(float x1, float y1, float x2, float y2)
        {
            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;
        }
        public static Rect createFromWH(float x1, float y1, float w, float h)
        {
            return new Rect(x1, y1, x1 + w, y1 + h);
        }
        public Rect(Point topLeft, Point botRight)
        {
            x1 = topLeft.x;
            y1 = topLeft.y;
            x2 = botRight.x;
            y2 = botRight.y;
        }
        public float w()
        {
            return Math.Abs(x2 - x1);
        }

        public float h()
        {
            return Math.Abs(y2 - y1);
        }
        public float area()
        {
            return w() * h();
        }
        public Point center()
        {
            return new Point((x1 + x2) / 2, (y1 + y2) / 2);
        }
        public string toString()
        {
            return x1.ToString() + ", " + y1.ToString() + ", " + x2.ToString() + ", " + y2.ToString();
        }
        
        public List<Point> getPoints()
        {
            return new List<Point>() { new Point(x1, y1), new Point(x2, y1), new Point(x2, y2), new Point(x1, y2) };
        }

        public Shape getShape()
        {
            List<Point> points = getPoints();
            return new Shape(points);
        }

        public bool overlaps(Rect other)
        {
            // If one rectangle is on left side of other
            if (x1 > other.x2 || other.x1 > x2)
                return false;
            // If one rectangle is above other
            if (y1 > other.y2 || other.y1 > y2)
                return false;
            return true;
        }

        public bool containsPoint(Point point)
        {
            return point.x > x1 && point.x < x2 && point.y > y1 && point.y < y2;
        }
    }
}
