using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class HadoukenWeapon : Weapon
    {
        public HadoukenWeapon(Player player) : base()
        {
            damager = new Damager(player, Damager.ohkoDamage, Global.defFlinch, 0.5f);
            ammo = 0;
            index = (int)WeaponIds.Hadouken;
            weaponBarBaseIndex = 19;
            weaponBarIndex = weaponBarBaseIndex;
            killFeedIndex = 14;
        }
    }

    public class HadoukenProj : Projectile
    {
        public HadoukenProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 250, Damager.ohkoDamage, player, "hadouken", Global.defFlinch, 0.15f, netProjId, player.ownedByLocalPlayer)
        {
            fadeSprite = "hadouken_fade";
            reflectable = true;
            destroyOnHit = true;
            projId = (int)ProjIds.Hadouken;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (time > 0.4)
            {
                destroySelf(fadeSprite);
            }
        }
    }

    public class Hadouken : CharState
    {
        bool fired = false;
        public Hadouken() : base("hadouken", "", "", "")
        {
            superArmor = true;
        }

        public override void update()
        {
            base.update();

            if (character.frameIndex >= 2 && !fired)
            {
                fired = true;

                Weapon weapon = new HadoukenWeapon(player);
                float x = character.pos.x;
                float y = character.pos.y;

                new HadoukenProj(weapon, new Point(x + (20 * character.xDir), y - 20), character.xDir, player, player.getNextActorNetId(), rpc: true);

                character.playSound("hadouken", sendRpc: true);
            }

            if (character.isAnimOver())
            {
                character.changeState(new Idle());
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.stopCharge();
        }

        public override void onExit(CharState newState)
        {
            character.hadoukenCooldownTime = character.maxHadoukenCooldownTime;
            base.onExit(newState);
        }
    }
}
