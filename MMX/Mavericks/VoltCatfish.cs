using System.Collections.Generic;
using System.Linq;

namespace MMXOnline
{
    public class VoltCatfish : Maverick
    {
        public static Weapon getWeapon() { return new Weapon(WeaponIds.VoltCatfish, 154); }
        public static Weapon getMeleeWeapon(Player player) { return new Weapon(WeaponIds.VoltCatfish, 154); }

        public Weapon meleeWeapon;
        public List<VoltCTriadThunderProj> mines = new List<VoltCTriadThunderProj>();
        public ShaderWrapper chargeShader;
        public bool bouncedOnce;

        public VoltCatfish(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            stateCooldowns.Add(typeof(MShoot), new MaverickStateCooldown(false, false, 1));
            stateCooldowns.Add(typeof(VoltCTriadThunderState), new MaverickStateCooldown(false, true, 0.75f));
    
            weapon = getWeapon();
            meleeWeapon = getMeleeWeapon(player);

            awardWeaponId = WeaponIds.TriadThunder;
            weakWeaponId = WeaponIds.TunnelFang;
            weakMaverickWeaponId = WeaponIds.TunnelRhino;

            ammo = 0;
            netActorCreateId = NetActorCreateId.VoltCatfish;
            bouncedOnce = true;

            chargeShader = Helpers.cloneGenericPaletteShader("paletteVoltCatfishCharge");
            
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
                        if (!mines.Any(m => m.electrified))
                        {
                            changeState(new VoltCTriadThunderState());
                            if (state is not VoltCTriadThunderState)
                            {
                                foreach (var mine in mines)
                                {
                                    mine.stopMoving();
                                }
                            }
                        }
                        else
                        {
                            changeState(new VoltCSuckState());
                        }
                    }
                    else if (input.isPressed(Control.Dash, player))
                    {
                        if (ammo >= 32)
                        {
                            changeState(new VoltCSpecialState());
                        }
                        else if (ammo >= 8)
                        {
                            changeState(new VoltCUpBeamState());
                        }
                    }
                }
                else if (state is MJump || state is MFall)
                {
                }
            }
        }

        public override string getMaverickPrefix()
        {
            return "voltc";
        }
    
        public override MaverickState getRandomAttackState()
        {
            return aiAttackStates().GetRandomItem();
        }

        public override MaverickState[] aiAttackStates()
        {
            var states = new List<MaverickState>
            {
                getShootState(true),
                getSpecialState(),
                new VoltCUpBeamState(),
            };

            return states.ToArray();
        }

        public MaverickState getSpecialState()
        {
            if (!mines.Any(m => m.electrified))
            {
                return new VoltCTriadThunderState();
            }
            else
            {
                return new VoltCSuckState();
            }
        }

        public MaverickState getDashState()
        {
            if (ammo >= 32)
            {
                return new VoltCSpecialState();
            }
            else if (ammo >= 8)
            {
                return new VoltCUpBeamState();
            }
            else
            {
                return null;
            }
        }

        public override List<ShaderWrapper> getShaders()
        {
            if (chargeShader == null || !sprite.name.EndsWith("_charge")) return new List<ShaderWrapper>();

            if (Global.isOnFrameCycle(4)) chargeShader.SetUniform("palette", 1);
            else chargeShader.SetUniform("palette", 2);

            return new List<ShaderWrapper>() { chargeShader };
        }

        public MaverickState getShootState(bool isAI)
        {
            var mshoot = new MShoot((Point pos, int xDir) =>
            {
                playSound("voltcCrash", sendRpc: true);
                new TriadThunderProjCharged(weapon, pos, xDir, 2, player, player.getNextActorNetId(), rpc: true);
            }, null);
            return mshoot;
        }

        public float getStompDamage()
        {
            float damage = 0;
            if (deltaPos.y > 150 * Global.spf) damage = 2;
            if (deltaPos.y > 225 * Global.spf) damage = 2;
            if (deltaPos.y > 300 * Global.spf) damage = 3;
            return damage;
        }

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            if (sprite.name.Contains("fall"))
            {
                float damagePercent = getStompDamage();
                if (damagePercent > 0)
                {
                    return new GenericMeleeProj(weapon, centerPoint, ProjIds.VoltCStomp, player, damage: getStompDamage(), flinch: Global.defFlinch, hitCooldown: 0.5f);
                }
            }
            return null;
        }

        public override void updateProjFromHitbox(Projectile proj)
        {
            if (sprite.name.EndsWith("fall"))
            {
                proj.damager.damage = getStompDamage();
            }
        }
    }

    public class VoltCTriadThunderProj : Projectile
    {
        public bool electrified;
        public VoltCatfish vc;
        public VoltCTriadThunderProj(Weapon weapon, Point pos, int xDir, int type, Point velDir, VoltCatfish vc, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 2, player, type == 0 ? "voltc_proj_triadt_deactivated" : "voltc_proj_ball", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.VoltCTriadThunder;
            destroyOnHit = false;
            shouldShieldBlock = false;
            vel = velDir.normalize().times(150);
            collider.wallOnly = true;
            maxTime = 1.75f;
            this.vc = vc;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public void electrify()
        {
            if (!electrified)
            {
                electrified = true;
                changeSprite("voltc_proj_triadt_electricity", true);
                time = 0;
            }
        }

        public override void update()
        {
            base.update();
            if (sprite.name == "voltc_proj_ball")
            {
                damager.flinch = Global.miniFlinch;
            }
            else if (sprite.name == "voltc_proj_triadt_electricity")
            {
                damager.flinch = Global.miniFlinch;
                damager.hitCooldown = 0.25f;
            }
            if (time > 0.75f)
            {
                stopMoving();
            }
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (!ownedByLocalPlayer) return;

            if (vel.isZero() && other.gameObject is VoltCTriadThunderProj ttp && ttp.ownedByLocalPlayer && ttp.owner == owner && ttp.sprite.name == "voltc_proj_ball")
            {
                electrify();
                ttp.destroySelf();
            }
            else if (other.gameObject is VoltCatfish vc && vc.state is VoltCSuckState && vc.ownedByLocalPlayer && vc == this.vc)
            {
                vc.addAmmo(electrified ? 8 : 4);
                destroySelf();
            }
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            if (!ownedByLocalPlayer) return;

            if (other.isGroundHit() || other.isCeilingHit())
            {
                vel.y = 0;
            }
            else
            {
                vel = Point.zero;
            }
        }

        public override void onDestroy()
        {
            base.onDestroy();
            if (!ownedByLocalPlayer) return;

            vc?.mines.Remove(this);
        }
    }

    public class VoltCTriadThunderState : MaverickState
    {
        public VoltCTriadThunderState() : base("spit", "")
        {
            exitOnAnimEnd = true;
        }

        public override void update()
        {
            base.update();
            Point? shootPos = maverick.getFirstPOI();
            var vc = maverick as VoltCatfish;
            if (!once && shootPos != null)
            {
                maverick.playSound("voltcTriadThunder", sendRpc: true);
                once = true;
                int type = (vc.mines.Count == 0 ? 0 : 1);
                var proj1 = new VoltCTriadThunderProj(maverick.weapon, shootPos.Value, maverick.xDir, type, new Point(maverick.xDir, 0.5f), vc, player, player.getNextActorNetId(), rpc: true);
                var proj2 = new VoltCTriadThunderProj(maverick.weapon, shootPos.Value, maverick.xDir, type, new Point(maverick.xDir, 0), vc, player, player.getNextActorNetId(), rpc: true);
                var proj3 = new VoltCTriadThunderProj(maverick.weapon, shootPos.Value, maverick.xDir, type, new Point(maverick.xDir, -0.5f), vc, player, player.getNextActorNetId(), rpc: true);
                if (type == 0)
                {
                    vc.mines.Add(proj1);
                    vc.mines.Add(proj2);
                    vc.mines.Add(proj3);
                }
            }
        }
    }

    public class VoltCSuckProj : Projectile
    {
        Player player;
        public VoltCatfish vc;
        public VoltCSuckProj(Weapon weapon, Point pos, int xDir, VoltCatfish vc, Player player, ushort netProjId, bool sendRpc = false) :
            base(weapon, pos, xDir, 0, 0, player, "voltc_proj_suck", 0, 0f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.VoltCSuck;
            setIndestructableProperties();
            this.player = player;
            this.vc = vc;

            if (sendRpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;
            if (vc.state is not VoltCSuckState)
            {
                destroySelf();
            }
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (!ownedByLocalPlayer) return;
            if (other.gameObject is VoltCTriadThunderProj ttp && ttp.ownedByLocalPlayer && ttp.owner == owner)
            {
                ttp.moveToPos(vc.getFirstPOIOrDefault(), 150);
            }
        }

        public override void onHitDamagable(IDamagable damagable)
        {
            base.onHitDamagable(damagable);
            if (vc == null) return;

            if (damagable is Character chr)
            {
                if (!chr.ownedByLocalPlayer) return;
                if (chr.isImmuneToKnockback()) return;
                chr.moveToPos(vc.getFirstPOIOrDefault(), 150);
            }
        }
    }

    public class VoltCSuckState : MaverickState
    {
        float partTime;
        VoltCSuckProj suckProj;
        public VoltCSuckState() : base("suck", "")
        {
        }

        public override void update()
        {
            base.update();
            partTime += Global.spf;
            if (partTime > 0.1f)
            {
                partTime = 0;
                float randX = Helpers.randomRange(25, 50);
                float randY = Helpers.randomRange(-25, 25);
                var spawnPos = maverick.pos.addxy(randX * maverick.xDir, -14 + randY);
                var spawnVel = spawnPos.directionToNorm(maverick.getFirstPOIOrDefault()).times(150);
                new Anim(spawnPos, "voltc_particle_suck", 1, player.getNextActorNetId(), false, sendRpc: true)
                {
                    vel = spawnVel,
                    ttl = 0.15f,
                };
            }

            if (isHoldStateOver(0.5f, 4, 2, Control.Special1))
            {
                maverick.changeToIdleOrFall();
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            suckProj = new VoltCSuckProj(maverick.weapon, maverick.pos.addxy(maverick.xDir * 75, 0), maverick.xDir, maverick as VoltCatfish, player, player.getNextActorNetId(), sendRpc: true);
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            suckProj?.destroySelf();
        }
    }


    public class VoltCUpBeamProj : Projectile
    {
        public VoltCUpBeamProj(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 4, player, "voltc_proj_thunder_small", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.VoltCUpBeam;
            destroyOnHit = false;
            shouldShieldBlock = false;

            if (type == 0)
            {
                vel = new Point(0, -150);
                maxTime = 0.675f;
            }
            else
            {
                vel = new Point(0, 450);
                maxTime = 0.25f;
                projId = (int)ProjIds.VoltCUpBeam2;
            }

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;
            if (loopCount >= 2 && sprite.name == "voltc_proj_thunder_small")
            {
                changeSprite("voltc_proj_thunder_medium", true);
            }
            else if (loopCount >= 2 && sprite.name == "voltc_proj_thunder_medium")
            {
                changeSprite("voltc_proj_thunder_big", true);
            }
        }
    }

    public class VoltCUpBeamState : MaverickState
    {
        public VoltCUpBeamState() : base("thunder_vertical", "")
        {
            exitOnAnimEnd = true;
        }

        public override void update()
        {
            base.update();
            Point? shootPos = maverick.getFirstPOI(0);
            Point? shootPos2 = maverick.getFirstPOI(1);
            if (!once && shootPos != null)
            {
                once = true;
                maverick.playSound("voltcWeakBolt", sendRpc: true);
                if (isAI || maverick.ammo >= 8)
                {
                    if (!isAI) maverick.deductAmmo(8);
                    new VoltCUpBeamProj(maverick.weapon, shootPos.Value, maverick.xDir, 0, player, player.getNextActorNetId(), rpc: true);
                }
                if (isAI || maverick.ammo >= 8)
                {
                    if (!isAI) maverick.deductAmmo(8);
                    new VoltCUpBeamProj(maverick.weapon, shootPos2.Value, maverick.xDir, 0, player, player.getNextActorNetId(), rpc: true);
                }
            }
        }
    }

    public class VoltCChargeProj : Projectile
    {
        public VoltCChargeProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 2, player, "voltc_proj_charge", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.VoltCCharge;
            setIndestructableProperties();

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }

    public class VoltCBarrierProj : Projectile
    {
        public VoltCBarrierProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 2, player, "voltc_proj_wall", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.VoltCBarrier;
            setIndestructableProperties();
            isShield = true;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }

    public class VoltCSparkleProj : Projectile
    {
        public VoltCSparkleProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 2, player, "voltc_proj_sparkle", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.VoltCSparkle;
            vel = new Point(Helpers.randomRange(-200, 200), Helpers.randomRange(-400, -200));
            useGravity = true;
            maxTime = 0.75f;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }

    public class VoltCSpecialState : MaverickState
    {
        int state = 0;
        VoltCUpBeamProj upBeamProj;
        VoltCChargeProj chargeProj1;
        VoltCChargeProj chargeProj2;
        VoltCBarrierProj barrierProj1;
        VoltCBarrierProj barrierProj2;
        const float drainAmmoRate = 6;
        public VoltCSpecialState() : base("charge_start", "")
        {
            superArmor = true;
        }

        public override void update()
        {
            base.update();
            if (state == 0)
            {
                maverick.ammo -= 8;
                var beamPos = maverick.pos.addxy(0, -150);
                upBeamProj = new VoltCUpBeamProj(maverick.weapon, beamPos, maverick.xDir, 1, player, player.getNextActorNetId(), rpc: true);
                maverick.playSound("voltcStrongBolt", sendRpc: true);
                state = 1;
            }
            else if (state == 1)
            {
                maverick.drainAmmo(drainAmmoRate);
                if (upBeamProj.destroyed)
                {
                    upBeamProj = null;
                    chargeProj2 = new VoltCChargeProj(maverick.weapon, maverick.getFirstPOI(1).Value, maverick.xDir, player, player.getNextActorNetId(), rpc: true);
                    chargeProj1 = new VoltCChargeProj(maverick.weapon, maverick.getFirstPOI(0).Value, maverick.xDir, player, player.getNextActorNetId(), rpc: true);
                    stateTime = 0;
                    maverick.playSound("voltcStatic", sendRpc: true);
                    maverick.changeSpriteFromName("charge", true);
                    state = 2;
                }
            }
            else if (state == 2)
            {
                maverick.drainAmmo(drainAmmoRate);
                updateChargeProjs();
                if (stateTime > 0.5f)
                {
                    stateTime = 0;
                    state = 3;
                    barrierProj1 = new VoltCBarrierProj(maverick.weapon, maverick.getFirstPOI(2).Value, maverick.xDir, player, player.getNextActorNetId(), rpc: true);
                    barrierProj2 = new VoltCBarrierProj(maverick.weapon, maverick.getFirstPOI(3).Value, maverick.xDir, player, player.getNextActorNetId(), rpc: true);
                }
            }
            else if (state == 3)
            {
                maverick.drainAmmo(drainAmmoRate);
                updateChargeProjs();
                updateBarrierProjs();
                spawnParticles();
                if (maverick.ammo <= 0 || isHoldStateOver(1, float.MaxValue, 2, Control.Dash))
                {
                    maverick.changeToIdleOrFall();
                }
            }
        }

        float partTime;
        float partSoundTime;
        public void spawnParticles()
        {
            partTime += Global.spf;
            if (partTime > 0.25f)
            {
                partTime = 0;
                new VoltCSparkleProj(maverick.weapon, maverick.getFirstPOI(0).Value, maverick.xDir, player, player.getNextActorNetId(), rpc: true);
            }
            Helpers.decrementTime(ref partSoundTime);
            if (partSoundTime <= 0)
            {
                partSoundTime = 0.5f;
                maverick.playSound("voltcCrash", sendRpc: true);
            }
        }

        public void updateChargeProjs()
        {
            chargeProj1.changePos(maverick.getFirstPOI(0).Value);
            chargeProj2.changePos(maverick.getFirstPOI(1).Value);
        }

        public void updateBarrierProjs()
        {
            barrierProj1.changePos(maverick.getFirstPOI(2).Value);
            barrierProj2.changePos(maverick.getFirstPOI(3).Value);
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            chargeProj1?.destroySelf();
            chargeProj2?.destroySelf();
            barrierProj1?.destroySelf();
            barrierProj2?.destroySelf();
        }
    }

    public class VoltCBounce : MaverickState
    {
        public VoltCBounce() : base("jump", "")
        {
        }

        public override void update()
        {
            base.update();

            if (maverick.vel.y * maverick.getYMod() > 0)
            {
                maverick.changeState(new MFall());
                return;
            }
            airCode();
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.vel.y = -maverick.getJumpPower() * 0.4f;
        }
    }
}
