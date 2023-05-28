using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SFML.Window.Keyboard;

namespace MMXOnline
{
    public class MagnetMine : Weapon
    {
        public const int maxMinesPerPlayer = 10;

        public MagnetMine() : base()
        {
            shootSounds = new List<string>() { "magnetMine", "magnetMine", "magnetMine", "magnetMineCharged" };
            rateOfFire = 0.75f;
            index = (int)WeaponIds.MagnetMine;
            weaponBarBaseIndex = 15;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 15;
            killFeedIndex = 20 + (index - 9);
            weaknessIndex = 11;
        }

        public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId)
        {
            if (chargeLevel != 3)
            {
                int yDir = 0;
                if (player.input.isHeld(Control.Down, player)) yDir = 1;
                else if (player.input.isHeld(Control.Up, player)) yDir = -1;
                var magnetMineProj = new MagnetMineProj(this, pos, xDir, yDir, player, netProjId);
                player.magnetMines.Add(magnetMineProj);
                if (player.magnetMines.Count > maxMinesPerPlayer)
                {
                    player.magnetMines[0].destroySelf();
                }
            }
            else
            {
                new MagnetMineProjCharged(this, pos, xDir, player, netProjId);
            }
        }
    }

    public class MagnetMineProj : Projectile, IDamagable
    {
        public bool landed;
        public float health = 2;
        public Player player;
        float maxSpeed = 150;

        public MagnetMineProj(Weapon weapon, Point pos, int xDir, int yDir, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 75, 2, player, "magnetmine_proj", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            //maxTime = 2f;
            maxDistance = 224;
            fadeSprite = "explosion";
            fadeSound = "explosion";
            reflectable = false;
            projId = (int)ProjIds.MagnetMine;
            this.player = player;
            //vel.y = yDir * speed;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) time = 0;

            updateProjectileCooldown();

            if (landed && ownedByLocalPlayer)
            {
                moveWithMovingPlatform();
            }

            if (ownedByLocalPlayer && owner != null && !landed)
            {
                vel.x += xDir * 600 * Global.spf;
                if (vel.x > maxSpeed) vel.x = maxSpeed;
                if (vel.x < -maxSpeed) vel.x = -maxSpeed;

                if (!owner.isDead)
                {
                    if (owner.input.isHeld(Control.Up, owner))
                    {
                        vel.y = Helpers.clampMin(vel.y - Global.spf * 2000, -300);
                    }
                    if (owner.input.isHeld(Control.Down, owner))
                    {
                        vel.y = Helpers.clampMax(vel.y + Global.spf * 2000, 300);
                    }
                }
            }
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (!ownedByLocalPlayer) return;
            if (!landed && other.gameObject is Wall)
            {
                landed = true;
                damager.damage *= 2;

                if (player.isMainPlayer)
                {
                    removeRenderEffect(RenderEffectType.BlueShadow);
                    removeRenderEffect(RenderEffectType.RedShadow);
                    addRenderEffect(RenderEffectType.GreenShadow);
                }

                vel = new Point();
                changeSprite("magnetmine_landed", true);
                playSound("minePlant");
                maxTime = 300;

                var triggers = Global.level.getTriggerList(this, 0, 0);
                if (triggers.Any(t => t.gameObject is MagnetMineProj))
                {
                    incPos(new Point(Helpers.randomRange(-2, 2), Helpers.randomRange(-2, 2)));
                }
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
            return player.alliance != damagerAlliance;
        }
        
        public bool isInvincible(Player attacker, int? projId)
        {
            return false;
        }

        public bool canBeHealed(int healerAlliance)
        {
            return false;
        }

        public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false)
        {
        }

        public override void onDestroy()
        {
            player.magnetMines.Remove(this);
        }

        public bool canBeSucked(int alliance)
        {
            if (player.alliance == alliance) return false;
            return true;
        }
    }

    public class MagnetMineProjCharged : Projectile
    {
        public float size;
        float startY;
        public MagnetMineProjCharged(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) : base(weapon, pos, xDir, 50, 1, player, "magnetmine_charged", Global.defFlinch, 0.2f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 4f;
            destroyOnHit = false;
            shouldShieldBlock = false;
            projId = (int)ProjIds.MagnetMineCharged;
            startY = pos.y;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            for (int i = Global.level.chargedCrystalHunters.Count - 1; i >= 0; i--)
            {
                var cch = Global.level.chargedCrystalHunters[i];
                if (cch.pos.distanceTo(pos) < CrystalHunterCharged.radius && cch.owner.alliance != damager.owner.alliance)
                {
                    cch.destroySelf(doRpcEvenIfNotOwned: true);
                    size = 11;
                    changeSprite("magnetmine_charged3", true);
                    damager.damage = 4;
                }
            }

            if (ownedByLocalPlayer && owner != null)
            {
                int maxY = 150;
                if (!owner.isDead)
                {
                    if (owner.input.isHeld(Control.Up, owner))
                    {
                        vel.y = Helpers.clampMin(vel.y - Global.spf * 2000, -300);
                    }
                    if (owner.input.isHeld(Control.Down, owner))
                    {
                        vel.y = Helpers.clampMax(vel.y + Global.spf * 2000, 300);
                    }
                }

                if (pos.y > startY + maxY)
                {
                    pos.y = startY + maxY;
                    vel.y = 0;
                }
                if (pos.y < startY - maxY)
                {
                    pos.y = startY - maxY;
                    vel.y = 0;
                }
            }
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (!ownedByLocalPlayer) return;
            var go = other.gameObject;
            if (go is Projectile)
            {
                var proj = go as Projectile;
                if (!proj.shouldVortexSuck) return;
                if (proj is MagnetMineProj magnetMine && !magnetMine.canBeSucked(damager.owner.alliance)) return;

                size += proj.damager.damage;
                proj.destroySelfNoEffect(doRpcEvenIfNotOwned: true);
                if (size > 10)
                {
                    changeSprite("magnetmine_charged3", true);
                    damager.damage = 4;
                }
                else if (size > 5)
                {
                    changeSprite("magnetmine_charged2", true);
                    damager.damage = 2;
                }
            }
        }
    }
}
