using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class GigaCrush : Weapon
    {
        public GigaCrush() : base()
        {
            shootSounds = new List<string>() { "gigaCrush", "gigaCrush", "gigaCrush", "gigaCrush" };
            rateOfFire = 1;
            ammo = 0;
            index = (int)WeaponIds.GigaCrush;
            weaponBarBaseIndex = 25;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 25;
            killFeedIndex = 13;
        }

        public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId)
        {
            if (player.character.ownedByLocalPlayer)
            {
                player.character.changeState(new GigaCrushCharState(), true);
            }
            new GigaCrushEffect(player.character);
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            return 32;
        }

        public override bool canShoot(int chargeLevel, Player player)
        {
            return player.character?.flag == null && ammo >= (player.hasChip(3) ? 16 : 32);
        }
    }

    public class GigaCrushProj : Projectile
    {
        public float radius = 10;
        public GigaCrushProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) : base(weapon, pos, xDir, 0, 12, player, "empty", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 0.4f;
            destroyOnHit = false;
            shouldShieldBlock = false;
            vel = new Point();
            projId = (int)ProjIds.GigaCrush;
            shouldVortexSuck = false;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (isRunByLocalPlayer())
            {
                foreach (var go in Global.level.getGameObjectArray())
                {
                    var actor = go as Actor;
                    var damagable = go as IDamagable;
                    if (actor == null) continue;
                    if (damagable == null) continue;
                    if (!damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, null)) continue;
                    if (actor.pos.distanceTo(pos) > radius + 15) continue;

                    damager.applyDamage(damagable, false, weapon, this, projId);
                }
            }
            radius += Global.spf * 400;
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            DrawWrappers.DrawCircle(pos.x + x, pos.y + y, radius, false, Color.White, 5, zIndex + 1, true, Color.White);
        }
    }

    public class GigaCrushCharState : CharState
    {
        GigaCrushProj proj;
        bool fired;
        public GigaCrushCharState() : base("gigacrush", "", "", "")
        {
            invincible = true;
        }

        public override void update()
        {
            base.update();
            if (character.frameIndex == 4 && !fired)
            {
                fired = true;
                proj = new GigaCrushProj(new GigaCrush(), character.getCenterPos(), character.xDir, player, player.getNextActorNetId(), rpc: true);
            }
            if (character.sprite.isAnimOver())
            {
                character.changeState(new Idle(), true);
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            player.character.useGravity = false;
            player.character.vel.y = 0;
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            player.character.useGravity = true;
            if (proj != null && !proj.destroyed) proj.destroySelf();
        }
    }

    public class GigaCrushEffect : Effect
    {
        public Character character;
        float frame1Time;
        float frame2Time;
        float time;
        public GigaCrushEffect(Character character) : base(character.pos)
        {
            this.character = character;
        }

        public override void update()
        {
            base.update();

            pos = character.pos;
            time += Global.spf;
            if (time > 3)
            {
                destroySelf();
                return;
            }

            if (character.sprite.name != "mmx_gigacrush" || character.frameIndex > 2)
            {
                return;
            }

            if (character.frameIndex < 2)
            {
                frame1Time += Global.spf;
            }
            if (character.frameIndex == 2)
            {
                frame2Time += Global.spf;
            }
        }

        public override void render(float offsetX, float offsetY)
        {
            base.render(offsetX, offsetY);

            if (character.sprite.name != "mmx_gigacrush" || character.frameIndex > 2)
            {
                return;
            }

            var pos = character.getCenterPos();
            if (character.frameIndex < 2)
            {
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 45;
                    float ox = Helpers.randomRange(10, 30) * Helpers.cosd(angle) * (MathF.Round(25 * frame1Time / 1.5f) % 5);
                    float oy = Helpers.randomRange(10, 30) * Helpers.sind(angle) * (MathF.Round(25 * frame1Time / 1.5f) % 5);
                    DrawWrappers.DrawLine(pos.x + ox, pos.y + oy, pos.x + ox * 2, pos.y + oy * 2, Color.White, 1, character.zIndex, true);
                }
            }
            else if (character.frameIndex == 2)
            {
                float radius = 150 - (frame2Time * 150 / 0.5f);
                if (radius > 0)
                {
                    DrawWrappers.DrawCircle(pos.x, pos.y, radius, true, Color.White, 0, ZIndex.Character - 1);
                }
            }
        }
    }
}
