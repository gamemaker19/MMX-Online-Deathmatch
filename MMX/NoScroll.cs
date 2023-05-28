using System;
using System.Collections.Generic;
using System.Text;

namespace MMXOnline
{
    public enum ScrollFreeDir
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    public class NoScroll
    {
        public Shape shape;
        public ScrollFreeDir freeDir;
        public bool snap;
        public string name;
        public NoScroll(string name, Shape shape, ScrollFreeDir dir, bool snap)
        {
            this.name = name;
            this.shape = shape;
            freeDir = dir;
            this.snap = snap;
        }
    }
}
