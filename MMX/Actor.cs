using SFML.Audio;
using SFML.Graphics;
using SFML.Graphics.Glsl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MMXOnline
{
    public partial class Actor : GameObject
    {
        public Sprite sprite; //Current sprite

        public int frameIndex { get { return sprite.frameIndex; } set { if (sprite == null) { return; } sprite.frameIndex = value; } }
        public float frameSpeed { get { return sprite.frameSpeed; } set { if (sprite == null) { return; } sprite.frameSpeed = value; } }
        public float frameTime { get { return sprite.frameTime; } set { if (sprite == null) { return; } sprite.frameTime = value; } }
        public float animTime { get { return sprite.animTime; } set { if (sprite == null) { return; } sprite.animTime = value; } }
        public int loopCount { get { return sprite.loopCount; } }
        public void setFrameIndexSafe(int newFrameIndex)
        {
            if (sprite == null) return;
            if (sprite.frames.InRange(newFrameIndex)) sprite.frameIndex = newFrameIndex;
        }
        
        public bool useFrameProjs;
        public Dictionary<string, List<Projectile>> spriteFrameToProjs = new Dictionary<string, List<Projectile>>();
        public List<Projectile> globalProjs = new List<Projectile>();
        public Dictionary<string, string> spriteFrameToSounds = new Dictionary<string, string>();

        public int xDir; //-1 or 1
        public int yDir;
        public Point pos; //Current location
        public Point prevPos;
        public Point deltaPos;
        public Point vel;
        public float xPushVel;
        public float xIceVel;
        public float xSwingVel;
        public float landingVelY;
        public bool immuneToKnockback;
        public bool isPlatform;

        public Dictionary<int, SoundWrapper> netSounds = new Dictionary<int, SoundWrapper>();
        public string startSound;
        public bool isStatic;
        public bool startMethodCalled;
        public float? _angle;
        public bool customAngleRendering;
        public bool useGravity;
        public bool gravityWellable { get { return this is Character || this is RideArmor || this is Maverick || this is RideChaser; } }
        public float gravityWellTime;
        public bool canBeGrounded = true;
        public bool grounded;
        public bool groundedIce;
        public string name { get; set; }
        public Dictionary<RenderEffectType, RenderEffect> renderEffects = new Dictionary<RenderEffectType, RenderEffect>();
        public long zIndex;
        public bool visible = true;
        public bool timeSlow;
        public bool destroyed;
        public ShaderWrapper genericShader;
        public virtual List<ShaderWrapper> getShaders() { return genericShader != null ? new List<ShaderWrapper> { genericShader } : null; }
        public float alpha = 1;
        public float xScale = 1;
        public float yScale = 1;
        public float gravityModifier = 1;
        public bool reversedGravity;
        public float gravityWellModifier = 1;
        public Dictionary<string, float> projectileCooldown { get; set; } = new Dictionary<string, float>();
        public Dictionary<int, float> flinchCooldown { get; set; } = new Dictionary<int, float>();
        public MusicWrapper musicSource;
        public bool checkLadderDown = false;
        public List<DamageText> damageTexts = new List<DamageText>();
        public ShaderWrapper invisibleShader;
        public List<DamageEvent> damageHistory = new List<DamageEvent>();
        public NetcodeModel? netcodeOverride;

        public bool ownedByLocalPlayer;
        public ushort? netId;

        public float? netXPos;
        public float? netYPos;
        public Point netIncPos;
        public int? netSpriteIndex;
        public int? netFrameIndex;
        public int? netXDir;
        public int? netYDir;
        public float? netAngle;
        public bool stopSyncingNetPos;
        public bool syncScale;

        private Point lastPos;
        private int lastSpriteIndex;
        private int lastFrameIndex;
        private int lastXDir;
        private int lastYDir;
        private float? lastAngle;
        public float lastNetUpdate;

        public NetActorCreateId netActorCreateId;
        public Player netOwner;
        float createRpcTime;

        public bool splashable;
        private Anim _waterWade;
        public Anim waterWade 
        { 
            get 
            {
                if (_waterWade == null)
                {
                    _waterWade = new Anim(pos, "wade", 1, null, false);
                }
                return _waterWade;
            }
        }

        public float lastWaterY;
        public bool isUnderwater()
        {
            float colliderHeight;
            if (globalCollider == null) colliderHeight = 10;
            else colliderHeight = globalCollider.shape.maxY - globalCollider.shape.minY;

            // May need a new overridable method to get "visual" height for situations like these
            if (sprite?.name?.Contains("sigma2_viral_") == true)
            {
                colliderHeight = 50;
            }

            foreach (var waterRect in Global.level.waterRects)
            {
                if (pos.x > waterRect.x1 && pos.x < waterRect.x2 && pos.y - colliderHeight > waterRect.y1 && pos.y < waterRect.y2)
                {
                    lastWaterY = waterRect.y1;
                    return true;
                }
            }
            return false;
            //if (Global.level.levelData.waterY == null) return false;
            //if (Global.level.levelData.name == "forest2" && pos.x > 1415 && pos.x < 1888 && pos.y < 527) return false;
        }

        public bool isWading()
        {
            foreach (var waterRect in Global.level.waterRects)
            {
                if (pos.x > waterRect.x1 && pos.x < waterRect.x2 && pos.y > waterRect.y1 && pos.y < waterRect.y2)
                {
                    lastWaterY = waterRect.y1;
                    return true;
                }
            }
            return false;
        }

        public float underwaterTime;
        public float bubbleTime;
        public float bigBubbleTime;
        public float waterTime;

        public Actor(string spriteName, Point pos, ushort? netId, bool ownedByLocalPlayer, bool dontAddToLevel)
        {
            this.pos = pos;
            prevPos = pos;

            if (Global.debug && Global.serverClient != null && netId != null && Global.level.getActorByNetId(netId.Value) != null)
            {
                string netIdDump = Global.level.getNetIdDump();
                Helpers.WriteToFile("netIdDump.txt", netIdDump);
                //Global.logToConsole("The netId " + netId.ToString() + " (sprite " + spriteName + " ) was already used", showConsole: true);
                throw new Exception("The netId " + netId.ToString() + " (sprite " + spriteName + " ) was already used.");
            }

            this.netId = netId;
            this.ownedByLocalPlayer = ownedByLocalPlayer;
            vel = new Point(0, 0);
            useGravity = true;
            frameIndex = 0;
            frameSpeed = 1;
            frameTime = 0;
            name = "";
            xDir = 1;
            yDir = 1;
            grounded = false;
            zIndex = ++Global.level.autoIncActorZIndex;
            changeSprite(spriteName, true);
            lastNetUpdate = Global.time;

            if (!dontAddToLevel)
            {
                Global.level.addGameObject(this);
            }

            if (isWading() || isUnderwater())
            {
                waterTime = 10;
                underwaterTime = 10;
            }
        }

        public void createActorRpc(int playerId)
        {
            if (netId == null) return;
            if (!ownedByLocalPlayer) return;

            byte[] xBytes = BitConverter.GetBytes(pos.x);
            byte[] yBytes = BitConverter.GetBytes(pos.y);
            byte[] netProjIdByte = BitConverter.GetBytes(netId.Value);

            var bytes = new List<byte>()
                {
                    (byte)netActorCreateId,
                    xBytes[0], xBytes[1], xBytes[2], xBytes[3],
                    yBytes[0], yBytes[1], yBytes[2], yBytes[3],
                    (byte)playerId,
                    netProjIdByte[0], netProjIdByte[1],
                    (byte)(xDir + 128)
                };

            Global.serverClient?.rpc(RPC.createActor, bytes.ToArray());
        }

        public void changeSpriteIfDifferent(string spriteName, bool resetFrame)
        {
            if (sprite?.name == spriteName) return;
            changeSprite(spriteName, resetFrame);
        }

        public virtual void changeSprite(string spriteName, bool resetFrame)
        {
            string oldSpriteName = sprite?.name;

            if (spriteName == null) return;

            if (sprite != null)
            {
                if (sprite.name == spriteName) return;
            }

            if (!Global.sprites.ContainsKey(spriteName)) return;

            if (sprite != null) Global.level.removeFromGridFast(this);

            int oldFrameIndex = sprite?.frameIndex ?? 0;
            float oldFrameTime = sprite?.frameTime ?? 0;
            float oldAnimTime = sprite?.animTime ?? 0;

            sprite = Global.sprites[spriteName].clone();

            changeGlobalColliderOnSpriteChange(spriteName);
            
            foreach (var hitbox in sprite.hitboxes)
            {
                hitbox.actor = this;
            }
            foreach (var frame in sprite.frames)
            {
                foreach (var hitbox in frame.hitboxes)
                {
                    hitbox.actor = this;
                }
            }

            if (resetFrame)
            {
                frameIndex = 0;
                frameTime = 0;
                animTime = 0;
            }
            else
            {
                frameIndex = oldFrameIndex;
                frameTime = oldFrameTime;
                animTime = oldAnimTime;
            }

            if (frameIndex >= sprite.frames.Count)
            {
                frameIndex = 0;
                frameTime = 0;
                animTime = 0;
            }

            Global.level.addGameObjectToGrid(this);

            if ((this is Character || this is Maverick) && spriteName != oldSpriteName)
            {
                if (spriteName.EndsWith("_warp_in") && !Global.level.mainPlayer.readyTextOver)
                {
                    Global.level.delayedActions.Add(new DelayedAction(() => 
                    {
                        playOverrideVoice(spriteName);
                    }, Player.maxReadyTime));
                }
                else if ((spriteName != "sigma_die" && spriteName != "sigma2_die" && spriteName != "sigma3_die") || (visible && (this as Character)?.isHyperSigmaBS?.getValue() != true))
                {
                    playOverrideVoice(spriteName);
                }
            }
        }

        public void playOverrideVoice(string spriteName)
        {
            Character chr = this as Character;
            int charNum = chr != null ? chr.player.charNum : 4;
            spriteName = spriteName.Replace("_na_", "_")
                                   .Replace("_bald_", "_")
                                   .Replace("_notail_", "_")
                                   .Replace("tongue2", "tongue")
                                   .Replace("tongue3", "tongue")
                                   .Replace("tongue4", "tongue")
                                   .Replace("tongue5", "tongue")
                                   .Replace("_bk", "")
                                   .Replace("_mc", "")
                                   .Replace("_rb", "")
                                   .Replace("_ag", "")
                                   .Replace("_mb", "");

            var matchingVoice = Helpers.getRandomMatchingVoice(Global.voiceBuffers, spriteName, charNum);

            // If vile mk2 and mk5 sounds were not found, use the vile ones
            if (matchingVoice == null && (spriteName.StartsWith("vilemk2_") || spriteName.StartsWith("vilemk5_")))
            {
                spriteName = spriteName.Replace("vilemk2_", "vile_").Replace("vilemk5_", "vile_");
                matchingVoice = Helpers.getRandomMatchingVoice(Global.voiceBuffers, spriteName, charNum);
            }

            if (matchingVoice != null)
            {
                playSound(matchingVoice);
            }
        }

        public float? angle
        {
            get
            {
                return _angle;
            }
            set
            {
                _angle = value;
                if (value == null) return;
                if (_angle < 0) _angle += 360;
                if (_angle > 360) _angle -= 360;
            }
        }

        public void setzIndex(long val)
        {
            this.zIndex = val;
        }

        public Frame currentFrame
        {
            get
            {
                return sprite?.getCurrentFrame();
            }
        }

        public float framePercent
        {
            get
            {
                float entireDuration = 0;
                foreach (var frame in sprite.frames)
                {
                    entireDuration += frame.duration;
                }
                return animTime / entireDuration;
            }
        }

        public virtual void onStart()
        {
            if (!string.IsNullOrEmpty(startSound))
            {
                playSound(startSound);
            }
        }

        public virtual void preUpdate()
        {
            collidedInFrame.Clear();
            deltaPos = pos.subtract(prevPos);
            prevPos = pos;

            if (useFrameProjs)
            {
                // Frame-based hitbox projectile section
                string spriteKey = null;
                if (sprite != null)
                {
                    spriteKey = sprite.name + "_" + sprite.frameIndex.ToString();
                    var hitboxes = sprite.getCurrentFrame().hitboxes.ToList();
                    hitboxes.AddRange(sprite.hitboxes);

                    if (spriteFrameToProjs.ContainsKey(spriteKey) && spriteFrameToProjs[spriteKey] != null)
                    {
                        foreach (var proj in spriteFrameToProjs[spriteKey])
                        {
                            proj.incPos(deltaPos);
                            updateProjFromHitbox(proj);
                        }
                    }
                    else if (hitboxes != null)
                    {
                        foreach (var hitbox in hitboxes)
                        {
                            var proj = getProjFromHitboxBase(hitbox);
                            if (proj != null)
                            {
                                if (!spriteFrameToProjs.ContainsKey(spriteKey) || spriteFrameToProjs[spriteKey] == null)
                                {
                                    spriteFrameToProjs[spriteKey] = new List<Projectile>();
                                }
                                spriteFrameToProjs[spriteKey].Add(proj);
                            }
                        }
                    }
                }

                foreach (var key in spriteFrameToProjs.Keys)
                {
                    if (key != spriteKey)
                    {
                        if (spriteFrameToProjs[key] != null)
                        {
                            foreach (var proj in spriteFrameToProjs[key])
                            {
                                //proj.destroyFrames = 2;
                                proj.destroySelf();
                            }
                        }
                        spriteFrameToProjs.Remove(key);
                    }
                }

                // Global hitbox projectile section
                foreach (var proj in globalProjs)
                {
                    proj.incPos(deltaPos);
                    updateProjFromHitbox(proj);
                }

                // Get misc. projectiles based on conditions (i.e. headbutt, awakened zero aura)
                var projToCreateDict = getGlobalProjs();

                // If the projectile id wasn't returned, remove it from current globalProj list.
                for (int i = globalProjs.Count - 1; i >= 0; i--)
                {
                    if (!projToCreateDict.ContainsKey(globalProjs[i].projId))
                    {
                        //globalProjs[i].destroyFrames = 2;
                        globalProjs[i].destroySelf();
                        globalProjs.RemoveAt(i);
                    }
                }

                // For all projectiles to create, add to the global proj list ONLY if the proj id doesn't already exist
                foreach (var kvp in projToCreateDict)
                {
                    var projIdToCreate = kvp.Key;
                    var projFunction = kvp.Value;
                    if (!globalProjs.Any(p => p.projId == projIdToCreate))
                    {
                        var newlyCreatedProj = projFunction();
                        globalProjs.Add(newlyCreatedProj);
                    }
                }
            }
        }

        public void addGravity(ref float yVar)
        {
            float maxVelY = Physics.maxFallSpeed;
            float gravity = Physics.gravity;

            if (isUnderwater())
            {
                maxVelY = Physics.maxUnderwaterFallSpeed;
                gravity *= 0.5f;
            }

            yVar += Global.spf * gravity;
            if (yVar > maxVelY) yVar = maxVelY;
        }

        public virtual void update()
        {
            if (immuneToKnockback)
            {
                stopMoving();
            }

            foreach (var key in netSounds.Keys.ToList())
            {
                if (!Global.sounds.Contains(netSounds[key]))
                {
                    netSounds.Remove(key);
                }
            }

            if (!startMethodCalled)
            {
                onStart();
                startMethodCalled = true;
            }

            if (ownedByLocalPlayer && netOwner != null)
            {
                createRpcTime += Global.spf;
                if (createRpcTime > 1)
                {
                    createRpcTime = 0;
                    createActorRpc(netOwner.id);
                }
            }

            var renderEffectsToRemove = new HashSet<RenderEffectType>();
            foreach (var kvp in renderEffects)
            {
                kvp.Value.time -= Global.spf;
                if (kvp.Value.time <= 0)
                {
                    renderEffectsToRemove.Add(kvp.Key);
                }
            }
            foreach (var renderEffect in renderEffectsToRemove)
            {
                renderEffects.Remove(renderEffect);
            }

            if (!ownedByLocalPlayer)
            {
                frameSpeed = 0;
                sprite.time += Global.spf;
            }

            if (ownedByLocalPlayer && sprite != null)
            {
                int oldFrameIndex = sprite.frameIndex;
                sprite?.update();

                if (sprite != null && sprite.frameIndex != oldFrameIndex)
                {
                    string spriteFrameKey = sprite.name + "/" + sprite.frameIndex.ToString(CultureInfo.InvariantCulture);
                    if (spriteFrameToSounds.ContainsKey(spriteFrameKey))
                    {
                        playSound(spriteFrameToSounds[spriteFrameKey], sendRpc: true);
                    }
                }
            }

            bool wading = isWading();
            bool underwater = isUnderwater();
            float terminalVelUp = Physics.maxFallSpeed;
            float terminalVelDown = Physics.maxFallSpeed;
            if (underwater) terminalVelDown = Physics.maxUnderwaterFallSpeed;

            var chr = this as Character;
            var ra = this as RideArmor;

            float grav = Global.level.gravity * gravityModifier * gravityWellModifier;
            if (ownedByLocalPlayer)
            {
                if (useGravity && !grounded)
                {
                    if (underwater) grav *= 0.5f;
                    if (this is Character)
                    {
                        int bubbleCount = (this as Character).chargedBubbles.Count;
                        float modifier = 1;
                        if (underwater)
                        {
                            modifier = 1 - (0.01f * bubbleCount);
                        }
                        else
                        {
                            modifier = 1 - (0.05f * bubbleCount);
                        }
                        grav *= modifier;
                    }
                    vel.y += grav * Global.spf;
                    if (vel.y > terminalVelDown)
                    {
                        vel.y = terminalVelDown;
                    }
                    else if (vel.y < -terminalVelUp)
                    {
                        vel.y = -terminalVelUp;
                    }
                }

                if (Math.Abs(xPushVel) > 5)
                {
                    xPushVel = Helpers.lerp(xPushVel, 0, Global.spf * 5);

                    var wall = Global.level.checkCollisionActor(this, xPushVel * Global.spf, 0);
                    if (wall != null && wall.gameObject is Wall)
                    {
                        xPushVel = 0;
                    }
                }
                else if (xPushVel != 0)
                {
                    xPushVel = 0;
                }

                if (Math.Abs(xSwingVel) > 0)
                {
                    if (chr != null)
                    {
                        if (chr.player.isX)
                        {
                            if (!chr.player.input.isHeld(Control.Dash, chr.player) || chr.flag != null)
                            {
                                xSwingVel = Helpers.lerp(xSwingVel, 0, Global.spf * 5);
                                if (MathF.Abs(xSwingVel) < 20) xSwingVel = 0;
                            }
                        }

                        if (chr.player.input.isHeld(Control.Left, chr.player) && xSwingVel > 0)
                        {
                            xSwingVel -= Global.spf * 1000;
                            if (xSwingVel < 0) xSwingVel = 0;
                        }
                        else if (chr.player.input.isHeld(Control.Right, chr.player) && xSwingVel < 0)
                        {
                            xSwingVel += Global.spf * 1000;
                            if (xSwingVel > 0) xSwingVel = 0;
                        }
                    }

                    var wall = Global.level.checkCollisionActor(this, xSwingVel * Global.spf, 0);
                    if (wall != null && wall.gameObject is Wall) xSwingVel = 0;
                    if (grounded) xSwingVel = 0;
                    if (Math.Abs(xSwingVel) < 5) xSwingVel = 0;

                    if (chr != null)
                    {
                        if (chr.charState is UpDash || chr.charState is Hover) xSwingVel = 0;
                        if (chr.charState is Dash || chr.charState is AirDash)
                        {
                            //if (MathF.Sign(chr.xDir) != MathF.Sign(xSwingVel)) xSwingVel = 0;
                            xSwingVel = 0;
                        }
                    }
                }

                if (!grounded) xIceVel = 0;
                if (xIceVel != 0)
                {
                    xIceVel = Helpers.lerp(xIceVel, 0, Global.spf);
                    if (MathF.Abs(xIceVel) < 1)
                    {
                        xIceVel = 0;
                    }
                    else
                    {
                        var wall = Global.level.checkCollisionActor(this, xIceVel * Global.spf, 0);
                        if (wall != null && wall.gameObject is Wall)
                        {
                            xIceVel = 0;
                        }
                    }
                }

                if (this is RideChaser && isWading())
                {
                    grounded = true;
                    changePos(new Point(pos.x, lastWaterY + 1));
                    if (vel.y > 0) vel.y = 0;
                }

                if (this is Character)
                {
                    move(vel.addxy(xIceVel + xPushVel + xSwingVel, 0), true, false, false);
                }
                else if (!isStatic)
                {
                    move(vel.addxy(xIceVel + xPushVel + xSwingVel, 0), true, true, false);
                }

                float yMod = reversedGravity ? -1 : 1;
                if (chr?.charState is VileMK2Grabbed)
                {
                    grounded = false;
                }
                else if (physicsCollider != null && !isStatic && (canBeGrounded || useGravity))
                {
                    float yDist = 1;
                    if (grounded)
                    {
                        yDist = 300 * Global.spf;
                    }
                    yDist *= yMod;

                    CollideData collideData = Global.level.checkCollisionActor(this, 0, yDist, checkPlatforms: true);
                    
                    var hitActor = collideData?.gameObject as Actor;
                    bool isPlatform = false;
                    bool tooLowOnPlatform = false;
                    if (hitActor?.isPlatform == true)
                    {
                        bool dropThruWolfPaw = hitActor is WolfSigmaHand && chr != null && !chr.grounded && chr.player.input.isHeld(Control.Down, chr.player);
                        if (!dropThruWolfPaw)
                        {
                            isPlatform = true;
                            if (pos.y > hitActor.getTopY() + 10)
                            {
                                tooLowOnPlatform = true;
                                isPlatform = false;
                            }
                        }
                        else
                        {
                            collideData = null;
                        }
                    }

                    if (this is Flag && hitActor is WolfSigmaHand)
                    {
                        isPlatform = false;
                    }

                    if (tooLowOnPlatform)
                    {
                        tooLowOnPlatform = false;
                        collideData = Global.level.checkCollisionActor(this, 0, yDist);
                    }

                    if (collideData != null && vel.y * yMod >= 0)
                    {
                        grounded = true;
                        landingVelY = vel.y;
                        vel.y = 0;

                        var hitWall = collideData.gameObject as Wall;
                        if (hitWall?.isMoving == true)
                        {
                            move(hitWall.deltaMove, useDeltaTime: false);
                        }
                        else if (hitWall != null && hitWall.moveX != 0)
                        {
                            if (this is RideChaser rc)
                            {
                                rc.addXMomentum(hitWall.moveX);
                            }
                            else
                            {
                                move(new Point(hitWall.moveX, 0));
                            }
                        }
                        if (isPlatform)
                        {
                            move(hitActor.deltaPos, useDeltaTime: false);
                        }

                        groundedIce = false;
                        if (hitWall != null && hitWall.slippery)
                        {
                            groundedIce = true;
                        }

                        //If already grounded, snap to ground further
                        CollideData collideDataCloseCheck = Global.level.checkCollisionActor(this, 0, 0.05f * yMod);
                        if (collideDataCloseCheck == null)
                        {
                            var yVel = new Point(0, yDist);
                            var mtv = Global.level.getMtvDir(this, 0, yDist, yVel, false, new List<CollideData>() { collideData });
                            if (mtv != null)
                            {
                                incPos(yVel);
                                incPos(mtv.Value.unitInc(0.01f));
                            }

                            var iceSled = this as ShotgunIceProjSled;
                            if (iceSled != null)
                            {
                                iceSled.increaseVel();
                            }
                        }
                    }
                    else
                    {
                        grounded = false;
                        groundedIce = false;
                    }
                }
            }

            if (this is RideChaser && isWading())
            {
                grounded = true;
                if (vel.y > 0) vel.y = 0;
            }

            bool isRaSpawning = (ra != null && ra.isSpawning());
            bool isChrSpawning = (chr != null && chr.isSpawning());
            if (splashable && !isChrSpawning && !isRaSpawning)
            {
                if (wading || underwater)
                {
                    if (waterTime == 0)
                    {
                        new Anim(new Point(pos.x, lastWaterY), "splash", 1, null, true);
                        playSound("splash");
                        vel.y = 0;
                    }
                    waterTime += Global.spf;
                }
                else
                {
                    if (waterTime > 0)
                    {
                        new Anim(new Point(pos.x, lastWaterY), "splash", 1, null, true);
                        playSound("splash");
                    }
                    waterTime = 0;
                }

                if (wading && !underwater)
                {
                    waterWade.visible = true;
                    if (waterWade.pos.x != pos.x)
                    {
                        waterWade.changeSprite("wade_move", false);
                    }
                    else
                    {
                        waterWade.changeSprite("wade", false);
                    }
                    waterWade.pos = new Point(pos.x, lastWaterY);
                }
                else
                {
                    waterWade.visible = false;
                }

                if (underwater)
                {
                    underwaterTime += Global.spf;
                    if (chr != null)
                    {
                        bubbleTime += Global.spf;
                        if (bubbleTime > 1)
                        {
                            bubbleTime = 0;
                            new BubbleAnim(chr.getHeadPos() ?? chr.getCenterPos(), "bubbles");
                        }
                    }
                    if (underwaterTime < 0.5f)
                    {
                        bigBubbleTime -= Global.spf;
                        if (bigBubbleTime <= 0)
                        {
                            bigBubbleTime = 0.08f;
                            var points = globalCollider?.shape.points;
                            if (points != null && points.Count >= 1) new BubbleAnim(new Point(pos.x, points[0].y), "bigbubble" + ((Global.frameCount % 3) + 1));
                        }
                    }
                }
                else
                {
                    underwaterTime = 0;
                }
            }

            if (gravityWellable)
            {
                Helpers.decrementTime(ref gravityWellTime);
                if (gravityWellTime <= 0)
                {
                    gravityWellModifier = 1;
                }
            }

            if (this is not CrackedWall)
            {
                // Process trigger events. Must loop thru all collisions in this case.
                List<CollideData> triggerList = Global.level.getTriggerList(this, 0, 0);

                // Prioritize certain colliders over others, running them first
                triggerList = triggerList.OrderBy(trigger => 
                {
                    if (trigger.gameObject is GenericMeleeProj && trigger.otherCollider.flag == (int)HitboxFlag.None &&
                        (trigger.otherCollider.originalSprite == "sigma_block" || trigger.otherCollider.originalSprite == "zero_block"))
                    {
                        return 0;                        
                    }
                    else if (trigger.otherCollider.originalSprite?.StartsWith("sigma3_kaiser") == true && trigger.otherCollider.name == "head")
                    {
                        return 0;
                    }
                    else if (trigger.gameObject is GenericMeleeProj && trigger.otherCollider.flag == (int)HitboxFlag.None && trigger.otherCollider.originalSprite == "drdoppler_absorb")
                    {
                        return 0;
                    }
                    return 1;
                }).ToList();

                foreach (var trigger in triggerList)
                {
                    registerCollision(trigger);
                }
            }

            for (int i = damageHistory.Count - 1; i >= 0; i--)
            {
                if (Global.time - damageHistory[i].time > 15)
                {
                    damageHistory.RemoveAt(i);
                }
            }
        }

        public float getTopY()
        {
            var collider = this.collider;

            float cy = 0;
            if (sprite.alignment == "topleft")
            {
                cy = 0;
            }
            else if (sprite.alignment == "topmid")
            {
                cy = 0;
            }
            else if (sprite.alignment == "topright")
            {
                cy = 0;
            }
            else if (sprite.alignment == "midleft")
            {
                cy = 0.5f;
            }
            else if (sprite.alignment == "center")
            {
                cy = 0.5f;
            }
            else if (sprite.alignment == "midright")
            {
                cy = 0.5f;
            }
            else if (sprite.alignment == "botleft")
            {
                cy = 1;
            }
            else if (sprite.alignment == "botmid")
            {
                cy = 1;
            }
            else if (sprite.alignment == "botright")
            {
                cy = 1;
            }

            return pos.y - (collider.shape.getRect().h() * cy);
        }

        public float getYMod()
        {
            if (reversedGravity) return -1;
            return 1;
        }

        public void reverseGravity()
        {
            vel = new Point(0, 0);
            gravityModifier *= -1;
            reversedGravity = !reversedGravity;
            yScale *= -1;
        }

        // The code here needs to work for non-owners too. So all variables in it needs to be synced
        public bool shouldDraw()
        {
            if (!visible) return false;
            if (this is Character character)
            {
                if (character.isStealthModeSynced() && character.isInvisibleEnemy())
                {
                    return false;
                }
                if (character.isCStingInvisibleGraphics() && character.cStingPaletteTime % 3 == 0)
                {
                    return false;
                }
                if (character.isInvulnBS.getValue())
                {
                    int mod10 = Global.level.frameCount % 4;
                    if (mod10 < 2) return false;
                }
            }
            if (this is Maverick maverick)
            {
                if (maverick.invulnTime > 0)
                {
                    int mod10 = Global.level.frameCount % 4;
                    if (mod10 < 2) return false;
                }
            }
            return true;
        }

        public void getKillerAndAssister(Player ownPlayer, ref Player killer, ref Player assister, ref int? weaponIndex, ref int? assisterProjId, ref int? assisterWeaponId)
        {
            if (damageHistory.Count > 0)
            {
                for (int i = damageHistory.Count - 1; i >= 0; i--)
                {
                    var lastAttacker = damageHistory[i];
                    if (lastAttacker.envKillOnly && weaponIndex != null) continue;
                    if (Global.time - lastAttacker.time > 10) continue;
                    killer = lastAttacker.attacker;
                    weaponIndex = lastAttacker.weapon;
                    break;
                }
            }
            if (damageHistory.Count > 0)
            {
                for (int i = damageHistory.Count - 1; i >= 0; i--)
                {
                    var secondLastAttacker = damageHistory[i];
                    if (secondLastAttacker.attacker == killer) continue;

                    // Non-suicide case: prevent assists aggressively
                    if (killer != ownPlayer)
                    {
                        if (secondLastAttacker.envKillOnly && weaponIndex != null) continue;
                        if (Damager.unassistable(secondLastAttacker.projId)) continue;
                        if (Global.time - secondLastAttacker.time > 2) continue;
                    }
                    // Suicide case: grant assists liberally to "punish" suicider more
                    else if (Global.time - secondLastAttacker.time > 10)
                    {
                        continue;
                    }

                    assister = secondLastAttacker.attacker;
                    assisterProjId = secondLastAttacker.projId;
                    assisterWeaponId = secondLastAttacker.weapon;
                }
            }
        }

        /// <summary>
        ///  Indicates whether this player's attacks should be defender-favored to everyone else
        /// </summary>
        public bool isDefenderFavored()
        {
            if (Global.isOffline) return false;
            if (netcodeOverride != null)
            {
                if (netcodeOverride == NetcodeModel.FavorDefender) return true;
                else return false;
            }
            if (this is Character chr) return chr.player?.isDefenderFavored == true;
            if (netOwner != null) return netOwner.isDefenderFavored;
            if (this is Projectile proj) return proj.owner?.isDefenderFavored == true;
            return false;
        }

        public bool isDefenderFavoredAndOwner()
        {
            return isDefenderFavored() && ownedByLocalPlayer;
        }

        // This can be used if a certain effect should be done by only the attacker or defender (if defender favor is on)
        public bool isRunByLocalPlayer()
        {
            if (Global.isOffline) return true;
            if (!isDefenderFavored())
            {
                if (!ownedByLocalPlayer) return false;
            }
            else
            {
                if (ownedByLocalPlayer) return false;
            }
            return true;
        }

        public virtual void postUpdate()
        {

        }

        public bool destroyPosSet;
        public float destroyPosTime;
        public float maxDestroyTime;
        public void moveToPosThenDestroy(Point destroyPos, float speed)
        {
            destroyPosSet = true;
            vel = pos.directionToNorm(destroyPos).times(speed);
            float distToDestroyPos = pos.distanceTo(destroyPos);
            maxDestroyTime = Math.Max(distToDestroyPos / speed, 0.1f);
            if (maxDestroyTime <= 0) maxDestroyTime = 1;
        }

        public void netUpdate()
        {
            if (netId == null) return;
            if (destroyPosSet)
            {
                destroyPosTime += Global.spf;
                incPos(vel.times(Global.spf));
                if (destroyPosTime > maxDestroyTime)
                {
                    destroySelf();
                }
                return;
            }

            if (ownedByLocalPlayer)
            {
                if (!Global.level.isSendMessageFrame()) return;
                sendActorNetData();
            }
            else
            {
                // 5 seconds since last net update: destroy the object
                if (Global.tickRate > 1 && Global.time - lastNetUpdate > 5 && cleanUpOnNoResponse())
                {
                    destroySelf(rpc: true);
                    return;
                }

                var netPos = pos;
                if (netXPos != null) netPos.x = (float)netXPos;
                if (netYPos != null) netPos.y = (float)netYPos;

                var incPos = netPos.subtract(pos).times(1f / Global.tickRate);
                var framePos = pos.add(incPos);

                if (pos.distanceTo(framePos) > 0.001f && !stopSyncingNetPos)
                {
                    changePos(framePos);
                }

                int spriteIndex = Global.spriteNames.IndexOf(sprite.name);
                if (netSpriteIndex != null && netSpriteIndex != spriteIndex)
                {
                    int index = (int)netSpriteIndex;
                    if (index >= 0 && index < Global.spriteNames.Count)
                    {
                        string spriteName = Global.spriteNames[index];
                        changeSprite(spriteName, true);
                    }
                }
                if (netFrameIndex != null && frameIndex != netFrameIndex)
                {
                    if (netFrameIndex >= 0 && netFrameIndex < sprite.frames.Count)
                    {
                        frameIndex = (int)netFrameIndex;
                    }
                }

                if (netXDir != null && xDir != netXDir)
                {
                    xDir = (int)netXDir;
                }
                if (netYDir != null && yDir != netYDir)
                {
                    yDir = (int)netYDir;
                }
                if (netAngle != null && netAngle != lastAngle)
                {
                    angle = netAngle;
                }
            }
        }

        public bool isRollingShield()
        {
            return this is RollingShieldProj;
        }

        public virtual void render(float x, float y)
        {
            if (sprite == null || currentFrame == null) return;

            //console.log(this.pos.x + "," + this.pos.y);

            var offsetX = xDir * currentFrame.offset.x;
            var offsetY = yDir * currentFrame.offset.y;

            // Don't draw actors out of the screen for optimization
            var alignOffset = sprite.getAlignOffset(frameIndex, xDir, yDir);
            var rx = pos.x + alignOffset.x;
            var ry = pos.y + alignOffset.y;
            var rect = new Rect(rx, ry, rx + currentFrame.rect.w(), ry + currentFrame.rect.h());
            var camRect = new Rect(Global.level.camX, Global.level.camY, Global.level.camX + Global.viewScreenW, Global.level.camY + Global.viewScreenH);
            if (!rect.overlaps(camRect))
            {
                return;
            }

            var drawX = pos.x + x + offsetX;
            var drawY = pos.y + y + offsetY;
            
            if (customAngleRendering)
            {
                renderFromAngle(x, y);
            }
            else
            {
                sprite.draw(frameIndex, drawX, drawY, xDir, yDir, getRenderEffectSet(), alpha, xScale, yScale, zIndex, getShaders(), angle: angle ?? 0, actor: this);
            }

            renderHitboxes();
        }

        public void commonHealLogic(Player healer, float healAmount, float currentHealth, float maxHealth, bool drawHealText)
        {
            if (drawHealText && ownedByLocalPlayer)
            {
                float reportAmount = Helpers.clampMax(healAmount, maxHealth - currentHealth);
                if (reportAmount == 0)
                {
                    bool hasSubtankCapacity = false;
                    if (this is Character chr && chr.player.hasSubtankCapacity()) hasSubtankCapacity = true;
                    if (this is Maverick maverick && maverick.player.hasSubtankCapacity()) hasSubtankCapacity = true;
                    if (hasSubtankCapacity)
                    {
                        RPC.playSound.sendRpc("subtankFill", netId);
                    }
                }
                else
                {
                    healer.creditHealing(reportAmount);
                    addDamageTextHelper(healer, -reportAmount, 16, sendRpc: true);
                }
            }
        }

        public void addDamageTextHelper(Player attacker, float damage, float maxHealth, bool sendRpc)
        {
            if (attacker == null) return;

            float reportDamage = Helpers.clampMax(damage, maxHealth);
            if (attacker.isMainPlayer)
            {
                addDamageText(reportDamage);
            }
            else if (ownedByLocalPlayer && sendRpc)
            {
                RPC.addDamageText.sendRpc(attacker.id, netId, reportDamage);
            }
        }

        public void addDamageText(float damage)
        {
            int xOff = 0;
            int yOff = 0;
            for (int i = damageTexts.Count - 1; i >= 0; i--)
            {
                if (damageTexts[i].time < 0.1f)
                {
                    yOff -= 8;
                }
            }
            string text = "-" + damage.ToString();
            bool isHeal = false;
            if (damage < 0)
            {
                text = "+" + (damage * -1).ToString();
                isHeal = true;
            }
            damageTexts.Add(new DamageText(text, 0, pos, new Point(xOff, yOff), isHeal));
        }

        public void renderDamageText(float yOff)
        {
            for (int i = damageTexts.Count - 1; i >= 0; i--)
            {
                var dt = damageTexts[i];
                dt.time += Global.spf;
                if (dt.time > 0.75f)
                {
                    damageTexts.RemoveAt(i);
                }
                float textPosX = dt.pos.x;
                float textPosY = dt.pos.y - yOff - (dt.time * 60);
                Color col = new Color(255, 32, 32, (byte)(255f - (dt.time * 0.00f * 255f)));
                Color outlineCol = new Color(Helpers.DarkBlue.R, Helpers.DarkBlue.G, Helpers.DarkBlue.B, (byte)(255f - (dt.time * 0.00f * 255f)));
                if (dt.isHeal)
                {
                    col = new Color(32, 255, 32, (byte)(255f - (dt.time * 0.00f * 255f)));
                    outlineCol = new Color(Helpers.DarkBlue.R, Helpers.DarkBlue.G, Helpers.DarkBlue.B, (byte)(255f - (dt.time * 0.00f * 255f)));
                }

                DrawWrappers.DrawText(dt.text, dt.offset.x + textPosX, dt.offset.y + textPosY, Alignment.Center, true, fontSize: 0.75f, color: col, outlineColor: outlineCol, Text.Styles.Regular, 1, true, ZIndex.HUD);
            }
        }

        public virtual void renderHUD()
        {

        }

        public HashSet<RenderEffectType> getRenderEffectSet()
        {
            var renderEffectSet = new HashSet<RenderEffectType>();
            foreach (var kvp in renderEffects)
            {
                if (!kvp.Value.isFlashing())
                {
                    renderEffectSet.Add(kvp.Key);
                }
            }
            return renderEffectSet;
        }

        public virtual void renderFromAngle(float x, float y)
        {
            sprite.draw(0, pos.x + x, pos.y + y, 1, 1, getRenderEffectSet(), 1, 1, 1, zIndex);
        }

        public bool isAnimOver()
        {
            return frameIndex == sprite.frames.Count - 1 && frameTime >= currentFrame.duration;
        }

        public void takeOwnership()
        {
            ownedByLocalPlayer = true;
            frameSpeed = 1;
        }

        public void addRenderEffect(RenderEffectType type, float flashTime = 0, float time = float.MaxValue)
        {
            if (renderEffects.ContainsKey(type)) return;
            renderEffects[type] = new RenderEffect(type, flashTime, time);
        }

        public void removeRenderEffect(RenderEffectType type)
        {
            renderEffects.Remove(type);
        }

        // It's important to configure actors properly for cleanup. The ones here indicate which ones to cleanup if no net response in 5 seconds
        public bool cleanUpOnNoResponse()
        {
            return this is Anim || this is Projectile || this is RaySplasherTurret;
        }

        // These are ones that should be cleaned up when the player leaves, but too important to be deleted if no response in 5 seconds
        public bool cleanUpOnPlayerLeave()
        {
            return this is Maverick || this is RideArmor || this is WolfSigmaHand || this is WolfSigmaHead;
        }

        public virtual void onDestroy()
        {
            if (netId != null && netOwner != null)
            {
                Global.level.recentlyDestroyedNetActors[netId.Value] = 0;
            }
        }

        //Optionally take in a sprite to draw when destroyed
        public virtual void destroySelf(string spriteName = null, string fadeSound = null, bool rpc = false, bool doRpcEvenIfNotOwned = false, bool favorDefenderProjDestroy = false)
        {
            // These should never be destroyed and can break the match if so
            if (this is Flag || this is FlagPedestal || this is ControlPoint || this is VictoryPoint)
            {
                return;
            }

            if (!destroyed)
            {
                destroyed = true;
                onDestroy();
            }
            else
            {
                return;
            }

            //console.log("DESTROYING")
            Global.level.removeGameObject(this);
            if (spriteName != null)
            {
                var anim = new Anim(getCenterPos(), spriteName, xDir, null, true);
                if (spriteName != "explosion")
                {
                    anim.angle = angle;
                    if (anim.angle != null)
                    {
                        anim.xDir = 1;
                    }
                }
                
                anim.xScale = xScale;
                anim.yScale = yScale;
            }
            if (fadeSound != null)
            {
                playSound(fadeSound);
            }

            // Character should not run destroy RPC. The destroyCharacter RPC handles that already
            var character = this as Character;
            if (character == null)
            {
                if ((ownedByLocalPlayer || doRpcEvenIfNotOwned) && netId != null && !rpc)
                {
                    float speed = vel.magnitude;
                    if (speed == 0) speed = deltaPos.magnitude / Global.spf;
                    RPC.destroyActor.sendRpc(netId.Value, (ushort)Global.spriteNames.IndexOf(spriteName), (ushort)Global.soundNames.IndexOf(fadeSound), pos, favorDefenderProjDestroy, speed);
                }
            }

            if (_waterWade != null)
            {
                _waterWade.destroySelf();
            }

            foreach (var projs in spriteFrameToProjs.Values)
            {
                foreach (var proj in projs)
                {
                    proj?.destroySelf();
                }
            }

            foreach (var proj in globalProjs)
            {
                proj?.destroySelf();
            }

            destroyMusicSource();
        }

        public void shakeCamera(bool sendRpc = false)
        {
            Point originPoint = Global.level.getSoundListenerOrigin();
            var dist = originPoint.distanceTo(pos);
            float distFactor = ownedByLocalPlayer ? Global.screenW : Global.screenW * 0.25f;
            var percent = Helpers.clamp01(1 - (dist / (distFactor)));
            Global.level.shakeY = percent * 0.2f;
            if (sendRpc)
            {
                RPC.actorToggle.sendRpc(netId, RPCActorToggleType.ShakeCamera);
            }
        }

        public float getSoundVolume()
        {
            if (Global.level == null || Global.level.is1v1()) return 100 * Options.main.soundVolume;

            Point originPoint = Global.level.getSoundListenerOrigin();

            var dist = originPoint.distanceTo(pos);
            var volume = 1 - (dist / (Global.screenW));

            volume = volume * 100 * Options.main.soundVolume;
            volume = Helpers.clamp(volume, 0, 100);

            return volume;
        }

        public Point getSoundPos()
        {
            Point originPoint = Global.level.getSoundListenerOrigin();
            float xPos = (pos.x - originPoint.x) / Global.halfScreenW;
            float yPos = (pos.y - originPoint.y) / Global.halfScreenH;
            return new Point(xPos, yPos);
        }

        public SoundWrapper playSound(string soundKey, bool forcePlay = false, bool sendRpc = false)
        {
            if (!Global.soundBuffers.ContainsKey(soundKey)) return null;
            return playSound(Global.soundBuffers[soundKey], forcePlay: forcePlay, sendRpc: sendRpc);
        }

        public SoundWrapper createSoundWrapper(SoundBufferWrapper soundBuffer, int? charNum)
        {
            if (charNum != null)
            {
                string charName;
                if (charNum.Value == 0) charName = "mmx";
                else if (charNum.Value == 1) charName = "zero";
                else if (charNum.Value == 2) charName = "vile";
                else if (charNum.Value == 3) charName = "axl";
                else charName = "sigma";

                var overrideSoundBuffer = Global.charSoundBuffers.GetValueOrDefault(soundBuffer.soundKey + "." + charName);
                if (overrideSoundBuffer != null)
                {
                    return new SoundWrapper(overrideSoundBuffer, this);
                }
            }
            
            return new SoundWrapper(soundBuffer, this);
        }

        public SoundWrapper playSound(SoundBufferWrapper soundBuffer, bool forcePlay = false, bool sendRpc = false)
        {
            var recentClipCount = Global.level.recentClipCount;
            if (recentClipCount.ContainsKey(soundBuffer.soundKey) && recentClipCount[soundBuffer.soundKey].Count > 1)
            {
                if (!forcePlay)
                {
                    return null;
                }
            }
            if (!recentClipCount.ContainsKey(soundBuffer.soundKey))
            {
                recentClipCount[soundBuffer.soundKey] = new List<float>();
            }
            if (getSoundVolume() > 0)
            {
                recentClipCount[soundBuffer.soundKey].Add(0);
            }

            int? charNum = null;
            if (this is Character || this is Maverick)
            {
                charNum = this is Character chr ? chr.player.charNum : 4;
            }

            SoundWrapper sound = createSoundWrapper(soundBuffer, charNum);
            sound.play();

            if (charNum != null && soundBuffer.soundPool != SoundPool.Voice)
            {
                var matchingVoice = Helpers.getRandomMatchingVoice(Global.voiceBuffers, soundBuffer.soundKey, charNum.Value);
                if (matchingVoice != null)
                {
                    playSound(matchingVoice);
                }
            }

            if (sendRpc && ownedByLocalPlayer)
            {
                RPC.playSound.sendRpc(soundBuffer.soundKey, netId);
            }

            return sound;
        }

        public bool withinX(Actor other, float amount)
        {
            return Math.Abs(pos.x - other.pos.x) <= amount;
        }

        public bool withinY(Actor other, float amount)
        {
            return Math.Abs(pos.y - other.pos.y) <= amount;
        }

        public bool isFacing(Actor other)
        {
            return ((pos.x < other.pos.x && xDir == 1) || (pos.x >= other.pos.x && xDir == -1));
        }

        // GMTODO must be more generic, account for other alignments
        // Then find all places using this and ajust as necessary
        public virtual Point getCenterPos()
        {
            if (collider == null) return pos;
            var rect = collider.shape.getNullableRect();
            if (rect == null) return pos;

            if (sprite.alignment.Contains("bot"))
            {
                return pos.addxy(0, -rect.Value.h() / 2);
            }

            return pos;
        }

        public void breakFreeze(Player player, Point? pos = null, bool sendRpc = false)
        {
            if (pos == null) pos = getCenterPos();
            if (!player.ownedByLocalPlayer) sendRpc = false;

            Anim.createGibEffect("shotgun_ice_break", pos.Value, player, sendRpc: sendRpc);

            playSound("iceBreak", sendRpc: sendRpc);
        }

        public void updateProjectileCooldown()
        {
            foreach (var key in projectileCooldown.Keys.ToList())
            {
                string projName = key;
                float cooldown = projectileCooldown[key];
                if (cooldown > 0)
                {
                    projectileCooldown[projName] = Helpers.clampMin(cooldown - Global.spf, 0);
                }
            }
            foreach (var key in flinchCooldown.Keys.ToList())
            {
                int projName = key;
                float cooldown = flinchCooldown[key];
                if (cooldown > 0)
                {
                    flinchCooldown[projName] = Helpers.clampMin(cooldown - Global.spf, 0);
                }
            }
        }

        public void turnToPos(Point lookPos)
        {
            if (lookPos.x > pos.x) xDir = 1;
            else xDir = -1;
        }

        public void turnToInput(Input input, Player player)
        {
            if (input.isHeld(Control.Left, player))
            {
                xDir = -1;
            }
            else if (input.isHeld(Control.Right, player))
            {
                xDir = 1;
            }
        }

        public void stopMoving()
        {
            xIceVel = 0;
            xPushVel = 0;
            xSwingVel = 0;
            vel.x = 0;
            vel.y = 0;
        }

        public void unstickFromGround()
        {
            useGravity = false;
            grounded = false;
            vel.y = -1;
        }

        public bool stopCeiling()
        {
            if (vel.y < 0 && Global.level.checkCollisionActor(this, 0, -1) != null)
            {
                vel.y = 0;
                return true;
            }
            return false;
        }

        public Point getPoiOrigin()
        {
            if (!reversedGravity) return pos;
            if (this is MagnaCentipede ms) return pos.addxy(0, -ms.height);
            return pos;
        }

        public Point getPOIPos(Point poi)
        {
            return getPoiOrigin().addxy(poi.x * xDir * xScale, poi.y * yScale);
        }

        public Point getFirstPOIOrDefault(int index = 0)
        {
            if (sprite?.getCurrentFrame()?.POIs?.Count > 0)
            {
                Point poi = sprite.getCurrentFrame().POIs[index];
                return getPoiOrigin().addxy(poi.x * xDir * xScale, poi.y * yScale);
            }
            return getCenterPos();
        }

        public Point getFirstPOIOrDefault(string tag, int? frameIndex = null)
        {
            Frame frame = frameIndex != null ? sprite?.frames?[frameIndex.Value] : sprite?.getCurrentFrame();
            if (frame?.POIs?.Count > 0)
            {
                int poiIndex = frame.POITags.FindIndex(t => t == tag);
                if (poiIndex >= 0)
                {
                    Point poi = frame.POIs[poiIndex];
                    return getPoiOrigin().addxy(poi.x * xDir * xScale, poi.y * yScale);
                }
            }
            return getCenterPos();
        }

        public Point? getFirstPOI(int index = 0)
        {
            if (sprite?.getCurrentFrame()?.POIs?.Count > 0)
            {
                Point poi = sprite.getCurrentFrame().POIs[index];
                return getPoiOrigin().addxy(poi.x * xDir * xScale, poi.y * yScale);
            }
            return null;
        }

        public Point? getFirstPOI(string tag)
        {
            if (sprite?.getCurrentFrame()?.POIs?.Count > 0)
            {
                int poiIndex = sprite.getCurrentFrame().POITags.FindIndex(t => t == tag);
                if (poiIndex >= 0)
                {
                    Point poi = sprite.getCurrentFrame().POIs[poiIndex];
                    return getPoiOrigin().addxy(poi.x * xDir * xScale, poi.y * yScale);
                }
            }
            return null;
        }

        public Point? getFirstPOIOffsetOnly(int index = 0)
        {
            if (sprite?.getCurrentFrame()?.POIs?.Count > 0)
            {
                Point poi = sprite.getCurrentFrame().POIs[index];
                return poi;
            }
            return null;
        }

        public Point getFtdPredictedPos()
        {
            if (netOwner == null) return pos;
            float combinedPing = (netOwner.ping ?? 0) + (Global.level.mainPlayer.ping ?? 0);
            float pingSeconds = combinedPing / 1000;
            return pos.add(deltaPos.times(pingSeconds / Global.spf));
        }

        public void addMusicSource(string musicName, Point worldPos, bool moveWithActor)
        {
            var ms = Global.musics[musicName].clone();
            ms.musicSourcePos = worldPos;
            ms.musicSourceActor = this;
            ms.volume = 0;
            ms.moveWithActor = moveWithActor;
            ms.play();
            Global.level.musicSources.Add(ms);
            musicSource = ms;
        }

        public void destroyMusicSource()
        {
            if (musicSource != null)
            {
                Global.level.musicSources.Remove(musicSource);
                musicSource.destroy();
                musicSource = null;
            }
        }

        public float getDistFromGround()
        {
            var ground = Global.level.raycast(pos, pos.addxy(0, 1000), new List<Type>() { typeof(Wall) });
            if (ground != null)
            {
                return MathF.Abs(pos.y - ground.hitData.hitPoint.Value.y);
            }
            return -1;
        }

        public void moveWithMovingPlatform()
        {
            if (!Global.level.hasMovingPlatforms) isStatic = true;
            var collideDatas = Global.level.getTriggerList(this, 0, 1, null, typeof(Wall), typeof(MovingPlatform));
            foreach (var collideData in collideDatas)
            {
                var hitWall = collideData?.gameObject as Wall;
                if (hitWall != null && hitWall.isMoving)
                {
                    move(hitWall.deltaMove, useDeltaTime: false);
                    break;
                }
            }
        }

        public const int labelCursorOffY = 5;
        public const int labelWeaponIconOffY = 18;
        public const int labelKillFeedIconOffY = 9;
        public const int labelAxlAimModeIconOffY = 15;
        public const int labelCooldownOffY = 15;
        public const int labelSubtankOffY = 17;
        public const int labelStatusOffY = 20;
        public const int labelHealthOffY = 6;
        public const int labelNameOffY = 10;

        public float currentLabelY;
        public void deductLabelY(float amount)
        {
            currentLabelY -= amount;
            // DrawWrappers.DrawLine(pos.x - 10, pos.y + currentLabelY, pos.x + 10, pos.y + currentLabelY, Color.Red, 1, ZIndex.HUD);
        }

        public void drawFuelMeter(float healthPct, float sx, float sy)
        {
            float healthBarInnerWidth = 30;
            Color color = new Color();
            float width = Helpers.clampMax(MathF.Ceiling(healthBarInnerWidth * healthPct), healthBarInnerWidth);
            if (healthPct > 0.66) color = Color.Green;
            else if (healthPct <= 0.66 && healthPct >= 0.33) color = Color.Yellow;
            else if (healthPct < 0.33) color = Color.Red;

            DrawWrappers.DrawRect(pos.x - 47 + sx, pos.y - 16 + sy, pos.x - 42 + sx, pos.y + 16 + sy, true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White);
            DrawWrappers.DrawRect(pos.x - 46 + sx, pos.y + 15 - width + sy, pos.x - 43 + sx, pos.y + 15 + sy, true, color, 0, ZIndex.HUD - 1);
        }

        public CollideData getHitWall(float x, float y)
        {
            var hits = Global.level.checkCollisionsActor(this, x, y, checkPlatforms: true);
            var bestWall = hits.FirstOrDefault(h => h.gameObject is Wall wall && !wall.collider.isClimbable);
            if (bestWall != null) return bestWall;
            return hits.FirstOrDefault();
        }

        public void setRaColorShader()
        {
            if (sprite.name == "neutralra_pieces")
            {
                genericShader = Helpers.cloneGenericPaletteShader("paletteChimera");
                genericShader?.SetUniform("palette", 1);
            }
            else if (sprite.name == "kangaroo_pieces")
            {
                genericShader = Helpers.cloneGenericPaletteShader("paletteKangaroo");
                genericShader?.SetUniform("palette", 1);
            }
            else if (sprite.name == "hawk_pieces")
            {
                genericShader = Helpers.cloneGenericPaletteShader("paletteHawk");
                genericShader?.SetUniform("palette", 1);
            }
            else if (sprite.name == "frog_pieces")
            {
                genericShader = Helpers.cloneGenericPaletteShader("paletteFrog");
                genericShader?.SetUniform("palette", 1);
            }
        }
    }
}
