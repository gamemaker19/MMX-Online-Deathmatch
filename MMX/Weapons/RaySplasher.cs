using SFML.Graphics;
using SFML.Graphics.Glsl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class RaySplasher : Weapon
    {
        public RaySplasher() : base()
        {
            shootSounds = new List<string>() { "raySplasher", "raySplasher", "raySplasher", "warpIn" };
            rateOfFire = 1f;
            index = (int)WeaponIds.RaySplasher;
            weaponBarBaseIndex = 21;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 21;
            killFeedIndex = 44;
            weaknessIndex = (int)WeaponIds.SpinningBlade;
        }

        public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId)
        {
            if (chargeLevel != 3)
            {
                player.character.setShootRaySplasher(true);
            }
            else
            {
                if (player.character.ownedByLocalPlayer) player.character.changeState(new RaySplasherChargedState(), true);
            }
        }
    }

    public class RaySplasherProj : Projectile
    {
        public RaySplasherProj(Weapon weapon, Point pos, int xDir, int spriteType, int dirType, bool isTurret, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 600, 1, player, "raysplasher_proj", 0, 0.075f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 0.25f;
            projId = (int)ProjIds.RaySplasher;
            if (isTurret)
            {
                projId = (int)ProjIds.RaySplasherChargedProj;
                damager.hitCooldown = 0;
            }
            reflectable = true;
            frameIndex = spriteType;
            frameSpeed = 0;
            fadeSprite = "raysplasher_fade";
            if (dirType == 0)
            {
                vel = new Point(600 * xDir, -150);
            }
            if (dirType == 1)
            {
                vel = new Point(600 * xDir, 150);
            }
            if (dirType == 2)
            {
                vel = new Point(600 * xDir, 0);
            }

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }

    public class RaySplasherTurret : Actor, IDamagable
    {
        int state = 0;
        Actor target;
        float health = 4;
        float maxHealth = 4;
        const float range = 130;
        float drainTime;

        float raySplasherShootTime;
        int raySplasherMod;

        float velY;
        ShaderWrapper replaceColorShader;

        public RaySplasherTurret(Point pos, Player player, int xDir, ushort netId, bool ownedByLocalPlayer, bool rpc = false) :
            base("raysplasher_turret_start", pos, netId, ownedByLocalPlayer, false)
        {
            useGravity = false;
            velY = -50;

            this.xDir = xDir;

            removeRenderEffect(RenderEffectType.BlueShadow);
            removeRenderEffect(RenderEffectType.RedShadow);

            
            replaceColorShader = Helpers.cloneShaderSafe("replacecolor");
            if (replaceColorShader != null)
            {
                Vec4 origColor = new Vec4(8 / 255f, 8 / 255f, 8 / 255f, 0);
                if (player.isMainPlayer)
                {
                    replaceColorShader.SetUniform("origColor", origColor);
                    replaceColorShader.SetUniform("replaceColor", new Vec4(0, 0.75f, 0, 0.5f));
                }
                else if (Global.level.gameMode.isTeamMode && player.alliance == GameMode.redAlliance)
                {
                    replaceColorShader.SetUniform("origColor", origColor);
                    replaceColorShader.SetUniform("replaceColor", new Vec4(0.75f, 0, 0, 0.5f));
                }
                else if (Global.level.gameMode.isTeamMode && player.alliance == GameMode.blueAlliance)
                {
                    replaceColorShader.SetUniform("origColor", origColor);
                    replaceColorShader.SetUniform("replaceColor", new Vec4(0, 0, 0.75f, 0.5f));
                }
            }

            netOwner = player;
            netActorCreateId = NetActorCreateId.RaySplasherTurret;
            if (rpc)
            {
                createActorRpc(player.id);
            }
        }

        public override List<ShaderWrapper> getShaders()
        {
            if (replaceColorShader != null)
            {
                return new List<ShaderWrapper>() { replaceColorShader };
            }
            return null;
        }

        public override void update()
        {
            base.update();
            updateProjectileCooldown();

            if (!ownedByLocalPlayer) return;

            if (state == 1 || state == 2)
            {
                drainTime += Global.spf;
                if (drainTime >= 4)
                {
                    destroySelf();
                    return;
                }
            }

            if (state == 0)
            {
                var hits = Global.level.getTriggerList(this, 0, velY * Global.spf, null, typeof(Wall));
                if (hits.Count == 0)
                {
                    move(new Point(0, velY));
                }
                velY += Global.spf * 75;
                if (velY >= 0)
                {
                    velY = 0;
                    state = 1;
                    changeSprite("raysplasher_turret", true);
                }
            }
            else if (state == 1)
            {
                isStatic = true;
                var closestTarget = Global.level.getClosestTarget(pos, netOwner.alliance, true, aMaxDist: range);
                if (closestTarget != null)
                {
                    target = closestTarget;
                    state = 2;
                    changeSprite("raysplasher_turret_fire", true);
                    playSound("raySplasher");
                }
            }
            else if (state == 2)
            {
                Actor oldTarget = target;
                target = Global.level.getClosestTarget(pos, netOwner.alliance, true);
                if (target == null || pos.distanceTo(target.getCenterPos()) >= range)
                {
                    state = 1;
                    target = null;
                    changeSprite("raysplasher_turret", true);
                }
                else
                {
                    raySplasherShootTime += Global.spf;
                    if (raySplasherShootTime > 0.1f)
                    {
                        var proj = new RaySplasherProj(new RaySplasher(), pos, (pos.x > target.getCenterPos().x ? -1 : 1), raySplasherMod % 3, 0, true, netOwner, netOwner.getNextActorNetId(), rpc: true);

                        float ang = pos.directionToNorm(target.getCenterPos()).angle;
                        ang += Helpers.randomRange(-22.5f, 22.5f);
                        proj.vel = Point.createFromAngle(ang).times(600);

                        //float randAngle = Helpers.randomRange(0, 360);
                        //proj.vel = new Point(600 * Helpers.cosd(randAngle), 600 * Helpers.sind(randAngle));

                        raySplasherShootTime = 0;
                        raySplasherMod++;
                    }
                }
            }
        }

        public void applyDamage(Player owner, int? weaponIndex, float damage, int? projId)
        {
            if (projId == (int)ProjIds.SpinningBlade)
            {
                damage *= 2;
            }

            addDamageTextHelper(owner, damage, 4, false);

            health -= damage;
            if (health <= 0)
            {
                health = 0;
                if (ownedByLocalPlayer) destroySelf();
            }
        }

        public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId)
        {
            if (sprite.name == "raysplasher_turret_start") return false;
            return projId == (int)ProjIds.RaySplasherHurtSelf || netOwner.alliance != damagerAlliance;
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
            playSound("freezebreak2");
            new Anim(pos, "raysplasher_turret_pieces", 1, null, false) { ttl = 2, useGravity = true, vel = Point.random(-150, -50, -100, -50), frameIndex = 0, frameSpeed = 0 };
            new Anim(pos, "raysplasher_turret_pieces", -1, null, false) { ttl = 2, useGravity = true, vel = Point.random(50, 150, -100, -50), frameIndex = 0, frameSpeed = 0 };
            new Anim(pos, "raysplasher_turret_pieces", 1, null, false) { ttl = 2, useGravity = true, vel = Point.random(-150, -50, -100, -50), frameIndex = 1, frameSpeed = 0 };
            new Anim(pos, "raysplasher_turret_pieces", -1, null, false) { ttl = 2, useGravity = true, vel = Point.random(50, 150, -100, -50), frameIndex = 1, frameSpeed = 0 };
            netOwner.turrets.Remove(this);
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            if (netOwner.alliance == Global.level.mainPlayer.alliance)
            {
                float healthBarInnerWidth = 14;
                Color color = new Color();

                float healthPct = health / maxHealth;
                float width = Helpers.clampMax(MathF.Ceiling(healthBarInnerWidth * healthPct), healthBarInnerWidth);
                if (healthPct > 0.66) color = Color.Green;
                else if (healthPct <= 0.66 && healthPct >= 0.33) color = Color.Yellow;
                else if (healthPct < 0.33) color = Color.Red;

                float offY = 12;
                DrawWrappers.DrawRect(pos.x - 8, pos.y - 3 - offY, pos.x + 8, pos.y - offY, true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White);
                DrawWrappers.DrawRect(pos.x - 7, pos.y - 2 - offY, pos.x - 7 + width, pos.y - 1 - offY, true, color, 0, ZIndex.HUD - 1);
            }

            if (netOwner.isMainPlayer && replaceColorShader == null)
            {
                Global.sprites["cursorchar"].draw(0, pos.x + x, pos.y + y - 17, 1, 1, null, 1, 1, 1, zIndex + 1);
            }

            renderDamageText(10);
        }
    }


    public class RaySplasherChargedState : CharState
    {
        bool fired = false;
        public RaySplasherChargedState() : base("point_up", "", "", "")
        {
            superArmor = true;
        }

        public override void update()
        {
            base.update();

            if (character.frameIndex >= 3 && !fired)
            {
                fired = true;

                var turret = new RaySplasherTurret(character.getShootPos(), player, character.xDir, player.getNextActorNetId(), character.ownedByLocalPlayer, rpc: true);
                player.turrets.Add(turret);
                if (player.turrets.Count > 1)
                {
                    player.turrets[0].destroySelf();
                }
            }

            if (stateTime > 0.5f)
            {
                character.changeState(new Idle());
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.useGravity = false;
            character.vel = new Point();
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.useGravity = true;
        }
    }
}
