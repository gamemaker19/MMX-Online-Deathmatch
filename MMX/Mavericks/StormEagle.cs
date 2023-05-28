using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class StormEagle : Maverick
    {
        public StormEDiveWeapon diveWeapon;

        public StormEagle(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            diveWeapon = new StormEDiveWeapon(player);

            stateCooldowns.Add(typeof(StormEAirShootState), new MaverickStateCooldown(true, true, 2f));
            stateCooldowns.Add(typeof(MShoot), new MaverickStateCooldown(true, true, 2f));
            stateCooldowns.Add(typeof(StormEEggState), new MaverickStateCooldown(false, true, 1.5f));
            stateCooldowns.Add(typeof(StormEGustState), new MaverickStateCooldown(false, true, 0.75f));
            stateCooldowns.Add(typeof(StormEDiveState), new MaverickStateCooldown(false, false, 1f));

            canFly = true;

            weapon = new Weapon(WeaponIds.StormEGeneric, 99);

            awardWeaponId = WeaponIds.Tornado;
            weakWeaponId = WeaponIds.Sting;
            weakMaverickWeaponId = WeaponIds.StingChameleon;

            netActorCreateId = NetActorCreateId.StormEagle;
            netOwner = player;
            if (sendRpc)
            {
                createActorRpc(player.id);
            }
        }

        public override void update()
        {
            base.update();

            if (!isUnderwater())
            {
                spriteFrameToSounds["storme_fly/1"] = "stormeFlap";
                spriteFrameToSounds["storme_fly_fall/1"] = "stormeFlap";
            }
            else
            {
                spriteFrameToSounds.Clear();
            }

            if (aiBehavior == MaverickAIBehavior.Control)
            {
                if (state is MIdle || state is MRun)
                {
                    if (shootPressed())
                    {
                        changeState(getShootState());
                    }
                    if (specialPressed())
                    {
                        changeState(new StormEEggState(true));
                    }
                    else if (input.isPressed(Control.Dash, player))
                    {
                        changeState(new StormEGustState());
                    }
                }
                else if (state is MJump || state is MFall)
                {
                    if (input.isPressed(Control.Dash, player))
                    {
                        changeState(new StormEDiveState());
                    }
                }
                else if (state is MFly)
                {
                    if (shootPressed())
                    {
                        changeState(new StormEAirShootState());
                    }
                    if (specialPressed())
                    {
                        changeState(new StormEEggState(false));
                    }
                    else if (input.isPressed(Control.Dash, player))
                    {
                        changeState(new StormEDiveState());
                    }
                }
            }
        }

        public override string getMaverickPrefix()
        {
            return "storme";
        }

        public MaverickState getShootState()
        {
            return new MShoot((Point pos, int xDir) =>
            {
                playSound("tornado", sendRpc: true);
                new TornadoProj(new StormETornadoWeapon(), pos, xDir, true, player, player.getNextActorNetId(), sendRpc: true);
            }, null);
        }

        public override MaverickState[] aiAttackStates()
        {
            return new MaverickState[]
            {
                getShootState(),
                new StormEEggState(true),
                new StormEGustState(),
            };
        }

        public override MaverickState getRandomAttackState()
        {
            if (grounded)
            {
                var attacks = new MaverickState[]
                {
                    getShootState(),
                    new StormEEggState(true),
                    new StormEGustState(),
                };
                return attacks.GetRandomItem();
            }
            else
            {
                var attacks = new MaverickState[]
                {
                    new StormEEggState(false),
                };
                return attacks.GetRandomItem();
            }
        }

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            if (sprite.name.Contains("dive"))
            {
                return new GenericMeleeProj(diveWeapon, centerPoint, ProjIds.StormEDive, player);
            }
            return null;
        }
    }

    #region weapons
    public class StormETornadoWeapon : Weapon
    {
        public StormETornadoWeapon()
        {
            index = (int)WeaponIds.StormETornado;
            killFeedIndex = 99;
        }
    }

    public class StormEDiveWeapon : Weapon
    {
        public StormEDiveWeapon(Player player)
        {
            damager = new Damager(player, 4, Global.defFlinch, 0.5f);
            index = (int)WeaponIds.StormEDive;
            killFeedIndex = 99;
        }
    }

    public class StormEEggWeapon : Weapon
    {
        public StormEEggWeapon()
        {
            index = (int)WeaponIds.StormEEgg;
            killFeedIndex = 99;
        }
    }

    public class StormEBirdWeapon : Weapon
    {
        public StormEBirdWeapon()
        {
            index = (int)WeaponIds.StormEBird;
            killFeedIndex = 99;
        }
    }

    public class StormEGustWeapon : Weapon
    {
        public StormEGustWeapon()
        {
            index = (int)WeaponIds.StormEGust;
            killFeedIndex = 99;
        }
    }
    #endregion

    #region projectiles
    public class StormEEggProj : Projectile
    {
        public StormEEggProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 100, 2, player, "storme_proj_egg", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.StormEEgg;
            maxTime = 0.675f;
            useGravity = true;
            vel.y = -100;
            collider.wallOnly = true;
            fadeSound = "explosion";
            fadeSprite = "explosion";

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            destroySelf();
        }

        public override void onDestroy()
        {
            base.onDestroy();
            if (!ownedByLocalPlayer) return;
            if (time > maxTime) return;
            Weapon w = new StormEBirdWeapon();
            new StormEBirdProj(w, pos, xDir, new Point(-1.25f * xDir, -1), new Point(xDir, 0.1f), owner, owner.getNextActorNetId(), rpc: true);
            new StormEBirdProj(w, pos, xDir, new Point(-1.25f * xDir, 1), new Point(xDir * 0.85f, -0.05f), owner, owner.getNextActorNetId(), rpc: true);
            new StormEBirdProj(w, pos, xDir, new Point(1.25f * xDir, -1), new Point(xDir * 0.85f, 0.05f), owner, owner.getNextActorNetId(), rpc: true);
            new StormEBirdProj(w, pos, xDir, new Point(1.25f * xDir, 1), new Point(xDir, -0.125f), owner, owner.getNextActorNetId(), rpc: true);
        }
    }

    public class StormEBirdProj : Projectile, IDamagable
    {
        public int health = 1;
        Point afterUnitVel;
        public StormEBirdProj(Weapon weapon, Point pos, int xDir, Point startUnitVel, Point afterUnitVel, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 3, player, "storme_proj_baby", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.StormEBird;
            maxTime = 1.5f;
            fadeSound = "explosion";
            fadeSprite = "explosion";
            vel = startUnitVel.times(50);
            xDir = MathF.Sign(vel.x);
            this.afterUnitVel = afterUnitVel;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public void applyDamage(Player owner, int? weaponIndex, float damage, int? projId)
        {
            destroySelf();
        }

        public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId)
        {
            return damagerAlliance != owner.alliance;
        }

        public bool canBeHealed(int healerAlliance)
        {
            return false;
        }

        public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false)
        {
        }

        public bool isInvincible(Player attacker, int? projId)
        {
            return false;
        }

        public override void update()
        {
            base.update();
            if (time > 0.25f)
            {
                vel = afterUnitVel.times(150);
                xDir = MathF.Sign(vel.x);
            }
        }
    }

    public class StormEGustProj : Projectile
    {
        float maxSpeed = 250;
        public StormEGustProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 250, 0, player, "storme_proj_gust", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.StormEGust;
            maxTime = 0.75f;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            vel.y -= Global.spf * 100;
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
                character.move(new Point(maxSpeed * 0.9f * xDir * modifier, 0));
                character.pushedByTornadoInFrame = true;
            }
        }
    }
    #endregion

    #region states
    public class StormEDiveState : MaverickState
    {
        Point diveVel;
        bool reverse;
        float incAmount;
        bool wasPrevStateFly;
        public StormEDiveState() : base("dive", "")
        {
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (reverse)
            {
                diveVel.y -= incAmount * Global.spf;
                incAmount += Global.spf * 1500;
                if (incAmount > 1500) incAmount = 1500;
                if (diveVel.y < 0 && sprite != "dive2")
                {
                    sprite = "dive2";
                    maverick.changeSpriteFromName(sprite, true);
                }
                if (diveVel.y < -250) diveVel.y = -250;
            }

            Point finalDiveVel = diveVel;
            if (maverick.isUnderwater())
            {
                finalDiveVel = finalDiveVel.times(0.75f);
            }
            if (maverick.pos.y <= -5)
            {
                finalDiveVel.y = Math.Max(finalDiveVel.y, 0);
            }

            maverick.move(finalDiveVel);

            if (maverick.grounded)
            {
                maverick.changeState(new MIdle());
                return;
            }

            var hit = checkCollisionNormal(finalDiveVel.x * Global.spf, finalDiveVel.y * Global.spf);
            if (hit != null && !hit.getNormalSafe().isGroundNormal())
            {
                maverick.changeState(wasPrevStateFly ? new MFly() : new MFall());
                return;
            }

            if (stateTime > 1)
            {
                maverick.changeState(wasPrevStateFly ? new MFly() : new MFall());
                return;
            }

            if (input.isHeld(Control.Up, player) && stateTime > 0.1f)
            {
                reverse = true;
            }
        }

        public override bool canEnter(Maverick maverick)
        {
            return base.canEnter(maverick);
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.stopMoving();
            maverick.useGravity = false;
            diveVel = new Point(maverick.xDir * 250, maverick.yDir * 250);
            maverick.playSound("stormeDive");
            incAmount = 750;
            wasPrevStateFly = oldState is MFly;
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            maverick.useGravity = true;
        }
    }

    public class StormEGustState : MaverickState
    {
        float soundTime = 0.5f;
        float gustTime;
        public StormEGustState() : base("flap", "")
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

            soundTime += Global.spf;
            if (soundTime > 0.4f)
            {
                soundTime = 0;
                if (!maverick.isUnderwater()) maverick.playSound("stormeFlap", sendRpc: true);
            }

            gustTime += Global.spf;
            if (gustTime > 0.1f)
            {
                gustTime = 0;
                float randX = maverick.pos.x + maverick.xDir * Helpers.randomRange(0, 65);
                Point pos = new Point(randX, maverick.pos.y);
                new StormEGustProj(new StormEGustWeapon(), pos, maverick.xDir, player, player.getNextActorNetId(), rpc: true);
            }

            if (isAI)
            {
                if (stateTime > 4)
                {
                    maverick.changeState(new MIdle());
                }
            }
            else
            {
                if (!input.isHeld(Control.Dash, player))
                {
                    maverick.changeState(new MIdle());
                }
            }
        }
    }

    public class StormEEggState : MaverickState
    {
        bool isGrounded;
        public StormEEggState(bool isGrounded) : base(isGrounded ? "air_eggshoot" : "air_eggshoot", "")
        {
            this.isGrounded = isGrounded;
        }

        public override bool canEnter(Maverick maverick)
        {
            return base.canEnter(maverick);
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            if (!isGrounded)
            {
                maverick.stopMoving();
                maverick.useGravity = false;
            }
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (maverick.frameIndex == 3 && !once)
            {
                once = true;
                var poi = maverick.getFirstPOI().Value;
                new StormEEggProj(new StormEEggWeapon(), poi, maverick.xDir, player, player.getNextActorNetId(), rpc: true);
            }

            if (maverick.isAnimOver())
            {
                maverick.changeState(isGrounded ? new MIdle() : new MFly());
            }
        }
    }

    public class StormEAirShootState : MaverickState
    {
        bool shotOnce;
        public StormEAirShootState() : base("air_shoot", "")
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
                maverick.playSound("tornado", sendRpc: true);
                new TornadoProj(new StormETornadoWeapon(), shootPos.Value, maverick.xDir, true, player, player.getNextActorNetId(), sendRpc: true);
            }

            if (maverick.isAnimOver())
            {
                maverick.changeState(new MFly());
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.useGravity = false;
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
        }
    }
    #endregion
}
