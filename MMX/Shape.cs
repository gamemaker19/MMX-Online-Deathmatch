using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MMXOnline
{
    public struct Shape
    {
        public List<Point> points;
        public List<Point> normals;
        public float minX;
        public float minY;
        public float maxX;
        public float maxY;

        public Shape(List<Point> points, List<Point> normals = null)
        {
            minX = float.MaxValue;
            minY = float.MaxValue;
            maxX = float.MinValue;
            maxY = float.MinValue;

            this.points = points;
            var isNormalsSet = true;
            if (normals == null)
            {
                normals = new List<Point>();
                isNormalsSet = false;
            }
            for (var i = 0; i < this.points.Count; i++)
            {
                var p1 = this.points[i];
                var p2 = (i == this.points.Count - 1 ? this.points[0] : this.points[i + 1]);

                if (!isNormalsSet)
                {
                    var v = new Point(p2.x - p1.x, p2.y - p1.y);
                    normals.Add(v.leftNormal().normalize());
                }

                if (p1.x < minX) minX = p1.x;
                if (p1.y < minY) minY = p1.y;
                if (p1.x > maxX) maxX = p1.x;
                if (p1.y > maxY) maxY = p1.y;
            }
            this.normals = normals;
        }

        //Called a lot
        public Rect? getNullableRect() 
        {
            if (points.Count != 4) return null;
            if (isRect()) 
            {
                return new Rect(points[0], points[2]);
            }
            return null;
        }

        public bool isRect()
        {
            if (points.Count != 4) return false;
            if (points[0].x == points[3].x && points[1].x == points[2].x && points[0].y == points[1].y && points[2].y == points[3].y)
            {
                return true;
            }
            return false;
        }

        public Rect getRect()
        {
            return new Rect(points[0], points[2]);
        }

        public List<Line> getLines() 
        {
            var lines = new List<Line>();
            for(var i = 0; i< points.Count; i++) 
            {
                var next = i + 1;
                if(next >= points.Count) next = 0;
                lines.Add(new Line(points[i], points[next]));
            }
            return lines;
        }

        public List<Point> getNormals() {
            return normals;
        }

        public bool intersectsLine(Line line)
        {
            var lines = getLines();
            foreach (var myLine in lines)
            {
                if (myLine.getIntersectPoint(line) != null)
                {
                    return true;
                }
            }
            return false;
        }

        public List<CollideData> getLineIntersectCollisions(Line line) 
        {
            var collideDatas = new List<CollideData>();
            var lines = getLines();
            var normals = getNormals();
            for(var i = 0; i < lines.Count; i++) 
            {
                var myLine = lines[i];
                var point = myLine.getIntersectPoint(line);
                if (point != null) 
                {
                    var normal = normals[i];
                    var collideData = new CollideData(null, null, null, false, null, new HitData(normal, new List<Point>() { point.Value }, new List<Line>() { myLine }));
                    collideDatas.Add(collideData);
                }
            }
            return collideDatas;
        }

          //IMPORTANT NOTE- When determining normals, it is always off "other".
          public HitData intersectsShape(Shape other, Point? vel = null) 
          {
            Global.collisionCalls++;
            var pointOutside = false;
            foreach (var point in points) 
            {
                if (!other.containsPoint(point))
                {
                    pointOutside = true;
                    break;
                }
            }
            var pointOutside2 = false;
            foreach (var point in other.points) 
            {
                if (!containsPoint(point))
                {
                    pointOutside2 = true;
                    break;
                }
            }
            if (!pointOutside || !pointOutside2) 
            {
                return new HitData(null, null);
            }

            List<Line> hitLines = new List<Line>();
            var lines1 = getLines();
            var lines2 = other.getLines();
            var hitNormals = new List<Point>();
            var hitPoints = new List<Point>();
            foreach (var line1 in lines1) 
            {
                var normals = other.getNormals();
                for (var i = 0; i < lines2.Count; i++)
                {
                    var line2 = lines2[i];
                    var hitPoint = line1.getIntersectPoint(line2);
                    if (hitPoint != null)
                    {
                        hitNormals.Add(normals[i]);
                        hitPoints.Add(hitPoint.Value);
                        hitLines.Add(line2);
                    }
                }
            }
            if (hitNormals.Count == 0) 
            {
                return null;
            }

            if (vel != null)
            {
                foreach (var normal in hitNormals)
                {
                    var ang = vel.Value.times(-1).angleWith(normal);
                    if (ang < 90)
                    {
                        return new HitData(normal, hitPoints, hitLines);
                    }
                }
            }
            if (hitNormals.Count > 0) 
            {
                return new HitData(hitNormals[0], hitPoints, hitLines);
            }

            return null;
        }

        public bool containsPoint(Point point) 
        {
            Rect? rect = getNullableRect();
            if (rect != null)
            {
                return rect.Value.containsPoint(point);
            }

            var x = point.x;
            var y = point.y;
            var vertices = points;
            // ray-casting algorithm based on
            // http://www.ecse.rpi.edu/Homepages/wrf/Research/Short_Notes/pnpoly.html
            bool inside = false;
            for (int i = 0, j = vertices.Count - 1; i < vertices.Count; j = i++) 
            {
                float xi = vertices[i].x, yi = vertices[i].y;
                float xj = vertices[j].x, yj = vertices[j].y;

                bool intersect = ((yi > y) != (yj > y))
                    && (x<(xj - xi) * (y - yi) / (yj - yi) + xi);
                if (intersect) inside = !inside;
            }
            return inside;
          }


        public Point? getIntersectPoint(Point point, Point dir)
        {
            if (containsPoint(point))
            {
                return point;
            }
            var intersections = new List<Point>();
            var pointLine = new Line(point, point.add(dir));
            foreach (var line in getLines())
            {
                var intersectPoint = line.getIntersectPoint(pointLine);
                if (intersectPoint != null)
                {
                    intersections.Add((Point)intersectPoint);
                }
            }
            if (intersections.Count == 0) return null;

            float minDist = float.MaxValue;
            Point? bestPoint = null;
            foreach (var intersection in intersections)
            {
                float dist = intersection.distanceTo(point);
                if (dist < minDist)
                {
                    minDist = dist;
                    bestPoint = intersection;
                }
            }
            return bestPoint;
        }

        // project vectors on to normal and return min/max value
        public float[] minMaxDotProd(Point normal)
        {
            float? min = null;
            float? max = null;
            foreach (var point in points)
            {
                var dp = point.dotProduct(normal);
                if (min == null || dp < min) min = dp;
                if (max == null || dp > max) max = dp;
            }
            return new float[] { (float)min, (float)max };
        }

        public Point? checkNormal(Shape other, Point normal)
        {
            var aMinMax = minMaxDotProd(normal);
            var bMinMax = other.minMaxDotProd(normal);

            //Containment
            float overlap = 0;
            if (aMinMax[0] > bMinMax[0] && aMinMax[1] < bMinMax[1])
            {
                overlap = aMinMax[1] - aMinMax[0];
            }
            if (bMinMax[0] > aMinMax[0] && bMinMax[1] < aMinMax[1])
            {
                overlap = bMinMax[1] - bMinMax[0];
            }
            if (overlap > 0)
            {
                float mins = bMinMax[0] - aMinMax[0];
                float maxs = bMinMax[1] - aMinMax[1];

                if (mins <= 0) mins = float.MaxValue;
                if (maxs <= 0) maxs = float.MaxValue;
                if (mins == float.MaxValue && maxs == float.MaxValue)
                {
                    return null;
                }
                
                //mins = MathF.Abs(mins);
                //maxs = MathF.Abs(maxs);

                // NOTE- depending on which is smaller you may need to
                // negate the separating axis!!
                if (mins < maxs)
                {
                    overlap += mins;
                }
                else
                {
                    overlap += maxs;
                }
                var correction = normal.times(overlap);
                return correction;
            }

            if (aMinMax[0] <= bMinMax[1] && aMinMax[1] >= bMinMax[0])
            {
                var correction = normal.times(bMinMax[1] - aMinMax[0]);
                return correction;
            }

            return null;
        }

        //Get the min trans vector to get this shape out in shape b.
        public Point? getMinTransVector(Shape b) 
        {
            Global.collisionCalls++;
            var correctionVectors = new List<Point>();
            var thisNormals = getNormals();
            var bNormals = b.getNormals();

            foreach (var normal in thisNormals)
            {
                var result = checkNormal(b, normal);
                if (result != null)
                {
                    correctionVectors.Add(result.Value);
                }
            }
            foreach (var normal in bNormals) 
            {
                var result = checkNormal(b, normal);
                if (result != null)
                {
                    correctionVectors.Add(result.Value);
                }
            }
            if (correctionVectors.Count > 0)
            {
                float minMag = float.MaxValue;
                Point? bestPoint = null;
                foreach (var correctionVector in correctionVectors)
                {
                    float magnitude = correctionVector.magnitude;
                    if (magnitude < minMag)
                    {
                        minMag = magnitude;
                        bestPoint = correctionVector;
                    }
                }
                return bestPoint;
            }
            return null;
        }

        public Point? getMinTransVectorDir(Shape b, Point dir)
        {
            dir = dir.normalize();
            Global.collisionCalls++;
            float mag = 0;
            float maxMag = 0;
            foreach (var point in points)
            {
                var line = new Line(point, point.add(dir.times(10000)));
                foreach (var bLine in b.getLines())
                {
                    var intersectPoint = bLine.getIntersectPoint(line);
                    if (intersectPoint != null)
                    {
                        mag = point.distanceTo((Point)intersectPoint);
                        if (mag > maxMag)
                        {
                            maxMag = mag;
                        }
                    }
                }
            }
            foreach (var point in b.points)
            {
                var line = new Line(point, point.add(dir.times(-10000)));
                foreach (var myLine in getLines())
                {
                    var intersectPoint = myLine.getIntersectPoint(line);
                    if (intersectPoint != null)
                    {
                        mag = point.distanceTo((Point)intersectPoint);
                        if (mag > maxMag)
                        {
                            maxMag = mag;
                        }
                    }
                }
            }
            if (maxMag == 0)
            {
                return null;
            }
            return dir.times(maxMag);
        }

        //Get the min trans vector to get this shape into shape b.
        public Point? getSnapVector(Shape b, Point dir)
        {
            float mag = 0;
            var minMag = float.MaxValue;
            foreach (var point in points)
            {
                var line = new Line(point, point.add(dir.times(10000)));
                foreach (var bLine in b.getLines())
                {
                    var intersectPoint = bLine.getIntersectPoint(line);
                    if (intersectPoint != null)
                    {
                        mag = point.distanceTo((Point)intersectPoint);
                        if (mag < minMag)
                        {
                            minMag = mag;
                        }
                    }
                }
            }
            if (mag == 0)
            {
                return null;
            }
            return dir.times(minMag);
        }

        public Shape clone(float x, float y)
        {
            var points = new List<Point>();
            for (var i = 0; i < this.points.Count; i++)
            {
                var point = this.points[i];
                points.Add(new Point(point.x + x, point.y + y));
            }
            return new Shape(points, normals);
        }
    }
}
