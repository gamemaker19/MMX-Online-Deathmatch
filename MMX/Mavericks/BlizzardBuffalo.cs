using SFML.Graphics;
using System;
using System.Collections.Generic;

namespace MMXOnline
{
    public class BlizzardBuffalo : Maverick
    {
        public static Weapon getWeapon() { return new Weapon(WeaponIds.BBuffaloGeneric, 151); }
        public static Weapon getMeleeWeapon(Player player) { return new Weapon(WeaponIds.BBuffaloGeneric, 151, new Damager(player, 4, Global.defFlinch, 1)); }

        public Weapon meleeWeapon;
        public BlizzardBuffalo(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            stateCooldowns.Add(typeof(MShoot), new MaverickStateCooldown(false, true, 0.75f));
            stateCooldowns.Add(typeof(BBuffaloDashState), new MaverickStateCooldown(false, true, 1.25f));
            stateCooldowns.Add(typeof(BBuffaloShootBeamState), new MaverickStateCooldown(false, false, 2));

            isHeavy = true;
            spriteFrameToSounds["bbuffalo_run/2"] = "walkStomp";
            spriteFrameToSounds["bbuffalo_run/6"] = "walkStomp";

            weapon = getWeapon();
            meleeWeapon = getMeleeWeapon(player);

            awardWeaponId = WeaponIds.FrostShield;
            weakWeaponId = WeaponIds.ParasiticBomb;
            weakMaverickWeaponId = WeaponIds.BlastHornet;

            netActorCreateId = NetActorCreateId.BlizzardBuffalo;
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
                    if (input.isPressed(Control.Shoot, player))
                    {
                        changeState(getShootState(false));
                    }
                    else if (input.isPressed(Control.Special1, player))
                    {
                        changeState(new BBuffaloShootBeamState());
                    }
                    else if (input.isPressed(Control.Dash, player))
                    {
                        changeState(new BBuffaloDashState());
                    }
                }
                else if (state is MJump || state is MFall)
                {
                }
            }
        }

        public override float getRunSpeed()
        {
            return 75;
        }

        public override string getMaverickPrefix()
        {
            return "bbuffalo";
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
                new BBuffaloShootBeamState(),
                new BBuffaloDashState(),
            };
        }

        public MaverickState getShootState(bool isAI)
        {
            var mshoot = new MShoot((Point pos, int xDir) =>
            {
                playSound("bbuffaloShoot", sendRpc: true);

                Point unitDir = new Point(xDir, -1);
                var inputDir = input.getInputDir(player);
                if (inputDir.y == -1) unitDir.y = -2;
                if (inputDir.y == 1) unitDir.y = 0;
                if (inputDir.x == xDir) unitDir.x = xDir * 2;
                int shootFramesHeld = (state as MShoot)?.shootFramesHeld ?? 0;
                float speedModifier = Helpers.clamp((shootFramesHeld + 3) / 10f, 0.5f, 1.5f);

                new BBuffaloIceProj(weapon, pos, xDir, unitDir.normalize(), speedModifier, player, player.getNextActorNetId(), sendRpc: true);
            }, null);
            if (isAI)
            {
                mshoot.consecutiveData = new MaverickStateConsecutiveData(0, 4, 0.75f);
            }
            return mshoot;
        }

        public float getStompDamage()
        {
            float damagePercent = 0;
            if (deltaPos.y > 150 * Global.spf) damagePercent = 0.5f;
            if (deltaPos.y > 225 * Global.spf) damagePercent = 0.75f;
            if (deltaPos.y > 300 * Global.spf) damagePercent = 1;
            return damagePercent;
        }

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            if (sprite.name.EndsWith("_dash"))
            {
                return new GenericMeleeProj(weapon, centerPoint, ProjIds.BBuffaloDrag, player, damage: 0, flinch: 0, hitCooldown: 0.5f, owningActor: this);
            }
            if (sprite.name.Contains("fall"))
            {
                float damagePercent = getStompDamage();
                if (damagePercent > 0)
                {
                    return new GenericMeleeProj(weapon, centerPoint, ProjIds.BBuffaloStomp, player, damage: 4 * damagePercent, flinch: Global.defFlinch, hitCooldown: 0.5f);
                }
            }
            return null;
        }

        public override void updateProjFromHitbox(Projectile proj)
        {
            if (sprite.name.EndsWith("fall"))
            {
                float damagePercent = getStompDamage();
                if (damagePercent > 0)
                {
                    proj.damager.damage = 4 * damagePercent;
                }
            }
        }
    }

    public class BBuffaloIceProj : Projectile
    {
        public BBuffaloIceProj(Weapon weapon, Point pos, int xDir, Point unitDir, float speedModifier, Player player, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, xDir, 150, 3, player, "bbuffalo_proj_iceball", Global.defFlinch, 0.01f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.BBuffaloIceProj;
            maxTime = 1.5f;
            useGravity = true;

            vel = new Point(unitDir.x * 200 * speedModifier, unitDir.y * 250 * speedModifier);

            collider.wallOnly = true;
            destroyOnHit = true;

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void onStart()
        {
            base.onStart();
            if (!ownedByLocalPlayer) return;

            if (checkCollision(0, 0) != null)
            {
                destroySelf();
                Anim.createGibEffect("bbuffalo_proj_ice_gibs", getCenterPos(), owner, sendRpc: true);
                playSound("iceBreak", sendRpc: true);
            }
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            if (!ownedByLocalPlayer) return;

            var hitNormal = other.getNormalSafe();
            destroySelf();
            new BBuffaloIceProjGround(weapon, other.getHitPointSafe(), hitNormal.angle, owner, owner.getNextActorNetId(), sendRpc: true);
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            base.onHitDamagable(damagable);
            if (!ownedByLocalPlayer) return;

            destroySelf();
            Anim.createGibEffect("bbuffalo_proj_ice_gibs", getCenterPos(), owner, sendRpc: true);
            playSound("iceBreak", sendRpc: true);
        }
    }

    public class BBuffaloIceProjGround : Projectile, IDamagable
    {
        float health = 6;
        public BBuffaloIceProjGround(Weapon weapon, Point pos, float angle, Player player, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, 1, 0, 3, player, "bbuffalo_proj_ice", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            maxTime = 5;
            projId = (int)ProjIds.BBuffaloIceProjGround;
            destroyOnHit = true;
            playSound("frostShield");
            this.angle = angle;
            updateHitboxes();
            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            updateProjectileCooldown();

            if (ownedByLocalPlayer)
            {
                moveWithMovingPlatform();
            }

            updateHitboxes();
        }

        public void updateHitboxes()
        {
            if (angle == null || collider?._shape == null) return;

            float angle360 = Helpers.to360(angle.Value);
            if (angle360 >= 0 && angle360 <= 45)
            {
                collider._shape.points = new List<Point>()
                {
                    new Point(-9, 0),
                    new Point(26, 0),
                    new Point(26, 30),
                    new Point(-9, 30),
                };
            }
            else if (angle360 > 45 && angle360 <= 135)
            {
                collider._shape.points = new List<Point>()
                {
                    new Point(0 - 12, 0 + 12),
                    new Point(30 - 12, 0 + 12),
                    new Point(30 - 12, 35 + 12),
                    new Point(0 - 12, 35 + 12),
                };
            }
            else if (angle360 > 135 && angle360 <= 225)
            {
                collider._shape.points = new List<Point>()
                {
                    new Point(-9 - 18, 0),
                    new Point(26 - 18, 0),
                    new Point(26 - 18, 30),
                    new Point(-9 - 18, 30),
                };
            }
            else if (angle360 > 225)
            {
                collider._shape.points = new List<Point>()
                {
                    new Point(0 - 12, 0 - 12),
                    new Point(30 - 12, 0 - 12),
                    new Point(30 - 12, 35 - 12),
                    new Point(0 - 12, 35 - 12),
                };
            }
        }

        public void applyDamage(Player owner, int? weaponIndex, float damage, int? projId)
        {
            if (!ownedByLocalPlayer) return;
            health -= damage;
            if (health <= 0)
            {
                destroySelf();
            }
        }

        public bool canBeHealed(int healerAlliance) { return false; }
        public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) { }
        public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId)
        {
            return damagerAlliance != owner.alliance;
        }
        public bool isInvincible(Player attacker, int? projId)
        {
            return false;
            //if (projId == null) return true;
            //return !Damager.canDamageFrostShield(projId.Value);
        }

        public override void onDestroy()
        {
            base.onDestroy();
            Anim.createGibEffect("bbuffalo_proj_ice_gibs", getCenterPos(), owner);
            playSound("iceBreak");
        }
    }

    public class BBuffaloBeamProj : Projectile
    {
        public Point startPos;
        public BlizzardBuffalo bb;
        public bool released;
        public float moveDistance2;
        public float maxDistance2;
        public BBuffaloBeamProj(Weapon weapon, Point pos, int xDir, BlizzardBuffalo bb, Player player, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, xDir, 150, 0, player, "bbuffalo_proj_beam_head", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.BBuffaloBeam;
            setStartPos(pos.addxy(-xDir * 10, 0));
            maxDistance2 = 200;
            this.bb = bb;
            setIndestructableProperties();

            if (sendRpc)
            {
                byte[] bbNetIdBytes = BitConverter.GetBytes(bb.netId ?? 0);
                rpcCreate(pos, player, netProjId, xDir, bbNetIdBytes[0], bbNetIdBytes[1]);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;
            if (bb == null) return;

            moveDistance2 += speed * Global.spf;
            
            if (!released)
            {
                setStartPos(startPos.add(bb.deltaPos));
                if (moveDistance2 > maxDistance2)
                {
                    release();
                }
            }
            else
            {
                setStartPos(startPos.addxy(xDir * speed * Global.spf, 0));
                if (moveDistance2 > maxDistance2)
                {
                    destroySelf();
                }
            }
        }

        public void release()
        {
            if (released) return;
            released = true;
            vel = Point.zero;
            maxDistance2 = moveDistance2;
            moveDistance2 = 0;
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            if (!ownedByLocalPlayer) return;
            release();
        }

        public void setStartPos(Point startPos)
        {
            this.startPos = startPos;
            globalCollider = new Collider(getPoints(), true, null, false, false, 0, Point.zero);
        }

        public List<Point> getPoints()
        {
            var points = new List<Point>();
            if (xDir == 1)
            {
                points.Add(new Point(startPos.x, startPos.y - 24));
                points.Add(new Point(pos.x, pos.y - 24));
                points.Add(new Point(pos.x, pos.y + 24));
                points.Add(new Point(startPos.x, startPos.y + 24));
            }
            else
            {
                points.Add(new Point(pos.x, pos.y - 24));
                points.Add(new Point(startPos.x, startPos.y - 24));
                points.Add(new Point(startPos.x, startPos.y + 24));
                points.Add(new Point(pos.x, pos.y + 24));
            }

            return points;
        }

        public override void render(float x, float y)
        {
            if (globalCollider?.shape.points == null) return;

            var color = new Color(184, 248, 248);
            DrawWrappers.DrawPolygon(getPoints(), color, true, bb?.zIndex ?? zIndex);

            base.render(x, y);
            Global.sprites["bbuffalo_proj_beam_head"].draw(0, startPos.x, startPos.y, -xDir, 1, null, 1, 1, 1, zIndex);
        }
    }

    public class BBuffaloShootBeamState : MaverickState
    {
        bool shotOnce;
        public Anim muzzle;
        public BBuffaloBeamProj proj;
        public BBuffaloShootBeamState() : base("shoot_beam", "shoot_beam_start")
        {
            //enterSound = "bbuffaloBeamStart";
        }

        public override void update()
        {
            base.update();
            if (inTransition()) return;

            Point? shootPos = maverick.getFirstPOI();
            if (!shotOnce && shootPos != null)
            {
                shotOnce = true;
                muzzle = new Anim(shootPos.Value, "bbuffalo_beam_muzzle", maverick.xDir, player.getNextActorNetId(), true, sendRpc: true, host: maverick);
                maverick.playSound("bbuffaloBeam", sendRpc: true);
            }

            if (proj != null && !proj.destroyed && input.isPressed(Control.Special1, player))
            {
                proj.release();
            }

            if (muzzle?.destroyed == true && proj == null)
            {
                proj = new BBuffaloBeamProj(maverick.weapon, shootPos.Value.addxy(maverick.xDir * 20, 0), maverick.xDir, maverick as BlizzardBuffalo, player, player.getNextActorNetId(), sendRpc: true);
            }

            if (proj?.released == true || proj?.destroyed == true)
            {
                maverick.changeToIdleOrFall();
            }
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            muzzle?.destroySelf();
            proj?.release();
        }
    }

    public class BBuffaloDashState : MaverickState
    {
        float dustTime;
        Character victim;
        public BBuffaloDashState() : base("dash", "dash_start")
        {
        }

        public override void update()
        {
            base.update();

            if (inTransition()) return;

            Helpers.decrementTime(ref dustTime);
            if (dustTime == 0)
            {
                new Anim(maverick.pos.addxy(-maverick.xDir * 30, 0), "dust", maverick.xDir, null, true) { vel = new Point(0, -25) };
                dustTime = 0.1f;
            }

            if (!player.ownedByLocalPlayer) return;

            var move = new Point(150 * maverick.xDir, 0);

            var hitGround = Global.level.checkCollisionActor(maverick, move.x * Global.spf * 5, 20);
            if (hitGround == null)
            {
                maverick.changeToIdleOrFall();
                return;
            }

            var hitWall = Global.level.checkCollisionActor(maverick, maverick.xDir * 20, -5);
            if (hitWall?.isSideWallHit() == true)
            {
                crashAndDamage();
                maverick.playSound("crash", sendRpc: true);
                maverick.shakeCamera(sendRpc: true);
                maverick.changeState(new MIdle());
                return;
            }

            maverick.move(move);

            if (isHoldStateOver(1, 4, 2, Control.Dash))
            {
                maverick.changeToIdleOrFall();
                return;
            }
        }

        public Character getVictim()
        {
            if (victim == null) return null;
            if (!victim.sprite.name.EndsWith("_grabbed"))
            {
                return null;
            }
            return victim;
        }

        public void crashAndDamage()
        {
            /*
            if (getVictim() != null)
            {
                (maverick as BlizzardBuffalo).meleeWeapon.applyDamage(victim, false, maverick, (int)ProjIds.BBuffaloCrash, sendRpc: true);
            }
            */

            new BBuffaloCrashProj(maverick.weapon, maverick.pos, maverick.xDir, player, player.getNextActorNetId(), rpc: true);
            maverick.playSound("crash", sendRpc: true);
            maverick.shakeCamera(sendRpc: true);
        }

        public override bool trySetGrabVictim(Character grabbed)
        {
            if (victim == null)
            {
                victim = grabbed;
                maverick.changeSpriteFromName("dash_grab", false);
            }
            return true;
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            victim?.releaseGrab(maverick);
        }
    }

    public class BBuffaloCrashProj : Projectile
    {
        public BBuffaloCrashProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 4, player, "bbuffalo_proj_crash", Global.defFlinch, 1, netProjId, player.ownedByLocalPlayer)
        {
            setIndestructableProperties();
            maxTime = 0.15f;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }

    public class BBuffaloDragged : GenericGrabbedState
    {
        public const float maxGrabTime = 4;
        public BBuffaloDragged(BlizzardBuffalo grabber) :
            base(grabber, maxGrabTime, "_dash", reverseZIndex: true, freeOnHitWall: false, lerp: true, additionalGrabSprite: "_dash_grab")
        {
        }
    }
}
