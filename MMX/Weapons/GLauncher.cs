using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class GLauncher : AxlWeapon
    {
        public GLauncher(int altFire) : base(altFire)
        {
            shootSounds = new List<string>() { "grenadeShoot", "grenadeShoot", "grenadeShoot", "rocketShoot" };
            index = (int)WeaponIds.GLauncher;
            weaponBarBaseIndex = 29;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 29;
            killFeedIndex = 29;
            switchCooldown = 0.1f;
            rateOfFire = 0.75f;

            sprite = "axl_arm_glauncher";
            flashSprite = "axl_pistol_flash_charged";
            chargedFlashSprite = "axl_pistol_flash_charged";
            altFireCooldown = 1.5f;
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            if (chargeLevel < 3) return 4;
            return 8;
        }

        public override float whiteAxlFireRateMod()
        {
            return 1.5f;
        }

        public override float whiteAxlAmmoMod()
        {
            return 1f;
        }

        public override void axlGetProjectile(Weapon weapon, Point bulletPos, int xDir, Player player, float angle, IDamagable target, Character headshotTarget, Point cursorPos, int chargeLevel, ushort netId)
        {
            Point bulletDir = Point.createFromAngle(angle);
            Projectile grenade;
            if (chargeLevel < 3)
            {
                grenade = new GrenadeProj(weapon, bulletPos, xDir, player, bulletDir, target, cursorPos, chargeLevel, netId);
                player.grenades.Add(grenade as GrenadeProj);
                if (player.grenades.Count > 8)
                {
                    player.grenades[0].destroySelf();
                    player.grenades.RemoveAt(0);
                }
            }
            else
            {
                grenade = new GrenadeProjCharged(weapon, bulletPos, xDir, player, bulletDir, target, netId);
            }

            if (player.ownedByLocalPlayer)
            {
                RPC.axlShoot.sendRpc(player.id, grenade.projId, netId, bulletPos, xDir, angle);
            }
        }
    }

    public class GrenadeProj : Projectile, IDamagable
    {
        public IDamagable target;
        int type = 0;
        bool planted;
        Player player;  // We use player here due to airblast reflect potential. The explosion should still be the original owner's
        public GrenadeProj(Weapon weapon, Point pos, int xDir, Player player, Point bulletDir, IDamagable target, Point cursorPos, int chargeLevel, ushort netProjId) : 
            base(weapon, pos, xDir, 300, 0, player, "axl_grenade", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            this.target = target;

            if (player?.axlLoadout?.blastLauncherAlt == 1)
            {
                type = 1;
                speed = 250;
            }

            if (type == 1)
            {
                fadeSound = "explosion";
                fadeSprite = "explosion";
            }

            vel.x = speed * bulletDir.x;
            vel.y = speed * bulletDir.y;

            projId = (int)ProjIds.GLauncher;
            useGravity = true;
            collider.wallOnly = true;
            destroyOnHit = false;
            shouldShieldBlock = false;
            reflectable2 = true;
            this.player = player;
            updateAngle();
        }

        int framesNotMoved;
        public override void update()
        {
            base.update();

            updateProjectileCooldown();

            if (!ownedByLocalPlayer) return;

            updateAngle();
            
            if (MathF.Abs(vel.y) < 0.5f && grounded)
            {
                vel.y = 0;
                vel.x *= 0.5f;
            }
            if (MathF.Abs(vel.x) < 1)
            {
                vel.x = 0;
            }

            if (deltaPos.isCloseToZero())
            {
                framesNotMoved++;
            }

            if (type == 1 && (vel.isZero() || framesNotMoved > 1) && !planted)
            {
                changeSprite("axl_grenade_mine", true);
                planted = true;
                stopMoving();
                useGravity = false;
            }

            if (planted)
            {
                if (!Global.level.hasMovingPlatforms) isStatic = true;
                moveWithMovingPlatform();
            }

            if (time > 2 && type == 0)
            {
                destroySelf();
            }
        }

        public void updateAngle()
        {
            if (vel.magnitude > 50)
            {
                angle = MathF.Atan2(vel.y, vel.x) * 180 / MathF.PI;
            }
            xDir = 1;
        }

        public override void onCollision(CollideData other)
        {
            if (!ownedByLocalPlayer) return;
            if (planted) return;

            base.onCollision(other);
            var damagable = other.gameObject as IDamagable;
            if (damagable != null && damagable.canBeDamaged(owner.alliance, owner.id, projId) && !vel.isZero() && type == 0)
            {
                destroySelf();
                return;
            }
            var wall = other.gameObject as Wall;
            if (wall != null)
            {
                Point? normal = other.hitData.normal;
                if (normal != null)
                {
                    if (normal.Value.x != 0) vel.x *= -0.5f;
                    if (normal.Value.y != 0) vel.y *= -0.5f;
                    if (type == 1)
                    {
                        vel.x = 0;
                        vel.y = MathF.Sign(vel.y);
                    }
                }
            }
        }

        public override void onDestroy()
        {
            if (!ownedByLocalPlayer) return;
            if (type == 0)
            {
                new GrenadeExplosionProj(weapon, pos, xDir, player, type, target, Math.Sign(vel.x), player.getNextActorNetId());
            }
        }

        float health = 2;
        public void applyDamage(Player owner, int? weaponIndex, float damage, int? projId)
        {
            if (type == 1)
            {
                health -= damage;
                if (health < 0)
                {
                    health = 0;
                    destroySelf();
                }
            }
        }

        public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId)
        {
            return sprite.name == "axl_grenade_mine" && owner.alliance != damagerAlliance;
        }
        
        public bool isInvincible(Player attacker, int? projId) { return false; }
        public bool canBeHealed(int healerAlliance) { return false; }
        public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) { }

        public void detonate()
        {
            playSound("detonate", sendRpc: true);
            new GrenadeExplosionProj(weapon, pos, xDir, player, type, target, Math.Sign(vel.x), player.getNextActorNetId());
            destroySelfNoEffect();
        }
    }

    public class GrenadeExplosionProj : Projectile
    {
        public IDamagable directHit;
        public int directHitXDir;
        public int type;
        public List<int> rands;

        public GrenadeExplosionProj(Weapon weapon, Point pos, int xDir, Player player, int type, IDamagable directHit, int directHitXDir, ushort netProjId) : 
            base(weapon, pos, xDir, 0, 2, player, "axl_grenade_explosion2", 0, 0.25f, netProjId, player.ownedByLocalPlayer)
        {
            this.xDir = xDir;
            this.directHit = directHit;
            this.directHitXDir = directHitXDir;
            this.type = type;
            destroyOnHit = false;
            projId = (int)ProjIds.GLauncherSplash;
            playSound("grenadeExplode");
            shouldShieldBlock = false;
            rands = new List<int>();
            for (int i = 0; i < 8; i++)
            {
                rands.Add(Helpers.randomRange(-22, 22));
            }
            if (type == 1)
            {
                damager.damage = 4;
            }
            if (ownedByLocalPlayer)
            {
                rpcCreate(pos, owner, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();

            if (sprite.name == "axl_grenade_explosion_hyper")
            {
                xScale = 1.5f;
                yScale = 1.5f;
            }

            if (isAnimOver())
            {
                destroySelf();
            }
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            //float maxTime = type == 0 ? 0.15f : 0.2f;
            //float len = type == 0 ? 10 : 15;
            float maxTime = 0.175f;
            float len = 7;
            if (time < maxTime)
            {
                for (int i = 0; i < 8; i++)
                {
                    float angle = (i * 45) + rands[i];
                    float ox = (len * time * 25) * Helpers.cosd(angle);
                    float oy = (len * time * 25) * Helpers.sind(angle);
                    float ox2 = len * Helpers.cosd(angle);
                    float oy2 = len * Helpers.sind(angle);
                    DrawWrappers.DrawLine(pos.x + ox, pos.y + oy, pos.x + ox + ox2, pos.y + oy + oy2, Color.Yellow, 1, zIndex, true);
                }
            }
        }

        public override DamagerMessage onDamage(IDamagable damagable, Player attacker)
        {
            Character character = damagable as Character;

            if (character != null)
            {
                bool directHit = this.directHit == character;
                int directHitXDir = this.directHitXDir;
                bool isSelf = (character == attacker.character);

                var victimCenter = character.getCenterPos();
                var bombCenter = pos;
                if (directHit)
                {
                    bombCenter.x = victimCenter.x - (directHitXDir * 5);
                }
                var dirTo = bombCenter.directionTo(victimCenter);
                var distFactor = Helpers.clamp01(1 - (bombCenter.distanceTo(victimCenter) / 60f));

                if (isSelf) character.vel.y += dirTo.y * 10 * distFactor;
                else character.vel.y = dirTo.y * 10 * distFactor;

                if (character == attacker.character)
                {
                    character.xSwingVel += dirTo.x * 10 * distFactor;
                    float damage = damager.damage;
                    if (character.isWhiteAxl()) damage = 0;
                    return new DamagerMessage()
                    {
                        damage = damage,
                        flinch = 0
                    };
                }
                else
                {
                    character.xPushVel = dirTo.x * 10 * distFactor;
                }
            }

            return null;
        }
    }

    public class GrenadeProjCharged : Projectile
    {
        IDamagable target;
        public GrenadeProjCharged(Weapon weapon, Point pos, int xDir, Player player, Point bulletDir, IDamagable target, ushort netProjId) : base(weapon, pos, xDir, 400, 0, player, "axl_rocket", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            this.target = target;
            vel.x = speed * bulletDir.x;
            vel.y = speed * bulletDir.y;
            collider.wallOnly = true;
            destroyOnHit = false;
            this.xDir = xDir;
            angle = MathF.Atan2(vel.y, vel.x) * 180 / MathF.PI;
            projId = (int)ProjIds.Explosion;
            maxTime = 0.35f;
            shouldShieldBlock = false;
        }

        public override void update()
        {
            if (!ownedByLocalPlayer) return;
            base.update();
            if (grounded)
            {
                destroySelf();
                return;
            }
        }

        public override void onCollision(CollideData other)
        {
            if (!ownedByLocalPlayer) return;
            base.onCollision(other);
            var damagable = other.gameObject as IDamagable;
            if (damagable != null && damagable.canBeDamaged(owner.alliance, owner.id, projId))
            {
                destroySelf();
                return;
            }
            var wall = other.gameObject as Wall;
            if (wall != null)
            {
                destroySelf();
                return;
            }
        }

        public override void onDestroy()
        {
            if (!ownedByLocalPlayer) return;
            if (time >= maxTime) return;
            var netId = owner.getNextActorNetId();
            new GrenadeExplosionProjCharged(weapon, pos, xDir, owner, angle.Value, target, Math.Sign(vel.x), netId);
        }
    }

    public class GrenadeExplosionProjCharged : Projectile
    {
        public IDamagable directHit;
        public int directHitXDir;
        public GrenadeExplosionProjCharged(Weapon weapon, Point pos, int xDir, Player player, float angle, IDamagable directHit, int directHitXDir, ushort netProjId) : 
            base(weapon, pos, xDir, 0, 3, player, "axl_rocket_explosion", 13, 0.25f, netProjId, player.ownedByLocalPlayer)
        {
            this.xDir = xDir;
            this.angle = angle;
            this.directHit = directHit;
            this.directHitXDir = directHitXDir;
            destroyOnHit = false;
            playSound("rocketExplode");
            projId = (int)ProjIds.ExplosionSplash;
            shouldShieldBlock = false;
            if (ownedByLocalPlayer)
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

        public override DamagerMessage onDamage(IDamagable damagable, Player attacker)
        {
            Character character = damagable as Character;

            if (character != null)
            {
                bool directHit = this.directHit == character;
                int directHitXDir = this.directHitXDir;
                float ownAxlFactor = 1f;
                if (character == attacker.character && !character.grounded)
                {
                    ownAxlFactor = 1.5f;
                }

                var victimCenter = character.getCenterPos();
                var bombCenter = pos;
                if (directHit)
                {
                    bombCenter.x = victimCenter.x - (directHitXDir * 5);
                }
                var dirTo = bombCenter.directionTo(victimCenter);
                var distFactor = Helpers.clamp01(1 - (bombCenter.distanceTo(victimCenter) / 60f));

                character.vel.y = dirTo.y * 25 * distFactor * ownAxlFactor;
                if (character == attacker.character)
                {
                    character.xSwingVel = dirTo.x * 12 * distFactor * ownAxlFactor;
                    float damage = damager.damage;
                    if (character.isWhiteAxl()) damage = 0;
                    return new DamagerMessage()
                    {
                        damage = damage,
                        flinch = 0
                    };
                }
                else
                {
                    character.xPushVel = dirTo.x * 12 * distFactor * ownAxlFactor;
                }
            }

            return null;
        }
    }
}
