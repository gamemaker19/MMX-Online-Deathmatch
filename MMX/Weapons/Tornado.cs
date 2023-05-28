using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class Tornado : Weapon
    {
        public Tornado() : base()
        {
            index = (int)WeaponIds.Tornado;
            killFeedIndex = 5;
            weaponBarBaseIndex = 5;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 5;
            weaknessIndex = 2;
            shootSounds = new List<string>() { "tornado", "tornado", "tornado", "buster3" };
            rateOfFire = 2f;
            switchCooldown = 0.5f;
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
                new TornadoProj(this, pos, xDir, false, player, netProjId);
            }
            else
            {
                new TornadoProjCharged(this, pos, xDir, player, netProjId);
            }
        }
    }


    public class TornadoProj : Projectile
    {
        public Sprite spriteStart;
        public List<Sprite> spriteMids = new List<Sprite>();
        public Sprite spriteEnd;
        public float length = 1;
        public float maxSpeed = 400;
        public float tornadoTime;
        public float blowModifier = 0.25f;

        public TornadoProj(Weapon weapon, Point pos, int xDir, bool isStormE, Player player, ushort netProjId, bool sendRpc = false) : 
            base(weapon, pos, xDir, 400, 1, player, "tornado_mid", 0, 0.25f, netProjId, player.ownedByLocalPlayer)
        {
            projId = isStormE ? (int)ProjIds.StormETornado : (int)ProjIds.Tornado;
            if (isStormE)
            {
                blowModifier = 1;
                damager.hitCooldown = 0.5f;
            }
            maxTime = 2;
            sprite.visible = false;
            spriteStart = Global.sprites["tornado_start"].clone();
            for (var i = 0; i < 6; i++)
            {
                var midSprite = Global.sprites["tornado_mid"].clone();
                midSprite.visible = false;
                spriteMids.Add(midSprite);
            }
            spriteEnd = Global.sprites["tornado_end"].clone();
            vel.x = 0;
            destroyOnHit = false;
            shouldShieldBlock = false;

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void render(float x, float y)
        {
            spriteStart.draw(frameIndex, pos.x + x, pos.y + y, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
            int spriteMidLen = 16;
            int i = 0;
            for (i = 0; i < length; i++)
            {
                spriteMids[i].visible = true;
                spriteMids[i].draw(frameIndex, pos.x + x + (i * xDir * spriteMidLen), pos.y + y, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
            }
            spriteEnd.draw(frameIndex, pos.x + x + (i * xDir * spriteMidLen), pos.y + y, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
            if (Global.showHitboxes && collider != null)
            {
                //Helpers.drawPolygon(Global.level.uiCtx, this.collider.shape.clone(x, y), true, "blue", "", 0, 0.5);
                //Helpers.drawCircle(Global.level.ctx, this.pos.x + x, this.pos.y + y, 1, "red");
            }
        }

        public override void update()
        {
            base.update();

            var topX = 0;
            var topY = 0;

            var spriteMidLen = 16;
            var spriteEndLen = 14;

            var botX = (length * spriteMidLen) + spriteEndLen;
            var botY = 32;

            var rect = new Rect(topX, topY, botX, botY);
            globalCollider = new Collider(rect.getPoints(), true, this, false, false, 0, new Point(0, 0));

            tornadoTime += Global.spf;
            if (tornadoTime > 0.2f)
            {
                if (length < 6)
                {
                    length++;
                }
                else
                {
                    vel.x = maxSpeed * xDir;
                }
                tornadoTime = 0;
            }
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            var character = damagable as Character;
            if (character == null) return;
            if (character.charState.invincible) return;
            if (character.isImmuneToKnockback()) return;

            //character.damageHistory.Add(new DamageEvent(damager.owner, weapon.killFeedIndex, true, Global.frameCount));
            if (character.isClimbingLadder())
            {
                character.setFall();
            }
            else if (!character.pushedByTornadoInFrame)
            {
                float modifier = 1;
                if (character.grounded) modifier = 0.5f;
                if (character.charState is Crouch) modifier = 0.25f;
                character.move(new Point(maxSpeed * 0.9f * xDir * modifier * blowModifier, 0));
                character.pushedByTornadoInFrame = true;
            }
        }
    }

    public class TornadoProjCharged : Projectile
    {
        public Sprite spriteStart;
        public List<Sprite> bodySprites = new List<Sprite>();
        public int length = 1;
        public float groundY = 0;
        public const int maxLength = 10;
        public float maxSpeed = 100;
        public const float pieceHeight = 32;
        public float growTime = 0;
        public float maxLengthTime = 0;

        public TornadoProjCharged(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId) : base(weapon, pos, xDir, 0, 2, player, "tornado_charge", Global.defFlinch, 0.33f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.TornadoCharged;
            sprite.visible = false;
            spriteStart = Global.sprites["tornado_charge"].clone();
            for (var i = 0; i < maxLength; i++)
            {
                var midSprite = Global.sprites["tornado_charge"].clone();
                midSprite.visible = false;
                bodySprites.Add(midSprite);
            }
            //this.ground();
            destroyOnHit = false;
            shouldShieldBlock = false;
        }

        public override void render(float x, float y)
        {
            spriteStart.draw(frameIndex, pos.x + x, pos.y + y, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
            for (int i = 0; i < length && i < bodySprites.Count; i++)
            {
                bodySprites[i].visible = true;
                bodySprites[i].draw(frameIndex, pos.x + x, pos.y + y - (i * pieceHeight), xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
            }
            if (Global.showHitboxes && collider != null)
            {
                //Helpers.drawPolygon(Global.level.uiCtx, this.collider.shape.clone(x, y), true, "blue", "", 0, 0.5);
            }
        }

        public void ground()
        {
            var ground = Global.level.raycast(pos.addxy(0, -10), pos.addxy(0, Global.level.height), new List<Type> { typeof(Wall) });
            if (ground != null)
            {
                pos.y = ((Point)ground.hitData.hitPoint).y;
            }
        }

        public override void update()
        {
            base.update();

            var botY = pieceHeight + (length * pieceHeight);

            var rect = new Rect(0, 0, 64, botY);
            globalCollider = new Collider(rect.getPoints(), true, this, false, false, 0, new Point(0, 0));

            growTime += Global.spf;
            if (growTime > 0.01)
            {
                if (length < maxLength)
                {
                    length++;
                    incPos(new Point(0, pieceHeight / 2));
                }
                else
                {
                    //this.vel.x = this.maxSpeed * this.xDir;
                }
                growTime = 0;
            }

            if (length >= maxLength)
            {
                maxLengthTime += Global.spf;
                if (maxLengthTime > 1)
                {
                    destroySelf();
                }
            }

        }

        public override void onHitDamagable(IDamagable damagable)
        {
            /*
            character.move(new Point(this.speed * 0.9 * this.xDir, 0));
            if(character.isClimbingLadder()) {
              character.setFall();
            }
            */
        }
    }
}
