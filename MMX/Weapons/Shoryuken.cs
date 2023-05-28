using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class ShoryukenWeapon : Weapon
    {
        public ShoryukenWeapon(Player player) : base()
        {
            damager = new Damager(player, Damager.ohkoDamage, Global.defFlinch, 0.5f);
            ammo = 0;
            index = (int)WeaponIds.Shoryuken;
            weaponBarBaseIndex = 32;
            weaponBarIndex = weaponBarBaseIndex;
            killFeedIndex = 32;
        }
    }

    public class Shoryuken : CharState
    {
        bool jumpedYet;
        float timeInWall;
        bool isUnderwater;
        Anim anim;
        float projTime;

        public Shoryuken(bool isUnderwater) : base("shoryuken", "", "")
        {
            this.isUnderwater = isUnderwater;
            superArmor = true;
        }

        public override void update()
        {
            base.update();

            if (character.isUnderwater() && anim != null)
            {
                anim.visible = false;
            }

            if (character.sprite.frameIndex >= 2 && !jumpedYet)
            {
                jumpedYet = true;
                character.dashedInAir++;
                character.vel.y = -character.getJumpPower() * 1.5f;
                if (!isUnderwater)
                {
                    character.playSound("shoryuken", sendRpc: true);
                }
            }
            if (character.sprite.frameIndex == 2 && character.currentFrame.POIs.Count > 0)
            {
                character.move(new Point(character.xDir * 150, 0));
                Point poi = character.currentFrame.POIs[0];
                Point firePos = character.pos.addxy(poi.x * character.xDir, poi.y);
                if (anim == null)
                {
                    anim = new Anim(firePos, "shoryuken", character.xDir, player.getNextActorNetId(), false, sendRpc: true);
                }
                else
                {
                    anim.changePos(firePos);
                }
            }
            else if (character.sprite.frameIndex > 2)
            {
                if (anim != null)
                {
                    anim.destroySelf();
                    anim = null;
                }
            }

            if (!isUnderwater)
            {
                projTime += Global.spf;
                if (projTime > 0.06f)
                {
                    projTime = 0;
                    var anim = new Anim(character.getCenterPos(), "shoryuken_fade", character.xDir, player.getNextActorNetId(), true, sendRpc: true);
                    anim.vel = new Point(-character.xDir * 50, 25);
                }
            }

            var wallAbove = Global.level.checkCollisionActor(character, 0, -10);
            if (wallAbove != null && wallAbove.gameObject is Wall)
            {
                timeInWall += Global.spf;
                if (timeInWall > 0.1f)
                {
                    character.changeState(new Fall());
                    return;
                }
            }

            if (character.isAnimOver())
            {
                character.changeState(new Fall());
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
        }

        public override void onExit(CharState newState)
        {
            if (anim != null)
            {
                anim.destroySelf();
                anim = null;
            }
            base.onExit(newState);
            character.shoryukenCooldownTime = character.maxShoryukenCooldownTime;
        }
    }
}
