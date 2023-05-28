using System;
using System.Collections.Generic;
using System.Text;

namespace MMXOnline
{
    public class Frame
    {
        public Rect rect;
        public float duration;
        public Point offset;
        public List<Collider> hitboxes;
        public List<Point> POIs;
        public List<string> POITags;
        public Point? headPos;

        public Frame(Rect rect, float duration, Point offset)
        {
            this.rect = rect;
            this.duration = duration;
            this.offset = offset;
            hitboxes = new List<Collider>();
            POIs = new List<Point>();
            POITags = new List<string>();
        }

        public Point? getBusterOffset()
        {
            if (POIs.Count > 0)
                return POIs[0];
            return null;
        }

        public Frame clone()
        {
            var clonedFrame = (Frame)MemberwiseClone();
            clonedFrame.hitboxes = new List<Collider>();
            foreach (Collider collider in hitboxes)
            {
                clonedFrame.hitboxes.Add(collider.clone());
            }
            return clonedFrame;
        }
    }
}