using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class NovaStrike : Weapon
    {
        public const float ammoUsage = 16;
        public NovaStrike(Player player) : base()
        {
            damager = new Damager(player, 4, Global.defFlinch, 0.5f);
            rateOfFire = 1.5f;
            index = (int)WeaponIds.NovaStrike;
            weaponBarBaseIndex = 42;
            weaponBarIndex = 36;
            weaponSlotIndex = 95;
            killFeedIndex = 104;
            ammo = 32;
        }

        public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId)
        {
            if (player.character.ownedByLocalPlayer)
            {
                player.character.changeState(new NovaStrikeState(), true);
            }
        }

        public override float getAmmoUsage(int chargeLevel)
        {
            if (Global.level?.isHyper1v1() == true)
            {
                return 0;
            }
            return ammoUsage;
        }

        public override bool canShoot(int chargeLevel, Player player)
        {
            return player.character?.flag == null && ammo >= (player.hasChip(3) ? ammoUsage / 2 : ammoUsage);
        }
    }

    public class NovaStrikeState : CharState
    {
        int leftOrRight;
        int upOrDown;
        public NovaStrikeState() : base("nova_strike_start", "", "", "")
        {
            superArmor = true;
            immuneToWind = true;
            invincible = true;
        }

        public override void update()
        {
            base.update();

            if (sprite == "nova_strike_start")
            {
                if (character.isAnimOver())
                {
                    if (player.input.isHeld(Control.Up, player))
                    {
                        upOrDown = -1;
                        sprite = "nova_strike_up";
                    }
                    else if (player.input.isHeld(Control.Down, player))
                    {
                        upOrDown = 1;
                        sprite = "nova_strike_down";
                    }
                    else
                    {
                        leftOrRight = 1;
                        sprite = "nova_strike";
                    }
                    character.playSound("speedBurnerCharged", sendRpc: true);
                    character.changeSpriteFromName(sprite, true);
                }
                return;
            }
            
            if (!character.tryMove(new Point(character.xDir * 350 * leftOrRight, 350 * upOrDown), out _))
            {
                player.character.changeState(new Idle(), true);
                return;
            }

            if (character.flag != null)
            {
                player.character.changeState(new Idle(), true);
                return;
            }
            if (stateTime > 0.6f)
            {
                player.character.changeState(new Idle(), true);
                return;
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            player.character.useGravity = false;
            player.character.vel.y = 0;
            player.character.stopCharge();
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            player.character.yDir = 1;
            player.character.useGravity = true;
        }
    }
}
