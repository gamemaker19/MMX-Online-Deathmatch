using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class MagnaCentipede : Maverick
    {
        public static Weapon getWeapon() { return new Weapon(WeaponIds.MagnaCGeneric, 147); }

        public const float constHeight = 50;
        public MagnaCMagnetMineParent magnetMineParent;
        public bool noTail;
        public float teleportCooldown;

        public MagnaCentipede(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            stateCooldowns.Add(typeof(MagnaCShootState), new MaverickStateCooldown(false, true, 0.5f));
            stateCooldowns.Add(typeof(MagnaCMagnetPullState), new MaverickStateCooldown(true, false, 2f));
            stateCooldowns.Add(typeof(MagnaCDrainState), new MaverickStateCooldown(true, false, 2f));

            weapon = getWeapon();

            awardWeaponId = WeaponIds.MagnetMine;
            weakWeaponId = WeaponIds.SilkShot;
            weakMaverickWeaponId = WeaponIds.MorphMoth;

            netActorCreateId = NetActorCreateId.MagnaCentipede;
            netOwner = player;
            if (sendRpc)
            {
                createActorRpc(player.id);
            }
        }

        bool shootHeldLastFrame;
        float shootHeldTime;
        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            Helpers.decrementTime(ref teleportCooldown);

            if (state is not MagnaCMagnetMineState)
            {
                magnetMineParent?.comeBack();
            }

            if (state is not MagnaCTeleportState && !reversedGravity)
            {
                rechargeAmmo(4);
            }

            if (reversedGravity)
            {
                drainAmmo(1);
                if (ammo <= 0)
                {
                    changeState(new MagnaCCeilingStartState());
                    return;
                }
            }

            if (pos.y < 0 && reversedGravity && state is not MagnaCCeilingStartState)
            {
                changeState(new MagnaCCeilingStartState());
                return;
            }

            if (aiBehavior == MaverickAIBehavior.Control)
            {
                if (state is MIdle || state is MRun)
                {
                    if (shootHeldTime > 0.2f && magnetMineParent == null && !noTail)
                    {
                        shootHeldTime = 0;
                        changeState(new MagnaCMagnetMineState());
                    }
                    else if (shootHeldLastFrame && !input.isHeld(Control.Shoot, player))
                    {
                        changeState(new MagnaCShootState());
                    }
                    else if (input.isPressed(Control.Special1, player) && !noTail)
                    {
                        changeState(new MagnaCMagnetPullState());
                    }
                    else if (input.isPressed(Control.Dash, player) && ammo >= 8 && teleportCooldown == 0)
                    {
                        deductAmmo(8);
                        changeState(new MagnaCTeleportState());
                    }
                }
                else if (state is MJump || state is MFall)
                {
                    if (input.isPressed(Control.Special1, player))
                    {
                        if (reversedGravity)
                        {
                            changeState(new MagnaCCeilingStartState());
                            return;
                        }

                        for (int i = 1; i <= 4; i++)
                        {
                            CollideData collideData = Global.level.checkCollisionActor(this, 0, -10 * i * getYMod(), autoVel: true);
                            if (collideData != null && collideData.gameObject is Wall wall && !wall.isMoving && !wall.topWall && collideData.isCeilingHit())
                            {
                                changeState(new MagnaCCeilingStartState());
                                return;
                            }
                        }
                    }
                }
            }

            if (state is MIdle || state is MRun)
            {
                shootHeldLastFrame = input.isHeld(Control.Shoot, player);
                if (shootHeldLastFrame)
                {
                    shootHeldTime += Global.spf;
                }
                else
                {
                    shootHeldTime = 0;
                }
            }
            else
            {
                shootHeldLastFrame = false;
                shootHeldTime = 0;
            }
        }

        public override float getRunSpeed()
        {
            return 200;
        }

        public override float getDashSpeed()
        {
            return 1;
        }

        public override string getMaverickPrefix()
        {
            return "magnac";
        }

        public override MaverickState[] aiAttackStates()
        {
            var attacks = new List<MaverickState>
            {
                new MagnaCShootState(),
                new MagnaCTeleportState(),
            };
            if (!noTail) attacks.Insert(1, new MagnaCMagnetPullState());
            return attacks.ToArray();
        }

        public override MaverickState getRandomAttackState()
        {
            return aiAttackStates().GetRandomItem();
        }

        public override void onDestroy()
        {
            base.onDestroy();
            magnetMineParent?.destroySelf();
        }

        public void removeTail()
        {
            noTail = true;
            changeSpriteFromName(state.sprite, false);
            Anim.createGibEffect("magnac_tail_gibs", getFirstPOIOrDefault(), player, gibPattern: GibPattern.SemiCircle, sendRpc: true);
        }

        public void setNoTail(bool val)
        {
            noTail = val;
            changeSpriteFromName(state.sprite, false);
        }

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            Projectile proj = null;
            if (sprite.name.EndsWith("_drain") && ((reversedGravity && hitbox.name == "flipped") || (!reversedGravity && hitbox.name != "flipped")))
            {
                var meleeProj = new GenericMeleeProj(weapon, getFirstPOIOrDefault(), ProjIds.MagnaCTail, player, damage: 0, flinch: 0, hitCooldown: 0, owningActor: this);
                var rect = new Rect(0, 0, 10, 10);
                meleeProj.globalCollider = new Collider(rect.getPoints(), true, meleeProj, false, false, HitboxFlag.Hitbox, Point.zero);
                return meleeProj;
            }
            return proj;
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (other.gameObject is Wall wall && wall.topWall && reversedGravity)
            {
                changeState(new MagnaCCeilingStartState());
            }
        }

        public void setTeleportCooldown()
        {
            teleportCooldown = 0.15f;
        }
    }

    public class MagnaCShurikenProj : Projectile
    {
        public float angleDist = 0;
        public float turnDir = -1;
        public Pickup pickup;
        public float maxSpeed = 250;
        public MagnaCShurikenProj(Weapon weapon, Point pos, int xDir, Point velDir, Player player, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, xDir, 0, 2, player, "magnac_shuriken", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.MagnaCShuriken;
            maxTime = 1f;
            vel = velDir.times(maxSpeed);
            angle = velDir.angle;

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            if (time > 0.15f)
            {
                var angInc = (-xDir * turnDir) * Global.spf * 200;
                angle += angInc;
                angleDist += MathF.Abs(angInc);
                vel.x = Helpers.cosd((float)angle) * maxSpeed;
                vel.y = Helpers.sind((float)angle) * maxSpeed;
            }
        }
    }

    public class MagnaCShootState : MaverickState
    {
        bool shotOnce;
        public MagnaCShootState() : base("shuriken_throw", "")
        {
        }

        public override void update()
        {
            base.update();

            Point? shootPos = maverick.getFirstPOI(0);
            Point? shootPos2 = maverick.getFirstPOI(1);
            Point? shootPos3 = maverick.getFirstPOI(2);
            if (!shotOnce && shootPos != null)
            {
                shotOnce = true;
                maverick.playSound("magnacShoot", sendRpc: true);
                new MagnaCShurikenProj(maverick.weapon, shootPos.Value, maverick.xDir, new Point(0, -maverick.getYMod()), player, player.getNextActorNetId(), sendRpc: true);
                new MagnaCShurikenProj(maverick.weapon, shootPos2.Value, maverick.xDir, new Point(maverick.xDir, -maverick.getYMod()), player, player.getNextActorNetId(), sendRpc: true);
                new MagnaCShurikenProj(maverick.weapon, shootPos3.Value, maverick.xDir, new Point(maverick.xDir, 0), player, player.getNextActorNetId(), sendRpc: true);
            }

            if (maverick.isAnimOver())
            {
                maverick.changeState(new MIdle());
            }
        }
    }

    public class MagnaCMagnetMineProj : Projectile
    {
        public float spinAngle;
        public Point originalOffset;
        public MagnaCMagnetMineProj(Weapon weapon, Point pos, Point originalOffset, float spinAngle, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, 1, 0, 3, player, "magnac_tail_part", Global.defFlinch, 1f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.MagnaCMagnetMine;
            setIndestructableProperties();
            this.spinAngle = spinAngle;
            this.originalOffset = originalOffset;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }

    public class MagnaCMagnetMineParent : Actor
    {
        public List<MagnaCMagnetMineProj> pieces = new List<MagnaCMagnetMineProj>();
        MagnaCentipede maverick;
        public float radius;
        public float targetRadius = 30;
        public bool isComingBack;
        public Point originOffset;
        float lerpProgress;
        public Actor target;
        public float range = 60;
        public bool controlled;

        public float stabTime;
        public float stabInvert = 1;  //-1 to 1
        public float stabInvertTime;
        public bool isStabInverting;
        public float stabInvertDest;
        public MagnaCMagnetMineParent(Point pos, MagnaCentipede maverick, bool ownedByLocalPlayer) : base("empty", pos, null, ownedByLocalPlayer, false)
        {
            this.maverick = maverick;
            useGravity = false;
            originOffset = pos.subtract(maverick.pos);

            var player = maverick.player;
            pieces.Add(new MagnaCMagnetMineProj(maverick.weapon, pos, new Point(-5 * maverick.xDir, -5), 0, player, player.getNextActorNetId(), rpc: true));
            pieces.Add(new MagnaCMagnetMineProj(maverick.weapon, pos, new Point(0, 0), 90, player, player.getNextActorNetId(), rpc: true));
            pieces.Add(new MagnaCMagnetMineProj(maverick.weapon, pos, new Point(5 * maverick.xDir, 5), 180, player, player.getNextActorNetId(), rpc: true));
            pieces.Add(new MagnaCMagnetMineProj(maverick.weapon, pos, new Point(10 * maverick.xDir, 10), 270, player, player.getNextActorNetId(), rpc: true));
        }

        public void enableDamager(bool enable)
        {
            /*
            foreach (var piece in pieces)
            {
                piece.updateDamager(enable ? 4 : 0);
            }
            */
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            Helpers.decrementTime(ref stabTime);
            if (stabTime == 0)
            {
                stabTime = 1;
                stabInvertTime = Global.spf;
                if (target != null)
                {
                    isStabInverting = true;
                    stabInvertDest = stabInvert == 1 ? -1 : 1;
                    enableDamager(true);
                }
            }

            if (isStabInverting)
            {
                if (stabInvertDest == 1)
                {
                    stabInvert += Global.spf * 10;
                    if (stabInvert >= 1)
                    {
                        stabInvert = 1;
                        isStabInverting = false;
                        enableDamager(false);
                    }
                }
                else
                {
                    stabInvert -= Global.spf * 10;
                    if (stabInvert <= -1)
                    {
                        stabInvert = -1;
                        isStabInverting = false;
                        enableDamager(false);
                    }
                }
            }

            foreach (var piece in pieces)
            {
                //piece.incPos(deltaPos);
                float radiusInvert = radius * stabInvert;
                float xOff = Helpers.cosd(piece.spinAngle) * radiusInvert;
                float yOff = Helpers.sind(piece.spinAngle) * radiusInvert;

                Point originalPos = pos.add(piece.originalOffset);
                Point targetPos = pos.addxy(xOff, yOff);

                piece.changePos(Point.lerp(originalPos, targetPos, lerpProgress));
                
                piece.spinAngle += Global.spf * 360;
            }

            if (!isComingBack && lerpProgress >= 1 && radius >= targetRadius && !controlled)
            {
                if (target != null)
                {
                    if (target.destroyed) target = null;
                    else if (target is Character chr && chr.player.isDead) target = null;
                    else if (target.getCenterPos().distanceTo(maverick.getCenterPos()) > range)
                    {
                        target = null;
                    }
                }

                if (target == null)
                {
                    target = Global.level.getClosestTarget(maverick.getCenterPos(), maverick.player.alliance, false, aMaxDist: range);
                }

                if (target != null)
                {
                    var targetPos = Point.lerp(pos, target.getCenterPos(), Global.spf * 10);
                    changePos(targetPos);
                }
            }

            if (!isComingBack)
            {
                if (radius < targetRadius)
                {
                    radius += Global.spf * targetRadius * 2;
                    if (radius >= targetRadius) radius = targetRadius;
                }
                lerpProgress += Global.spf * 10;
                if (lerpProgress > 1) lerpProgress = 1;
            }
            else
            {
                if (radius > 0)
                {
                    radius -= Global.spf * targetRadius * 2;
                    if (radius <= 0) radius = 0;
                }
                lerpProgress -= Global.spf * 10;
                if (lerpProgress < 0) lerpProgress = 0;
            }
        }

        public void comeBack()
        {
            isComingBack = true;
            float speed = 500;
            Point destPos = maverick.pos.add(originOffset);
            moveToPos(destPos, speed);
            if (pos.distanceTo(destPos) < speed * Global.spf * 2 && lerpProgress < 0.25f)
            {
                maverick.playSound("magnacConnect", sendRpc: true);
                maverick.setNoTail(false);
                destroySelf();
            }
        }

        public override void onDestroy()
        {
            base.onDestroy();
            foreach (var piece in pieces)
            {
                piece.destroySelf();
            }
            maverick.magnetMineParent = null;
        }
    }

    public class MagnaCMagnetMineState : MaverickState
    {
        bool shotOnce;
        public MagnaCentipede magnac;
        public SoundWrapper sound;
        public MagnaCMagnetMineState() : base("telekinesis", "")
        {
        }

        public override void update()
        {
            base.update();

            Point? shootPos = maverick.getFirstPOI();
            if (!shotOnce && shootPos != null)
            {
                shotOnce = true;
                magnac.magnetMineParent = new MagnaCMagnetMineParent(shootPos.Value, magnac, true);
                sound = maverick.playSound("magnacMagnetMine", sendRpc: true);
            }

            if (magnac.magnetMineParent != null)
            {
                magnac.magnetMineParent.controlled = false;
                var inputDir = input.getInputDir(player);
                if (!inputDir.isZero())
                {
                    magnac.magnetMineParent.moveMaxDist(inputDir.times(200), magnac.getCenterPos(), 150);
                    magnac.magnetMineParent.controlled = true;
                }
            }

            if (!input.isHeld(Control.Shoot, player) && stateTime > 0.25f)
            {
                maverick.changeToIdleOrFall();
                return;
            }

            if (stateTime > 4)
            {
                maverick.changeToIdleOrFall();
                return;
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            magnac = maverick as MagnaCentipede;
        }

        public override bool canEnter(Maverick maverick)
        {
            if (maverick is MagnaCentipede ms && ms.noTail) return false;
            return base.canEnter(maverick);
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            if (sound != null && !sound.deleted)
            {
                sound.sound?.Stop();
            }
            RPC.stopSound.sendRpc("magnacMagnetMine", maverick.netId);
            if (magnac.magnetMineParent != null && !magnac.magnetMineParent.destroyed)
            {
                magnac.noTail = true;
                magnac.changeSpriteFromName(newState.sprite, false);
                //magnac.setNoTail(true);
            }
        }
    }

    public class MagnaCTeleportState : MaverickState
    {
        int state = 0;
        Actor clone;
        bool inverted;
        public MagnaCTeleportState() : base("teleport_out", "")
        {
            enterSound = "magnacTeleportOut";
        }

        public override void update()
        {
            base.update();

            if (state == 0)
            {
                var dir = new Point(0, 0);
                if (input.isPressed(Control.Up, player)) dir.y = -1;
                else if (input.isPressed(Control.Down, player)) dir.y = 1;
                if (input.isHeld(Control.Left, player)) dir.x = -1;
                else if (input.isHeld(Control.Right, player)) dir.x = 1;

                float moveAmount = dir.x * 200 * Global.spf;
                if (dir.x != 0) clone.xDir = MathF.Round(dir.x);

                if (dir.y == -1 && !inverted)
                {
                    var hits = Global.level.raycastAllSorted(clone.getCenterPos(), clone.getCenterPos().addxy(0, -200), new List<Type> { typeof(Wall) });
                    var hit = hits.FirstOrDefault();
                    if (hit != null && hit.gameObject is Wall wall && !wall.topWall)
                    {
                        clone.visible = true;
                        inverted = true;
                        clone.yScale = -1;
                        clone.changePos(hit.getHitPointSafe().addxy(0, MagnaCentipede.constHeight));
                    }
                }
                if (dir.y == 1 && inverted)
                {
                    var hits = Global.level.raycastAllSorted(clone.getCenterPos(), clone.getCenterPos().addxy(0, 200), new List<Type> { typeof(Wall) });
                    var hit = hits.FirstOrDefault();
                    if (hit != null && hit.gameObject is Wall wall && !wall.topWall)
                    {
                        clone.visible = true;
                        inverted = false;
                        clone.yScale = 1;
                        clone.changePos(hit.getHitPointSafe());
                    }
                }

                clone.move(new Point(moveAmount, 0), useDeltaTime: false);
                if (MathF.Abs(moveAmount) > 0)
                {
                    clone.visible = true;
                }

                if (!canChangePos())
                {
                    if (inverted)
                    {
                        var hits = Global.level.raycastAllSorted(clone.getCenterPos(), clone.getCenterPos().addxy(0, -200), new List<Type> { typeof(Wall) });
                        var hit = hits.FirstOrDefault();
                        if (hit != null && hit.gameObject is Wall wall && !wall.topWall)
                        {
                            clone.changePos(hit.getHitPointSafe().addxy(0, MagnaCentipede.constHeight));
                        }
                    }
                    else
                    {
                        var hits = Global.level.raycastAllSorted(clone.getCenterPos(), clone.getCenterPos().addxy(0, 200), new List<Type> { typeof(Wall) });
                        var hit = hits.FirstOrDefault();
                        if (hit != null && hit.gameObject is Wall wall && !wall.topWall)
                        {
                            clone.changePos(hit.getHitPointSafe());
                        }
                    }
                }

                if (!canChangePos())
                {
                    var redXPos = clone.getCenterPos();
                    DrawWrappers.DrawLine(redXPos.x - 10, redXPos.y - 10, redXPos.x + 10, redXPos.y + 10, Color.Red, 2, ZIndex.HUD);
                    DrawWrappers.DrawLine(redXPos.x - 10, redXPos.y + 10, redXPos.x + 10, redXPos.y - 10, Color.Red, 2, ZIndex.HUD);
                }

                if (maverick.isAnimOver())
                {
                    state = 1;
                    clone.visible = false;
                    maverick.changeSpriteFromName("teleport_in", true);
                    if (canChangePos())
                    {
                        if ((inverted && !maverick.reversedGravity) || (!inverted && maverick.reversedGravity))
                        {
                            maverick.reverseGravity();
                        }
                        var prevCamPos = player.character.getCamCenterPos();
                        player.character.stopCamUpdate = true;
                        maverick.changePos(clone.pos);
                        if (player.isTagTeam()) Global.level.snapCamPos(player.character.getCamCenterPos(), prevCamPos);
                        maverick.xDir = clone.xDir;
                    }
                    maverick.playSound("magnacTeleportIn", sendRpc: true);
                }
            }
            else if (state == 1)
            {
                if (maverick.isAnimOver())
                {
                    maverick.changeToIdleOrFall();
                }
            }
        }

        public bool canChangePos()
        {
            float yDir = inverted ? -5 : 5;
            if (Global.level.checkCollisionActor(clone, 0, yDir) == null) return false;
            var hits = Global.level.getTriggerList(clone, 0, yDir, null, new Type[] { typeof(KillZone) });
            if (hits.Count > 0) return false;
            return true;
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            clone = new Actor("empty", maverick.pos, null, true, false);
            var rect = new Rect(0, 0, maverick.width, maverick.height);
            clone.spriteToCollider["teleport_out"] = new Collider(rect.getPoints(), false, clone, false, false, 0, new Point(0, 0));
            clone.spriteToCollider["notail_teleport_out"] = new Collider(rect.getPoints(), false, clone, false, false, 0, new Point(0, 0));
            clone.changeSprite((maverick as MagnaCentipede).noTail ? "magnac_notail_teleport_out" : "magnac_teleport_out", false);
            clone.frameSpeed = 0;
            clone.alpha = 0.5f;
            clone.xDir = maverick.xDir;
            clone.visible = false;
            clone.useGravity = false;
            maverick.useGravity = false;
            if (maverick.reversedGravity)
            {
                inverted = true;
                clone.yScale = -1;
            }
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            maverick.useGravity = true;
            (maverick as MagnaCentipede).teleportCooldown = 0.25f;
            clone.destroySelf();
        }
    }

    public class MagnaCMagnetPullProj : Projectile
    {
        public float radius = 150;
        public MagnaCMagnetPullProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 0, player, "empty", 0, 0.01f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.MagnaCMagnetPull;
            setIndestructableProperties();

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            foreach (var go in Global.level.getGameObjectArray())
            {
                var chr = go as Character;
                if (chr == null || !chr.ownedByLocalPlayer || chr.isImmuneToKnockback()) continue;
                var damagable = go as IDamagable;
                if (!damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, null)) continue;
                if (chr.pos.distanceTo(pos) > radius + 15) continue;
                if (!Global.level.noWallsInBetween(chr.getCenterPos(), pos)) continue;

                vel.y = -1;
                if (!chr.grounded) chr.vel.y = 0;
                chr.grounded = false;
                float mag = 250 * Helpers.clamp01(1 - (chr.getCenterPos().distanceTo(pos) / 150));
                Point velVector = chr.getCenterPos().directionToNorm(pos).times(mag);
                if (chr.getCenterPos().distanceTo(pos) > mag * Global.spf * 2)
                {
                    chr.move(velVector, true);
                }
                else
                {
                    var chrCenterPosOffset = chr.getCenterPos().subtract(chr.pos);
                    chr.changePos(pos.subtract(chrCenterPosOffset));
                }
            }
        }
    }

    public class MagnaCMagnetPullState : MaverickState
    {
        SoundWrapper pullSound;
        public MagnaCMagnetPullProj proj;
        public MagnaCMagnetPullState() : base("drain_start", "")
        {
        }

        public override void update()
        {
            base.update();

            if (maverick.sprite.name.EndsWith("drain_start") && maverick.isAnimOver())
            {
                maverick.changeSpriteFromName("drain", true);
            }

            if (proj == null && maverick.getFirstPOI() != null)
            {
                pullSound = maverick.playSound("magnacPull", sendRpc: true);
                proj = new MagnaCMagnetPullProj(maverick.weapon, maverick.getFirstPOI().Value, maverick.xDir, player, player.getNextActorNetId(), rpc: true);
            }

            if (isAI)
            {
                if (stateTime > 1)
                {
                    maverick.changeToIdleOrFall();
                }
            }
            else if (!input.isHeld(Control.Special1, player) && stateTime > 0.25f)
            {
                maverick.changeToIdleOrFall();
            }

            if (stateTime > 2)
            {
                maverick.changeToIdleOrFall();
                return;
            }
        }

        public override bool trySetGrabVictim(Character grabbed)
        {
            maverick.changeState(new MagnaCDrainState(grabbed), true);
            return true;
        }

        public override bool canEnter(Maverick maverick)
        {
            if (maverick is MagnaCentipede ms && ms.noTail) return false;
            return base.canEnter(maverick);
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            proj?.destroySelf();
            if (pullSound != null && !pullSound.deleted)
            {
                pullSound.sound?.Stop();
            }
            RPC.stopSound.sendRpc("magnacPull", maverick.netId);
        }
    }

    public class MagnaCDrainState : MaverickState
    {
        Character victim;
        float soundTime = 1;
        float leechTime = 0.5f;
        public bool victimWasGrabbedSpriteOnce;
        float timeWaiting;
        public MagnaCDrainState(Character grabbedChar) : base("drain", "")
        {
            this.victim = grabbedChar;
        }

        public override void update()
        {
            base.update();
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
                    }
                    return;
                }
            }

            if (leechTime > 0.5f)
            {
                leechTime = 0;
                var damager = new Damager(player, 0, 0, 0);
                damager.applyDamage(victim, false, maverick.weapon, maverick, (int)ProjIds.MagnaCTail);
            }

            soundTime += Global.spf;
            if (soundTime > 1f)
            {
                soundTime = 0;
                maverick.playSound("magnacDrain", sendRpc: true);
            }

            if (stateTime > 2f)
            {
                maverick.changeToIdleOrFall();
            }
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            victim?.releaseGrab(maverick);
        }
    }

    public class MagnaCDrainGrabbed : GenericGrabbedState
    {
        public const float maxGrabTime = 4;
        public MagnaCDrainGrabbed(MagnaCentipede grabber) : base(grabber, maxGrabTime, "_drain")
        {
            this.grabber = grabber;
            grabTime = maxGrabTime;
        }
    }

    public class MagnaCCeilingStartState : MaverickState
    {
        public MagnaCCeilingStartState() : base("gravity_shift", "")
        {
        }

        public override void update()
        {
            base.update();

            if (!maverick.reversedGravity && maverick.isAnimOver())
            {
                maverick.changeToIdleOrFall();
            }
            else
            {
                if (maverick.deltaPos.isCloseToZero())
                {
                    maverick.changeToIdleOrFall();
                }
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.reverseGravity();
        }

        public override void onExit(MaverickState newState)
        {
            maverick.useGravity = true;
            base.onExit(newState);
        }
    }

    public class MagnaCCeilingState : MaverickState
    {
        public MagnaCCeilingState() : base("drain", "")
        {
        }

        public override void update()
        {
            base.update();

            if (input.isPressed(Control.Jump, player))
            {
                maverick.changeToIdleOrFall();
            }
        }
    }
}
