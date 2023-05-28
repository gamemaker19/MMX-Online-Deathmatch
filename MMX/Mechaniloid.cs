using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public enum MechaniloidType
    {
        Tank,
        Hopper,
        Bird,
        Fish,
    }

    public class MechaniloidWeapon : Weapon
    {
        public MechaniloidType mechaniloidType;
        public ProjIds projId;
        public MechaniloidWeapon(Player player, MechaniloidType mechaniloidType) : base()
        {
            weaponSlotIndex = 105 + (int)mechaniloidType;
            this.mechaniloidType = mechaniloidType;
            index = (int)WeaponIds.Sigma2Mechaniloid;
            projId = ProjIds.Sigma2BirdProj;
            float damage = 3;
            if (mechaniloidType == MechaniloidType.Bird)
            {
                damage = 6;
            }
            damager = new Damager(player, damage, Global.defFlinch, 1);
            killFeedIndex = 137 + (int)mechaniloidType;
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            if (mechaniloidType == MechaniloidType.Tank) return 3;
            if (mechaniloidType == MechaniloidType.Hopper) return 2;
            if (mechaniloidType == MechaniloidType.Fish) return 2;
            if (mechaniloidType == MechaniloidType.Bird) return 1;
            return 1;
        }
    }

    public class BirdMechaniloidProj : Projectile, IDamagable
    {
        public Actor target;
        public float smokeTime = 0;
        public float maxSpeed = 150;
        float health = 4;
        int state = 0;
        public BirdMechaniloidProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 3, player, "sigma2_bird", 0, 0f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.Sigma2BirdProj;
            fadeSprite = "explosion";
            fadeSound = "explosion";
            destroyOnHit = true;
            vel = new Point(0, -100);

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            updateProjectileCooldown();

            if (MathF.Abs(deltaPos.x) > 100 * Global.spf)
            {
                damager.damage = 6;
                damager.flinch = Global.defFlinch;
            }

            if (!ownedByLocalPlayer) return;

            if (isUnderwater())
            {
                destroySelf();
                return;
            }

            if (state == 0)
            {
                vel.y = Helpers.lerp(vel.y, 0, Global.spf * 5);
                if (MathF.Abs(vel.y) < 10)
                {
                    state = 1;
                    vel.y = 0;
                }
            }
            else if (state == 1)
            {
                maxTime = 2f;
                if (MathF.Abs(vel.x) < 200)
                {
                    vel.x += xDir * 500 * Global.spf;
                }
            }
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            if (!ownedByLocalPlayer) return;

            destroySelf();
        }

        public void applyDamage(Player owner, int? weaponIndex, float damage, int? projId)
        {
            health -= damage;
            if (health <= 0)
            {
                health = 0;
                if (ownedByLocalPlayer) destroySelf();
            }
        }

        public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId)
        {
            return damager.owner.alliance != damagerAlliance;
        }

        public bool isInvincible(Player attacker, int? projId) { return false; }
        public bool canBeHealed(int healerAlliance) { return false; }
        public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) { }

        public override void onDestroy()
        {
            base.onDestroy();
            owner.mechaniloids.Remove(this);
        }
    }

    public class Mechaniloid : Actor, IDamagable
    {
        int state = 0;
        Actor target;
        float health = 8;
        float maxHealth = 8;
        const float sightRange = 130;
        float attackRange;
        string baseSprite;
        public MechaniloidType type;
        MechaniloidWeapon weapon;
        float speed;
        float maxTime = 16;

        public static string getSpriteFromType(MechaniloidType type)
        {
            if (type == MechaniloidType.Tank) return "sigma2_tank";
            if (type == MechaniloidType.Hopper) return "sigma2_hopper";
            return "sigma2_fish";
        }

        public Mechaniloid(Point pos, Player player, int xDir, MechaniloidWeapon weapon, MechaniloidType type, ushort netId, bool ownedByLocalPlayer, bool rpc = false) :
            base(getSpriteFromType(type), pos, netId, ownedByLocalPlayer, false)
        {
            this.xDir = xDir;
            this.baseSprite = sprite.name;
            this.type = type;
            this.weapon = weapon;

            useFrameProjs = true;

            if (type == MechaniloidType.Tank)
            {
                speed = 100;
                attackRange = 125;
                useGravity = true;
                netActorCreateId = NetActorCreateId.MechaniloidTank;
            }
            if (type == MechaniloidType.Fish)
            {
                speed = 75;
                useGravity = false;
                netActorCreateId = NetActorCreateId.MechaniloidFish;
            }
            if (type == MechaniloidType.Hopper)
            {
                speed = 100;
                attackRange = 30;
                useGravity = true;
                maxHealth = 4;
                netActorCreateId = NetActorCreateId.MechaniloidHopper;
            }

            if (player == Global.level.mainPlayer)
            {
                addRenderEffect(RenderEffectType.GreenShadow);
            }
            else if (Global.level.gameMode.isTeamMode)
            {
                if (player.alliance == GameMode.blueAlliance)
                {
                    addRenderEffect(RenderEffectType.BlueShadow);
                }
                else
                {
                    addRenderEffect(RenderEffectType.RedShadow);
                }
            }

            health = maxHealth;

            leftPatrolX = pos.x - 150;
            rightPatrolX = pos.x + 150;

            netOwner = player;
            if (rpc)
            {
                createActorRpc(player.id);
            }
        }

        float time;
        public override void update()
        {
            base.update();
            updateProjectileCooldown();

            if (!ownedByLocalPlayer) return;

            Helpers.decrementTime(ref attackCooldown);
            Helpers.decrementTime(ref jumpCooldown);

            var leeway = 500;
            time += Global.spf;
            if (time > maxTime || pos.x > Global.level.width + leeway || pos.x < -leeway || pos.y > Global.level.height + leeway || pos.y < -leeway)
            {
                destroySelf();
                return;
            }

            if (type == MechaniloidType.Hopper)
            {
                if (isJumping && grounded)
                {
                    isJumping = false;
                    jumpCooldown = 1;
                    changeSprite("sigma2_hopper", true);
                }
            }

            // Patrolling
            if (state == 0)
            {
                if (type == MechaniloidType.Tank || type == MechaniloidType.Fish)
                {
                    patrol();
                }
                if (type == MechaniloidType.Tank || type == MechaniloidType.Hopper)
                {
                    var closestTarget = Global.level.getClosestTarget(pos, netOwner.alliance, true, aMaxDist: sightRange);
                    if (closestTarget != null)
                    {
                        target = closestTarget;
                        state = 2;
                    }
                }
            }
            // Turning
            else if (state == 1)
            {
                if (frameIndex == 0)
                {
                    move(new Point(xDir * speed * (sprite.animTime / 0.16f), 0));
                }
                if (sprite.isAnimOver())
                {
                    changeSprite(baseSprite, true);
                    state = 0;
                    xDir *= -1;
                }
            }
            // Move to target
            else if (state == 2)
            {
                target = Global.level.getClosestTarget(pos, netOwner.alliance, true);
                if (target == null || pos.distanceTo(target.getCenterPos()) >= sightRange)
                {
                    state = 0;
                    target = null;
                    return;
                }

                turnToPos(target.pos);

                if (type == MechaniloidType.Tank)
                {
                    moveToXPos(target.pos, speed);
                }
                else if (type == MechaniloidType.Hopper)
                {
                    jump();
                }

                if (targetInAttackRange())
                {
                    state = 3;
                }
            }
            // Attacking
            else if (state == 3)
            {
                if (!targetInAttackRange())
                {
                    state = 2;
                    if (type == MechaniloidType.Hopper)
                    {
                        changeSprite("sigma2_hopper", true);
                    }
                    return;
                }

                if (type == MechaniloidType.Tank)
                {
                    tankAttack();
                }
                else if (type == MechaniloidType.Hopper)
                {
                    hopperAttack();
                }
            }
        }

        public float leftPatrolX;
        public float rightPatrolX;
        public void patrol()
        {
            var move = new Point(xDir * speed, 0);
            var hitGround = Global.level.checkCollisionActor(this, xDir * 30, 20);
            var hitWall = Global.level.checkCollisionActor(this, move.x * Global.spf * 2, -5);
            bool blocked = ((grounded && hitGround == null) || hitWall?.isSideWallHit() == true);
            this.move(move);

            if (xDir == 1)
            {
                if (pos.x > rightPatrolX || blocked)
                {
                    if (blocked)
                    {
                        leftPatrolX = pos.x - 300;
                        rightPatrolX = pos.x;
                    }
                    changeSprite(baseSprite + "_turn", true);
                    state = 1;
                }
            }
            else if (xDir == -1)
            {
                if (pos.x < leftPatrolX || blocked)
                {
                    if (blocked)
                    {
                        leftPatrolX = pos.x;
                        rightPatrolX = pos.x + 300;
                    }
                    changeSprite(baseSprite + "_turn", true);
                    state = 1;
                }
            }
        }

        float attackCooldown;
        bool shot;
        public void tankAttack()
        {
            if (sprite.name != "sigma2_tank_shoot")
            {
                if (attackCooldown == 0)
                {
                    changeSprite("sigma2_tank_shoot", true);
                }
            }
            else
            {
                if (frameIndex == 0 && !shot)
                {
                    shot = true;
                    playSound("viralSigmaTankShoot", sendRpc: true);
                    var poi = getFirstPOIOrDefault();
                    new TankMechaniloidProj(weapon, poi, xDir, netOwner, netOwner.getNextActorNetId(), rpc: true);
                }
                if (sprite.isAnimOver())
                {
                    shot = false;
                    attackCooldown = 0.5f;
                    changeSprite("sigma2_tank", true);
                }
            }
        }

        public void hopperAttack()
        {
            if (sprite.name != "sigma2_hopper_attack")
            {
                changeSprite("sigma2_hopper_attack", true);
            }
        }

        bool isJumping;
        float jumpCooldown;
        public void jump()
        {
            if (!isJumping && jumpCooldown == 0)
            {
                isJumping = true;
                changeSprite("sigma2_hopper_hop", true);
                vel.y = -250;
                grounded = false;
            }
            else if (isJumping)
            {
                moveToXPos(target.pos, speed);
            }
        }

        public override Dictionary<int, Func<Projectile>> getGlobalProjs()
        {
            var retProjs = new Dictionary<int, Func<Projectile>>();
            if (collider == null || weapon == null)
            {
                return retProjs;
            }

            retProjs[(int)weapon.projId] = () =>
            {
                Point centerPoint = collider.shape.getRect().center();
                Projectile proj = new GenericMeleeProj(weapon, centerPoint, weapon.projId, netOwner);
                proj.globalCollider = collider.clone();
                proj.netcodeOverride = NetcodeModel.FavorDefender;
                return proj;
            };

            return retProjs;
        }

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            Projectile proj = null;
            if (sprite.name == "sigma2_hopper_attack")
            {
                proj = new GenericMeleeProj(weapon, centerPoint, ProjIds.Sigma2HopperDrill, netOwner, 1, Global.defFlinch, 0.15f);
                proj.netcodeOverride = NetcodeModel.FavorDefender;
            }
            return proj;
        }

        public bool targetInAttackRange()
        {
            target = Global.level.getClosestTarget(pos, netOwner.alliance, true);
            if (target == null) return false;
            return MathF.Abs(pos.x - target.pos.x) < attackRange && MathF.Abs(pos.y - target.pos.y) < 30;
        }

        public void applyDamage(Player owner, int? weaponIndex, float damage, int? projId)
        {
            health -= damage;
            if (health <= 0)
            {
                health = 0;
                if (ownedByLocalPlayer) destroySelf();
            }
        }

        public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId)
        {
            return netOwner.alliance != damagerAlliance;
        }

        public bool isInvincible(Player attacker, int? projId)
        {
            return false;
        }

        public bool canBeHealed(int healerAlliance)
        {
            return netOwner.alliance == healerAlliance && health < maxHealth;
        }

        public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false)
        {
            health += healAmount;
            if (drawHealText && healer != netOwner && ownedByLocalPlayer)
            {
                addDamageTextHelper(netOwner, -healAmount, 16, sendRpc: true);
            }
            if (health > maxHealth) health = maxHealth;
        }

        public override void onDestroy()
        {
            base.onDestroy();
            playSound("explosion");
            new Anim(getCenterPos(), "explosion", 1, null, true);
            if (ownedByLocalPlayer)
            {
                netOwner.mechaniloids.Remove(this);
            }
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (!ownedByLocalPlayer) return;

            var killZone = other.gameObject as KillZone;
            if (killZone != null)
            {
                killZone.applyDamage(this);
            }
        }
    }

    public class TankMechaniloidProj: Projectile
    {
        public TankMechaniloidProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 250, 2, player, "sigma2_tank_proj", 0, 0.1f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.Sigma2TankProj;
            destroyOnHit = true;
            maxTime = 0.5f;
            destroyOnHitWall = true;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }
}
