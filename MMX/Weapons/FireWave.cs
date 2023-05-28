using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class FireWave : Weapon
    {
        public FireWave() : base()
        {
            index = (int)WeaponIds.FireWave;
            killFeedIndex = 4;
            weaponBarBaseIndex = 4;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 4;
            weaknessIndex = 5;
            shootSounds = new List<string>() { "fireWave", "fireWave", "fireWave", "fireWave" };
            rateOfFire = 0.06f;
            isStream = true;
            switchCooldown = 0.25f;
        }

        public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId)
        {
            if (player.character != null && player.character.isUnderwater()) return;
            if (chargeLevel != 3)
            {
                var proj = new FireWaveProj(this, pos, xDir, player, netProjId);
                proj.vel.inc(player.character.vel.times(-0.5f));
            }
            else
            {
                var proj = new FireWaveProjChargedStart(this, pos, xDir, player, netProjId);
            }
        }
    }

    public class FireWaveProj : Projectile
    {
        public FireWaveProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId) : base(weapon, pos, xDir, 400, 1, player, "fire_wave", 0, 0.2f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.FireWave;
            fadeSprite = "fire_wave_fade";
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            if (time > 0.1)
            {
                destroySelf(fadeSprite);
            }
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            var character = damagable as Character;
            character?.unfreezeIfFrozen();
        }
    }

    public class FireWaveProjChargedStart : Projectile
    {
        public FireWaveProjChargedStart(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId) : base(weapon, pos, xDir, 150, 2, player, "fire_wave_charge", 0, 0.222f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.FireWaveChargedStart;
            collider.wallOnly = true;
            destroyOnHit = false;
            shouldShieldBlock = false;
        }

        public override void update()
        {
            base.update();

            if (!ownedByLocalPlayer) return;

            if (isUnderwater())
            {
                destroySelf();
                return;
            }
            incPos(new Point(0, Global.spf * 100));
            if (grounded)
            {
                destroySelf();
                new FireWaveProjCharged(weapon, pos, xDir, damager.owner, 0, Global.level.mainPlayer.getNextActorNetId(), 0, rpc: true);
                playSound("fireWave");
            }
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            base.onHitDamagable(damagable);
            var character = damagable as Character;
            character?.unfreezeIfFrozen();
        }

        public void putOutFire()
        {
            base.destroySelf(null, null, false, true);
        }
    }

    public class FireWaveProjCharged : Projectile
    {
        public Sprite spriteMid;
        public Sprite spriteTop;
        public float riseY = 0;
        public float parentTime = 0;
        public FireWaveProjCharged child;
        public bool reversedOnce;
        public int timesReversed;
        float soundCooldown;
        public FireWaveProjCharged(Weapon weapon, Point pos, int xDir, Player player, float parentTime, ushort netProjId, int timesReversed, bool rpc = false) : base(weapon, pos, xDir, 0, 1, player, "fire_wave_charge", 0, 0.33f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.FireWaveCharged;
            spriteMid = Global.sprites["fire_wave_charge"].clone();
            spriteMid.visible = false;
            spriteTop = Global.sprites["fire_wave_charge"].clone();
            spriteTop.visible = false;
            useGravity = true;
            collider.wallOnly = true;
            frameSpeed = 0;
            this.parentTime = parentTime;
            destroyOnHit = false;
            shouldShieldBlock = false;
            this.timesReversed = timesReversed;
            new Anim(this.pos.clone(), "fire_wave_charge_flash", 1, null, true);

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void render(float x, float y)
        {
            sprite.draw(frameIndex, pos.x + x, pos.y + y - riseY, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
            spriteMid.draw((int)MathF.Round(frameIndex + (sprite.frames.Count / 3)) % sprite.frames.Count, pos.x + x, pos.y + y - 6 - riseY, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
            spriteTop.draw((int)MathF.Round(frameIndex + (sprite.frames.Count / 2)) % sprite.frames.Count, pos.x + x, pos.y + y - 12 - riseY, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
        }

        public override void update()
        {
            base.update();
            if (isUnderwater())
            {
                destroySelf();
                return;
            }
            if (soundCooldown > 0)
            {
                soundCooldown = Helpers.clampMin0(soundCooldown - Global.spf);
            }
            frameSpeed = 1;
            if (time >= 0.16f)
            {
                spriteTop.visible = true;
                spriteMid.visible = true;
                riseY += (Global.spf * 75);
            }
            if (time >= 0.48f)
            {
                destroySelf();
            }
            if (time > 0.2f && child == null && parentTime < 3)
            {
                if (soundCooldown == 0)
                {
                    playSound("fireWave");
                    soundCooldown = 0.25f;
                }

                if (ownedByLocalPlayer)
                {
                    var wall = Global.level.checkCollisionActor(this, 16 * xDir, -4);
                    var sign = 1;
                    if (wall != null && wall.gameObject is Wall && wall.hitData.normal != null && !wall.hitData.normal.Value.isAngled())
                    {
                        sign = -1;
                        timesReversed++;
                    }
                    else
                    {
                    }

                    if (timesReversed > 0)
                    {
                        destroySelf();
                        return;
                    }

                    child = new FireWaveProjCharged(weapon, pos.addxy(16 * xDir, 0), xDir * sign, damager.owner, time + parentTime, Global.level.mainPlayer.getNextActorNetId(), timesReversed, rpc: true);
                }
            }
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            base.onHitDamagable(damagable);
            var character = damagable as Character;
            character?.unfreezeIfFrozen();
        }

        public override void onDestroy()
        {
            var newPos = pos.addxy(0, -24 - riseY);
            new Anim(newPos, "fire_wave_charge_fade", 1, null, true);
        }

        public void putOutFire()
        {
            base.destroySelf(null, null, false, true);
        }
    }
}
