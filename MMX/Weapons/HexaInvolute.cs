using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class HexaInvoluteWeapon : Weapon
    {
        public HexaInvoluteWeapon()
        {
            index = (int)WeaponIds.HexaInvolute;
            killFeedIndex = 179;
        }
    }

    public class HexaInvoluteState : CharState
    {
        HexaInvoluteProj proj;
        bool startGrounded;
        float ammoTime;
        public HexaInvoluteState() : base("super", "", "", "")
        {
            superArmor = true;
            immuneToWind = true;
            invincible = true;
        }

        public override void update()
        {
            base.update();

            if (startGrounded && !once)
            {
                //character.move(new Point(0, -100));
            }

            if (!once && character.frameIndex >= 2)
            {
                once = true;
                proj = new HexaInvoluteProj(new HexaInvoluteWeapon(), character.getFirstPOIOrDefault(), 1, player, player.getNextActorNetId(), rpc: true);
            }

            if (proj != null)
            {
                character.usedAmmoLastFrame = true;
                Helpers.decrementTime(ref ammoTime);
                if (ammoTime == 0)
                {
                    ammoTime = 0.125f;
                    player.vileAmmo--;
                }
            }

            if (player.vileAmmo <= 0 || (player.input.isPressed(Control.Special1, player) && stateTime > 1))
            {
                character.changeToIdleOrFall();
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            if (character.grounded)
            {
                startGrounded = true;
            }
            character.useGravity = false;
            character.grounded = false;
            character.stopMoving();
            character.vileHoverTime = character.vileMaxHoverTime;
            character.getOffMK5Platform();
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            proj?.destroySelf();
            character.useGravity = true;
        }
    }

    public class HexaInvolutePart
    {
        public float time;
        public Point pos;
        public float angle;
        public float baseRadius;
        public float maxTime = 0.15f;

        public HexaInvolutePart(Point pos)
        {
            time = 0;
            angle = Helpers.randomRange(0, 360);
            Point randInc = Point.createFromAngle(angle).times(Helpers.randomRange(0, 10));
            this.pos = pos.add(randInc);
            baseRadius = Helpers.randomRange(0.1f, 0.5f);
        }

        public float getAlpha()
        {
            return 1 - 0.5f * (time / maxTime);
        }

        public float getRadius()
        {
            float timeProcess = (time / maxTime);
            return baseRadius * (1 + timeProcess);
        }

        public Point getPos()
        {
            float partX = pos.x + (Helpers.cosd(angle) * time * 50);
            float partY = pos.y + (Helpers.sind(angle) * time * 50);
            return new Point(partX, partY);
        }
    }

    public class HexaInvoluteProj : Projectile
    {
        public Point destPos;
        public float sinDampTime = 1;
        public Anim muzzle;
        float radius = 120;
        public float ang;
        SoundWrapper sound;
        float soundCooldown;
        public List<HexaInvolutePart> parts = new List<HexaInvolutePart>();
        public HexaInvoluteProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 1, player, "empty", Global.defFlinch, 0.15f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.HexaInvolute;
            setIndestructableProperties();
            sprite.hitboxes = new List<Collider>();

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            
            if (ownedByLocalPlayer && owner.character != null)
            {
                incPos(owner.character.deltaPos);
            }

            for (int i = parts.Count - 1; i >= 0; i--)
            {
                parts[i].time += Global.spf;
                if (parts[i].time > parts[i].maxTime)
                {
                    parts.RemoveAt(i);
                }
            }

            Helpers.decrementTime(ref soundCooldown);
            if (soundCooldown == 0)
            {
                sound = owner.character?.playSound("hexaInvolute");
                soundCooldown = 2.1f;
            }

            if (ownedByLocalPlayer)
            {
                ang += Global.spf * 45;
                ang = Helpers.to360(ang);
            }
        }

        float partCooldown;
        public override void render(float x, float y)
        {
            base.render(x, y);
            Helpers.decrementTime(ref partCooldown);
            for (int i = 0; i < 6; i++)
            {
                Point origin = pos;
                
                Point dest = pos.addxy(Helpers.cosd(ang + (i * 60)) * radius, Helpers.sind(ang + (i * 60)) * radius);
                var hitPoint = Global.level.raycast(pos, dest, Helpers.wallTypeList);
                if (hitPoint != null) dest = hitPoint.getHitPointSafe();

                drawLine(origin, dest);
                var hitbox = getHitbox(origin, dest);
                if (sprite.hitboxes.InRange(i)) sprite.hitboxes[i] = hitbox;
                else sprite.hitboxes.Add(hitbox);
                if (partCooldown == 0)
                {
                    if (!Options.main.lowQualityParticles())
                    {
                        parts.Add(new HexaInvolutePart(dest));
                    }
                }
            }
            if (partCooldown == 0)
            {
                if (!Options.main.lowQualityParticles())
                {
                    parts.Add(new HexaInvolutePart(pos));
                }
            }

            foreach (var part in parts)
            {
                Point partPos = part.getPos();
                float partSize = part.getRadius();
                if (Global.level.gameMode.isTeamMode && damager.owner.alliance == GameMode.redAlliance)
                {
                    Global.sprites["vilemk5_super_part2"].draw(0, partPos.x, partPos.y, 1, 1, null, part.getAlpha(), partSize, partSize, zIndex + 1);
                }
                else
                {
                    Global.sprites["vilemk5_super_part"].draw(0, partPos.x, partPos.y, 1, 1, null, part.getAlpha(), partSize, partSize, zIndex + 1);
                }
            }
        }

        public Collider getHitbox(Point origin, Point dest)
        {
            var points = new List<Point>();
            points.Add(origin);
            points.Add(dest);
            var dirTo = origin.directionTo(dest);
            dest = dest.add(dirTo.leftNormal().normalize().times(5));
            points.Add(dest);
            dest = dest.subtract(dirTo);
            points.Add(dest);

            return new Collider(points, true, null, false, false, 0, Point.zero);
        }

        public List<Point> getNodes(Point origin, Point dest)
        {
            List<Point> nodes = new List<Point>();
            int nodeCount = 8;
            Point dirTo = origin.directionTo(dest).normalize();
            float len = origin.distanceTo(dest);
            Point lastNode = origin;
            for (int i = 0; i <= nodeCount; i++)
            {
                Point node = i == 0 ? lastNode : lastNode.add(dirTo.times(len / 8));
                Point randNode = node;
                if (i > 0 && i < nodeCount) randNode = node.addRand(10, 10);
                nodes.Add(randNode);
                lastNode = node;
            }
            return nodes;
        }

        public void drawLine(Point origin, Point dest)
        {
            var col1 = new Color(74, 78, 221);
            var col2 = new Color(61, 113, 255);
            var col3 = new Color(245, 252, 255);
            if (Global.level.gameMode.isTeamMode && damager.owner.alliance == GameMode.redAlliance)
            {
                col1 = new Color(221, 78, 74);
                col2 = new Color(255, 113, 61);
                col3 = new Color(255, 245, 240);
            }

            float sin = MathF.Sin(Global.time * 30);
            var nodes = getNodes(origin, dest);

            for (int i = 1; i < nodes.Count; i++)
            {
                Point startPos = nodes[i - 1];
                Point endPos = nodes[i];
                if (Options.main.lowQualityParticles())
                {
                    DrawWrappers.DrawLine(startPos.x, startPos.y, endPos.x, endPos.y, col3, 2 + sin, 0, true);
                }
                else
                {
                    DrawWrappers.DrawLine(startPos.x, startPos.y, endPos.x, endPos.y, col1, 4 + sin, 0, true);
                    DrawWrappers.DrawLine(startPos.x, startPos.y, endPos.x, endPos.y, col2, 3 + sin, 0, true);
                    DrawWrappers.DrawLine(startPos.x, startPos.y, endPos.x, endPos.y, col3, 2 + sin, 0, true);
                }
            }
        }

        public override void onDestroy()
        {
            base.onDestroy();
            muzzle?.destroySelf();
            if (sound != null && Global.sounds.Contains(sound))
            {
                sound.sound.Stop();
                sound.sound.Dispose();
                Global.sounds.Remove(sound);
                sound = null;
            }
        }
    }
}
