using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class StingChameleon : Maverick
    {
        public StingCStingWeapon stingWeapon = new StingCStingWeapon();
        public StingCTongueWeapon tongueWeapon;
        public StingCSpikeWeapon specialWeapon = new StingCSpikeWeapon();
        public bool uncloakSoundPlayed;
        public float invisibleCooldown;
        public const float maxInvisibleCooldown = 2;
        public bool isInvisible;
        public float cloakTransitionTime;
        public float uncloakTransitionTime;

        public StingChameleon(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            tongueWeapon = new StingCTongueWeapon(player);

            stateCooldowns.Add(typeof(MShoot), new MaverickStateCooldown(false, true, 0.75f));
            stateCooldowns.Add(typeof(StingCTongueState), new MaverickStateCooldown(false, true, 1));
            stateCooldowns.Add(typeof(StingCClimbTongueState), new MaverickStateCooldown(false, true, 1));
            stateCooldowns.Add(typeof(StingCHangState), new MaverickStateCooldown(false, false, 2f));
            stateCooldowns.Add(typeof(StingCClingShootState), new MaverickStateCooldown(false, true, 0.5f));

            weapon = new Weapon(WeaponIds.StingCGeneric, 98);

            canClimb = true;
            invisibleShader = Helpers.cloneShaderSafe("invisible");

            awardWeaponId = WeaponIds.Sting;
            weakWeaponId = WeaponIds.Boomerang;
            weakMaverickWeaponId = WeaponIds.BoomerKuwanger;

            netActorCreateId = NetActorCreateId.StingChameleon;
            netOwner = player;
            if (sendRpc)
            {
                createActorRpc(player.id);
            }
        }

        public bool isCloakTransition()
        {
            return cloakTransitionTime > 0 || uncloakTransitionTime > 0;
        }

        public override void update()
        {
            base.update();

            Helpers.decrementTime(ref invisibleCooldown);

            if (!isCloakTransition())
            {
                if (isInvisible)
                {
                    drainAmmo(4);
                    if (ammo <= 0)
                    {
                        uncloakTransitionTime = 1;
                        playSound("stingcCloak", sendRpc: true);
                    }
                }
                else
                {
                    rechargeAmmo(1);
                }
            }

            if (uncloakTransitionTime > 0)
            {
                Helpers.decrementTime(ref uncloakTransitionTime);
                alpha = Helpers.clamp01(1 - uncloakTransitionTime);
                if (uncloakTransitionTime == 0)
                {
                    isInvisible = false;
                }
            }
            else if (cloakTransitionTime > 0)
            {
                Helpers.decrementTime(ref cloakTransitionTime);
                alpha = Helpers.clamp01(cloakTransitionTime);
            }

            if (aiBehavior == MaverickAIBehavior.Control && !isCloakTransition())
            {
                if (state is MIdle || state is MRun)
                {
                    if (shootPressed() && !isInvisible)
                    {
                        var inputDir = input.getInputDir(player);
                        int type = 0;
                        if (inputDir.y == -1 && inputDir.x == 0) type = 2;
                        if (inputDir.y == -1 && inputDir.x != 0) type = 1;
                        changeState(new StingCTongueState(type));
                    }
                    else if (specialPressed() && !isInvisible)
                    {
                        changeState(getShootState(false));
                    }
                    else if (input.isPressed(Control.Dash, player))
                    {
                        cloakOrUncloak();
                    }
                }
                else if (state is MJump || state is MFall)
                {
                    if (input.isHeld(Control.Special1, player) && !isInvisible)
                    {
                        var hit = Global.level.raycast(pos, pos.addxy(0, -105), new List<Type>() { typeof(Wall) });
                        if (vel.y < 100 && hit?.gameObject is Wall wall && !wall.topWall)
                        {
                            changeState(new StingCHangState(hit.getHitPointSafe().y));
                        }
                    }
                }
                else if (state is StingCClimb)
                {
                    if (input.isPressed(Control.Shoot, player) && !isInvisible)
                    {
                        var inputDir = input.getInputDir(player);
                        changeState(new StingCClimbTongueState(inputDir));
                    }
                    else if (input.isPressed(Control.Special1, player) && !isInvisible)
                    {
                        changeState(new StingCClingShootState());
                    }
                    else if (input.isPressed(Control.Dash, player))
                    {
                        cloakOrUncloak();
                    }
                }
            }
        }

        public void decloak()
        {
            isInvisible = false;
            cloakTransitionTime = 0;
            uncloakTransitionTime = 0;
            alpha = 1;
        }

        public void cloakOrUncloak()
        {
            if (isCloakTransition()) return;
            if (!isInvisible)
            {
                if (ammo >= 8)
                {
                    deductAmmo(8);
                    isInvisible = true;
                    cloakTransitionTime = 1;
                    playSound("stingcCloak", sendRpc: true);
                }
            }
            else
            {
                uncloakTransitionTime = 1;
                playSound("stingcCloak", sendRpc: true);
            }
        }

        public override string getMaverickPrefix()
        {
            return "stingc";
        }

        public MaverickState getShootState(bool isAI)
        {
            var shootState = new MShoot((Point pos, int xDir) =>
            {
                playSound("stingcSting", sendRpc: true);
                new StingCStingProj(new StingCStingWeapon(), pos, xDir, 3, player, player.getNextActorNetId(), rpc: true);
                new StingCStingProj(new StingCStingWeapon(), pos, xDir, 4, player, player.getNextActorNetId(), rpc: true);
                new StingCStingProj(new StingCStingWeapon(), pos, xDir, 5, player, player.getNextActorNetId(), rpc: true);
            }, null);
            if (isAI)
            {
                shootState.consecutiveData = new MaverickStateConsecutiveData(0, 2, 0.5f);
            }
            return shootState;
        }

        public override MaverickState[] aiAttackStates()
        {
            return new MaverickState[]
            {
                new StingCTongueState(0),
                getShootState(true),
            };
        }

        public override MaverickState getRandomAttackState()
        {
            var attacks = new MaverickState[]
            {
                getShootState(true),
                new StingCTongueState(0),
                //new StingCHangState(),
            };
            return attacks.GetRandomItem();
        }

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            if (sprite.name.Contains("tongue"))
            {
                return new GenericMeleeProj(tongueWeapon, centerPoint, ProjIds.StingCTongue, player);
            }
            return null;
        }

        /*
        public override List<Shader> getShaders()
        {
            List<Shader> shaders = new List<Shader>();
            
            // alpha float doesn't work if one or more shaders exist. So need to use the invisible shader instead
            if (alpha < 1)
            {
                invisibleShader?.SetUniform("alpha", alpha);
                shaders.Add(invisibleShader);
            }
            return shaders;
        }
        */
    }

    #region weapons
    public class StingCStingWeapon : Weapon
    {
        public StingCStingWeapon()
        {
            index = (int)WeaponIds.StingCSting;
            killFeedIndex = 98;
        }
    }

    public class StingCTongueWeapon : Weapon
    {
        public StingCTongueWeapon(Player player)
        {
            damager = new Damager(player, 4, Global.defFlinch, 0.5f);
            index = (int)WeaponIds.StingCTongue;
            killFeedIndex = 98;
        }
    }

    public class StingCSpikeWeapon : Weapon
    {
        public StingCSpikeWeapon()
        {
            index = (int)WeaponIds.StingCSpike;
            killFeedIndex = 98;
        }
    }
    #endregion

    #region projectiles
    public class StingCStingProj : Projectile
    {
        public StingCStingProj(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 2, player, "stingc_proj_csting", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.StingCSting;
            maxTime = 0.75f;

            frameSpeed = 0;
            if (type == 0)
            {
                vel = new Point(0, 250);
            }
            if (type == 1)
            {
                frameIndex = 1;
                vel = new Point(125 * xDir, 250);
            }
            else if (type == 2)
            {
                frameIndex = 2;
                vel = new Point(250 * xDir, 250);
            }

            else if (type == 3)
            {
                frameIndex = 3;
                vel = new Point(250 * xDir, 100);
            }
            else if (type == 4)
            {
                frameIndex = 4;
                vel = new Point(250 * xDir, 0);
            }
            else if (type == 5)
            {
                frameIndex = 5;
                vel = new Point(250 * xDir, -100);
            }

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
        }
    }

    public class StingCSpikeProj : Projectile
    {
        public StingCSpikeProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 2, player, "stingc_proj_spike", Global.defFlinch, 0, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.StingCSpike;
            maxTime = 0.75f;
            useGravity = true;
            vel.y = 50;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
        }
    }
    #endregion

    #region states
    public class StingCClimb : MaverickState
    {
        public StingCClimb() : base("climb")
        {
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.stopMoving();
            maverick.useGravity = false;
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            maverick.frameSpeed = 1;
            maverick.useGravity = true;
        }

        public override void update()
        {
            base.update();
            maverick.stopMoving();
            if (inTransition())
            {
                return;
            }

            maverick.frameSpeed = 0;
            Point oldPos = maverick.pos;
            if (input.isHeld(Control.Up, player))
            {
                maverick.move(new Point(0, -75));
                maverick.frameSpeed = 1;
            }
            else if (input.isHeld(Control.Down, player))
            {
                maverick.move(new Point(0, 75));
                maverick.frameSpeed = 1;
            }
            if (input.isHeld(Control.Left, player))
            {
                maverick.move(new Point(-75, 0));
                maverick.frameSpeed = 1;
            }
            else if (input.isHeld(Control.Right, player))
            {
                maverick.move(new Point(75, 0));
                maverick.frameSpeed = 1;
            }

            maverick.turnToInput(input, player);

            if (!player.isAI && input.isPressed(Control.Jump, player))
            {
                maverick.changeState(new MFall());
            }

            bool lastFrameHitLadder = hitLadder;
            if (!checkClimb())
            {
                if (!lastFrameHitLadder)
                {
                    maverick.changePos(oldPos);
                }
                else
                {
                    maverick.changeState(new MFall());
                }
            }

            if (maverick.grounded)
            {
                maverick.changeState(new MIdle());
            }
        }
    }

    public class StingCTongueState : MaverickState
    {
        public StingCTongueState(int type) : base(type == 0 ? "tongue" : (type == 1 ? "tongue2" : "tongue3"), "")
        {
        }

        public override bool canEnter(Maverick maverick)
        {
            return base.canEnter(maverick);
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.stopMoving();
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (maverick.isAnimOver())
            {
                maverick.changeState(new MIdle());
            }
        }
    }

    public class StingCClimbTongueState : MaverickState
    {
        public MaverickState oldState;
        public StingCClimbTongueState(Point inputDir) : base(getSpriteFromInputDir(inputDir))
        {
        }

        private static string getSpriteFromInputDir(Point inputDir)
        {
            if (inputDir.y == 0) return "cling_tongue";
            else if (inputDir.y == 1)
            {
                if (inputDir.x != 0) return "cling_tongue2";
                else return "cling_tongue3";
            }
            else
            {
                if (inputDir.x != 0) return "cling_tongue4";
                else return "cling_tongue5";
            }
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (maverick.isAnimOver())
            {
                maverick.changeState(oldState);
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.stopMoving();
            maverick.useGravity = false;
            this.oldState = oldState;
        }
    }

    public class StingCClingShootState : MaverickState
    {
        bool shotOnce;
        public MaverickState oldState;
        public StingCClingShootState() : base("cling_shoot", "")
        {
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            Point? shootPos = maverick.getFirstPOI();
            if (!shotOnce && shootPos != null)
            {
                shotOnce = true;
                maverick.playSound("stingcSting", sendRpc: true);
                new StingCStingProj(new StingCStingWeapon(), shootPos.Value, maverick.xDir, 0, player, player.getNextActorNetId(), rpc: true);
                new StingCStingProj(new StingCStingWeapon(), shootPos.Value, maverick.xDir, 1, player, player.getNextActorNetId(), rpc: true);
                new StingCStingProj(new StingCStingWeapon(), shootPos.Value, maverick.xDir, 2, player, player.getNextActorNetId(), rpc: true);
            }

            if (maverick.isAnimOver())
            {
                maverick.changeState(oldState);
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.useGravity = false;
            this.oldState = oldState;
        }
    }

    public class StingCHangState : MaverickState
    {
        int state;
        float spikeTime;
        float endTime;
        float ceilingY;
        public StingCHangState(float ceilingY) : base("hang", "")
        {
            this.ceilingY = ceilingY;
        }

        public override bool canEnter(Maverick maverick)
        {
            Point incPos = getTargetPos(maverick).subtract(maverick.pos);
            if (Global.level.checkCollisionActor(maverick, incPos.x, incPos.y) != null)
            {
                return false;
            }
            return base.canEnter(maverick);
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.stopMoving();
            maverick.useGravity = false;
            maverick.changePos(getTargetPos(maverick));
            maverick.frameSpeed = 0;
        }

        private Point getTargetPos(Maverick maverick)
        {
            return new Point(maverick.pos.x, ceilingY + 97);
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            maverick.useGravity = true;
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (state == 0)
            {
                if (stateTime > 0.25f)
                {
                    maverick.frameSpeed = 1;
                    state = 1;
                }
            }
            else if (state == 1)
            {
                spikeTime += Global.spf;
                if (spikeTime > 0.075f)
                {
                    spikeTime = 0;
                    float randX = Helpers.randomRange(-150, 150);
                    Point pos = new Point(maverick.pos.x + randX, ceilingY);
                    new StingCSpikeProj(new StingCSpikeWeapon(), pos, 1, player, player.getNextActorNetId(), rpc: true);
                    maverick.playSound("stingcSpikeDrop", sendRpc: true);
                }

                if (maverick.loopCount > 4)
                {
                    maverick.frameSpeed = 0;
                    maverick.frameIndex = 1;
                    state = 2;
                }
            }
            else if (state == 2)
            {
                endTime += Global.spf;
                if (endTime > 0.25f)
                {
                    maverick.changeState(new MFall());
                }
            }
        }
    }

    #endregion
}
