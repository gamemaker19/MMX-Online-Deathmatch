using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace MMXOnline
{
    public class ChargeParticle : Actor
    {
        public float time;
        public ChargeParticle(Point pos, float time, ushort? netId) : base("charge_part_1", new Point(pos.x, pos.y), netId, true, true)
        {
            this.time = time;
        }

        public override void update() {
            base.update();
        }

        public override void render(float x, float y)
        {
        }
    }

    public class ChargeEffect 
    {
        public List<Point> origPoints;
        public List<ChargeParticle> chargeParts;
        public bool active = false;

        public ChargeEffect()
        {
            chargeParts = new List<ChargeParticle>();
            var radius = 24;

            var angle = 0;
            var point1 = new Point(Helpers.sind(angle) * radius, Helpers.cosd(angle) * radius); angle += 45;
            var point2 = new Point(Helpers.sind(angle) * radius, Helpers.cosd(angle) * radius); angle += 45;
            var point3 = new Point(Helpers.sind(angle) * radius, Helpers.cosd(angle) * radius); angle += 45;
            var point4 = new Point(Helpers.sind(angle) * radius, Helpers.cosd(angle) * radius); angle += 45;
            var point5 = new Point(Helpers.sind(angle) * radius, Helpers.cosd(angle) * radius); angle += 45;
            var point6 = new Point(Helpers.sind(angle) * radius, Helpers.cosd(angle) * radius); angle += 45;
            var point7 = new Point(Helpers.sind(angle) * radius, Helpers.cosd(angle) * radius); angle += 45;
            var point8 = new Point(Helpers.sind(angle) * radius, Helpers.cosd(angle) * radius); angle += 45;

            origPoints = new List<Point>() {
              point1, point2, point3, point4, point5, point6, point7, point8
            };

            chargeParts = new List<ChargeParticle>()
            {
                new ChargeParticle(point1.clone(), 0, null),
                new ChargeParticle(point2.clone(), 3, null),
                new ChargeParticle(point3.clone(), 0, null),
                new ChargeParticle(point4.clone(), 1.5f, null),
                new ChargeParticle(point5.clone(), -1.5f, null),
                new ChargeParticle(point6.clone(), -3, null),
                new ChargeParticle(point7.clone(), -1.5f, null),
                new ChargeParticle(point8.clone(), -1.5f, null)
            };
        }

        public void stop()
        {
            active = false;
        }

        public void reset()
        {
            chargeParts[0].time = 0;
            chargeParts[1].time = 3;
            chargeParts[2].time = 0;
            chargeParts[3].time = 1.5f;
            chargeParts[4].time = -1.5f;
            chargeParts[5].time = -3;
            chargeParts[6].time = -1.5f;
            chargeParts[7].time = -1.5f;
        }

        public void update(float chargeLevel)
        {
            active = true;
            for(int i = 0; i < chargeParts.Count; i++)
            {
                var part = chargeParts[i];
                if (part.time > 0)
                {
                    part.pos.x = Helpers.moveTo(part.pos.x, 0, Global.spf * 70);
                    part.pos.y = Helpers.moveTo(part.pos.y, 0, Global.spf * 70);
                }
                var chargePart = "charge_part_" + chargeLevel.ToString();
                part.changeSprite(chargePart, true);
                part.time += Global.spf * 20;
                if (part.time > 3)
                {
                    part.time = -3;
                    part.pos.x = origPoints[i].x;
                    part.pos.y = origPoints[i].y;
                }
            }

        }

        public void render(Point centerPos)
        {
            for (var i = 0; i < chargeParts.Count; i++)
            {
                var part = chargeParts[i];
                if (!active)
                {
                    part.sprite.visible = false;
                }
                else if (part.time > 0)
                {
                    part.sprite.visible = true;

                    float x = centerPos.x + part.pos.x;
                    float y = centerPos.y + part.pos.y;
                    float halfWidth = 10;

                    var rect = new Rect(x - halfWidth, y - halfWidth, x + halfWidth, y + halfWidth);
                    var camRect = new Rect(Global.level.camX, Global.level.camY, Global.level.camX + Global.viewScreenW, Global.level.camY + Global.viewScreenH);
                    if (rect.overlaps(camRect))
                    {
                        part.sprite.draw((int)Math.Round(part.time), x, y, 1, 1, null, 1, 1, 1, ZIndex.Foreground);
                    }
                }
                else
                {
                    part.sprite.visible = false;
                }
            }
        }

        public void destroy()
        {
            foreach (var chargePart in chargeParts)
            {
                chargePart.destroySelf(null, null);
            }
        }

    }

    public class DieParticleActor : Actor
    {
        public DieParticleActor(string spriteName, Point pos) : base(spriteName, pos, null, true, false)
        {
        }

        public override void render(float x, float y)
        {
        }
    }

    public class DieEffectParticles
    {
        public Point centerPos;
        public float time = 0;
        public float ang = 0;
        public float alpha = 1;
        public List<Actor> dieParts = new List<Actor>();
        public bool destroyed = false;

        public DieEffectParticles(Point centerPos, int charNum)
        {
            this.centerPos = centerPos;
            for (var i = ang; i < ang + 360; i += 22.5f)
            {
                var x = this.centerPos.x + Helpers.cosd(i) * time * 150;
                var y = this.centerPos.y + Helpers.sind(i) * time * 150;
                var diePartSprite = charNum == 1 ? "die_particle_zero" : "die_particle";
                var diePart = new DieParticleActor(diePartSprite, new Point(centerPos.x, centerPos.y));
                dieParts.Add(diePart);
            }
        }

        public void render(float offsetX, float offsetY)
        {
            var counter = 0;
            for (var i = ang; i < ang + 360; i += 22.5f)
            {
                if (counter >= dieParts.Count) continue;
                var diePart = dieParts[counter];
                if (diePart == null) continue;

                var x = centerPos.x + Helpers.cosd(i) * time * 150;
                var y = centerPos.y + Helpers.sind(i) * time * 150;
                float halfWidth = 10;

                var rect = new Rect(x - halfWidth, y - halfWidth, x + halfWidth, y + halfWidth);
                var camRect = new Rect(Global.level.camX, Global.level.camY, Global.level.camX + Global.viewScreenW, Global.level.camY + Global.viewScreenH);
                if (rect.overlaps(camRect))
                {
                    int frameIndex = (int)MathF.Round(time * 20) % diePart.sprite.frames.Count;
                    diePart.sprite.draw(frameIndex, x + offsetX, y + offsetY, 1, 1, null, alpha, 1, 1, ZIndex.Foreground);
                }

                counter++;
            }

        }

        public void update()
        {
            time += Global.spf;
            alpha = Helpers.clamp01(1 - time * 0.5f);
            ang += Global.spf * 100;

            if (alpha <= 0)
            {
                destroy();
            }
        }

        public void destroy()
        {
            foreach (var diePart in dieParts)
            {
                diePart.destroySelf();
            }
            destroyed = true;
        }
    }

    public class Effect
    {
        public Point pos;
        public float effectTime;
        public Effect(Point pos)
        {
            this.pos = pos;
            Global.level.addEffect(this);
        }

        public virtual void update()
        {
            effectTime += Global.spf;
        }

        public virtual void render(float offsetX, float offsetY)
        {

        }

        public virtual void destroySelf()
        {
            Global.level.effects.Remove(this);
        }

    }

    public class ExplodeDieEffect : Effect
    {
        public float timer = 3;
        public float spawnTime = 0;
        public int radius;
        public Anim exploder;
        public bool isExploderVisible;
        public bool destroyed;
        public Player owner;
        public bool silent;
        public Actor host;

        public ExplodeDieEffect(Player owner, Point centerPos, Point animPos, string spriteName, int xDir, long zIndex, bool isExploderVisible, int radius, float maxTime, bool isMaverick) : base(centerPos)
        {
            this.owner = owner;
            this.radius = radius;
            timer = maxTime;
            if (!owner.ownedByLocalPlayer) return;

            exploder = new Anim(animPos, spriteName, xDir, owner.getNextActorNetId(), false, sendRpc: true);
            exploder.zIndex = zIndex;
            exploder.sprite.frameIndex = exploder.sprite.frames.Count - 1;
            exploder.visible = isExploderVisible;
            exploder.maverickFade = isMaverick;
        }

        public static ExplodeDieEffect createFromActor(Player owner, Actor actor, int radius, float maxTime, bool isMaverick, Point? overrideCenterPos = null)
        {
            return new ExplodeDieEffect(owner, overrideCenterPos ?? actor.getCenterPos(), actor.pos, actor.sprite.name, actor.xDir, actor.zIndex, true, radius, maxTime, isMaverick);
        }

        public override void update()
        {
            if (!owner.ownedByLocalPlayer)
            {
                destroySelf();
                return;
            }

            base.update();

            if (host != null)
            {
                pos = host.pos;
            }

            timer -= Global.spf;
            if (timer <= 0)
            {
                exploder.destroySelf();
                destroySelf();
                return;
            }

            spawnTime += Global.spf;
            if (spawnTime >= 0.125f)
            {
                spawnTime = 0;
                int randX = Helpers.randomRange(-radius, radius);
                int randY = Helpers.randomRange(-radius, radius);
                var randomPos = pos.addxy(randX, randY);
                
                if (owner != null && owner.ownedByLocalPlayer)
                {
                    new Anim(randomPos, "explosion", 1, owner.getNextActorNetId(), true, sendRpc: true);
                }

                if (!exploder.maverickFade)
                {
                    if (!silent)
                    {
                        exploder.playSound("explosion", sendRpc: true);
                    }
                }
            }
        }

        public void changeSprite(string newSprite)
        {
            exploder?.changeSprite(newSprite, true);
        }

        public override void destroySelf()
        {
            base.destroySelf();
            destroyed = true;
            if (exploder != null && !exploder.destroyed)
            {
                exploder.destroySelf();
            }
        }
    }

    public class DieEffect : Effect
    {
        public float timer = 100;
        public float spawnCount = 0;
        public List<DieEffectParticles> dieEffects = new List<DieEffectParticles>();
        public float repeatCount = 0;
        public int charNum;

        public DieEffect(Point centerPos, int charNum) : base(centerPos)
        {
            this.charNum = charNum;
        }

        public override void update() {
            base.update();
            var repeat = 1;
            var repeatPeriod = 0.5;
            timer += Global.spf;
            if (timer > repeatPeriod)
            {
                timer = 0;
                repeatCount++;
                if (repeatCount > repeat)
                {
                }
                else
                {
                    var dieEffect = new DieEffectParticles(pos, charNum);
                    dieEffects.Add(dieEffect);
                }
            }
            foreach (var dieEffect in dieEffects)
            {
                if (!dieEffect.destroyed)
                    dieEffect.update();
            }
            if (dieEffects[0].destroyed)
                destroySelf();
        }

        public override void render(float offsetX, float offsetY) 
        {
            foreach (var dieEffect in dieEffects)
            {
                if (!dieEffect.destroyed)
                    dieEffect.render(offsetX, offsetY);
            }
        }
    }
}
