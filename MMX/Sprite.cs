using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MMXOnline
{
    public class Sprite
    {
        public string name;
        public string customMapName;
        public List<Collider> hitboxes;
        public int loopStartFrame;
        public List<Frame> frames;
        public string alignment;
        public float alignOffX;
        public float alignOffY;
        public string spritesheetPath;  // Legacy system had the full path. New system only saves the file name in the json since the entire path isn't needed or used anywhere
        public string wrapMode; //Can be "once", "loop" or "pingpong"
        public bool freeForPool = false;
        public Texture bitmap;
        public Texture xArmorBootsBitmap;
        public Texture xArmorBodyBitmap;
        public Texture xArmorHelmetBitmap;
        public Texture xArmorArmBitmap;
        public Texture xArmorBootsBitmap2;
        public Texture xArmorBodyBitmap2;
        public Texture xArmorHelmetBitmap2;
        public Texture xArmorArmBitmap2;
        public Texture xArmorBootsBitmap3;
        public Texture xArmorBodyBitmap3;
        public Texture xArmorHelmetBitmap3;
        public Texture xArmorArmBitmap3;
        public Texture axlArmBitmap;
        public bool isXSprite;

        public float time;
        public int frameIndex = 0;
        public float frameSpeed = 1;
        public float frameTime = 0;
        public float animTime = 0;
        public int loopCount = 0;
        public bool visible = true;
        public bool reversed;

        public Sprite(Texture texture)
        {
            bitmap = texture;
            frames = new List<Frame>();
            hitboxes = new List<Collider>();
        }

        public Sprite(string spriteJsonStr, string name, string customMapName)
        {
            dynamic spriteJson = JsonConvert.DeserializeObject(spriteJsonStr);

            this.name = name;
            this.customMapName = customMapName;
            alignment = Convert.ToString(spriteJson.alignment);

            alignOffX = Convert.ToInt32(spriteJson.alignOffX);
            alignOffY = Convert.ToInt32(spriteJson.alignOffY);

            wrapMode = Convert.ToString(spriteJson.wrapMode);
            loopStartFrame = Convert.ToInt32(spriteJson.loopStartFrame);

            spritesheetPath = Path.GetFileName(Convert.ToString(spriteJson.spritesheetPath));
            if (!string.IsNullOrEmpty(customMapName))
            {
                spritesheetPath = customMapName + ":" + spritesheetPath;
            }

            string textureName = Path.GetFileNameWithoutExtension(spritesheetPath);
            bitmap = Global.textures[textureName];

            if (textureName == "XDefault")
            {
                isXSprite = true;

                xArmorBootsBitmap = Global.textures["XBoots"];
                xArmorBodyBitmap = Global.textures["XBody"];
                xArmorHelmetBitmap = Global.textures["XHelmet"];
                xArmorArmBitmap = Global.textures["XArm"];

                xArmorBootsBitmap2 = Global.textures["XBoots2"];
                xArmorBodyBitmap2 = Global.textures["XBody2"];
                xArmorHelmetBitmap2 = Global.textures["XHelmet2"];
                xArmorArmBitmap2 = Global.textures["XArm2"];

                xArmorBootsBitmap3 = Global.textures["XBoots3"];
                xArmorBodyBitmap3 = Global.textures["XBody3"];
                xArmorHelmetBitmap3 = Global.textures["XHelmet3"];
                xArmorArmBitmap3 = Global.textures["XArm3"];
            }
            if (textureName == "axl")
            {
                axlArmBitmap = Global.textures["axlArm"];
            }

            this.frames = new List<Frame>();
            this.hitboxes = new List<Collider>();

            JArray hitboxes = spriteJson["hitboxes"];
            foreach (dynamic hitboxJson in hitboxes)
            {
                float width = (float)Convert.ToDouble(hitboxJson["width"]);
                float height = (float)Convert.ToDouble(hitboxJson["height"]);
                float offsetX = (float)Convert.ToDouble(hitboxJson["offset"]["x"]);
                float offsetY = (float)Convert.ToDouble(hitboxJson["offset"]["y"]);
                bool isTrigger = Convert.ToBoolean(hitboxJson["isTrigger"]);
                int flag = Convert.ToInt32(hitboxJson["flag"]);
                string hitboxName = Convert.ToString(hitboxJson["name"]);

                Collider hitbox = new Collider(new List<Point>()
                {
                  new Point(0, 0),
                  new Point(0 + width, 0),
                  new Point(0 + width, 0 + height),
                  new Point(0, 0 + height)
                }, isTrigger ? true : false, null, false, false, (HitboxFlag)flag, new Point(offsetX, offsetY));
                hitbox.name = hitboxName;
                hitbox.originalSprite = this.name;
                this.hitboxes.Add(hitbox);
            }

            JArray frames = spriteJson["frames"];
            foreach (dynamic frameJson in frames) 
            {
                float x1 = (float)Convert.ToDouble(frameJson["rect"]["topLeftPoint"]["x"]);
                float y1 = (float)Convert.ToDouble(frameJson["rect"]["topLeftPoint"]["y"]);
                float x2 = (float)Convert.ToDouble(frameJson["rect"]["botRightPoint"]["x"]);
                float y2 = (float)Convert.ToDouble(frameJson["rect"]["botRightPoint"]["y"]);
                float duration = (float)Convert.ToDouble(frameJson["duration"]);
                float offsetX = (float)Convert.ToDouble(frameJson["offset"]["x"]);
                float offsetY = (float)Convert.ToDouble(frameJson["offset"]["y"]);

                Frame frame = new Frame(
                    new Rect(x1, y1, x2, y2),
                    duration,
                    new Point(offsetX, offsetY)
                );

                if (frameJson["POIs"] != null)
                {
                    dynamic poisJson = frameJson["POIs"];
                    for (int j = 0; j < poisJson.Count; j++)
                    {
                        float poiX = (float)Convert.ToDouble(poisJson[j]["x"]);
                        float poiY = (float)Convert.ToDouble(poisJson[j]["y"]);
                        string tags = Convert.ToString(poisJson[j]["tags"]);
                        if (tags == "h" || (name.Contains("zero_") && string.IsNullOrEmpty(tags)))
                        {
                            frame.headPos = new Point(poiX, poiY);
                        }
                        else
                        {
                            frame.POIs.Add(new Point(poiX, poiY));
                            frame.POITags.Add(tags);
                        }
                    }
                }

                if (frameJson["hitboxes"] != null)
                {
                    JArray hitboxFramesJson = frameJson["hitboxes"];

                    for (int j = 0; j < hitboxFramesJson.Count; j++)
                    {
                        dynamic hitboxFrameJson = hitboxFramesJson[j];

                        float width = (float)Convert.ToDouble(hitboxFrameJson["width"]);
                        float height = (float)Convert.ToDouble(hitboxFrameJson["height"]);
                        bool isTrigger = Convert.ToBoolean(hitboxFrameJson["isTrigger"]);
                        int flag = Convert.ToInt32(hitboxFrameJson["flag"]);
                        float offsetX2 = (float)Convert.ToDouble(hitboxFrameJson["offset"]["x"]);
                        float offsetY2 = (float)Convert.ToDouble(hitboxFrameJson["offset"]["y"]);
                        string hitboxName = Convert.ToString(hitboxFrameJson["name"]);

                        Collider hitbox = new Collider(new List<Point>()
                        {
                            new Point(0, 0),
                            new Point(0 + width, 0),
                            new Point(0 + width, 0 + height),
                            new Point(0, 0 + height)
                        }, isTrigger ? true : false, null, false, false, (HitboxFlag)flag, new Point(offsetX2, offsetY2));
                        hitbox.name = hitboxName;
                        hitbox.originalSprite = this.name;
                        frame.hitboxes.Add(hitbox);
                    }
                }
                this.frames.Add(frame);

            }
        }

        public bool update()
        {
            frameTime += Global.spf * frameSpeed;
            animTime += Global.spf * frameSpeed;
            time += Global.spf;
            var currentFrame = getCurrentFrame();
            if (currentFrame != null && frameTime >= currentFrame.duration)
            {
                bool onceEnd = wrapMode == "once" && frameIndex == frames.Count - 1;
                if (!onceEnd)
                {
                    frameTime = 0;
                    frameIndex++;
                    if (frameIndex >= frames.Count)
                    {
                        frameIndex = loopStartFrame;
                        animTime = 0;
                        loopCount++;
                    }
                    return true;
                }
            }
            return false;
        }

        public void overrideSprite(Sprite overrideSprite)
        {
            for (int i = 0; i < overrideSprite.frames.Count; i++)
            {
                frames[i].rect = overrideSprite.frames[i].rect;
                frames[i].offset = overrideSprite.frames[i].offset;
            }
            spritesheetPath = overrideSprite.spritesheetPath;
            string textureName = Path.GetFileNameWithoutExtension(spritesheetPath);
            bitmap = Global.textures[textureName];
        }

        public void restart()
        {
            frameIndex = 0;
            frameTime = 0;
            animTime = 0;
        }

        public Point getAlignOffset(int frameIndex, int flipX, int flipY) 
        {
            var frame = frames[frameIndex];
            var rect = frame.rect;
            var offset = frame.offset;
            return getAlignOffsetHelper(rect, offset, flipX, flipY);
        }

        //Draws a sprite immediately in screen coords. Good for HUD sprites whose z-index must be more fine grain controlled
        public void drawToHUD(int frameIndex, float x, float y, float alpha = 1)
        {
            if (!frames.InRange(frameIndex)) return;

            Frame currentFrame = frames[frameIndex];
            frameTime = 0;

            float cx = 0;
            float cy = 0;

            if (alignment == "topleft")
            {
                cx = 0; cy = 0;
            }
            else if (alignment == "topmid")
            {
                cx = 0.5f; cy = 0;
            }
            else if (alignment == "topright")
            {
                cx = 1; cy = 0;
            }
            else if (alignment == "midleft")
            {
                cx = 0; cy = 0.5f;
            }
            else if (alignment == "center")
            {
                cx = 0.5f; cy = 0.5f;
            }
            else if (alignment == "midright")
            {
                cx = 1; cy = 0.5f;
            }
            else if (alignment == "botleft")
            {
                cx = 0; cy = 1;
            }
            else if (alignment == "botmid")
            {
                cx = 0.5f; cy = 1;
            }
            else if (alignment == "botright")
            {
                cx = 1; cy = 1;
            }

            cx = cx * currentFrame.rect.w();
            cy = cy * currentFrame.rect.h();

            cx += alignOffX - currentFrame.offset.x;
            cy += alignOffY - currentFrame.offset.y;

            DrawWrappers.DrawTextureHUD(bitmap, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x - cx, y - cy, alpha);
        }

        public List<Trail> lastFiveTrailDraws = new List<Trail>();
        public List<Trail> lastTwoBkTrailDraws = new List<Trail>();
        public void draw(int frameIndex, float x, float y, int flipX, int flipY, HashSet<RenderEffectType> renderEffects, float alpha, float scaleX, float scaleY, long zIndex, List<ShaderWrapper> shaders = null, float angle = 0, Actor actor = null, bool useFrameOffsets = false)
        {
            if (!visible) return;
            if (actor != null)
            {
                if (!actor.shouldDraw()) return;
            }

            // Character-specific draw section
            int[] armors = null;
            bool drawAxlArms = true;
            bool hyperBusterReady = false;
            bool isUPX = false;
            bool isUltX = false;
            Character character = actor as Character;
            if (character != null)
            {
                if (character.isInvisibleBS.getValue() && !Global.shaderWrappers.ContainsKey("invisible"))
                {
                    alpha = 0.25f;
                }
                if (character.player.isX)
                {
                    armors = new int[] { character.player.bootsArmorNum, character.player.bodyArmorNum, character.player.helmetArmorNum, character.player.armArmorNum };
                }
                if (character.flattenedTime > 0)
                {
                    scaleY = 0.5f;
                }
                if (character.player.isAxl && character.player.axlWeapon != null)
                {
                    drawAxlArms = !character.player.axlWeapon.isTwoHanded(true);
                }
                isUPX = character.player.isX && (character.isHyperXBS.getValue() || (character.sprite.name == "mmx_revive" && character.frameIndex > 3));
                isUltX = character.player.isX && character.hasUltimateArmorBS.getValue();
            }

            if (name == "mmx_unpo_grab" || name == "mmx_unpo_grab2") zIndex = ZIndex.MainPlayer;

            Frame currentFrame = getCurrentFrame(frameIndex);
            if (currentFrame == null) return;

            float cx = 0;
            float cy = 0;

            if (alignment == "topleft")
            {
                cx = 0; cy = 0;
            }
            else if (alignment == "topmid")
            {
                cx = 0.5f; cy = 0;
            }
            else if (alignment == "topright")
            {
                cx = 1; cy = 0;
            }
            else if (alignment == "midleft")
            {
                cx = 0; cy = 0.5f;
            }
            else if (alignment == "center")
            {
                cx = 0.5f; cy = 0.5f;
            }
            else if (alignment == "midright")
            {
                cx = 1; cy = 0.5f;
            }
            else if (alignment == "botleft")
            {
                cx = 0; cy = 1;
            }
            else if (alignment == "botmid")
            {
                cx = 0.5f; cy = 1;
            }
            else if (alignment == "botright")
            {
                cx = 1; cy = 1;
            }

            cx = cx * currentFrame.rect.w();
            cy = cy * currentFrame.rect.h();

            cx += alignOffX;
            cy += alignOffY;


            if (scaleY == -1 && (actor is MagnaCentipede ms || name.Contains("magnac_teleport") || name.Contains("magnac_notail_teleport")))
            {
                cy -= MagnaCentipede.constHeight;
            }

            cx = MathF.Floor(cx);
            cy = MathF.Floor(cy);

            float frameOffsetX = 0;
            float frameOffsetY = 0;

            if (useFrameOffsets)
            {
                frameOffsetX = currentFrame.offset.x * flipX;
                frameOffsetY = currentFrame.offset.y * flipY;
            }

            if (shaders == null) shaders = new List<ShaderWrapper>();

            if (renderEffects != null)
            {
                ShaderWrapper shader = null;
                if (renderEffects.Contains(RenderEffectType.Hit))
                {
                    shader = Global.shaderWrappers.GetValueOrDefault("hit");
                    if (shaders.Count > 1) shaders.RemoveAt(1);
                }
                else if (renderEffects.Contains(RenderEffectType.Flash))
                {
                    shader = Global.shaderWrappers.GetValueOrDefault("flash");
                }
                else if (renderEffects.Contains(RenderEffectType.StockedCharge))
                {
                    shader = Global.shaderWrappers.GetValueOrDefault("stockedcharge");
                }
                else if (renderEffects.Contains(RenderEffectType.StockedSaber))
                {
                    shader = Global.shaderWrappers.GetValueOrDefault("stockedsaber");
                }
                else if (renderEffects.Contains(RenderEffectType.InvisibleFlash) && alpha == 1)
                {
                    shader = Global.shaderWrappers.GetValueOrDefault("invisible");
                    shader?.SetUniform("alpha", 0.5f - (MathF.Sin(Global.level.time * 5) * 0.25f));
                }
                else if (renderEffects.Contains(RenderEffectType.StealthModeBlue))
                {
                    shader = Global.shaderWrappers.GetValueOrDefault("stealthmode_blue");
                }
                else if (renderEffects.Contains(RenderEffectType.StealthModeRed))
                {
                    shader = Global.shaderWrappers.GetValueOrDefault("stealthmode_red");
                }
                if (shader != null)
                {
                    shaders.Add(shader);
                }

                if (renderEffects.Contains(RenderEffectType.Shake))
                {
                    frameOffsetX += Helpers.randomRange(-1, 1);
                    frameOffsetY += Helpers.randomRange(-1, 1);
                }
            }

            float xDirArg = flipX * scaleX;
            float yDirArg = flipY * scaleY;

            Texture bitmap = this.bitmap;

            if (renderEffects != null && !renderEffects.Contains(RenderEffectType.Invisible))
            {
                if (renderEffects.Contains(RenderEffectType.BlueShadow) && alpha >= 1)
                {
                    var blueShader = Global.shaderWrappers.GetValueOrDefault("outline_blue");
                    if (blueShader != null)
                    {
                        DrawWrappers.DrawTexture(bitmap, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX - 1, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, new List<ShaderWrapper>() { blueShader }, true);
                        DrawWrappers.DrawTexture(bitmap, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX + 1, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, new List<ShaderWrapper>() { blueShader }, true);
                        DrawWrappers.DrawTexture(bitmap, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX, y + frameOffsetY - 1, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, new List<ShaderWrapper>() { blueShader }, true);
                        DrawWrappers.DrawTexture(bitmap, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX, y + frameOffsetY + 1, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, new List<ShaderWrapper>() { blueShader }, true);
                    }
                }
                else if (renderEffects.Contains(RenderEffectType.RedShadow) && alpha >= 1)
                {
                    var redShader = Global.shaderWrappers.GetValueOrDefault("outline_red");
                    if (redShader != null)
                    {
                        DrawWrappers.DrawTexture(bitmap, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX - 1, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, new List<ShaderWrapper>() { redShader }, true);
                        DrawWrappers.DrawTexture(bitmap, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX + 1, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, new List<ShaderWrapper>() { redShader }, true);
                        DrawWrappers.DrawTexture(bitmap, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX, y + frameOffsetY - 1, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, new List<ShaderWrapper>() { redShader }, true);
                        DrawWrappers.DrawTexture(bitmap, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX, y + frameOffsetY + 1, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, new List<ShaderWrapper>() { redShader }, true);
                    }
                }
                else if (renderEffects.Contains(RenderEffectType.GreenShadow) && alpha >= 1)
                {
                    var greenShader = Global.shaderWrappers.GetValueOrDefault("outline_green");
                    if (greenShader != null)
                    {
                        DrawWrappers.DrawTexture(bitmap, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX - 1, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, new List<ShaderWrapper>() { greenShader }, true);
                        DrawWrappers.DrawTexture(bitmap, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX + 1, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, new List<ShaderWrapper>() { greenShader }, true);
                        DrawWrappers.DrawTexture(bitmap, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX, y + frameOffsetY - 1, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, new List<ShaderWrapper>() { greenShader }, true);
                        DrawWrappers.DrawTexture(bitmap, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX, y + frameOffsetY + 1, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, new List<ShaderWrapper>() { greenShader }, true);
                    }
                }

                if ((name == "boomerk_dash" || name == "boomerk_bald_dash") && (animTime > 0.01f || frameIndex > 0))
                {
                    if (Global.isOnFrameCycle(4))
                    {
                        var trail = lastTwoBkTrailDraws.ElementAtOrDefault(5);
                        if (trail != null)
                        {
                            trail.action.Invoke(trail.time);
                            trail.time -= Global.spf;
                        }
                    }
                    else
                    {
                        var trail = lastTwoBkTrailDraws.ElementAtOrDefault(9);
                        if (trail != null)
                        {
                            trail.action.Invoke(trail.time);
                            trail.time -= Global.spf;
                        }
                    }

                    var shaderList = new List<ShaderWrapper>();
                    if (Global.shaderWrappers.ContainsKey("boomerkTrail"))
                    {
                        ShaderWrapper boomerkTrail = Global.shaderWrappers["boomerkTrail"];
                        boomerkTrail.SetUniform("paletteTexture", Global.textures["boomerkTrailPalette"]);
                        shaderList.Add(boomerkTrail);
                    }

                    if (lastTwoBkTrailDraws.Count > 10) lastTwoBkTrailDraws.PopFirst();
                    lastTwoBkTrailDraws.Add(new Trail()
                    {
                        action = (float time) =>
                        {
                            DrawWrappers.DrawTexture(bitmap, frames[1].rect.x1, frames[1].rect.y1, frames[1].rect.w(), frames[1].rect.h(), x + frameOffsetX, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, shaderList, true);
                        },
                        time = 0.25f
                    });
                }
                else
                {
                    lastTwoBkTrailDraws.Clear();
                }

                if (renderEffects.Contains(RenderEffectType.Trail))
                {
                    for (int i = lastFiveTrailDraws.Count - 1; i >= 0; i--)
                    {
                        var trail = lastFiveTrailDraws[i];
                        trail.action.Invoke(trail.time);
                        trail.time -= Global.spf;
                    }

                    var shaderList = new List<ShaderWrapper>();
                    if (Global.shaderWrappers.ContainsKey("trail")) shaderList.Add(Global.shaderWrappers["trail"]);

                    if (lastFiveTrailDraws.Count > 5) lastFiveTrailDraws.PopFirst();
                    lastFiveTrailDraws.Add(new Trail()
                    {
                        action = (float time) =>
                        {
                            DrawWrappers.DrawTexture(bitmap, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, shaderList, true);
                        }, 
                        time = 0.25f
                    });   
                }

                if (renderEffects.Contains(RenderEffectType.SpeedDevilTrail) && character != null && Global.shaderWrappers.ContainsKey("speedDevilTrail"))
                {
                    for (int i = character.lastFiveTrailDraws.Count - 1; i >= 0; i--)
                    {
                        Trail trail = character.lastFiveTrailDraws[i];
                        if (character.isDashing)
                        {
                            trail.action.Invoke(trail.time);
                        }
                        trail.time -= Global.spf;
                        if (trail.time <= 0) character.lastFiveTrailDraws.RemoveAt(i);
                    }

                    var shaderList = new List<ShaderWrapper>();
                    
                    var speedDevilShader = Helpers.cloneShaderSafe("speedDevilTrail");
                    shaderList.Add(speedDevilShader);

                    if (character.lastFiveTrailDraws.Count > 1) character.lastFiveTrailDraws.PopFirst();

                    character.lastFiveTrailDraws.Add(new Trail()
                    {
                        action = (float time) =>
                        {
                            speedDevilShader?.SetUniform("alpha", time * 2);
                            DrawWrappers.DrawTexture(bitmap, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, shaderList, true);
                        },
                        time = 0.125f
                    });
                }
            }

            float extraYOff = 0;
            if (isUltX)
            {
                bitmap = Global.textures["XUltimate"];
                extraYOff = 3;
                armors = null;
            }

            if (isUPX)
            {
                bitmap = Global.textures["XUP"];
            }
                
            DrawWrappers.DrawTexture(bitmap, currentFrame.rect.x1, currentFrame.rect.y1 - extraYOff, currentFrame.rect.w(), currentFrame.rect.h() + extraYOff, x + frameOffsetX, y + frameOffsetY - extraYOff, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, shaders, true);
            
            if (isUPX)
            {
                var upShaders = new List<ShaderWrapper>(shaders);
                if (Global.isOnFrameCycle(5))
                {
                    if (Global.shaderWrappers.ContainsKey("hit"))
                    {
                        upShaders.Add(Global.shaderWrappers["hit"]);
                    }
                }
                DrawWrappers.DrawTexture(Global.textures["XUPGlow"], currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, upShaders, true);
            }

            if (armors != null && isXSprite)
            {
                bool isShootSprite = needsX3BusterCorrection();

                float xOff = 0;
                float extraW = 0;
                float flippedExtraW = 0;

                if (isShootSprite)
                {
                    if (name.Contains("mmx_wall_slide_shoot"))
                    {
                        flippedExtraW = 5;
                        extraW = flippedExtraW;
                        xOff = -flippedExtraW * flipX;
                    }
                    else
                    {
                        extraW = 5;
                    }
                }

                var x3ArmShaders = new List<ShaderWrapper>(shaders);
                if (hyperBusterReady)
                {
                    if (Global.isOnFrameCycle(5))
                    {
                        if (Global.shaderWrappers.ContainsKey("hit"))
                        {
                            x3ArmShaders.Add(Global.shaderWrappers["hit"]);
                        }
                    }
                }

                if (armors[2] == 1) DrawWrappers.DrawTexture(xArmorHelmetBitmap, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, shaders, true);
                if (armors[2] == 2) DrawWrappers.DrawTexture(xArmorHelmetBitmap2, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, shaders, true);
                if (armors[2] == 3) DrawWrappers.DrawTexture(xArmorHelmetBitmap3, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, shaders, true);

                if (armors[0] == 1) DrawWrappers.DrawTexture(xArmorBootsBitmap, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, shaders, true);
                if (armors[0] == 2) DrawWrappers.DrawTexture(xArmorBootsBitmap2, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, shaders, true);
                if (armors[0] == 3) DrawWrappers.DrawTexture(xArmorBootsBitmap3, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, shaders, true);

                if (armors[1] == 1) DrawWrappers.DrawTexture(xArmorBodyBitmap, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, shaders, true);
                if (armors[1] == 3) DrawWrappers.DrawTexture(xArmorBodyBitmap3, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, shaders, true);
                if (armors[1] == 2) DrawWrappers.DrawTexture(xArmorBodyBitmap2, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, shaders, true);

                if (armors[3] == 1) DrawWrappers.DrawTexture(xArmorArmBitmap, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, shaders, true);
                if (armors[3] == 2) DrawWrappers.DrawTexture(xArmorArmBitmap2, currentFrame.rect.x1 - flippedExtraW, currentFrame.rect.y1, currentFrame.rect.w() + extraW, currentFrame.rect.h(), x + frameOffsetX + xOff, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, shaders, true);
                if (armors[3] == 3) DrawWrappers.DrawTexture(xArmorArmBitmap3, currentFrame.rect.x1 - flippedExtraW, currentFrame.rect.y1, currentFrame.rect.w() + extraW, currentFrame.rect.h(), x + frameOffsetX + xOff, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, angle, alpha, x3ArmShaders, true);
            }

            if (axlArmBitmap != null && drawAxlArms)
            {
                DrawWrappers.DrawTexture(axlArmBitmap, currentFrame.rect.x1, currentFrame.rect.y1, currentFrame.rect.w(), currentFrame.rect.h(), x + frameOffsetX, y + frameOffsetY, zIndex, cx, cy, xDirArg, yDirArg, 0, alpha, shaders, true);
            }
        }

        public bool needsX3BusterCorrection()
        {
            return name.Contains("mmx_shoot") || name.Contains("mmx_run_shoot") || name.Contains("mmx_fall_shoot") || name.Contains("mmx_jump_shoot") || name.Contains("mmx_dash_shoot") || name.Contains("mmx_ladder_shoot")
                || name.Contains("mmx_wall_slide_shoot") || name.Contains("mmx_up_dash_shoot") || name.Contains("mmx_wall_kick_shoot");
        }

        public Frame getCurrentFrame(int frameIndex = -1)
        {
            if (frameIndex == -1) frameIndex = this.frameIndex;
            if (reversed) frameIndex = frames.Count - 1 - frameIndex;
            if (frameIndex < 0 || frameIndex >= frames.Count) return null;
            return frames[frameIndex];
        }

        public bool isAnimOver()
        {
            return (frameIndex == frames.Count - 1 && frameTime >= getCurrentFrame().duration) || loopCount > 0;
        }

        public float getAnimLength()
        {
            float total = 0;
            foreach (Frame frame in frames)
            {
                total += frame.duration;
            }
            return total;
        }

        public Sprite clone()
        {
            var clonedSprite = (Sprite)MemberwiseClone();
            clonedSprite.lastFiveTrailDraws = new List<Trail>();
            clonedSprite.hitboxes = new List<Collider>();
            foreach (Collider collider in hitboxes)
            {
                clonedSprite.hitboxes.Add(collider.clone());
            }
            clonedSprite.frames = new List<Frame>();
            foreach (Frame frame in frames)
            {
                clonedSprite.frames.Add(frame.clone());
            }
            return clonedSprite;
        }

        public Point getAlignOffsetHelper(Rect rect, Point offset, int? flipX, int? flipY)
        {
            flipX = flipX ?? 1;
            flipY = flipY ?? 1;

            var w = rect.w();
            var h = rect.h();

            var halfW = w * 0.5f;
            var halfH = h * 0.5f;

            if (flipX > 0) halfW = MathF.Floor(halfW);
            else halfW = MathF.Ceiling(halfW);
            if (flipY > 0) halfH = MathF.Floor(halfH);
            else halfH = MathF.Ceiling(halfH);

            float x = 0; 
            float y = 0;

            if (alignment == "topleft")
            {
                x = flipX == -1 ? -w : 0;
                y = 0;
            }
            else if (alignment == "topmid")
            {
                x = -halfW;
                y = 0;
            }
            else if (alignment == "topright")
            {
                x = flipX == -1 ? 0 : -w;
                y = 0;
            }
            else if (alignment == "midleft")
            {
                x = flipX == -1 ? -w : 0;
                y = -halfH;
            }
            else if (alignment == "center")
            {
                x = -halfW;
                y = -halfH;
            }
            else if (alignment == "midright")
            {
                x = flipX == -1 ? 0 : -w;
                y = -halfH;
            }
            else if (alignment == "botleft")
            {
                x = flipX == -1 ? -w : 0;
                y = -h;
            }
            else if (alignment == "botmid")
            {
                x = -halfW;
                y = -h;
            }
            else if (alignment == "botright")
            {
                x = flipX == -1 ? 0 : -w;
                y = -h;
            }

            x += alignOffX;
            y += alignOffY;

            return new Point(x + offset.x * (int)flipX, y + offset.y * (int)flipY);
        }
    }

    public class Trail
    {
        public Action<float> action;
        public float time;
    }
}
