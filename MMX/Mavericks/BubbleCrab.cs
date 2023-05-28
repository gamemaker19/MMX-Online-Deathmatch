using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class BubbleCrab : Maverick
    {
        public static Weapon getWeapon() { return new Weapon(WeaponIds.BCrabGeneric, 143); }

        public BCrabShieldProj shield;
        public List<BCrabSummonCrabProj> crabs = new List<BCrabSummonCrabProj>();
        public bool lastFrameWasUnderwater;

        float clawSoundTime;

        public BubbleCrab(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            //stateCooldowns.Add(typeof(BCrabShieldStartState), new MaverickStateCooldown(false, true, 0.75f));

            weapon = getWeapon();

            awardWeaponId = WeaponIds.BubbleSplash;
            weakWeaponId = WeaponIds.SpinWheel;
            weakMaverickWeaponId = WeaponIds.WheelGator;

            netActorCreateId = NetActorCreateId.BubbleCrab;
            netOwner = player;
            if (sendRpc)
            {
                createActorRpc(player.id);
            }
        }

        public override void update()
        {
            base.update();

            Helpers.decrementTime(ref clawSoundTime);
            if (sprite.name.Contains("jump_attack"))
            {
                if (clawSoundTime == 0)
                {
                    clawSoundTime = 0.03f;
                    playSound("bcrabClaw");
                }
            }

            if (!ownedByLocalPlayer) return;

            if (sprite.name.Contains("jump_attack"))
            {
                if (shield != null)
                {
                    shield.destroySelf();
                    shield = null;
                }
            }

            if (shield != null)
            {
                if (shield.destroyed)
                {
                    shield = null;
                }
                else
                {
                    //shield.changePos(getFirstPOIOrDefault("shield"));
                    shield.changePos(getCenterPos());
                }
            }

            if (shield == null)
            {
                rechargeAmmo(1);
            }

            bool floating = false;
            if (aiBehavior == MaverickAIBehavior.Control)
            {
                if (state is MIdle || state is MRun)
                {
                    if (input.isPressed(Control.Shoot, player))
                    {
                        changeState(new BCrabShootState());
                    }
                    else if (input.isPressed(Control.Special1, player))
                    {
                        changeState(getSpecialState());
                    }
                    else if (input.isPressed(Control.Dash, player))
                    {
                        changeState(new BCrabClawState());
                    }
                }
                else if (state is MJump || state is MFall)
                {
                    if (input.isPressed(Control.Dash, player))
                    {
                        changeState(new BCrabClawJumpState());
                    }
                }

                float floatY = -150;
                if ((state is MJump || state is MFall) && !grounded)
                {
                    if (isUnderwater() && shield != null)
                    {
                        if (input.isHeld(Control.Jump, player) && vel.y > floatY)
                        {
                            vel.y = floatY;
                            if (state is MFall)
                            {
                                floating = true;
                                changeSpriteFromName("jump", true);
                            }
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

            if (!floating && state is MFall && vel.y > 0)
            {
                changeSpriteFromName("fall", true);
            }

            lastFrameWasUnderwater = isUnderwater() && shield != null;
        }

        public MaverickState getSpecialState()
        {
            if (shield == null && ammo >= 8)
            {
                return new BCrabShieldStartState();
            }
            else if (crabs.Count < 3)
            {
                return new BCrabSummonState();
            }
            else
            {
                return null;
            }
        }

        public override string getMaverickPrefix()
        {
            return "bcrab";
        }

        public override MaverickState[] aiAttackStates()
        {
            return new MaverickState[]
            {
                new BCrabShootState(),
                getSpecialState(),
                new BCrabClawState(),
            };
        }

        public override MaverickState getRandomAttackState()
        {
            return aiAttackStates().GetRandomItem();
        }

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            if (sprite.name.Contains("jump_attack"))
            {
                return new GenericMeleeProj(weapon, centerPoint, ProjIds.BCrabClaw, player, 1, Global.defFlinch, 0.15f);
            }
            return null;
        }

        public override void onDestroy()
        {
            base.onDestroy();
            shield?.destroySelf();
            foreach (var crab in crabs.ToList())
            {
                crab?.destroySelf();
            }
            crabs.Clear();
        }
    }

    public class BCrabBubbleSplashProj : Projectile
    {
        bool once;
        int num;
        int type;
        public BCrabBubbleSplashProj(Weapon weapon, Point pos, int xDir, int num, int type, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 0, player, "bcrab_bubble_ring_start", 0, 0.15f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.BCrabBubbleSplash;
            this.num = num;
            this.type = type;
            fadeSprite = "bcrab_bubble_ring_poof";
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

            if (type == 1 && once)
            {
                vel.y -= Global.spf * 500;
                speed = vel.magnitude;
                if (vel.y < -150)
                {
                    vel.y = -150;
                }
            }

            if (isAnimOver() && !once)
            {
                once = true;
                changeSprite("bcrab_bubble_ring", true);
                if (type == 0)
                {
                    vel = new Point(xDir * 200, Helpers.randomRange(num == 0 ? -25 : -50, 0));
                }
                else
                {
                    vel = new Point(xDir * 150, 0);
                }
                speed = vel.magnitude;
                maxDistance = 150;
                updateDamager(2);
                destroyOnHit = true;
            }
        }
    }

    public class BCrabShootState : MaverickState
    {
        bool secondAnim;
        float shootCooldown;
        int num;
        public BCrabShootState() : base("ring_attack_start", "")
        {
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (maverick.frameIndex == 0)
            {
                maverick.turnToInput(input, player);
            }

            Point? shootPos = maverick.getFirstPOI("bubble_ring");
            Helpers.decrementTime(ref shootCooldown);
            if (shootCooldown == 0 && shootPos != null)
            {
                shootCooldown = 0.25f;
                num = (num == 1 ? 0 : 1);
                maverick.playSound("bcrabShoot", sendRpc: true);
                int type = input.isHeld(Control.Up, player) ? 1 : 0;
                new BCrabBubbleSplashProj(maverick.weapon, shootPos.Value, maverick.xDir, num, type, player, player.getNextActorNetId(), rpc: true);
            }

            if (!secondAnim)
            {
                if (maverick.isAnimOver())
                {
                    maverick.changeSpriteFromName("ring_attack", true);
                    secondAnim = true;
                }
            }
            else
            {
                if (isAI)
                {
                    if (maverick.loopCount >= 4)
                    {
                        maverick.changeState(new MIdle());
                    }
                }
                else if (maverick.loopCount >= 4)
                {
                    maverick.changeState(new MIdle());
                }
                else if (maverick.loopCount >= 1 && !input.isHeld(Control.Shoot, player))
                {
                    maverick.changeState(new MIdle());
                }
            }
        }
    }

    public class BCrabClawState : MaverickState
    {
        public BCrabClawState() : base("jump_attack_start", "")
        {
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            maverick.turnToInput(input, player);
            genericJumpCode();
            if (!maverick.grounded)
            {
                maverick.changeState(new BCrabClawJumpState());
                return;
            }

            if (isAI)
            {
                if (stateTime > 2)
                {
                    maverick.changeToIdleOrFall();
                }
            }
            else if (!input.isHeld(Control.Dash, player))
            {
                maverick.changeToIdleOrFall();
                return;
            }
        }
    }

    public class BCrabClawJumpState : MaverickState
    {
        public BCrabClawJumpState() : base("jump_attack", "")
        {
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            airCode();
            if (maverick.grounded)
            {
                maverick.changeState(new BCrabClawState());
                return;
            }

            if (isAI)
            {
                if (stateTime > 1)
                {
                    maverick.changeToIdleOrFall();
                }
            }
            else if (!input.isHeld(Control.Dash, player))
            {
                maverick.changeToIdleOrFall();
                return;
            }
        }
    }

    public class BCrabShieldProj : Projectile, IDamagable
    {
        public float health = 8;
        public bool once;
        public BCrabShieldProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 0, player, "bcrab_shield", 0, 1, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.BCrabBubbleShield;
            syncScale = true;
            yScale = 0;
            setIndestructableProperties();
            if (player.character != null) setzIndex(player.character.zIndex - 100);
            alpha = 0.75f;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            updateProjectileCooldown();

            if (!ownedByLocalPlayer) return;

            if (!once)
            {
                if (yScale < 1)
                {
                    yScale += Global.spf * 2;
                    if (yScale > 1)
                    {
                        once = true;
                        yScale = 1;
                    }
                }
            }
            else
            {
                updateBubbleBounce();
            }
        }

        public void applyDamage(Player owner, int? weaponIndex, float damage, int? projId)
        {
            if (!ownedByLocalPlayer) return;

            if (projId == (int)ProjIds.SpinWheel || projId == (int)ProjIds.SpinWheelCharged || projId == (int)ProjIds.WheelGSpinWheel) damage = health;
            health -= damage;
            if (health <= 0)
            {
                destroySelf();
            }
        }

        public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) { return damagerAlliance != owner.alliance; }
        public bool canBeHealed(int healerAlliance) { return false; }
        public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) { }
        public bool isInvincible(Player attacker, int? projId) { return false; }
    }

    public class BCrabShieldStartState : MaverickState
    {
        public BCrabShieldStartState() : base("shield_start", "")
        {
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (!once)
            {
                Point? shootPos = maverick.getFirstPOI("shield");
                if (!once && shootPos != null)
                {
                    once = true;
                    maverick.deductAmmo(8);
                    maverick.playSound("bcrabShield", sendRpc: true);
                    var shield = new BCrabShieldProj(maverick.weapon, shootPos.Value, maverick.xDir, player, player.getNextActorNetId(), rpc: true);
                    (maverick as BubbleCrab).shield = shield;
                }
            }

            if (maverick.isAnimOver())
            {
                maverick.changeToIdleOrFall();
            }
        }
    }

    public class BCrabSummonBubbleProj : Projectile, IDamagable
    {
        float health = 2;
        public BCrabSummonBubbleProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 0, player, "bcrab_summon_bubble", 0, 1, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.BCrabCrablingBubble;
            setIndestructableProperties();
            syncScale = true;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            updateBubbleBounce();
        }

        public void applyDamage(Player owner, int? weaponIndex, float damage, int? projId)
        {
            if (!ownedByLocalPlayer) return;

            if (projId == (int)ProjIds.SpinWheel || projId == (int)ProjIds.SpinWheelCharged || projId == (int)ProjIds.WheelGSpinWheel) damage = health;
            health -= damage;
            if (health <= 0)
            {
                destroySelf();
            }
        }

        public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) { return damagerAlliance != owner.alliance; }
        public bool canBeHealed(int healerAlliance) { return false; }
        public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) { }
        public bool isInvincible(Player attacker, int? projId) { return false; }
    }

    public class BCrabSummonCrabProj : Projectile, IDamagable
    {
        float health = 2;
        int? moveDirOnce = null;
        BCrabSummonBubbleProj shield;
        BubbleCrab maverick;
        public BCrabSummonCrabProj(Weapon weapon, Point pos, Point vel, BubbleCrab maverick, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, 1, 0, 2, player, "bcrab_summon_crab", Global.halfFlinch, 1, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.BCrabCrabling;
            this.vel = vel;
            this.maverick = maverick;
            fadeSprite = "explosion";
            fadeSound = "explosion";
            collider.wallOnly = true;
            useGravity = true;
            netcodeOverride = NetcodeModel.FavorDefender;
            setIndestructableProperties();

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void onStart()
        {
            base.onStart();
            if (!ownedByLocalPlayer) return;
            shield = new BCrabSummonBubbleProj(maverick.weapon, pos, xDir, owner, owner.getNextActorNetId(), rpc: true);
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            if (shield != null)
            {
                if (shield.destroyed) shield = null;
                else shield.changePos(pos);
            }
            else
            {
                patrol();
            }
        }

        public void patrol()
        {
            var closestTarget = Global.level.getClosestTarget(pos, owner.alliance, false, 150, true);
            if (closestTarget != null)
            {
                if (moveDirOnce == null) moveDirOnce = MathF.Sign(closestTarget.pos.x - pos.x);
                var hitGround = Global.level.checkCollisionActor(this, moveDirOnce.Value * 30, 20);
                var hitWall = Global.level.checkCollisionActor(this, moveDirOnce.Value * Global.spf * 2, -5);
                bool blocked = (grounded && hitGround == null) || hitWall?.isSideWallHit() == true;
                if (!blocked)
                {
                    move(new Point(moveDirOnce.Value * 100, 0));
                }
                else
                {
                    moveDirOnce = null;
                }
            }
        }

        int bounces;
        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            if (!ownedByLocalPlayer) return;

            if (shield == null)
            {
                if (other.isGroundHit())
                {
                    stopMoving();
                }
            }
            else
            {
                bounces++;
                if (bounces >= 2)
                {
                    stopMoving();
                    return;
                }

                var normal = other.hitData.normal ?? new Point(0, -1);
                if (normal.isSideways())
                {
                    vel.x *= -0.5f;
                    shield.startShieldBounceX();
                    incPos(new Point(5 * MathF.Sign(vel.x), 0));
                }
                else
                {
                    vel.y *= -0.5f;
                    shield.startShieldBounceY();
                    if (vel.y < -300) vel.y = -300;
                    incPos(new Point(0, 5 * MathF.Sign(vel.y)));
                }
            }
        }

        public override void onDestroy()
        {
            base.onDestroy();
            if (!ownedByLocalPlayer) return;
            maverick.crabs.Remove(this);
            shield?.destroySelf();
        }

        public void applyDamage(Player owner, int? weaponIndex, float damage, int? projId)
        {
            if (!ownedByLocalPlayer) return;

            if (projId == (int)ProjIds.SpinWheel || projId == (int)ProjIds.SpinWheelCharged || projId == (int)ProjIds.WheelGSpinWheel) damage = health;
            health -= damage;
            if (health <= 0)
            {
                destroySelf();
            }
        }

        public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) { return damagerAlliance != owner.alliance; }
        public bool canBeHealed(int healerAlliance) { return false; }
        public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) { }
        public bool isInvincible(Player attacker, int? projId) { return false; }
    }

    public class BCrabSummonState : MaverickState
    {
        public BCrabSummonState() : base("summon", "")
        {
        }

        public override void update()
        {
            base.update();
            if (player == null) return;
            
            Point? shootPos = maverick.getFirstPOI("summon_crab");
            if (!once && shootPos != null)
            {
                once = true;
                var bc = maverick as BubbleCrab;
                if (bc.crabs.Count < 3) bc.crabs.Add(new BCrabSummonCrabProj(maverick.weapon, shootPos.Value, new Point(-50, -300), bc, player, player.getNextActorNetId(), rpc: true));
                if (bc.crabs.Count < 3) bc.crabs.Add(new BCrabSummonCrabProj(maverick.weapon, shootPos.Value, new Point(0, -300), bc, player, player.getNextActorNetId(), rpc: true));
                if (bc.crabs.Count < 3) bc.crabs.Add(new BCrabSummonCrabProj(maverick.weapon, shootPos.Value, new Point(50, -300), bc, player, player.getNextActorNetId(), rpc: true));
            }

            if (maverick.isAnimOver())
            {
                maverick.changeToIdleOrFall();
            }
        }
    }
}
