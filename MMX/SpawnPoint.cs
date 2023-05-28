using System;
using System.Collections.Generic;
using System.Text;

namespace MMXOnline
{
    public class SpawnPoint
    {
        public string name;
        public Point pos;
        public int xDir;
        public int alliance;
        public SpawnPoint(string name, Point point, int xDir, int alliance)
        {
            this.name = name;
            pos = point;
            this.xDir = xDir == 0 ? 1 : xDir;
            this.alliance = alliance;
        }

        public bool occupied()
        {
            //if(this.name !== "Spawn Point2") return true; //Beginning of level
            if (Global.level.is1v1() || Global.level.isTraining()) return false;
            var nearbyChars = getActorsInRadius(pos, 30);
            if (nearbyChars.Count > 0) return true;
            return false;
        }

        public List<Actor> getActorsInRadius(Point pos, float radius)
        {
            var actors = new List<Actor>();
            foreach (var go in Global.level.gameObjects)
            {
                var chr = go as Character;
                if (chr != null)
                {
                    if (chr.abstractedActor().pos.distanceTo(pos) < radius)
                    {
                        actors.Add(chr);
                    }
                }
            }
            return actors;
        }


        public float getGroundY()
        {
            var hit = Global.level.raycast(pos, pos.addxy(0, 60), new List<Type> { typeof(Wall) });
            if (hit == null) return 0;
            return ((Point)hit.hitData.hitPoint).y - 1;
        }

    }
}
