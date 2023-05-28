using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MMXOnline
{
    public class Anim : Actor
    {
        bool destroyOnEnd;
        public float? ttl;
        public float time;
        public Point acc;
        public Actor host;
        public bool fadeIn;
        public bool fadeOut;
        public bool blink;
        public bool maverickFade;
        public bool grow;
        
        public Anim(Point pos, string spriteName, int xDir, ushort? netId, bool destroyOnEnd, bool sendRpc = false, bool ownedByLocalPlayer = true, Actor host = null,
            long? zIndex = null, Actor zIndexRelActor = null, bool fadeIn = false, bool hasRaColorShader = false) : 
            base(spriteName, new Point(pos.x, pos.y), netId, ownedByLocalPlayer, true)
        {
            this.host = host;
            useGravity = false;
            this.xDir = xDir;
            this.destroyOnEnd = destroyOnEnd;
            Global.level.gameObjects.Add(this);

            if (isMaverickDeathAnim(spriteName))
            {
                maverickFade = true;
                setzIndex(ZIndex.Character - 1);
                playSound("maverickDie", sendRpc: true);
            }

            if (spriteName.StartsWith("drlight"))
            {
                addMusicSource("drlight", getCenterPos(), false);
            }

            if (spriteName.StartsWith("cannon_muzzle"))
            {
                angle = 0;
                //if (host != null)
                {
                    setzIndex(ZIndex.HUD);
                }
            }

            if (spriteName == "risingspecter_muzzle")
            {
                xScale = 1;
                yScale = 1;
                setzIndex(ZIndex.Foreground);
            }

            if (spriteName == "csnail_shell_spin")
            {
                angle = 0;
            }

            if (!ownedByLocalPlayer)
            {
                if (spriteName == "spiralmagnum_shell" || spriteName == "plasmagun_effect")
                {
                    angle = 0;
                }
                if (spriteName == "spiralmagnum_shell")
                {
                    if (collider != null)
                    {
                        collider.wallOnly = true;
                    }
                }
            }

            if (spriteName == "sigma3_kaiser_empty" || spriteName == "sigma3_kaiser_exhaust")
            {
                this.zIndex = ZIndex.Background + 1000;
            }
            else if (spriteName == "hover_exhaust")
            {
                this.zIndex = ZIndex.Character - 100;
            }
            else if (spriteName == "bbuffalo_anim_exhaust")
            {
                this.zIndex = ZIndex.Background + 1000;
            }
            else if (spriteName == "drdoppler_barrier")
            {
                alpha = 0.5f;
            }
            else if (spriteName == "drdoppler_coat")
            {
                fadeOut = true;
            }
            else if (spriteName == "sigma3_kaiser_empty_fadeout")
            {
                setFadeOut(0.25f);
            }

            if (zIndex != null)
            {
                this.zIndex = (zIndexRelActor?.zIndex ?? 0) + zIndex.Value;
            }

            if (fadeIn)
            {
                this.fadeIn = true;
                alpha = 0;
            }

            if (hasRaColorShader)
            {
                setRaColorShader();
            }

            if (sendRpc && ownedByLocalPlayer && netId != null)
            {
                byte[] xBytes = BitConverter.GetBytes(pos.x);
                byte[] yBytes = BitConverter.GetBytes(pos.y);
                byte[] netProjIdByte = BitConverter.GetBytes(netId.Value);
                int spriteIndex = Global.spriteNames.IndexOf(spriteName);
                byte[] spriteIndexBytes = BitConverter.GetBytes((ushort)spriteIndex);

                var bytes = new List<byte>
                {
                    netProjIdByte[0], netProjIdByte[1],
                    spriteIndexBytes[0], spriteIndexBytes[1],
                    xBytes[0], xBytes[1], xBytes[2], xBytes[3],
                    yBytes[0], yBytes[1], yBytes[2], yBytes[3],
                    (byte)(xDir + 128)
                };
                
                if (zIndex != null || zIndexRelActor?.netId != null || fadeIn != false || hasRaColorShader != false)
                {
                    var extendedAnimModel = new RPCAnimModel();
                    extendedAnimModel.zIndex = zIndex;
                    extendedAnimModel.zIndexRelActorNetId = zIndexRelActor?.netId;
                    extendedAnimModel.fadeIn = fadeIn;
                    extendedAnimModel.hasRaColorShader = hasRaColorShader;
                    bytes.AddRange(Helpers.serialize(extendedAnimModel));
                }

                Global.serverClient?.rpc(RPC.createAnim, bytes.ToArray());
            }
        }

        private bool isMaverickDeathAnim(string spriteName)
        {
            string[] maverickDieSprites = new string[]
            {
                "chillp_die", "sparkm_die", "armoreda_die", "armoreda_na_die", "launcho_die", "boomerk_die", "boomerk_bald_die", "stingc_die", "storme_die", "flamem_die",
                "wsponge_die", "wheelg_die", "bcrab_die", "fstag_die", "morphmc_die", "morphm_die", "magnac_die", "magnac_notail_die", "csnail_die", "overdriveo_die", "fakezero_die",
                "bbuffalo_die", "tseahorse_die", "tunnelr_die", "voltc_die", "crushc_die", "neont_die", "gbeetle_die", "bhornet_die", "drdoppler_die",
            };
            return maverickDieSprites.Any(s => s == spriteName);
        }

        public override void update()
        {
            base.update();

            if (sprite.name == "risingspecter_muzzle")
            {
                float sin = MathF.Sin(Global.time * 100);
                float sinDamp = Helpers.clamp01(1 - (time / 0.5f));
                xScale = (0.75f + sin * 0.25f) * sinDamp;
                yScale = xScale;
            }

            if (blink)
            {
                visible = Global.isOnFrameCycle(5);
            }

            if (!acc.isZero())
            {
                vel.inc(acc.times(Global.spf));
            }

            if (destroyOnEnd && isAnimOver())
            {
                destroySelf();
                return;
            }

            if (maverickFade && fadeBlackShader != null)
            {
                const float blackStartTime = 2.5f;
                const float blackDuration = 1;
                const float fadeStartTime = 5f;
                const float fadeDuration = 0.5f;

                float blackTime = Helpers.clamp01((time - blackStartTime) * (1 / blackDuration));
                float fadeTime = 1 - Helpers.clamp01((time - fadeStartTime) * (1 / fadeDuration));

                fadeBlackShader.SetUniform("factor", blackTime);
                fadeBlackShader.SetUniform("alpha", fadeTime);
            }

            time += Global.spf;

            if (fadeIn)
            {
                alpha = Helpers.clamp01(time / sprite.getAnimLength());
            }
            else if (fadeOut)
            {
                alpha = Helpers.clamp01(1 - (time / sprite.getAnimLength()));
            }

            if (grow)
            {
                xScale = 1 + 2 * Helpers.clamp01(time / sprite.getAnimLength());
                yScale = 1 + 2 * Helpers.clamp01(time / sprite.getAnimLength());
            }

            if (sprite.name == "sigma3_kaiser_virus_return")
            {
                xScale -= Global.spf * 2.5f;
                yScale = xScale;
                if (xScale < 0)
                {
                    xScale = 0;
                    yScale = 0;
                }
            }

            if (ttl != null)
            {
                if (time > ttl.Value)
                {
                    destroySelf();
                    return;
                }
            }
            
            var leeway = 500;
            if (pos.x > Global.level.width + leeway || pos.x < -leeway || pos.y > Global.level.height + leeway || pos.y < -leeway)
            {
                destroySelf();
                return;
            }

            if (host != null && host.destroyed)
            {
                destroySelf();
            }
        }

        public override void postUpdate()
        {
            base.postUpdate();
            if (host != null)
            {
                if (sprite.name == "plasmagun_effect")
                {
                    if (ownedByLocalPlayer)
                    {
                        Character chr = host as Character;
                        Point bulletPos = chr.getAxlBulletPos();
                        angle = chr.getShootAngle(true);
                        changePos(bulletPos);
                    }
                }
                else
                {
                    incPos(host.deltaPos);
                }
            }
        }

        public ShaderWrapper fadeBlackShader;
        public ShaderWrapper viralSigmaShader;
        public override List<ShaderWrapper> getShaders()
        {
            if (maverickFade && Global.shaderWrappers.ContainsKey("fadeBlack"))
            {
                if (fadeBlackShader == null)
                {
                    fadeBlackShader = new ShaderWrapper("fadeBlack");
                    fadeBlackShader.SetUniform("alpha", 1f);
                }
                return new List<ShaderWrapper>() { fadeBlackShader };
            }
            if (sprite.name.Contains("sigma2_viral_") && Global.shaderWrappers.ContainsKey("viralsigma"))
            {
                if (viralSigmaShader == null)
                {
                    viralSigmaShader = new ShaderWrapper("viralsigma");
                }
                viralSigmaShader?.SetUniform("palette", 6);
                viralSigmaShader?.SetUniform("paletteTexture", Global.textures["paletteViralSigma"]);
                return new List<ShaderWrapper>() { viralSigmaShader };
            }
            return base.getShaders();
        }

        public static void createGibEffect(string spriteName, Point centerPos, Player player, GibPattern gibPattern = GibPattern.Radial, float randVelStart = 100, float randVelEnd = 200, float randDistStart = 0, float randDistEnd = 25, bool sendRpc = false)
        {
            var sprite = Global.sprites[spriteName];
            float startAngle = 0;
            for (int i = 0; i < sprite.frames.Count; i++)
            {
                float angle = Helpers.randomRange(0, 360);
                if (gibPattern == GibPattern.Radial || gibPattern == GibPattern.SemiCircle)
                {
                    angle = startAngle;
                    angle += Helpers.randomRange(-10, 10);
                }

                float randVel = Helpers.randomRange(randVelStart, randVelEnd);
                float randDist = Helpers.randomRange(randDistStart, randDistEnd);
                float compX = Helpers.cosd(angle);
                float compY = Helpers.sind(angle);
                var anim = new Anim(centerPos.addxy(compX * randDist, compY * randDist), spriteName, 1, sendRpc ? player.getNextActorNetId() : null, false, sendRpc: sendRpc);

                anim.useGravity = true;
                anim.ttl = 0.75f;
                anim.vel = new Point(compX * randVel, compY * randVel * 1.25f);
                anim.frameSpeed = 0;
                anim.frameIndex = i;

                if (gibPattern == GibPattern.Radial)
                {
                    startAngle -= 360 / sprite.frames.Count;
                }
                else if (gibPattern == GibPattern.SemiCircle)
                {
                    startAngle -= 180 / sprite.frames.Count;
                }
            }
        }

        public void setFadeOut(float timeToLive)
        {
            fadeOut = true;
            ttl = timeToLive;
            time = 0;
            sprite.restart();
        }
    }

    public enum GibPattern
    {
        Random,
        Radial,
        SemiCircle
    }

    public class BubbleAnim : Anim
    {
        public BubbleAnim(Point pos, string spriteName, ushort? netId = null, bool sendRpc = false, bool ownedByLocalPlayer = true) : 
            base(pos, spriteName, 1, netId, true, sendRpc, ownedByLocalPlayer)
        {
            vel.y = -50;
            ttl = 2;
        }

        public override void update()
        {
            base.update();
            if (!isUnderwater())
            {
                destroySelf();
                return;
            }
            if (sprite.name == "bubbles")
            {
                var col = Global.level.checkCollisionPoint(pos, new List<GameObject>() { });
                if (col != null && col.gameObject is Wall)
                {
                    destroySelf();
                    return;
                }
            }
        }
    }

    public class ParasiteAnim : Anim
    {
        float flashFrameTime;
        float flashFramePeriod = 0.25f;
        int flashFrameIndex;
        public ParasiteAnim(Point pos, string spriteName, ushort? netId = null, bool sendRpc = false, bool ownedByLocalPlayer = true) :
            base(pos, spriteName, 1, netId, false, sendRpc, ownedByLocalPlayer)
        {
        }

        public override void update()
        {
            base.update();
            flashFrameTime += Global.spf;
            if (flashFrameTime > flashFramePeriod)
            {
                flashFrameTime = 0;
                flashFrameIndex++;
                if (flashFrameIndex > 3) flashFrameIndex = 0;
            }
            flashFramePeriod -= Global.spf * 0.125f;
        }

        public override void render(float x, float y)
        {
            base.render(x, y);
            Global.sprites["parasitebomb_light"].draw(flashFrameIndex, pos.x + x, pos.y + y, 1, 1, null, 1, 1, 1, zIndex);
        }
    }

    public class AssassinBulletTrailAnim : Anim
    {
        Point sPos;
        Actor proj;
        Point lastProjPos;
        float fadeTime;
        const float maxFadeTime = 0.5f;
        public AssassinBulletTrailAnim(Point pos, Actor proj, ushort? netId = null, bool sendRpc = false, bool ownedByLocalPlayer = true) :
            base(pos, "empty", 1, netId, false, sendRpc, ownedByLocalPlayer)
        {
            this.proj = proj;
            sPos = pos;
            lastProjPos = proj.pos;
        }

        public override void update()
        {
            base.update();
            if (proj != null)
            {
                lastProjPos = proj.pos;
            }

            fadeTime += Global.spf;
            if (fadeTime > maxFadeTime)
            {
                destroySelf();
            }
        }

        public override void render(float x, float y)
        {
            byte alpha = (byte)(255 * (maxFadeTime - fadeTime));
            DrawWrappers.DrawLine(sPos.x, sPos.y, lastProjPos.x, lastProjPos.y, new Color(128, 128, 128, alpha), 4, zIndex - 100);
            DrawWrappers.DrawLine(sPos.x, sPos.y, lastProjPos.x, lastProjPos.y, new Color(255, 255, 255, alpha), 2, zIndex - 100);
        }
    }
}
