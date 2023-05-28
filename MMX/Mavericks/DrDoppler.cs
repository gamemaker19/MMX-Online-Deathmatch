using System;
using System.Collections.Generic;

namespace MMXOnline
{
    public class DrDoppler : Maverick
    {
        public static Weapon getWeapon() { return new Weapon(WeaponIds.DrDoppler, 159); }

        public Weapon meleeWeapon;
        public int ballType;
        public DrDoppler(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            stateCooldowns.Add(typeof(MShoot), new MaverickStateCooldown(false, true, 0.75f));
            stateCooldowns.Add(typeof(DrDopplerAbsorbState), new MaverickStateCooldown(false, true, 0.75f));
            stateCooldowns.Add(typeof(DrDopplerDashStartState), new MaverickStateCooldown(false, false, 1.5f));

            weapon = getWeapon();
            canClimbWall = true;
            canClimb = true;

            weakWeaponId = WeaponIds.AcidBurst;
            weakMaverickWeaponId = WeaponIds.ToxicSeahorse;

            netActorCreateId = NetActorCreateId.DrDoppler;
            netOwner = player;
            if (sendRpc)
            {
                createActorRpc(player.id);
            }
        }

        public override void update()
        {
            base.update();

            if (state is not DrDopplerAbsorbState)
            {
                rechargeAmmo(1);
            }
            else
            {
                drainAmmo(2);
            }

            if (aiBehavior == MaverickAIBehavior.Control)
            {
                if (input.isPressed(Control.Up, player) || input.isPressed(Control.Down, player))
                {
                    if (state is not StingCClimb && grounded)
                    {
                        ballType++;
                        if (ballType == 2) ballType = 0;
                        if (ballType == 1)
                        {
                            Global.level.gameMode.setHUDErrorMessage(player, "Switched to Neurocomputer Vaccine.", false, true);
                            if (player.weapon is DrDopplerWeapon ddw)
                            {
                                ddw.ballType = 1;
                            }
                        }
                        else
                        {
                            Global.level.gameMode.setHUDErrorMessage(player, "Switched to Neurocomputer Shock Gun.", false, true);
                            if (player.weapon is DrDopplerWeapon ddw)
                            {
                                ddw.ballType = 0;
                            }
                        }
                    }
                }

                if (state is MIdle || state is MRun)
                {
                    if (input.isPressed(Control.Shoot, player))
                    {
                        changeState(getShootState(false));
                    }
                    else if (input.isPressed(Control.Special1, player) && ammo >= 8)
                    {
                        deductAmmo(8);
                        changeState(new DrDopplerAbsorbState());
                    }
                    else if (input.isPressed(Control.Dash, player))
                    {
                        changeState(new DrDopplerDashStartState());
                    }
                }
                else if (state is MJump || state is MFall)
                {
                }
            }
        }

        public override string getMaverickPrefix()
        {
            return "drdoppler";
        }
    
        public override MaverickState getRandomAttackState()
        {
            return aiAttackStates().GetRandomItem();
        }

        public override MaverickState[] aiAttackStates()
        {
            return new MaverickState[]
            {
                getShootState(true),
                new DrDopplerAbsorbState(),
                new DrDopplerDashStartState(),
            };
        }

        public MaverickState getShootState(bool isAI)
        {
            var mshoot = new MShoot((Point pos, int xDir) =>
            {
                new DrDopplerBallProj(weapon, pos, xDir, ballType, player, player.getNextActorNetId(), sendRpc: true);
            }, null);
            if (isAI)
            {
                mshoot.consecutiveData = new MaverickStateConsecutiveData(0, 4, 0f);
            }
            return mshoot;
        }

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            if (sprite.name == "drdoppler_dash")
            {
                return new GenericMeleeProj(weapon, centerPoint, ProjIds.DrDopplerDash, player, damage: 4, flinch: Global.defFlinch, hitCooldown: 0.5f, owningActor: this);
            }
            else if (sprite.name == "drdoppler_dash_water")
            {
                return new GenericMeleeProj(weapon, centerPoint, ProjIds.DrDopplerDashWater, player, damage: 2, flinch: 0, hitCooldown: 0.5f, owningActor: this);
            }
            else if (sprite.name == "drdoppler_absorb")
            {
                return new GenericMeleeProj(weapon, centerPoint, ProjIds.DrDopplerAbsorb, player, damage: 0, flinch: 0, hitCooldown: 0.25f, owningActor: this);
            }
            return null;
        }

