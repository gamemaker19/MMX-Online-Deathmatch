using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class Velguarder : Maverick
    {
        public VelGMeleeWeapon meleeWeapon;

        public Velguarder(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            stateCooldowns.Add(typeof(MShoot), new MaverickStateCooldown(false, true, 0.75f));
            meleeWeapon = new VelGMeleeWeapon(player);
            canClimbWall = true;

            awardWeaponId = WeaponIds.Buster;
            weakWeaponId = WeaponIds.ShotgunIce;
            weakMaverickWeaponId = WeaponIds.ChillPenguin;

            weapon = new Weapon(WeaponIds.VelGGeneric, 101);

            netActorCreateId = NetActorCreateId.Velguarder;
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
                        changeState(getShootState());
                    }
                    else if (specialPressed())
                    {
                        changeState(getShootState2());
                    }
                    else if (input.isPressed(Control.Dash, player))
                    {
                        changeState(new VelGPounceStartState());
                    }
                }
                else if (state is MJump || state is MFall)
                {
                }
            }
        }

        public override string getMaverickPrefix()
        {
            return "velg";
        }

        public override float getRunSpeed()
        {
            return 135f;
        }

        public MaverickState getShootState()
        {
            return new VelGShootFireState();
        }

        public MaverickState getShootState2()
        {
            return new VelGShootIceState();
        }

        public override MaverickState[] aiAttackStates()
        {
            return new MaverickState[]
            {
                new VelGShootFireState(),
                new VelGShootIceState(),
                new VelGPounceStartState(),
            };
        }

        public override MaverickState getRandomAttackState()
        {
            var attacks = new MaverickState[]
            {
                new VelGShootFireState(),
                new VelGShootIceState(),
                new VelGPounceStartState(),
            };
            return attacks.GetRandomItem();
        }

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            if (sprite.name.Contains("pounce"))
            {
                return new GenericMeleeProj(meleeWeapon, centerPoint, ProjIds.VelGMelee, player);
            }
            return null;
        }
    }

    #region weapons
    public class VelGFireWeapon : Weapon
    {
        public VelGFireWeapon()
        {
            index = (int)WeaponIds.VelGFire;
            killFeedIndex = 101;
        }
    }

    public class VelGIceWeapon : Weapon
    {
        public VelGIceWeapon()
        {
            index = (int)WeaponIds.VelGIce;
            killFeedIndex = 101;
        }
    }

    public class VelGMeleeWeapon : Weapon
    {
        public VelGMeleeWeapon(Player player)
        {
            index = (int)WeaponIds.VelGMelee;
            killFeedIndex = 101;
            damager = new Damager(player, 3, Global.defFlinch, 0.5f);
        }
    }
    #endregion

    #region projectiles
    public class VelGFireProj : Projectile
    {
        public VelGFireProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 125, 1, player, "velg_proj_fire", 0, 0.01f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.VelGFire;
            maxTime = 1f;
            vel.y = 200;

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
            if (MathF.Abs(vel.y) < 600) vel.y -= Global.spf * 600;
        }
    }

    public class VelGIceProj : Projectile
    {
        public VelGIceProj(Weapon weapon, Point pos, int xDir, Point vel, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 2, player, "velg_proj_ice", 0, 0.01f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.VelGIce;
            maxTime = 0.75f;
            useGravity = true;
            this.vel = vel;

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
    public class VelGShootFireState : MaverickState
    {
        float shootTime;
        public VelGShootFireState() : base("shoot2", "")
        {
            
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (maverick.frameIndex == 1)
            {
                var poi = maverick.getFirstPOIOrDefault();
                shootTime += Global.spf;
                if (shootTime > 0.05f)
                {
                    shootTime = 0;
                    maverick.playSound("fireWave", sendRpc: true);
                    new VelGFireProj(new VelGFireWeapon(), poi, maverick.xDir, player, player.getNextActorNetId(), rpc: true);
                }
            }

            if (maverick.isAnimOver())
            {
                maverick.changeState(new MIdle());
            }
        }
    }

    public class VelGShootIceState : MaverickState
    {
        bool shot;
        int index = 0;
        public VelGShootIceState() : base("shoot", "")
        {

        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            maverick.turnToInput(input, player);

            if (maverick.frameIndex % 2 == 1)
            {
                if (!shot)
                {
                    shot = true;
                    index++;
                    var poi = maverick.getFirstPOIOrDefault();
                    //float xSpeed = (index % 2 == 0 ? 150 : 200);
                    var inputDir = input.getInputDir(player);
                    float xSpeed = inputDir.x == maverick.xDir ? 200 : 150;
                    new VelGIceProj(new VelGIceWeapon(), poi, maverick.xDir, new Point(xSpeed * maverick.xDir, -200), player, player.getNextActorNetId(), rpc: true);
                }
            }
            else
            {
                shot = false;
            }

            if (maverick.isAnimOver())
            {
                maverick.changeState(new MIdle());
            }
        }
    }

    public class VelGPounceStartState : MaverickState
    {
        public VelGPounceStartState() : base("jump_start")
        {
        }

        public override void update()
        {
            base.update();

            if (maverick.isAnimOver())
            {
                maverick.vel.y = -maverick.getJumpPower() * 0.75f;
                maverick.changeState(new VelGPounceState());
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
        }
    }

    public class VelGPounceState : MaverickState
    {
        public VelGPounceState() : base("pounce")
        {
        }

        public override void update()
        {
            base.update();

            if (maverick.grounded && stateTime > 0.05f)
            {
                landingCode();
                return;
            }

            wallClimbCode();

            if (Global.level.checkCollisionActor(maverick, 0, -1) != null && maverick.vel.y < 0)
            {
                maverick.vel.y = 0;
            }

            maverick.move(new Point(maverick.xDir * 300, 0));
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
        }
    }

    public class VelGDeathAnim : Anim
    {
        Player player;
        public VelGDeathAnim(Point pos, int xDir, Player player, ushort? netId = null, bool sendRpc = false, bool ownedByLocalPlayer = true) :
            base(pos, "velg_anim_die", xDir, netId, false, sendRpc, ownedByLocalPlayer)
        {
            vel = new Point(-xDir * 150, -150);
            ttl = 0.5f;
            useGravity = true;
            collider.wallOnly = true;
            this.player = player;
        }

        public override void update()
        {
            base.update();
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (other.gameObject is Wall && time > 0.1f)
            {
                destroySelf();
            }
        }

        public override void onDestroy()
        {
            base.onDestroy();
            var dieEffect = new ExplodeDieEffect(player, getCenterPos(), getCenterPos(), "empty", 1, zIndex, false, 25, 0.75f, false);
            Global.level.addEffect(dieEffect);
            Anim.createGibEffect("velg_pieces", getCenterPos(), player, GibPattern.SemiCircle, randVelStart: 150, randVelEnd: 200, sendRpc: true);
        }
    }

    #endregion
}
