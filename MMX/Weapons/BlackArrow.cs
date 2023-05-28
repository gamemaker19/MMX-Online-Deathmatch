using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class BlackArrow : AxlWeapon
    {
        public BlackArrow(int altFire) : base(altFire)
        {
            shootSounds = new List<string>() { "blackArrow", "blackArrow", "blackArrow", "blackArrow" };
            rateOfFire = 0.4f;
            altFireCooldown = 0.8f;
            index = (int)WeaponIds.BlackArrow;
            weaponBarBaseIndex = 33;
            weaponSlotIndex = 53;
            killFeedIndex = 68;

            sprite = "axl_arm_blackarrow";
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            if (chargeLevel == 3)
            {
                if (altFire == 1)
                {
                    return 6;
                }
                return 4;
            }
            return 2;
        }

        public override float whiteAxlFireRateMod()
        {
            return 2f;
        }

        public override void axlGetProjectile(Weapon weapon, Point bulletPos, int xDir, Player player, float angle, IDamagable target, Character headshotTarget, Point cursorPos, int chargeLevel, ushort netId)
        {
            if (!player.ownedByLocalPlayer) return;

            Point bulletDir = Point.createFromAngle(angle);
            Projectile bullet = null;
            if (chargeLevel < 3)
            {
                bullet = new BlackArrowProj(weapon, bulletPos, player, bulletDir, 0, netId, rpc: true);
            }
            else
            {
                if (altFire == 0)
                {
                    new WindCutterProj(weapon, bulletPos, player, bulletDir, netId, rpc: true);
                }
                else
                {
                    new BlackArrowProj(weapon, bulletPos, player, bulletDir, 1, netId, rpc: true);
                    new BlackArrowProj(weapon, bulletPos, player, Point.createFromAngle(angle - 30), 1, player.getNextActorNetId(), rpc: true);
                    new BlackArrowProj(weapon, bulletPos, player, Point.createFromAngle(angle + 30), 1, player.getNextActorNetId(), rpc: true);
                }
            }

            if (player.character != null)
            {
                RPC.playSound.sendRpc(shootSounds[0], player.character.netId);
            }
        }
    }

    public class BlackArrowProj : Projectile
    {
        int type;
        public Actor target;
        public List<Point> lastPoses = new List<Point>();

        public BlackArrowProj(Weapon weapon, Point pos, Player player, Point bulletDir, int type, ushort netProjId, bool rpc = false) :
            base(weapon, pos, 1, 450, 1, player, "blackarrow_proj", 0, 0f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 0.5f;
            vel.x = bulletDir.x * speed;
            vel.y = bulletDir.y * speed;
            this.type = type;
            projId = (int)ProjIds.BlackArrow;
            useGravity = true;
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

        public override void update()
        {
            lastPoses.Add(pos);
            if (lastPoses.Count > 5) lastPoses.RemoveAt(0);

            if (ownedByLocalPlayer)
            {
                if (type == 0)
                {
                    target = Global.level.getClosestTarget(pos, damager.owner.alliance, true);

                    if (!Global.level.gameObjects.Contains(target))
                    {
                        target = null;
                    }

                    if (target != null)
                    {
                        useGravity = false;
                        var dTo = pos.directionTo(target.getCenterPos()).normalize();
                        var destAngle = MathF.Atan2(dTo.y, dTo.x) * 180 / MathF.PI;
                        destAngle = Helpers.to360(destAngle);
                        float distFactor = pos.distanceTo(target.getCenterPos()) / 100;
                        angle = Helpers.moveAngle((float)angle, destAngle, Global.spf * 200 * distFactor);

                        vel.x = Helpers.cosd((float)angle) * speed;
                        vel.y = Helpers.sind((float)angle) * speed;
                    }
                    else
                    {
                        useGravity = true;
                        updateAngle();
                    }
                }
                else if (type == 1)
                {
                    updateAngle();
                }

                if (getHeadshotVictim(owner, out IDamagable victim, out Point? hitPoint))
                {
                    damager.applyDamage(victim, false, weapon, this, projId, overrideDamage: damager.damage * Damager.headshotModifier);
                    damager.damage = 0;
                    playSound("hurt");
                    destroySelf();
                    return;
                }
            }

            base.update();
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            if (!ownedByLocalPlayer) return;
            if (type != 2)
            {
                useGravity = false;
                maxTime = 4;
                if (owner.character?.isWhiteAxl() == true)
                {
                    maxTime = 10;
                }
                time = 0;
                type = 2;
                changeSprite("blackarrow_stuck_proj", true);
                vel = new Point();
            }
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            if (Options.main.lowQualityParticles()) return;

            for (int i = lastPoses.Count - 1; i >= 1; i--)
            {
                Point head = lastPoses[i];
                Point outerTail = lastPoses[i - 1];
                Point innerTail = lastPoses[i - 1];
                if (i == 1)
                {
                    innerTail = innerTail.add(head.directionToNorm(innerTail).times(5));
                }

                DrawWrappers.DrawLine(head.x, head.y, outerTail.x, outerTail.y, new Color(80, 59, 145, 64), 4, 0, true);
                DrawWrappers.DrawLine(head.x, head.y, innerTail.x, innerTail.y, new Color(24, 24, 32, 128), 2, 0, true);
            }
        }
    }

    public class WindCutterProj : Projectile
    {
        Actor target;
        public float angleDist = 0;
        public float turnDir = 1;
        public bool targetHit;
        public WindCutterProj(Weapon weapon, Point pos, Player player, Point bulletDir, ushort netProjId, bool rpc = false) :
            base(weapon, pos, 1, 450, 2, player, "windcutter_proj", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 1f;
            vel.x = bulletDir.x * speed;
            vel.y = bulletDir.y * speed;
            projId = (int)ProjIds.WindCutter;
            updateAngle();
            destroyOnHit = true;

            int startXDir = MathF.Sign(bulletDir.x);
            if (bulletDir.y > 0.2f) turnDir = startXDir;
            else turnDir = -startXDir;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public void updateAngle()
        {
            angle = vel.angle;
        }

        public override void update()
        {
            if (!ownedByLocalPlayer)
            {
                base.update();
                return;
            }

            if (!targetHit)
            {
                target = Global.level.getClosestTarget(pos, damager.owner.alliance, true);
                if (!Global.level.gameObjects.Contains(target))
                {
                    target = null;
                }
            }
            else
            {
                target = null;
            }

            if (target != null)
            {
                var dTo = pos.directionTo(target.getCenterPos()).normalize();
                var destAngle = MathF.Atan2(dTo.y, dTo.x) * 180 / MathF.PI;
                destAngle = Helpers.to360(destAngle);
                float distFactor = pos.distanceTo(target.getCenterPos()) / 100;
                if (MathF.Abs(angle.Value - destAngle) > 5)
                {
                    angle = Helpers.moveAngle((float)angle, destAngle, Global.spf * 400 * distFactor);
                }
                vel.x = Helpers.cosd((float)angle) * speed;
                vel.y = Helpers.sind((float)angle) * speed;
            }
            else
            {
                returnToSelf();
                updateAngle();
            }

            base.update();
        }

        public void returnToSelf()
        {
            if (time > 0.1f)
            {
                if (angleDist < 180)
                {
                    var angInc = turnDir * Global.spf * 500;
                    angle += angInc;
                    angleDist += MathF.Abs(angInc);
                    vel.x = Helpers.cosd((float)angle) * speed;
                    vel.y = Helpers.sind((float)angle) * speed;
                }
                else if (owner.character != null)
                {
                    Point destPos = owner.character.getAxlBulletPos();
                    var dTo = pos.directionTo(destPos).normalize();
                    var destAngle = MathF.Atan2(dTo.y, dTo.x) * 180 / MathF.PI;
                    destAngle = Helpers.to360(destAngle);
                    angle = Helpers.lerpAngle((float)angle, destAngle, Global.spf * 10);
                    vel.x = Helpers.cosd((float)angle) * speed;
                    vel.y = Helpers.sind((float)angle) * speed;
                    if (pos.distanceTo(destPos) < 15)
                    {
                        onReturn();
                    }
                }
                else
                {
                    destroySelf();
                }
            }
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (!ownedByLocalPlayer) return;

            if (target != null && other.gameObject == target && !targetHit)
            {
                targetHit = true;
                maxTime = 1f;
            }
            var character = other.gameObject as Character;
            if (time > 0.22f && character != null && character.player == owner)
            {
                onReturn();
            }
        }

        public void onReturn()
        {
            if (!destroyed)
            {
                destroySelf();
                if (owner.weapon is BlackArrow)
                {
                    owner.weapon.ammo += 2;
                }
            }
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
        }
    }
}
