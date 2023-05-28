using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class BoundBlaster : AxlWeapon
    {
        public BoundBlaster(int altFire) : base(altFire)
        {
            shootSounds = new List<string>() { "boundBlaster", "boundBlaster", "boundBlaster", "movingWheel" };
            rateOfFire = 0.15f;
            index = (int)WeaponIds.BoundBlaster;
            weaponBarBaseIndex = 35;
            weaponSlotIndex = 55;
            killFeedIndex = 70;

            sprite = "axl_arm_boundblaster";
            flashSprite = "axl_pistol_flash";
            chargedFlashSprite = "axl_pistol_flash_charged";
            altFireCooldown = 2;

            if (altFire == 1)
            {
                shootSounds[3] = "boundBlaster";
            }
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            if (chargeLevel == 0) return 1f;
            if (altFire == 0)
            {
                return 8;
            }
            return 4;
        }

        public override void axlGetProjectile(Weapon weapon, Point bulletPos, int xDir, Player player, float angle, IDamagable target, Character headshotTarget, Point cursorPos, int chargeLevel, ushort netId)
        {
            if (!player.ownedByLocalPlayer) return;
            Point bulletDir = Point.createFromAngle(angle);
            Projectile bullet = null;
            var hit = Global.level.checkCollisionPoint(bulletPos, new List<GameObject>() {});
            
            if (hit?.gameObject is Wall wall && player.character != null)
            {
                float distToCenter = bulletPos.distanceTo(player.character.getCenterPos());
                Point rayOrigin = bulletPos.add(bulletDir.times(-distToCenter));
                Point rayDir = bulletDir.times(distToCenter + 3);

                Point? intersectPoint = wall.collider.shape.getIntersectPoint(rayOrigin, rayDir);
                if (intersectPoint != null)
                {
                    bulletPos = intersectPoint.Value.add(bulletDir.times(-3));
                }
            }

            if (chargeLevel < 3)
            {
                bullet = new BoundBlasterProj(weapon, bulletPos, xDir, player, bulletDir, netId, rpc: true);
                RPC.playSound.sendRpc(shootSounds[0], player.character?.netId);
            }
            else
            {
                if (altFire == 0)
                {
                    bullet = new MovingWheelProj(weapon, bulletPos, bulletDir.x > 0 ? 1 : -1, player, netId, rpc: true);
                    RPC.playSound.sendRpc(shootSounds[3], player.character?.netId);
                }
                else
                {
                    bullet = new BoundBlasterAltProj(weapon, bulletPos, xDir, player, bulletDir, netId, rpc: true);
                    RPC.playSound.sendRpc(shootSounds[3], player.character?.netId);
                }
            }
        }
    }

    public class BoundBlasterProj : Projectile
    {
        float len = 0;
        float lenDelay = 0;
        float lastAngle;
        const float circleRadius = 14;
        const float maxLen = 60;
        float partTime;

        public BoundBlasterProj(Weapon weapon, Point pos, int xDir, Player player, Point bulletDir, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 250, 1, player, "boundblaster_proj", 0, 0.1f, netProjId, player.ownedByLocalPlayer)
        {
            reflectable = true;
            vel.x = bulletDir.x * speed;
            vel.y = bulletDir.y * speed;
            maxTime = 1.5f;
            if (player.character?.isWhiteAxl() == true)
            {
                maxTime = 3f;
            }
            projId = (int)ProjIds.BoundBlaster;
            updateAngle();

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public void updateAngle()
        {
            angle = vel.angle;
        }

        public void reflectSide()
        {
            vel.x *= -1;
            len = 0;
            lenDelay = 0;
            updateAngle();
        }

        public void onDeflect()
        {
            len = 0;
            lenDelay = 0;
            updateAngle();
        }

        public override void update()
        {
            if (!ownedByLocalPlayer)
            {
                if (destroyPosSet)
                {
                    base.update();
                    return;
                }

                vel.x = Helpers.cosd(angle.Value);
                vel.y = Helpers.sind(angle.Value);
                if (angle.Value != lastAngle)
                {
                    len = 0;
                    lenDelay = 0;
                }
                lastAngle = angle.Value;
            }

            if (lenDelay > 0.01f)
            {
                len += Global.spf * 300;
                if (len > maxLen) len = maxLen;
            }
            lenDelay += Global.spf;

            partTime += Global.spf;
            if (partTime > 0.03f)
            {
                partTime = 0;
                if (!Options.main.lowQualityParticles())
                {
                    new BoundBlasterParticle(pos, Global.level.gameMode.isTeamMode && damager.owner.alliance == GameMode.redAlliance);
                }
            }

            if (ownedByLocalPlayer)
            {
                bool reflected = false;
                var wall = Global.level.checkCollisionPoint(pos.addxy(vel.x * Global.spf, 0), new List<GameObject>() { this });
                if (wall?.gameObject is Wall)
                {
                    vel.x *= -1;
                    reflected = true;
                }

                wall = Global.level.checkCollisionPoint(pos.addxy(0, vel.y * Global.spf), new List<GameObject>() { this });
                if (wall?.gameObject is Wall)
                {
                    vel.y *= -1;
                    reflected = true;
                }

                if (reflected)
                {
                    len = 0;
                    lenDelay = 0;
                    updateAngle();
                    if (owner?.character?.isWhiteAxl() == true)
                    {
                        increasePower();
                    }
                }
            }

            base.update();
        }

        public void increasePower()
        {
            speed += 50;
            updateDamager(damager.damage + 0.5f);
        }

        public override void render(float x, float y)
        {
            if (Options.main.lowQualityParticles())
            {
                int oldXDir = xDir;
                xDir = 1;
                base.render(x, y);
                xDir = oldXDir;
                return;
            }

            var normVel = vel.normalize();
            
            var col1 = new Color(74, 78, 221);
            var col2 = new Color(61, 113, 255);
            var col3 = new Color(215, 244, 255);
            if (Global.level.gameMode.isTeamMode && damager.owner.alliance == GameMode.redAlliance)
            {
                col1 = new Color(221, 78, 74);
                col2 = new Color(255, 113, 61);
                col3 = new Color(255, 244, 215);
            }

            float xOff1 = -(normVel.x * len);
            float yOff1 = -(normVel.y * len);

            Point tail = new Point(pos.x + xOff1, pos.y + yOff1);
            Point head = new Point(pos.x, pos.y);

            Point tailToHead = tail.directionToNorm(head);

            Point tail2 = tail.add(tailToHead.times(2));
            Point head2 = head.add(tailToHead.times(-2));

            Point tail3 = tail.add(tailToHead.times(4));
            Point head3 = head.add(tailToHead.times(-4));

            DrawWrappers.DrawLine(tail3.x, tail3.y, head3.x, head3.y, col1, 6, 0, true);
            DrawWrappers.DrawLine(tail2.x, tail2.y, head2.x, head2.y, col2, 4, 0, true);
            DrawWrappers.DrawLine(tail.x, tail.y, head.x, head.y, col3, 2, 0, true);
        }
    }

    public class BoundBlasterParticle : Anim
    {
        bool isRed;
        const float radius = 10;
        public BoundBlasterParticle(Point pos, bool isRed) : base(pos, "empty", 1, null, false)
        {
            ttl = 0.25f;
            this.isRed = isRed;
        }

        public override void update()
        {
            base.update();
        }

        public override void render(float x, float y)
        {
            float radiusProgress = 1 - (time / (ttl.Value * 4));
            float alphaProgress = 1 - (time / ttl.Value);
            Color col = new Color(167, 195, 255, (byte)(alphaProgress * 255));
            Color col2 = new Color(74, 78, 221, (byte)(alphaProgress * 255));
            if (isRed)
            {
                col = new Color(255, 195, 167, (byte)(alphaProgress * 255));
                col2 = new Color(221, 78, 74, (byte)(alphaProgress * 255));
            }
            DrawWrappers.DrawCircle(pos.x + x, pos.y + y, radius * radiusProgress, false, col, 1, zIndex, outlineColor: col);
            DrawWrappers.DrawCircle(pos.x + x, pos.y + y, (radius + 1) * radiusProgress, false, col2, 1, zIndex, outlineColor: col2);
        }
    }

    public class BoundBlasterAltProj : Projectile
    {
        public int state = 0;
        public Actor stuckActor;
        public bool isActorStuck;
        const float circleRadius = 14;
        float circleTime;
        public BoundBlasterAltProj(Weapon weapon, Point pos, int xDir, Player player, Point bulletDir, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 350, 2, player, "boundblaster_alt_proj", 0, 0.15f, netProjId, player.ownedByLocalPlayer)
        {
            reflectable = true;
            vel.x = bulletDir.x * speed;
            vel.y = bulletDir.y * speed;
            maxTime = 2f;
            if (!ownedByLocalPlayer || player.character?.isWhiteAxl() == true)
            {
                maxTime = float.MaxValue;
            }
            projId = (int)ProjIds.BoundBlaster2;
            if (ownedByLocalPlayer)
            {
                Global.level.boundBlasterAltProjs.Add(this);
            }

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();

            circleTime += Global.spf * 10;
            if (circleTime > 1)
            {
                circleTime = 0;
            }

            if (!ownedByLocalPlayer) return;

            if (owner.character == null || owner.character.destroyed)
            {
                if (maxTime > 20f)
                {
                    maxTime = 20f;
                }
            }

            if (stuckActor != null)
            {
                if (stuckActor.destroyed)
                {
                    stuckActor = null;
                    destroySelf();
                    return;
                }
            }
        }

        public override void postUpdate()
        {
            base.postUpdate();
            if (stuckActor != null)
            {
                incPos(stuckActor.deltaPos);
            }
        }

        public override void onDestroy()
        {
            base.onDestroy();
            Global.level.boundBlasterAltProjs.Remove(this);
        }

        public override void onHitWall(CollideData other)
        {
            stick(null);
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            stick(damagable as Actor);
        }

        public void stick(Actor stuckActor)
        {
            if (!ownedByLocalPlayer) return;
            if (state == 0)
            {
                damager.damage = 0;
                state = 1;
                vel = new Point();
                this.stuckActor = stuckActor;
                isActorStuck = (stuckActor != null);
                if (isActorStuck)
                {
                    RPC.boundBlasterStick.sendRpc(netId, stuckActor?.netId, pos);
                }
                maxTime = Math.Max(20f, maxTime);
            }
        }

        public override void render(float x, float y)
        {
            if (!isActorStuck)
            {
                base.render(x, y);
            }

            var col1 = new Color(74, 78, 221);
            if (Global.level.gameMode.isTeamMode && damager.owner.alliance == GameMode.redAlliance)
            {
                col1 = new Color(221, 78, 74);
            }

            DrawWrappers.DrawCircle(pos.x, pos.y, circleTime * circleRadius, false, col1, 1, zIndex, outlineColor: col1);
        }
    }

    public class MovingWheelProj : Projectile
    {
        int started;
        float soundTime;
        float startMaxTime = 2.5f;
        int hitCount;
        float hitCooldown;
        public MovingWheelProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 0, 3, player, "movingwheel_proj", Global.defFlinch, 1, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.MovingWheel;
            if (player.character?.isWhiteAxl() == true)
            {
                startMaxTime = 5;
            }
            maxTime = startMaxTime;
            useGravity = true;
            collider.isTrigger = false;
            collider.wallOnly = true;
            damager.damage = 2;
            damager.flinch = 0;
            destroyOnHit = true;
            //xScale = 0.75f;
            //yScale = 0.75f;
            angle = 0;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            base.onHitDamagable(damagable);
            if (hitCooldown > 0 || started == 0) return;
            hitCooldown = 0.75f;
            speed *= 0.66f;
            damager.damage--;
            damager.flinch /= 2;
            hitCount++;
            if (damager.damage <= 1)
            {
                damager.damage = 1;
                damager.flinch = 0;
            }
            updateDamager();
            if (hitCount >= 3)
            {
                //destroySelf();
            }
        }

        public override void update()
        {
            base.update();
            Helpers.decrementTime(ref hitCooldown);
            if (started == 0)
            {
                if (frameIndex > 0) frameIndex = 0;
                if (grounded)
                {
                    started = 1;
                    damager.damage = 3;
                    if (isDefenderFavored()) damager.damage = 4;
                    damager.flinch = Global.defFlinch;
                    destroyOnHit = false;
                    maxTime = startMaxTime;
                    speed = 250;
                    updateDamager();
                }
            }
            if (started == 1)
            {
                vel.x = xDir * speed;
                angle += xDir * speed * 3 * Global.spf;
                if (Global.level.checkCollisionActor(this, 0, -1) == null)
                {
                    var collideData = Global.level.checkCollisionActor(this, xDir, 0, vel);
                    if (collideData != null && collideData.hitData != null && !((Point)collideData.hitData.normal).isAngled())
                    {
                        xDir *= -1;
                        maxTime = startMaxTime;
                        startMaxTime -= 0.2f;
                    }
                }
                soundTime += Global.spf;
                if (soundTime > 0.15f)
                {
                    soundTime = 0;
                    //playSound("spinWheelLoop");
                }
            }
        }
    }
}
