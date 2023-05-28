using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class Boomerang : Weapon
    {
        public Boomerang() : base()
        {
            index = (int)WeaponIds.Boomerang;
            killFeedIndex = 7;
            weaponBarBaseIndex = 7;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 7;
            weaknessIndex = 1;
            shootSounds = new List<string>() { "boomerang", "boomerang", "boomerang", "buster3" };
            rateOfFire = 0.5f;
        }

        public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId)
        {
            if (chargeLevel != 3) new BoomerangProj(this, pos, xDir, player, netProjId);
            else
            {
                player.setNextActorNetId(netProjId);

                var twin1 = new BoomerangProjCharged(this, pos.addxy(0, 5), null, xDir, player, 90, 1, player.getNextActorNetId(true), null);
                var twin2 = new BoomerangProjCharged(this, pos.addxy(5, 0), null, xDir, player, 0, 1, player.getNextActorNetId(true), null);
                var twin3 = new BoomerangProjCharged(this, pos.addxy(0, -5), null, xDir, player, -90, 1, player.getNextActorNetId(true), null);
                var twin4 = new BoomerangProjCharged(this, pos.addxy(-5, 0), null, xDir, player, -180, 1, player.getNextActorNetId(true), null);

                var a = new BoomerangProjCharged(this, pos.addxy(0, 5), pos.addxy(0, 35), xDir, player, 90, 0, player.getNextActorNetId(true), twin1);
                var b = new BoomerangProjCharged(this, pos.addxy(5, 0), pos.addxy(35, 0), xDir, player, 0, 0, player.getNextActorNetId(true), twin2);
                var c = new BoomerangProjCharged(this, pos.addxy(0, -5), pos.addxy(0, -35), xDir, player, -90, 0, player.getNextActorNetId(true), twin3);
                var d = new BoomerangProjCharged(this, pos.addxy(-5, 0), pos.addxy(-35, 0), xDir, player, -180, 0, player.getNextActorNetId(true), twin4);

                twin1.twin = a;
                twin2.twin = b;
                twin3.twin = c;
                twin4.twin = d;
            }
        }
    }

    public class BoomerangProj : Projectile
    {
        public float angleDist = 0;
        public float turnDir = 1;
        public Pickup pickup;
        public float maxSpeed = 250;
        public BoomerangProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId) : base(weapon, pos, xDir, 250, 2, player, "boomerang", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.Boomerang;
            customAngleRendering = true;
            angle = 0;
            if (xDir == -1) angle = -180;
            if (!player.character.grounded)
            {
                turnDir = -1;
            }
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (!ownedByLocalPlayer) return;
            if (destroyed) return;

            if (other.gameObject is Pickup && pickup == null)
            {
                pickup = other.gameObject as Pickup;
                if (!pickup.ownedByLocalPlayer)
                {
                    pickup.takeOwnership();
                    RPC.clearOwnership.sendRpc(pickup.netId);
                }
            }

            var character = other.gameObject as Character;
            if (time > 0.22 && character != null && character.player == damager.owner)
            {
                if (pickup != null)
                {
                    pickup.changePos(character.getCenterPos());
                }
                destroySelf();
                if (character.player.weapon is Boomerang)
                {
                    if (character.player.hasChip(3)) character.player.weapon.ammo += 0.5f;
                    else character.player.weapon.ammo++;
                }
            }
        }

        public override void onDestroy()
        {
            base.onDestroy();
            if (pickup != null)
            {
                pickup.useGravity = true;
                pickup.collider.isTrigger = false;
            }
        }

        public override void renderFromAngle(float x, float y)
        {
            sprite.draw(frameIndex, pos.x + x, pos.y + y, 1, 1, getRenderEffectSet(), 1, 1, 1, zIndex, actor: this);
        }

        public override void update()
        {
            base.update();

            if (!destroyed && pickup != null)
            {
                pickup.collider.isTrigger = true;
                pickup.useGravity = false;
                pickup.changePos(pos);
            }

            if (time > 0.22)
            {
                if (angleDist < 180)
                {
                    var angInc = (-xDir * turnDir) * Global.spf * 300;
                    angle += angInc;
                    angleDist += MathF.Abs(angInc);
                    vel.x = Helpers.cosd((float)angle) * maxSpeed;
                    vel.y = Helpers.sind((float)angle) * maxSpeed;
                }
                else if (damager.owner.character != null)
                {
                    var dTo = pos.directionTo(damager.owner.character.getCenterPos()).normalize();
                    var destAngle = MathF.Atan2(dTo.y, dTo.x) * 180 / MathF.PI;
                    destAngle = Helpers.to360(destAngle);
                    angle = Helpers.lerpAngle((float)angle, destAngle, Global.spf * 10);
                    vel.x = Helpers.cosd((float)angle) * maxSpeed;
                    vel.y = Helpers.sind((float)angle) * maxSpeed;
                }
                else
                {
                    destroySelf();
                }
            }
        }

    }

    public class BoomerangProjCharged : Projectile
    {
        public float angleDist = 0;
        public float turnDir = 1;
        public Pickup pickup;
        public float maxSpeed = 400;
        public int type = 0;
        public Point blurPosOffset;
        public BoomerangProjCharged twin;
        
        public Point lerpOffset;
        public float lerpTime;

        public BoomerangProjCharged(Weapon weapon, Point pos, Point? lerpToPos, int xDir, Player player, float angle, int type, ushort netProjId, BoomerangProjCharged twin) : 
            base(weapon, pos, xDir, 0, 2, player, type == 0 ? "boomerang_charge" : "boomerang_charge2", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.BoomerangCharged;
            maxTime = 1.2f;
            customAngleRendering = true;
            this.angle = angle;
            this.type = type;
            shouldShieldBlock = false;
            this.twin = twin;
            destroyOnHit = false;
            if (lerpToPos != null)
            {
                lerpOffset = lerpToPos.Value.subtract(pos);
            }
        }

        public override void renderFromAngle(float x, float y)
        {
            sprite.draw(frameIndex, pos.x + x, pos.y + y, 1, 1, getRenderEffectSet(), 1, 1, 1, zIndex);
        }

        public override void update()
        {
            base.update();
            if (lerpTime < 1)
            {
                incPos(lerpOffset.times(Global.spf * 15));
                lerpTime += Global.spf * 15;
            }
            vel.x = Helpers.cosd((float)angle) * maxSpeed;
            vel.y = Helpers.sind((float)angle) * maxSpeed;
            if (type == 0)
            {
                if (time > 0.1 && time < 0.72)
                {
                    angle += Global.spf * 500;
                }
            }
            else
            {
                if (time > 0.15 && time < 0.77)
                {
                    angle += Global.spf * 500;
                }
            }
        }

        public override void onDestroy()
        {
            if (twin != null) twin.destroySelfNoEffect();
        }
    }
}
