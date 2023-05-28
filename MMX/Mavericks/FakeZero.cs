namespace MMXOnline
{
    public class FakeZero : Maverick
    {
        public static Weapon getWeapon() { return new Weapon(WeaponIds.FakeZeroGeneric, 150); }

        float dashDist;
        float baseSpeed = 50;
        float accSpeed;
        int lastDirX;
        public Anim exhaust;
        public const float topSpeed = 200;
        public float jumpXMomentum = 1;

        public FakeZero(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            stateCooldowns.Add(typeof(MShoot), new MaverickStateCooldown(false, true, 0.5f));
            stateCooldowns.Add(typeof(FakeZeroGroundPunchState), new MaverickStateCooldown(false, false, 1.5f));
            stateCooldowns.Add(typeof(FakeZeroShootAirState), new MaverickStateCooldown(false, true, 0.5f));
            stateCooldowns.Add(typeof(FakeZeroShootAir2State), new MaverickStateCooldown(false, true, 0.5f));

            weapon = getWeapon();

            weakWeaponId = WeaponIds.SpeedBurner;
            weakMaverickWeaponId = WeaponIds.FlameStag;
            canClimbWall = true;
            canClimb = true;

            exhaust = new Anim(pos, "fakezero_exhaust", xDir, null, false, false, ownedByLocalPlayer);

            netActorCreateId = NetActorCreateId.FakeZero;
            netOwner = player;
            if (sendRpc)
            {
                createActorRpc(player.id);
            }
        }

        public override void preUpdate()
        {
            base.preUpdate();
            if (sprite.name.Contains("run"))
            {
                exhaust.zIndex = zIndex - 100;
                exhaust.visible = true;
                exhaust.xDir = xDir;
                exhaust.changePos(getFirstPOIOrDefault());
            }
            else
            {
                exhaust.visible = false;
            }
        }

        public override float getAirSpeed()
        {
            return jumpXMomentum;
        }

        public override void update()
        {
            base.update();

            if (!ownedByLocalPlayer) return;

            rechargeAmmo(1);

            if (lastDirX != xDir)
            {
                accSpeed = 0;
                dashDist = 0;
            }
            lastDirX = xDir;

            if (state is MRun || state is FakeZeroMeleeState)
            {
                dashDist += accSpeed * Global.spf;
                accSpeed += Global.spf * 500;
                if (accSpeed > topSpeed) accSpeed = topSpeed;
                /*
                if (dashDist > 250)
                {
                    accSpeed = 0;
                    dashDist = 0;
                    changeState(new MIdle());
                    return;
                }
                */
            }
            else if (state is MJumpStart)
            {
                jumpXMomentum = 1 + 0.5f * (accSpeed / topSpeed);
            }
            else
            {
                accSpeed = 0;
            }

            if (aiBehavior == MaverickAIBehavior.Control)
            {
                if (state is MIdle || state is MRun)
                {
                    if (input.isHeld(Control.Shoot, player) && state is MRun)
                    {
                        changeState(new FakeZeroMeleeState());   
                    }
                    else if (input.isHeld(Control.Shoot, player) && ammo >= 2)
                    {
                        changeState(getShootState(false));
                    }
                    else if (input.isPressed(Control.Special1, player) && ammo >= 8)
                    {
                        changeState(new FakeZeroShoot2State());
                    }
                    else if (input.isPressed(Control.Dash, player))
                    {
                        changeState(new FakeZeroGroundPunchState());
                    }
                    else if (input.isHeld(Control.Down, player))
                    {
                        changeState(new FakeZeroGuardState());
                    }
                }
                else if (state is MJump || state is MFall || state is MWallKick)
                {
                    if (input.isHeld(Control.Shoot, player) && ammo >= 2)
                    {
                        changeState(new FakeZeroShootAirState());
                    }
                    else if (input.isPressed(Control.Special1, player) && ammo >= 8)
                    {
                        changeState(new FakeZeroShootAir2State());
                    }
                }
            }
        }

        public override float getRunSpeed()
        {
            if (state is MRun || state is FakeZeroMeleeState) return baseSpeed + accSpeed;
            return 100;
        }

        public override string getMaverickPrefix()
        {
            return "fakezero";
        }

        public override MaverickState[] aiAttackStates()
        {
            var attacks = new MaverickState[]
            {
                getShootState(true),
                new FakeZeroShoot2State(),
                new FakeZeroGroundPunchState(),
            };
            return attacks;
        }

        public override MaverickState getRandomAttackState()
        {
            return aiAttackStates().GetRandomItem();
        }

        public MaverickState getShootState(bool isAI)
        {
            var mshoot = new MShoot((Point pos, int xDir) =>
            {
                playSound("zbuster2", sendRpc: true);
                deductAmmo(2);
                new FakeZeroBusterProj(weapon, pos, xDir, player, player.getNextActorNetId(), rpc: true);
            }, null);
            if (isAI)
            {
                mshoot.consecutiveData = new MaverickStateConsecutiveData(0, 4, 0.75f);
            }
            return mshoot;
        }

        public override void onDestroy()
        {
            base.onDestroy();
            exhaust?.destroySelf();
        }
    }

    public class FakeZeroBusterProj : Projectile
    {
        public FakeZeroBusterProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 350, 3, player, "fakezero_buster_proj", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.FakeZeroBuster;
            reflectable = true;
            maxTime = 0.75f;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }

    public class FakeZeroBuster2Proj : Projectile
    {
        public FakeZeroBuster2Proj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 350, 3, player, "fakezero_buster2_proj", Global.defFlinch, 0.01f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.FakeZeroBuster2;
            maxTime = 0.75f;
            reflectable = true;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }

    public class FakeZeroSwordBeamProj : Projectile
    {
        public FakeZeroSwordBeamProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 350, 2, player, "fakezero_sword_proj", 0, 0.01f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.FakeZeroSwordBeam;
            maxTime = 0.75f;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }

    public class FakeZeroShootAirState : MaverickState
    {
        public FakeZeroShootAirState() : base("shoot_air", "")
        {
        }

        public override void update()
        {
            base.update();

            airCode();
            Point? shootPos = maverick.getFirstPOI();

            if (shootPos != null)
            {
                if (!once)
                {
                    once = true;
                    maverick.playSound("zbuster2", sendRpc: true);
                    maverick.deductAmmo(2);
                    new FakeZeroBusterProj(maverick.weapon, shootPos.Value, maverick.xDir, player, player.getNextActorNetId(), rpc: true);
                }
            }

            if (maverick.isAnimOver() || maverick.grounded)
            {
                maverick.changeToIdleOrFall();
            }
        }
    }

    public class FakeZeroShootAir2State : MaverickState
    {
        public FakeZeroShootAir2State() : base("shoot_air2", "")
        {
        }

        public override void update()
        {
            base.update();

            airCode();
            Point? shootPos = maverick.getFirstPOI();

            if (shootPos != null)
            {
                if (!once)
                {
                    once = true;
                    maverick.deductAmmo(8);
                    maverick.playSound("zbuster3", sendRpc: true);
                    new FakeZeroBuster2Proj(maverick.weapon, shootPos.Value, maverick.xDir, player, player.getNextActorNetId(), rpc: true);
                }
            }

            if (maverick.isAnimOver() || maverick.grounded)
            {
                maverick.changeToIdleOrFall();
            }
        }
    }

    public class FakeZeroShoot2State : MaverickState
    {
        int shootNum;
        int lastShootFrame;
        public FakeZeroShoot2State() : base("shoot2", "")
        {
        }

        public override void update()
        {
            base.update();

            Point? shootPos = maverick.getFirstPOI();

            if (maverick.frameIndex == 4 || maverick.frameIndex == 9)
            {
                maverick.turnToInput(input, player);
            }

            if (maverick.ammo < 8 && maverick.frameIndex == 4)
            {
                maverick.changeState(new MIdle());
                return;
            }

            if (shootPos != null && maverick.frameIndex != lastShootFrame)
            {
                if (shootNum == 0)
                {
                    maverick.deductAmmo(8);
                    maverick.playSound("zbuster3", sendRpc: true);
                    new FakeZeroBuster2Proj(maverick.weapon, shootPos.Value, maverick.xDir, player, player.getNextActorNetId(), rpc: true);
                }
                else if (shootNum == 1)
                {
                    maverick.deductAmmo(8);
                    maverick.playSound("zbuster3", sendRpc: true);
                    new FakeZeroBuster2Proj(maverick.weapon, shootPos.Value, maverick.xDir, player, player.getNextActorNetId(), rpc: true);
                }
                else if (shootNum == 2)
                {
                    maverick.playSound("beamsaber", sendRpc: true);
                    new FakeZeroSwordBeamProj(maverick.weapon, shootPos.Value, maverick.xDir, player, player.getNextActorNetId(), rpc: true);
                }
                shootNum++;
                lastShootFrame = maverick.frameIndex;
            }

            if (maverick.isAnimOver())
            {
                maverick.changeState(new MIdle());
            }
        }
    }

    public class FakeZeroMeleeProj : Projectile
    {
        public FakeZeroMeleeProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 2, player, "fakezero_run_sword", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.FakeZeroMelee;
            setIndestructableProperties();

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (deltaPos.magnitude > FakeZero.topSpeed * Global.spf)
            {
                damager.damage = 4;
                damager.flinch = Global.defFlinch;
            }
            else if (deltaPos.magnitude <= FakeZero.topSpeed * Global.spf && deltaPos.magnitude > 150 * Global.spf)
            {
                damager.damage = 3;
                damager.flinch = Global.halfFlinch;
            }
            else
            {
                damager.damage = 2;
                damager.flinch = 0;
            }
        }
    }

    public class FakeZeroMeleeState : MaverickState
    {
        FakeZeroMeleeProj proj;
        public FakeZeroMeleeState() : base("run_attack", "")
        {
        }

        public override void update()
        {
            base.update();

            var move = new Point(0, 0);
            if (input.isHeld(Control.Left, player))
            {
                if (maverick.xDir != -1)
                {
                    maverick.changeState(new MIdle());
                    return;
                }
                move.x = -maverick.getRunSpeed();
            }
            else if (input.isHeld(Control.Right, player))
            {
                if (maverick.xDir != 1)
                {
                    maverick.changeState(new MIdle());
                    return;
                }
                move.x = maverick.getRunSpeed();
            }
            if (move.magnitude > 0)
            {
                maverick.move(move);
            }
            else
            {
                maverick.changeState(new MIdle());
            }
            groundCode();

            proj.xDir = maverick.xDir;
            proj.changePos(maverick.getFirstPOIOrDefault(1));
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            proj = new FakeZeroMeleeProj(maverick.weapon, maverick.getFirstPOIOrDefault(1), maverick.xDir, player, player.getNextActorNetId(), rpc: true);
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            proj?.destroySelf();
        }
    }

    public class FakeZeroRockProj : Projectile
    {
        public FakeZeroRockProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 3, player, "fakezero_rock", Global.halfFlinch, 0.1f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.FakeZeroGroundPunch;
            maxTime = 1.25f;
            useGravity = true;
            vel = new Point(0, -500);

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }

    public class FakeZeroGuardState : MaverickState
    {
        public FakeZeroGuardState() : base("guard", "")
        {
        }

        public override void update()
        {
            base.update();

            if (!input.isHeld(Control.Down, player))
            {
                maverick.changeToIdleOrFall();
            }
        }
    }

    public class FakeZeroGroundPunchState : MaverickState
    {
        public FakeZeroGroundPunchState() : base("groundpunch", "")
        {
        }

        public override void update()
        {
            base.update();

            if (maverick.frameIndex == 3 && !once)
            {
                maverick.playSound("triadThunderCharged", sendRpc: true);
                maverick.shakeCamera(sendRpc: true);
                once = true;
                Weapon w = maverick.weapon;
                new FakeZeroRockProj(w, maverick.pos.addxy(-15, 0), maverick.xDir, player, player.getNextActorNetId(), rpc: true);
                new FakeZeroRockProj(w, maverick.pos.addxy(15, 0), maverick.xDir, player, player.getNextActorNetId(), rpc: true);

                Global.level.delayedActions.Add(new DelayedAction(() =>
                {
                    new FakeZeroRockProj(w, maverick.pos.addxy(-35, 0), maverick.xDir, player, player.getNextActorNetId(), rpc: true);
                    new FakeZeroRockProj(w, maverick.pos.addxy(35, 0), maverick.xDir, player, player.getNextActorNetId(), rpc: true);
                }, 0.075f));

                Global.level.delayedActions.Add(new DelayedAction(() =>
                {
                    new FakeZeroRockProj(w, maverick.pos.addxy(-55, 0), maverick.xDir, player, player.getNextActorNetId(), rpc: true);
                    new FakeZeroRockProj(w, maverick.pos.addxy(55, 0), maverick.xDir, player, player.getNextActorNetId(), rpc: true);
                }, 0.15f));
            }

            if (maverick.isAnimOver())
            {
                maverick.changeToIdleOrFall();
            }
        }
    }
}
