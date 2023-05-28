using SFML.Graphics;
using System;
using System.Linq;

namespace MMXOnline
{
    public class MorphMothCocoon : Maverick
    {
        public static Weapon getWeapon() { return new Weapon(WeaponIds.MorphMCGeneric, 145); }

        public float scrapRegenTime;
        public Point latchPos;
        public float latchLen;
        public int scrapAbsorbed;
        public bool isBurned { get { return sprite.name.Contains("_burn"); } }
        public float smokeTime;

        public MorphMothCocoon(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            stateCooldowns.Add(typeof(MShoot), new MaverickStateCooldown(false, true, 0.75f));
            stateCooldowns.Add(typeof(MorphMCThreadState), new MaverickStateCooldown(false, true, 0.75f));

            weapon = getWeapon();
            angle = 0;
            ammo = 0;

            spriteToCollider["*_hang"] = getDashCollider();

            awardWeaponId = WeaponIds.SilkShot;
            weakWeaponId = WeaponIds.SpeedBurner;
            weakMaverickWeaponId = WeaponIds.FlameStag;

            netActorCreateId = NetActorCreateId.MorphMothCocoon;
            netOwner = player;
            if (sendRpc)
            {
                createActorRpc(player.id);
            }
        }

        public float selfDestructTime;
        public override void update()
        {
            base.update();

            Helpers.decrementTime(ref crashCooldown);
            zIndex = ZIndex.Background - 1;

            if (selfDestructTime > 0)
            {
                addRenderEffect(RenderEffectType.Shake);
                addRenderEffect(RenderEffectType.Hit);
            }

            xScale = 1 + (scrapAbsorbed / 64f);
            yScale = 1 + (scrapAbsorbed / 64f);

            if (!ownedByLocalPlayer) return;

            scrapRegenTime += Global.spf;
            if (scrapRegenTime > 0.5f)
            {
                scrapRegenTime = 0;
                ammo += 1;
                if (ammo > maxAmmo) ammo = maxAmmo;
            }

            if (isBurned)
            {
                Helpers.decrementTime(ref smokeTime);
                if (smokeTime == 0)
                {
                    smokeTime = 0.2f;
                    new Anim(getCenterPos().addxy(0, -10).addRand(10, 10), "dust", 1, null, true)
                    {
                        vel = new Point(0, -50)
                    };
                }
                if (isAnimOver() && state is MorphMCHangState)
                {
                    changeSpriteFromName(state.sprite, true);
                }
            }

            if (state is not MorphMCHangState && state is not MFall)
            {
                angle = 0;
            }

            if ((health < maxHealth * 0.5f && health > 0) || scrapAbsorbed >= 32)
            {
                if (selfDestructTime == 0)
                {
                    selfDestructTime = 0.1f;
                    playSound("morphmMorph", sendRpc: true);
                    RPC.actorToggle.sendRpc(netId, RPCActorToggleType.MorphMothCocoonSelfDestruct);
                }
            }

            if (selfDestructTime > 0)
            {
                selfDestructTime += Global.spf;

                if (selfDestructTime > 1.1f)
                {
                    var mmw = player.weapons.FirstOrDefault(w => w is MorphMothWeapon mmw) as MorphMothWeapon;
                    if (mmw != null)
                    {
                        bool wasCocoon = player.currentMaverick == this;
                        mmw.isMoth = true;
                        //Point spawnPos = getCenterPos().addxy(0, 27 * yScale);
                        Point spawnPos = getCenterPos().addxy(0, 0);
                        mmw.summon(player, spawnPos, spawnPos, xDir, isMothHatch: true);
                        mmw.maverick.invulnTime = 0.5f;
                        mmw.maverick.health = health;

                        if (wasCocoon)
                        {
                            player.character?.becomeMaverick(mmw.maverick);
                        }

                        playSound("morphmHatch", sendRpc: true);

                        new Anim(getCenterPos().addxy(0, 0), "morphmc_part_left", 1, player.getNextActorNetId(), false, sendRpc: true) { vel = new Point(-100, 100), ttl = 1f, xScale = xScale, yScale = yScale };
                        new Anim(getCenterPos().addxy(0, 0), "morphmc_part_right", 1, player.getNextActorNetId(), false, sendRpc: true) { vel = new Point(100, 100), ttl = 1f, xScale = xScale, yScale = yScale };
                        destroySelf();

                        return;
                    }
                }
                return;
            }

            if (aiBehavior == MaverickAIBehavior.Control)
            {
                if (state is MIdle || state is MRun)
                {
                    if (input.isPressed(Control.Special1, player))
                    {
                        changeState(new MorphMCThreadState());
                    }
                    else if (input.isPressed(Control.Dash, player) || input.isPressed(Control.Shoot, player))
                    {
                        changeState(new MorphMCSpinState());
                    }
                }
                else if (state is MJump || state is MFall)
                {
                    if (input.isPressed(Control.Dash, player) || input.isPressed(Control.Shoot, player))
                    {
                        changeState(new MorphMCSpinState());
                    }
                }
            }
        }