        public void healDrDoppler(Player attacker, float damage)
        {
            if (ownedByLocalPlayer && health < maxHealth)
            {
                addHealth(damage, true);
                playSound("heal", sendRpc: true);
                addDamageText(-damage);
                ammo -= damage;
                if (ammo < 0) ammo = 0;
                RPC.addDamageText.sendRpc(attacker.id, netId, -damage);
            }
        }
    }

    public class DrDopplerBallProj : Projectile
    {
        public DrDopplerBallProj(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, xDir, 250, 3, player, type == 0 ? "drdoppler_proj_ball" : "drdoppler_proj_ball2", Global.miniFlinch, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            if (type == 0)
            {
                projId = (int)ProjIds.DrDopplerBall;
            }
            else
            {
                projId = (int)ProjIds.DrDopplerBall2;
                damager.damage = 0;
                destroyOnHit = false;
            }
            maxTime = 0.75f;

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
        }
    }

    public class DrDopplerDashStartState : MaverickState
    {
        public DrDopplerDashStartState() : base("dash_charge", "")
        {
            stopMovingOnEnter = true;
        }

        public override void update()
        {
            base.update();
            if (maverick.isAnimOver())
            {
                maverick.changeState(new DrDopplerDashState());
            }
        }
    }

    public class DrDopplerDashState : MaverickState
    {
        Anim barrier;
        public DrDopplerDashState() : base("dash", "dash_start")
        {
            stopMovingOnEnter = true;
        }

        public override void update()
        {
            base.update();
            if (inTransition())
            {
                if (!once)
                {
                    once = true;
                    barrier = new Anim(maverick.pos, "drdoppler_barrier", maverick.xDir, player.getNextActorNetId(), false, sendRpc: true);
                }
                if (barrier != null)
                {
                    barrier.incPos(maverick.deltaPos);
                }
                return;
            }

            else if (barrier != null)
            {
                barrier.destroySelf();
                barrier = null;
            }

            if (maverick.isUnderwater())
            {
                if (!maverick.sprite.name.EndsWith("dash_water"))
                {
                    maverick.changeSpriteFromName("dash_water", false);
                }
            }
            else
            {
                if (!maverick.sprite.name.EndsWith("dash"))
                {
                    maverick.changeSpriteFromName("dash", false);
                }
            }

            var move = new Point(250 * maverick.xDir, 0);

            var hitWall = Global.level.checkCollisionActor(maverick, move.x * Global.spf * 2, -5);
            if (hitWall?.isSideWallHit() == true)
            {
                maverick.changeToIdleOrFall();
                return;
            }

            maverick.move(move);

            if (stateTime > 1.25f || input.isPressed(Control.Dash, player))
            {
                maverick.changeState(new MIdle());
                return;
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            useGravity = false;
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            barrier?.destroySelf();
            useGravity = true;
        }
    }

    public class DrDopplerAbsorbState : MaverickState
    {
        public DrDopplerAbsorbState() : base("absorb", "")
        {
            exitOnAnimEnd = true;
            superArmor = true;
        }

        public override void update()
        {
            base.update();

            if (maverick.ammo <= 0)
            {
                maverick.changeToIdleOrFall();
                return;
            }

            if (input.isHeld(Control.Special1, player) && maverick.frameIndex == 13)
            {
                maverick.frameIndex = 9;
            }
        }
    }

    public class DrDopplerUncoatState : MaverickState
    {
        public DrDopplerUncoatState() : base("uncoat", "")
        {
            exitOnAnimEnd = true;
        }

        public override void update()
        {
            base.update();
            if (!once && maverick.frameIndex >= 1)
            {
                once = true;
                new Anim(maverick.getFirstPOIOrDefault(), "drdoppler_coat", maverick.xDir, player.getNextActorNetId(), false, sendRpc: true) { vel = new Point(maverick.xDir * 50, 0) };
            }
        }
    }
}
