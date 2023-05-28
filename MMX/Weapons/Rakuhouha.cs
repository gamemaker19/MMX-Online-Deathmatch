using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public enum RakuhouhaType
    {
        Rakuhouha,
        CFlasher,
        Rekkoha,
        ShinMessenkou,
        DarkHold,
    }

    public class RakuhouhaWeapon : Weapon
    {
        public RakuhouhaWeapon(Player player) : base()
        {
            damager = new Damager(player, 4, Global.defFlinch, 0.5f);
            ammo = 0;
            rateOfFire = 1;
            index = (int)WeaponIds.Rakuhouha;
            weaponBarBaseIndex = 27;
            weaponBarIndex = 33;
            killFeedIndex = 16;
            weaponSlotIndex = 51;
            type = (int)RakuhouhaType.Rakuhouha;
            displayName = "Rakuhouha";
            description = new string[] { "Channels stored energy in one blast.", "Energy cost: 16" };
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            return 16;
        }

        public static Weapon getWeaponFromIndex(Player player, int index)
        {
            if (index == (int)RakuhouhaType.Rakuhouha) return new RakuhouhaWeapon(player);
            else if (index == (int)RakuhouhaType.CFlasher) return new CFlasher(player);
            else if (index == (int)RakuhouhaType.Rekkoha) return new RekkohaWeapon(player);
            else throw new Exception("Invalid Zero hyouretsuzan weapon index!");
        }
    }

    public class RekkohaWeapon : Weapon
    {
        public RekkohaWeapon(Player player) : base()
        {
            damager = new Damager(player, 4, Global.defFlinch, 0.5f);
            ammo = 0;
            rateOfFire = 2;
            index = (int)WeaponIds.Rekkoha;
            weaponBarBaseIndex = 40;
            weaponBarIndex = 34;
            killFeedIndex = 38;
            weaponSlotIndex = 63;
            type = (int)RakuhouhaType.Rekkoha;
            displayName = "Rekkoha";
            description = new string[] { "Summon down pillars of light energy.", "Energy cost: 32" };
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            return 32;
        }
    }

    public class CFlasher : Weapon
    {
        public CFlasher(Player player) : base()
        {
            damager = new Damager(player, 2, 0, 0.5f);
            ammo = 0;
            rateOfFire = 1f;
            index = (int)WeaponIds.CFlasher;
            weaponBarBaseIndex = 41;
            weaponBarIndex = 35;
            killFeedIndex = 81;
            weaponSlotIndex = 64;
            type = (int)RakuhouhaType.CFlasher;
            displayName = "C-Flasher";
            description = new string[] { "A less damaging blast that can pierce enemies.", "Energy cost: 8" };
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            return 8;
        }
    }

    public class ShinMessenkou : Weapon
    {
        public ShinMessenkou(Player player) : base()
        {
            damager = new Damager(player, 4, Global.defFlinch, 0.5f);
            ammo = 0;
            rateOfFire = 1f;
            index = (int)WeaponIds.ShinMessenkou;
            killFeedIndex = 86;
            type = (int)RakuhouhaType.ShinMessenkou;
            weaponBarBaseIndex = 43;
            weaponBarIndex = 37;
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            return 16;
        }
    }

    public class Rakuhouha : CharState
    {
        public Weapon weapon;
        RakuhouhaType type { get { return (RakuhouhaType)weapon.type; } }
        bool fired = false;
        bool fired2 = false;
        bool fired3 = false;
        const float shinMessenkouWidth = 40;
        public DarkHoldProj darkHoldProj;
        public Rakuhouha(Weapon weapon) : base(weapon.type == (int)RakuhouhaType.CFlasher || weapon.type == (int)RakuhouhaType.DarkHold ? "cflasher" : "rakuhouha", "", "", "")
        {
            this.weapon = weapon;
            invincible = true;
        }

        public override void update()
        {
            base.update();
            bool isCFlasher = type == RakuhouhaType.CFlasher;
            bool isRakuhouha = type == RakuhouhaType.Rakuhouha;
            bool isShinMessenkou = type == RakuhouhaType.ShinMessenkou;
            bool isDarkHold = type == RakuhouhaType.DarkHold;
            // isDarkHold = true;

            float x = character.pos.x;
            float y = character.pos.y;
            if (character.frameIndex > 7 && !fired)
            {
                fired = true;

                if (isShinMessenkou)
                {
                    new ShinMessenkouProj(weapon, new Point(x - shinMessenkouWidth, y), character.xDir, player, player.getNextActorNetId(), rpc: true);
                    new ShinMessenkouProj(weapon, new Point(x + shinMessenkouWidth, y), character.xDir, player, player.getNextActorNetId(), rpc: true);
                }
                else if (isDarkHold)
                {
                    darkHoldProj = new DarkHoldProj(weapon, new Point(x, y), character.xDir, player, player.getNextActorNetId(), rpc: true);
                }
                else
                {
                    new RakuhouhaProj(weapon, new Point(x, y), isCFlasher, -1, 0, player, player.getNextActorNetId(), 180, rpc: true);
                    new RakuhouhaProj(weapon, new Point(x, y), isCFlasher, -0.92f, -0.38f, player, player.getNextActorNetId(), 135, rpc: true);
                    new RakuhouhaProj(weapon, new Point(x, y), isCFlasher, -0.707f, -0.707f, player, player.getNextActorNetId(), 135, rpc: true);
                    new RakuhouhaProj(weapon, new Point(x, y), isCFlasher, -0.38f, -0.92f, player, player.getNextActorNetId(), 135, rpc: true);
                    new RakuhouhaProj(weapon, new Point(x, y), isCFlasher, 0, -1, player, player.getNextActorNetId(), 90, rpc: true);
                    new RakuhouhaProj(weapon, new Point(x, y), isCFlasher, 0.92f, -0.38f, player, player.getNextActorNetId(), 45, rpc: true);
                    new RakuhouhaProj(weapon, new Point(x, y), isCFlasher, 0.707f, -0.707f, player, player.getNextActorNetId(), 45, rpc: true);
                    new RakuhouhaProj(weapon, new Point(x, y), isCFlasher, 0.38f, -0.92f, player, player.getNextActorNetId(), 45, rpc: true);
                    new RakuhouhaProj(weapon, new Point(x, y), isCFlasher, 1, 0, player, player.getNextActorNetId(), 0, rpc: true);
                }

                if (!isCFlasher && !isDarkHold)
                {
                    character.shakeCamera(sendRpc: true);
                    character.playSound("rakuhouha", sendRpc: true);
                }
                else
                {
                    character.playSound("cflasher", sendRpc: true);
                }
            }
            
            if (!fired2 && isShinMessenkou && character.frameIndex > 11)
            {
                fired2 = true;
                new ShinMessenkouProj(weapon, new Point(x - shinMessenkouWidth * 2, y), character.xDir, player, player.getNextActorNetId(), rpc: true);
                new ShinMessenkouProj(weapon, new Point(x + shinMessenkouWidth * 2, y), character.xDir, player, player.getNextActorNetId(), rpc: true);
            }

            if (!fired3 && isShinMessenkou && character.frameIndex > 14)
            {
                fired3 = true;
                new ShinMessenkouProj(weapon, new Point(x - shinMessenkouWidth * 3, y), character.xDir, player, player.getNextActorNetId(), rpc: true);
                new ShinMessenkouProj(weapon, new Point(x + shinMessenkouWidth * 3, y), character.xDir, player, player.getNextActorNetId(), rpc: true);
            }

            if (character.isAnimOver())
            {
                character.changeState(new Idle());
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
        }

        public override void onExit(CharState newState)
        {
            weapon.shootTime = weapon.rateOfFire;
            base.onExit(newState);
        }
    }

    public class RakuhouhaProj : Projectile
    {
        bool isCFlasher;
        public RakuhouhaProj(Weapon weapon, Point pos, bool isCFlasher, float xVel, float yVel, Player player, ushort netProjId, int angle, bool rpc = false) :
            base(weapon, pos, xVel >= 0 ? 1 : -1, 300, 4, player, isCFlasher ? "cflasher" : "rakuhouha", Global.defFlinch, 1f, netProjId, player.ownedByLocalPlayer)
        {
            this.isCFlasher = isCFlasher;

            if (angle == 45)
            {
                var sprite = isCFlasher ? "cflasher_diag" : "rakuhouha_diag";
                changeSprite(sprite, false);
            }
            else if (angle == 90)
            {
                var sprite = isCFlasher ? "cflasher_up" : "rakuhouha_up";
                changeSprite(sprite, false);
            }
            else if (angle == 135)
            {
                xDir = -1;
                var sprite = isCFlasher ? "cflasher_diag" : "rakuhouha_diag";
                changeSprite(sprite, false);
            }
            else if (angle == 180)
            {
                xDir = -1;
            }

            if (!isCFlasher)
            {
                fadeSprite = "rakuhouha_fade";
            }
            else
            {
                damager.damage = 2;
                damager.hitCooldown = 0.5f;
                damager.flinch = 0;
                destroyOnHit = false;
            }

            reflectable = true;
            projId = (int)ProjIds.Rakuhouha;
            if (isCFlasher) projId = (int)ProjIds.CFlasher;
            vel.x = xVel * 300;
            vel.y = yVel * 300;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (time > 0.5)
            {
                destroySelf(fadeSprite);
            }
        }
    }

    public class ShinMessenkouProj : Projectile
    {
        int state = 0;
        public ShinMessenkouProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 4, player, "shinmessenkou_start", Global.defFlinch, 1f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 0.6f;
            destroyOnHit = false;
            projId = (int)ProjIds.ShinMessenkou;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (state == 0 && isAnimOver())
            {
                state = 1;
                changeSprite("shinmessenkou_proj", true);
                vel.y = -300;
            }
        }
    }

    public class Rekkoha : CharState
    {
        bool fired1 = false;
        bool fired2 = false;
        bool fired3 = false;
        bool fired4 = false;
        bool fired5 = false;
        bool sound;
        int loop;
        public RekkohaEffect effect;
        public Weapon weapon;
        public Rekkoha(Weapon weapon) : base("rekkoha", "", "", "")
        {
            this.weapon = weapon;
            invincible = true;
        }

        public override void update()
        {
            base.update();

            float topScreenY = Global.level.getTopScreenY(character.pos.y);

            if (character.frameIndex == 13 && loop < 15)
            {
                character.frameIndex = 10;
                loop++;
            }

            if (stateTime >= 0.15f && !sound)
            {
                sound = true;
                character.playSound("rekkoha", sendRpc: true);
            }

            if (stateTime > 0.4f && !fired1)
            {
                fired1 = true;
                new RekkohaProj(weapon, new Point(character.pos.x, topScreenY), player, player.getNextActorNetId(), rpc: true);
            }
            if (stateTime > 0.6f && !fired2)
            {
                fired2 = true;
                new RekkohaProj(weapon, new Point(character.pos.x - 35, topScreenY), player, player.getNextActorNetId(), rpc: true);
                new RekkohaProj(weapon, new Point(character.pos.x + 35, topScreenY), player, player.getNextActorNetId(), rpc: true);
            }
            if (stateTime > 0.8f && !fired3)
            {
                fired3 = true;
                new RekkohaProj(weapon, new Point(character.pos.x - 70, topScreenY), player, player.getNextActorNetId(), rpc: true);
                new RekkohaProj(weapon, new Point(character.pos.x + 70, topScreenY), player, player.getNextActorNetId(), rpc: true);
            }
            if (stateTime > 1f && !fired4)
            {
                fired4 = true;
                new RekkohaProj(weapon, new Point(character.pos.x - 110, topScreenY), player, player.getNextActorNetId(), rpc: true);
                new RekkohaProj(weapon, new Point(character.pos.x + 110, topScreenY), player, player.getNextActorNetId(), rpc: true);
            }
            if (stateTime > 1.2f && !fired5)
            {
                fired5 = true;
                new RekkohaProj(weapon, new Point(character.pos.x - 150, topScreenY), player, player.getNextActorNetId(), rpc: true);
                new RekkohaProj(weapon, new Point(character.pos.x + 150, topScreenY), player, player.getNextActorNetId(), rpc: true);
            }

            if (character.isAnimOver())
            {
                character.changeState(new Idle());
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            if (player.isMainPlayer)
            {
                effect = new RekkohaEffect();
            }
        }

        public override void onExit(CharState newState)
        {
            weapon.shootTime = weapon.rateOfFire;
            base.onExit(newState);
        }
    }

    public class RekkohaProj : Projectile
    {
        float len = 0;
        public RekkohaProj(Weapon weapon, Point pos, Player player, ushort netProjId, bool rpc = false) : base(weapon, pos, 1, 0, 3, player, "rekkoha_proj", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.Rekkoha;
            vel.y = 400;
            maxTime = 1f;
            destroyOnHit = false;
            shouldShieldBlock = false;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            len += Global.spf * 200;
            if (len > 230) len = 230;

            var newRect = new Rect(0, 0, 16, 63 + len);
            globalCollider = new Collider(newRect.getPoints(), true, this, false, false, 0, new Point(0, 0));
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            int intLen = (int)len / 23;
            int i;
            for (i = 0; i < intLen; i++)
            {
                Global.sprites["rekkoha_proj_mid"].draw(sprite.frameIndex, pos.x + x, pos.y + y - 63 - (i * 23), 1, 1, null, alpha, 1, 1, zIndex);
            }
            Global.sprites["rekkoha_proj_top"].draw(sprite.frameIndex, pos.x + x, pos.y + y - 63 - (i * 23), 1, 1, null, alpha, 1, 1, zIndex);
        }
    }


    public class RekkohaEffect : Effect
    {
        public RekkohaEffect() : base(new Point(Global.level.camX, Global.level.camY))
        {
        }

        public override void update()
        {
            base.update();
            pos.x = Global.level.camX;
            pos.y = Global.level.camY;

            if (effectTime > 2)
            {
                destroySelf();
            }
        }

        public override void render(float offsetX, float offsetY)
        {
            if (Global.level.server.fixedCamera) return;

            offsetX += pos.x;
            offsetY += pos.y;
            if (effectTime < 0.15f)
            {
                byte alpha = (byte)(255 * (effectTime * 4));
                DrawWrappers.DrawRect(offsetX, offsetY, offsetX + Global.screenW, offsetY + Global.screenH, true, new Color(0, 0, 0, alpha), 0, ZIndex.Background, true);
                return;
            }
            else if (effectTime < 0.2f)
            {
                DrawWrappers.DrawRect(offsetX, offsetY, offsetX + Global.screenW, offsetY + Global.screenH, true, new Color(255, 255, 255), 0, ZIndex.Background, true);
                return;
            }

            for (int i = 0; i < 38; i++)
            {
                float offY = (effectTime * 448) * (i % 2 == 0 ? 1 : -1);
                while (offY > 596) offY -= 596;
                while (offY < -596) offY += 596;

                int index = i + (int)(effectTime * 20);

                Global.sprites["rekkoha_effect_strip"].draw(index % 3, offsetX + i * 8, offsetY + offY - 596, 1, 1, null, 1, 1, 1, ZIndex.Background + 10);
                Global.sprites["rekkoha_effect_strip"].draw(index % 3, offsetX + i * 8, offsetY + offY, 1, 1, null, 1, 1, 1, ZIndex.Background + 10);
                Global.sprites["rekkoha_effect_strip"].draw(index % 3, offsetX + i * 8, offsetY + offY + 596, 1, 1, null, 1, 1, 1, ZIndex.Background + 10);
            }
        }
    }

    public class DarkHoldWeapon : Weapon
    {
        public DarkHoldWeapon() : base()
        {
            ammo = 0;
            rateOfFire = 1f;
            index = (int)WeaponIds.DarkHold;
            type = (int)RakuhouhaType.DarkHold;
            killFeedIndex = 175;
            weaponBarBaseIndex = 69;
            weaponBarIndex = 58;
            weaponSlotIndex = 122;
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            return 16;
        }
    }

    public class DarkHoldProj : Projectile
    {
        public float radius = 10;
        public float attackRadius { get { return radius + 15; } }
        public ShaderWrapper screenShader;
        public DarkHoldProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 6, player, "empty", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 0.5f;
            vel = new Point();
            projId = (int)ProjIds.DarkHold;
            setIndestructableProperties();

            Global.level.darkHoldProjs.Add(this);

            if (Options.main.enablePostProcessing)
            {
                screenShader = Helpers.cloneShaderSafe("darkHoldScreen");
                updateShader();
            }

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();

            updateShader();

            if (isRunByLocalPlayer())
            {
                foreach (var go in Global.level.getGameObjectArray())
                {
                    var actor = go as Actor;
                    var damagable = go as IDamagable;
                    if (actor == null) continue;
                    if (damagable == null) continue;
                    if (!damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, null)) continue;
                    if (!inRange(actor)) continue;

                    damager.applyDamage(damagable, false, weapon, this, projId, overrideDamage: 0, overrideFlinch: 0);
                }
            }
            radius += Global.spf * 400;
        }

        public bool inRange(Actor actor)
        {
            float dist = actor.getCenterPos().distanceTo(pos);
            if (dist > attackRadius) return false;
            return true;
        }

        public void updateShader()
        {
            if (screenShader != null)
            {
                var screenCoords = new Point(pos.x - Global.level.camX, pos.y - Global.level.camY);
                var normalizedCoords = new Point(screenCoords.x / Global.viewScreenW, 1 - screenCoords.y / Global.viewScreenH);
                var normalizedRadius = radius / 261f;

                screenShader.SetUniform("x", normalizedCoords.x);
                screenShader.SetUniform("y", normalizedCoords.y);
                if (Global.viewSize == 2) screenShader.SetUniform("r", normalizedRadius * 0.5f);
                else screenShader.SetUniform("r", normalizedRadius);
            }
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            if (screenShader == null)
            {
                var col = new Color(255, 251, 239, (byte)(164 - 164 * (time / maxTime)));
                var col2 = new Color(255, 219, 74, (byte)(224 - 224 * (time / maxTime)));
                DrawWrappers.DrawCircle(pos.x + x, pos.y + y, radius, true, col, 1, zIndex + 1, true);
                DrawWrappers.DrawCircle(pos.x + x, pos.y + y, radius, false, col2, 3, zIndex + 1, true, col2);
            }
        }

        public override void onDestroy()
        {
            base.onDestroy();
            Global.level.darkHoldProjs.Remove(this);
        }
    }

    public class DarkHoldState : CharState
    {
        public float stunTime = totalStunTime;
        public const float totalStunTime = 5;
        int frameIndex;
        public bool shouldDrawAxlArm;
        public float lastArmAngle;
        public DarkHoldState(Character character) : base(character?.sprite?.name ?? "grabbed")
        {
            immuneToWind = true;

            this.frameIndex = character.frameIndex;
            this.shouldDrawAxlArm = character.shouldDrawArm();
            this.lastArmAngle = character.netArmAngle;
        }

        public override void update()
        {
            base.update();
            character.stopMoving();
            stunTime -= player.mashValue();
            if (stunTime <= 0)
            {
                stunTime = 0;
                character.changeToIdleOrFall();
            }
        }

        public override bool canEnter(Character character)
        {
            if (!base.canEnter(character)) return false;
            if (character.darkHoldInvulnTime > 0) return false;
            if (character.isInvulnerable()) return false;
            if (character.isVaccinated()) return false;
            return !character.isCCImmune() && !character.charState.invincible;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.useGravity = false;
            character.frameSpeed = 0;
            character.frameIndex = frameIndex;
            character.stopMoving();
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.useGravity = true;
            character.frameSpeed = 1;
            character.darkHoldInvulnTime = 1;
        }
    }
}