        public override string getMaverickPrefix()
        {
            return "morphmc";
        }

        public override Point getCenterPos()
        {
            return pos.addxy(0, (height * yScale) / 2f);
        }

        public override MaverickState[] aiAttackStates()
        {
            var attacks = new MaverickState[]
            {
                new MorphMCSpinState(),
            };
            return attacks;
        }

        public override MaverickState getRandomAttackState()
        {
            return aiAttackStates().GetRandomItem();
        }

        public override Collider getGlobalCollider()
        {
            var rect = Rect.createFromWH(0, 0, 16 * xScale, 14 * yScale);
            return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
        }

        public Collider getHangingCollider()
        {
            var rect = Rect.createFromWH(0, 0, 16 * xScale, 23 * yScale);
            return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
        }

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            if (sprite.name.Contains("spin"))
            {
                return new GenericMeleeProj(weapon, centerPoint, ProjIds.MorphMCSpin, player, 1, 0, 0.5f);
            }
            else if (sprite.name.Contains("hang"))
            {
                return new GenericMeleeProj(weapon, centerPoint, ProjIds.MorphMCSwing, player, 0, Global.defFlinch, 0.5f);
            }
            return null;
        }

        public override void updateProjFromHitbox(Projectile proj)
        {
            if (proj.projId == (int)ProjIds.MorphMCSpin)
            {
                float damage = 1;
                int flinch = 0;
                if (deltaPos.magnitude > 300 * Global.spf * Helpers.progress(scrapAbsorbed, 32f))
                {
                    damage = 4;
                    flinch = Global.defFlinch;
                }
                else if (deltaPos.magnitude > 200 * Global.spf * Helpers.progress(scrapAbsorbed, 32f))
                {
                    damage = 2;
                    flinch = Global.halfFlinch;
                }
                proj.damager.damage = damage;
                proj.damager.flinch = flinch;
            }
            else if (proj.projId == (int)ProjIds.MorphMCSwing)
            {
                if (deltaPos.magnitude > 250 * Global.spf) proj.damager.damage = 4;
                else proj.damager.damage = 0;
            }
        }

