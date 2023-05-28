using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class ParallaxSprite
    {
        public Sprite sprite;
        public Point pos;
        public int xDir;
        public int yDir;
        public int frameIndex;
        public float frameTime;

        public ParallaxSprite(string spriteName, Point pos, int xDir, int yDir)
        {
            this.sprite = Global.sprites[spriteName].clone();
            this.pos = pos;
            this.xDir = xDir;
            this.yDir = yDir;
        }

        public void render(float x, float y)
        {
            sprite.update();

            var currentFrame = sprite.frames[frameIndex];
            var offsetX = xDir * currentFrame.offset.x;
            var offsetY = yDir * currentFrame.offset.y;

            // Don't draw actors out of the screen for optimization
            var alignOffset = sprite.getAlignOffset(frameIndex, xDir, yDir);
            var rx = pos.x + x + offsetX + alignOffset.x;
            var ry = pos.y + y + offsetY + alignOffset.y;
            var rect = new Rect(rx, ry, rx + currentFrame.rect.w(), ry + currentFrame.rect.h());
            var camRect = new Rect(Global.level.camX, Global.level.camY, Global.level.camX + Global.viewScreenW, Global.level.camY + Global.viewScreenH);
            if (rect.overlaps(camRect))
            {
                sprite.draw(sprite.frameIndex, pos.x + x, pos.y + y, xDir, yDir, null, 1, 1, 1, ZIndex.Parallax + 100);
            }
        }
    }
}
