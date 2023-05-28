using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{    
    public class SigmaClawWeapon : Weapon
    {
        public SigmaClawWeapon() : base()
        {
            index = (int)WeaponIds.Sigma2Claw;
            killFeedIndex = 132;
        }
    }

    public class SigmaClawState : CharState
    {
        CharState prevCharState;
        float slideVel;
        bool isAir;
        public SigmaClawState(CharState prevCharState, bool isAir) : base(prevCharState.attackSprite, "", "", "")
        {
            this.prevCharState = prevCharState;
            this.isAir = isAir;
        }

        public override void update()
        {
            base.update();

            if (!character.grounded)
            {
                airCode();
                landSprite = "attack";
            }
            else if (isAir && character.grounded)
            {
                once = true;
            }

            if (MathF.Abs(slideVel) > 0)
            {
                character.move(new Point(slideVel, 0));
                slideVel -= Global.spf * 750 * character.xDir;
                if (MathF.Abs(slideVel) < Global.spf * 1000)
                {
                    slideVel = 0;
                }
            }

            /*
            if (!player.input.isHeld(Control.Shoot, player))
            {
                shootHeldContinuously = false;
            }
            if (shootHeldContinuously && character.grounded && character.frameIndex >= 4)
            {
                character.changeState(new Idle(), true);
                return;
            }
            */

            if (player.input.isPressed(Control.Shoot, player) && character.grounded && character.frameIndex >= 4 && sprite != "attack2" && !once)
            {
                once = true;
                sprite = "attack2";
                defaultSprite = sprite;
                character.saberCooldown = character.sigmaSaberMaxCooldown;
                character.changeSpriteFromName(sprite, true);
                character.playSound("sigma2slash", sendRpc: true);
                return;
            }

            if (character.isAnimOver())
            {
                if (character.grounded) character.changeState(new Idle(), true);
                else character.changeState(new Fall(), true);
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            if (oldState is Dash)
            {
                slideVel = character.xDir * character.getDashSpeed() * character.getRunSpeed();
            }
            character.playSound("sigma2slash", sendRpc: true);
        }
    }

    public class SigmaElectricBallWeapon : Weapon
    {
        public SigmaElectricBallWeapon() : base()
        {
            index = (int)WeaponIds.Sigma2Ball;
            killFeedIndex = 135;
        }
    }

    public class SigmaElectricBallProj : Projectile
    {
        public SigmaElectricBallProj(Weapon weapon, Point pos, float angle, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, 1, 0, 3, player, "sigma2_ball", 0, 0.2f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.Sigma2Ball;
            destroyOnHit = false;
            maxTime = 0.5f;

            this.vel = Point.createFromAngle(angle).times(200);

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }

    public class SigmaElectricBallState : CharState
    {
        bool fired;
        public SigmaElectricBallState(string transitionSprite = "") : base("shoot", "", "", transitionSprite)
        {
            enterSound = "sigma2shoot";
            invincible = true;
        }

        public override void update()
        {
            base.update();

            if (character.frameIndex > 0 && !fired)
            {
                fired = true;
                character.playSound("sigma2ball", sendRpc: true);
                var weapon = new SigmaElectricBallWeapon();
                Point pos = character.pos.addxy(0, -20);
                new SigmaElectricBallProj(weapon, pos, 0, player, player.getNextActorNetId(), rpc: true);
                new SigmaElectricBallProj(weapon, pos, -45, player, player.getNextActorNetId(), rpc: true);
                new SigmaElectricBallProj(weapon, pos, -90, player, player.getNextActorNetId(), rpc: true);
                new SigmaElectricBallProj(weapon, pos, -135, player, player.getNextActorNetId(), rpc: true);
                new SigmaElectricBallProj(weapon, pos, -180, player, player.getNextActorNetId(), rpc: true);
            }

            if (character.isAnimOver())
            {
                character.changeState(new Idle(), true);
            }
        }
    }

    public class SigmaElectricBall2Weapon : Weapon
    {
        public SigmaElectricBall2Weapon() : base()
        {
            index = (int)WeaponIds.Sigma2Ball2;
            killFeedIndex = 135;
        }
    }

    public class SigmaElectricBall2Proj : Projectile
    {
        public SigmaElectricBall2Proj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 300, 6, player, "sigma2_ball2", Global.defFlinch, 0.2f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.Sigma2Ball2;
            destroyOnHit = false;
            maxTime = 0.4f;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }

    public class SigmaElectricBall2State : CharState
    {
        bool fired;
        bool sound;
        public SigmaElectricBall2State(string transitionSprite = "") : base("shoot2", "", "", transitionSprite)
        {
            invincible = true;
        }

        public override void update()
        {
            base.update();

            character.turnToInput(player.input, player);

            if (!sound && character.frameIndex >= 13)
            {
                sound = true;
                character.playSound("sparkmSpark", sendRpc: true);
            }

            if (!fired && character.getFirstPOI() != null)
            {
                fired = true;
                new SigmaElectricBall2Proj(new SigmaElectricBall2Weapon(), character.getFirstPOI().Value, character.xDir, player, player.getNextActorNetId(), rpc: true);
            }

            if (character.isAnimOver())
            {
                character.changeState(new Idle(), true);
            }
        }
    }

    public class SigmaCooldownState : CharState
    {
        public SigmaCooldownState(string sprite) : base(sprite)
        {
        }

        public override void update()
        {
            base.update();
            if (character.isAnimOver())
            {
                character.changeToIdleOrFall();
            }
        }
    }

    public class SigmaUpDownSlashState : CharState
    {
        bool isUp;
        public SigmaUpDownSlashState(bool isUp) : base(isUp ? "upslash" : "downslash", "", "", "")
        {
            this.isUp = isUp;
            enterSound = "sigma2slash";
        }

        public override void update()
        {
            base.update();

            var moveAmount = new Point(0, isUp ? -1 : 1);

            float maxStateTime = isUp ? 0.35f : 0.5f;
            if (stateTime > maxStateTime || Global.level.checkCollisionActor(character, moveAmount.x, moveAmount.y, moveAmount) != null)
            {
                character.changeState(character.grounded ? new SigmaCooldownState("downslash_land") : new Fall(), true);
                return;
            }

            character.move(moveAmount.times(300));
        }

        public override bool canEnter(Character character)
        {
            if (!base.canEnter(character)) return false;
            if (isUp && character.dashedInAir > 0) return false;
            return true;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.useGravity = false;
            character.stopMoving();
            if (isUp)
            {
                character.unstickFromGround();
                character.dashedInAir++;
            }
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.useGravity = true;
        }
    }
}
