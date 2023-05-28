using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MMXOnline
{
    public class Wall : Geometry
    {
        public bool isMoving;
        public Point deltaMove;
        public bool slippery;
        public bool isCracked;
        public bool topWall;
        public float moveX; // i.e. conveyer belts

        public Wall() : base("", new List<Point>())
        {
        }

        public Wall(string name, List<Point> points) : base(name, points)
        {
            collider.isClimbable = true;
            collider.wallOnly = false;
        }
    }

    public class Gate : Wall
    {
        public Gate(string name, List<Point> points) : base(name, points)
        {
            
        }

        public override void render(float x, float y)
        {
            base.render(x, y);

            Rect wallRect = collider.shape.getRect();
            float x1 = wallRect.x1;
            float x2 = wallRect.x2;
            float y1 = wallRect.y1;
            float y2 = wallRect.y2;

            float xMod = (x2 - x1) % 5;
            if (xMod != 0) x2 += (5 - xMod);

            float yMod = (y2 - y1) % 5;
            if (yMod != 0) y2 += (5 - yMod);

            for (float i = x1; i <= x2; i += 5)
            {
                DrawWrappers.DrawLine(i, y1, i, y2, new Color(128, 128, 128), 1, ZIndex.Background);
            }

            for (float j = y1; j <= y2; j += 5)
            {
                if (j < 0 || j > Global.level.height) continue;
                DrawWrappers.DrawLine(x1, j, x2, j, new Color(128, 128, 128), 1, ZIndex.Background);
            }
        }
    }

    public class MovingPlatform : Wall
    {
        public Sprite sprite;
        public Sprite idleSprite;
        public Point origin;
        public float moveSpeed;
        public float timeOffset;
        public List<Point> movePoints = new List<Point>();
        public List<float> moveWaitTimes = new List<float>();
        public float period;
        public string nodeName;
        public NavMeshNode node;
        public Point nodeOriginOffset;
        public string killZoneName;
        public KillZone killZone;
        public string crackedWallName;
        public CrackedWall linkedCrackedWall;
        public long zIndex;
        public bool flipXOnMoveLeft;
        public bool flipYOnMoveUp;
        int xDirToUse = 1;
        int yDirToUse = 1;

        public MovingPlatform(string spriteName, string idleSpriteName, Point origin, string moveData, float moveSpeed, float timeOffset, string nodeName, string killZoneName, string crackedWallName, long zIndex,
            bool flipXOnMoveLeft, bool flipYOnMoveUp)
        {
            name = "MovingPlatform";
            isMoving = true;
            
            sprite = Global.sprites[spriteName].clone();
            idleSprite = !string.IsNullOrEmpty(idleSpriteName) ? Global.sprites[idleSpriteName].clone() : null;

            var rect = sprite.hitboxes[0].shape.getRect();
            origin = origin.addxy(-rect.w() / 2f, -rect.h() / 2f);
            this.origin = origin;

            updateCollider(origin);

            this.moveSpeed = moveSpeed;
            this.timeOffset = timeOffset;
            this.nodeName = nodeName;
            this.killZoneName = killZoneName;
            this.crackedWallName = crackedWallName;
            this.zIndex = zIndex;
            this.flipXOnMoveLeft = flipXOnMoveLeft;
            this.flipYOnMoveUp = flipYOnMoveUp;

            var lines = moveData.Split('\n');
            foreach (var line in lines)
            {
                var pieces = line.Split(',');
                if (pieces.Length == 3)
                {
                    float x = float.Parse(pieces[0]);
                    float y = float.Parse(pieces[1]);
                    float t = float.Parse(pieces[2]);
                    movePoints.Add(new Point(origin.x + x, origin.y + y));
                    moveWaitTimes.Add(t);
                }
            }
            period = getPeriod();
        }

        bool once;
        Point? lastSyncPos;
        public void update(float globalSyncTime)
        {
            if (!once)
            {
                once = true;
                node = Global.level.navMeshNodes.FirstOrDefault(n => n.name == nodeName);
                if (node != null)
                {
                    node.movingPlatform = this;
                    nodeOriginOffset = node.pos.subtract(origin);
                }
                killZone = Global.level.gameObjects.FirstOrDefault(n => n.name == killZoneName) as KillZone;
            }

            if (idleSprite != null && deltaMove.isZero())
            {
                idleSprite.update();
            }
            else
            {
                sprite.update();
            }

            Point syncPos;
            // First time we move, the last sync position should be before time offset is applied, so that time offset moving for kill zone works
            if (lastSyncPos == null)
            {
                syncPos = getSyncPos(globalSyncTime % period);
                lastSyncPos = syncPos;
            }

            globalSyncTime += timeOffset;
            syncPos = getSyncPos(globalSyncTime % period);
            Point deltaPos = syncPos.subtract(lastSyncPos.Value);

            changePos(syncPos);
            
            killZone?.move(deltaPos);
            linkedCrackedWall?.move(deltaPos);

            lastSyncPos = syncPos;

            if (node != null)
            {
                node.pos = syncPos.add(nodeOriginOffset);
            }
        }

        public void updateCollider(Point pos)
        {
            var points = new List<Point>();
            foreach (var point in sprite.hitboxes[0].shape.points)
            {
                points.Add(pos.add(point));
            }
            collider = new Collider(points, sprite.hitboxes[0].isTrigger, null, true, false, HitboxFlag.None, new Point(0, 0));
        }

        public float getPeriod()
        {
            float period = 0;
            for (int i = 0; i < movePoints.Count; i++)
            {
                int nextI = i == movePoints.Count - 1 ? 0 : i + 1;
                var cur = movePoints[i];
                var dest = movePoints[nextI];
                float dist = cur.distanceTo(dest);
                float timeToReachDest = dist / moveSpeed;
                period += timeToReachDest + moveWaitTimes[i];
            }
            return period;
        }

        public Point getSyncPos(float syncTime)
        {
            for (int i = 0; i < movePoints.Count; i++)
            {
                syncTime -= moveWaitTimes[i];
                if (syncTime <= 0)
                {
                    return movePoints[i];
                }

                int nextI = i == movePoints.Count - 1 ? 0 : i + 1;
                var cur = movePoints[i];
                var dest = movePoints[nextI];
                float dist = cur.distanceTo(dest);
                float timeToReachDest = dist / moveSpeed;
                float remainingSyncTime = syncTime;
                syncTime -= timeToReachDest;
                if (syncTime <= 0)
                {
                    float progress = Helpers.clamp01(remainingSyncTime / timeToReachDest);
                    return cur.add(cur.directionTo(dest).times(progress));
                }
            }

            return origin;
        }

        public void changePos(Point newOrigin)
        {
            var oldPos = collider.shape.points[0];
            Global.level.removeGameObject(this);
            updateCollider(newOrigin);
            Global.level.addGameObject(this);
            var newPos = collider.shape.points[0];
            deltaMove = new Point(newPos.x - oldPos.x, newPos.y - oldPos.y);
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            Point pos = collider.shape.getRect().center();
            var currentFrame = sprite.getCurrentFrame();

            Sprite spriteToUse = sprite;
            if (idleSprite != null && deltaMove.isZero())
            {
                spriteToUse = idleSprite;
            }
            if (flipXOnMoveLeft && deltaMove.x < 0) xDirToUse = -1;
            if (flipXOnMoveLeft && deltaMove.x > 0) xDirToUse = 1;
            if (flipYOnMoveUp && deltaMove.y < 0) yDirToUse = -1;
            if (flipYOnMoveUp && deltaMove.y > 0) yDirToUse = 1;

            spriteToUse.draw(spriteToUse.frameIndex, pos.x + currentFrame.offset.x, pos.y + currentFrame.offset.y, xDirToUse, yDirToUse, null, 1, 1, 1, zIndex);
        }
    }

    public class CrackedWall : Actor, IDamagable
    {
        public Wall wall;
        public float health;
        public float maxHealth;
        public string destroyInstanceName;
        public byte id;
        public bool destroySilently;
        public int flag;
        public string gibSprite;

        public CrackedWall(Point pos, string crackedWallSprite, string gibSprite, int xDir, int yDir, int flag, int health, string destroyInstanceName, bool ownedByLocalPlayer) : 
            base(crackedWallSprite, pos, null, ownedByLocalPlayer, false)
        {
            this.health = health;
            maxHealth = health;
            isStatic = true;
            this.flag = flag;
            this.gibSprite = gibSprite;
            this.xDir = xDir;
            this.yDir = yDir;

            id = Global.level.crackedWallAutoIncId;
            if (Global.level.crackedWallAutoIncId == 255)
            {
                throw new Exception("Cannot have more than 255 cracked walls!");
            }
            Global.level.crackedWallAutoIncId++;

            useGravity = false;

            collider.flag = (int)HitboxFlag.Hurtbox;
            var rect = collider.shape.getRect().getPoints();
            wall = new Wall("crackedwall", new List<Point>()
            {
                rect[0].addxy(1, 1),
                rect[1].addxy(-1, 1),
                rect[2].addxy(-1, -1),
                rect[3].addxy(1, -1),
            });
            
            wall.isCracked = true;
            Global.level.addGameObject(wall);

            this.destroyInstanceName = destroyInstanceName;
        }

        public override void update()
        {
            base.update();
            updateProjectileCooldown();
        }

        public void move(Point deltaPos)
        {
            incPos(deltaPos);
            Global.level.removeFromGridFast(wall);
            var rect = collider.shape.getRect().getPoints();
            wall.collider._shape.points = new List<Point>()
            {
                rect[0].addxy(1, 1),
                rect[1].addxy(-1, 1),
                rect[2].addxy(-1, -1),
                rect[3].addxy(1, -1),
            };
            Global.level.addGameObjectToGrid(wall);
        }

        // Only if 0 is returned, it can't damage it. Even if null, it still can
        public static float? canDamageCrackedWall(int projId, CrackedWall cw)
        {
            if (cw?.flag == 3) return null;

            if (projId == (int)ProjIds.GigaCrush) return 12;
            if (projId == (int)ProjIds.Rakuhouha) return 12;
            if (projId == (int)ProjIds.Rekkoha) return 12;
            if (projId == (int)ProjIds.MechPunch || projId == (int)ProjIds.MechKangarooPunch || projId == (int)ProjIds.MechGoliathPunch || projId == (int)ProjIds.MechDevilBearPunch) return null;
            if (projId == (int)ProjIds.MechStomp) return null;
            if (projId == (int)ProjIds.MechChain) return null;
            if (projId == (int)ProjIds.MechMissile) return null;
            if (projId == (int)ProjIds.Torpedo) return null;
            if (projId == (int)ProjIds.TorpedoCharged) return null;
            if (projId == (int)ProjIds.MechTorpedo) return null;
            if (projId == (int)ProjIds.MagnetMine) return null;
            if (projId == (int)ProjIds.ExplosionSplash) return null;
            if (projId == (int)ProjIds.Explosion) return null;
            if (projId == (int)ProjIds.GLauncher) return null;
            if (projId == (int)ProjIds.GLauncherSplash) return null;
            if (projId == (int)ProjIds.SpinWheel) return 1;
            if (projId == (int)ProjIds.TunnelFang) return null;
            if (projId == (int)ProjIds.TunnelFang2) return null;
            if (projId == (int)ProjIds.TunnelFangCharged) return null;
            if (projId == (int)ProjIds.TriadThunderQuake) return null;
            if (projId == (int)ProjIds.Headbutt && cw?.flag == 1) return 12;
            if (projId == (int)ProjIds.VileMissile) return null;
            if (projId == (int)ProjIds.PopcornDemon) return null;
            if (projId == (int)ProjIds.PopcornDemonSplit) return null;
            if (projId == (int)ProjIds.LaunchOMissle) return null;
            if (projId == (int)ProjIds.LaunchOTorpedo) return null;
            if (projId == (int)ProjIds.NecroBurst) return 12;
            if (projId == (int)ProjIds.SparkMPunch) return 12;
            if (projId == (int)ProjIds.TBreaker) return 12;
            if (projId == (int)ProjIds.TBreakerProj) return 12;
            if (projId == (int)ProjIds.KKnuckle || projId == (int)ProjIds.KKnuckle2) return null;
            if (projId == (int)ProjIds.KKnuckleMegaPunch) return null;
            if (projId == (int)ProjIds.WheelGSpinWheel) return 3;
            if (projId == (int)ProjIds.WheelGSpin) return 3;
            if (projId == (int)ProjIds.TunnelRTornadoFang) return null;
            if (projId == (int)ProjIds.TunnelRTornadoFang2) return null;
            if (projId == (int)ProjIds.TunnelRTornadoFangDiag) return null;
            if (projId == (int)ProjIds.TunnelRDash) return null;

            return 0;
        }

        public void applyDamage(Player owner, int? weaponIndex, float damage, int? projId)
        {
            if (!Global.isHost)
            {
                RPC.actorToggle.sendRpcDamageCw(id, (byte)(int)damage);
                return;
            }

            health -= damage;
            if (health <= 0)
            {
                health = 0;
                destroySelf();
                RPC.actorToggle.sendRpcDestroyCw(id);
            }
        }

        public override void onDestroy()
        {
            base.onDestroy();
            Global.level.removeGameObject(wall);
            if (!string.IsNullOrEmpty(destroyInstanceName))
            {
                if (destroyInstanceName.StartsWith("No Scroll"))
                {
                    Global.level.noScrolls.RemoveAll(ns => ns.name == destroyInstanceName);
                }
                else
                {
                    var toRemove = Global.level.gameObjects.FirstOrDefault(go => go.name == destroyInstanceName);
                    if (toRemove != null)
                    {
                        Global.level.removeGameObject(toRemove);
                    }
                }
            }

            if (destroySilently) return;

            // Animation section
            foreach (var poi in sprite.frames[0].POIs)
            {
                new Anim(pos.addxy(poi.x, poi.y), "explosion", 1, null, true);
            }
            playSound("explosion");

            if (!string.IsNullOrEmpty(gibSprite))
            {
                Point centerPos = pos.add(Point.average(sprite.frames[0].POIs));
                Anim.createGibEffect(gibSprite, centerPos, Global.level.mainPlayer, gibPattern: GibPattern.SemiCircle, randVelStart: 200, randVelEnd: 300);
            }
        }

        public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) { return true; }
        public bool isInvincible(Player attacker, int? projId) { return false; }
        public bool canBeHealed(int healerAlliance) { return false; }
        public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) { }
    }

    public class Ladder : Geometry
    {
        public Ladder(string name, List<Point> points) : base(name, points)
        {
            collider.isTrigger = true;
        }
    }

    public class KillZone : Geometry
    {
        public bool killInvuln;
        public float damage;
        public bool flinch;
        public float hitCooldown;
        public KillZone(string name, List<Point> points, bool killInvuln, float? damage, bool flinch, float hitCooldown) : base(name, points)
        {
            this.killInvuln = killInvuln;
            this.damage = damage ?? Damager.envKillDamage;
            this.flinch = flinch;
            this.hitCooldown = hitCooldown;
            collider.isTrigger = true;
        }

        public void applyDamage(IDamagable damagable)
        {
            if (!damagable.actor().ownedByLocalPlayer) return;
            if (damage == Damager.envKillDamage)
            {
                damagable.applyDamage(null, null, damage, null);
                return;
            }
            
            if (damagable.projectileCooldown.ContainsKey("killzone") && damagable.projectileCooldown["killzone"] > 0) return;
            if (damagable.canBeDamaged(-1, null, null) && !damagable.isInvincible(null, null))
            {
                damagable.projectileCooldown["killzone"] = hitCooldown;
                damagable.applyDamage(null, null, damage, null);
                if (damagable is Character chr)
                {
                    chr.playSound(flinch ? "hurt" : "hit", sendRpc: true);
                    chr.addRenderEffect(RenderEffectType.Hit, 0.05f, 0.1f);
                    if (flinch)
                    {
                        chr.changeState(new Hurt(-chr.xDir, flinch ? Global.defFlinch : 0, 0));
                    }
                }
                else
                {
                    damagable.actor().playSound("hit", sendRpc: true);
                    damagable.actor().addRenderEffect(RenderEffectType.Hit, 0.05f, 0.1f);
                }
            }
        }

        public void move(Point deltaPos)
        {
            Global.level.removeFromGridFast(this);
            for (int i = 0; i < collider._shape.points.Count; i++)
            {
                Point point = collider._shape.points[i];
                collider._shape.points[i] = point.add(deltaPos);
            }
            Global.level.addGameObjectToGrid(this);
        }
    }

    public class MoveZone : Geometry
    {
        public Point moveVel;
        public MoveZone(string name, List<Point> points, float moveVelX, float moveVelY) : base(name, points)
        {
            moveVel = new Point(moveVelX, moveVelY);
            collider.isTrigger = true;
        }
    }

    public class JumpZone : Geometry
    {
        public string targetNode;
        public int forceDir;
        public float jumpTime;
        public JumpZone(string name, List<Point> points, string targetNode, int forceDir, float jumpTime) : base(name, points)
        {
            collider.isTrigger = true;
            this.targetNode = targetNode;
            this.forceDir = forceDir;
            this.jumpTime = jumpTime;
        }
    }

    public class TurnZone : Geometry
    {
        public int xDir;
        public bool jumpAfterTurn;
        public TurnZone(string name, List<Point> points, int xDir, bool jumpAfterTurn) : base(name, points)
        {
            collider.isTrigger = true;
            this.xDir = xDir;
            this.jumpAfterTurn = jumpAfterTurn;
        }
    }

    public class BrakeZone : Geometry
    {
        public BrakeZone(string name, List<Point> points) : base(name, points)
        {
            collider.isTrigger = true;
        }
    }
    public class BackwallZone : Geometry
    {
        public bool isExclusion;
        public BackwallZone(string name, List<Point> points, bool isExclusion) : base(name, points)
        {
            collider.isTrigger = true;
            this.isExclusion = isExclusion;
        }
    }
}
