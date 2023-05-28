using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class FlameMammoth : Maverick
    {
        public FlameMStompWeapon stompWeapon;

        public FlameMammoth(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            stateCooldowns.Add(typeof(MShoot), new MaverickStateCooldown(false, true, 0.5f));
            stompWeapon = new FlameMStompWeapon(player);
            stateCooldowns.Add(typeof(FlameMOilState), new MaverickStateCooldown(false, true, 0.5f));
            isHeavy = true;

            awardWeaponId = WeaponIds.FireWave;
            weakWeaponId = WeaponIds.Tornado;
            weakMaverickWeaponId = WeaponIds.StormEagle;

            weapon = new Weapon(WeaponIds.FlameMGeneric, 100);

            netActorCreateId = NetActorCreateId.FlameMammoth;
            netOwner = player;
            if (sendRpc)
            {
                createActorRpc(player.id);
            }
        }

        public override void update()
        {
            base.update();
            if (aiBehavior == MaverickAIBehavior.Control)
            {
                if (state is MIdle || state is MRun)
                {
                    if (shootPressed())
                    {
                        changeState(getShootState(false));
                    }
                    else if (specialPressed())
                    {
                        changeState(new FlameMOilState());
                    }
                }
                else if (state is MJump || state is MFall)
                {
                    if (input.isPressed(Control.Dash, player) && getDistFromGround() > 75)
                    {
                        changeState(new FlameMJumpPressState());
                    }
                }
            }
        }

        public override string getMaverickPrefix()
        {
            return "flamem";
        }

        public MaverickState getShootState(bool isAI)
        {
            var shootState = new MShoot((Point pos, int xDir) =>
            {
                playSound("flamemShoot", sendRpc: true);
                new FlameMFireballProj(new FlameMFireballWeapon(), pos, xDir, player.input.isHeld(Control.Down, player), player, player.getNextActorNetId(), rpc: true);
            }, null);
            if (isAI)
            {
                shootState.consecutiveData = new MaverickStateConsecutiveData(0, 4, 0.5f);
            }
            return shootState;
        }

        public override MaverickState[] aiAttackStates()
        {
            return new MaverickState[]
            {
                getShootState(true),
                new FlameMOilState(),
                new MJumpStart(),
            };
        }

        public override MaverickState getRandomAttackState()
        {
            var attacks = new MaverickState[]
            {
                getShootState(true),
                new FlameMOilState(),
                new MJumpStart(),
            };
            return attacks.GetRandomItem();
        }

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            if (sprite.name.Contains("fall"))
            {
                float damage = 0;
                if (deltaPos.y > 100 * Global.spf) damage = 2f;
                if (deltaPos.y > 200 * Global.spf) damage = 4f;
                if (deltaPos.y > 300 * Global.spf) damage = 6f;
                if (damage > 0)
                {
                    return new GenericMeleeProj(stompWeapon, centerPoint, ProjIds.FlameMStomp, player, damage: damage);
                }
            }
            return null;
        }
    }

    #region weapons
    public class FlameMFireballWeapon : Weapon
    {
        public FlameMFireballWeapon()
        {
            index = (int)WeaponIds.FlameMFireball;
            killFeedIndex = 100;
        }
    }

    public class FlameMStompWeapon : Weapon
    {
        public FlameMStompWeapon(Player player)
        {
            index = (int)WeaponIds.FlameMStomp;
            killFeedIndex = 100;
            damager = new Damager(player, 6, Global.defFlinch, 0.5f);
        }
    }

    public class FlameMOilWeapon : Weapon
    {
        public FlameMOilWeapon()
        {
            index = (int)WeaponIds.FlameMOil;
            killFeedIndex = 100;
        }
    }

    public class FlameMOilFireWeapon : Weapon
    {
        public FlameMOilFireWeapon()
        {
            index = (int)WeaponIds.FlameMOilFire;
            killFeedIndex = 100;
        }
    }

    #endregion

    #region projectiles
    public class FlameMFireballProj : Projectile
    {
        public FlameMFireballProj(Weapon weapon, Point pos, int xDir, bool isShort, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 250, 2, player, "flamem_proj_fireball", 0, 0.01f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.FlameMFireball;
            fadeSprite = "flamem_anim_fireball_fade";
            maxTime = 0.75f;
            useGravity = true;
            gravityModifier = 0.5f;
            collider.wallOnly = true;
            if (isShort)
            {
                vel.x *= 0.5f;
            }

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
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
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            destroySelf();
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (!ownedByLocalPlayer) return;
            if (other.gameObject is FlameMOilSpillProj oilSpill && oilSpill.ownedByLocalPlayer)
            {
                playSound("flamemOilBurn", sendRpc: true);
                new FlameMBigFireProj(new FlameMOilFireWeapon(), oilSpill.pos, oilSpill.xDir, oilSpill.angle ?? 0, owner, owner.getNextActorNetId(), rpc: true);
                // oilSpill.time = 0;
                oilSpill.destroySelf(doRpcEvenIfNotOwned: true);
                destroySelf();
            }
        }
    }

    public class FlameMOilProj : Projectile
    {
        public FlameMOilProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 175, 0, player, "flamem_proj_oilball", 0, 0.01f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.FlameMOil;
            maxTime = 0.75f;
            useGravity = true;
            vel.y = -150;
            collider.wallOnly = true;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            if (!ownedByLocalPlayer) return;
            if (!destroyed)
            {
                new FlameMOilSpillProj(new FlameMOilWeapon(), other.getHitPointSafe(), 1, other.getNormalSafe().angle + 90, owner, owner.getNextActorNetId(), rpc: true);
                playSound("flamemOil", sendRpc: true);
                destroySelf();
            }
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
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (!ownedByLocalPlayer) return;
            if (other.gameObject is FlameMBigFireProj bigFire && bigFire.ownedByLocalPlayer && !destroyed)
            {
                playSound("flamemOilBurn", sendRpc: true);
                bigFire.reignite();
                destroySelf();
            }
        }
    }

    public class FlameMOilSpillProj : Projectile
    {
        public FlameMOilSpillProj(Weapon weapon, Point pos, int xDir, float angle, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 0, player, "flamem_proj_oilspill", 0, 0f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.FlameMOilSpill;
            maxTime = 8f;
            this.angle = angle;
            destroyOnHit = false;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            moveWithMovingPlatform();

            if (isUnderwater())
            {
                destroySelf();
                return;
            }
        }
    }

    public class FlameMBigFireProj : Projectile
    {
        public FlameMBigFireProj(Weapon weapon, Point pos, int xDir, float angle, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 2, player, "flamem_proj_bigfire", Global.defFlinch, 0.15f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.FlameMOilFire;
            maxTime = 8;
            this.angle = angle;
            destroyOnHit = false;
            shouldShieldBlock = false;
            shouldVortexSuck = false;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            if (!ownedByLocalPlayer) return;
            base.update();

            moveWithMovingPlatform();

            if (isUnderwater())
            {
                destroySelf();
                return;
            }
        }

        public void reignite()
        {
            frameIndex = 0;
            frameTime = 0;
            time = 0;
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (!ownedByLocalPlayer) return;
            if (other.gameObject is FlameMOilSpillProj oilSpill && oilSpill.ownedByLocalPlayer && frameIndex >= 4)
            {
                playSound("flamemOilBurn", sendRpc: true);
                new FlameMBigFireProj(new FlameMOilFireWeapon(), oilSpill.pos, oilSpill.xDir, oilSpill.angle ?? 0, owner, owner.getNextActorNetId(), rpc: true);
                // oilSpill.time = 0;
                oilSpill.destroySelf();
            }
        }
    }


    public class FlameMStompShockwave : Projectile
    {
        public FlameMStompShockwave(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 0, player, "flamem_proj_shockwave", 0, 1f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 0.75f;
            projId = (int)ProjIds.FlameMStompShockwave;
            destroyOnHit = false;
            shouldShieldBlock = false;
            shouldVortexSuck = false;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void onStart()
        {
            base.onStart();
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            if (isAnimOver())
            {
                destroySelf();
            }
        }
    }

    #endregion

    #region states

    public class FlameMOilState : MaverickState
    {
        public FlameMOilState() : base("shoot2", "")
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

            if (maverick.frameIndex == 6 && !once)
            {
                once = true;
                var poi = maverick.getFirstPOI().Value;
                new FlameMOilProj(new FlameMOilWeapon(), poi, maverick.xDir, player, player.getNextActorNetId(), rpc: true);
            }

            if (maverick.isAnimOver())
            {
                maverick.changeState(new MIdle());
            }
        }
    }

    public class FlameMJumpPressState : MaverickState
    {
        public FlameMJumpPressState() : base("fall")
        {
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (maverick.grounded)
            {
                landingCode();
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.vel = new Point(0, 300);
        }
    }
    #endregion
}
