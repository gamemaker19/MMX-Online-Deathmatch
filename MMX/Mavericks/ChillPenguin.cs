using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class ChillPenguin : Maverick
    {
        public ChillPIceShotWeapon iceShotWeapon = new ChillPIceShotWeapon();
        public ChillPIceStatueWeapon iceStatueWeapon = new ChillPIceStatueWeapon();
        public ChillPIceBlowWeapon iceWindWeapon = new ChillPIceBlowWeapon();
        public ChillPBlizzardWeapon blizzardWeapon = new ChillPBlizzardWeapon();
        public ChillPSlideWeapon slideWeapon;

        public ChillPenguin(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) : 
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            slideWeapon = new ChillPSlideWeapon(player);
            stateCooldowns.Add(typeof(ChillPIceBlowState), new MaverickStateCooldown(true, false, 2f));
            stateCooldowns.Add(typeof(ChillPSlideState), new MaverickStateCooldown(true, false, 0.5f));
            stateCooldowns.Add(typeof(ChillPBlizzardState), new MaverickStateCooldown(false, false, 3f));
            stateCooldowns.Add(typeof(MShoot), new MaverickStateCooldown(false, true, 0.75f));
            spriteToCollider.Add("slide", getDashCollider());

            weapon = new Weapon(WeaponIds.ChillPGeneric, 93);

            awardWeaponId = WeaponIds.ShotgunIce;
            weakWeaponId = WeaponIds.FireWave;
            weakMaverickWeaponId = WeaponIds.FlameMammoth;

            netActorCreateId = NetActorCreateId.ChillPenguin;
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
                if (state is MTaunt && input.isHeld(Control.Taunt, player))
                {
                    if (input.isPressed(Control.Down, player) || input.isPressed(Control.Up, player))
                    {
                        player.removeOwnedIceStatues();
                    }
                    else if (input.isPressed(Control.Left, player) || input.isPressed(Control.Right, player))
                    {
                        if (player.iceStatues.Count <= 1)
                        {
                            player.removeOwnedIceStatues();
                        }
                        else
                        {
                            var sortedStatues = player.iceStatues.OrderBy(ic => ic.pos.x).ToList();
                            var leftStatue = sortedStatues[0];
                            var rightStatue = sortedStatues[1];
                            if (input.isPressed(Control.Left, player))
                            {
                                leftStatue.destroySelf();
                            }
                            else
                            {
                                rightStatue.destroySelf();
                            }
                        }
                    }
                }
                if (state is MIdle || state is MRun)
                {
                    if (shootPressed())
                    {
                        changeState(getShootState(false));
                    }
                    else if (specialPressed())
                    {
                        changeState(new ChillPIceBlowState());
                    }
                    else if (input.isPressed(Control.Dash, player))
                    {
                        changeState(new ChillPSlideState(false));
                    }
                }
                else if (state is MJump || state is MFall)
                {
                    if (input.isHeld(Control.Special1, player))
                    {
                        var hit = Global.level.checkCollisionActor(this, 0, -ChillPBlizzardState.switchSpriteHeight - 5);
                        if (vel.y < 0 && hit?.gameObject is Wall wall && !wall.topWall)
                        {
                            changeState(new ChillPBlizzardState(false));
                        }
                    }
                }
            }
        }

        public override string getMaverickPrefix()
        {
            return "chillp";
        }

        public MaverickState getShootState(bool isAI)
        {
            var mshoot = new MShoot((Point pos, int xDir) =>
            {
                new ChillPIceProj(iceShotWeapon, pos, xDir, player, input.isHeld(Control.Down, player) ? 1 : 0, player.getNextActorNetId(), rpc: true);
            }, null);
            if (isAI)
            {
                mshoot.consecutiveData = new MaverickStateConsecutiveData(0, 4, 0.5f);
            }
            return mshoot;
        }

        public override MaverickState[] aiAttackStates()
        {
            return new MaverickState[]
            {
                getShootState(true),
                new ChillPIceBlowState(),
                new ChillPSlideState(true),
            };
        }

        public override MaverickState getRandomAttackState()
        {
            var attacks = new MaverickState[]
            {
                getShootState(true),
                new ChillPIceBlowState(),
                //new ChillPSlideState(true),
                //new ChillPBlizzardState(true),
            };
            return attacks.GetRandomItem();
        }

        /*
        public override void onHitboxHit(Collider attackHitbox, CollideData collideData)
        {
            var damagable = collideData.gameObject as IDamagable;
            if (isSlidingAndCanDamage() && damagable != null && damagable.canBeDamaged(player.alliance, player.id, null))
            {
                slideWeapon.applyDamage(damagable, false, this, (int)ProjIds.ChillPSlide);
            }
        }
        */

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            Projectile proj = null;
            if (isSlidingAndCanDamage())
            {
                proj = new GenericMeleeProj(slideWeapon, centerPoint, ProjIds.ChillPSlide, player);
            }

            return proj;
        }

        public bool isSlidingAndCanDamage()
        {
            return sprite.name.EndsWith("slide") && MathF.Abs(deltaPos.x) > 1.66f;
        }
    }

    #region weapons
    public class ChillPIceShotWeapon : Weapon
    {
        public ChillPIceShotWeapon()
        {
            index = (int)WeaponIds.ChillPIceShot;
            killFeedIndex = 93;
        }
    }

    public class ChillPIceBlowWeapon : Weapon
    {
        public ChillPIceBlowWeapon()
        {
            index = (int)WeaponIds.ChillPIceBlow;
            killFeedIndex = 93;
        }
    }

    public class ChillPIceStatueWeapon : Weapon
    {
        public ChillPIceStatueWeapon()
        {
            index = (int)WeaponIds.ChillPIcePenguin;
            killFeedIndex = 93;
        }
    }

    public class ChillPBlizzardWeapon : Weapon
    {
        public ChillPBlizzardWeapon()
        {
            index = (int)WeaponIds.ChillPBlizzard;
            killFeedIndex = 93;
        }
    }

    public class ChillPSlideWeapon : Weapon
    {
        public ChillPSlideWeapon(Player player)
        {
            index = (int)WeaponIds.ChillPSlide;
            killFeedIndex = 93;
            damager = new Damager(player, 3, Global.defFlinch, 0.75f);
        }
    }

    #endregion

    #region projectiles
    public class ChillPIceProj : Projectile
    {
        public int type = 0;
        public Character hitChar;
        public ChillPIceProj(Weapon weapon, Point pos, int xDir, Player player, int type, ushort netProjId, Character hitChar = null, bool rpc = false) : 
            base(weapon, pos, xDir, 250, 3, player, "chillp_proj_ice", 0, 0.01f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.ChillPIceShot;
            maxTime = 0.75f;
            this.hitChar = hitChar;
            this.type = type;
            collider.wallOnly = true;
            isShield = true;
            if (type == 1)
            {
                useGravity = true;
                vel.x *= 0.75f;
                vel.y = -50;
                maxTime = 1;
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

        public override void onHitDamagable(IDamagable damagable)
        {
            base.onHitDamagable(damagable);
            onHit();
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            if (type == 1 && other.hitData.normal != null)
            {
                Point normal = other.hitData.normal.Value;
                if (normal.y != 0 && normal.x == 0)
                {
                    vel.y *= -0.5f;
                    return;
                }
            }
            onHit();
            destroySelf();
        }

        bool hit;
        public void onHit()
        {
            if (!ownedByLocalPlayer) return;
            if (hit) return;
            hit = true;
            Func<float> yVel = () => Helpers.randomRange(-150, -50); 
            var pieces = new List<Anim>()
            {
                new Anim(pos.addxy(-5, 0), "chillp_anim_ice_piece", 1, owner.getNextActorNetId(), false, sendRpc: true)
                {
                    vel = new Point(-50, yVel())
                },
                new Anim(pos.addxy(-5, 0), "chillp_anim_ice_piece", 1, owner.getNextActorNetId(), false, sendRpc: true)
                {
                    vel = new Point(-100, yVel())
                },
                new Anim(pos.addxy(5, 0), "chillp_anim_ice_piece", 1, owner.getNextActorNetId(), false, sendRpc: true)
                {
                    vel = new Point(50, yVel())
                },
                new Anim(pos.addxy(5, 0), "chillp_anim_ice_piece", 1, owner.getNextActorNetId(), false, sendRpc: true)
                {
                    vel = new Point(100, yVel())
                },
            };
            foreach (var piece in pieces)
            {
                piece.frameSpeed = 0;
                piece.useGravity = true;
                piece.ttl = 1f;
            }

            playSound("freezebreak2", sendRpc: true);
        }
    }


    public class ChillPIceStatueProj : Projectile, IDamagable
    {
        public float health = 8;
        public float maxHealth = 8;

        public ChillPIceStatueProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, xDir, 0, 2, player, "chillp_proj_statue", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.ChillPIcePenguin;
            fadeSound = "iceBreak";
            shouldShieldBlock = false;
            destroyOnHit = true;
            
            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();

            updateProjectileCooldown();

            if (!ownedByLocalPlayer)
            {
                // This is to sync the damage in FTD
                if (sprite.frameIndex == sprite.frames.Count - 1)
                {
                    damager.damage = 4;
                }
                return;
            }

            if (sprite.isAnimOver())
            {
                useGravity = true;
                collider.wallOnly = true;
                damager.flinch = Global.defFlinch;
                damager.damage = 4;
            }
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (!ownedByLocalPlayer) return;
            if (other.gameObject is Wall && other.hitData?.normal != null && other.hitData.normal.Value.isSideways() && !deltaPos.isZero() && MathF.Abs(deltaPos.x) > 0.1f)
            {
                destroySelf();
            }
            else if (other.gameObject is ChillPenguin cp && cp.isSlidingAndCanDamage() && cp.player == owner)
            {
                destroySelf();
            }

        }

        public override void onDestroy()
        {
            breakFreeze(owner, pos.addxy(0, -16));
            owner.iceStatues.RemoveAll(i => i == this);
        }

        public void applyDamage(Player owner, int? weaponIndex, float damage, int? projId)
        {
            health -= damage;
            if (health <= 0)
            {
                destroySelf();
            }
        }

        public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId)
        {
            return owner.alliance != damagerAlliance;
        }

        public bool isInvincible(Player attacker, int? projId)
        {
            return false;
        }

        public bool canBeHealed(int healerAlliance)
        {
            return false;
        }

        public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false)
        {
        }
    }

    public class ChillPBlizzardProj : Projectile
    {
        const float pushSpeed = 150;
        public ChillPBlizzardProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 0, player, "chillp_wind", 0, 0f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.ChillPBlizzard;
            shouldShieldBlock = false;
            destroyOnHit = false;
            shouldVortexSuck = false;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (sprite.loopCount > 30)
            {
                destroySelf();
            }
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (other.gameObject is ChillPIceStatueProj iceStatue && iceStatue.owner == owner && iceStatue.isAnimOver())
            {
                iceStatue.move(new Point(pushSpeed * xDir, 0));
            }
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            base.onHitDamagable(damagable);
            if (damagable is not Character character) return;
            if (character.charState.invincible) return;
            if (character.charState.immuneToWind) return;
            if (immuneToKnockback) return;
            if (character.isCCImmune()) return;

            float modifier = 1;
            if (character.grounded) modifier = 0.5f;
            if (character.charState is Crouch) modifier = 0.25f;
            character.move(new Point(pushSpeed * xDir * modifier, 0));
            character.pushedByTornadoInFrame = true;
        }
    }
    #endregion

    #region states
    public class ChillPIceBlowState : MaverickState
    {
        float shootTime;
        bool soundOnce;
        bool statueOnce;
        public ChillPIceBlowState() : base("blow", "")
        {
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            Helpers.decrementTime(ref shootTime);

            if (shootTime == 0)
            {
                Point? shootPos = maverick.getFirstPOI();
                if (shootPos != null)
                {
                    shootTime = 0.1f;
                    new ShotgunIceProjCharged((maverick as ChillPenguin).iceWindWeapon, shootPos.Value, maverick.xDir, player, 1, true, player.getNextActorNetId(), rpc: true);
                    if (!soundOnce)
                    {
                        soundOnce = true;
                        maverick.playSound("icyWind", sendRpc: true);
                    }
                }
            }

            if (stateTime > 0.25f && !statueOnce)
            {
                var iceStatuePos1 = new Point(maverick.pos.x + maverick.xDir * 35, maverick.pos.y - 5);
                var iceStatuePos2 = new Point(maverick.pos.x + maverick.xDir * 65, maverick.pos.y - 5);

                statueOnce = true;
                if (player.iceStatues.Count == 0)
                {
                    addIceStatueIfSpace(iceStatuePos1);
                    addIceStatueIfSpace(iceStatuePos2);
                }
                else if (player.iceStatues.Count == 1)
                {
                    var existingIceStatue = player.iceStatues[0];
                    if (existingIceStatue.pos.distanceTo(iceStatuePos1) > existingIceStatue.pos.distanceTo(iceStatuePos2))
                    {
                        addIceStatueIfSpace(iceStatuePos1);
                    }
                    else
                    {
                        addIceStatueIfSpace(iceStatuePos2);
                    }
                }
            }

            if (maverick.sprite.loopCount > 12)
            {
                maverick.changeState(new MIdle());
            }
        }

        public void addIceStatueIfSpace(Point pos)
        {
            player.iceStatues.Add(new ChillPIceStatueProj((maverick as ChillPenguin).iceStatueWeapon, pos, maverick.xDir, player, player.getNextActorNetId(), sendRpc: true));
            /*
            var rect = new Rect(pos.addxy(-14, -32), pos.addxy(14, 0));
            if (Global.level.checkCollisionShape(rect.getShape(), null) == null)
            {
                player.iceStatues.Add(new ChillPIceStatueProj((maverick as ChillPenguin).iceStatueWeapon, pos, maverick.xDir, player, player.getNextActorNetId(), sendRpc: true));
            }
            */
        }
    }

    public class ChillPBlizzardState : MaverickState
    {
        Point? switchPos;
        int state;
        new bool isAI;
        public const float switchSpriteHeight = 60;
        public ChillPBlizzardState(bool isAI) : base("jump", "")
        {
            this.isAI = isAI;
        }

        public override bool canEnter(Maverick maverick)
        {
            var ceiling = Global.level.raycast(maverick.pos, maverick.pos.addxy(0, -175), new List<Type> { typeof(Wall) });
            if (ceiling?.hitData?.hitPoint != null)
            {
                switchPos = ceiling.hitData.hitPoint.Value;
            }

            if (switchPos == null)
            {
                return false;
            }

            return base.canEnter(maverick);
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);

            if (!isAI)
            {
                state = 1;
            }
            else
            {
                maverick.vel.y = -maverick.getJumpPower() * 1.75f;
            }
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
                if (maverick.pos.y - switchSpriteHeight <= switchPos.Value.y + 5)
                {
                    state = 1;
                }
            }
            else if (state == 1)
            {
                if (!maverick.sprite.name.Contains("switch"))
                {
                    maverick.changeSpriteFromName("switch", true);
                    maverick.changePos(new Point(maverick.pos.x, switchPos.Value.y + switchSpriteHeight));
                }
                maverick.useGravity = false;
                maverick.stopMoving();
                if (!once && maverick.frameIndex == 3)
                {
                    once = true;
                    float topY = Global.level.getTopScreenY(maverick.pos.y);
                    if (player.isPuppeteer() && player.currentMaverick == maverick) topY = maverick.pos.y - 80;
                    new ChillPBlizzardProj((maverick as ChillPenguin).blizzardWeapon, new Point(maverick.pos.x, topY), maverick.xDir, player, player.getNextActorNetId(), rpc: true);
                    maverick.playSound("chillpBlizzard", sendRpc: true);
                }
                if (maverick.sprite.isAnimOver())
                {
                    maverick.changeState(new MFall());
                }
            }
        }
    }

    public class ChillPSlideState : MaverickState
    {
        public float slideTime;
        float slideSpeed = 350;
        const float timeBeforeSlow = 0.75f;
        const float slowTime = 0.5f;
        bool soundOnce;
        public ChillPSlideState(bool isAI) : base("slide", "")
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
            if (!maverick.isAnimOver()) return;

            if (!soundOnce)
            {
                soundOnce = true;
                maverick.playSound("chillpSlide", sendRpc: true);
            }

            slideTime += Global.spf;

            Point moveAmount = new Point(maverick.xDir * slideSpeed, 0);
            var hitWall = checkCollisionSlide(moveAmount.x * Global.spf * 2, -2);
            if (hitWall?.isSideWallHit() == true)
            {
                maverick.xDir *= -1;
                moveAmount.x *= -1;
            }
            maverick.move(moveAmount);

            var inputDir = input.getInputDir(player);
            if (inputDir.x != 0 && MathF.Sign(inputDir.x) != MathF.Sign(maverick.xDir))
            {
                slideTime += Global.spf;
            }

            if (input.isPressed(Control.Jump, player) && maverick.grounded && canDamageOrJump())
            {
                maverick.vel.y = -maverick.getJumpPower() * 0.75f;
            }
            if (!maverick.grounded && maverick.vel.y < 0 && Global.level.checkCollisionActor(maverick, 0, maverick.vel.y * Global.spf * 2) != null)
            {
                maverick.vel.y = 0;
            }

            if (slideTime >= timeBeforeSlow)
            {
                float perc = 1 - ((slideTime - timeBeforeSlow) / slowTime);
                slideSpeed = 300 * perc;
                if (slideSpeed <= 1)
                {
                    maverick.changeState(new MIdle());
                }
            }
        }

        public bool canDamageOrJump()
        {
            return slideTime > 0 && slideTime < (timeBeforeSlow + slowTime) * 0.75f;
        }
    }

    public class ChillPBurnState : MaverickState
    {
        Point pushDir;
        public ChillPBurnState() : base("burn")
        {
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            maverick.move(pushDir);

            if (stateTime > 0.5f)
            {
                maverick.changeState(new MIdle());
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.playSound("chillpBurn", sendRpc: true);
            pushDir = new Point(-maverick.xDir * 75, 0);
            maverick.vel.y = -100;
        }
    }

    #endregion
}
