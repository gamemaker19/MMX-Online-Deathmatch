using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class RideChaser : Actor, IDamagable
    {
        public float health = 24;
        public float maxHealth = 24;
        public float healAmount = 0;
        public float healTime = 0;
        public Character character;
        public float selfDestructTime;
        public float maxSelfDestructTime = 10;
        public Anim hawkElec;
        public float enterCooldown;
        public bool claimed;
        public Player player { get { return character?.player; } } //WARNING: this gets the player of the character riding the armor. For the owner, use netOwner
        public bool isExploding;
        public int neutralId;
        public Weapon gunWeapon;
        public Weapon hitWeapon;
        public float slowdownTime;
        public static Weapon getGunWeapon() { return new Weapon(WeaponIds.RideChaserGun, 170); }
        public bool mountedOnce;

        public RideChaser(Player owner, Point pos, int neutralId, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) : 
            base("ridechaser_idle", pos, netId, ownedByLocalPlayer, true)
        {
            netOwner = owner;
            health = maxHealth;

            gunWeapon = getGunWeapon();
            hitWeapon = new Weapon(WeaponIds.RideChaserHit, 169);
            this.neutralId = neutralId;
            useFrameProjs = true;
            splashable = true;

            Global.level.addGameObject(this);

            netActorCreateId = NetActorCreateId.RideChaser;
            if (sendRpc)
            {
                createActorRpc(owner.id);
            }
        }

        public override void postUpdate()
        {
            base.postUpdate();

            updateProjectileCooldown();
            fadeXMomentum();

            Helpers.decrementTime(ref slowdownTime);

            if (selfDestructTime > 0)
            {
                int flashFrequency = 30;
                if (selfDestructTime < 3) flashFrequency = 60;
                else if (selfDestructTime >= 3 && selfDestructTime < 6) flashFrequency = 30;
                else if (selfDestructTime >= 6 && selfDestructTime < 8) flashFrequency = 15;
                else if (selfDestructTime >= 8) flashFrequency = 5;

                if (Global.frameCount % flashFrequency == 0)
                {
                    addRenderEffect(RenderEffectType.Hit);
                }
                else
                {
                    removeRenderEffect(RenderEffectType.Hit);
                }

                selfDestructTime += Global.spf;
                if (selfDestructTime >= maxSelfDestructTime)
                {
                    explode();
                    destroySelf();
                    return;
                }
            }

            if (isUnderwater() && selfDestructTime == 0)
            {
                if (hawkElec == null)
                {
                    hawkElec = new Anim(pos.addxy(0, 0), "hawk_elec", 1, null, false);
                }
                selfDestructTime = Global.spf;
            }

            if (hawkElec != null)
            {
                hawkElec.changePos(pos.addxy(0, -10));
            }

            // Cutoff point
            if (!ownedByLocalPlayer)
            {
                return;
            }

            if (pos.y > Global.level.killY)
            {
                incPos(new Point(0, 50));
                applyDamage(null, null, Damager.envKillDamage, null);
            }

            Helpers.decrementTime(ref enterCooldown);
            if (isExploding) return;

            if (character != null)
            {
                if (health >= maxHealth)
                {
                    healAmount = 0;
                }
                if (healAmount > 0 && health > 0)
                {
                    healTime += Global.spf;
                    if (healTime > 0.05)
                    {
                        healTime = 0;
                        healAmount--;
                        health = Helpers.clampMax(health + 1, maxHealth);
                        if (player == Global.level.mainPlayer)
                        {
                            playSound("heal");
                        }
                    }
                }
            }

            if (Global.level.isRace() && character?.charState is Die)
            {
                destroySelf();
                explode();
                return;
            }

            if (character != null && (character.destroyed || character.rideChaser != this || character.charState is not InRideChaser))
            {
                enterCooldown = 0.5f;
                character.rideChaser = null;
                removeCharacter();
            }

            if (Global.level.isRace() && Global.level.gameMode.setupTime > 0)
            {
                return;
            }

            if (character != null)
            {
                driveCode(out bool shouldDrawIncline, out bool shouldDrawShoot, out bool shouldDrawIdle, out int inclineFrameIndex);

                if (shouldDrawIncline && shouldDrawShoot)
                {
                    changeSpriteIfDifferent("ridechaser_incline_shoot", false);
                    character.changeSpriteFromNameIfDifferent("rc_incline", true);
                    frameIndex = inclineFrameIndex;
                    if (inclineFrameIndex < 1 || character.frameIndex < inclineFrameIndex)
                    {
                        character.frameIndex = inclineFrameIndex;
                    }
                }
                else if (shouldDrawIncline)
                {
                    changeSpriteIfDifferent("ridechaser_incline", false);
                    character.changeSpriteFromNameIfDifferent("rc_incline", true);
                    frameIndex = inclineFrameIndex;
                    if (inclineFrameIndex < 1 || character.frameIndex < inclineFrameIndex)
                    {
                        character.frameIndex = inclineFrameIndex;
                    }
                }
                else if (shouldDrawShoot)
                {
                    changeSpriteIfDifferent("ridechaser_idle_shoot", true);
                    character.changeSpriteFromNameIfDifferent("rc_idle", true);
                }
                else if (shouldDrawIdle)
                {
                    changeSpriteIfDifferent("ridechaser_idle", true);
                    character.changeSpriteFromNameIfDifferent("rc_idle", true);
                }
            }

            if (character == null)
            {
                speed = Helpers.moveTo(speed, 0, 300 * Global.spf, snap: true);
            }

            bounceSpeed = Helpers.moveTo(bounceSpeed, 0, 600 * Global.spf, snap: true);

            Point moveAmount = new Point(xDir * speed, 0);
            if (bounceSpeed > 0)
            {
                moveAmount = new Point(-xDir * bounceSpeed, 0);
            }
            moveAmount.x += xMomentum;

            bool hitSideWall = false;
            if (inclineTime == 0)
            {
                if (sprite.hitboxes.Count > 0)
                {
                    var hit = Global.level.checkCollisionShape(sprite.hitboxes[0].shape, null);
                    if (hit?.isSideWallHit() == true && Math.Sign(hit.getNormalSafe().x) != Math.Sign(moveAmount.x))
                    {
                        hitSideWall = true;
                    }
                }
            }
            else
            {
                var hit = Global.level.checkCollisionShape(globalCollider.shape, null);
                if (hit?.isSideWallHit() == true && Math.Sign(hit.getNormalSafe().x) != Math.Sign(moveAmount.x))
                {
                    hitSideWall = true;
                }
            }

            if (!moveAmount.isZero() && !hitSideWall)
            {
                move(moveAmount);
            }

            bool stuck2 = MathF.Abs(deltaPos.x) < 0.1f && speed > 10;
            if (stuck2 && deltaPos.y < 0)
            {
                //pos.inc(new Point(0, -1));
            }

            bool stuck = hitSideWall;
            if (stuck)
            {
                stuckFrames++;

                if (stuckFrames >= 2)
                {
                    if (character != null && canGetHurtFromCarCrash())
                    {
                        character.playSound("hurt", sendRpc: true);
                        character.playSound("rcCrash", sendRpc: true);
                        character.shakeCamera(sendRpc: true);
                        character.applyDamage(null, null, 4, null);
                        applyDamage(null, null, 8, (int)ProjIds.RideChaserCrash);
                        bounceSpeed = speed * 0.75f;
                    }
                    speed = 0;
                    stuckFrames = 0;
                }
            }
            else
            {
                stuckFrames = 0;
            }

            character?.changePos(pos);
        }

        float xMomentum;
        public void addXMomentum(float moveX)
        {
            xMomentum = moveX;
        }

        public void fadeXMomentum()
        {
            xMomentum -= Global.spf * 400;
            if (xMomentum < 0) xMomentum = 0;
        }

        float bounceSpeed;
        int stuckFrames;
        float soundTime;
        bool isShooting;
        float shootTime;
        bool isJumping;
        bool isDashing;
        public bool isTurning;
        float speed = 0;
        int destXDir;
        float inclineTime;
        const float maxInclineTime = 0.15f;
        float jumpTime;
        Point? lastInclineVec;
        GameObject lastInclineObj;
        const float jumpInterval = 0.15f;
        public void driveCode(out bool shouldDrawIncline, out bool shouldDrawShoot, out bool shouldDrawIdle, out int inclineFrameIndex)
        {
            shouldDrawIncline = false;
            shouldDrawShoot = false;
            shouldDrawIdle = true;
            inclineFrameIndex = 0;

            if (bounceSpeed > 0) return;

            // Acceleration/decceleration
            float topSpeed = isDashing ? 400 : 200;
            float acc = isDashing ? 600 : 300;
            if (character.flag != null) topSpeed *= 0.5f;
            else if (slowdownTime > 0) topSpeed *= 0.75f;

            if (!isTurning)
            {
                float incAmount = Global.spf * acc;
                if (speed < topSpeed) speed += incAmount;
                else if (speed > topSpeed) speed -= incAmount;
                if (MathF.Abs(speed - topSpeed) < incAmount * 2) speed = topSpeed;
            }
            else
            {
                speed = Helpers.lerp(speed, 0, Global.spf * 5);
            }

            // Sound
            if (grounded && !isTurning)
            {
                if (!isDashing)
                {
                    Helpers.decrementTime(ref soundTime);
                    if (soundTime == 0 && !isWading())
                    {
                        playSound("rc", sendRpc: true);
                        soundTime = 0.2f;
                    }
                }
                else
                {
                    Helpers.decrementTime(ref soundTime);
                    if (soundTime == 0)
                    {
                        playSound("rcDash", sendRpc: true);
                        soundTime = 0.2f;
                    }
                }
            }

            // Incline and incline jumping
            var groundHit = Global.level.raycast(pos, pos.addxy(0, 15), Helpers.wallTypeList);

            // If we just left the previous frame incline, leap off that incline
            if (lastInclineObj != null && groundHit?.gameObject != lastInclineObj && !isJumping && lastInclineVec.Value.y < 0)
            {
                Point leapVel = lastInclineVec.Value.times(speed);
                vel.y = leapVel.y * 1.5f;
                jumpTime = jumpInterval * 2f;
                isJumping = true;
                grounded = false;
            }

            if (groundHit?.getNormalSafe().isAngled() == true)
            {
                inclineTime += Global.spf;
                if (inclineTime > maxInclineTime) inclineTime = maxInclineTime;
                lastInclineVec = groundHit.getNormalSafe().leftOrRightNormal(xDir);
                lastInclineObj = groundHit.gameObject;
            }
            else
            {
                Helpers.decrementTime(ref inclineTime);
                lastInclineVec = null;
                lastInclineObj = null;
            }
            
            if (!isTurning)
            {
                // Not turning code
                if (inclineTime > 0)
                {
                    shouldDrawIncline = true;
                    if (inclineTime >= maxInclineTime)
                    {
                        inclineFrameIndex = 1;
                    }
                }
            }
            else
            {
                // Turning code
                if (isAnimOver())
                {
                    xDir *= -1;
                    character.xDir = destXDir;
                    isTurning = false;
                }

                shouldDrawIncline = false;
                shouldDrawShoot = false;
                shouldDrawIdle = false;

                return;
            }

            // Check for turning code
            if (
                ((player.input.isPressed(Control.Left, player) && xDir != -1) || (player.input.isPressed(Control.Right, player) && xDir != 1)) &&
                !isJumping
                )
            {
                isTurning = true;
                isShooting = false;
                shootTime = 0;
                destXDir = xDir * -1;
                changeSprite("ridechaser_turn", true);
                character.changeSpriteFromName("rc_turn", true);
                playSound("rcTurn", sendRpc: true);

                shouldDrawShoot = false;
                shouldDrawIncline = false;
                shouldDrawIdle = false;

                return;
            }

            // Shooting code
            if (!isShooting)
            {
                if (player.input.isHeld(Control.Shoot, player) && (sprite.name == "ridechaser_idle" || sprite.name == "ridechaser_incline"))
                {
                    sprite.frameTime = 0;
                    isShooting = true;
                    shootTime = 0;
                    shouldDrawShoot = true;
                }
            }
            else
            {
                shouldDrawShoot = true;
                if (shootTime == 0)
                {
                    new RCProj(gunWeapon, getFirstPOIOrDefault(), xDir, Helpers.clampMin0(speed - 150), player, player.getNextActorNetId(), sendRpc: true);
                    new Anim(getFirstPOIOrDefault(), "ridechaser_muzzle", xDir, player.getNextActorNetId(), true, sendRpc: true, host: this);
                    playSound("rcShoot", sendRpc: true);
                }
                shootTime += Global.spf;
                if (shootTime > 0.15f)
                {
                    if (player.input.isHeld(Control.Shoot, player))
                    {
                        shootTime = 0;
                    }
                    else
                    {
                        isShooting = false;
                    }
                }
            }

            // Jumping code
            if (!isJumping)
            {
                if (player.input.isPressed(Control.Jump, player) && sprite.name != "ridechaser_turn" && grounded)
                {
                    isJumping = true;
                    vel.y = -225;
                }
            }
            else
            {
                if (jumpTime < 0.15f && player.input.isHeld(Control.Jump, player))
                {
                    vel.y = -225;
                }
                jumpTime += Global.spf;
                if (jumpTime < jumpInterval)
                {
                    shouldDrawIncline = true;
                }
                else if (jumpTime >= jumpInterval && jumpTime < jumpInterval * 2)
                {
                    inclineFrameIndex = 1;
                    shouldDrawIncline = true;
                }
                else if (jumpTime >= jumpInterval * 2 && jumpTime < jumpInterval * 3)
                {
                    shouldDrawIncline = true;
                }
                else if (jumpTime > jumpInterval * 3)
                {
                    shouldDrawIncline = false;
                }

                if (grounded)
                {
                    jumpTime = 0;
                    isJumping = false;
                }
            }

            // Check for dash code
            if (!isDashing)
            {
                if (player.input.isHeld(Control.Dash, player) && character.flag == null)
                {
                    isDashing = true;
                }
            }
            else
            {
                if (!player.input.isHeld(Control.Dash, player))
                {
                    isDashing = false;
                }
            }
        }

        Sprite rideChaserSparkSprite = Global.sprites["ridechaser_sparks"].clone();
        Sprite rideChaserBoostSprite = Global.sprites["ridechaser_boost"].clone();
        public int drawState = 0;  // 0 = no particle, 1 = sparks, 2 = boost
        public override void render(float x, float y)
        {
            base.render(x, y);

            if (ownedByLocalPlayer)
            {
                drawState = 0;
                if (grounded && character != null && !isTurning)
                {
                    if (!isDashing)
                    {
                        if (!isWading())
                        {
                            drawState = 1;
                        }
                    }
                    else
                    {
                        drawState = 2;
                    }
                }
            }

            if (drawState == 1)
            {
                rideChaserSparkSprite.update();
                rideChaserSparkSprite.draw(rideChaserSparkSprite.frameIndex, pos.x + x - (19 * xDir), pos.y + y, xDir, 1, null, 1, 1, 1, zIndex - 1);
            }
            else if (drawState == 2)
            {
                rideChaserBoostSprite.update();
                rideChaserBoostSprite.draw(rideChaserBoostSprite.frameIndex, pos.x + x - (29 * xDir), pos.y + y - 10, xDir, 1, null, 1, 1, 1, zIndex - 1);
            }
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (isExploding) return;

            /*
            var killZone = other.gameObject as KillZone;
            if (killZone != null && Global.level.isRace())
            {
                killZone.applyDamage(this);
            }
            */

            if (canBeEntered() && enterCooldown == 0)
            {
                var chr = other.otherCollider.actor as Character;
                if (chr != null && chr.canEnterRideChaser() && chr.charState is Fall fallState && MathF.Abs(chr.pos.x - pos.x) < 15)
                {
                    if (Global.serverClient != null)
                    {
                        if (claimed)
                        {
                            return;
                        }
                        else if (!ownedByLocalPlayer && chr.ownedByLocalPlayer)
                        {
                            fallState.setLimboVehicleCheck(this);
                            return;
                        }
                        else if (!(ownedByLocalPlayer && chr.ownedByLocalPlayer))
                        {
                            return;
                        }
                    }

                    putCharInRideChaser(chr);
                }
            }
        }

        public override Collider getGlobalCollider()
        {
            var rect = new Rect(0, 0, 5, 26);
            return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
        }

        public void removeCharacter()
        {
            character = null;
            claimed = false;
            changeSpriteIfDifferent("ridechaser_idle", true);
            isShooting = false;
            isDashing = false;
            isJumping = false;
            isTurning = false;
            inclineTime = 0;
            jumpTime = 0;
            stuckFrames = 0;
        }

        public void addHealth(float amount)
        {
            healAmount += amount;
        }

        public void explode()
        {
            if (!isExploding)
            {
                isExploding = true;
                playSound("rcExplode");
                Anim.createGibEffect("ridechaser_piece", getCenterPos(), netOwner, GibPattern.Radial);
                new ExplodeDieEffect(player ?? netOwner, getCenterPos(), getCenterPos(), "empty", 1, zIndex, false, 35, 0.5f, false);
            }
        }

        public override void onDestroy()
        {
            base.onDestroy();
            hawkElec?.destroySelf();
        }

        public bool canBeEntered()
        {
            return character == null;
        }

        public void putCharInRideChaser(Character chr)
        {
            mountedOnce = true;
            character = chr;
            chr.xDir = xDir;
            chr.rideChaser = this;
            chr.changeState(new InRideChaser(), true);
        }

        public void applyDamage(Player owner, int? weaponIndex, float damage, int? projId)
        {
            if (!ownedByLocalPlayer) return;
            if (Global.level.isRace() && damage != Damager.envKillDamage && projId != (int)ProjIds.RideChaserCrash)
            {
                damage = 0.25f;
            }

            health -= damage;
            if (projId == (int)ProjIds.RideChaserProj)
            {
                //slowdownTime = 0.25f;
            }

            if (owner != null && weaponIndex != null)
            {
                damageHistory.Add(new DamageEvent(owner, weaponIndex.Value, projId, false, Global.time));
            }

            if (health <= 0)
            {
                int? assisterProjId = null;
                int? assisterWeaponId = null;
                Player killer = null;
                Player assister = null;
                getKillerAndAssister(player, ref killer, ref assister, ref weaponIndex, ref assisterProjId, ref assisterWeaponId);
                creditKill(killer, assister, weaponIndex);

                destroySelf();
                explode();
            }
        }

        public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId)
        {
            if (character == null) return false;
            if (Global.level.isRace() && Global.level.gameMode.setupTime > 0) return false;
            return !character.isInvulnerable(true) && character.player.alliance != damagerAlliance;
        }

        public bool isInvincible(Player attacker, int? projId)
        {
            return false;
        }

        public bool canBeHealed(int healerAlliance)
        {
            if (character == null) return false;
            return character.player.alliance == healerAlliance && health > 0 && health < maxHealth;
        }

        public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false)
        {
            if (!allowStacking && this.healAmount > 0) return;
            commonHealLogic(healer, healAmount, health, maxHealth, drawHealText);
            this.healAmount = healAmount;
        }

        bool canRunEnemyOver()
        {
            return MathF.Abs(deltaPos.x) >= 350 * Global.spf;
        }

        bool canHurtEnemy()
        {
            return MathF.Abs(deltaPos.x) >= 225 * Global.spf;
        }

        bool canGetHurtFromCarCrash()
        {
            return speed >= 300;
        }

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            if (player != null && canRunEnemyOver())
            {
                return new GenericMeleeProj(hitWeapon, centerPoint, ProjIds.RideChaserHit, player, 4, Global.defFlinch, 1);
            }
            else if (player != null && canHurtEnemy())
            {
                return new GenericMeleeProj(hitWeapon, centerPoint, ProjIds.RideChaserHit, player, 2, 0, 1);
            }
            return null;
        }

        public override void updateProjFromHitbox(Projectile proj)
        {
            if (player != null && canRunEnemyOver())
            {
                proj.damager.damage = 4;
                proj.damager.flinch = Global.defFlinch;
                proj.damager.owner = player;
            }
            else if (player != null && canHurtEnemy())
            {
                proj.damager.damage = 2;
                proj.damager.flinch = 0;
                proj.damager.owner = player;
            }
            else
            {
                proj.damager.damage = 0;
            }
        }

        public void creditKill(Player killer, Player assister, int? weaponIndex)
        {
            if (killer != null && killer != player)
            {
                /*
                killer.addKill();
                if (Global.level.gameMode is TeamDeathMatch)
                {
                    if (Global.isHost)
                    {
                        if (player.alliance == GameMode.redAlliance) Global.level.gameMode.bluePoints++;
                        if (player.alliance == GameMode.blueAlliance) Global.level.gameMode.redPoints++;
                        Global.level.gameMode.syncTeamScores();
                    }
                }
                */

                killer.awardScrap();
            }

            if (assister != null && assister != player)
            {
                //assister.addAssist();
                //assister.addKill();
                assister.awardScrap();
            }

            if (ownedByLocalPlayer)
            {
                RPC.creditPlayerKillVehicle.sendRpc(killer, assister, this, weaponIndex);
            }
        }
    }

    public class RCProj : Projectile
    {
        public RCProj(Weapon weapon, Point pos, int xDir, float additionalXSpeed, Player player, ushort netProjId, bool sendRpc = false) : 
            base(weapon, pos, xDir, 350 + additionalXSpeed, 1, player, "ridechaser_proj", 0, 0, netProjId, player.ownedByLocalPlayer)
        {
            reflectable = true;
            maxTime = 0.5f;
            projId = (int)ProjIds.RideChaserProj;

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }

    public class InRideChaser : CharState
    {
        public InRideChaser(string transitionSprite = "") : base("rc_idle", "", "", transitionSprite)
        {
        }

        public override void update()
        {
            base.update();

            if (!character.ownedByLocalPlayer) return;

            if (Global.level.isRace())
            {
                if (player.input.isHeld(Control.WeaponLeft, player))
                {
                    character.camOffsetX -= Global.spf * 150;
                    character.camOffsetX = MathF.Round(character.camOffsetX);
                    if (character.camOffsetX < -100) character.camOffsetX = -100;
                }
                else if (player.input.isHeld(Control.WeaponRight, player))
                {
                    character.camOffsetX += Global.spf * 150;
                    character.camOffsetX = MathF.Round(character.camOffsetX);
                    if (character.camOffsetX > 100) character.camOffsetX = 100;
                }
            }

            if (character.rideChaser == null || character.rideChaser.destroyed)
            {
                if (Global.level.isRace())
                {
                    character.applyDamage(null, null, Damager.envKillDamage, null);
                }
                else
                {
                    character.changeToIdleOrFall();
                }
                return;
            }

            if (!Global.level.isRace())
            {
                bool ejectInput = character.player.input.isHeld(Control.Up, player) && character.player.input.isPressed(Control.Jump, player);
                if (ejectInput && !character.rideChaser.isTurning)
                {
                    character.vel.y = -character.getJumpPower();
                    character.incPos(new Point(0, -5));
                    character.changeState(new Jump(), true);
                }
            }

            if (Global.level.gameMode.isOver)
            {
                character.changeToIdleOrFall();
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.stopMoving();
            character.useGravity = false;
            character.setGlobalColliderTrigger(true);
            var mechWeapon = player.weapons.FirstOrDefault(m => m is MechMenuWeapon) as MechMenuWeapon;
            if (mechWeapon != null) mechWeapon.isMenuOpened = false;
            character.rideChaser.setzIndex(character.zIndex - 1);
            if (character.isCharging())
            {
                character.stopCharge();
            }
        }

        public override void onExit(CharState newState)
        {
            character.rideChaser = null;
            character.useGravity = true;
            character.setGlobalColliderTrigger(false);
            base.onExit(newState);
        }
    }
}
