using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MMXOnline
{
    public class Projectile : Actor
    {
        public Damager damager;
        public Player owner
        {
            get { return damager.owner; }
        }
        public string fadeSprite;
        public string fadeSound;
        public float time = 0;
        public float maxTime = float.MaxValue;
        public bool fadeOnAutoDestroy;

        public float moveDistance;
        public float maxDistance = float.MaxValue;

        public Weapon weapon;
        public bool destroyOnHit = true;
        public bool destroyOnHitWall = false;
        public bool reflectable = false;
        public bool reflectable2 = false;
        public int reflectCount;
        public bool shouldShieldBlock = true;
        public int projId;
        public float speed;
        public int healAmount;
        public bool isShield;
        public bool isReflectShield;
        public bool isDeflectShield;
        public bool shouldVortexSuck = true;
        bool damagedOnce;
        public ShaderWrapper nightmareZeroShader;
        //public int? destroyFrames;

        public Projectile(Weapon weapon, Point pos, int xDir, float speed, float damage, Player player, string sprite, int flinch, float hitCooldown, ushort? netId, bool ownedByLocalPlayer) : base(sprite, pos, netId, ownedByLocalPlayer, false)
        {
            this.weapon = weapon;
            this.speed = speed;
            vel = new Point(speed * xDir, 0);
            useGravity = false;
            damager = new Damager(player, damage, flinch, hitCooldown);
            this.xDir = xDir;
            if (Global.level.gameMode.isTeamMode && !(this is NapalmPartProj) && !(this is FlameBurnerProj))
            {
                if (player.alliance == GameMode.blueAlliance)
                {
                    addRenderEffect(RenderEffectType.BlueShadow);
                }
                else
                {
                    addRenderEffect(RenderEffectType.RedShadow);
                }
            }
        }

        public void setIndestructableProperties()
        {
            destroyOnHit = false;
            shouldVortexSuck = false;
            shouldShieldBlock = false;
        }

        public float getSpeed() 
        {
            return vel.magnitude;
        }

        public int getRpcAngle()
        {
            if (angle == null) return 0;
            return (int)(Helpers.to360(angle.Value) * 0.5f);
        }

        const float leeway = 500;
        public override void update() 
        {
            base.update();

            time += Global.spf;
            moveDistance += deltaPos.magnitude;

            /*
            if (moveDistance > Global.screenW)
            {
                if (this is SpinWheelProj || this is WheelGSpinWheelProj || this is MovingWheelProj || this is BoundBlasterProj || (this is ShotgunIceProjSled sip && !sip.ridden) || this is ElectricSparkProjCharged || this is TriadThunderProjCharged ||
                    this is SonicSlicerProjCharged || this is OverdriveOSonicSlicerUpProj)
                {
                    netcodeOverride = NetcodeModel.FavorDefender;
                }
            }
            */
            
            if (time > maxTime || moveDistance > maxDistance || pos.x > Global.level.width + leeway || pos.x < -leeway || pos.y > Global.level.height + leeway || pos.y < -leeway)
            {
                if (fadeOnAutoDestroy) destroySelf();
                else destroySelfNoEffect();
                return;
            }
        }

        public override void postUpdate()
        {
            base.postUpdate();
            /*
            if (!destroyed && destroyFrames != null)
            {
                destroyFrames--;
                if (destroyFrames <= 0)
                {
                    destroySelf();
                }
            }
            */
        }

        public override List<ShaderWrapper> getShaders()
        {
            var shaders = new List<ShaderWrapper>();
            if (owner?.character?.isNightmareZeroBS.getValue() == true && Global.shaders.ContainsKey("nightmareZero"))
            {
                if (nightmareZeroShader == null)
                {
                    nightmareZeroShader = Helpers.cloneNightmareZeroPaletteShader("paletteNightmareZero");
                }
                shaders.Add(nightmareZeroShader);
            }
            if (shaders.Count > 0)
            {
                return shaders;
            }
            else
            {
                return base.getShaders();
            }
        }

        public void reflect(Player player, bool playDingSound = true) 
        {
            if (reflectCount > 0) return;
            reflectCount++;
            if (playDingSound) playSound("ding");
            if (this is RayGunProj)
            {
                (this as RayGunProj).reflectSide();
                damager.owner = player;
                return;
            }
            if (this is BoundBlasterProj)
            {
                (this as BoundBlasterProj).reflectSide();
                damager.owner = player;
                return;
            }
            
            damager.owner = player;
            
            xDir *= -1;
            time = 0;
            if (this is ElectricSparkProj) {
                vel.y *= -1;
            }
            if (this is RaySplasherProj)
            {
                float randY = Helpers.randomRange(-2f, 1f);
                vel.y *= randY;
            }
            if (this is StingProj)
            {
                vel.y *= -1;
                yDir *= -1;
            }
            vel.x *= -1;
        }

        public void deflect(Player player, bool playDingSound = true)
        {
            if (reflectCount > 0) return;
            reflectCount++;
            if (playDingSound) playSound("ding");
            damager.owner = player;
            time = 0;

            if (damager.damage > 1)
            {
                destroySelf();
            }

            float velAngle = vel.angle;
            if ((velAngle > 45 && velAngle < 135) || (velAngle > 225 && velAngle < 315))
            {
                angle = Helpers.to360(velAngle + Helpers.SignOr1(vel.x) * 135);
                vel = Point.createFromAngle(angle.Value).times(vel.magnitude);
                xDir = 1;
            }
            else
            {
                angle = Helpers.to360(velAngle - Helpers.SignOr1(vel.x) * 135);
                vel = Point.createFromAngle(angle.Value).times(vel.magnitude);
                xDir = 1;
            }

            if (this is RayGunProj)
            {
                (this as RayGunProj).onDeflect();
                return;
            }
            else if (this is BoundBlasterProj)
            {
                (this as BoundBlasterProj).onDeflect();
                return;
            }
        }

        // Airblast reflect
        public void reflect2(Player player, float reflectAngle, bool sendRpc = false)
        {
            if (reflectCount > 0) return;
            reflectCount++;

            if (this is TorpedoProj torpedo)
            {
                torpedo.reflect(reflectAngle);
            }
            else if (this is GrenadeProj grenade)
            {
                if (grenade.deltaPos.isZero()) return;
                grenade.vel = Point.createFromAngle(reflectAngle).times(grenade.speed);
            }
            else
            {
                if (customAngleRendering)
                {
                    customAngleRendering = false;
                    xDir = 1;
                    angle = reflectAngle;
                    vel = Point.createFromAngle(reflectAngle).times(vel.magnitude);
                }
                else
                {
                    angle = reflectAngle;
                    vel = Point.createFromAngle(reflectAngle).times(vel.magnitude);
                }
            }

            damager.owner = player;

            time = 0;

            if (sendRpc)
            {
                RPC.reflectProj.sendRpc(netId, owner.id, reflectAngle);
            }
        }

        public bool getHeadshotVictim(Player player, out IDamagable victim, out Point? hitPoint)
        {
            victim = null;
            hitPoint = null;
            float w = collider?.shape.getRect().w() ?? 2;
            foreach (var enemy in Global.level.players)
            {
                var headPosNullable = enemy?.character?.getHeadPos();
                if (headPosNullable != null && enemy.character.canBeDamaged(player.alliance, player.id, projId))
                {
                    var headPos = headPosNullable.Value;
                    Shape headShape = enemy.character.getHeadRect().getShape();

                    Point dirToDest = vel.normalize();
                    var positions = new List<Point>();
                    positions.Add(pos);
                    positions.Add(pos.add(dirToDest.leftNormal().times(w - 1)));
                    positions.Add(pos.add(dirToDest.rightNormal().times(w - 1)));
                    for (int i = 0; i < positions.Count; i++)
                    {
                        var intersectPoint = headShape.getIntersectPoint(positions[i], vel.times(Global.spf * 3));
                        if (intersectPoint != null)
                        {
                            victim = enemy.character;
                            hitPoint = intersectPoint.Value;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override void destroySelf(string spriteName = null, string fadeSound = null, bool rpc = false, bool doRpcEvenIfNotOwned = false, bool favorDefenderProjDestroy = false)
        {
            base.destroySelf(fadeSprite, this.fadeSound, rpc, doRpcEvenIfNotOwned, favorDefenderProjDestroy: favorDefenderProjDestroy);
        }

        public void destroySelfNoEffect(bool rpc = false, bool doRpcEvenIfNotOwned = false)
        {
            base.destroySelf(null, null, rpc, doRpcEvenIfNotOwned);
        }

        public static bool charsCanClang(Character attacker, Character defender)
        {
            if (attacker == null || defender == null) return false;
            if (attacker.player.alliance == defender.player.alliance) return false;
            if (!defender.sprite.name.Contains("attack") && !defender.sprite.name.Contains("block")) return false;
            if (defender.sprite.name.Contains("sigma2")) return false;
            if (attacker.isHyperZero()) return false;

            // Not facing each other
            if (attacker.pos.x >= defender.pos.x && (attacker.xDir != -1 || defender.xDir != 1)) return false;
            if (attacker.pos.x < defender.pos.x && (attacker.xDir != 1 || defender.xDir != -1)) return false;

            return true;
        }

        public bool canClangChar()
        {
            return isShield || isDeflectShield || isReflectShield;
        }

        public bool canBeParried()
        {
            return (this is GenericMeleeProj || this is SigmaSlashProj);
        }

        bool clangedOnce;
        public override void onCollision(CollideData other) 
        {
            if (weapon == null) return;
            //if (destroyed) return;    // If this causes issues use the destroyFrames system instead

            var otherProj = other.gameObject as Projectile;

            // Sonic slicer interaction with wire sponge spin
            if (otherProj != null)
            {
                if (Damager.isSonicSlicer(projId) && otherProj.projId == (int)ProjIds.WSpongeChainSpin)
                {
                    return;
                }
                if (Damager.isSonicSlicer(otherProj.projId) && projId == (int)ProjIds.WSpongeChainSpin)
                {
                    return;
                }
            }

            var gmp = this as GenericMeleeProj;
            var isSaber = gmp != null && gmp.isZSaber();
            if (isSaber && owner.character?.isCCImmune() != true)
            {
                // Case 1: hitting a clangable projectile.
                if (ownedByLocalPlayer && otherProj != null && otherProj.owner.alliance != owner.alliance)
                {
                    if ((otherProj.canClangChar() && charsCanClang(owner.character, otherProj.owner.character)) || otherProj.isShield)
                    {
                        if (!clangedOnce) clangedOnce = true;
                        else return;

                        owner.character.changeState(new ZeroClang(-owner.character.xDir), true);
                        owner.character.playSound("ding", sendRpc: true);

                        if (other.hitData.hitPoint != null)
                        {
                            new Anim(other.hitData.hitPoint.Value, "zsaber_shot_fade", 1, owner.getNextActorNetId(), true, sendRpc: true);
                        }

                        destroySelf();
                        return;
                    }
                }

                // Case 2: hitting a zero that's in swordblock state. Projectile should not run any damage code
                // This logic could have also lived in "canBeDamaged" but since it's related to the clang code above, it's put here
                if (other.gameObject is Character chr)
                {
                    if (charsCanClang(owner.character, chr))
                    {
                        return;
                    }
                }
            }

            if (ownedByLocalPlayer)
            {
                if (shouldVortexSuck && otherProj is GenericMeleeProj otherGmp && otherProj.projId == (int)ProjIds.WheelGEat && damager.owner.alliance != otherProj.damager.owner.alliance && otherGmp.owningActor is WheelGator wheelGator)
                {
                    destroySelfNoEffect();
                    if (wheelGator.ownedByLocalPlayer)
                    {
                        wheelGator.feedWheelGator(damager.damage);
                        return;
                    }
                    else
                    {
                        RPC.feedWheelGator.sendRpc(wheelGator, damager.damage);
                    }
                    return;
                }

                if (shouldVortexSuck && otherProj is GenericMeleeProj otherGmp2 && otherProj.projId == (int)ProjIds.DrDopplerAbsorb && damager.owner.alliance != otherProj.damager.owner.alliance && otherGmp2.owningActor is DrDoppler drDoppler)
                {
                    destroySelfNoEffect();
                    if (drDoppler.ownedByLocalPlayer)
                    {
                        drDoppler.healDrDoppler(damager.owner, damager.damage);
                        return;
                    }
                    else
                    {
                        RPC.healDoppler.sendRpc(drDoppler, damager.damage, damager.owner);
                    }
                    return;
                }

                if (this is ShotgunIceProj)
                {
                    var shotgunIceProj = this as ShotgunIceProj;
                    if (shotgunIceProj == other.gameObject) return;
                }

                var otherRs = other.gameObject as RollingShieldProj;
                var otherRsc = other.gameObject as RollingShieldProjCharged;
                if ((otherRs != null || otherRsc != null) && damager.owner.alliance != otherProj.damager.owner.alliance)
                {
                    if (this is ElectricSparkProj || this is ElectricSparkProjCharged || this is PlasmaGunProj || projId == (int)ProjIds.SparkMSpark)
                    {
                        otherRs?.destroySelf(doRpcEvenIfNotOwned: true);
                        otherRsc?.destroySelf(doRpcEvenIfNotOwned: true);
                    }
                    else
                    {
                        if (otherRsc != null)
                        {
                            float decAmount = damager.damage * 2;
                            otherRsc.decAmmo(decAmount);
                            var bytes = BitConverter.GetBytes(decAmount);
                            Global.serverClient?.rpc(RPC.decShieldAmmo, (byte)otherProj.damager.owner.id, bytes[0], bytes[1], bytes[2], bytes[3]);
                        }

                        if (shouldShieldBlock)
                        {
                            destroySelf(fadeSprite, fadeSound);
                            return;
                        }
                    }
                }

                if (otherProj != null && otherProj.isReflectShield && reflectable && damager.owner.alliance != otherProj.damager.owner.alliance)
                {
                    if (deltaPos.x != 0 && Math.Sign(deltaPos.x) != otherProj.xDir)
                    {
                        reflect(otherProj.owner, playDingSound: false);
                        playSound("ding", sendRpc: true);
                    }
                }

                if (otherProj != null && otherProj.isDeflectShield && reflectable && damager.owner.alliance != otherProj.damager.owner.alliance)
                {
                    if (deltaPos.x != 0 && Math.Sign(deltaPos.x) != otherProj.xDir)
                    {
                        deflect(otherProj.owner, playDingSound: false);
                        playSound("ding", sendRpc: true);
                    }
                }

                if (otherProj != null && otherProj.isShield && damager.owner.alliance != otherProj.damager.owner.alliance)
                {
                    if ((this is ParasiticBombProj || this is ParasiticBombProjCharged) &&
                        (otherProj.projId == (int)ProjIds.FrostShield || otherProj.projId == (int)ProjIds.FrostShieldAir || otherProj.projId == (int)ProjIds.FrostShieldGround))
                    {
                        // Parasitic bomb should go straight through frost shield projectiles
                    }
                    else
                    {
                        bool isDestroyable = otherProj is IDamagable;
                        if (shouldShieldBlock && !isDestroyable)
                        {
                            destroySelf(fadeSprite, fadeSound);
                            playSound("ding", sendRpc: true);
                            return;
                        }
                    }
                }

                var otherBsc = other.gameObject as BubbleSplashProjCharged;
                if (otherBsc != null && damager.owner.alliance != otherProj.damager.owner.alliance)
                {
                    otherBsc.destroySelf(doRpcEvenIfNotOwned: true);
                    if (shouldShieldBlock && !(this is SpinWheelProj) && !(this is SpinWheelProjCharged) && !(this is SpinWheelProjCharged))
                    {
                        destroySelf(fadeSprite, fadeSound);
                        return;
                    }
                }

                if (otherProj != null && otherProj is FrostShieldProj && projId == (int)ProjIds.TBreaker)
                {
                    otherProj.destroySelf(doRpcEvenIfNotOwned: true);
                }

                var otherPenguinSled = other.gameObject as ShotgunIceProjSled;
                if ((this is FireWaveProj || this is FireWaveProjCharged) && otherPenguinSled != null && damager.owner.alliance != otherPenguinSled.damager.owner.alliance)
                {
                    otherPenguinSled.destroySelf(doRpcEvenIfNotOwned: true);
                }

                var otherFireWaveCharged = other.gameObject as FireWaveProjCharged;
                if ((this is TornadoProj || this is TornadoProjCharged) && otherFireWaveCharged != null && damager.owner.alliance != otherFireWaveCharged.damager.owner.alliance)
                {
                    otherFireWaveCharged.putOutFire();
                }
                var otherFireWaveChargedStart = other.gameObject as FireWaveProjChargedStart;
                if ((this is TornadoProj || this is TornadoProjCharged) && otherFireWaveChargedStart != null && damager.owner.alliance != otherFireWaveChargedStart.damager.owner.alliance)
                {
                    otherFireWaveChargedStart.putOutFire();
                }
                var otherFlameMBigFire = other.gameObject as FlameMBigFireProj;
                if ((this is TornadoProj || this is TornadoProjCharged) && otherFlameMBigFire != null && damager.owner.alliance != otherFlameMBigFire.damager.owner.alliance)
                {
                    otherFlameMBigFire.destroySelf(doRpcEvenIfNotOwned: true);
                }

                var otherSpeedBurner = other.gameObject as SpeedBurnerProj;
                if ((this is BubbleSplashProj || this is BubbleSplashProjCharged) && otherSpeedBurner != null && damager.owner.alliance != otherSpeedBurner.damager.owner.alliance)
                {
                    otherSpeedBurner.destroySelf(doRpcEvenIfNotOwned: true);
                }
            }

            var character = other.gameObject as Character;
            var damagable = other.gameObject as IDamagable;
            var damagableActor = damagable as Actor;

            if (damagable != null && !damagedOnce && other.otherCollider.isHurtBox())
            {
                bool canBeDamaged = damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, projId);

                if (canBeDamaged)
                {
                    Point? hitPos = other.otherCollider.shape.getIntersectPoint(pos, vel);
                    if (hitPos != null && destroyOnHit) changePos(hitPos.Value);

                    bool weakness = false;
                    if (character != null && character.player.isX)
                    {
                        int wi = character.player.weapon.weaknessIndex;
                        if (wi > 0 && wi == weapon.index) weakness = true;
                    }

                    if (this is BlackArrowProj && destroyed) return;
                    if ((this is BlackArrowProj || this is SpiralMagnumProj) && character?.isAlwaysHeadshot() == true)
                    {
                        weakness = true;
                    }

                    if (owner.ownedByLocalPlayer && projId == (int)ProjIds.CopyShot && character != null)
                    {
                        owner.copyShotDamageEvents.Add(new CopyShotDamageEvent(character));
                    }

                    if (shouldDealDamage(damagable))
                    {
                        if (!isDefenderFavored())
                        {
                            if (ownedByLocalPlayer)
                            {
                                damager.applyDamage(damagable, weakness, weapon, this, projId);
                            }
                        }
                        else
                        {
                            if (damagable.actor().ownedByLocalPlayer)
                            {
                                damager.applyDamage(damagable, weakness, weapon, this, projId);
                            }
                        }
                    }

                    onHitDamagable(damagable);
                }

                bool isMaverickHealProj = projId == (int)ProjIds.MorphMCScrap || projId == (int)ProjIds.MorphMPowder;
                if (ownedByLocalPlayer && (damagable != damager.owner.character || isMaverickHealProj) && !damager.owner.mavericks.Contains(damagable as Maverick) && damagable.canBeHealed(damager.owner.alliance) && healAmount > 0)
                {
                    if (Global.serverClient == null || damagableActor.ownedByLocalPlayer)
                    {
                        damagable.heal(owner, healAmount, allowStacking: false, drawHealText: true);
                    }
                    else
                    {
                        RPC.heal.sendRpc(owner, damagableActor.netId.Value, healAmount);
                    }
                    onHitDamagable(damagable);
                }

                // Vaccination
                if (projId == (int)ProjIds.DrDopplerBall2 && ownedByLocalPlayer && damagable is Character damagableChr && damagableChr.player.alliance == damager.owner.alliance)
                {
                    playSound("drDopplerVaccine", sendRpc: true);
                    damagableChr.addVaccineTime(2);
                    RPC.actorToggle.sendRpc(damagableChr.netId, RPCActorToggleType.AddVaccineTime);
                    destroySelf();
                }
            }

            var wall = other.gameObject;
            if (wall is Wall)
            {
                onHitWall(other);
            }
        }

        public virtual bool shouldDealDamage(IDamagable damagable)
        {
            return true;
        }

        // This method is run even on non-owners of the projectile, so it can be used to apply effects to other clients without creating a new RPC
        // However, this means that if you want a owner-only effect, you need an ownedByLocalPlayer check in front.
        // Keep in mind, the client's collision check would use a "favor the defender model", so if you want an effect to favor the shooter, then
        // it needs to be in the Damager class as a "on<PROJ>Damage() method"
        // Also, this runs on every hit regardless of hit cooldown, so if hit cooldown must be factored, use onDamage
        public virtual void onHitDamagable(IDamagable damagable)
        {
            if (destroyOnHit)
            {
                damagedOnce = true;
                destroySelf(fadeSprite, fadeSound, favorDefenderProjDestroy: isDefenderFavoredAndOwner());
            }
        }
        
        // Can be used in lieu of the on<PROJ>Damage() method in damager method with caveat that this causes issues where the actor isn't created yet leading to point blank shots under lag not running this
        public virtual DamagerMessage onDamage(IDamagable damagable, Player attacker)
        {
            return null;
        }

        // Updates damager values and sends them over the network.
        public void updateDamager(float? damage = null, int? flinch = null)
        {
            if (damager == null) return;

            damager.damage = damage ?? damager.damage;
            damager.flinch = flinch ?? damager.flinch;
            if (ownedByLocalPlayer && netId != null)
            {
                RPC.changeDamage.sendRpc(netId.Value, damager.damage, damager.flinch);
            }
        }

        // Run on non-owners as well
        public virtual void onHitWall(CollideData other) 
        {
            if (ownedByLocalPlayer && destroyOnHitWall)
            {
                destroySelf();
            }
        }

        private void rpcCreateHelper(Point pos, Player player, ushort netProjId, int xDirOrAngle, bool isAngle, float damage, int flinch, params byte[] extraData)
        {
            byte[] projIdBytes = BitConverter.GetBytes((ushort)projId);
            byte[] xBytes = BitConverter.GetBytes(pos.x);
            byte[] yBytes = BitConverter.GetBytes(pos.y);
            byte[] netProjIdByte = BitConverter.GetBytes(netProjId);
            byte[] damageBytes = BitConverter.GetBytes(damage);
            byte flinchByte = (byte)flinch;

            var bytes = new List<byte>()
            {
                projIdBytes[0], projIdBytes[1],
                xBytes[0], xBytes[1], xBytes[2], xBytes[3],
                yBytes[0], yBytes[1], yBytes[2], yBytes[3],
                (byte)player.id,
                netProjIdByte[0], netProjIdByte[1],
                isAngle ? (byte)xDirOrAngle : (byte)(xDir + 128),
                damageBytes[0], damageBytes[1], damageBytes[2], damageBytes[3],
                flinchByte,
            };

            if (extraData != null && extraData.Length > 0)
            {
                bytes.AddRange(extraData);
            }

            Global.serverClient?.rpc(RPC.createProj, bytes.ToArray());
        }

        public virtual void rpcCreate(Point pos, Player player, ushort netProjId, int xDir, params byte[] extraData)
        {
            rpcCreateHelper(pos, player, netProjId, xDir, false, damager.damage, damager.flinch, extraData);
        }

        public virtual void rpcCreateAngle(Point pos, Player player, ushort netProjId, int angle, params byte[] extraData)
        {
            rpcCreateHelper(pos, player, netProjId, angle, true, damager.damage, damager.flinch, extraData);
        }

        bool acidFadeOnce;
        public void acidFadeEffect()
        {
            if (!acidFadeOnce) acidFadeOnce = true;
            else return;

            new Anim(pos.addxy(0, -3), "dust", 1, owner.getNextActorNetId(), true, true, ownedByLocalPlayer) { vel = new Point(0, -50) };
            new Anim(pos.addxy(-3, 0), "acidburst_fade", 1, owner.getNextActorNetId(), true, true, ownedByLocalPlayer);
            new Anim(pos.addxy(0, -6), "acidburst_fade", 1, owner.getNextActorNetId(), true, true, ownedByLocalPlayer);
            new Anim(pos.addxy(3, -3), "acidburst_fade", 1, owner.getNextActorNetId(), true, true, ownedByLocalPlayer);
        }

        public void acidSplashEffect(CollideData other, ProjIds projId)
        {
            Point hitPoint = other.hitData.hitPoint ?? pos;
            int yDir = 1;
            int downY = 1;
            Point norm = other.hitData.normal ?? new Point(0, -1);
            bool isSide = norm.isSideways();
            if (!isSide && norm.y > 0)
            {
                yDir = -1;
                downY = 0;
            }

            acidSplashParticles(hitPoint, isSide, yDir, downY, projId);

            var splash = new Anim(hitPoint, isSide ? "acidburst_splash_side" : "acidburst_splash", xDir, owner.getNextActorNetId(), true, true, ownedByLocalPlayer);
            splash.yDir = yDir;
            var smoke = new Anim(hitPoint.addxy(-5, 0), "dust", 1, owner.getNextActorNetId(), true, true, ownedByLocalPlayer) { vel = new Point(0, -25) };
            smoke = new Anim(hitPoint.addxy(0, -5), "dust", 1, owner.getNextActorNetId(), true, true, ownedByLocalPlayer) { vel = new Point(0, -25) };
            smoke = new Anim(hitPoint.addxy(-2.5f, 5), "dust", 1, owner.getNextActorNetId(), true, true, ownedByLocalPlayer) { vel = new Point(0, -25) };
        }

        public void acidSplashParticles(Point hitPoint, bool isSide, int yDir, int downY, ProjIds projId)
        {
            if (!isSide)
            {
                new AcidBurstProjSmall(weapon, hitPoint.addxy(0, -5 * yDir), 1, new Point(xDir * 50, -150 * downY), projId, owner, owner.getNextActorNetId(), rpc: true);
                new AcidBurstProjSmall(weapon, hitPoint.addxy(0, -5 * yDir), 1, new Point(xDir * 25, -150 * downY), projId, owner, owner.getNextActorNetId(), rpc: true);
                new AcidBurstProjSmall(weapon, hitPoint.addxy(0, -5 * yDir), 1, new Point(xDir * -25, -150 * downY), projId, owner, owner.getNextActorNetId(), rpc: true);
                new AcidBurstProjSmall(weapon, hitPoint.addxy(0, -5 * yDir), 1, new Point(xDir * -50, -150 * downY), projId, owner, owner.getNextActorNetId(), rpc: true);
            }
            else
            {
                new AcidBurstProjSmall(weapon, hitPoint.addxy(-5 * xDir, 0), 1, new Point(-xDir * 10, -150), projId, owner, owner.getNextActorNetId(), rpc: true);
                new AcidBurstProjSmall(weapon, hitPoint.addxy(-5 * xDir, 0), 1, new Point(-xDir * 20, -75), projId, owner, owner.getNextActorNetId(), rpc: true);
                new AcidBurstProjSmall(weapon, hitPoint.addxy(-5 * xDir, 0), 1, new Point(-xDir * 30, -35), projId, owner, owner.getNextActorNetId(), rpc: true);
                new AcidBurstProjSmall(weapon, hitPoint.addxy(-5 * xDir, 0), 1, new Point(-xDir * 40, 0), projId, owner, owner.getNextActorNetId(), rpc: true);
            }
        }

        public float shieldBounceTimeX = 0;
        public float shieldBounceTimeY = 0;
        public float shieldBounceMaxTime = 0.25f;
        public float halfShieldBounceMaxTime { get { return shieldBounceMaxTime / 2f; } }

        public void updateBubbleBounce()
        {
            if (shieldBounceTimeY > 0)
            {
                shieldBounceTimeY += Global.spf;
                if (shieldBounceTimeY > shieldBounceMaxTime)
                {
                    shieldBounceTimeY = 0;
                    xScale = 1;
                    yScale = 1;
                }
                else
                {
                    // progress will go from 0 to 1 and then back to 0 in shieldBounceMaxTime seconds
                    float progress = shieldBounceTimeY / halfShieldBounceMaxTime;
                    if (progress > 1) progress = 2 - progress;
                    yScale = 1 - (progress * 0.5f);
                    xScale = 1 + (progress * 0.5f);
                }
            }

            if (shieldBounceTimeX > 0)
            {
                shieldBounceTimeX += Global.spf;
                if (shieldBounceTimeX > shieldBounceMaxTime)
                {
                    shieldBounceTimeX = 0;
                    xScale = 1;
                    yScale = 1;
                }
                else
                {
                    // progress will go from 0 to 1 and then back to 0 in shieldBounceMaxTime seconds
                    float progress = shieldBounceTimeX / halfShieldBounceMaxTime;
                    if (progress > 1) progress = 2 - progress;
                    xScale = 1 - (progress * 0.5f);
                    yScale = 1 + (progress * 0.5f);
                }
            }
        }

        public void startShieldBounceX()
        {
            if (shieldBounceTimeX == 0)
            {
                shieldBounceTimeX = Global.spf;
            }
        }

        public void startShieldBounceY()
        {
            if (shieldBounceTimeY == 0)
            {
                shieldBounceTimeY = Global.spf;
            }
        }

        public float wallCrawlSpeed = 250;
        public bool wallCrawlUpdateAngle;

        int wallCrawlDir = 1;
        WallPathNode currentNode;
        bool useLegacyWallCrawl;

        public void setupWallCrawl(Point initialMoveDir)
        {
            useLegacyWallCrawl = Global.level.levelData.wallPathNodes.Count == 0;
            if (ownedByLocalPlayer)
            {
                if (useLegacyWallCrawl)
                {
                    setupLegacyWallCrawl();
                }
                else
                {
                    setupModernWallCrawl(initialMoveDir);
                }
            }
        }

        public void updateWallCrawl()
        {
            if (useLegacyWallCrawl)
            {
                updateLegacyWallCrawl();
            }
            else
            {
                updateModernWallCrawl();
            }
        }

        public struct WallPathNodeData
        {
            public WallPathNode bestStartNode;
            public Point? bestPointOnLine;
            public float minDist;
        }

        public WallPathNodeData getBestWallPath(List<WallPathNode> wallPaths)
        {
            WallPathNode bestStartNode = null;
            Point? bestPointOnLine = null;
            float minDist = float.MaxValue;
            foreach (var node in wallPaths)
            {
                // Optimization as distance functions are expensive
                if (node.isPointTooFar(pos, 30)) continue;

                Point closestPointOnLine = node.line.closestPointOnLine(pos);
                float distToLine = pos.distanceTo(closestPointOnLine);
                if (distToLine < minDist)
                {
                    minDist = distToLine;
                    bestStartNode = node;
                    bestPointOnLine = closestPointOnLine;
                }
            }

            return new WallPathNodeData
            {
                bestStartNode = bestStartNode,
                bestPointOnLine = bestPointOnLine,
                minDist = minDist
            };
        }

        public void setupModernWallCrawl(Point initialMoveDir)
        {
            WallPathNodeData bestWallPath1 = getBestWallPath(Global.level.levelData.wallPathNodes);
            WallPathNodeData bestWallPath2 = getBestWallPath(Global.level.levelData.wallPathNodesInverted);

            if (bestWallPath1.bestStartNode == null || bestWallPath2.bestStartNode == null)
            {
                useLegacyWallCrawl = true;
                setupLegacyWallCrawl();
                return;
            }

            WallPathNodeData bestWallPathToUse;

            // Decide which path to use (clockwise or counterclockwise) depending on initial movement angle
            float initialMoveAngle = initialMoveDir.angle;
            float crawlAngle1 = bestWallPath1.bestStartNode.angle;
            float crawlAngle2 = bestWallPath2.bestStartNode.angle;
            if (Helpers.getClosestAngleDiff(initialMoveAngle, crawlAngle1) < Helpers.getClosestAngleDiff(initialMoveAngle, crawlAngle2))
            {
                bestWallPathToUse = bestWallPath1;
                wallCrawlDir = 1;
            }
            else
            {
                bestWallPathToUse = bestWallPath2;
                wallCrawlDir = -1;
            }

            if (bestWallPathToUse.minDist > 30)
            {
                useLegacyWallCrawl = true;
                setupLegacyWallCrawl();
                return;
            }

            currentNode = bestWallPathToUse.bestStartNode;
            changePos(bestWallPathToUse.bestPointOnLine.Value);
        }

        public void updateModernWallCrawl()
        {
            var nextNode = currentNode.next;
            Point destPoint = nextNode.point;
            Point dirToDest = pos.directionToNorm(destPoint);
            move(dirToDest.times(wallCrawlSpeed));
            if (wallCrawlUpdateAngle)
            {
                angle = dirToDest.angle;
            }

            if (pos.distanceTo(destPoint) < 5)
            {
                currentNode = nextNode;
            }
        }

        #region legacy wall crawl code, still needed for maps without wall paths
        GameObject currentWall;
        List<Point> dests;
        int? destIndex;
        float initWallCooldown;

        public void setupLegacyWallCrawl()
        {
            var wallCollideDatas = Global.level.getTriggerList(this, 0, 0, null, typeof(Wall));
            if (wallCollideDatas.Count == 0)
            {
                initWall(null, null, false);
            }
            else
            {
                var allowedWallCollideDatas = getAllowedWallCollideDatas(wallCollideDatas);
                var bestWallCollideData = allowedWallCollideDatas.OrderBy(cd =>
                {
                    Line line1 = cd.Item1;
                    float line1YRatio = MathF.Abs(line1.y2 - line1.y1) / Helpers.clampMin(MathF.Abs(line1.x2 - line1.x1), 1);
                    return line1YRatio;
                }).FirstOrDefault();

                if (bestWallCollideData != null)
                {
                    initWall(bestWallCollideData.Item2, bestWallCollideData.Item1, false);
                }
                else
                {
                    initWall(null, null, false);
                }
            }
        }

        public void updateLegacyWallCrawl()
        {
            Helpers.decrementTime(ref initWallCooldown);

            if (destIndex == null) return;

            Point destPoint = dests[destIndex.Value];
            Point dirToDest = pos.directionToNorm(destPoint);
            move(dirToDest.times(wallCrawlSpeed));

            if (pos.distanceTo(destPoint) < 5)
            {
                destIndex += wallCrawlDir;
                if (destIndex >= dests.Count) destIndex = 0;
                else if (destIndex < 0) destIndex = dests.Count - 1;
            }

            Point origin = pos;
            var collideDatas = Global.level.raycastAll(origin, origin.add(dirToDest.times(12)), new List<Type>() { typeof(Wall) });

            Point origin2 = origin.add(dirToDest.leftNormal().times(12));
            collideDatas.AddRange(Global.level.raycastAll(origin2, origin2.add(dirToDest.times(12)), new List<Type>() { typeof(Wall) }));

            Point origin3 = origin.add(dirToDest.rightNormal().times(12));
            collideDatas.AddRange(Global.level.raycastAll(origin3, origin3.add(dirToDest.times(12)), new List<Type>() { typeof(Wall) }));

            if (collideDatas.Any(cd => currentWall.name != cd.gameObject.name))
            {
                Global.breakpoint = true;
                collideDatas = Global.level.checkCollisionsShape(collider.shape, null).FindAll(cd => cd.gameObject is Wall);
                var bestWallCollideData = getAllowedWallCollideDatas(collideDatas).FirstOrDefault(cd => cd.Item2.gameObject.name != currentWall.name);
                // DevConsole.log("Currentwall: " + currentWall.name);
                Global.breakpoint = false;
                if (bestWallCollideData != null)
                {
                    // DevConsole.log("1");
                    // DevConsole.log(bestWallCollideData.Item2.gameObject.name);
                    var collideData = bestWallCollideData.Item2;
                    var line = bestWallCollideData.Item1;
                    Point normal = collideData?.hitData?.normal ?? new Point(0, 0);
                    if (!normal.isZero())
                    {
                        // DevConsole.log("2");
                        initWall(collideData, line, true);
                    }
                }
            }
        }

        private List<Tuple<Line, CollideData>> getAllowedWallCollideDatas(List<CollideData> wallCollideDatas)
        {
            var results = new List<Tuple<Line, CollideData>>();
            if (Global.breakpoint)
            {
                // DevConsole.consoleLog.Clear();
                // DevConsole.log("CHECK");
            }
            foreach (var wallCollideData in wallCollideDatas)
            {
                foreach (var line in wallCollideData.hitData.distinctHitLines)
                {
                    // DevConsole.log(line.ToString() + " | " + wallCollideData.gameObject.name);
                    results.Add(new Tuple<Line, CollideData>(line, wallCollideData));
                }
            }
            // DevConsole.log("RESULTS:");
            // results = results.OrderByDescending(r => r.Item2.hitData.hitPoint.Value.distanceTo(pos)).ToList();
            foreach (var result in results)
            {
                // DevConsole.log(result.Item1.ToString() + " | " + result.Item2.gameObject.name);
            }
            return results;
        }

        public void initWall(CollideData wallCollideData, Line? line, bool useCooldown)
        {
            if (useCooldown)
            {
                if (initWallCooldown > 0) return;
                initWallCooldown = 0.1f;
            }

            destIndex = null;
            if (wallCollideData != null && line != null)
            {
                currentWall = wallCollideData.gameObject;
                dests = currentWall.collider.shape.points;
                Point firstPoint = (xDir == -1) ? line.Value.point1 : line.Value.point2;
                for (int i = 0; i < dests.Count; i++)
                {
                    if (dests[i].equals(firstPoint))
                    {
                        destIndex = i;
                        //DevConsole.log(destIndex.ToString());
                        break;
                    }
                }
            }

            if (destIndex == null)
            {
                vel = new Point(xDir * wallCrawlSpeed, 0);
            }
        }

        public bool isUpperPoint(Point point, List<Point> points)
        {
            int yLessThanCount = 0;
            foreach (var p in points)
            {
                if (point.y < p.y) yLessThanCount++;
            }
            return yLessThanCount >= points.Count / 2;
        }

        public bool isLeftPoint(Point point, List<Point> points)
        {
            int xLessThanCount = 0;
            foreach (var p in points)
            {
                if (point.x < p.x) xLessThanCount++;
            }
            return xLessThanCount >= points.Count / 2;
        }
        #endregion

        public void checkBigAcidUnderwater()
        {
            if (isUnderwater())
            {
                new BubbleAnim(pos, "bigbubble1", null, false) { vel = new Point(0, -75) };
                Global.level.delayedActions.Add(new DelayedAction(() => { new BubbleAnim(pos, "bigbubble2", null, false) { vel = new Point(0, -75) }; }, 0.1f));

                new BubbleAnim(pos.addxy(5, -5), "bigbubble1", null, false) { vel = new Point(0, -75) };
                Global.level.delayedActions.Add(new DelayedAction(() => { new BubbleAnim(pos.addxy(5, -5), "bigbubble2", null, false) { vel = new Point(0, -75) }; }, 0.1f));

                destroySelf();
            }
        }
    }
}