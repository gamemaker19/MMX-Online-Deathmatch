using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class XSaber : Weapon
    {
        public XSaber(Player player) : base()
        {
            damager = new Damager(player, 4, Global.defFlinch, 0.25f);
            index = (int)WeaponIds.XSaber;
            weaponBarBaseIndex = 21;
            weaponBarIndex = weaponBarBaseIndex;
            killFeedIndex = 66;
        }
    }

    public class XSaberProj : Projectile
    {
        public XSaberProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 300, 4, player, "zsaber_shot", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            //this.fadeSprite = "zsaber_shot_fade";
            reflectable = true;
            projId = (int)ProjIds.XSaberProj;
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

    public class XSaberState : CharState
    {
        bool fired;
        bool grounded;
        public XSaberState(bool grounded) : base(grounded ? "beam_saber" : "beam_saber_air", "", "", "")
        {
            this.grounded = grounded;
            landSprite = "beam_saber";
        }

        public override void update()
        {
            base.update();
            if (!character.grounded)
            {
                airCode();
                if (player.input.isHeld(Control.Dash, player))
                {
                    character.isDashing = true;
                }
            }

            if (character.frameIndex >= 6 && !fired)
            {
                fired = true;
                character.playSound("saberShot");
                new XSaberProj(new XSaber(player), character.pos.addxy(20 * character.xDir, -20), character.xDir, player, player.getNextActorNetId(), rpc: true);
            }

            if (character.isAnimOver())
            {
                if (character.grounded) character.changeState(new Idle(), true);
                else character.changeState(new Fall(), true);
            }
        }
    }

    public class X6SaberState : CharState
    {
        bool fired;
        bool grounded;
        public X6SaberState(bool grounded) : base(grounded ? "beam_saber2" : "beam_saber_air2", "", "", "")
        {
            this.grounded = grounded;
            landSprite = "beam_saber2";
        }

        public override void update()
        {
            base.update();
            if (!character.grounded)
            {
                airCode();
                if (player.input.isHeld(Control.Dash, player))
                {
                    character.isDashing = true;
                }
            }

            int frameSound = 5;
            if (character.frameIndex >= frameSound && !fired)
            {
                fired = true;
                character.playSound("saberShot");
                //new XSaberProj(new XSaber(player), character.pos.addxy(30 * character.xDir, -29), character.xDir, player, player.getNextActorNetId(), rpc: true);
            }

            if (player.character.canCharge() && player.input.isHeld(Control.Shoot, player))
            {
                player.character.increaseCharge();
            }

            if (character.isAnimOver())
            {
                if (character.grounded) character.changeState(new Idle(), true);
                else character.changeState(new Fall(), true);
            }
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
        }
    }
}
