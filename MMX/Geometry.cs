using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace MMXOnline
{
    //Umbrella class for walls, nav meshes, ladders, etc.
    public class Geometry : GameObject
    {
        public string name { get; set; }
        public Collider collider { get; set; }

        public Geometry(string name, List<Point> points)
        {
            this.name = name;
            collider = new Collider(points, false, null, true, true, 0, new Point(0, 0));
        }

        public virtual void preUpdate()
        {

        }

        public virtual void update()
        {

        }

        public virtual void netUpdate()
        {

        }

        public virtual void render(float x, float y)
        {
            if (Global.showHitboxes && this is not BackwallZone)
            {
                DrawWrappers.DrawPolygon(collider.shape.clone(x, y).points, new Color(0, 0, 255, 128), true, ZIndex.HUD + 100, true);
            }
            if (Global.showAIDebug && this is JumpZone)
            {
                DrawWrappers.DrawPolygon(collider.shape.clone(x, y).points, new Color(0, 0, 255, 128), true, ZIndex.HUD + 100, true);
            }
        }

        public virtual void onCollision(CollideData other)
        {

        }

        public void onStart()
        {
        }

        public void postUpdate()
        {
        }

        public List<Collider> getAllColliders()
        {
            if (collider != null)
            {
                return new List<Collider> { collider };
            }
            return new List<Collider>();
        }

        public Shape? getAllCollidersShape()
        {
            return collider?.shape;
        }
    }
}
