using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public enum ZeroAttackLoadoutType
    {
        ZSaber,
        KKnuckle,
        ZBuster,
    }

    public class ZSaber : Weapon
    {
        public ZSaber(Player player) : base()
        {
            damager = new Damager(player, 3, 0, 0.5f);
            index = (int)WeaponIds.ZSaber;
            weaponBarBaseIndex = 21;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 48;
            killFeedIndex = 9;
            type = (int)ZeroAttackLoadoutType.ZSaber;
            displayName = "Z-Saber";
            description = new string[] { "Zero's trusty beam saber." };
        }
    }

    public class ShippuugaWeapon : Weapon
    {
        public ShippuugaWeapon(Player player) : base()
        {
            damager = new Damager(player, 2, Global.defFlinch, 0.5f);
            index = (int)WeaponIds.Shippuuga;
            weaponBarBaseIndex = 21;
            killFeedIndex = 39;
        }
    }

    public class ZSaberProj : Projectile
    {
        public ZSaberProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 300, 3, player, "zsaber_shot", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            //this.fadeSprite = "zsaber_shot_fade";
            reflectable = true;
            projId = (int)ProjIds.ZSaberProj;
            if (player.character?.isBlackZero2() == true)
            {
                damager.damage = 4;
                genericShader = player.character.zeroPaletteShader;
            }
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

    public class KKnuckleWeapon : Weapon
    {
        public KKnuckleWeapon(Player player) : base()
        {
            damager = new Damager(player, 3, Global.defFlinch, 0.25f);
            index = (int)WeaponIds.KKnuckle;
            weaponBarBaseIndex = 21;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 120;
            killFeedIndex = 106;
            type = (int)ZeroAttackLoadoutType.KKnuckle;
            displayName = "K-Knuckle";
            description = new string[] { "Use your fists to teach foes a lesson." };
        }
    }


    public class ZeroSpinKickState : CharState
    {
        public float dashTime = 0;
        public float soundTime = 0;

        public ZeroSpinKickState() : base("spinkick")
        {
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.isDashing = true;
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.dashAttackCooldown = 0.5f;
        }

        public override void update()
        {
            base.update();
            groundCode();
            soundTime -= Global.spf;
            if (soundTime <= 0)
            {
                soundTime = 0.15f;
                character.playSound("spinkick", sendRpc: true);
            }

            if (
                (player.input.isPressed(Control.Left, player) && character.xDir == 1) || 
                (player.input.isPressed(Control.Right, player) && character.xDir == -1) ||
                player.input.isPressed(Control.Dash, player)
            )
            {
                changeToIdle();
                return;
            }

            dashTime += Global.spf;
            float modifier = 1;
            if (dashTime > 0.6 * modifier)
            {
                changeToIdle();
                return;
            }
            var move = new Point(0, 0);
            move.x = character.getRunSpeed() * character.getDashSpeed() * modifier * character.xDir;
            character.move(move);
        }
    }

}
