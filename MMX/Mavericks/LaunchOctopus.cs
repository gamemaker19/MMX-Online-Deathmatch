using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class LaunchOctopus : Maverick
    {
        public LaunchOMissileWeapon missileWeapon = new LaunchOMissileWeapon();
        public LaunchODrainWeapon meleeWeapon;
        public LaunchOHomingTorpedoWeapon homingTorpedoWeapon = new LaunchOHomingTorpedoWeapon();
        public bool lastFrameWasUnderwater;

        public LaunchOctopus(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            meleeWeapon = new LaunchODrainWeapon(player);

            stateCooldowns.Add(typeof(MShoot), new MaverickStateCooldown(false, true, 1f));
            stateCooldowns.Add(typeof(LaunchOShoot), new MaverickStateCooldown(false, true, 0.325f));
            stateCooldowns.Add(typeof(LaunchOHomingTorpedoState), new MaverickStateCooldown(false, true, 1.5f));
            stateCooldowns.Add(typeof(LaunchOWhirlpoolState), new MaverickStateCooldown(false, false, 2));

            weapon = new Weapon(WeaponIds.LaunchOGeneric, 96);

            awardWeaponId = WeaponIds.Torpedo;
            weakWeaponId = WeaponIds.RollingShield;
            weakMaverickWeaponId = WeaponIds.ArmoredArmadillo;

            netActorCreateId = NetActorCreateId.LaunchOctopus;
            netOwner = player;
            if (sendRpc)
            {
                createActorRpc(player.id);
            }
        }

        float timeBeforeRecharge;
        public override void update()
        {
            base.update();

            if (state is not LaunchOShoot)
            {
                Helpers.decrementTime(ref timeBeforeRecharge);
                if (timeBeforeRecharge == 0)
                {
                    rechargeAmmo(4.5f);
                }
            }
            else
            {
                timeBeforeRecharge = 1;
            }

            if (aiBehavior == MaverickAIBehavior.Control)
            {
                if (state is MIdle || state is MRun)
                {
                    if (shootPressed())
                    {
                        if (ammo > 0)
                        {
                            changeState(new LaunchOShoot(grounded));
                        }
                    }
                    else if (specialPressed())
                    {
                        changeState(new LaunchOHomingTorpedoState());
                    }
                }
                else if (state is MJump || state is MFall)
                {
                    if (input.isPressed(Control.Shoot, player))
                    {
                        if (ammo > 0)
                        {
                            changeState(new LaunchOShoot(grounded));
                        }
                    }
                    else if (input.isPressed(Control.Dash, player) && getDistFromGround() > 75)
                    {
                        changeState(new LaunchOWhirlpoolState());
                    }
                }

                if ((state is MJump || state is MFall) && !grounded)
                {
                    if (isUnderwater())
                    {
                        if (input.isHeld(Control.Jump, player) && vel.y > -106)
                        {
                            vel.y = -106;
                        }
                    }
                    else
                    {
                        if (lastFrameWasUnderwater && input.isHeld(Control.Jump, player) && input.isHeld(Control.Up, player))
                        {
                            vel.y = -425;
                        }
                    }
                }
            }

            lastFrameWasUnderwater = isUnderwater();
        }

        public override string getMaverickPrefix()
        {
            return "launcho";
        }

        public override MaverickState[] aiAttackStates()
        {
            return new MaverickState[]
            {
                new LaunchOShoot(grounded),
                new LaunchOHomingTorpedoState(),
                new LaunchOWhirlpoolState(),
            };
        }

        public override MaverickState getRandomAttackState()
        {
            var attacks = new MaverickState[]
            {
                new LaunchOShoot(grounded),
                new LaunchOWhirlpoolState(),
                new LaunchOHomingTorpedoState(),
            };
            return attacks.GetRandomItem();
        }

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            if (sprite.name.Contains("launcho_spin"))
            {
                return new GenericMeleeProj(meleeWeapon, centerPoint, ProjIds.LaunchODrain, player, damage: 0, flinch: 0, hitCooldown: 0, owningActor: this);
            }
            return null;
        }
    }

    #region weapons
    public class LaunchOMissileWeapon : Weapon
    {
        public LaunchOMissileWeapon()
        {
            index = (int)WeaponIds.LaunchOMissile;
            killFeedIndex = 96;
        }
    }

    public class LaunchOWhirlpoolWeapon : Weapon
    {
        public LaunchOWhirlpoolWeapon()
        {
            index = (int)WeaponIds.LaunchOWhirlpool;
            killFeedIndex = 96;
        }
    }

    public class LaunchODrainWeapon : Weapon
    {
        public LaunchODrainWeapon(Player player)
        {
            index = (int)WeaponIds.LaunchOMelee;
            killFeedIndex = 96;
            damager = new Damager(player, 3, Global.defFlinch, 0.5f);
        }
    }

    public class LaunchOHomingTorpedoWeapon : Weapon
    {
        public LaunchOHomingTorpedoWeapon()
        {
            index = (int)WeaponIds.LaunchOHomingTorpedo;
            killFeedIndex = 96;
        }
    }
    #endregion

    #region projectiles
    public class LaunchOMissile : Projectile, IDamagable
    {
        public float smokeTime = 0;
        public LaunchOMissile(Weapon weapon, Point pos, int xDir, Player player, Point unitVel, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 100, 3, player, "launcho_proj_missile", 0, 0.15f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.LaunchOMissle;
            maxTime = 0.75f;
            fadeSprite = "explosion";
            fadeSound = "explosion";
            vel = unitVel.times(speed);
            vel.x *= xDir;
            reflectable2 = true;
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            updateProjectileCooldown();

            if (MathF.Abs(vel.x) < 300)
            {
                vel.x += Global.spf * 300 * xDir;
            }

            smokeTime += Global.spf;
            if (smokeTime > 0.2)
            {
                smokeTime = 0;
                new Anim(pos, "torpedo_smoke", 1, null, true);
            }
        }

        public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) { return owner.alliance != damagerAlliance; }
        public bool canBeHealed(int healerAlliance) { return false; }
        public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) { }
        public bool isInvincible(Player attacker, int? projId) { return false; }
        public void applyDamage(Player owner, int? weaponIndex, float damage, int? projId)
        {
            if (damage > 0)
            {
                destroySelf();
            }
        }
    }

    public class LaunchOWhirlpoolProj : Projectile
    {
        Player player;
        public LaunchOWhirlpoolProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, 1, 0, 0, player, "launcho_whirlpool", 0, 0.25f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.LaunchOWhirlpool;
            shouldShieldBlock = false;
            shouldVortexSuck = false;
            destroyOnHit = false;
            this.player = player;

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;
        }

        /*
        public override bool shouldDealDamage(IDamagable damagable)
        {
            if (damagable is Actor actor && MathF.Abs(pos.x - actor.pos.x) > 40)
            {
                return false;
            }
            return true;
        }
        */

        public override void onHitDamagable(IDamagable damagable)
        {
            base.onHitDamagable(damagable);
            if (damagable is Character chr)
            {
                float modifier = 1;
                if (chr.isUnderwater()) modifier = 2;
                if (chr.isImmuneToKnockback()) return;
                float xMoveVel = MathF.Sign(pos.x - chr.pos.x);
                chr.move(new Point(xMoveVel * 100 * modifier, 0));
            }
        }
    }
    #endregion

    #region states
    public class LaunchOShoot : MaverickState
    {
        int shootState;
        bool isGrounded;
        float afterShootTime;
        public LaunchOShoot(bool isGrounded) : base(isGrounded ? "shoot" : "air_shoot", "")
        {
            this.isGrounded = isGrounded;
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (isGrounded && !maverick.grounded)
            {
                sprite = "air_shoot";
                maverick.changeSpriteFromName(sprite, true);
                isGrounded = false;
            }
            else if (!isGrounded && maverick.grounded)
            {
                maverick.changeState(new MIdle());
                return;
            }

            if (!isGrounded)
            {
                airCode();
            }

            var lo = maverick as LaunchOctopus;
            Point? shootPos = lo.getFirstPOI();
            if (shootState == 0 && shootPos != null)
            {
                shootState = 1;
                maverick.playSound("torpedo", sendRpc: true);
                if (maverick.ammo >= 1) new LaunchOMissile(lo.missileWeapon, shootPos.Value.addxy(0, -3), lo.xDir, player, new Point(1, -0.2f), player.getNextActorNetId(), rpc: true);
                if (maverick.ammo >= 2) new LaunchOMissile(lo.missileWeapon, shootPos.Value.addxy(0, 0), lo.xDir, player, new Point(1, -0.05f), player.getNextActorNetId(), rpc: true);
                if (maverick.ammo >= 3) new LaunchOMissile(lo.missileWeapon, shootPos.Value.addxy(0, 5), lo.xDir, player, new Point(1, 0.25f), player.getNextActorNetId(), rpc: true);
                maverick.ammo -= 3;
                if (maverick.ammo < 0) maverick.ammo = 0;
            }

            if (shootState == 1 && (isAI || input.isPressed(Control.Shoot, player)))
            {
                maverick.frameSpeed = 0;
                shootState = 2;
            }

            if (shootState == 2)
            {
                afterShootTime += Global.spf;
                if (afterShootTime > 0.325f)
                {
                    shootState = 3;
                    maverick.frameSpeed = 1;
                    sprite += "2";
                    maverick.changeSpriteFromName(sprite, true);
                    maverick.playSound("torpedo", sendRpc: true);
                    shootPos = lo.getFirstPOI();
                    if (maverick.ammo >= 1) new LaunchOMissile(lo.missileWeapon, shootPos.Value.addxy(0, -3), lo.xDir, player, new Point(1, -0.2f), player.getNextActorNetId(), rpc: true);
                    if (maverick.ammo >= 2) new LaunchOMissile(lo.missileWeapon, shootPos.Value.addxy(0, 0), lo.xDir, player, new Point(1, -.05f), player.getNextActorNetId(), rpc: true);
                    if (maverick.ammo >= 3) new LaunchOMissile(lo.missileWeapon, shootPos.Value.addxy(0, 5), lo.xDir, player, new Point(1, 0.25f), player.getNextActorNetId(), rpc: true);
                    maverick.ammo -= 3;
                    if (maverick.ammo < 0) maverick.ammo = 0;
                }
            }
            
            if (maverick.ammo == 0 || lo.isAnimOver())
            {
                if (maverick.grounded) lo.changeState(new MIdle());
                else lo.changeState(new MFall());
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

    public class LaunchOHomingTorpedoState : MaverickState
    {
        public bool shootOnce;
        public LaunchOHomingTorpedoState() : base("ht", "")
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

            if (maverick.frameIndex == 3 && !shootOnce)
            {
                shootOnce = true;
                maverick.playSound("torpedo", sendRpc: true);
                var pois = maverick.currentFrame.POIs;
                var lo = (maverick as LaunchOctopus);

                new TorpedoProj(lo.homingTorpedoWeapon, lo.pos.add(pois[0]), 1, player, 3, player.getNextActorNetId(), 0, rpc: true);
                new TorpedoProj(lo.homingTorpedoWeapon, lo.pos.add(pois[1]), 1, player, 3, player.getNextActorNetId(), 0, rpc: true);
                new TorpedoProj(lo.homingTorpedoWeapon, lo.pos.add(pois[2]), 1, player, 3, player.getNextActorNetId(), 180, rpc: true);
                new TorpedoProj(lo.homingTorpedoWeapon, lo.pos.add(pois[3]), 1, player, 3, player.getNextActorNetId(), 180, rpc: true);
            }

            if (maverick.isAnimOver())
            {
                maverick.changeState(new MIdle());
            }
        }
    }

    public class LaunchOWhirlpoolState : MaverickState
    {
        public bool shootOnce;
        LaunchOWhirlpoolProj whirlpool;
        float whirlpoolSoundTime;
        int initYDir = 1;
        public LaunchOWhirlpoolState() : base("spin", "")
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
            maverick.useGravity = false;
            whirlpool = new LaunchOWhirlpoolProj(new LaunchOWhirlpoolWeapon(), maverick.pos.addxy(0, isAI ? -100 : 25), 1, player, player.getNextActorNetId(), sendRpc: true);
            maverick.playSound("launchoWhirlpool", sendRpc: true);
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (!maverick.tryMove(new Point(0, 100 * initYDir), out CollideData hit))
            {
                if (initYDir == 1)
                {
                    maverick.unstickFromGround();
                }
                initYDir *= -1;
            }

            whirlpoolSoundTime += Global.spf;
            if (whirlpoolSoundTime > 0.5f)
            {
                whirlpoolSoundTime = 0;
                maverick.playSound("launchoWhirlpool", sendRpc: true);
            }

            if (stateTime > 2f)
            {
                maverick.changeState(new MFall());
            }
        }
        
        public override bool trySetGrabVictim(Character grabbed)
        {
            maverick.changeState(new LaunchODrainState(grabbed), true);
            return true;
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            whirlpool.destroySelf();
            maverick.useGravity = true;
        }
    }

    public class LaunchODrainState : MaverickState
    {
        Character victim;
        float soundTime = 1;
        float leechTime = 0.5f;
        public bool victimWasGrabbedSpriteOnce;
        float timeWaiting;
        public LaunchODrainState(Character grabbedChar) : base("drain", "")
        {
            this.victim = grabbedChar;
        }

        public override void update()
        {
            base.update();
            if (player == null) return;
            leechTime += Global.spf;

            if (victimWasGrabbedSpriteOnce && !victim.sprite.name.EndsWith("_grabbed"))
            {
                maverick.changeState(new MFall(), true);
                return;
            }

            if (victim.sprite.name.EndsWith("_grabbed") || victim.sprite.name.EndsWith("_die"))
            {
                victimWasGrabbedSpriteOnce = true;
            }
            if (!victimWasGrabbedSpriteOnce)
            {
                timeWaiting += Global.spf;
                if (timeWaiting > 1)
                {
                    victimWasGrabbedSpriteOnce = true;
                }
                if (maverick.isDefenderFavored())
                {
                    if (leechTime > 0.5f)
                    {
                        leechTime = 0;
                        maverick.addHealth(2, true);
                    }
                    return;
                }
            }

            if (leechTime > 0.5f)
            {
                leechTime = 0;
                maverick.addHealth(2, true);
                var damager = new Damager(player, 2, 0, 0);
                damager.applyDamage(victim, false, new LaunchODrainWeapon(player), maverick, (int)ProjIds.LaunchODrain);
            }

            soundTime += Global.spf;
            if (soundTime > 1f)
            {
                soundTime = 0;
                maverick.playSound("launchoDrain", sendRpc: true);
            }

            if (stateTime > 4f)
            {
                maverick.changeState(new MFall());
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
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            maverick.useGravity = true;
            victim?.releaseGrab(maverick);
            var whirlpoolCooldown = maverick.stateCooldowns[typeof(LaunchOWhirlpoolState)];
            whirlpoolCooldown.cooldown = whirlpoolCooldown.maxCooldown;
        }
    }

    public class WhirlpoolGrabbed : GenericGrabbedState
    {
        public const float maxGrabTime = 4;
        public WhirlpoolGrabbed(LaunchOctopus grabber) : base(grabber, maxGrabTime, "_drain")
        {
        }
    }

    #endregion
}
