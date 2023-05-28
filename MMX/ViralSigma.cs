using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class ViralSigmaTackleWeapon : Weapon
    {
        public ViralSigmaTackleWeapon(Player player) : base()
        {
            index = (int)WeaponIds.Sigma2ViralTackle;
            damager = new Damager(player, 4, Global.defFlinch, 1f);
            killFeedIndex = 136;
        }
    }

    public class ViralSigmaBeamWeapon : Weapon
    {
        public ViralSigmaBeamWeapon() : base()
        {
            index = (int)WeaponIds.Sigma2ViralBeam;
            killFeedIndex = 136;
        }
    }

    public class ViralSigmaIdle : CharState
    {
        bool winTauntOnce;

        public ViralSigmaIdle() : base("viral_idle")
        {
            immuneToWind = true;
        }

        public override void update()
        {
            stateTime += Global.spf;
            character.stopMoving();

            var inputDir = player.input.getInputDir(player);
            var moveAmount = inputDir.times(125);
            character.move(moveAmount);

            clampViralSigmaPos();

            if (character.angle == 0)
            {
                if (player.input.isPressed(Control.Dash, player) && character.viralSigmaTackleCooldown == 0)
                {
                    character.viralSigmaTackleCooldown = character.viralSigmaTackleMaxCooldown;
                    character.changeState(new ViralSigmaTackle(inputDir), true);
                    return;
                }

                if (player.input.isPressed(Control.Special1, player))
                {
                    character.changeState(new ViralSigmaBeamState(), true);
                    return;
                }

                if (player.input.isPressed(Control.Shoot, player) && player.sigmaAmmo >= player.weapon.getAmmoUsage(0))
                {
                    character.changeState(new ViralSigmaShoot(inputDir.x != 0 ? inputDir.x : character.lastViralSigmaXDir), true);
                    return;
                }

                if (player.input.isPressed(Control.Jump, player) && character.possessTarget != null)
                {
                    character.changeState(new ViralSigmaPossessStart(character.possessTarget), true);
                    return;
                }

                if (Global.level.gameMode.isOver && Global.level.gameMode.playerWon(player))
                {
                    if (!winTauntOnce)
                    {
                        winTauntOnce = true;
                        character.changeState(new ViralSigmaTaunt(true), true);
                    }
                }
                else if (player.input.isPressed(Control.Taunt, player))
                {
                    character.changeState(new ViralSigmaTaunt(false), true);
                }
            }
        }
    }

    public class ViralSigmaTaunt : CharState
    {
        float lastFrameTime;
        bool isWin;
        public ViralSigmaTaunt(bool isWin) : base("viral_taunt")
        {
            immuneToWind = true;
            this.isWin = isWin;
        }

        public override void update()
        {
            base.update();
            if (character.loopCount > 2)
            {
                if (character.frameIndex >= character.sprite.frames.Count - 1)
                {
                    character.frameIndex = character.sprite.frames.Count - 1;
                    character.frameSpeed = 0;
                    lastFrameTime += Global.spf;
                    if (lastFrameTime > 0 && !isWin)
                    {
                        character.changeState(new ViralSigmaIdle(), true);
                    }
                }
            }
        }
    }

    public class ViralSigmaPossessStart : CharState
    {
        Character target;
        public ViralSigmaPossessStart(Character target) : base("viral_possess")
        {
            immuneToWind = true;
            this.target = target;
        }

        public override void update()
        {
            base.update();

            var inputDir = player.input.getInputDir(player);
            //character.move(inputDir.times(125));
            character.changePos(target.pos.addxy(0, -20));

            character.possessEnemyTime += Global.spf;
            if (character.possessEnemyTime > character.maxPossessEnemyTime)
            {
                character.possessEnemyTime = 0;
                character.numPossesses++;
                float ammoToRefill = 32; //character.player.health
                player.sigmaAmmo += ammoToRefill;
                if (player.sigmaAmmo > player.sigmaMaxAmmo) player.sigmaAmmo = player.sigmaMaxAmmo;
                target.player.startPossess(player, sendRpc: true);
                character.changeState(new ViralSigmaPossess(target), true);
                return;
            }

            if ((stateTime > 0.2f && !player.input.isHeld(Control.Jump, player)) || !character.canPossess(target))
            {
                character.changeState(new ViralSigmaIdle(), true);
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.possessEnemyTime = 0;
        }
    }

    public class ViralSigmaPossess : CharState
    {
        public Character target;
        int state;
        public ViralSigmaPossess(Character target) : base("viral_exit")
        {
            immuneToWind = true;
            this.target = target;
        }

        public override void update()
        {
            base.update();
            if (state == 0)
            {
                character.xScale -= Global.spf;
                if (character.xScale < 0) character.xScale = 0;
                character.yScale = character.xScale;
                character.changePos(target.pos.addxy(0, -20));
                bool unpossessButtonPressed = player.input.isPressed(Control.Shoot, player) || player.input.isPressed(Control.Special1, player);
                if (target.player.possessedTime == 0 || target.destroyed || unpossessButtonPressed)
                {
                    unpossess();
                }
                else
                {
                    target.player.possesserUpdate();
                }
            }
            else if (state == 1)
            {
                character.xScale += Global.spf;
                if (character.xScale > 1) character.xScale = 1;
                character.yScale = character.xScale;
                if (character.xScale >= 1)
                {
                    character.changeState(new ViralSigmaIdle(), true);
                }
            }
        }

        public void unpossess()
        {
            target.player.unpossess(sendRpc: true);
            character.possessTarget = null;
            target = null;
            state = 1;
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            target?.player?.unpossess(sendRpc: true);
            character.possessTarget = null;
        }
    }

    public class ViralSigmaShoot : CharState
    {
        int xDir;
        ViralSigmaShootProj proj;
        public ViralSigmaShoot(float xDir) : base("viral_spit")
        {
            immuneToWind = true;
            this.xDir = MathF.Sign(xDir);
            if (this.xDir == 0) this.xDir = 1;
        }

        public override void update()
        {
            base.update();

            var mechaniloidWeapon = player.weapon as MechaniloidWeapon;
            var poi = character.getFirstPOI();
            if (poi != null && !once)
            {
                player.sigmaAmmo -= mechaniloidWeapon.getAmmoUsage(0);
                once = true;
                character.playSound("viralSigmaShoot", sendRpc: true);
                proj = new ViralSigmaShootProj(mechaniloidWeapon, poi.Value, xDir, player, player.getNextActorNetId(), rpc: true);
            }

            if (player.input.isPressed(Control.Shoot, player) && proj != null && proj.time > 0.05f && (mechaniloidWeapon.mechaniloidType == MechaniloidType.Bird || mechaniloidWeapon.mechaniloidType == MechaniloidType.Fish))
            {
                proj.destroySelf();
            }

            if (character.isAnimOver())
            {
                character.changeState(new ViralSigmaIdle(), true);
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.xDir = xDir;
            character.angle = null;
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.xDir = 1;
        }
    }

    public class ViralSigmaShootProj : Projectile
    {
        MechaniloidWeapon mechaniloidWeapon;
        public ViralSigmaShootProj(MechaniloidWeapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 0, 0, player, "sigma2_viral_proj", 0, 0.1f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.Sigma2ViralProj;
            destroyOnHit = true;
            vel = new Point(150 * xDir, 250);
            maxTime = 0.5f;
            mechaniloidWeapon = weapon;

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            if (!ownedByLocalPlayer) return;
            destroySelf();
        }

        public override void onDestroy()
        {
            base.onDestroy();
            if (ownedByLocalPlayer && (time < maxTime || mechaniloidWeapon.mechaniloidType == MechaniloidType.Bird || mechaniloidWeapon.mechaniloidType == MechaniloidType.Fish))
            {
                Actor mechaniloid = null;
                if (mechaniloidWeapon.mechaniloidType == MechaniloidType.Bird)
                {
                    mechaniloid = new BirdMechaniloidProj(weapon, pos, xDir, owner, owner.getNextActorNetId(), rpc: true);
                }
                else
                {
                    mechaniloid = new Mechaniloid(pos, owner, xDir, mechaniloidWeapon, mechaniloidWeapon.mechaniloidType, owner.getNextActorNetId(), true, rpc: true);
                }
                owner.mechaniloids.Add(mechaniloid);
                if (owner.mechaniloids.Count > 3)
                {
                    owner.mechaniloids[0].destroySelf();
                }
            }
        }

        public override void update()
        {
            base.update();
        }
    }

    public class ViralSigmaTackle : CharState
    {
        Point tackleDir;
        public ViralSigmaTackle(Point tackleDir) : base("viral_tackle")
        {
            immuneToWind = true;
            this.tackleDir = tackleDir;
        }

        public override void update()
        {
            base.update();
            if (tackleDir.x > 0) character.viralSigmaAngle = Helpers.to360(tackleDir.angle + 90);
            else character.viralSigmaAngle = Helpers.to360(tackleDir.angle + 90);
            if (!character.tryMove(tackleDir.times(225), out _) || stateTime > 0.6f)
            {
                character.changeState(new ViralSigmaIdle(), true);
                return;
            }

            clampViralSigmaPos();
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.frameSpeed = 2f;
            character.xDir = tackleDir.x != 0 ? MathF.Sign(tackleDir.x) : character.lastViralSigmaXDir;
            if (tackleDir.isZero())
            {
                tackleDir = new Point(character.lastViralSigmaXDir, 0);
            }
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.viralSigmaAngle = 0;
            character.frameSpeed = 1;
        }
    }

    public class ViralSigmaBeamState : CharState
    {
        ViralSigmaBeamProj proj;
        public ViralSigmaBeamState() : base("viral_shoot")
        {
            immuneToWind = true;
        }

        public override void update()
        {
            base.update();

            var inputDir = player.input.getInputDir(player);
            //inputDir.y = 0;
            character.move(inputDir.times(125));
            clampViralSigmaPos();

            character.viralSigmaBeamLength -= Global.spf * 0.1f;
            if (character.viralSigmaBeamLength <= 0)
            {
                character.viralSigmaBeamLength = 0;
                character.changeState(new ViralSigmaIdle(), true);
                return;
            }

            if ((stateTime > 0.2f && !player.input.isHeld(Control.Special1, player)))
            {
                character.changeState(new ViralSigmaIdle(), true);
            }
            proj?.changePos(character.getFirstPOIOrDefault());
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.frameSpeed = 2f;
            proj = new ViralSigmaBeamProj(new ViralSigmaBeamWeapon(), character.getFirstPOIOrDefault(), player, player.getNextActorNetId(), rpc: true);
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.angle = 0;
            character.frameSpeed = 1;
            proj.destroySelf();
        }
    }

    public class ViralSigmaBeamProj : Projectile
    {
        float soundTime;
        public float bottomY;
        float explosionTime;
        public ViralSigmaBeamProj(Weapon weapon, Point pos, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, 1, 0, 1, player, "empty", Global.halfFlinch, 0.15f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.Sigma2ViralBeam;
            destroyOnHit = false;
            reflectable = false;
            shouldShieldBlock = false;
            shouldVortexSuck = false;
            bottomY = pos.y;
            getBottomY();

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public void getBottomY()
        {
            if (ownedByLocalPlayer)
            {
                float beamLength = owner?.character == null ? 150 : owner.character.viralSigmaBeamLength * 150;
                var hit = Global.level.raycast(pos.addxy(0, -10), pos.addxy(0, beamLength), new List<Type>() { typeof(Wall) });
                bottomY = hit?.hitData?.hitPoint?.y ?? pos.y + beamLength;
            }

            if (globalCollider == null)
            {
                globalCollider = new Collider(getPoints(), true, null, false, false, 0, new Point(0, 0));
            }
            else
            {
                changeGlobalCollider(getPoints());
            }
        }

        public List<Point> getPoints()
        {
            return new List<Point>
            {
                pos.addxy(-9, 0),
                pos.addxy(8, 0),
                pos.addxy(8, bottomY - pos.y),
                pos.addxy(-9, bottomY - pos.y),
            };
        }

        public override void update()
        {
            base.update();
            getBottomY();
            Helpers.decrementTime(ref soundTime);
            Helpers.decrementTime(ref explosionTime);
            if (soundTime == 0)
            {
                playSound("viralSigmaBeam");
                soundTime = 0.164f;
            }
            if (explosionTime == 0)
            {
                playSound("explosion");
                new Anim(new Point(pos.x, bottomY).addRand(10, 10), "explosion", 1, null, true);
                explosionTime = 0.15f;
            }
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            int r = 200 + (int)(MathF.Sin(Global.time * 25) * 55);
            Color color = new Color((byte)r, 0, 0, 128);
            DrawWrappers.DrawRect(pos.x - 9, pos.y, pos.x + 8, bottomY, true, color, 1, ZIndex.Character);
        }
    }

    public class ViralSigmaRevive : CharState
    {
        int state = 0;
        public ExplodeDieEffect explodeDieEffect;
        public ViralSigmaRevive(ExplodeDieEffect explodeDieEffect) : base("viral_enter")
        {
            this.explodeDieEffect = explodeDieEffect;
        }

        public override void update()
        {
            base.update();

            if (state == 0)
            {
                if (explodeDieEffect == null || explodeDieEffect.destroyed)
                {
                    state = 1;
                    character.frameSpeed = 1;
                    character.addMusicSource("viralsigma", character.pos, true);
                    RPC.actorToggle.sendRpc(character.netId, RPCActorToggleType.AddViralSigmaMusicSource);
                    character.visible = true;
                }
            }
            else if (state == 1)
            {
                if (Global.debug && player.input.isPressed(Control.Special1, player))
                {
                    character.xScale = 1;
                    character.yScale = 1;
                    state = 2;
                    stateTime = 0;
                    character.frameSpeed = 0;
                    character.frameIndex = character.sprite.frames.Count - 1;
                    return;
                }

                if (character.xScale < 1) character.xScale += Global.spf * 0.5f;
                else character.xScale = 1;
                character.yScale = character.xScale;
                if (character.loopCount > 1)
                {
                    if (character.frameIndex > character.sprite.frames.Count * 0.5f)
                    {
                        character.frameSpeed = Helpers.lerp(character.frameSpeed, 0, Global.spf * 2);
                    }
                    if (character.frameIndex >= character.sprite.frames.Count - 1)
                    {
                        state = 2;
                        stateTime = 0;
                        character.frameSpeed = 0;
                        character.frameIndex = character.sprite.frames.Count - 1;
                    }
                }
            }
            else if (state == 2)
            {
                if (stateTime > 0f)
                {
                    player.health = 1;
                    character.addHealth(player.maxHealth);
                    state = 3;
                }
            }
            else if (state == 3)
            {
                if (Global.debug && player.input.isPressed(Control.Special1, player))
                {
                    player.health = player.maxHealth;
                }

                if (player.health >= player.maxHealth)
                {
                    player.weapons.Add(new MechaniloidWeapon(player, MechaniloidType.Tank));
                    player.weapons.Add(new MechaniloidWeapon(player, MechaniloidType.Hopper));
                    player.weapons.Add(new MechaniloidWeapon(player, MechaniloidType.Bird));

                    player.weaponSlot = 0;

                    character.frameSpeed = 1;

                    character.invulnTime = 0.5f;
                    character.useGravity = false;
                    character.angle = 0;
                    character.stopMoving();
                    character.grounded = false;
                    character.canBeGrounded = false;

                    character.changeState(new ViralSigmaIdle(), true);
                }
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.syncScale = true;
            character.isHyperSigma = true;
            character.frameSpeed = 0;
            character.frameIndex = 0;
            character.xScale = 0;
            character.yScale = 0;
            character.incPos(new Point(0, -33));
            character.immuneToKnockback = true;
            player.sigmaAmmo = player.sigmaMaxAmmo;
        }
    }
}
