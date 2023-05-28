using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace MMXOnline
{
    public class Flag : Actor
    {
        public int alliance = 0;
        public Point pedestalPos;
        public Character chr;
        public float timeDropped = 0;
        public bool pickedUpOnce;
        public FlagPedestal pedestal;
        public float killFeedThrottleTime;
        public float? updraftY;
        public bool nonOwnerHasUpdraft;
        public List<UpdraftParticle> particles = new List<UpdraftParticle>();
        public float pickupCooldown;

        public Flag(int alliance, Point pos, ushort? netId, bool ownedByLocalPlayer) : base(alliance == GameMode.blueAlliance ? "blue_flag" : "red_flag", pos, netId, ownedByLocalPlayer, false)
        {
            this.alliance = alliance;
            collider.wallOnly = true;
            setzIndex(ZIndex.Character - 2);
            for (int i = 0; i < 4; i++)
            {
                particles.Add(getRandomParticle(i * (UpdraftParticle.maxTime * 0.25f)));
            }
        }

        public override void onStart()
        {
            var hit = Global.level.raycast(pos.addxy(0, -10), pos.addxy(0, 60), new List<Type>() { typeof(Wall), typeof(Ladder) });
            pos = (Point)hit.hitData.hitPoint;
            pedestal = new FlagPedestal(alliance, (Point)hit.hitData.hitPoint, null, ownedByLocalPlayer);
            pedestalPos = pedestal.pos;
        }

        public override void update()
        {
            base.update();
            Helpers.decrementTime(ref pickupCooldown);
            if (!ownedByLocalPlayer) return;

            if (killFeedThrottleTime > 0)
            {
                killFeedThrottleTime += Global.spf;
                if (killFeedThrottleTime > 1) killFeedThrottleTime = 0;
            }

            if (chr != null && !Global.level.gameObjects.Contains(chr))
            {
                dropFlag();
            }
            else if (chr != null && chr.isWarpOut())
            {
                dropFlag();
            }

            if (chr == null) 
            {
                if (Global.level.gameMode.isOvertime())
                {
                    timeDropped += Global.spf * 5;
                }
                else
                {
                    timeDropped += Global.spf;
                }
            }
            
            if (updraftY == null)
            {
                float? gottenUpdraftY = getUpdraftY();
                if (gottenUpdraftY != null)
                {
                    updraftY = gottenUpdraftY;
                    vel.y = 0;
                }
            }
            if (updraftY != null)
            {
                if (grounded)
                {
                    removeUpdraft();
                }
                else
                {
                    if (pos.y > updraftY)
                    {
                        gravityModifier = -1;
                        if (vel.y < -100) vel.y = -100;
                    }
                    else
                    {
                        gravityModifier = 1;
                    }
                }
            }
            
            if (timeDropped > 30 && pickedUpOnce)
            {
                returnFlag();
            }
        }

        public float? getUpdraftY()
        {
            if (grounded) return null;
            if (pos.y > Math.Min(Global.level.killY, Global.level.height))
            {
                return Global.level.killY - 175;
            }
            var hitKillZones = Global.level.getTriggerList(this, 0, 0, null, new Type[] { typeof(KillZone) });
            if (hitKillZones.Count > 0 && hitKillZones[0].otherCollider != null && hitKillZones[0].gameObject is KillZone kz && kz.killInvuln)
            {
                return hitKillZones[0].otherCollider.shape.minY - 175;
            }
            return null;
        }

        public void removeUpdraft()
        {
            gravityModifier = 1;
            stopMoving();
            updraftY = null;
        }

        // Only humans can take flags from bots
        public bool canTakeFlag(Character taker, Character holder)
        {
            /*
            if (taker == null || holder == null) return false;
            if (taker == holder) return false;
            return !taker.player.isBot && holder.player.isBot;
            */
            return false;
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (!ownedByLocalPlayer) return;
            if (other.otherCollider?.flag == (int)HitboxFlag.Hitbox) return;
            if (pickupCooldown > 0) return;

            var chr = other.gameObject as Character;
            if (this.chr != null && !canTakeFlag(chr, this.chr)) return;
            if (chr != null && chr.player.alliance != alliance && chr.canPickupFlag())
            {
                pickupFlag(chr);
            }
        }

        public void pickupFlag(Character chr)
        {
            removeUpdraft();
            chr.onFlagPickup(this);
            timeDropped = 0;
            this.chr = chr;
            useGravity = false;
            pickedUpOnce = true;
            Global.level.gameMode.addKillFeedEntry(new KillFeedEntry(chr.player.name + " took flag", chr.player.alliance, chr.player), true);

            if (chr.ai != null && chr.ai.aiState is FindPlayer)
            {
                (chr.ai.aiState as FindPlayer).setDestNodePos();
            }
        }

        public void dropFlag()
        {
            if (chr != null)
            {
                removeUpdraft();
                Global.level.gameMode.addKillFeedEntry(new KillFeedEntry(chr.player.name + " dropped flag", chr.player.alliance, chr.player), true);
                useGravity = true;
                chr = null;
            }
        }

        public void returnFlag()
        {
            removeUpdraft();
            timeDropped = 0;
            pickedUpOnce = false;
            var team = alliance == GameMode.blueAlliance ? "Blue " : "Red ";
            if (killFeedThrottleTime == 0)
            {
                killFeedThrottleTime += Global.spf;
                Global.level.gameMode.addKillFeedEntry(new KillFeedEntry(team + "flag returned", alliance), true);
            }
            useGravity = true;
            if (chr != null) chr.flag = null;
            chr = null;
            pos = pedestalPos.clone();
        }

        public UpdraftParticle getRandomParticle(float time)
        {
            return new UpdraftParticle(new Point(pos.x + Helpers.randomRange(-20, 20), pos.y + Helpers.randomRange(50, 100)), time);
        }

        public bool hasUpdraft()
        {
            if (ownedByLocalPlayer)
            {
                return updraftY != null;
            }
            else
            {
                return nonOwnerHasUpdraft;
            }
        }

        public override void render(float x, float y)
        {
            if (hasUpdraft())
            {
                for (int i = particles.Count - 1; i >= 0; i--)
                {
                    particles[i].time += Global.spf;
                    if (particles[i].time > UpdraftParticle.maxTime)
                    {
                        particles[i] = getRandomParticle(0);
                    }
                    else
                    {
                        particles[i].pos.inc(new Point(0, -Global.spf * 200));
                    }
                    Point pos = particles[i].pos;
                    //DrawWrappers.DrawLine(pos.x, pos.y, pos.x, pos.y - 20, Color.White, 1, ZIndex.Foreground, true);
                    DrawWrappers.DrawCircle(pos.x, pos.y, 1, true, new Color(255, 255, 255, (byte)(255 * (particles[i].time / UpdraftParticle.maxTime))), 1, ZIndex.Foreground, true);
                }
            }

            // To avoid latency of flag not sticking to character in online
            if (Global.serverClient != null)
            {
                foreach (var player in Global.level.players)
                {
                    if (player.character != null && player.character.flag == this)
                    {
                        Point centerPos = player.character.getCenterPos();
                        base.render(centerPos.x - pos.x, centerPos.y - pos.y);
                        return;
                    }
                }
            }

            if (pickedUpOnce && timeDropped > 0 && chr == null)
            {
                drawSpinner(1 - (timeDropped / 30));
            }

            base.render(x, y);
        }

        public void drawSpinner(float progress)
        {
            float cx = pos.x + 8;
            float cy = pos.y - 40;
            float ang = -90;
            float radius = 3f;
            float thickness = 1.25f;
            int count = 40;

            for (int i = 0; i < count; i++)
            {
                float angCopy = ang;
                DrawWrappers.deferredTextDraws.Add(() => DrawWrappers.DrawCircle(
                    (-Global.level.camX + cx + Helpers.cosd(angCopy) * radius) / Global.viewSize,
                    (-Global.level.camY + cy + Helpers.sind(angCopy) * radius) / Global.viewSize,
                    thickness / Global.viewSize, true, Color.Black, 1, ZIndex.HUD, isWorldPos: false));
                ang += (360f / count);
            }

            for (int i = 0; i < count * progress; i++)
            {
                float angCopy = ang;
                DrawWrappers.deferredTextDraws.Add(() => DrawWrappers.DrawCircle(
                    (-Global.level.camX + cx + Helpers.cosd(angCopy) * radius) / Global.viewSize,
                    (-Global.level.camY + cy + Helpers.sind(angCopy) * radius) / Global.viewSize,
                    (thickness - 0.5f) / Global.viewSize, true, alliance == GameMode.redAlliance ? Color.Red : Color.Blue, 1, ZIndex.HUD, isWorldPos: false));
                ang += (360f / count);
            }
        }
    }

    public class UpdraftParticle
    {
        public Point pos;
        public float time;
        public const float maxTime = 0.25f;
        public UpdraftParticle(Point pos, float time)
        {
            this.pos = pos;
            this.time = time;
        }
    }

    public class FlagPedestal : Actor
    {
        public int alliance = 0;
        public FlagPedestal(int alliance, Point pos, ushort? netId, bool ownedByLocalPlayer) : base("flag_pedastal", pos, netId, ownedByLocalPlayer, false)
        {
            this.alliance = alliance;
            useGravity = false;
            setzIndex(ZIndex.Character - 1);
            if (this.alliance == GameMode.blueAlliance)
            {
                addRenderEffect(RenderEffectType.BlueShadow);
            }
            else
            {
                addRenderEffect(RenderEffectType.RedShadow);
            }
        }

        public override void onCollision(CollideData other) 
        {
            base.onCollision(other);
            if (!ownedByLocalPlayer) return;
            if (other.otherCollider?.flag == (int)HitboxFlag.Hitbox) return;

            var chr = other.gameObject as Character;
            if (chr != null && chr.flag != null && chr.player.alliance == alliance) 
            {
                chr.flag.returnFlag();
                chr.flag = null;
                if (chr.ai != null)
                {
                    chr.ai.changeState(new FindPlayer(chr));
                }
                chr.player.scrap += 5;
                RPC.actorToggle.sendRpc(chr.netId, RPCActorToggleType.AwardScrap);

                var msg = chr.player.name + " scored";
                Global.level.gameMode.addKillFeedEntry(new KillFeedEntry(msg, chr.player.alliance, chr.player), true);
                var ctf = Global.level.gameMode as CTF;
                if (Global.isHost)
                {
                    if (alliance == GameMode.blueAlliance)
                    {
                        ctf.bluePoints++;
                    }
                    else
                    {
                        ctf.redPoints++;
                    }
                    Global.level.gameMode.syncTeamScores();
                }
            }
        }
    }
}