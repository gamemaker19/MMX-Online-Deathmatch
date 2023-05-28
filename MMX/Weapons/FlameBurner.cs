using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class FlameBurner : AxlWeapon
    {
        public FlameBurner(int altFire) : base(altFire)
        {
            shootSounds = new List<string>() { "flameBurner", "flameBurner", "flameBurner", "circleBlaze" };
            rateOfFire = 0.075f;
            altFireCooldown = 1.5f;
            index = (int)WeaponIds.FlameBurner;
            weaponBarBaseIndex = 38;
            weaponSlotIndex = 58;
            killFeedIndex = 73;

            sprite = "axl_arm_flameburner";

            if (altFire == 1)
            {
                shootSounds[3] = "flameBurner2";
                altFireCooldown = 1;
            }
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            if (chargeLevel == 3)
            {
                if (altFire == 0)
                {
                    return 8;
                }
                return 4;
            }
            return 0.5f;
        }

        public override void axlGetProjectile(Weapon weapon, Point bulletPos, int xDir, Player player, float angle, IDamagable target, Character headshotTarget, Point cursorPos, int chargeLevel, ushort netId)
        {
            if (!player.ownedByLocalPlayer) return;
            Point bulletDir = Point.createFromAngle(angle);
            Projectile bullet = null;
            if (chargeLevel < 3)
            {
                if (player?.character?.isUnderwater() == false)
                {
                    bullet = new FlameBurnerProj(weapon, bulletPos, xDir, player, bulletDir, netId, sendRpc: true);
                    bullet = new FlameBurnerProj(weapon, bulletPos.add(bulletDir.times(5)), xDir, player, Point.createFromAngle(angle + Helpers.randomRange(-10, 10)), player.getNextActorNetId(), sendRpc: true);
                    bullet = new FlameBurnerProj(weapon, bulletPos.add(bulletDir.times(10)), xDir, player, Point.createFromAngle(angle + Helpers.randomRange(-10, 10)), player.getNextActorNetId(), sendRpc: true);
                }
                RPC.playSound.sendRpc(shootSounds[0], player.character?.netId);
            }
            else
            {
                if (altFire == 0)
                {
                    bullet = new CircleBlazeProj(weapon, bulletPos, xDir, player, bulletDir, netId, sendRpc: true);
                    RPC.playSound.sendRpc(shootSounds[3], player.character?.netId);
                }
                else
                {
                    bullet = new FlameBurnerAltProj(weapon, bulletPos, xDir, player, bulletDir, netId, sendRpc: true);
                    RPC.playSound.sendRpc(shootSounds[3], player.character?.netId);
                }
            }
        }
    }

    public class FlameBurnerProj : Projectile
    {
        bool hitWall;
        public FlameBurnerProj(Weapon weapon, Point pos, int xDir, Player player, Point bulletDir, ushort netProjId, bool sendRpc = false) : 
            base(weapon, pos, xDir, 150, 1, player, "flameburner_proj", 0, 0.2f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.FlameBurner;
            maxTime = 0.5f;
            if (player.character?.isWhiteAxl() == true)
            {
                projId = (int)ProjIds.FlameBurnerHyper;
            }
            vel.x = bulletDir.x * speed;
            vel.y = bulletDir.y * speed;

            collider.wallOnly = true;
            angle = Helpers.randomRange(0, 360);
            if (isUnderwater())
            {
                destroySelf();
                return;
            }
            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            float progress = (time / maxTime);
            if (!Options.main.lowQualityParticles())
            {
                alpha = 1 - progress;
                xScale = 1 + progress * 2;
                yScale = 1 + progress * 2;
            }
            if (!ownedByLocalPlayer) return;
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            var character = damagable as Character;
            character?.unfreezeIfFrozen();
            if (maxTime < 0.475f) maxTime = 0.475f;
            stopMoving();
        }

        public override void onHitWall(CollideData other)
        {
            if (!ownedByLocalPlayer) return;
            if (!hitWall)
            {
                hitWall = true;
                vel.multiply(0.5f);
                if (projId == (int)ProjIds.FlameBurnerHyper)
                {
                    new MK2NapalmFlame(weapon, other?.hitData?.hitPoint ?? pos, xDir, owner, owner.getNextActorNetId(), rpc: true)
                    {
                        useGravity = false
                    };
                }
            }
        }
    }

    public class FlameBurnerAltProj : Projectile
    {
        public float maxSpeed = 400;
        public FlameBurnerAltProj(Weapon weapon, Point pos, int xDir, Player player, Point bulletDir, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, xDir, 100, 0, player, "airblast_proj", 0, 0.15f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.FlameBurner2;
            maxTime = 0.15f;
            if (player.character?.isWhiteAxl() == true)
            {
                maxTime *= 2;
            }
            xScale = 1;
            yScale = 1;
            vel.x = bulletDir.x * speed;
            vel.y = bulletDir.y * speed;
            destroyOnHit = false;
            shouldShieldBlock = false;
            updateAngle();
            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public void updateAngle()
        {
            angle = vel.angle;
        }

        public override void onStart()
        {
            base.onStart();
            if (!ownedByLocalPlayer) return;
            Character chr = owner.character;
            if (chr != null)
            {
                Point moveVel = chr.getAxlBulletDir().times(-250);
                chr.vel.y = moveVel.y;
                chr.xSwingVel = moveVel.x * 0.66f;
            }
        }

        public override void update()
        {
            base.update();
            updateAngle();

            float timeFactor = (time / sprite.getAnimLength());
            xScale = 1 + (3 * timeFactor);
            yScale = 1 + (3 * timeFactor);
            alpha = 1 - timeFactor;

            if (!ownedByLocalPlayer) return;
            if (isAnimOver())
            {
                destroySelf();
            }
        }

        // Airblast reflect should favor both attacker and defender
        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (other.gameObject is Projectile proj && proj.owner.alliance != owner.alliance && proj.reflectCount == 0 && proj.reflectable2)
            {
                proj.reflect2(owner, deltaPos.angle, sendRpc: true);
            }
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            var character = damagable as Character;
            if (character == null) return;
            if (character.charState.invincible) return;
            if (character.charState.immuneToWind) return;
            if (!character.ownedByLocalPlayer) return;
            if (character.isCCImmune()) return;

            //character.damageHistory.Add(new DamageEvent(damager.owner, weapon.killFeedIndex, true, Global.frameCount));
            if (character.isClimbingLadder())
            {
                character.setFall();
            }
            else
            {
                float modifier = 1.33f;
                Point pushVel = getPushVel();
                character.vel.y = pushVel.y * modifier;
                character.xPushVel = pushVel.x * modifier;
            }
        }

        public Point getPushVel()
        {
            return deltaPos.normalize().times(300);
        }
    }

    public class CircleBlazeProj : Projectile
    {
        bool hitWall;
        bool exploded;
        public CircleBlazeProj(Weapon weapon, Point pos, int xDir, Player player, Point bulletDir, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, 1, 250, 0, player, "circleblaze_proj", 0, 0.2f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.CircleBlaze;
            //fadeSprite = "circleblaze_fade";
            fadeSound = "circleBlazeExplosion";
            maxTime = 0.5f;
            vel.x = bulletDir.x * speed;
            vel.y = bulletDir.y * speed;
            angle = vel.angle;
            if (isUnderwater())
            {
                destroySelf();
                return;
            }
            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            var character = damagable as Character;
            character?.unfreezeIfFrozen();
            base.onHitDamagable(damagable);
            explode();
        }

        public override void onHitWall(CollideData other)
        {
            if (!ownedByLocalPlayer) return;
            if (!hitWall)
            {
                hitWall = true;
                explode();
                destroySelf();
            }
        }

        public override void onDestroy()
        {
            base.onDestroy();
        }

        public void explode()
        {
            if (!ownedByLocalPlayer) return;
            if (exploded) return;
            exploded = true;
            new CircleBlazeExplosionProj(weapon, pos, xDir, owner, owner.getNextActorNetId(), sendRpc: true);
            /*
            for (int i = 0; i < 8; i++)
            {
                new MK2NapalmFlame(weapon, pos.add(Point.random(-10, 10, -10, 10)), xDir, owner, owner.getNextActorNetId(), rpc: true)
                {
                    vel = Point.random(-25, 25, -25, 25)
                };
            }
            */
        }
    }


    public class CircleBlazeExplosionProj : Projectile
    {
        public CircleBlazeExplosionProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, xDir, 0, 2, player, "circleblaze_fade", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            destroyOnHit = false;
            projId = (int)ProjIds.CircleBlazeExplosion;
            shouldShieldBlock = false;
            if (sendRpc)
            {
                rpcCreate(pos, owner, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (isAnimOver())
            {
                destroySelf();
            }
        }
    }

}
