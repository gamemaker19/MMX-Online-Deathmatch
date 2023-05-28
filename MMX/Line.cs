using System;
using System.Collections.Generic;
using System.Text;

namespace MMXOnline
{
    public struct Line
    {
        public Point point1;
        public Point point2;
        public Line(Point point1, Point point2)
        {
            this.point1 = point1;
            this.point2 = point2;
        }

        // Given three colinear points p, q, r, the function checks if
        // point q lies on line segment 'pr'
        public bool onSegment(Point p, Point q, Point r)
        {
            if (q.x <= Math.Max(p.x, r.x) && q.x >= Math.Min(p.x, r.x) &&
                q.y <= Math.Max(p.y, r.y) && q.y >= Math.Min(p.y, r.y))
                return true;
            return false;
        }

        // To find orientation of ordered triplet (p, q, r).
        // The function returns following values
        // 0 --> p, q and r are colinear
        // 1 --> Clockwise
        // 2 --> Counterclockwise
        public int orientation(Point p, Point q, Point r)
        {
            // See https://www.geeksforgeeks.org/orientation-3-ordered-points/
            // for details of below formula.
            var val = (q.y - p.y) * (r.x - q.x) -
                      (q.x - p.x) * (r.y - q.y);

            if (val == 0) return 0;  // colinear
            return (val > 0) ? 1 : 2; // clock or counterclock wise
        }

        public float x1 { get { return point1.x; } }
        public float y1 { get { return point1.y; } }
        public float x2 { get { return point2.x; } }
        public float y2 { get { return point2.y; } }

        public LineIntersectionResult checkLineIntersection(float line1StartX, float line1StartY, float line1EndX, float line1EndY, float line2StartX, float line2StartY, float line2EndX, float line2EndY)
        {
            // if the lines intersect, the result contains the x and y of the intersection (treating the lines as infinite) and booleans for whether line segment 1 or line segment 2 contain the point
            float denominator, a, b, numerator1, numerator2;
            LineIntersectionResult result = new LineIntersectionResult()
            {
                x = null,
                y = null,
                onLine1 = false,
                onLine2 = false
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
            result.x = line1StartX + (a* (line1EndX - line1StartX));
            result.y = line1StartY + (a* (line1EndY - line1StartY));
            /*
            // it is worth noting that this should be the same as:
            x = line2StartX + (b * (line2EndX - line2StartX));
            y = line2StartX + (b * (line2EndY - line2StartY));
            */
            // if line1 is a segment and line2 is infinite, they intersect if:
            if (a > 0 && a< 1) {
                result.onLine1 = true;
            }
            // if line2 is a segment and line1 is infinite, they intersect if:
            if (b > 0 && b< 1) {
                result.onLine2 = true;
            }
            // if line1 and line2 are segments, they intersect if both of the above are true
            return result;
        }

        public Point? getIntersectPoint(Line other)
        {
            //https://www.geeksforgeeks.org/check-if-two-given-line-segments-intersect/
            var doesIntersect = false;
            Point? coincidePoint = null;
            var p1 = point1;
            var q1 = point2;
            var p2 = other.point1;
            var q2 = other.point2;
            // Find the four orientations needed for general and
            // special cases
            var o1 = orientation(p1, q1, p2);
            var o2 = orientation(p1, q1, q2);
            var o3 = orientation(p2, q2, p1);
            var o4 = orientation(p2, q2, q1);

            // General case
            if (o1 != o2 && o3 != o4) {
              doesIntersect = true;
            }

            // Special Cases
            // p1, q1 and p2 are colinear and p2 lies on segment p1q1
            if (o1 == 0 && onSegment(p1, p2, q1)) {
                coincidePoint = p2;
            }
            // p1, q1 and q2 are colinear and q2 lies on segment p1q1
            else if (o2 == 0 && onSegment(p1, q2, q1)) {
                coincidePoint = q2;
            }
            // p2, q2 and p1 are colinear and p1 lies on segment p2q2
            else if (o3 == 0 && onSegment(p2, p1, q2)) {
                coincidePoint = p1;
            }
            // p2, q2 and q1 are colinear and q1 lies on segment p2q2
            else if (o4 == 0 && onSegment(p2, q1, q2)) {
                coincidePoint = q1;
            }
    
            if(coincidePoint != null) doesIntersect = true;
            if(!doesIntersect) return null;

            if(coincidePoint != null) return coincidePoint;
            var intersection = checkLineIntersection(x1, y1, x2, y2, other.x1, other.y1, other.x2, other.y2);
            if (intersection.x != null && intersection.y != null)
            {
                return new Point((float)intersection.x, (float)intersection.y);
            }
            return new Point((x1 + x2) / 2, (y1 + y2) / 2);
        }

        public float slope
        {
            get
            {
                if (x1 == x2) return Single.NaN;
                return (y1 - y2) / (x1 - x2);
            }
        }

        public float yInt
        {
            get
            {
                if (x1 == x2) return y1 == 0 ? 0 : Single.NaN;
                if (y1 == y2) return y1;
                return y1 - slope * x1;
            }
        }

        public float xInt
        {
            get
            {
                if (y1 == y2) return x1 == 0 ? 0 : Single.NaN;
                if (x1 == x2) return x1;
                return (-1 * ((slope * x1 - y1)) / slope);
            }
        }

        public override string ToString()
        {
            return point1.ToString() + " " + point2.ToString();
        }

        public bool equals(Line other)
        {
            return (point1.equals(other.point1) && point2.equals(other.point2)) || (point1.equals(other.point2) && point2.equals(other.point1));
        }

        public Point closestPointOnLine(Point pos)
        {
            return FindNearestPointOnLine(point1, point2, pos);
        }

        private Point FindNearestPointOnLine(Point origin, Point end, Point point)
        {
            //Get heading
            Point heading = end.subtract(origin);
            float magnitudeMax = heading.magnitude;
            heading = heading.normalize();

            //Do projection from the point but clamp it
            Point lhs = point.subtract(origin);
            float dotP = lhs.dotProduct(heading);
            dotP = Math.Clamp(dotP, 0f, magnitudeMax);
            return origin.add(heading.times(dotP));
        }
    }

    public struct IntersectData
    {
        public Point intersectPoint;
        public Point normal;

        public IntersectData(Point intersectPoint, Point normal)
        {
            this.intersectPoint = intersectPoint;
            this.normal = normal;
        }
    }

    public struct LineIntersectionResult
    {
        public float? x;
        public float? y;
        public bool onLine1;
        public bool onLine2;
    }
}
