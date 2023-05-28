using System;
using System.Collections.Generic;
using System.Text;

namespace MMXOnline
{
    public struct GridCoords
    {
        public int i;
        public int j;

        public GridCoords(int i, int j)
        {
            this.i = i;
            this.j = j;
        }

        public GridCoords(Point pos)
        {
            i = (int)Math.Floor(pos.y / 8);
            j = (int)Math.Floor(pos.x / 8);
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override bool Equals(object o)
        {
            return this == (GridCoords)o;
        }

        public static bool operator ==(GridCoords mine, GridCoords other)
        {
            return mine.i == other.i && mine.j == other.j;
        }

        public static bool operator !=(GridCoords mine, GridCoords other)
        {
            return mine.i != other.i || mine.j != other.j;
        }

        public float distTo(GridCoords other)
        {
            return (float)Math.Sqrt(Math.Pow(i - other.i, 2) + Math.Pow(j - other.j, 2));
        }
    }
}
