using SFML.Graphics;
using System.Collections.Generic;

namespace MMXOnline
{
    public class FlameStag : Maverick
    {
        public static Weapon getWeapon() { return new Weapon(WeaponIds.FStagGeneric, 144); }
        public static Weapon getUppercutWeapon(Player player) { return new Weapon(WeaponIds.FStagGeneric, 144, new Damager(player, 4, Global.defFlinch, 0.25f)); }
        public Weapon uppercutWeapon;

        public Sprite antler;
        public Sprite antlerDown;
        public Sprite antlerSide;

        public FlameStag(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            weapon = getWeapon();
            uppercutWeapon = getUppercutWeapon(player);

            canClimbWall = true;
            width = 20;

            antler = Global.sprites["fstag_antler"].clone();
            antlerDown = Global.sprites["fstag_antler_down"].clone();
            antlerSide = Global.sprites["fstag_antler_side"].clone();

            //stateCooldowns.Add(typeof(FStagShoot), new MaverickStateCooldown(false, false, 0.25f));
            stateCooldowns.Add(typeof(FStagDashChargeState), new MaverickStateCooldown(true, false, 0.75f));
            stateCooldowns.Add(typeof(FStagDashState), new MaverickStateCooldown(true, false, 0.75f));

            awardWeaponId = WeaponIds.SpeedBurner;
            weakWeaponId = WeaponIds.BubbleSplash;
            weakMaverickWeaponId = WeaponIds.BubbleCrab;

            netActorCreateId = NetActorCreateId.FlameStag;
            netOwner = player;
            if (sendRpc)
            {
                createActorRpc(player.id);
            }
        }

        public override void update()
        {
            base.update();

            if (isUnderwater())
            {
                antler.visible = false;
                antlerDown.visible = false;
                antlerSide.visible = false;
            }
            else
            {
                antler.visible = true;
                antlerDown.visible = true;
                antlerSide.visible = true;
            }

            antler.update();
            antlerDown.update();
            antlerSide.update();

            if (!ownedByLocalPlayer) return;

            if (aiBehavior == MaverickAIBehavior.Control)
            {
                if (state is MIdle || state is MRun)
                {
                    if (input.isPressed(Control.Shoot, player))
                    {
                        changeState(new FStagShoot(false));
                    }
                    else if (input.isPressed(Control.Special1, player))
                    {
                        changeState(new FStagGrabState(false));
                    }
                    else if (input.isPressed(Control.Dash, player))
                    {
                        changeState(new FStagDashChargeState());
                    }
                }
                else if (state is MJump || state is MFall)
                {
                    var inputDir = input.getInputDir(player);
                    if (inputDir.x != 0)
                    {
                        if (!sprite.name.EndsWith("wall_dash")) changeSpriteFromName("wall_dash", true);
                    }
                    else
                    {
                        if (sprite.name.EndsWith("wall_dash")) changeSpriteFromName("fall", true);
                    }
                }
                else if (state is MWallKick mwk)
                {
                    var inputDir = input.getInputDir(player);
                    if (inputDir.x != 0 && inputDir.x == mwk.kickDir)
                    {
                        if (!sprite.name.EndsWith("wall_dash")) changeSpriteFromName("wall_dash", true);
                    }
                    else
                    {
                        if (sprite.name.EndsWith("wall_dash")) changeSpriteFromName("fall", true);
                    }
                }
            }
        }

        public override string getMaverickPrefix()
        {
            return "fstag";
        }

        public override MaverickState[] aiAttackStates()
        {
            var attacks = new MaverickState[]
            {
                new FStagShoot(false),
                new FStagGrabState(false),
                new FStagDashChargeState(),
            };
            return attacks;
        }

        public override MaverickState getRandomAttackState()
        {
            var attacks = new MaverickState[]
            {
                new FStagShoot(false),
                new FStagGrabState(false),
            };
            return attacks.GetRandomItem();
        }

        public Point? getAntlerPOI(out string tag)
        {
            tag = "";
            if (sprite?.getCurrentFrame()?.POIs?.Count > 0)
            {
                for (int i = 0; i < sprite.getCurrentFrame().POITags.Count; i++)
                {
                    tag = sprite.getCurrentFrame().POITags[i];
                    if (tag == "antler" || tag == "antler_side" || tag == "antler_down")
                    {
                        return getFirstPOIOffsetOnly(i);
                    }
                }
            }
            return null;
        }

        public override float getRunSpeed()
        {
            return 200;
        }

        public override float getDashSpeed()
        {
            return 1.5f;
        }

