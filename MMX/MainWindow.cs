using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SFML.Graphics;
using SFML.System;
using SFML.Audio;
using SFML.Window;
using Newtonsoft.Json;
using static SFML.Window.Keyboard;
using SFML.Graphics.Glsl;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;

namespace MMXOnline
{
    public partial class Global
    {
        public static RenderWindow window;
        public static bool fullscreen;

        public static RenderTexture renderTexture;
        public static RenderTexture screenRenderTexture;
        public static RenderTexture srtBuffer1;
        public static RenderTexture srtBuffer2;
        public static RenderTexture radarRenderTexture;

        // Normal (small) camera
        public static RenderTexture screenRenderTextureS;
        public static RenderTexture srtBuffer1S;
        public static RenderTexture srtBuffer2S;

        // Large camera
        public static RenderTexture screenRenderTextureL;
        public static RenderTexture srtBuffer1L;
        public static RenderTexture srtBuffer2L;

        public static View view;
        public static View backgroundView;

        public const uint screenW = 298;
        public const uint screenH = 224;

        public static uint viewScreenW { get { return screenW * (uint)viewSize; } }
        public static uint viewScreenH { get { return screenH * (uint)viewSize; } }

        public static uint halfViewScreenW { get { return viewScreenW / 2; } }
        public static uint halfViewScreenH { get { return viewScreenH / 2; } }

        public static uint halfScreenW = screenW / 2;
        public static uint halfScreenH = screenH / 2;

        public static uint windowW;
        public static uint windowH;

        public static int viewSize = 1;

        public static void changeWindowSize(uint windowScale)
        {
            windowW = screenW * windowScale;
            windowH = screenH * windowScale;
            if (window != null)
            {
                window.Size = new Vector2u(windowW, windowH);
            }
        }

        public static void initMainWindow(Options options)
        {
            fullscreen = options.fullScreen;

            changeWindowSize(options.windowScale);

            renderTexture = new RenderTexture(screenW, screenH);

            screenRenderTextureS = new RenderTexture(screenW, screenH);
            srtBuffer1S = new RenderTexture(screenW, screenH);
            srtBuffer2S = new RenderTexture(screenW, screenH);

            screenRenderTextureL = new RenderTexture(screenW * 2, screenH * 2);
            srtBuffer1L = new RenderTexture(screenW * 2, screenH * 2);
            srtBuffer2L = new RenderTexture(screenW * 2, screenH * 2);

            var viewPort = new FloatRect(0, 0, 1, 1);

            if (!fullscreen)
            {
                window = new RenderWindow(new VideoMode(windowW, windowH), "MMX Online: Deathmatch");
                window.SetVerticalSyncEnabled(options.vsync);
                if (Global.hideMouse) window.SetMouseCursorVisible(false);
            }
            else
            {
                var desktopWidth = VideoMode.DesktopMode.Width;
                var desktopHeight = VideoMode.DesktopMode.Height;
                window = new RenderWindow(new VideoMode(desktopWidth, desktopHeight), "MMX Online: Deathmatch", Styles.Fullscreen);
                window.SetMouseCursorVisible(false);
                viewPort = getFullScreenViewPort();
            }

            var image = new Image(Global.assetPath + "assets/menu/icon.png");
            window.SetIcon(image.Size.X, image.Size.Y, image.Pixels);

            view = new View(new Vector2f(0, 0), new Vector2f(screenW, screenH));
            view.Viewport = viewPort;

            DrawWrappers.initHUD();
            DrawWrappers.hudView.Viewport = viewPort;

            window.SetView(view);
            if (Global.overrideFPS != null)
            {
                window.SetFramerateLimit((uint)Global.overrideFPS);
            }
            else
            {
                window.SetFramerateLimit((uint)options.maxFPS);
            }

            window.SetActive();
        }

        public static FloatRect getFullScreenViewPort()
        {
            float desktopWidth = VideoMode.DesktopMode.Width;
            float desktopHeight = VideoMode.DesktopMode.Height;
            float heightMultiple = (float)VideoMode.DesktopMode.Height / (float)screenH;

            if (Options.main.integerFullscreen)
            {
                heightMultiple = MathF.Floor((float)VideoMode.DesktopMode.Height / (float)screenH);
            }
            float extraWidthPercent = (desktopWidth - (float)screenW * heightMultiple) / desktopWidth;
            float extraHeightPercent = (desktopHeight - (float)screenH * heightMultiple) / desktopHeight;

            return new FloatRect(extraWidthPercent / 2f, extraHeightPercent / 2f, 1f - extraWidthPercent, 1f - extraHeightPercent);
        }
    }
}
