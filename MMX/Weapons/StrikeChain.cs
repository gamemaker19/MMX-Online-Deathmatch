using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class StrikeChain : Weapon
    {
        public StrikeChain() : base()
        {
            shootSounds = new List<string>() { "strikeChain", "strikeChain", "strikeChain", "strikeChainCharged" };
            rateOfFire = 0.75f;
            index = (int)WeaponIds.StrikeChain;
            weaponBarBaseIndex = 14;
            weaponBarIndex = weaponBarBaseIndex;
            weaponSlotIndex = 14;
            killFeedIndex = 20 + (index - 9);
            weaknessIndex = 13;
            switchCooldown = 0;
        }

        public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId)
        {
            if (!player.ownedByLocalPlayer) return;

            int upOrDown = 0;
            if (player.input.isHeld(Control.Up, player)) upOrDown = -1;
            else if (player.input.isHeld(Control.Down, player))
            {
                if ((player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player)) && player.character.grounded) { }
                else upOrDown = 1;
            }
            if (chargeLevel != 3)
            {
                new StrikeChainProj(this, pos, xDir, 0, upOrDown, player, netProjId, rpc: true);
            }
            else
            {
                new StrikeChainProj(this, pos, xDir, 1, upOrDown, player, netProjId, rpc: true);
            }
        }
    }

    public class StrikeChainProj : Projectile
    {
        public int state = 0;
        public Player player;
        public float distMoved;
        public float distRetracted;
        public bool reversed;
        public Point toWallVel;
        public Actor hookedActor;
        public List<Sprite> spriteMids = new List<Sprite>();
        public int maxDist = 120;
        public int origXDir;
        public int type;
        public bool isCharged { get { return type == 1; } }
        public float hookWaitTime;
        public int upOrDown;
        public Point chainVel;
        public Actor anchorActor;
        public StrikeChainProj(Weapon weapon, Point pos, int xDir, int type, int upOrDown, Player player, ushort netProjId, bool rpc = false) : 
            base(weapon, pos, xDir, 400, 2, player, xDir == 1 ? "strikechain_proj" : "strikechain_proj_left", 0, 0.5f, netProjId, player.ownedByLocalPlayer)
        {
            destroyOnHit = false;
            this.player = player;
            this.type = type;
            if (player?.character != null)
            {
                player.character.strikeChainProj = this;
            }
            origXDir = xDir;
            projId = (int)ProjIds.StrikeChain;

            if (type == 1)
            {
                changeSprite(xDir == 1 ? "strikechain_charged" : "strikechain_charged_left", true);
                maxDist = 180;
                damager.damage = 4;
                speed *= 1.5f;
            }

            if (player?.character?.flag != null)
            {
                maxDist /= 2;
            }

            this.upOrDown = upOrDown;
            if (upOrDown == 0)
            {
                angle = 0;
                if (xDir == -1) angle = 180;
            }
            else if (upOrDown == 1)
            {
                angle = 45;
                if (xDir == -1) angle = 135;
            }
            else if (upOrDown == -1)
            {
                angle = -45;
                if (xDir == -1) angle = -135;
            }

            chainVel.x = Helpers.cosd(angle.Value) * speed;
            chainVel.y = Helpers.sind(angle.Value) * speed;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir, (byte)type, (byte)(upOrDown + 128));
            }
            for (int i = 0; i < maxDist / 8; i++)
            {
                string sprite = "";
                if (type == 0 && xDir == 1) sprite = "strikechain_chain";
                if (type == 0 && xDir == -1) sprite = "strikechain_chain_left";
                if (type == 1 && xDir == 1) sprite = "strikechain_charged_chain";
                if (type == 1 && xDir == -1) sprite = "strikechain_charged_chain_left";

                var midSprite = Global.sprites[sprite].clone();
                spriteMids.Add(midSprite);
            }
        }

        public override void postUpdate()
        {
            base.postUpdate();
            if (player.character != null)
            {
                var shootPos = player.character.getShootPos();
                changePos(new Point(shootPos.x + (distMoved - distRetracted) * Helpers.cosd(angle.Value), shootPos.y + (distMoved - distRetracted) * Helpers.sind(angle.Value)));
            }
        }

        public override void update()
        {
            base.update();
            frameIndex = 1;

            if (player.character == null || (player.character.xDir != origXDir && !(player.character.charState is WallSlide)))
            {
                destroySelf();
                return;
            }

            if (!ownedByLocalPlayer)
            {
                if (!reversed) distMoved += MathF.Abs(speed * Global.spf);
                else distRetracted += MathF.Abs(speed * Global.spf);
                return;
            }

            if (anchorActor != null && !anchorActor.destroyed)
            {
                incPos(anchorActor.deltaPos);
                player.character.incPos(anchorActor.deltaPos);
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
                    reverseDir();
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
                    hookedActor.move(hookedActor.pos.directionTo(player.character.getCenterPos()).normalize().times(speed));
                }
                if (distRetracted >= distMoved + 10)
                {
                    if (hookedActor != null && !(hookedActor is Character))
                    {
                        hookedActor.changePos(player.character.getCenterPos());
                        hookedActor.useGravity = true;
                    }
                    destroySelf();
                }
            }
            // Retracting (pulled towards wall)
            else if (state == 2)
            {
                player.character.move(toWallVel);
                distRetracted += MathF.Abs(toWallVel.magnitude * Global.spf);
                var collision = Global.level.checkCollisionActor(player.character, toWallVel.x * Global.spf, toWallVel.y * Global.spf, toWallVel);
                if (distRetracted >= distMoved || collision?.gameObject is Wall)
                {
                    destroySelf();
                    float momentum = 0.25f * (distRetracted / maxDist);
                    player.character.xSwingVel = toWallVel.x * (0.25f + momentum) * 0.5f;
                    if (player.character.isDashing && player.bootsArmorNum == 2 && player.character.flag == null) player.character.xSwingVel *= 1.1f;
                    player.character.vel.y = toWallVel.y;
                }
            }
        }

        public override void onDestroy()
        {
            if (player.character != null)
            {
                player.character.strikeChainProj = null;
            }
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
            float length = distMoved - distRetracted;
            int chainFrame = Helpers.clamp((int)(5 * length / maxDist), 0, 5);
            int maxI = MathF.Ceiling(length / 8);
            for (int i = 0; i < maxI; i++)
            {
                if (i >= spriteMids.Count) break;
                if (i == maxI - 1 && xDir == -1) break;
                float xOff = 0;
                float yOff = 0;
                int iOff = (xDir == -1 ? 1 : 0);
                if (angle == 0 || angle == 180)
                {
                    xOff = (i + iOff) * xDir * -8;
                }
                else
                {
                    xOff = (i + iOff) * xDir * -5.657f;
                    yOff = (i + iOff) * upOrDown * -5.657f;
                }
                spriteMids[i].draw(5 - chainFrame, pos.x + x + xOff, pos.y + y + yOff, xDir, yDir, getRenderEffectSet(), 1, 1, 1, ZIndex.Character - 1, angle: angle.Value);
            }
            base.render(x, y);
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (!ownedByLocalPlayer) return;
            if (hookedActor != null) return;
            if (destroyed) return;
            if (state == 2) return;
            if (player.character == null) return;
            if (reversed) return;
            if (state == 1 && time > 0.2f) return;
            
            // This code prevents the strike chain landing on the ground when X is falling and has the chain extended and still pulling X
            if (other.gameObject is Wall w && !w.isCracked)
            {
                if (angle == 0 || angle == 180)
                {
                    var triggerList = Global.level.getTriggerList(this, -deltaPos.x, 0, null, typeof(Wall), typeof(Actor));
                    if (triggerList.Any(t => t.gameObject == other.gameObject))
                    {
                        return;
                    }
                }
            }

            var wall = other.gameObject as Wall;
            var actor = other.gameObject as Actor;

            //bool latchToActor = type == 0 && actor != null && (actor is Character || actor is Maverick || actor is RideArmor || actor is RideChaser);
            bool latchToActor = false;

            if ((wall != null && wall.collider.isClimbable && !wall.topWall) || latchToActor)
            {
                reverseDir();
                state = 2;
                toWallVel = chainVel;
                if (player.character.flag != null) toWallVel.multiply(0.5f);
                chainVel.x = 0;
                chainVel.y = 0;
                if (player.character.charState is WallSlide)
                {
                    player.character.xDir *= -1;
                    player.character.changeState(new Fall(), true);
                }
                else if (toWallVel.y < 0 && player.character.grounded)
                {
                    player.character.grounded = false;
                    player.character.changeState(new Fall(), true);
                }
                distMoved = pos.distanceTo(player.character.getShootPos());
                player.character.changeState(new StrikeChainPullToWall(this, player.character.charState.shootSprite, toWallVel.y < 0), true);
                anchorActor = actor;
            }
            else if (actor != null)
            {
                var chr = actor as Character;
                var pickup = actor as Pickup;
                if (chr == null && pickup == null)
                {
                    if (actor is Maverick || actor is RideArmor)
                    {
                        hookActor(null);
                    }
                    return;
                }
                if (chr != null && (!chr.canBeDamaged(player.alliance, player.id, projId) || isDefenderFavored())) return;
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
            reverseDir();
            hookedActor = actor;
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
            if (ownedByLocalPlayer)
            {
                Global.serverClient?.rpc(RPC.playerToggle, (byte)player.id, (byte)RPCToggleType.StrikeChainReversed);
            }
        }
    }

    public class StrikeChainPullToWall : CharState
    {
        StrikeChainProj scp;
        public bool isUp;
        public StrikeChainPullToWall(StrikeChainProj scp, string lastSprite, bool isUp) : base(string.IsNullOrEmpty(lastSprite) ? "shoot" : lastSprite, "", "", "")
        {
            this.scp = scp;
            this.isUp = isUp;
        }

        public override void update()
        {
            base.update();
            if (scp == null || scp.destroyed)
            {
                var collision = Global.level.checkCollisionActor(player.character, 0, 1);
                if (collision?.gameObject is Wall)
                {
                    player.character.vel.y = 0;
                    player.character.changeState(new Idle(), true);
                }
                else
                {
                    player.character.changeState(new Fall(), true);
                }
                return;
            }
        }

        public override bool canEnter(Character character)
        {
            if (character.isCCImmune()) return false;
            return base.canEnter(character);
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            player.character.useGravity = false;
            player.character.vel.y = 0;
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            player.character.useGravity = true;
            if (scp != null && !scp.destroyed) scp.destroySelf();
        }
    }

    public class StrikeChainHooked : CharState
    {
        float stunTime;
        Projectile scp;
        Point lastScpPos;
        bool isDone;
        bool flinch;
        bool isWireSponge;
        public StrikeChainHooked(Projectile scp, bool flinch) : base(flinch ? "hurt" : "fall", flinch ? "" : "fall_shoot", flinch ? "" : "fall_attack")
        {
            this.scp = scp;
            this.flinch = flinch;
            isGrabbedState = true;
            lastScpPos = scp.pos;
            isWireSponge = scp is WSpongeSideChainProj;
        }

        public override bool canEnter(Character character)
        {
            if (!character.ownedByLocalPlayer) return false;
            if (!base.canEnter(character)) return false;
            if (character.isCCImmune()) return false;
            return !character.charState.invincible;
        }

        public override bool canExit(Character character, CharState newState)
        {
            if (newState is Die) return true;
            return isDone;
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            player.character.useGravity = false;
            player.character.vel.y = 0;
            player.character.mk5RideArmorPlatform = null;
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            player.character.useGravity = true;
        }

        public override void update()
        {
            base.update();

            Character scpChar = scp.damager?.owner?.character;
            if (scp is StrikeChainProj && scpChar != null && !isWireSponge)
            {
                if (scpChar.getShootXDir() == 1 && character.pos.x < scpChar.pos.x + 15)
                {
                    stateTime = 5;
                }
                else if (scpChar.getShootXDir() == -1 && character.pos.x > scpChar.pos.x - 15)
                {
                    stateTime = 5;
                }
            }

            if (scp.destroyed || stateTime >= 5)
            {
                player.character.useGravity = true;
                stunTime += Global.spf;
                if (!flinch || stunTime > 0.375f)
                {
                    isDone = true;
                    character.changeState(new Idle(), true);
                    return;
                }
            }
            else if (scpChar != null)
            {
                if (!isWireSponge)
                {
                    Point newPos = character.pos.add(scp.deltaPos);
                    if (newPos.distanceTo(scpChar.pos) < character.pos.distanceTo(scpChar.pos))
                    {
                        character.move(scp.deltaPos, useDeltaTime: false);
                    }
                }
                else
                {
                    if (Math.Sign(scp.deltaPos.x) != scp.xDir)
                    {
                        character.move(scp.deltaPos, useDeltaTime: false);
                    }
                }
            }
        }
    }
}
