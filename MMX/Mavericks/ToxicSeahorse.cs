using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline
{
    public class ToxicSeahorse : Maverick
    {
        public static Weapon getWeapon() { return new Weapon(WeaponIds.TSeahorseGeneric, 152); }
        public float teleportCooldown;
        public ToxicSeahorse(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            stateCooldowns.Add(typeof(MShoot), new MaverickStateCooldown(true, true, 1f));
            stateCooldowns.Add(typeof(TSeahorseShoot2State), new MaverickStateCooldown(true, true, 2f));
            stateCooldowns.Add(typeof(TSeahorseTeleportState), new MaverickStateCooldown(false, true, 0.75f));
    
            weapon = getWeapon();

            awardWeaponId = WeaponIds.AcidBurst;
            weakWeaponId = WeaponIds.FrostShield;
            weakMaverickWeaponId = WeaponIds.BlizzardBuffalo;

            spriteToCollider["teleport"] = getDashCollider(1, 0.25f);

            netActorCreateId = NetActorCreateId.ToxicSeahorse;
            netOwner = player;
            if (sendRpc)
            {
                createActorRpc(player.id);
            }
        }

        public override void update()
        {
            base.update();
            Helpers.decrementTime(ref teleportCooldown);

            if (state is not TSeahorseTeleportState)
            {
                rechargeAmmo(4);
            }
            else
            {
                drainAmmo(3);
            }

            if (aiBehavior == MaverickAIBehavior.Control)
            {
                if (state is MIdle || state is MRun)
                {
                    if (input.isPressed(Control.Shoot, player))
                    {
                        changeState(getShootState(false));
                    }
                    else if (input.isPressed(Control.Special1, player))
                    {
                        changeState(new TSeahorseShoot2State());
                    }
                    else if (input.isPressed(Control.Dash, player) && teleportCooldown == 0)
                    {
                        if (ammo >= 8)
                        {
                            deductAmmo(8);
                            changeState(new TSeahorseTeleportState());
                        }
                    }
                }
            }
        }

        public override string getMaverickPrefix()
        {
            return "tseahorse";
        }
    
        public override MaverickState getRandomAttackState()
        {
            return aiAttackStates().GetRandomItem();
        }

        public override MaverickState[] aiAttackStates()
        {
            return new MaverickState[]
            {
                getShootState(false),
                new TSeahorseShoot2State(),
                new TSeahorseTeleportState(),
            };
        }

        public MaverickState getShootState(bool isAI)
        {
            var mshoot = new MShoot((Point pos, int xDir) =>
            {
                // playSound("zbuster2", sendRpc: true);
                new TSeahorseAcidProj(weapon, pos, xDir, player, player.getNextActorNetId(), sendRpc: true);
            }, null);
            if (isAI)
            {
                // mshoot.consecutiveData = new MaverickStateConsecutiveData(0, 4, 0.75f);
            }
            return mshoot;
        }

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            if (sprite.name.Contains("teleport2"))
            {
                return new GenericMeleeProj(weapon, centerPoint, ProjIds.TSeahorseEmerge, player, damage: 4, flinch: Global.defFlinch, hitCooldown: 0.5f);
            }
            else if (sprite.name.Contains("teleport"))
            {
                //return new GenericMeleeProj(weapon, centerPoint, ProjIds.TSeahorsePuddle, player, damage: 0, flinch: 0, hitCooldown: 1f, this);
            }
            return null;
        }
    }

    public class TSeahorseAcidProj : Projectile, IDamagable
    {
        bool firstHit;
        float hitWallCooldown;
        float health = 3;
        public TSeahorseAcidProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, xDir, 0, 2, player, "tseahorse_proj_acid_start", 0, 0.01f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.TSeahorseAcid1;
            maxTime = 2;
            useGravity = false;

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
            checkBigAcidUnderwater();
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            if (sprite.name.EndsWith("_start"))
            {
                if (isAnimOver())
                {
                    changeSprite("tseahorse_proj_acid", true);
                    vel = new Point(xDir * 125, 0);
                    speed = 125;
                }
                return;
            }

            Helpers.decrementTime(ref hitWallCooldown);
            checkBigAcidUnderwater();
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            if (!ownedByLocalPlayer) return;
            if (hitWallCooldown > 0) return;

            bool didHit = false;
            bool wasSideHit = false;
            int moveDirX = MathF.Sign(vel.x);
            if (!firstHit)
            {
                firstHit = true;
                vel.x *= -1;
                vel.y = -speed * 0.25f;
                didHit = true;
                wasSideHit = true;
            }
            else if (other.isSideWallHit())
            {
                vel.x *= -1;
                didHit = true;
                wasSideHit = true;
            }
            else if (other.isCeilingHit() || other.isGroundHit())
            {
                acidSplashEffect(other, ProjIds.TSeahorseAcid1);
                vel.y *= -1;
                didHit = true;
                destroySelf();
            }
            if (didHit)
            {
                //playSound("gbeetleProjBounce", sendRpc: true);
                hitWallCooldown = 0.1f;
                if (wasSideHit)
                {
                    new AcidBurstProjSmall(weapon, other.getHitPointSafe().addxy(-5 * moveDirX, 0), 1, new Point(-moveDirX * 50, 0), ProjIds.TSeahorseAcid1, owner, owner.getNextActorNetId(), rpc: true);
                    new AcidBurstProjSmall(weapon, other.getHitPointSafe().addxy(-5 * moveDirX, 0), 1, new Point(-moveDirX * 100, 0), ProjIds.TSeahorseAcid1, owner, owner.getNextActorNetId(), rpc: true);
                }
            }
        }

        public void applyDamage(Player owner, int? weaponIndex, float damage, int? projId)
        {
            if (!ownedByLocalPlayer) return;
            health -= damage;
            if (health <= 0)
            {
                destroySelf();
                Anim.createGibEffect("tseahorse_acid_gib", pos, owner, gibPattern: GibPattern.Random, sendRpc: true);
            }
        }

        public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) => damagerAlliance != owner.alliance;
        public bool isInvincible(Player attacker, int? projId) => false;
        public bool canBeHealed(int healerAlliance) => false;
        public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) { }
    }

    public class TSeahorseAcid2Proj : Projectile
    {
        int bounces = 0;
        int type;
        bool once;
        public TSeahorseAcid2Proj(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 300, 0, player, "tseahorse_proj_acid", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 4f;
            projId = (int)ProjIds.TSeahorseAcid2;
            useGravity = true;
            fadeSound = "acidBurst";
            vel = new Point(xDir * 112, -235);
            this.type = type;
            if (type == 2) vel = new Point(xDir * 50, -300);

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
            checkBigAcidUnderwater();
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            if (sprite.name == "acidburst_charged_start" && isAnimOver())
            {
                changeSprite("acidburst_charged", true);
                vel.x = xDir * 100;
            }

            checkBigAcidUnderwater();
        }

        public override void onHitWall(CollideData other)
        {
            if (!ownedByLocalPlayer) return;
            acidSplashEffect(other, ProjIds.TSeahorseAcid2);
            bounces++;
            if (bounces > 3)
            {
                destroySelf();
                return;
            }

            var normal = other.hitData.normal ?? new Point(0, -1);

            if (normal.isSideways())
            {
                vel.x *= -1;
                incPos(new Point(5 * MathF.Sign(vel.x), 0));
            }
            else
            {
                if (type == 1 && !once)
                {
                    once = true;
                    vel.x *= -1;
                }
                vel.y *= -1;
                if (vel.y < -300) vel.y = -300;
                incPos(new Point(0, 5 * MathF.Sign(vel.y)));
            }
            playSound("acidBurst", sendRpc: true);
        }

        bool acidSplashOnce;
        public override void onHitDamagable(IDamagable damagable)
        {
            if (ownedByLocalPlayer)
            {
                if (!acidSplashOnce)
                {
                    acidSplashOnce = true;
                    acidSplashParticles(pos, false, 1, 1, ProjIds.TSeahorseAcid2);
                    acidFadeEffect();
                }
            }
            base.onHitDamagable(damagable);
        }
    }

    public class TSeahorseShoot2State : MaverickState
    {
        bool shotOnce;
        public TSeahorseShoot2State() : base("shoot2", "")
        {
            exitOnAnimEnd = true;
        }

        public override void update()
        {
            base.update();

            Point? shootPos = maverick.getFirstPOI();
            if (!shotOnce && shootPos != null)
            {
                shotOnce = true;
                maverick.playSound("acidBurst", sendRpc: true);
                new TSeahorseAcid2Proj(maverick.weapon, shootPos.Value, maverick.xDir, 0, player, player.getNextActorNetId(), rpc: true);
                new TSeahorseAcid2Proj(maverick.weapon, shootPos.Value, maverick.xDir, 1, player, player.getNextActorNetId(), rpc: true);
            }
        }
    }

    public class TSeahorseTeleportState : MaverickState
    {
        int state = 0;
        float shootCooldown;
        public TSeahorseTeleportState() : base("teleport", "")
        {
            enterSound = "tseahorseTeleportOut";
        }

        public override void update()
        {
            base.update();

            if (state == 0)
            {
                if (maverick.frameIndex == maverick.sprite.frames.Count - 1)
                {
                    state = 1;
                    stateTime = 0;
                    maverick.frameIndex = maverick.sprite.frames.Count - 1;
                    maverick.frameSpeed = 0;
                }
            }
            else if (state == 1)
            {
                Helpers.decrementTime(ref shootCooldown);
                maverick.frameSpeed = 0;
                var dir = input.getInputDir(player);
                maverick.turnToInput(input, player);
                if (dir.x != 0)
                {
                    var move = new Point(100 * dir.x, 0);
                    var hitGroundMove = Global.level.checkCollisionActor(maverick, dir.x * 20, 20);
                    if (hitGroundMove == null)
                    {
                    }
                    else
                    {
                        maverick.move(move);
                        maverick.frameSpeed = 1;
                    }
                }

                if (input.isPressed(Control.Dash, player) || maverick.ammo <= 0 || (isAI && stateTime > 1))
                {
                    state = 2;
                    maverick.changeSpriteFromName("teleport2", true);
                    maverick.playSound("tseahorseTeleportIn", sendRpc: true);
                }
            }
            else if (state == 2)
            {
                if (maverick.isAnimOver())
                {
                    maverick.changeToIdleOrFall();
                }
            }
        }
        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            maverick.useGravity = true;
            maverick.angle = 0;
            (maverick as ToxicSeahorse).teleportCooldown = 0.25f;
        }
    }
}
