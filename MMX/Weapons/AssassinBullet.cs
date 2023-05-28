using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class AssassinBullet : AxlWeapon
    {
        public AssassinBullet() : base(0)
        {
            sprite = "axl_arm_pistol";
            flashSprite = "axl_pistol_flash_charged";
            chargedFlashSprite = "axl_pistol_flash_charged";
            shootSounds = new List<string>() { "assassinate", "assassinate", "assassinate", "assassinate" };
            index = (int)WeaponIds.AssassinBullet;
            weaponBarBaseIndex = 28;
            weaponBarIndex = 28;
            weaponSlotIndex = 47;
            killFeedIndex = 61;
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            if (chargeLevel == 3) return 0;
            else return 0;
        }

        public override void axlGetProjectile(Weapon weapon, Point bulletPos, int xDir, Player player, float angle, IDamagable target, Character headshotTarget, Point cursorPos, int chargeLevel, ushort netId)
        {
            if (player.assassinHitPos == null)
            {
                player.assassinHitPos = player.character.getFirstHitPos(AssassinBulletProj.range);
            }
            var bullet = new AssassinBulletProj(weapon, bulletPos, player.assassinHitPos.hitPos, xDir, player, target, headshotTarget, netId);
            bullet.applyDamage(player.assassinHitPos.hitGos.ElementAtOrDefault(0), player.assassinHitPos.isHeadshot);

            if (player.ownedByLocalPlayer)
            {
                AssassinBulletTrailAnim trail = new AssassinBulletTrailAnim(bulletPos, bullet);
                RPC.axlShoot.sendRpc(player.id, bullet.projId, netId, bulletPos, xDir, angle);
            }
        }
    }

    public class AssassinBulletProj : Projectile
    {
        public Player player;
        public Character headshotChar;
        public IDamagable target;
        public Point destroyPos;
        public float distTraveled;

        int frames;
        float dist;
        float maxDist;
        Point hitPos;
        public const float range = 150;
        public AssassinBulletProj(Weapon weapon, Point pos, Point hitPos, int xDir, Player player, IDamagable target, Character headshotChar, ushort netProjId) :
            base(weapon, pos, xDir, 1000, 8, player, "assassin_bullet_proj", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            this.target = target;

            Point bulletDir = pos.directionToNorm(hitPos);

            vel.x = bulletDir.x * speed;
            vel.y = bulletDir.y * speed;
            this.player = player;
            this.headshotChar = headshotChar;

            fadeSprite = "axl_bullet_fade";
            projId = (int)ProjIds.AssassinBullet;

            if (player?.character?.isQuickAssassinate == true)
            {
                projId = (int)ProjIds.AssassinBulletQuick;
            }

            this.xDir = xDir;
            this.angle = bulletDir.angle;
            visible = false;
            reflectable = true;

            this.hitPos = hitPos;
            maxTime = float.MaxValue;
            maxDist = pos.distanceTo(hitPos);
        }

        public override void update()
        {
            base.update();

            angle = deltaPos.angle;
            if (!ownedByLocalPlayer) return;
            if (destroyed) return;

            // Zero reflect, rolling shield, ice shield
            frames++;
            if (frames > 1)
            {
                visible = true;
            }

            dist += deltaPos.magnitude;
            if (pos.distanceTo(hitPos) < speed * Global.spf || dist >= maxDist)
            {
                changePos(hitPos);
                destroySelf();
                return;
            }
        }

        public void applyDamage(IDamagable damagable, bool weakness)
        {
            float overrideDamage = weakness ? (damager.damage * Damager.headshotModifier) : damager.damage;
            if (weapon is AssassinBullet && weakness)
            {
                overrideDamage = Damager.ohkoDamage;
            }
            //DevConsole.log("Weakness: " + weakness.ToString() + ",bd:" + damager.damage.ToString() + ",");
            damager.applyDamage(damagable, false, weapon, this, projId, overrideDamage: overrideDamage);
            
            if (weakness)
            {
                //hitChar.damageTexts.Add(new DamageText("headshot!", 0));
                playSound("hurt");
            }
        }
    }

    public class Assassinate : CharState
    {
        public float time;
        bool fired;
        public Assassinate(bool isGrounded) : base(isGrounded ? "idle" : "fall", "shoot", "attack", "")
        {
            superArmor = true;
        }

        public override void update()
        {
            base.update();
            time += Global.spf;
            if (!Options.main.useMouseAim && Options.main.lockOnSound && player.assassinCursorPos != null)
            {
                player.axlCursorPos = player.assassinCursorPos.Value;
            }
            if (!fired)
            {
                fired = true;
                //player.character.axlCursorTarget = null;
                //player.character.axlHeadshotTarget = null;
                //player.character.updateAxlAim();
                (new AssassinBullet()).axlShoot(player, AxlBulletType.Assassin);
            }
            if (time > 0.5f)
            {
                character.changeState(new Idle(), true);
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.useGravity = false;
            character.vel = new Point();
            character.xDir = (character.pos.x > player.axlGenericCursorWorldPos.x ? -1 : 1);
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.useGravity = true;
        }
    }
}
