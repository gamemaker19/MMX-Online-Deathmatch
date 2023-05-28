using SFML.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline
{
    public class WireSponge : Maverick
    {
        public static Weapon getWeapon() { return new Weapon(WeaponIds.WSpongeGeneric, 141); }
        public static Weapon getChainWeapon(Player player) { return new Weapon(WeaponIds.WSpongeStrikeChain, 141, new Damager(player, 4, Global.defFlinch, 0.5f)); }
        public Weapon chainWeapon;

        public ShaderWrapper chargeShader;
        public float chargeTime;

        public WireSponge(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
            base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
        {
            stateCooldowns.Add(typeof(WSpongeSeedThrowState), new MaverickStateCooldown(false, true, 0.75f));
            stateCooldowns.Add(typeof(WSpongeHangSeedThrowState), new MaverickStateCooldown(false, true, 0.75f));
            stateCooldowns.Add(typeof(WSpongeLightningState), new MaverickStateCooldown(false, true, 0.75f));
            stateCooldowns.Add(typeof(WSpongeChainSpinState), new MaverickStateCooldown(false, true, 0.75f));

            weapon = getWeapon();
            chainWeapon = getChainWeapon(player);

            awardWeaponId = WeaponIds.StrikeChain;
            weakWeaponId = WeaponIds.SonicSlicer;
            weakMaverickWeaponId = WeaponIds.OverdriveOstrich;

            chargeShader = Helpers.cloneShaderSafe("wspongeCharge");

            netActorCreateId = NetActorCreateId.WireSponge;
            netOwner = player;
            if (sendRpc)
            {
                createActorRpc(player.id);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            if (aiBehavior == MaverickAIBehavior.Control)
            {
                if (state is MIdle || state is MRun)
                {
                    if (input.isPressed(Control.Shoot, player))
                    {
                        if (input.isHeld(Control.Up, player))
                        {
                            changeState(new WSpongeUpChainStartState());
                        }
                        else
                        {
                            changeState(new WSpongeChainSpinState());
                        }
                    }
                    else if (input.isPressed(Control.Special1, player))
                    {
                        changeState(new WSpongeSeedThrowState(Control.Special1));
                    }
                    else if (input.isPressed(Control.Dash, player))
                    {
                        changeState(new WSpongeChargeState());
                    }
                }
                //else if ((state is MJump mJump && !mJump.fromCling) || state is MFall)
                else if (state is MFall)
                {
                    var inputDir = input.getInputDir(player);
                    if (MathF.Sign(inputDir.x) == xDir)
                    {
                        var hitWall = getHitWall(xDir, 0);
                        if (hitWall != null && hitWall.isSideWallHit())
                        {
                            changeState(new WSpongeClingState(), true);
                        }
                    }
                }
            }
        }

        public override List<ShaderWrapper> getShaders()
        {
            if (sprite.name.EndsWith("angry_start") && chargeShader != null)
            {
                chargeShader.SetUniform("chargeTexture", Global.textures["wspongeChargeRed"]);
                float chargeFactor = Helpers.clamp01(1 - (chargeTime / WSpongeChargeState.maxChargeTime));
                chargeShader.SetUniform("chargeFactor", chargeFactor);
                return new List<ShaderWrapper>() { chargeShader };
            }
            return base.getShaders();
        }

        public new CollideData getHitWall(float x, float y)
        {
            var rect = collider.shape.getRect();
            rect.y1 += 20f;
            rect.y2 -= 20f;
            if (x > 0) rect.x2 += x * 2;
            else if (x < 0) rect.x1 += x * 2;
            var hits = Global.level.checkCollisionsShape(rect.getShape(), null);

            var bestWall = hits.FirstOrDefault(h => h.gameObject is Wall wall && !wall.collider.isClimbable);
            if (bestWall != null) return bestWall;
            return hits.FirstOrDefault(h => h.gameObject is Wall);
        }

        public override string getMaverickPrefix()
        {
            return "wsponge";
        }

        public override MaverickState[] aiAttackStates()
        {
            var attacks = new MaverickState[]
            {
                new WSpongeChainSpinState(),
                new WSpongeSeedThrowState(Control.Special1),
                new WSpongeChargeState(),
            };
            return attacks;
        }

        public override MaverickState getRandomAttackState()
        {
            return aiAttackStates().GetRandomItem();
        }
    }

    public class WSpongeChainSpinProj : Projectile
    {
        public WSpongeChainSpinProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 250, 1, player, "wsponge_vine_spin_shield", Global.defFlinch, 0.2f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.WSpongeChainSpin;
            setIndestructableProperties();
            isDeflectShield = true;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }
    }

    public class WSpongeChainSpinState : MaverickState
    {
        WSpongeChainSpinProj proj;
        public WSpongeChainSpinState() : base("vine_spin", "")
        {
        }

        public override void update()
        {
            base.update();

            maverick.turnToInput(input, player);

            proj.changePos(maverick.getFirstPOIOrDefault());

            if (isAI)
            {
                if (stateTime > 1)
                {
                    maverick.changeState(new WSpongeSideChainState(stateTime));
                }
            }
            else if (!input.isHeld(Control.Shoot, player))
            {
                maverick.changeState(new WSpongeSideChainState(stateTime));
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.stopMoving();
            proj = new WSpongeChainSpinProj(maverick.weapon, maverick.getFirstPOIOrDefault(), maverick.xDir, player, player.getNextActorNetId(), rpc: true);
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            proj.destroySelf();
        }
    }

    public class WSpongeSideChainProj : Projectile
    {
        public int state = 0;
        public Player player;
        public float distMoved;
        public float distRetracted;
        public bool reversed;
        public Point toWallVel;
        public Actor hookedActor;
        public int maxDist = 100;
        public int origXDir;
        public int type;
        public bool isCharged { get { return type == 1; } }
        public float hookWaitTime;
        public Point chainVel;
        public Actor wireSponge;
        public Point netOrigin;
        public WSpongeSideChainProj(Weapon weapon, Point pos, int xDir, Actor wireSponge, float spinTime, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 3, player, "wsponge_vine_spike", Global.defFlinch, 0.25f, netProjId, player.ownedByLocalPlayer)
        {
            setIndestructableProperties();
            this.player = player;
            this.wireSponge = wireSponge;

            origXDir = xDir;
            projId = (int)ProjIds.WSpongeChain;

            maxDist = 50 + (int)(Helpers.clampMax(spinTime, 1.5f) * 100);
            vel = new Point(xDir * (300 + maxDist), 0);
            speed = MathF.Abs(vel.x);

            chainVel = vel;
            netOrigin = pos;
        
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void postUpdate()
        {
            base.postUpdate();
            if (!ownedByLocalPlayer) return;

            if (wireSponge != null)
            {
                var shootPos = wireSponge.getFirstPOIOrDefault();
                changePos(new Point(shootPos.x + wireSponge.xDir * (distMoved - distRetracted), shootPos.y));
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer)
            {
                if (!reversed) distMoved += MathF.Abs(speed * Global.spf);
                else distRetracted += MathF.Abs(speed * Global.spf);
                return;
            }

            // Hooked character? Wait for them to become hooked before pulling back. Wait a max of 200 ms
            var hookedChar = hookedActor as Character;
            if (hookedChar != null && !hookedChar.ownedByLocalPlayer && !hookedChar.isStrikeChainHookedBS.getValue())
            {
                hookWaitTime += Global.spf;
                if (hookWaitTime < 0.2f) return;
            }

            // Firing
            if (state == 0)
            {
                distMoved += MathF.Abs(speed * Global.spf);
                if (distMoved >= maxDist) // || (type == 0 && !player.input.isHeld(Control.Shoot)))
                {
                    reversed = true;
                    chainVel.x *= -1;
                    chainVel.y *= -1;
                    state = 1;
                    time = 0;
                }
            }
            // Retracting (not hooked to wall, possible actor pulled)
            else if (state == 1)
            {
                distRetracted += MathF.Abs(speed * Global.spf);
                if (hookedActor != null && !(hookedActor is Character))
                {
                    if (!hookedActor.ownedByLocalPlayer)
                    {
                        hookedActor.takeOwnership();
                        RPC.clearOwnership.sendRpc(hookedActor.netId);
                    }
                    hookedActor.useGravity = false;
                    hookedActor.grounded = false;
                    hookedActor.move(hookedActor.pos.directionTo(wireSponge.getCenterPos()).normalize().times(speed));
                }
                if (distRetracted >= distMoved + 10)
                {
                    if (hookedActor != null && !(hookedActor is Character))
                    {
                        hookedActor.changePos(wireSponge.getCenterPos());
                        hookedActor.useGravity = true;
                    }
                    destroySelf();
                }
            }
            // Retracting (pulled towards wall)
            else if (state == 2)
            {
                wireSponge.useGravity = false;
                wireSponge.stopMoving();
                wireSponge.move(toWallVel);
                distRetracted += MathF.Abs(toWallVel.magnitude * Global.spf);
                var collision = Global.level.checkCollisionActor(wireSponge, toWallVel.x * Global.spf, toWallVel.y * Global.spf, toWallVel);
                if (distRetracted >= distMoved + 20 || collision?.gameObject is Wall)
                {
                    destroySelf();
                }
            }
        }

        public override void onDestroy()
        {
            var hookedChar = hookedActor as Character;

            if (hookedChar != null && hookedChar.charState is StrikeChainHooked)
            {
                hookedChar.changeState(new Idle());
            }
            if (hookedActor is Anim)
            {
                hookedActor.useGravity = true;
                hookedActor.vel.x = xDir * 150;
                hookedActor.vel.y = -100;
                (hookedActor as Anim).ttl = 0.5f;
            }
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            Point origin = ownedByLocalPlayer ? wireSponge.getFirstPOIOrDefault() : netOrigin;
            renderGeneric(origin);
            if (!ownedByLocalPlayer) return;
            netOrigin = origin;
        }

        public void renderGeneric(Point origin)
        {
            float distFromStartX = MathF.Abs(pos.x - origin.x);
            const float len = 8;
            float pieceCount = distFromStartX / len;
            for (int i = 0; i < pieceCount; i++)
            {
                Global.sprites["wsponge_vine_base_left"].draw(0, origin.x + (xDir * len * i), pos.y, xDir, 1, null, 1, 1, 1, ZIndex.Background + 100);
            }
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (!ownedByLocalPlayer) return;
            if (hookedActor != null) return;
            if (destroyed) return;
            if (state == 2) return;
            if (wireSponge.destroyed) return;
            if (reversed) return;
            if (state == 1 && time > 0.2f) return;

            // This code prevents the strike chain landing on the ground when X is falling and has the chain extended and still pulling X
            if (other.gameObject is Wall w && !w.isCracked)
            {
                var triggerList = Global.level.getTriggerList(this, -deltaPos.x, 0, null, typeof(Wall), typeof(Actor));
                if (triggerList.Any(t => t.gameObject == other.gameObject))
                {
                    return;
                }
            }

            var wall = other.gameObject as Wall;
            var actor = other.gameObject as Actor;

            if (wall != null && wall.collider.isClimbable && !wall.topWall)
            {
                reversed = true;
                state = 2;
                toWallVel = chainVel;
                chainVel.x = 0;
                chainVel.y = 0;
                var hitPoint = other.getHitPointSafe();
                changePos(new Point(hitPoint.x - wireSponge.xDir * 8, pos.y));
                distMoved = pos.distanceTo(wireSponge.getFirstPOIOrDefault());
                // wireSponge.changeState(new StrikeChainPullToWall(this, wireSponge.charState.shootSprite, toWallVel.y < 0), true);
            }
            else if (actor != null)
            {
                var chr = actor as Character;
                var pickup = actor as Pickup;
                if (chr == null && pickup == null) return;
                if (chr != null && (!chr.canBeDamaged(player.alliance, player.id, projId) || isDefenderFavored())) return;
                changePos(new Point(chr.pos.x, pos.y));
                distMoved = pos.distanceTo(wireSponge.getFirstPOIOrDefault());
                hookActor(actor);
                if (chr != null && chr.canBeDamaged(player.alliance, player.id, projId))
                {
                    if (Global.serverClient != null)
                    {
                        RPC.commandGrabPlayer.sendRpc(netId, chr.netId, CommandGrabScenario.StrikeChain, false);
                    }
                    chr.hook(this);
                }
            }
        }

        public void hookActor(Actor actor)
        {
            int reverse = 1;
            if (state == 1)
            {
                if (time <= 0.2f) reverse = -1;
            }

            state = 1;
            chainVel.x *= -1 * reverse;
            chainVel.y *= -1 * reverse;
            reversed = true;
            hookedActor = actor;
            updateDamager(0);
        }

        public override DamagerMessage onDamage(IDamagable damagable, Player attacker)
        {
            if (isDefenderFavored())
            {
                if (damagable is Character chr)
                {
                    if (Global.serverClient != null)
                    {
                        RPC.commandGrabPlayer.sendRpc(netId, chr.netId, CommandGrabScenario.StrikeChain, true);
                    }
                    chr.hook(this);
                }
            }

            return null;
        }

        public void reverseDir()
        {
            reversed = true;
            chainVel.x *= -1;
            chainVel.y *= -1;
            state = 1;
            time = 0;
        }

        public bool isLatched()
        {
            return state == 2;   
        }
    }

    public class WSpongeSideChainState : MaverickState
    {
        WSpongeSideChainProj proj;
        new int jumpFramesHeld;
        new const int maxJumpFrames = 10;
        bool jumpedOnce;
        float spinTime;

        public WSpongeSideChainState(float spinTime) : base("vine_throw", "")
        {
            this.spinTime = spinTime;
        }

        public override void update()
        {
            base.update();

            bool jumpHeld = input.isHeld(Control.Jump, player);
            if (jumpHeld)
            {
                jumpFramesHeld++;
                if (jumpFramesHeld > maxJumpFrames)
                {
                    jumpHeld = false;
                }
            }
            if (!jumpHeld)
            {
                if (!jumpedOnce && jumpFramesHeld > 0)
                {
                    jumpedOnce = true;
                    maverick.vel.y = -maverick.getJumpPower() * getJumpModifier();
                    jumpFramesHeld = 0;
                    maverick.changeSprite("vinethrow_jump", true);
                }
            }

            if (!maverick.grounded && (proj == null || !proj.isLatched()))
            {
                var inputDir = input.getInputDir(player);
                if (MathF.Sign(inputDir.x) == maverick.xDir)
                {
                    maverick.move(new Point(inputDir.x * 150, 0));
                }

                if (Global.level.checkCollisionActor(maverick, 0, -1) != null && maverick.vel.y < 0)
                {
                    maverick.vel.y = 0;
                }
            }

            if (!maverick.grounded && maverick.sprite.name.EndsWith("wsponge_vinethrow_jump"))
            {
                maverick.changeSpriteFromName("vine_throw", true);
            }

            if (proj == null && maverick.getFirstPOI() != null)
            {
                proj = new WSpongeSideChainProj((maverick as WireSponge).chainWeapon, maverick.getFirstPOIOrDefault(), maverick.xDir, maverick, spinTime, player, player.getNextActorNetId(), rpc: true);
                maverick.playSound("wspongeChain", sendRpc: true);
            }
            else if (proj != null)
            {
                if (input.isPressed(Control.Shoot, player) && !proj.reversed)
                {
                    proj.reverseDir();
                }

                if (proj.destroyed)
                {
                    maverick.changeToIdleOrFall();
                    return;
                }
            }
        }

        public new float getJumpModifier()
        {
            if (jumpFramesHeld == 1) return 1f;
            if (jumpFramesHeld == 2) return 1f;
            if (jumpFramesHeld == 3) return 1.01f;
            if (jumpFramesHeld == 4) return 1.015f;
            if (jumpFramesHeld == 5) return 1.02f;
            if (jumpFramesHeld == 6) return 1.025f;
            if (jumpFramesHeld == 7) return 1.05f;
            if (jumpFramesHeld == 8) return 1.1f;
            if (jumpFramesHeld == 9) return 1.25f;
            if (jumpFramesHeld >= 10) return 1.5f;
            return 0;
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            proj?.destroySelf();
            maverick.useGravity = true;
        }
    }
    
    public class WSpongeClingState : MaverickState
    {
        new int jumpFramesHeld;
        new int maxJumpFrames = 1;
        bool jumpedOnce;
        public WSpongeClingState() : base("cling", "")
        {
        }

        public override void update()
        {
            base.update();

            var inputDir = input.getInputDir(player);
            if (MathF.Sign(inputDir.x) == maverick.xDir)
            {
                var hitWall = maverick.getHitWall(maverick.xDir * 5, 0);
                if (hitWall == null)
                {
                    maverick.changeToIdleOrFall();
                    return;
                }
            }

            if (maverick.isAnimOver())
            {
                bool jumpHeld = input.isHeld(Control.Jump, player);
                if (jumpHeld)
                {
                    jumpFramesHeld++;
                    if (jumpFramesHeld >= maxJumpFrames)
                    {
                        jumpFramesHeld = maxJumpFrames;
                        jumpHeld = false;
                    }
                }
                if (!jumpHeld)
                {
                    if (!jumpedOnce && jumpFramesHeld > 0)
                    {
                        jumpedOnce = true;
                        maverick.vel.y = -maverick.getJumpPower() * getJumpModifier();
                        jumpFramesHeld = 0;
                        maverick.changeState(new MJump() { fromCling = true });
                        return;
                    }
                }
            }

            if (MathF.Sign(inputDir.x) != maverick.xDir)
            {
                maverick.changeToIdleOrFall();
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.useGravity = false;
            maverick.stopMoving();
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            maverick.useGravity = true;
        }

        public new float getJumpModifier()
        {
            if (jumpFramesHeld == 1) return 1f;
            if (jumpFramesHeld == 2) return 1f;
            if (jumpFramesHeld == 3) return 1.01f;
            if (jumpFramesHeld == 4) return 1.015f;
            if (jumpFramesHeld == 5) return 1.02f;
            if (jumpFramesHeld == 6) return 1.025f;
            if (jumpFramesHeld == 7) return 1.05f;
            if (jumpFramesHeld == 8) return 1.1f;
            if (jumpFramesHeld == 9) return 1.25f;
            if (jumpFramesHeld >= 10) return 1.5f;
            return 0;
        }
    }

    public class WSpongeUpChainProj : Projectile
    {
        public float dist;
        public float maxDist;
        public bool reversed;
        public Actor wireSponge;
        public bool latched;
        public Point netOrigin;
        public WSpongeUpChainProj(Weapon weapon, Point pos, int xDir, Actor wireSponge, float spinTime, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 3, player, "wsponge_vine_spike_up", Global.defFlinch, 0.25f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.WSpongeUpChain;
            setIndestructableProperties();
            this.wireSponge = wireSponge;
            vel = new Point(0, -350);
            maxDist = 50 + (int)(Helpers.clampMax(spinTime, 1.5f) * 100);
            netOrigin = pos;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();
            if (!ownedByLocalPlayer) return;

            var hit = Global.level.checkCollisionActor(this, 0, -1);
            if (hit?.gameObject is Wall wall && !wall.topWall && !wall.isMoving && !reversed)
            {
                latched = true;
                vel.y = 0;
                collider.isTrigger = true;
                var hitPoint = hit.getHitPointSafe();
                changePos(new Point(pos.x, hitPoint.y + 8));
                updateDamager(0);
            }

            dist += MathF.Abs(vel.y * Global.spf);

            if (dist > maxDist && !reversed && !latched)
            {
                reverse();
            }

            if (dist > maxDist * 2 || (reversed && pos.y > wireSponge.getFirstPOIOrDefault().y))
            {
                destroySelf();
                return;
            }
        }

        public void reverse()
        {
            reversed = true;
            vel.y *= -1;
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            Point origin = ownedByLocalPlayer ? wireSponge.getFirstPOIOrDefault() : netOrigin;
            renderGeneric(origin);
            if (!ownedByLocalPlayer) return;
            netOrigin = origin;
        }

        public void renderGeneric(Point origin)
        {
            float distFromStartY = MathF.Abs(pos.y - origin.y);
            float len = 8;
            float pieceCount = distFromStartY / len;
            for (int i = 0; i < pieceCount; i++)
            {
                Global.sprites["wsponge_vine_base_up"].draw(0, pos.x, origin.y - (len * i), xDir, 1, null, 1, 1, 1, ZIndex.Background + 100);
            }
        }
    }

    public class WSpongeUpChainStartState : MaverickState
    {
        WSpongeUpChainProj proj;
        public WSpongeUpChainStartState() : base("vine_up_start", "")
        {
        }

        public override void update()
        {
            base.update();

            if (proj == null && maverick.getFirstPOI() != null)
            {
                proj = new WSpongeUpChainProj((maverick as WireSponge).chainWeapon, maverick.getFirstPOIOrDefault(), maverick.xDir, maverick, 1, player, player.getNextActorNetId(), rpc: true);
                maverick.playSound("wspongeChain", sendRpc: true);
            }

            if (proj != null)
            {
                if (!proj.latched)
                {
                    proj.incPos(maverick.deltaPos);
                    if (!proj.reversed && input.isPressed(Control.Shoot, player))
                    {
                        proj.reverse();
                    }
                }
                else
                {
                    maverick.changeState(new WSpongeUpChainLatchState(proj));
                    return;
                }

                if (proj.destroyed)
                {
                    maverick.changeToIdleOrFall();
                    return;
                }
            }

            if (stateTime > 1.5f)
            {
                maverick.changeToIdleOrFall();
                return;
            }
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            if (newState is not WSpongeUpChainLatchState)
            {
                proj?.destroySelf();
            }
        }
    }

    public class WSpongeUpChainLatchState : MaverickState
    {
        WSpongeUpChainProj proj;
        float dist;
        float distToTravel;
        public WSpongeUpChainLatchState(WSpongeUpChainProj proj) : base("vine_up_loop", "")
        {
            this.proj = proj;
        }

        public override void update()
        {
            base.update();

            if (proj == null || proj.destroyed)
            {
                maverick.changeToIdleOrFall();
                return;
            }

            if (dist < distToTravel)
            {
                maverick.move(new Point(0, -200));
                dist += 200 * Global.spf;
                var hit = Global.level.checkCollisionActor(maverick, 0, -25);
                if (hit != null)
                {
                    dist = distToTravel;
                }
            }
            else
            {
                maverick.changeState(new WSpongeUpChainHangState(proj));
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            distToTravel = MathF.Abs(proj.pos.y - maverick.pos.y) / 2;
            maverick.unstickFromGround();
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            if (newState is not WSpongeUpChainHangState)
            {
                proj?.destroySelf();
                maverick.useGravity = true;
            }
        }
    }

    public class WSpongeUpChainHangState : MaverickState
    {
        WSpongeUpChainProj proj;
        public WSpongeUpChainHangState(WSpongeUpChainProj proj) : base("vine_up_loop", "")
        {
            this.proj = proj;
        }

        public override void update()
        {
            base.update();

            if (proj == null || proj.destroyed)
            {
                maverick.changeToIdleOrFall();
                return;
            }

            if (input.isPressed(Control.Jump, player))
            {
                maverick.changeToIdleOrFall();
                return;
            }
            else if (input.isPressed(Control.Special1, player))
            {
                maverick.changeState(new WSpongeHangSeedThrowState(proj, Control.Special1));
                return;
            }

            if (input.isPressed(Control.Left, player) && maverick.xDir == 1)
            {
                maverick.xDir = -1;
                if (!maverick.tryMoveExact(new Point(maverick.xDir * 17, 0), out _))
                {
                    maverick.xDir = 1;
                }
            }
            else if (input.isPressed(Control.Right, player) && maverick.xDir == -1)
            {
                maverick.xDir = 1;
                if (!maverick.tryMoveExact(new Point(maverick.xDir * 17, 0), out _))
                {
                    maverick.xDir = -1;
                }
            }

            Point moveAmount = new Point(0, 0);
            if (input.isHeld(Control.Up, player) && maverick.pos.y - proj.pos.y > 70)
            {
                moveAmount.y = -1;
            }
            else if (input.isHeld(Control.Down, player) && maverick.pos.y - proj.pos.y < 200)
            {
                moveAmount.y = 1;
            }

            maverick.tryMove(moveAmount.times(200), out var hit);
            if (hit?.gameObject is Wall)
            {
                maverick.changeToIdleOrFall();
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.stopMoving();
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            if (newState is not WSpongeHangSeedThrowState)
            {
                proj?.destroySelf();
                maverick.useGravity = true;
            }
        }
    }
    
    public class WSpongeSeedProj : Projectile
    {
        bool once;
        public WSpongeSeedProj(Weapon weapon, Point pos, int xDir, Point vel, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 3, player, "wsponge_seed", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.WSpongeSeed;
            maxTime = 2f;
            destroyOnHit = true;
            useGravity = true;
            startSound = "wspongeSeed";
            collider.wallOnly = true;
            this.vel = vel;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            if (!ownedByLocalPlayer) return;
            plantSeed(other);
        }

        public void plantSeed(CollideData other)
        {
            if (!ownedByLocalPlayer) return;
            if (once) return;

            once = true;
            int type = 0;
            if (other == null) type = 0;
            else if (other.isSideWallHit()) type = 1;
            else if (other.isCeilingHit()) type = 2;

            var triggers = Global.level.getTriggerList(this, 0, 0);

            var seedSpike = new WSpongeSpike(weapon, other?.getHitPointSafe() ?? getCenterPos(), -xDir, type, owner, owner.getNextActorNetId(), rpc: true);

            if (triggers.Any(t => t.gameObject is WSpongeSpike))
            {
                seedSpike.incPos(new Point(Helpers.randomRange(-2, 2), 0));
            }

            owner.seeds.Add(seedSpike);
            if (owner.seeds.Count > 8)
            {
                owner.seeds[0].destroySelf();
            }
            destroySelf();
        }
    }

    public class WSpongeSpike : Projectile, IDamagable
    {
        public float health = 2;
        public WSpongeSpike(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 2, player, getSpriteFromType(type), Global.defFlinch, 0f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.WSpongeSpike;
            destroyOnHit = true;
            fadeSprite = "explosion";
            fadeSound = "explosion";
            
            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        private static string getSpriteFromType(int type)
        {
            if (type == 0) return "wsponge_spike_ground";
            if (type == 1) return "wsponge_spike_wall";
            return "wsponge_spike_ceiling";
        }

        public override void update()
        {
            base.update();
            updateProjectileCooldown();

            if (ownedByLocalPlayer)
            {
                moveWithMovingPlatform();
            }
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
            return damagerAlliance != owner.alliance;
        }

        public bool canBeHealed(int healerAlliance)
        {
            return false;
        }

        public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false)
        {
        }

        public bool isInvincible(Player attacker, int? projId)
        {
            return false;
        }

        public override void onDestroy()
        {
            base.onDestroy();
            owner.seeds.Remove(this);
        }
    }

    public class WSpongeSeedThrowState : MaverickState
    {
        string shootControl;
        int framesShootHeld;
        bool frameStopHeld;
        bool inputDirUpOnce;
        public WSpongeSeedThrowState(string shootControl) : base("seedthrow", "")
        {
            this.shootControl = shootControl;
            consecutiveData = new MaverickStateConsecutiveData(0, 4, 0.25f);
        }

        public override void update()
        {
            base.update();

            if (input.isHeld(shootControl, player) && !frameStopHeld)
            {
                framesShootHeld++;
            }
            if (!input.isHeld(shootControl, player))
            {
                frameStopHeld = false;
            }
            
            var inputDir = input.getInputDir(player);
            if (!inputDirUpOnce) inputDirUpOnce = inputDir.y < 0;

            if (!once && maverick.getFirstPOI() != null)
            {
                once = true;
                Point vel;

                float xMod = input.isXDirHeld(maverick.xDir, player) ? 2 : 1;
                float mag = 0.5f + (framesShootHeld / 19f);
                vel = new Point(100 * maverick.xDir * xMod * mag, -250 * (mag * 0.75f));
                
                if (inputDirUpOnce && inputDir.x == 0)
                {
                    vel.x = 0;
                    vel.y /= 0.75f;
                }

                if (isAI)
                {
                    vel = new Point(maverick.xDir * Helpers.randomRange(0, 200), Helpers.randomRange(-250, -200));
                }

                new WSpongeSeedProj(maverick.weapon, maverick.getFirstPOIOrDefault(), maverick.xDir, vel, player, player.getNextActorNetId(), rpc: true);
            }

            if (isAI)
            {
                if (consecutiveWaitTime > 0)
                {
                    consecutiveWaitTime += Global.spf;
                    if (consecutiveWaitTime >= consecutiveData.consecutiveDelay)
                    {
                        consecutiveData.consecutiveCount++;
                        var newState = new WSpongeSeedThrowState(shootControl);
                        newState.consecutiveData = consecutiveData;
                        maverick.changeState(newState, ignoreCooldown: true);
                    }
                }
                if (maverick.isAnimOver())
                {
                    if (consecutiveData?.isOver() == false)
                    {
                        if (consecutiveWaitTime == 0)
                        {
                            maverick.changeSpriteFromName("idle", true);
                            consecutiveWaitTime = Global.spf;
                        }
                    }
                    else
                    {
                        maverick.changeToIdleOrFall();
                    }
                }
            }
            else
            {
                if (maverick.isAnimOver())
                {
                    maverick.changeToIdleOrFall();
                }
            }
        }
    }

    public class WSpongeHangSeedThrowState : MaverickState
    {
        WSpongeUpChainProj proj;
        string shootControl;
        int framesShootHeld;
        bool frameStopHeld;
        bool inputDirUpOnce;
        public WSpongeHangSeedThrowState(WSpongeUpChainProj proj, string shootControl) : base("vine_up_seedthrow", "")
        {
            this.proj = proj;
            this.shootControl = shootControl;
        }

        public override void update()
        {
            base.update();

            if (proj == null || proj.destroyed)
            {
                maverick.changeToIdleOrFall();
                return;
            }

            proj.incPos(maverick.deltaPos);

            if (input.isHeld(shootControl, player) && !frameStopHeld)
            {
                framesShootHeld++;
            }
            if (!input.isHeld(shootControl, player))
            {
                frameStopHeld = false;
            }

            var inputDir = input.getInputDir(player);
            if (!inputDirUpOnce) inputDirUpOnce = inputDir.y < 0;

            if (!once && maverick.getFirstPOI() != null)
            {
                once = true;
                Point vel;

                float xMod = input.isXDirHeld(maverick.xDir, player) ? 2 : 1;
                float mag = 0.5f + (framesShootHeld / 19f);
                vel = new Point(100 * maverick.xDir * xMod * mag, -250 * (mag * 0.75f));
                if (inputDirUpOnce && inputDir.x == 0)
                {
                    vel.x = 0;
                    vel.y /= 0.75f;
                }

                new WSpongeSeedProj(maverick.weapon, maverick.getFirstPOIOrDefault(), maverick.xDir, vel, player, player.getNextActorNetId(), rpc: true);
                new WSpongeSeedProj(maverick.weapon, maverick.getFirstPOIOrDefault(), maverick.xDir, vel.addxy(-50, 0), player, player.getNextActorNetId(), rpc: true);
                new WSpongeSeedProj(maverick.weapon, maverick.getFirstPOIOrDefault(), maverick.xDir, vel.addxy(50, 0), player, player.getNextActorNetId(), rpc: true);
            }

            if (maverick.isAnimOver())
            {
                maverick.changeState(new WSpongeUpChainHangState(proj));
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            maverick.useGravity = false;
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            if (newState is not WSpongeUpChainHangState)
            {
                proj?.destroySelf();
                maverick.useGravity = true;
            }
        }
    }
    
    public class WSpongeChargeState : MaverickState
    {
        int state = 0;
        public const float maxChargeTime = 4;
        SoundWrapper chargeSound;

        public WSpongeChargeState() : base("angry_start", "")
        {
            superArmor = true;
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (state == 0)
            {
                if (!isAI && !input.isHeld(Control.Dash, player))
                {
                    maverick.changeToIdleOrFall();
                }

                (maverick as WireSponge).chargeTime = stateTime;
                if (stateTime > maxChargeTime)
                {
                    state = 1;
                    stateTime = 0;
                    maverick.changeSpriteFromName("angry_explode", true);
                }
            }
            else if (state == 1)
            {
                if (!once && maverick.getFirstPOI() != null)
                {
                    once = true;
                    maverick.playSound("wspongePuff", sendRpc: true);
                    new Anim(maverick.getFirstPOIOrDefault(), "wsponge_angry_puff", maverick.xDir, player.getNextActorNetId(), true, sendRpc: true);
                }
                if (maverick.isAnimOver())
                {
                    maverick.changeState(new WSpongeLightningState());
                }
            }
        }

        public override void onEnter(MaverickState oldState)
        {
            base.onEnter(oldState);
            chargeSound = maverick.playSound("wspongeCharge", sendRpc: true);
        }

        public override void onExit(MaverickState newState)
        {
            base.onExit(newState);
            (maverick as WireSponge).chargeTime = 0;
            if (!chargeSound.deleted)
            {
                chargeSound.sound.Stop();
            }
            RPC.stopSound.sendRpc("wspongeCharge", maverick.netId);
        }
    }

    public class WSpongeLightningState : MaverickState
    {
        int state;
        Point poi;
        public WSpongeLightningState() : base("angry_thunder_start", "")
        {
            superArmor = true;
        }

        public override void update()
        {
            base.update();
            if (player == null) return;

            if (state == 0 && maverick.getFirstPOI() != null)
            {
                state++;
                poi = maverick.getFirstPOIOrDefault();
                new WolfSigmaBeam(maverick.weapon, poi.addxy(0, -130), maverick.xDir, 1, 1, player, player.getNextActorNetId(), rpc: true);
            }

            if (state == 1 && maverick.frameIndex == 5)
            {
                state++;
                var spawnPos = poi.addxy(50 * maverick.xDir, -120);
                var closestTarget = Global.level.getClosestTarget(poi, player.alliance, true, aMaxDist: 150);
                if (closestTarget != null) spawnPos.x = closestTarget.pos.x;
                new WolfSigmaBeam(maverick.weapon, spawnPos, maverick.xDir, 1, 2, player, player.getNextActorNetId(), rpc: true);
            }

            if (state == 2 && maverick.frameIndex == 8)
            {
                state++;
                var spawnPos = poi.addxy(-50 * maverick.xDir, -120);
                var closestTarget = Global.level.getClosestTarget(poi, player.alliance, true, aMaxDist: 150);
                if (closestTarget != null) spawnPos.x = closestTarget.pos.x;
                new WolfSigmaBeam(maverick.weapon, spawnPos, maverick.xDir, 1, 2, player, player.getNextActorNetId(), rpc: true);
            }

            if (maverick.isAnimOver())
            {
                maverick.changeToIdleOrFall();
            }
        }
    }
}