        public Point? getAttackPOI()
        {
            if (sprite?.getCurrentFrame()?.POIs?.Count > 0)
            {
                int poiIndex = sprite.getCurrentFrame().POITags.FindIndex(tag => tag != "antler" && tag != "antler_side" && tag != "antler_down");
                if (poiIndex >= 0) return getFirstPOIOrDefault(poiIndex);
            }
            return null;
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            var antlerPOI = getAntlerPOI(out string tag);
            if (antlerPOI != null)
            {
                Sprite sprite = antler;
                if (tag == "antler_side") sprite = antlerSide;
                if (tag == "antler_down") sprite = antlerDown;
                sprite.draw(sprite.frameIndex, pos.x + (xDir * antlerPOI.Value.x), pos.y + antlerPOI.Value.y, xDir, 1, null, 1, 1, 1, zIndex + 100, useFrameOffsets: true);
            }
        }

        public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint)
        {
            if (sprite.name.Contains("dash_grab"))
            {
                return new GenericMeleeProj(weapon, centerPoint, ProjIds.FStagUppercut, player, damage: 0, flinch: 0, hitCooldown: 0, owningActor: this);
            }
            return null;
        }
    }

    public class FStagFireballProj : Projectile
    {
        Wall hitWall;
        public bool launched;
        public FStagFireballProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 2, player, "fstag_fireball_proj", 0, 0.01f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.FStagFireball;
            maxTime = 0.75f;
            
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;
            if (isUnderwater())
            {
                destroySelf();
            }
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            if (!ownedByLocalPlayer) return;

            if (!launched) return;

            if (hitWall != null && hitWall != other.gameObject)
            {
                destroySelf();
                return;
            }
            else if (hitWall == null)
            {
                if (other.isGroundHit())
                {
                    hitWall = other.gameObject as Wall;
                    changePos(other.getHitPointSafe());
                    changeSprite("fstag_fireball_ground", true);
                    vel = other.getNormalSafe().leftOrRightNormal(xDir).normalize().times(350);
                }
                else if (other.isSideWallHit())
                {
                    hitWall = other.gameObject as Wall;
                    changePos(other.getHitPointSafe());
                    changeSprite("fstag_fireball_wall", true);
                    vel = other.getNormalSafe().leftOrRightNormal(xDir).normalize().times(350);
                }
            }
        }
    }

    public class FStagShoot : MaverickState
    {
        bool shotOnce;
        FStagFireballProj fireball;
        bool isSecond;
        public FStagShoot(bool isSecond) : base(isSecond ? "punch2" : "punch", "")
        {
            this.isSecond = isSecond;
        }

        public override void update()
        {
            base.update();

            Point? shootPos = (maverick as FlameStag).getAttackPOI();
            if (!shotOnce && shootPos != null)
            {
                shotOnce = true;
                fireball = new FStagFireballProj(maverick.weapon, shootPos.Value, maverick.xDir, player, player.getNextActorNetId(), rpc: true);
            }

            if (fireball != null)
            {
                if (shootPos != null)
                {
                    fireball.changePos(shootPos.Value);
                }

                if (!isSecond)
                {
                    if (maverick.frameIndex >= 8)
                    {
                        if (!fireball.launched)
                        {
                            fireball.launched = true;
                            fireball.vel = new Point(fireball.xDir * 350, 50);
                            maverick.playSound("fstagShoot", sendRpc: true);
                        }
                    }
                }
                else
                {
                    if (maverick.frameIndex >= 6)
                    {
                        if (!fireball.launched)
                        {
                            fireball.launched = true;
                            fireball.vel = new Point(fireball.xDir * 350, -50);
                            maverick.playSound("fstagShoot", sendRpc: true);
                        }
                    }
                }
            }

            if (!isSecond && maverick.frameIndex >= 8)
            {
                if (isAI || input.isPressed(Control.Shoot, player))
                {
                    maverick.changeState(new FStagShoot(true));
                    return;
                }
            }

            if (maverick.isAnimOver())
            {
                maverick.changeToIdleOrFall();
                return;
            }
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            if (fireball != null && fireball.vel.isZero())
            {
                fireball.destroySelf();
            }
        }
    }

    public class FStagTrailProj : Projectile
    {
        float sparkTime;
        public FStagTrailProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 1, player, "fstag_fire_trail", 0, 1, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.FStagDashTrail;
            setIndestructableProperties();
            if (isUnderwater())
            {
                destroySelf();
                return;
            }

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();

            Helpers.decrementTime(ref sparkTime);
            if (sparkTime <= 0)
            {
                sparkTime = 0.1f;
                new Anim(getFirstPOIOrDefault(), "fstag_fire_trail_extra", xDir, null, true);
            }

            if (!ownedByLocalPlayer) return;

            if (isAnimOver())
            {
                destroySelf();
            }
        }
    }

    public class FStagDashChargeProj : Projectile
    {
        public FStagDashChargeProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 1, player, "fstag_fire_body", 0, 0.25f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.FStagDashCharge;
            setIndestructableProperties();
            if (isUnderwater())
            {
                visible = false;
            }

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }

    public class FStagDashChargeState : MaverickState
    {
        FStagDashChargeProj proj;
        public FStagDashChargeState() : base("angry", "")
        {
        }

        public override void update()
        {
            base.update();
            if (player == null) return;
            
            proj.incPos(maverick.deltaPos);
            maverick.turnToInput(input, player);

            if (isAI)
            {
                if (stateTime > 0.4f)
                {
                    maverick.changeState(new FStagDashState(stateTime));
                }
            }
            else if ((!input.isHeld(Control.Dash, player) && stateTime > 0.2f) || stateTime > 0.6f)
            {
                maverick.changeState(new FStagDashState(stateTime));
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.stopMoving();
            proj = new FStagDashChargeProj(maverick.weapon, maverick.getFirstPOIOrDefault("fire_body"), maverick.xDir, player, player.getNextActorNetId(), rpc: true);
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            proj.destroySelf();
        }
    }

    public class FStagDashProj : Projectile
    {
        public FStagDashProj(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 3, player, type == 0 ? "fstag_fire_dash" : "fstag_fire_updash", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.FStagDash;
            setIndestructableProperties();
            if (isUnderwater())
            {
                visible = false;
            }

            if (type == 2)
            {
                yDir = -1;
            }

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }

    public class FStagDashState : MaverickState
    {
        float trailTime;
        FStagDashProj proj;
        float chargeTime;
        public FStagDashState(float chargeTime) : base("dash", "")
        {
            this.chargeTime = chargeTime;
            enterSound = "fstagDash";
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (input.isPressed(Control.Special1, player))
            {
                maverick.changeState(new FStagGrabState(true));
                return;
            }

            proj.incPos(maverick.deltaPos);

            maverick.move(new Point(maverick.xDir * 400, 0));

            Helpers.decrementTime(ref trailTime);
            if (trailTime <= 0)
            {
                trailTime = 0.04f;
                new FStagTrailProj(maverick.weapon, maverick.getFirstPOIOrDefault("fire_trail"), maverick.xDir, player, player.getNextActorNetId(), rpc: true);
            }

            if (input.isPressed(Control.Dash, player) || stateTime > chargeTime)
            {
                maverick.changeToIdleOrFall();
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.stopMoving();
            proj = new FStagDashProj(maverick.weapon, maverick.getFirstPOIOrDefault("fire_dash"), maverick.xDir, 0, player, player.getNextActorNetId(), rpc: true);
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            proj.destroySelf();
        }
    }

    public class FStagGrabState : MaverickState
    {
        float xVel = 400;
        public Character victim;
        float endLagTime;
        public FStagGrabState(bool fromDash) : base("dash_grab", "")
        {
            if (!fromDash) xVel = 0;
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            xVel = Helpers.lerp(xVel, 0, Global.spf * 2);
            maverick.move(new Point(maverick.xDir * xVel, 0));

            maverick.turnToInput(input, player);

            if (victim == null && maverick.frameIndex >= 6)
            {
                maverick.changeToIdleOrFall();
                return;
            }

            if (maverick.isAnimOver())
            {
                if (victim != null)
                {
                    endLagTime += Global.spf;
                    if (endLagTime > 0.25f)
                    {
                        maverick.changeState(new FStagUppercutState(victim));
                    }
                }
                else
                {
                    maverick.changeToIdleOrFall();
                }
            }
        }

        public override bool trySetGrabVictim(Character grabbed)
        {
            if (victim == null)
            {
                victim = grabbed;
                return true;
            }
            return false;
        }
    }

    public class FStagUppercutState : MaverickState
    {
        FStagDashProj proj;
        float yDist;
        int state;
        public Character victim;
        float topDelay;
        int upHitCount;
        int downHitCount;
        public FStagUppercutState(Character victim) : base("updash", "")
        {
            this.victim = victim;
            enterSound = "fstagUppercut";
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            proj.incPos(maverick.deltaPos);

            float speed = 450;
            float yFactor = 1;
            if (state == 2)
            {
                yFactor = -1;
            }

            Point moveAmount = new Point(maverick.xDir * 50, -speed * yFactor);
            if (state != 1)
            {
                maverick.move(moveAmount);
                yDist += Global.spf * speed;
            }

            if (state == 0)
            {
                var hit = checkCollisionNormal(moveAmount.x * Global.spf, moveAmount.y * Global.spf);
                if (hit != null)
                {
                    if (hit.isCeilingHit())
                    {
                        crashAndDamage(true);
                        reverse();
                    }
                    else
                    {
                        upHitCount++;
                        if (upHitCount > 5)
                        {
                            crashAndDamage(true);
                            reverse();
                        }
                        else
                        {
                            maverick.xDir *= -1;
                        }
                    }
                }
                else if (yDist > 224)
                {
                    reverse();
                }
            }
            else if (state == 1)
            {
                topDelay += Global.spf;
                if (topDelay > 0.1f)
                {
                    state = 2;
                }
            }
            else
            {
                var hit = checkCollisionNormal(moveAmount.x * Global.spf, moveAmount.y * Global.spf);
                if (hit != null)
                {
                    if (hit.isGroundHit())
                    {
                        crashAndDamage(false);
                        maverick.changeToIdleOrFall();
                    }
                    else
                    {
                        downHitCount++;
                        if (downHitCount > 5)
                        {
                            crashAndDamage(false);
                            maverick.changeToIdleOrFall();
                        }
                        else
                        {
                            maverick.xDir *= -1;
                        }
                    }
                }
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

        public void crashAndDamage(bool isCeiling)
        {
            if (getVictim() != null)
            {
                (maverick as FlameStag).uppercutWeapon.applyDamage(victim, false, maverick, (int)ProjIds.FStagUppercut, 
                    overrideDamage: isCeiling ? 3 : 5, overrideFlinch: isCeiling ? 0 : Global.defFlinch, sendRpc: true);
            }
            maverick.playSound("crash", sendRpc: true);
            maverick.shakeCamera(sendRpc: true);
        }

        public void reverse()
        {
            if (state == 0)
            {
                state = 1;
                proj.changeSprite("fstag_fire_downdash", true);
                maverick.changeSpriteFromName("downdash", true);
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.unstickFromGround();
            proj = new FStagDashProj(maverick.weapon, maverick.pos, maverick.xDir, 1, player, player.getNextActorNetId(), rpc: true);
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            maverick.useGravity = true;
            proj?.destroySelf();
            if (getVictim() != null)
            {
                victim.releaseGrab(maverick);
            }
        }
    }

    public class FStagGrabbed : GenericGrabbedState
    {
        public Character grabbedChar;
        public float timeNotGrabbed;
        string lastGrabberSpriteName;
        public const float maxGrabTime = 4;
        public FStagGrabbed(FlameStag grabber) : base(grabber, maxGrabTime, "_dash_grab")
        {
            customUpdate = true;
        }

        public override void update()
        {
            base.update();

            string grabberSpriteName = grabber.sprite?.name ?? "";
            if (grabberSpriteName.EndsWith("_dash_grab") == true)
            {
                trySnapToGrabPoint(true);
            }
            else if (grabberSpriteName.EndsWith("_updash") == true || grabberSpriteName.EndsWith("_downdash") == true)
            {
                grabTime -= player.mashValue();
                if (grabTime <= 0)
                {
                    character.changeToIdleOrFall();
                }

                if (lastGrabberSpriteName != grabberSpriteName)
                {
                    trySnapToGrabPoint(true);
                }
                else
                {
                    character.incPos(grabber.deltaPos);
                }
            }
            else
            {
                timeNotGrabbed += Global.spf;
                if (timeNotGrabbed > 1f)
                {
                    character.changeToIdleOrFall();
                    return;
                }
            }
            lastGrabberSpriteName = grabberSpriteName;
        }
    }

    public class FStagWallDashState : MaverickState
    {
        public FStagWallDashState() : base("wall_dash")
        {
        }

        public override void update()
        {
            base.update();

            if (maverick.grounded)
            {
                landingCode();
                return;
            }

            wallClimbCode();

            if (Global.level.checkCollisionActor(maverick, 0, -1) != null && maverick.vel.y < 0)
            {
                maverick.vel.y = 0;
            }

            maverick.move(new Point(maverick.xDir * 350, 0));
        }
    }
}
