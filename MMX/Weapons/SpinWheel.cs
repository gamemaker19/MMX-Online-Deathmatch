using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class SpinWheel : Weapon
    {
        public SpinWheel() : base()
        {
            shootSounds = new List<string>() { "spinWheel", "spinWheel", "spinWheel", "spinWheelCharged" };
            rateOfFire = 1f;
            index = (int)WeaponIds.SpinWheel;
            weaponBarBaseIndex = 12;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 12;
            killFeedIndex = 20 + (index - 9);
            weaknessIndex = 14;
        }
        
        public override float getAmmoUsage(int chargeLevel)
        {
            if (chargeLevel != 3) return 2;
            return 8;
        }

        public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId)
        {
            if (chargeLevel != 3)
            {
                new SpinWheelProj(this, pos, xDir, player, netProjId);
            }
            else
            {
                new SpinWheelProjChargedStart(this, pos, xDir, player, netProjId);
            }
        }
    }

    public class SpinWheelProj : Projectile
    {
        int started;
        float startedTime;
        public Anim sparks;
        float soundTime;
        float startMaxTime = 2.5f;
        float lastHitTime;
        const float hitCooldown = 0.2f;
        public SpinWheelProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 0, 1, player, "spinwheel_start", 0, hitCooldown, netProjId, player.ownedByLocalPlayer)
        {
            destroyOnHit = false;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            projId = (int)ProjIds.SpinWheel;
            if (collider != null)
            {
                collider.isTrigger = false;
                collider.wallOnly = true;
            }
            if (started == 0 && sprite.isAnimOver())
            {
                started = 1;
                changeSprite("spinwheel_proj", true);
                useGravity = true;
                collider.isTrigger = false;
                collider.wallOnly = true;
            }
            if (started == 1)
            {
                startedTime += Global.spf;
                if (startedTime > 1)
                {
                    started = 2;
                    maxTime = startMaxTime;
                }
            }
            if (started == 2)
            {
                vel.x = xDir * 250;
                if (lastHitTime > 0) vel.x = xDir * 4;
                Helpers.decrementTime(ref lastHitTime);
                if (Global.level.checkCollisionActor(this, 0, -1) == null)
                {
                    var collideData = Global.level.checkCollisionActor(this, xDir, 0, vel);
                    if (collideData != null && collideData.hitData != null && !((Point)collideData.hitData.normal).isAngled())
                    {
                        xDir *= -1;
                        if (sparks != null) sparks.xDir *= -1;
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
            if (started > 0 && grounded && !destroyed)
            {
                if (sparks == null)
                {
                    sparks = new Anim(pos, "spinwheel_sparks", xDir, null, false);
                }
                sparks.pos = pos.addxy(-xDir * 10, 10);
                sparks.visible = true;
            }
            else
            {
                if (sparks != null) sparks.visible = false;
            }
        }

        public override void onDestroy()
        {
            if (sparks != null)
            {
                sparks.destroySelf();
            }
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            if (damagable is CrackedWall)
            {
                damager.hitCooldown = hitCooldown;
                return;
            }

            lastHitTime = hitCooldown;

            var chr = damagable as Character;
            if (chr != null && chr.ownedByLocalPlayer && !chr.isImmuneToKnockback())
            {
                chr.vel = Point.lerp(chr.vel, Point.zero, Global.spf * 10);
                chr.slowdownTime = 0.25f;
            }

            base.onHitDamagable(damagable);
        }
    }

    public class SpinWheelProjChargedStart : Projectile
    {
        public SpinWheelProjChargedStart(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 0, 4, player, "spinwheel_charged_start", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.SpinWheelChargedStart;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (sprite.isAnimOver())
            {
                new SpinWheelProjCharged(weapon, pos, -1, -1, damager.owner, damager.owner.getNextActorNetId(), rpc: true);
                new SpinWheelProjCharged(weapon, pos, -1, 0, damager.owner, damager.owner.getNextActorNetId(), rpc: true);
                new SpinWheelProjCharged(weapon, pos, -1, 1, damager.owner, damager.owner.getNextActorNetId(), rpc: true);
                new SpinWheelProjCharged(weapon, pos, 0, -1, damager.owner, damager.owner.getNextActorNetId(), rpc: true);
                new SpinWheelProjCharged(weapon, pos, 0, 1, damager.owner, damager.owner.getNextActorNetId(), rpc: true);
                new SpinWheelProjCharged(weapon, pos, 1, -1, damager.owner, damager.owner.getNextActorNetId(), rpc: true);
                new SpinWheelProjCharged(weapon, pos, 1, 0, damager.owner, damager.owner.getNextActorNetId(), rpc: true);
                new SpinWheelProjCharged(weapon, pos, 1, 1, damager.owner, damager.owner.getNextActorNetId(), rpc: true);
                destroySelf();
            }
        }
    }

    public class SpinWheelProjCharged : Projectile
    {
        public SpinWheelProjCharged(Weapon weapon, Point pos, int xDir, int yDir, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 200, 1, player, "spinwheel_charged", Global.defFlinch, 0, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.SpinWheelCharged;
            maxTime = 0.75f;

            this.xDir = xDir == 0 ? 1 : xDir;
            this.yDir = yDir == 0 ? 1 : yDir;

            if (xDir == 0) changeSprite("spinwheel_charged_up", true);
            else if (yDir != 0)
            {
                changeSprite("spinwheel_charged_diag", true);
            }

            if (xDir != 0 && yDir != 0) speed *= 0.71f;

            vel.x = speed * MathF.Sign(xDir);
            vel.y = speed * MathF.Sign(-yDir);

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
        }
    }
}
