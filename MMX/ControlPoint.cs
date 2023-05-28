using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class ControlPoint : Actor
    {
        public int alliance = 0;
        public int num;
        public bool isInit = false;
        public bool locked = false;
        public bool captured = false;
        public float redCaptureTime;
        public float blueCaptureTime;
        public float redRemainingTime = 180;
        public float blueRemainingTime = 180;
        public float captureTime = 0;
        public float maxCaptureTime = 30;
        public float awardTime;
        public List<Character> chrsOnPoint = new List<Character>();
        public List<Character> defenders = new List<Character>();
        public List<Character> attackers = new List<Character>();
        public NavMeshNode navMeshNode;
        public bool isHill;
        const float captureSpeed = 1;
        public float yOff;
        public byte hillAttackerCountSync;

        // Non-host properties
        public int attackerCount;
        public int defenderCount;

        public ControlPoint(int alliance, Point pos, int num, bool isHill, float maxCaptureTime, float awardTime, ushort netId, bool ownedByLocalPlayer) : 
            base("capture_point", pos, netId, ownedByLocalPlayer, false)
        {
            this.alliance = alliance;
            locked = num == 1 ? false : true;
            this.num = num;
            this.isHill = isHill;
            useGravity = false;
            setzIndex(ZIndex.Character - 2);
            this.maxCaptureTime = maxCaptureTime;
            this.awardTime = awardTime;
            sprite.frameSpeed = 0;
        }

        public override void onStart()
        {
            isInit = true;
            var hit = Global.level.raycast(pos.addxy(0, -10), pos.addxy(0, 60), new List<Type>() { typeof(Wall) });
            pos = hit.hitData.hitPoint.Value.addxy(0, 2 + yOff);
        }

        public override void preUpdate()
        {
            base.preUpdate();
            if (!ownedByLocalPlayer) return;

            chrsOnPoint.Clear();
            defenders.Clear();
            attackers.Clear();
        }

        public override void postUpdate()
        {
            base.postUpdate();

            if (attacked()) addRenderEffect(RenderEffectType.InvisibleFlash);
            else removeRenderEffect(RenderEffectType.InvisibleFlash);

            if (!ownedByLocalPlayer) return;

            if (alliance == GameMode.redAlliance)
            {
                sprite.frameIndex = locked ? 2 : 0;
            }
            else if (alliance == GameMode.blueAlliance)
            {
                sprite.frameIndex = locked ? 3 : 1;
            }
            else
            {
                sprite.frameIndex = 8;
            }

            if (captured == true)
            {
                sync();
                return;
            }

            chrsOnPoint.RemoveAll(c => c?.player == null);
            defenders = chrsOnPoint.Where(c => c.player.alliance == alliance && canDefend(c)).ToList();
            attackers = chrsOnPoint.Where(c => c.player.alliance != alliance && canAttack(c)).ToList();

            if (attacked()) addRenderEffect(RenderEffectType.InvisibleFlash);
            else removeRenderEffect(RenderEffectType.InvisibleFlash);

            if (attacked())
            {
                int attackerAlliance = attackers[0].player.alliance;
                float decisiveCaptureTime = 0;

                if (alliance == GameMode.neutralAlliance)
                {
                    if (attackerAlliance == GameMode.redAlliance)
                    {
                        if (blueCaptureTime > 0)
                        {
                            blueCaptureTime -= Global.spf * attackers.Count * captureSpeed;
                            if (blueCaptureTime < 0) blueCaptureTime = 0;
                            decisiveCaptureTime = blueCaptureTime;
                        }
                        else
                        {
                            redCaptureTime += Global.spf * attackers.Count * captureSpeed;
                            decisiveCaptureTime = redCaptureTime;
                        }
                    }
                    else if (attackerAlliance == GameMode.blueAlliance)
                    {
                        if (redCaptureTime > 0)
                        {
                            redCaptureTime -= Global.spf * attackers.Count * captureSpeed;
                            if (redCaptureTime < 0) redCaptureTime = 0;
                            decisiveCaptureTime = redCaptureTime;
                        }
                        else
                        {
                            blueCaptureTime += Global.spf * attackers.Count * captureSpeed;
                            decisiveCaptureTime = blueCaptureTime;
                        }
                    }
                }
                else
                {
                    captureTime += Global.spf * attackers.Count * captureSpeed;
                    decisiveCaptureTime = captureTime;
                }

                if (decisiveCaptureTime >= maxCaptureTime)
                {
                    if (!isHill)
                    {
                        captured = true;
                        locked = true;
                        captureTime = maxCaptureTime;
                        Global.level.gameMode.remainingTime += awardTime;
                        if (Global.level.gameMode.remainingTime < 120)
                        {
                            Global.level.gameMode.remainingTime = 120;
                        }
                    }
                    else
                    {
                        captureTime = 0;
                        blueCaptureTime = 0;
                        redCaptureTime = 0;
                        if (attackerAlliance == GameMode.redAlliance)
                        {
                            Global.level.gameMode.remainingTime = redRemainingTime;
                        }
                        else
                        {
                            Global.level.gameMode.remainingTime = blueRemainingTime;
                        }
                    }

                    int index = Global.level.controlPoints.IndexOf(this);
                    if (Global.level.controlPoints.Count > index + 1)
                    {
                        Global.level.controlPoints[index + 1].locked = false;
                    }

                    alliance = attackerAlliance;
                    Global.level.gameMode.addKillFeedEntry(new KillFeedEntry(GameMode.getTeamName(attackerAlliance) + " captured point", GameMode.blueAlliance), true);
                }
            }
            else if (!contested())
            {
                float overTimeFactor = Global.level.gameMode.remainingTime <= 0 ? 4 : 1;
                redCaptureTime = Helpers.clampMin0(redCaptureTime - Global.spf * overTimeFactor);
                blueCaptureTime = Helpers.clampMin0(blueCaptureTime - Global.spf * overTimeFactor);
                captureTime = Helpers.clampMin0(captureTime - Global.spf * overTimeFactor);
            }

            if (isHill)
            {
                if (alliance == GameMode.redAlliance)
                {
                    Helpers.decrementTime(ref redRemainingTime);
                }
                else if (alliance == GameMode.blueAlliance)
                {
                    Helpers.decrementTime(ref blueRemainingTime);
                }
            }

            sync();
        }

        public void sync()
        {
            // Sync this CP to all clients every half second
            if (Global.isHost && Global.isOnFrame(30))
            {
                Global.serverClient?.rpc(RPC.syncControlPoints,
                    (byte)(num - 1),
                    (byte)alliance,
                    (byte)MathF.Round(captureTime),
                    captured ? (byte)0 : (byte)attackers.Count,
                    captured ? (byte)0 : (byte)defenders.Count,
                    locked ? (byte)1 : (byte)0,
                    captured ? (byte)1 : (byte)0,
                    (byte)MathF.Round(redCaptureTime),
                    (byte)MathF.Round(blueCaptureTime),
                    (byte)MathF.Round(redRemainingTime),
                    (byte)MathF.Round(blueRemainingTime),
                    hillAttackerCountSync);
            }
        }

        public string getHillText()
        {
            if (Global.isHost)
            {
                if (attacked())
                {
                    hillAttackerCountSync = (byte)getAttackerCount();
                    return string.Format("{0}x", getAttackerCount());
                }
                else if (contested())
                {
                    hillAttackerCountSync = 255;
                    return string.Format("blocked");
                }
                else
                {
                    hillAttackerCountSync = 0;
                    return "";
                }
            }
            else
            {
                if (hillAttackerCountSync == 255)
                {
                    return string.Format("blocked");
                }
                else if (hillAttackerCountSync > 0)
                {
                    return string.Format("{0}x", hillAttackerCountSync);
                }
                else
                {
                    return "";
                }
            }
        }

        public bool contested()
        {
            if (alliance == GameMode.neutralAlliance)
            {
                return attackers.Any(a => a.player.alliance == GameMode.redAlliance) && attackers.Any(a => a.player.alliance == GameMode.blueAlliance);
            }
            else
            {
                if (!Global.isHost) return defenderCount > 0 && attackerCount > 0;
                return defenders.Count > 0 && attackers.Count > 0;
            }
        }

        public bool attacked()
        {
            if (isHill && alliance == GameMode.neutralAlliance && contested())
            {
                return false;
            }
            if (!Global.isHost) return defenderCount == 0 && attackerCount > 0;
            return defenders.Count == 0 && attackers.Count > 0;
        }

        public int getAttackerCount()
        {
            if (!Global.isHost) return attackerCount;
            return attackers.Count;
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (other.otherCollider?.flag == (int)HitboxFlag.Hitbox) return;

            if (!ownedByLocalPlayer) return;

            if (captured || locked) return;

            var chr = other.gameObject as Character;
            if (chr != null && !chrsOnPoint.Contains(chr))
            {
                chrsOnPoint.Add(chr);
            }
        }

        public bool canDefend(Character c)
        {
            if (c.player.isDisguisedAxl) return false;
            return true;
        }

        public bool canAttack(Character c)
        {
            if (c.player.isDisguisedAxl) return false;
            return true;
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            var drawX = pos.x + x;
            var drawY = pos.y + y;
            if (alliance == GameMode.neutralAlliance)
            {
                sprite.draw(9, drawX, drawY, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
            }
            else
            {
                sprite.draw(frameIndex + 4, drawX, drawY, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
            }
        }
    }
}
