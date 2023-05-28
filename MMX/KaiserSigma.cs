using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public partial class Character
    {
        public bool kaiserWinTauntOnce;
        public float kaiserMissileShootTime;
        public Anim kaiserExhaustL;
        public Anim kaiserExhaustR;
        public float kaiserHoverCooldown;
        public float kaiserLeftMineShootTime;
        public float kaiserRightMineShootTime;
        public int leftMineMod;
        public int rightMineMod;

        public Collider getKaiserSigmaGlobalCollider()
        {
            if (player.isKaiserViralSigma())
            {
                if (sprite.name == "sigma3_kaiser_virus_return")
                {
                    return null;
                }
                var rect2 = new Rect(0, 0, 20, 32);
                return new Collider(rect2.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
            }
            else
            {
                var rect2 = new Rect(0, 0, 60, 110);
                return new Collider(rect2.getPoints(), false, this, false, false, HitboxFlag.None, new Point(0, 0));
            }
        }

        public void changeToKaiserIdleOrFall()
        {
            changeState(new KaiserSigmaIdleState(), true);
        }

        public bool isKaiserSigmaGrounded()
        {
            return charState is not KaiserSigmaHoverState && charState is not KaiserSigmaFallState;
        }

        public bool canKaiserSpawn(out Point spawnPos)
        {
            // Get ground snapping pos
            Point groundPos;
            var groundHit = getGroundHit(Global.halfScreenH);
            if (groundHit != null)
            {
                groundPos = groundHit.Value;
            }
            else
            {
                spawnPos = Point.zero;
                return false;
            }

            // Check if ample space to revive in
            int w = 60;
            int h = 110;
            var rect = new Rect(new Point(groundPos.x - w / 2, groundPos.y - h), new Point(groundPos.x + w / 2, groundPos.y - 1));

            // DrawWrappers.DrawRect(rect.x1, rect.y1, rect.x2, rect.y2, true, new Color(255, 0, 0, 64), 1, ZIndex.HUD);

            List<CollideData> hits = null;
            hits = Global.level.checkCollisionsShape(rect.getShape(), null);
            if (hits.Any(h => h.gameObject is Wall))
            {
                var hitPoints = new List<Point>();
                foreach (var hit in hits)
                {
                    if (hit?.hitData?.hitPoints == null) continue;
                    hitPoints.AddRange(hit.hitData.hitPoints.Where(p => p.y > pos.y - 30));
                }
                if (hitPoints.Count > 0)
                {
                    var bestHitPoint = hitPoints.OrderBy(p => p.y).First();
                    float savedH = rect.h();
                    rect.y2 = bestHitPoint.y - 1;
                    rect.y1 = rect.y2 - savedH;
                    groundPos.y = bestHitPoint.y;
                }

                // DrawWrappers.DrawRect(rect.x1, rect.y1, rect.x2, rect.y2, true, new Color(0, 0, 255, 64), 1, ZIndex.HUD);
            }

            hits = Global.level.checkCollisionsShape(rect.getShape(), null);
            if (hits.Any(h => h.gameObject is Wall))
            {
                spawnPos = Point.zero;
                return false;
            }

            foreach (var player in Global.level.players)
            {
                Character otherChar = player?.character;
                if (otherChar != null && otherChar != this && otherChar.isHyperSigmaBS.getValue() == true && otherChar.player.isSigma1Or3() && pos.distanceTo(otherChar.pos) < Global.screenW)
                {
                    spawnPos = Point.zero;
                    return false;
                }
            }

            spawnPos = groundPos;
            return true;
        }
    }

    public class KaiserSigmaBaseState : CharState
    {
        public bool canShootBallistics;
        public bool showExhaust;
        public int exhaustMoveDir;
        public KaiserSigmaBaseState(string sprite) : base(sprite)
        {
            immuneToWind = true;
        }

        public override void update()
        {
            base.update();
            character.stopMoving();

            if (this is not KaiserSigmaHoverState && this is not KaiserSigmaFallState && character.kaiserHoverCooldown == 0)
            {
                Helpers.decrementTime(ref character.kaiserHoverTime);
            }

            Helpers.decrementTime(ref character.kaiserMissileShootTime);
            Helpers.decrementTime(ref character.kaiserLeftMineShootTime);
            Helpers.decrementTime(ref character.kaiserRightMineShootTime);
            Helpers.decrementTime(ref character.kaiserHoverCooldown);

            if (!isKaiserSigmaTouchingGround())
            {
                if (character.charState is KaiserSigmaIdleState || character.charState is KaiserSigmaBeamState)
                {
                    character.changeState(new KaiserSigmaHoverState(), true);
                }
            }

            if (Global.level.gameMode.isOver && Global.level.gameMode.playerWon(player))
            {
                if (!character.kaiserWinTauntOnce && character.charState is KaiserSigmaIdleState)
                {
                    character.kaiserWinTauntOnce = true;
                    character.changeState(new KaiserSigmaTauntState(), true);
                }
            }

            if (showExhaust)
            {
                character.kaiserExhaustL.visible = true;
                character.kaiserExhaustR.visible = true;
                character.kaiserExhaustL.changePos(character.getFirstPOIOrDefault("exhaustL"));
                character.kaiserExhaustR.changePos(character.getFirstPOIOrDefault("exhaustR"));
                if (exhaustMoveDir != 0)
                {
                    character.kaiserExhaustL.changeSpriteIfDifferent("sigma3_kaiser_exhaust2", true);
                    character.kaiserExhaustR.changeSpriteIfDifferent("sigma3_kaiser_exhaust2", true);
                    character.kaiserExhaustL.xDir = -exhaustMoveDir;
                    character.kaiserExhaustR.xDir = -exhaustMoveDir;
                }
                else
                {
                    character.kaiserExhaustL.changeSpriteIfDifferent("sigma3_kaiser_exhaust", true);
                    character.kaiserExhaustR.changeSpriteIfDifferent("sigma3_kaiser_exhaust", true);
                }
            }
            else
            {
                character.kaiserExhaustL.visible = false;
                character.kaiserExhaustR.visible = false;
            }

            if (canShootBallistics)
            {
                ballisticAttackLogic();
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
        }

        public void tauntLogic()
        {
            if (!Global.level.gameMode.isOver && player.input.isPressed(Control.Taunt, player))
            {
                character.changeState(new ViralSigmaTaunt(false), true);
            }
        }

        public bool isKaiserSigmaTouchingGround()
        {
            return character.checkCollision(0, 5) != null;
        }

        public void ballisticAttackLogic()
        {
            bool weaponL = player.input.isPressed(Control.WeaponLeft, player);
            bool weaponR = player.input.isPressed(Control.WeaponRight, player);
            if (player.input.isPressed(Control.Special1, player) && character.isKaiserSigmaGrounded())
            {
                if (character.kaiserMissileShootTime == 0)
                {
                    character.kaiserMissileShootTime = 2f;
                    var posL = character.getFirstPOIOrDefault("missileL");
                    var posR = character.getFirstPOIOrDefault("missileR");

                    Global.level.delayedActions.Add(new DelayedAction(() => { new KaiserSigmaMissileProj(new KaiserMissileWeapon(), posL.addxy(-8 * character.xDir, 0), player, player.getNextActorNetId(), rpc: true); }, 0f));
                    Global.level.delayedActions.Add(new DelayedAction(() => { new KaiserSigmaMissileProj(new KaiserMissileWeapon(), posL, player, player.getNextActorNetId(), rpc: true); }, 0.15f));
                    Global.level.delayedActions.Add(new DelayedAction(() => { new KaiserSigmaMissileProj(new KaiserMissileWeapon(), posR, player, player.getNextActorNetId(), rpc: true); }, 0.3f));
                    Global.level.delayedActions.Add(new DelayedAction(() => { new KaiserSigmaMissileProj(new KaiserMissileWeapon(), posR.addxy(8 * character.xDir, 0), player, player.getNextActorNetId(), rpc: true); }, 0.45f));
                }
            }
            else if (weaponL || weaponR)
            {
                if ((weaponR && character.xDir == 1) || (weaponL && character.xDir == -1))
                {
                    if (character.kaiserRightMineShootTime == 0)
                    {
                        character.kaiserRightMineShootTime = 1;
                        if (character.rightMineMod % 2 == 0) new KaiserSigmaMineProj(new KaiserMineWeapon(), character.getFirstPOIOrDefault("mineR1"), character.xDir, 0, player, player.getNextActorNetId(), rpc: true);
                        else new KaiserSigmaMineProj(new KaiserMineWeapon(), character.getFirstPOIOrDefault("mineR2"), character.xDir, 1, player, player.getNextActorNetId(), rpc: true);
                        character.rightMineMod++;
                    }
                }
                else if ((weaponR && character.xDir == -1) || (weaponL && character.xDir == 1))
                {
                    if (character.kaiserRightMineShootTime == 0)
                    {
                        character.kaiserRightMineShootTime = 1;
                        if (character.leftMineMod % 2 == 0) new KaiserSigmaMineProj(new KaiserMineWeapon(), character.getFirstPOIOrDefault("mineL1"), -character.xDir, 0, player, player.getNextActorNetId(), rpc: true);
                        else new KaiserSigmaMineProj(new KaiserMineWeapon(), character.getFirstPOIOrDefault("mineL2"), -character.xDir, 1, player, player.getNextActorNetId(), rpc: true);
                        character.leftMineMod++;
                    }
                }
            }
        }
    }
    
    public class KaiserSigmaIdleState : KaiserSigmaBaseState
    {
        public KaiserSigmaIdleState() : base("kaiser_idle")
        {
            canShootBallistics = true;
            immuneToWind = true;
        }

        public override void update()
        {
            base.update();

            if (player.input.isPressed(Control.Shoot, player))
            {
                character.changeState(new KaiserSigmaBeamState(player.input.isHeld(Control.Down, player)));
            }
            else if (player.input.isHeld(Control.Up, player) || player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player))
            {
                if (character.kaiserHoverCooldown == 0 && character.kaiserHoverTime < character.kaiserMaxHoverTime - 0.25f)
                {
                    character.changeState(new KaiserSigmaHoverState(), true);
                    return;
                }
            }
            else if (player.input.isPressed(Control.Dash, player))
            {
                if (UpgradeMenu.subtankDelay > 0)
                {
                    Global.level.gameMode.setHUDErrorMessage(player, "Cannot become Virus in battle");
                }
                else
                {
                    character.changeState(new KaiserSigmaVirusState(), true);
                }
                return;
            }
            else if (player.input.isPressed(Control.Taunt, player))
            {
                character.changeState(new KaiserSigmaTauntState(), true);
                return;
            }

            ballisticAttackLogic();
        }
    }

    public class KaiserSigmaTauntState : KaiserSigmaBaseState
    {
        public KaiserSigmaTauntState() : base("kaiser_taunt")
        {
            immuneToWind = true;
        }

        public override void update()
        {
            base.update();

            if (character.isAnimOver())
            {
                character.changeToKaiserIdleOrFall();
            }
        }
    }

    public class KaiserSigmaHoverState : KaiserSigmaBaseState
    {
        public KaiserSigmaHoverState() : base("kaiser_hover")
        {
            immuneToWind = true;
            showExhaust = true;
            canShootBallistics = true;
        }

        public override void update()
        {
            base.update();

            if (player.input.isPressed(Control.Jump, player) || character.kaiserHoverTime > character.kaiserMaxHoverTime)
            {
                character.changeState(new KaiserSigmaFallState(), true);
                return;
            }

            var inputDir = player.input.getInputDir(player);
            var moveAmount = inputDir.times(75);
            moveAmount.y *= 0.5f;

            character.kaiserHoverTime += (Global.spf * 0.5f);
            if (moveAmount.y < 0)
            {
                character.kaiserHoverTime += (Global.spf * 1.5f);
            }
            if (character.kaiserHoverTime > character.kaiserMaxHoverTime)
            {
                moveAmount.y = 0;
            }

            exhaustMoveDir = 0;
            if (!moveAmount.isZero())
            {
                if (moveAmount.y > 0 && character.checkCollision(0, moveAmount.y * Global.spf) != null)
                {
                    character.changeToKaiserIdleOrFall();
                    character.playSound("crash", sendRpc: true);
                    character.shakeCamera(sendRpc: true);
                    return;
                }
                character.move(new Point(moveAmount.x, moveAmount.y));
                if (moveAmount.x != 0)
                {
                    exhaustMoveDir = Math.Sign(moveAmount.x);
                    character.xDir = exhaustMoveDir;
                }
            }
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.kaiserHoverCooldown = 0.75f;
        }
    }

    public class KaiserSigmaFallState : KaiserSigmaBaseState
    {
        public float velY;
        public KaiserSigmaFallState() : base("kaiser_fall")
        {
            immuneToWind = true;
        }

        public override void update()
        {
            base.update();

            character.addGravity(ref velY);

            var moveAmount = new Point(0, velY);

            /*
            if (!character.tryMove(moveAmount, out var hitData))
            {
                character.playSound("crash", sendRpc: true);
                character.shakeCamera(sendRpc: true);
                float snapY = hitData.getHitPointSafe().y;
                if (snapY > character.pos.y)
                {
                    character.changePos(new Point(character.pos.x, snapY));
                }
                character.changeToKaiserIdleOrFall();
            }
            */

            if (!moveAmount.isZero())
            {
                if (moveAmount.y > 0 && character.checkCollision(0, moveAmount.y * Global.spf) != null)
                {
                    character.changeToKaiserIdleOrFall();
                    character.playSound("crash", sendRpc: true);
                    character.shakeCamera(sendRpc: true);
                    return;
                }
                character.move(new Point(moveAmount.x, moveAmount.y));
            }
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.kaiserHoverCooldown = 0.75f;
        }
    }

    public class KaiserSigmaVirusState : CharState
    {
        public Anim kaiserShell;
        public Anim relocatedKaiserShell;
        public Anim viralSigmaHeadReturn;

        public bool isLerpingBack;
        Point kaiserSigmaDestPos;
        public bool startAnimOver;
        public bool isRelocating;
        public int origXDir;

        public KaiserSigmaVirusState() : base("kaiser_virus")
        {
            immuneToWind = true;
        }

        public void lerpBack(Point destPos, bool isRelocating)
        {
            if (!isLerpingBack)
            {
                this.isRelocating = isRelocating;
                isLerpingBack = true;
                character.changeSpriteFromName("kaiser_virus_return", true);
                kaiserSigmaDestPos = destPos;
                if (!isRelocating)
                {
                    character.xDir = origXDir;
                }
            }
        }

        public override void update()
        {
            base.update();

            stateTime += Global.spf;
            character.stopMoving();

            if (!startAnimOver)
            {
                character.xScale += Global.spf * 2.5f;
                character.yScale = character.xScale;
                if (character.xScale > 1)
                {
                    startAnimOver = true;
                    character.xScale = 1;
                    character.yScale = 1;
                }
                return;
            }

            if (player.input.isPressed(Control.Dash, player))
            {
                //lerpBack(kaiserShell.pos, false);
            }

            if (isLerpingBack)
            {
                if (viralSigmaHeadReturn == null)
                {
                    viralSigmaHeadReturn = new Anim(character.pos, "sigma3_kaiser_virus_return", character.xDir, player.getNextActorNetId(), false, sendRpc: true) { zIndex = character.zIndex };
                    character.visible = false;
                    character.changePos(kaiserSigmaDestPos);
                    character.changeSpriteFromName("kaiser_idle", true);
                }

                // Note: code to shrink is in Anim.cs

                if (isRelocating && relocatedKaiserShell == null)
                {
                    relocatedKaiserShell = new Anim(kaiserSigmaDestPos, "sigma3_kaiser_empty", character.xDir, player.getNextActorNetId(), false, sendRpc: true, fadeIn: true);
                }

                Point lerpDestPos = kaiserSigmaDestPos.addxy(12 * character.xDir, -94);
                viralSigmaHeadReturn.changePos(Point.lerp(viralSigmaHeadReturn.pos, lerpDestPos, Global.spf * 10));
                if (viralSigmaHeadReturn.pos.distanceTo(lerpDestPos) < 10 && viralSigmaHeadReturn.xScale == 0)
                {
                    character.changeToKaiserIdleOrFall();

                    character.destroyMusicSource();
                    character.addMusicSource("kaisersigma", character.getCenterPos(), true);
                    RPC.actorToggle.sendRpc(character.netId, RPCActorToggleType.AddKaiserSigmaMusicSource);

                    character.xScale = 1;
                    character.yScale = 1;
                }
                return;
            }

            var inputDir = player.input.getInputDir(player);
            var moveAmount = inputDir.times(100);
            character.move(moveAmount);
            character.turnToInput(player.input, player);

            clampViralSigmaPos();

            bool canSpawnAtPos = character.canKaiserSpawn(out var spawnPoint);
            if (player.input.isPressed(Control.Shoot, player) || player.input.isPressed(Control.Jump, player))
            {
                if (canSpawnAtPos)
                {
                    lerpBack(spawnPoint, true);
                    return;
                }
            }

            if (!canSpawnAtPos)
            {
                var redXPos = character.getCenterPos();
                DrawWrappers.DrawLine(redXPos.x - 10, redXPos.y - 10, redXPos.x + 10, redXPos.y + 10, Color.Red, 2, ZIndex.HUD);
                DrawWrappers.DrawLine(redXPos.x - 10, redXPos.y + 10, redXPos.x + 10, redXPos.y - 10, Color.Red, 2, ZIndex.HUD);
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            kaiserShell = new Anim(character.pos, "sigma3_kaiser_empty_fadeout", character.xDir, player.getNextActorNetId(), false, sendRpc: true) { zIndex = character.zIndex - 10 };

            character.changePos(character.pos.addxy(12 * character.xDir, -94));
            origXDir = character.xDir;

            character.xScale = 0;
            character.yScale = 0;

            RPC.actorToggle.sendRpc(character.netId, RPCActorToggleType.AddKaiserViralSigmaMusicSource);
            character.destroyMusicSource();
            character.addMusicSource("kaisersigmavirus", character.getCenterPos(), true);
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            character.visible = true;
            if (newState is Die && kaiserShell != null && !kaiserShell.destroyed)
            {
                var effectPos = kaiserShell.pos.addxy(0, -55);
                var ede = new ExplodeDieEffect(player, effectPos, effectPos, "empty", 1, character.zIndex, false, 60, 3, false);
                Global.level.addEffect(ede);
                var anim = new Anim(kaiserShell.pos, kaiserShell.sprite.name, kaiserShell.xDir, player.getNextActorNetId(), false, sendRpc: true);
                anim.ttl = 3;
                anim.blink = true;
            }
            kaiserShell?.destroySelf();
            relocatedKaiserShell?.destroySelf();
            viralSigmaHeadReturn?.destroySelf();
        }
    }

    public class KaiserSigmaBeamState : KaiserSigmaBaseState
    {
        int state = 0;
        float chargeTime;
        KaiserSigmaBeamProj proj;
        float randPartTime;
        bool isDown;
        SoundWrapper chargeSound;
        SoundWrapper beamSound;
        public KaiserSigmaBeamState(bool isDown) : base(isDown ? "kaiser_shoot" : "kaiser_shoot2")
        {
            immuneToWind = true;
            canShootBallistics = true;
            this.isDown = isDown;
        }

        public override void update()
        {
            base.update();

            if (state == 0)
            {
                if (chargeTime == 0)
                {
                    chargeSound = character.playSound("kaiserSigmaCharge", sendRpc: true);
                }
                chargeTime += Global.spf;
                Point shootPos = character.getFirstPOIOrDefault();

                randPartTime += Global.spf;
                if (randPartTime > 0.025f)
                {
                    randPartTime = 0;
                    var partSpawnAngle = Helpers.randomRange(0, 360);
                    float spawnRadius = 20;
                    float spawnSpeed = 150;
                    var partSpawnPos = shootPos.addxy(Helpers.cosd(partSpawnAngle) * spawnRadius, Helpers.sind(partSpawnAngle) * spawnRadius);
                    var partVel = partSpawnPos.directionToNorm(shootPos).times(spawnSpeed);
                    new Anim(partSpawnPos, "sigma3_kaiser_charge", 1, player.getNextActorNetId(), false, sendRpc: true)
                    {
                        vel = partVel,
                        ttl = ((spawnRadius - 2) / spawnSpeed),
                    };
                }

                if (chargeTime > 1f)
                {
                    state = 1;
                    proj = new KaiserSigmaBeamProj(new KaiserBeamWeapon(), shootPos, character.xDir, !isDown, player, player.getNextActorNetId(), rpc: true);
                    beamSound = character.playSound("kaiserSigmaBeam", sendRpc: true);
                }
            }
            else if (state == 1)
            {
                if (proj.destroyed)
                {
                    character.changeToKaiserIdleOrFall();
                }
            }
        }

        public override void onExit(CharState newState)
        {
            base.onExit(newState);
            proj?.destroySelf();
            if (chargeSound != null && !chargeSound.deleted)
            {
                chargeSound.sound.Stop();
                RPC.stopSound.sendRpc(chargeSound.soundBuffer.soundKey, character.netId);
            }
            if (beamSound != null && !beamSound.deleted)
            {
                beamSound.sound.Stop();
                RPC.stopSound.sendRpc(beamSound.soundBuffer.soundKey, character.netId);
            }
        }
    }

    public class KaiserBeamWeapon : Weapon
    {
        public KaiserBeamWeapon() : base()
        {
            weaponSlotIndex = 116;
            index = (int)WeaponIds.Sigma3KaiserBeam;
            killFeedIndex = 166;
        }
    }

    public class KaiserSigmaBeamProj : Projectile
    {
        public float beamAngle;
        public float beamWidth;
        const float beamLen = 150;
        const float maxBeamTime = 2;
        public KaiserSigmaBeamProj(Weapon weapon, Point pos, int xDir, bool isUp, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, 1, 0, 1, player, "empty", 0, 0.15f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.Sigma3KaiserBeam;
            setIndestructableProperties();

            if (ownedByLocalPlayer)
            {
                if (xDir == 1 && !isUp) beamAngle = 45;
                if (xDir == -1 && !isUp) beamAngle = 135;
                if (xDir == -1 && isUp) beamAngle = 225;
                if (xDir == 1 && isUp) beamAngle = 315;
            }

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void update()
        {
            base.update();

            if (globalCollider == null)
            {
                globalCollider = new Collider(getPoints(), true, null, false, false, 0, new Point(0, 0));
            }
            else
            {
                changeGlobalCollider(getPoints());
            }

            if (!ownedByLocalPlayer) return;

            if (time < 1)
            {
                beamWidth += Global.spf * 20;
            }
            else if (time >= 1 && time < 1 + maxBeamTime)
            {
                beamWidth = 20;
                if (owner.input.isPressed(Control.Shoot, owner))
                {
                    time = 1 + maxBeamTime;
                }
            }
            else if (time >= 1 + maxBeamTime && time < 2 + maxBeamTime)
            {
                beamWidth -= Global.spf * 20;
            }
            else if (time >= 2 + maxBeamTime)
            {
                destroySelf();
            }
        }

        public List<Point> getPoints()
        {
            float ang1 = beamAngle - beamWidth;
            float ang2 = beamAngle + beamWidth;
            Point pos1 = new Point(beamLen * Helpers.cosd(ang1), beamLen * Helpers.sind(ang1));
            Point pos2 = new Point(beamLen * Helpers.cosd(ang2), beamLen * Helpers.sind(ang2));

            var points = new List<Point>
            {
                pos,
                pos.add(pos1),
                pos.add(pos2),
            };
            return points;
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            Color color = new Color(132, 132, 231);
            DrawWrappers.DrawPolygon(getPoints(), color, true, ZIndex.Character);
        }
    }

    public class KaiserMissileWeapon : Weapon
    {
        public KaiserMissileWeapon() : base()
        {
            weaponSlotIndex = 114;
            index = (int)WeaponIds.Sigma3KaiserMissile;
            killFeedIndex = 164;
        }
    }

    public class KaiserSigmaMissileProj : Projectile
    {
        public Actor target;
        public float smokeTime = 0;
        public float maxSpeed = 150;
        public float health = 2;
        public KaiserSigmaMissileProj(Weapon weapon, Point pos, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, 1, 0, 2, player, "sigma3_kaiser_missile", Global.defFlinch, 0f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.Sigma3KaiserMissile;
            maxTime = 2f;
            fadeOnAutoDestroy = true;
            reflectable2 = true;
            netcodeOverride = NetcodeModel.FavorDefender;

            fadeSprite = "explosion";
            fadeSound = "explosion";
            angle = 270;

            if (rpc)
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
                if (!Global.level.gameObjects.Contains(target))
                {
                    target = null;
                }

                if (target != null)
                {
                    if (time < 3f)
                    {
                        var dTo = pos.directionTo(target.getCenterPos()).normalize();
                        var destAngle = MathF.Atan2(dTo.y, dTo.x) * 180 / MathF.PI;
                        destAngle = Helpers.to360(destAngle);
                        angle = Helpers.lerpAngle((float)angle, destAngle, Global.spf * 3);
                    }
                }
                if (time >= 0.1 && target == null)
                {
                    target = Global.level.getClosestTarget(pos, damager.owner.alliance, false, aMaxDist: Global.screenW);
                }

                vel.x = Helpers.cosd((float)angle) * maxSpeed;
                vel.y = Helpers.sind((float)angle) * maxSpeed;
            }

            smokeTime += Global.spf;
            if (smokeTime > 0.2)
            {
                smokeTime = 0;
                new Anim(pos, "torpedo_smoke", 1, null, true);
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
        public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) { return damager.owner.alliance != damagerAlliance; }
        public bool isInvincible(Player attacker, int? projId) { return false; }
        public bool canBeHealed(int healerAlliance) { return false; }
        public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) { }
    }

    public class KaiserMineWeapon : Weapon
    {
        public KaiserMineWeapon() : base()
        {
            weaponSlotIndex = 115;
            index = (int)WeaponIds.Sigma3KaiserMine;
            killFeedIndex = 165;
        }
    }

    public class KaiserSigmaMineProj : Projectile, IDamagable
    {
        bool firstHit;
        float hitWallCooldown;
        float health = 3;
        int type;
        bool startWall;
        public KaiserSigmaMineProj(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false) :
            base(weapon, pos, xDir, 100, 4, player, "sigma3_kaiser_mine", Global.defFlinch, 0.15f, netProjId, player.ownedByLocalPlayer)
        {
            projId = (int)ProjIds.Sigma3KaiserMine;
            maxTime = 4f;
            this.type = type;
            fadeSprite = "explosion";
            fadeSound = "explosion";
            netcodeOverride = NetcodeModel.FavorDefender;

            if (type == 1)
            {
                vel.y = 100;
                vel = vel.normalize().times(speed);
            }

            if (rpc)
            {
                rpcCreate(pos, player, netProjId, xDir);
            }
        }

        public override void onStart()
        {
            base.onStart();
            if (Global.level.checkCollisionShape(collider.shape, null) != null)
            {
                startWall = true;
            }
        }

        public override void update()
        {
            base.update();
            updateProjectileCooldown();
            Helpers.decrementTime(ref hitWallCooldown);
            if (startWall)
            {
                if (Global.level.checkCollisionShape(collider.shape, null) == null)
                {
                    startWall = false;
                }
            }
        }

        public override void onHitWall(CollideData other)
        {
            base.onHitWall(other);
            if (!ownedByLocalPlayer) return;
            if (hitWallCooldown > 0) return;
            if (startWall) return;

            bool didHit = false;
            if (!firstHit && type == 0)
            {
                firstHit = true;
                vel.x *= -1;
                vel.y = -speed;
                vel = vel.normalize().times(speed);
                didHit = true;
            }
            else if (other.isSideWallHit())
            {
                vel.x *= -1;
                vel = vel.normalize().times(speed);
                didHit = true;
            }
            else if (other.isCeilingHit() || other.isGroundHit())
            {
                vel.y *= -1;
                vel = vel.normalize().times(speed);
                didHit = true;
            }
            if (didHit)
            {
                //playSound("gbeetleProjBounce", sendRpc: true);
                hitWallCooldown = 0.1f;
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
        public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) { return damager.owner.alliance != damagerAlliance; }
        public bool isInvincible(Player attacker, int? projId) { return false; }
        public bool canBeHealed(int healerAlliance) { return false; }
        public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) { }
    }

    public class KaiserStompWeapon : Weapon
    {
        public KaiserStompWeapon(Player player) : base()
        {
            index = (int)WeaponIds.Sigma3KaiserStomp;
            killFeedIndex = 163;
            damager = new Damager(player, 12, Global.defFlinch, 1);
        }
    }

    public class KaiserSigmaRevive : CharState
    {
        int state = 0;
        public ExplodeDieEffect explodeDieEffect;
        public Point spawnPoint;
        public KaiserSigmaRevive(ExplodeDieEffect explodeDieEffect, Point spawnPoint) : base("kaiser_enter")
        {
            this.explodeDieEffect = explodeDieEffect;
            this.spawnPoint = spawnPoint;
        }

        float alphaTime;
        public override void update()
        {
            base.update();

            if (state == 0)
            {
                if (explodeDieEffect == null || explodeDieEffect.destroyed)
                {
                    state = 1;
                    character.addMusicSource("kaisersigma", character.pos, true);
                    RPC.actorToggle.sendRpc(character.netId, RPCActorToggleType.AddKaiserSigmaMusicSource);
                    character.visible = true;
                    character.changePos(spawnPoint);
                }
            }
            else if (state == 1)
            {
                alphaTime += Global.spf;
                if (alphaTime >= 0.2) character.alpha = 0.2f;
                if (alphaTime >= 0.4) character.alpha = 0.4f;
                if (alphaTime >= 0.6) character.alpha = 0.6f;
                if (alphaTime >= 0.8) character.alpha = 0.8f;
                if (alphaTime >= 1) character.alpha = 1f;
                if (character.alpha >= 1)
                {
                    character.alpha = 1;
                    character.frameSpeed = 1;
                    state = 2;
                }
            }
            else if (state == 2)
            {
                if (Global.debug && player.input.isPressed(Control.Special1, player))
                {
                    character.frameIndex = character.sprite.frames.Count - 1;
                }

                if (character.isAnimOver())
                {
                    state = 3;
                }
            }
            else if (state == 3)
            {
                if (stateTime > 0.5f)
                {
                    player.health = 1;
                    character.addHealth(player.maxHealth);
                    state = 4;
                }
            }
            else if (state == 4)
            {
                if (Global.debug && player.input.isPressed(Control.Special1, player))
                {
                    player.health = player.maxHealth;
                }

                if (player.health >= player.maxHealth)
                {
                    character.invulnTime = 0.5f;
                    character.useGravity = false;
                    character.stopMoving();
                    character.grounded = false;
                    character.canBeGrounded = false;

                    character.changeState(new KaiserSigmaIdleState(), true);
                }
            }
        }

        public override void onEnter(CharState oldState)
        {
            base.onEnter(oldState);
            character.syncScale = true;
            character.isHyperSigma = true;
            character.frameIndex = 0;
            character.frameSpeed = 0;
            character.immuneToKnockback = true;
            character.alpha = 0;
            player.sigmaAmmo = player.sigmaMaxAmmo;
            if (character.kaiserExhaustL == null)
            {
                character.kaiserExhaustL = new Anim(character.pos, "sigma3_kaiser_exhaust", character.xDir, player.getNextActorNetId(), false, sendRpc: true) { visible = false };
            }
            if (character.kaiserExhaustR == null)
            {
                character.kaiserExhaustR = new Anim(character.pos, "sigma3_kaiser_exhaust", character.xDir, player.getNextActorNetId(), false, sendRpc: true) { visible = false };
            }
        }
    }
}