        float crashCooldown;
        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (other.gameObject is Wall && sprite.name.Contains("spin") && deltaPos.magnitude > 200 * Global.spf && deltaPos.angleWith(other.getNormalSafe()) > 135)
            {
                crash();
            }
        }

        public void crash()
        {
            if (crashCooldown == 0)
            {
                playSound("morphmCrash");
                shakeCamera();
                crashCooldown = 0.25f;
            }
        }

        public void setLatchPos(Point pos)
        {
            latchPos = pos;
        }

        public void setLatchLen()
        {
            latchLen = pos.y - latchPos.y;
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            if (sprite.name.Contains("latch") || sprite.name.Contains("hang"))
            {
                Point origin = getFirstPOIOrDefault();
                DrawWrappers.DrawLine(origin.x, origin.y, latchPos.x, latchPos.y, Color.Black, 3, ZIndex.Background - 100);
                DrawWrappers.DrawLine(origin.x, origin.y, latchPos.x, latchPos.y, Color.White, 1, ZIndex.Background - 100);
            }
        }

        public void absorbScrap()
        {
            scrapAbsorbed++;
            if (scrapAbsorbed > 32)
            {
                scrapAbsorbed = 32;
            }
        }

        public MaverickState burn()
        {
            if (isBurned) return null;
            if (isUnderwater()) return null;
            playSound("morphmBurn", sendRpc: true);
            if (state is MorphMCHangState)
            {
                changeSpriteFromName("burn_hang", true);
                return null;
            }
            else
            {
                return new MorphMCBurnState();
            }
        }

        public Actor getHealTarget()
        {
            var targets = Global.level.getTargets(pos, player.alliance, true, aMaxDist: 20, isRequesterAI: true, includeAllies: true, callingActor: this);
            return targets.FirstOrDefault((t) => 
            {
                if (t is Character chr)
                {
                    return chr.player.alliance != player.alliance || chr.player.health < chr.player.maxHealth;
                }
                /*
                // GMTODO use an "isAtMaxHealth" boolstate
                else if (t is Maverick mvk)
                {
                    return mvk.player.alliance != player.alliance || mvk.health < mvk.maxHealth;
                }
                else if (t is RideArmor ra)
                {
                    return ra.player.alliance != player.alliance || ra.health < ra.maxHealth;
                }
                */
                return false;
            });
        }
    }

    public class MorphMCScrapProj : Projectile
    {
        bool isSuck;
        MorphMothCocoon mmCocoon;
        public MorphMCScrapProj(Weapon weapon, Point pos, int xDir, Point vel, float maxTime, bool isSuck, MorphMothCocoon mmCocoon, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 1, player, "morphmc_scrap_proj", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.MorphMCScrap;
            this.maxTime = maxTime;
            this.vel = vel;
            speed = vel.magnitude;
            this.isSuck = isSuck;
            this.mmCocoon = mmCocoon;
            destroyOnHit = true;

            frameIndex = Helpers.randomRange(0, sprite.frames.Count - 1);
            frameSpeed = 0;
            useGravity = !isSuck;
            healAmount = 1;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            if (isSuck)
            {
                vel = pos.directionToNorm(mmCocoon.getCenterPos()).times(speed);
            }
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (!ownedByLocalPlayer) return;

            if (isSuck && mmCocoon != null && ownedByLocalPlayer && mmCocoon.ownedByLocalPlayer && other.gameObject == mmCocoon)
            {
                mmCocoon.absorbScrap();
                mmCocoon.addAmmo(1);
                destroySelf();
            }
        }
    }

    public class MorphMCSpinState : MaverickState
    {
        public MorphMothCocoon mmCocoon;
        float xPushVel;
        float shootTime;
        float soundTime;

        public MorphMCSpinState() : base("spin", "")
        {
        }

        public override void update()
        {
            base.update();

            Point airMove = new Point(0, 0);
            genericJumpCode();
            if (!maverick.grounded)
            {
                if (Global.level.checkCollisionActor(maverick, 0, -1) != null && maverick.vel.y < 0)
                {
                    maverick.vel.y = 0;
                }

                if (input.isHeld(Control.Left, player))
                {
                    airMove.x = -maverick.getRunSpeed() * maverick.getDashSpeed();
                    maverick.xDir = -1;
                }
                else if (input.isHeld(Control.Right, player))
                {
                    airMove.x = maverick.getRunSpeed() * maverick.getDashSpeed();
                    maverick.xDir = 1;
                }

                if (airMove.magnitude > 0)
                {
                    maverick.move(airMove);
                }
            }

            Helpers.decrementTime(ref soundTime);
            if (soundTime <= 0)
            {
                soundTime = 0.404f;
                maverick.playSound("morphmSpin", sendRpc: true);
            }

            Helpers.decrementTime(ref shootTime);
            if ((isAI || input.isHeld(Control.Shoot, player)) && mmCocoon.ammo > 0)
            {
                if (shootTime == 0)
                {
                    shootTime = 0.15f;
                    Point vel = new Point(Helpers.randomRange(-75, 75), Helpers.randomRange(-300, -250));
                    new MorphMCScrapProj(mmCocoon.weapon, maverick.getFirstPOIOrDefault(), MathF.Sign(vel.x), vel, 0.75f, false, null, player, player.getNextActorNetId(), rpc: true);
                    mmCocoon.ammo--;
                    mmCocoon.scrapRegenTime = 0;
                }
            }

            float maxSpeed = 250 - MathF.Abs(airMove.x);

            if (input.isHeld(Control.Left, player))
            {
                maverick.xDir = -1;
                if (xPushVel > 0) xPushVel -= Global.spf * 200;
                else xPushVel -= Global.spf * 200;
                if (xPushVel < -maxSpeed) xPushVel = -maxSpeed;
            }
            else if (input.isHeld(Control.Right, player))
            {
                maverick.xDir = 1;
                if (xPushVel < 0) xPushVel += Global.spf * 200;
                else xPushVel += Global.spf * 200;
                if (xPushVel > maxSpeed) xPushVel = maxSpeed;
            }
            else
            {
                if (Math.Abs(xPushVel) > 5)
                {
                    xPushVel = Helpers.lerp(xPushVel, 0, Global.spf * 5);
                }
            }

            maverick.move(new Point(xPushVel, 0));

            var wall = checkCollisionSlide(xPushVel * Global.spf, -2);
            if (wall != null && wall.gameObject is Wall)
            {
                //if (MathF.Abs(xPushVel) >= 200) mmCocoon.crash();
                xPushVel = 0;
            }

            if (isAI)
            {
                if (mmCocoon.getHealTarget() == null)
                {
                    maverick.changeToIdleOrFall();
                }
            }
            else if (!input.isHeld(Control.Dash, player) && !input.isHeld(Control.Shoot, player))
            {
                maverick.changeToIdleOrFall();
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            mmCocoon = maverick as MorphMothCocoon;
        }
    }

    public class MorphMCThreadProj : Projectile
    {
        Point origin;
        public bool hitCeiling;
        bool reversed;
        float reverseDist;
        float range = 200;
        public MorphMCThreadProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 2, player, "morphmc_silk_spike", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.MorphMCThread;
            vel = new Point(0, -200);
            origin = pos;
            setIndestructableProperties();

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (deltaPos.y >= 0) damager.damage = 0;
            else damager.damage = 2;

            if (!ownedByLocalPlayer) return;

            if (moveDistance > range && !reversed)
            {
                reverse();
            }
            if (reversed && moveDistance > reverseDist * 2)
            {
                destroySelf();
            }
        }

        public void reverse()
        {
            if (!reversed)
            {
                reversed = true;
                vel.y *= -1;
                reverseDist = moveDistance;
            }
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            if (!reversed && other.gameObject is Wall wall && !wall.topWall && !wall.isMoving)
            {
                hitCeiling = true;
                stopMoving();
                changePos(other.getHitPointSafe());
            }
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            float dist = origin.y - pos.y;
            int h = 8;
            for (int i = 0; i < dist / h; i++)
            {
                Global.sprites["morphmc_silk_animated"].draw(frameIndex, origin.x, origin.y - (i * h), 1, 1, null, 1, 1, 1, ZIndex.Character);
            }
        }
    }

    public class MorphMCThreadState : MaverickState
    {
        MorphMCThreadProj proj;
        public MorphMCThreadState() : base("idle", "")
        {
        }

        public override void update()
        {
            base.update();

            if (proj == null || proj.destroyed)
            {
                maverick.changeToIdleOrFall();
                return;
            }

            if (input.isPressed(Control.Special1, player))
            {
                proj.reverse();
            }

            if (proj.hitCeiling)
            {
                var latchPos = new Point(maverick.pos.x, proj.pos.y);
                (maverick as MorphMothCocoon).setLatchPos(latchPos);
                float latchDestY = ((maverick.getFirstPOIOrDefault().y + latchPos.y) / 2) + 10;
                maverick.changeState(new MorphMCLatchState(latchDestY));
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.stopMoving();
            proj = new MorphMCThreadProj(maverick.weapon, maverick.getFirstPOIOrDefault(), 1, player, player.getNextActorNetId(), rpc: true);
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            proj?.destroySelf();
        }
    }

    public class MorphMCLatchState : MaverickState
    {
        float latchDestY;
        public MorphMCLatchState(float latchDestY) : base("latch", "")
        {
            this.latchDestY = latchDestY;
        }

        public override void update()
        {
            base.update();

            maverick.move(new Point(0, -100));
            if (maverick.pos.y < latchDestY)
            {
                maverick.incPos(new Point(0, -14 * maverick.yScale));
                (maverick as MorphMothCocoon).setLatchLen();
                maverick.changeState(new MorphMCHangState());
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.stopMoving();
            maverick.useGravity = false;
            maverick.grounded = false;
            maverick.canBeGrounded = false;
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            if (newState is not MorphMCHangState)
            {
                maverick.useGravity = true;
            }
        }
    }

    public class MorphMCHangState : MaverickState
    {
        float shootTime;
        MorphMothCocoon mmCocoon;
        Point latchPos;
        float len;
        Point origin;
        Point swingVel;
        float suckSoundTime;
        public MorphMCHangState() : base("hang", "")
        {
        }

        float suckAngle;
        float suckRadius = 125;

        public override void update()
        {
            base.update();

            Helpers.decrementTime(ref shootTime);
            Helpers.decrementTime(ref suckSoundTime);
            if (input.isHeld(Control.Shoot, player) && mmCocoon.ammo > 0)
            {
                if (shootTime == 0)
                {
                    shootTime = 0.15f;
                    Point vel = new Point(Helpers.randomRange(-75, 75), Helpers.randomRange(-300, -250));
                    new MorphMCScrapProj(maverick.weapon, maverick.getFirstPOIOrDefault(), MathF.Sign(vel.x), vel, 1.5f, false, null, player, player.getNextActorNetId(), rpc: true);
                    mmCocoon.ammo--;
                    mmCocoon.scrapRegenTime = 0;
                }
            }
            else if (input.isHeld(Control.Special1, player))
            {
                suckAngle += Global.spf * 100;
                if (suckAngle >= 360) suckAngle = 0;
                if (suckSoundTime <= 0)
                {
                    suckSoundTime = 0.269f;
                    mmCocoon.playSound("morphmVacuum", sendRpc: true);
                }
                if (shootTime == 0)
                {
                    shootTime = 0.1f;
                    Point suckPos = maverick.getCenterPos().add(Point.createFromAngle(suckAngle).times(suckRadius));
                    Point vel = suckPos.directionToNorm(maverick.getCenterPos()).times(350);
                    new MorphMCScrapProj(maverick.weapon, suckPos, MathF.Sign(vel.x), vel, 0.5f, true, mmCocoon, player, player.getNextActorNetId(), rpc: true);
                    mmCocoon.scrapRegenTime = 0;
                }
            }

            var inputDir = input.getInputDir(player);
            if (inputDir.y != 0)
            {
                float oldLatchLen = mmCocoon.latchLen;
                mmCocoon.latchLen += inputDir.y * Global.spf * 100;
                mmCocoon.latchLen = Helpers.clamp(mmCocoon.latchLen, 10, 200);

                float angle = mmCocoon.latchPos.directionToNorm(maverick.pos).angle;
                if (Global.level.checkCollisionActor(mmCocoon, Helpers.cosd(angle) * 5, Helpers.sind(angle) * 5) != null)
                {
                    mmCocoon.latchLen = oldLatchLen;
                }
            }

            latchPos = mmCocoon.latchPos;
            len = mmCocoon.latchLen;
            origin = latchPos.addxy(0, len);

            float swingMod = Helpers.clamp01(len / 75);

            swingVel.y += Physics.gravity * Global.spf * swingMod;
            Point vec = latchPos.directionToNorm(maverick.pos);
            Point leftNorm = vec.leftNormal();
            Point rightNorm = vec.rightNormal();
            Point norm = (leftNorm.y > rightNorm.y) ? leftNorm : rightNorm;

            if (inputDir.x != 0)
            {
                swingVel.x += inputDir.x * 150 * swingMod * Global.spf;
            }
            else
            {
                if (swingMod == 0) swingVel.x = 0;
                else swingVel.x = Helpers.lerp(swingVel.x, 0, Global.spf / swingMod);
            }

            swingVel = swingVel.project(norm);

            Point destPos = maverick.pos.add(swingVel.times(Global.spf));

            float distFromOrigin = destPos.distanceTo(latchPos);
            if (MathF.Abs(distFromOrigin - len) > 0.1f)
            {
                float newAngle = latchPos.directionToNorm(destPos).angle;
                destPos.x = latchPos.x + Helpers.cosd(newAngle) * len;
                destPos.y = latchPos.y + Helpers.sind(newAngle) * len;
            }

            Point moveAmount = maverick.pos.directionTo(destPos);
            var hit = Global.level.checkCollisionActor(maverick, moveAmount.x, moveAmount.y);
            if (hit == null)
            {
                maverick.incPos(moveAmount);
            }
            else
            {
                swingVel = Point.zero;
                if (hit.hitData.normal != null)
                {
                    if (hit.getNormalSafe().isGroundNormal())
                    {
                        snapPosOnExit(destPos);
                        maverick.changeToIdleOrFall();
                        return;
                    }
                }
            }

            maverick.angle = vec.angle - 90;

            if (input.isPressed(Control.Jump, player))
            {
                snapPosOnExit(destPos);
                maverick.changeToIdleOrFall();
                maverick.xSwingVel = swingVel.x;
                maverick.vel.y = swingVel.y;
            }
        }

        public void snapPosOnExit(Point destPos)
        {
            float newAngle = latchPos.directionToNorm(destPos).angle;
            Point snapPos = new Point();
            snapPos.x = latchPos.x + Helpers.cosd(newAngle) * (len + (14 * mmCocoon.xScale));
            snapPos.y = latchPos.y + Helpers.sind(newAngle) * (len + (14 * mmCocoon.yScale));
            mmCocoon.changePos(snapPos);
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.stopMoving();
            maverick.angle = 0;
            mmCocoon = maverick as MorphMothCocoon;
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            maverick.useGravity = true;
        }
    }

    public class MorphMCBurnState : MaverickState
    {
        public MorphMCBurnState() : base("burn", "")
        {
        }

        public override void update()
        {
            base.update();

            if (maverick.isAnimOver())
            {
                maverick.changeToIdleOrFall();
            }
        }
    }
}
