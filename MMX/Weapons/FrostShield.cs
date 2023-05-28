using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class FrostShield : Weapon
    {
        public FrostShield() : base()
        {
            shootSounds = new List<string>() { "frostShield", "frostShield", "frostShield", "frostShieldCharged" };
            rateOfFire = 1f;
            index = (int)WeaponIds.FrostShield;
            weaponBarBaseIndex = 23;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 23;
            killFeedIndex = 46;
            weaknessIndex = (int)WeaponIds.ParasiticBomb;
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
                new FrostShieldProj(this, pos, xDir, player, netProjId);
            }
            else
            {
                if (player.character?.isUnderwater() == true)
                {
                    var cfs = new FrostShieldProjPlatform(this, pos, xDir, player, netProjId);
                }
                else
                {
                    var cfs = new FrostShieldProjCharged(this, pos, xDir, player, netProjId);
                    if (player.character.ownedByLocalPlayer) player.character.chargedFrostShield = cfs;
                }
            }
        }
    }

    public class FrostShieldProj : Projectile
    {
        int state = 0;
        float stateTime;
        public Anim exhaust;
        public bool noSpawn;
        public FrostShieldProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 3, 2, player, "frostshield_start", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 3;
            projId = (int)ProjIds.FrostShield;
            destroyOnHit = true;
            exhaust = new Anim(pos, "frostshield_exhaust", xDir, null, false);
            isShield = true;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();

            exhaust.pos = pos;
            exhaust.xDir = xDir;
            if (!ownedByLocalPlayer) return;

            if (state == 0)
            {
                stateTime += Global.spf;
                if (stateTime >= 0.5f)
                {
                    state = 1;
                    changeSprite("frostshield_proj", true);
                }
            }
            else if (state == 1)
            {
                vel.x += Global.spf * 200 * xDir;
                if (MathF.Abs(vel.x) > 150) vel.x = 150 * xDir;
            }
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            if (!ownedByLocalPlayer) return;
            destroySelf();
        }

        public void shatter()
        {
            breakFreeze(owner);
            if (ownedByLocalPlayer && noSpawn == false)
            {
                new FrostShieldProjAir(weapon, pos, -xDir, vel.x, owner, owner.getNextActorNetId(), rpc: true);
            }
        }

        public override void onDestroy()
        {
            base.onDestroy();
            exhaust?.destroySelf();
            shatter();
        }
    }

    public class FrostShieldProjAir : Projectile
    {
        public FrostShieldProjAir(Weapon weapon, Point pos, int xDir, float xVel, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 100, 0, player, "frostshield_air", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 3;
            projId = (int)ProjIds.FrostShieldAir;
            useGravity = true;
            destroyOnHit = false;
            collider.wallOnly = true;
            vel = new Point(-xVel * 0.5f, -150);
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            var wall = Global.level.checkCollisionActor(this, vel.x * Global.spf, vel.y * Global.spf, vel);
            if (wall != null && wall.gameObject is Wall)
            {
                vel.x *= -1;
            }
            
            if (grounded)
            {
                destroySelf();
                new FrostShieldProjGround(weapon, pos, xDir, owner, owner.getNextActorNetId(), rpc: true);
            }
        }
    }

    public class FrostShieldProjGround : Projectile, IDamagable
    {
        float health = 4;
        public FrostShieldProjGround(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 2, player, "frostshield_ground_start", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 5;
            projId = (int)ProjIds.FrostShieldGround;
            destroyOnHit = true;
            isShield = true;
            playSound("frostShield");
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            updateProjectileCooldown();

            if (ownedByLocalPlayer)
            {
                moveWithMovingPlatform();
            }
        }

        public void applyDamage(Player owner, int? weaponIndex, float damage, int? projId)
        {
            health -= damage;
            if (health <= 0)
            {
                destroySelf();
            }
        }

        public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId)
        {
            return damagerAlliance != owner.alliance;
        }

        public bool canBeHealed(int healerAlliance)
        {
            return false;
        }

        public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false)
        {
        }

        public bool isInvincible(Player attacker, int? projId)
        {
            if (projId == null) return true;
            return !Damager.canDamageFrostShield(projId.Value);
        }

        public override void onDestroy()
        {
            base.onDestroy();
            breakFreeze(owner);
        }
    }

    public class FrostShieldProjCharged : Projectile
    {
        public Character character;
        public FrostShieldProjCharged(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 300, 3, player, "frostshield_charged_start", Global.defFlinch, 1, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 5;
            projId = (int)ProjIds.FrostShield;
            destroyOnHit = false;
            shouldVortexSuck = false;
            character = player.character;
            isShield = true;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            if (isAnimOver())
            {
                if (character.charState is Dash || character.charState is AirDash)
                {
                    if (damager.damage != 3) updateDamager(3);
                }
                else
                {
                    if (damager.damage != 0) updateDamager(0);
                }
            }

            if (character == null || character.destroyed)
            {
                destroySelf();
                return;
            }

            if (character.player.input.isPressed(Control.Shoot, character.player))
            {
                destroySelf();
            }
        }

        public override void postUpdate()
        {
            base.postUpdate();
            if (!ownedByLocalPlayer) return;
            if (destroyed) return;

            changePos(character.getShootPos());
            xDir = character.getShootXDir();
        }

        public override void onDestroy()
        {
            base.onDestroy();
            breakFreeze(owner);
            if (!ownedByLocalPlayer) return;

            character.removeBusterProjs();
            new FrostShieldProjChargedGround(weapon, pos, character.xDir, owner, owner.getNextActorNetId(), rpc: true);
        }
    }

    public class FrostShieldProjChargedGround : Projectile
    {
        public Anim slideAnim;
        public Character character;
        public FrostShieldProjChargedGround(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 3, player, "frostshield_charged_ground", Global.defFlinch, 1, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 4;
            projId = (int)ProjIds.FrostShieldChargedGrounded;
            destroyOnHit = true;
            shouldVortexSuck = false;
            character = player.character;
            useGravity = true;
            isShield = true;
            vel = new Point(xDir * 150, -100);
            collider.wallOnly = true;
            slideAnim = new Anim(pos, "frostshield_charged_slide", xDir, null, false);
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            slideAnim.visible = grounded;
            slideAnim.changePos(pos.addxy(-xDir * 5, 0));

            if (!ownedByLocalPlayer) return;
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            //if (!other.isGroundHit()) destroySelf();
            if (other.isSideWallHit())
            {
                vel.x *= -1;
                xDir *= -1;
                slideAnim.xDir *= -1;
            }
        }

        public override void onDestroy()
        {
            base.onDestroy();
            breakFreeze(owner);
            slideAnim?.destroySelf();
            if (!ownedByLocalPlayer) return;
        }
    }

    public class FrostShieldProjPlatform : Projectile
    {
        public FrostShieldProjPlatform(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 3, player, "frostshield_charged_platform", Global.defFlinch, 1, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 8;
            projId = (int)ProjIds.FrostShieldChargedPlatform;
            setIndestructableProperties();
            isShield = true;
            collider.wallOnly = true;
            grounded = false;
            canBeGrounded = false;
            useGravity = false;
            isPlatform = true;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            if (isAnimOver() && isUnderwater())
            {
                if (damager.damage != 0) updateDamager(0);
                move(new Point(0, -100));
            }
        }
    }
}
