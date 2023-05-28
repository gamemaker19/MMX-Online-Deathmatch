using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using static SFML.Window.Keyboard;

namespace MMXOnline
{
    public class OptionsMenu : IMainMenu
    {
        public int selectedArrowPosY;
        public IMainMenu previous;

        public int startY = 40;
        public const int lineH = 9;
        public const uint fontSize = 20;
        public const int presetYPos = 7;
        public int frames;

        public List<MenuOption> menuOptions;

        public float blinkTime = 0;
        public bool isChangingName;
        public string playerName;
        public bool inGame;
        public int? charNum;
        public bool isGraphics;

        public bool oldFullscreen;
        public uint oldWindowScale;
        public int oldMaxFPS;
        public bool oldDisableShaders;
        public bool oldEnablePostprocessing;
        public bool oldUseOptimizedAssets;
        private int oldParticleQuality;
        public bool oldIntegerFullscreen;
        public bool oldVsync;

        public OptionsMenu(IMainMenu mainMenu, bool inGame, int? charNum, bool isGraphics)
        {
            previous = mainMenu;
            this.inGame = inGame;
            this.isGraphics = isGraphics;

            oldIntegerFullscreen = Options.main.integerFullscreen;
            oldFullscreen = Options.main.fullScreen;
            oldWindowScale = Options.main.windowScale;
            oldDisableShaders = Options.main.disableShaders;
            oldMaxFPS = Options.main.maxFPS;
            oldEnablePostprocessing = Options.main.enablePostProcessing;
            oldUseOptimizedAssets = Options.main.useOptimizedAssets;
            oldParticleQuality = Options.main.particleQuality;
            oldVsync = Options.main.vsync;

            playerName = Options.main.playerName;
            this.charNum = charNum;

            if (!isGraphics && charNum == null)
            {
                startY = 35;
            }

            if (isGraphics)
            {
                menuOptions = new List<MenuOption>()
                {
                    // Full screen
                    new MenuOption(30, startY,
                        () =>
                        {
                            if (inGame) return;
                            if (Global.input.isPressedMenu(Control.MenuLeft))
                            {
                                if (Options.main.fullScreen)
                                {
                                    Options.main.fullScreen = false;
                                }
                            }
                            else if (Global.input.isPressedMenu(Control.MenuRight))
                            {
                                if (!Options.main.fullScreen)
                                {
                                    Options.main.fullScreen = true;
                                }
                            }
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "FULLSCREEN: " + (Options.main.fullScreen ? "Yes": "No"), pos.x, pos.y, fontSize: fontSize, color: getColor(), selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Set to Yes to make the game render fullscreen."),
                    
                    // Windowed resolution
                    new MenuOption(30, startY,
                        () =>
                        {
                            if (inGame) return;
                            if (Global.input.isPressedMenu(Control.MenuLeft))
                            {
                                Options.main.windowScale = (uint)Helpers.clamp((int)Options.main.windowScale - 1, 1, 6);
                            }
                            else if (Global.input.isPressedMenu(Control.MenuRight))
                            {
                                Options.main.windowScale = (uint)Helpers.clamp((int)Options.main.windowScale + 1, 1, 6);
                            }
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "WINDOWED RESOLUTION: " + getWindowedResolution(), pos.x, pos.y, fontSize: fontSize, color: getColor(), selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Change the windowed resolution of the game."),
                    
                    // Show FPS
                    new MenuOption(30, startY,
                        () =>
                        {
                            if (Global.input.isPressedMenu(Control.MenuLeft))
                            {
                                Options.main.showFPS = false;
                            }
                            else if (Global.input.isPressedMenu(Control.MenuRight))
                            {
                                Options.main.showFPS = true;
                            }
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "SHOW FPS: " + Helpers.boolYesNo(Options.main.showFPS), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Show the frames per second (FPS) in the bottom right."),

                    // Lock FPS
                    new MenuOption(30, startY,
                        () =>
                        {
                            if (inGame) return;
                            if (Global.input.isHeldMenu(Control.MenuLeft))
                            {
                                Options.main.maxFPS--;
                                if (Options.main.maxFPS < 30) Options.main.maxFPS = 30;
                            }
                            else if (Global.input.isHeldMenu(Control.MenuRight))
                            {
                                Options.main.maxFPS++;
                                if (Options.main.maxFPS > Global.fpsCap) Options.main.maxFPS = Global.fpsCap;
                            }
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "MAX FPS: " + Options.main.maxFPS.ToString(), pos.x, pos.y, fontSize: fontSize, color: getColor(), selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Controls the max framerate the game can run.\nLower values are more choppy but use less CPU."),
                    
                    // VSYNC
                     new MenuOption(30, startY,
                        () =>
                        {
                            if (inGame) return;
                            Helpers.menuLeftRightBool(ref Options.main.vsync);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "ENABLE VSYNC: " + Helpers.boolYesNo(Options.main.vsync), pos.x, pos.y, fontSize: fontSize, color: getColor(), selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Set to Yes to enable vsync.\nMakes movement/scrolling smoother, but adds input lag."),

                    // Use optimized sprites
                    new MenuOption(30, startY,
                        () =>
                        {
                            if (inGame) return;
                            Helpers.menuLeftRightBool(ref Options.main.useOptimizedAssets);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "USE OPTIMIZED ASSETS: " + Helpers.boolYesNo(Options.main.useOptimizedAssets), pos.x, pos.y, fontSize: fontSize, color: getColor(), selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Set to Yes to use optimized assets.\nThis can result in better performance."),
                    
                    // Full screen integer
                     new MenuOption(30, startY,
                        () =>
                        {
                            if (inGame) return;
                            Helpers.menuLeftRightBool(ref Options.main.integerFullscreen);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "FULLSCREEN ROUNDING: " + Helpers.boolYesNo(Options.main.integerFullscreen), pos.x, pos.y, fontSize: fontSize, color: getColor(), selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Set to Yes to round down fullscreen pixels to the nearest integer.\nReduces distortion on lower resolution monitors."),

                    // preset
                    new MenuOption(30, startY,
                        () =>
                        {
                            if (inGame) return;
                            if (Global.input.isPressedMenu(Control.MenuLeft))
                            {
                                if (Options.main.graphicsPreset > 0)
                                {
                                    Options.main.graphicsPreset--;
                                    setPresetQuality(Options.main.graphicsPreset.Value);
                                }
                            }
                            else if (Global.input.isPressedMenu(Control.MenuRight))
                            {
                                if (Options.main.graphicsPreset < 3)
                                {
                                    Options.main.graphicsPreset++;
                                    setPresetQuality(Options.main.graphicsPreset.Value);
                                }
                            }
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "PRESET QUALITY: " + qualityToString(Options.main.graphicsPreset.Value), pos.x, pos.y, fontSize: fontSize, color: getColor(), selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Choose a pre-configured set of graphics settings."),

                    // Shaders
                    new MenuOption(40, startY,
                        () =>
                        {
                            if (inGame) return;
                            if (Options.main.graphicsPreset < 3) return;
                            if (Global.input.isPressedMenu(Control.MenuLeft))
                            {
                                Options.main.disableShaders = true;
                            }
                            else if (Global.input.isPressedMenu(Control.MenuRight))
                            {
                                Options.main.disableShaders = false;
                            }
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "ENABLE SHADERS: " + Helpers.boolYesNo(!Options.main.disableShaders), pos.x, pos.y, fontSize: fontSize, color: getVideoSettingColor(), selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Enables special effects like weapon palettes.\nNot all PCs support this."),

                    // Post processing
                    new MenuOption(40, startY,
                        () =>
                        {
                            if (inGame) return;
                            if (Options.main.graphicsPreset < 3) return;
                            Helpers.menuLeftRightBool(ref Options.main.enablePostProcessing);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "ENABLE POST PROCESSING: " + Helpers.boolYesNo(Options.main.enablePostProcessing), pos.x, pos.y, fontSize: fontSize, color: getVideoSettingColor(), selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Enables special screen distortion effects.\nNot all PCs support this."),

                    // fontType
                    new MenuOption(40, startY,
                        () =>
                        {
                            if (inGame) return;
                            if (Options.main.graphicsPreset < 3) return;
                            Helpers.menuLeftRightInc(ref Options.main.fontType, 0, 2);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "FONT TYPE: " + fontTypeToString(Options.main.fontType), pos.x, pos.y, fontSize: fontSize, color: getVideoSettingColor(), selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Set the font type. Bitmap uses PNG, Vector uses TFF.\nHybrid will use Bitmap in menus and Vector in-game."),

                    // particleQuality
                    new MenuOption(40, startY,
                        () =>
                        {
                            if (inGame) return;
                            if (Options.main.graphicsPreset < 3) return;
                            Helpers.menuLeftRightInc(ref Options.main.particleQuality, 0, 2);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "PARTICLE QUALITY: " + qualityToString(Options.main.particleQuality), pos.x, pos.y, fontSize: fontSize, color: getVideoSettingColor(), selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Set the particle effect quality.\nLower quality results in faster performance."),

                    // map sprites
                    new MenuOption(40, startY,
                        () =>
                        {
                            if (inGame) return;
                            if (Options.main.graphicsPreset < 3) return;
                            Helpers.menuLeftRightBool(ref Options.main.enableMapSprites);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "ENABLE MAP SPRITES: " + Helpers.boolYesNo(Options.main.enableMapSprites), pos.x, pos.y, fontSize: fontSize, color: getVideoSettingColor(), selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Enable or disable map sprites.\nDisabling map sprites results in faster performance."),
                };
            }
            else if (charNum == null)
            {
                if (!Global.regionPingTask.IsCompleted)
                {
                    Global.regionPingTask.Wait();
                }

                menuOptions = new List<MenuOption>()
                {
                    // Music volume
                    new MenuOption(30, startY,
                        () =>
                        {
                            if (Global.input.isHeldMenu(Control.MenuLeft))
                            {
                                Options.main.musicVolume = Helpers.clamp(Options.main.musicVolume - 0.01f, 0, 1);
                                Global.music.updateVolume();
                            }
                            else if (Global.input.isHeldMenu(Control.MenuRight))
                            {
                                Options.main.musicVolume = Helpers.clamp(Options.main.musicVolume + 0.01f, 0, 1);
                                Global.music.updateVolume();
                            }
                        },
                        (Point pos, int index) =>
                        {
                            var musicVolume100 = (int)Math.Round(Options.main.musicVolume * 100);
                            Helpers.drawTextStd(TCat.Option, "MUSIC VOLUME: " + musicVolume100.ToString(), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Adjust the game music volume."),

                    // Sound volume
                    new MenuOption(30, startY,
                        () =>
                        {
                            if (Global.input.isHeldMenu(Control.MenuLeft))
                            {
                                Options.main.soundVolume = Helpers.clamp(Options.main.soundVolume - 0.01f, 0, 1);
                            }
                            else if (Global.input.isHeldMenu(Control.MenuRight))
                            {
                                Options.main.soundVolume = Helpers.clamp(Options.main.soundVolume + 0.01f, 0, 1);
                            }
                        },
                        (Point pos, int index) =>
                        {
                            var soundVolume100 = (int)Math.Round(Options.main.soundVolume * 100);
                            Helpers.drawTextStd(TCat.Option, "SOUND VOLUME: " + soundVolume100.ToString(), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Adjust the game sound volume."),

                    // Multiplayer Name
                    new MenuOption(30, startY,
                        () =>
                        {
                            if (inGame) return;
                            if (!isChangingName && (Global.input.isPressedMenu(Control.MenuLeft) || Global.input.isPressedMenu(Control.MenuRight) || Global.input.isPressedMenu(Control.MenuSelectPrimary)))
                            {
                                isChangingName = true;
                            }
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "MULTIPLAYER NAME: " + playerName, pos.x, pos.y, fontSize: fontSize, color: getColor(), selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Your name that appears to others when you play online."),

                    /*
                    // Multiplayer region
                    new MenuOption(30, startY,
                        () =>
                        {
                            if (inGame) return;
                            if (Global.input.isPressedMenu(Control.MenuLeft))
                            {
                                Options.main.regionIndex--;
                                if (Options.main.regionIndex < 0) Options.main.regionIndex = 0;
                            }
                            else if (Global.input.isPressedMenu(Control.MenuRight))
                            {
                                Options.main.regionIndex++;
                                if (Options.main.regionIndex > Global.regions.Count - 1) Options.main.regionIndex = Global.regions.Count - 1;
                            }
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "MULTIPLAYER REGION: " + Options.main.getRegion().name + (" (" + Options.main.getRegion().getDisplayPing() + " ping)"), 
                                pos.x, pos.y, fontSize: fontSize, color: isRegionDisabled() ? Helpers.Gray : Color.White, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Preferred server region for hosting matches.\nChoose the one with lowest ping."),
                    */

                    // Preferred character
                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightInc(ref Options.main.preferredCharacter, 0, 4);
                        },
                        (Point pos, int index) =>
                        {
                            string preferredChar = Character.charDisplayNames[Options.main.preferredCharacter];
                            Helpers.drawTextStd(TCat.Option, "PREFERRED CHARACTER: " + preferredChar, pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Choose a default character the game will\npre-select for you."),

                    // Hide Menu Helper Text
                    new MenuOption(30, startY,
                        () =>
                        {
                            if (Global.input.isPressedMenu(Control.MenuLeft))
                            {
                                Options.main.showInGameMenuHUD = false;
                            }
                            else if (Global.input.isPressedMenu(Control.MenuRight))
                            {
                                Options.main.showInGameMenuHUD = true;
                            }
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "SHOW IN-GAME MENU KEYS: " + Helpers.boolYesNo(Options.main.showInGameMenuHUD), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Show or hide additional menu help text in\nbottom right of the in-match HUD."),

                    // System requirements check
                    new MenuOption(30, startY,
                        () =>
                        {
                            if (Global.input.isPressedMenu(Control.MenuLeft))
                            {
                                Options.main.showSysReqPrompt = false;
                            }
                            else if (Global.input.isPressedMenu(Control.MenuRight))
                            {
                                Options.main.showSysReqPrompt = true;
                            }
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "SHOW STARTUP WARNINGS: " + Helpers.boolYesNo(Options.main.showSysReqPrompt), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "On launch, check for system requirements\nand other startup warnings."),

                    // Disable Chat
                    new MenuOption(30, startY,
                        () =>
                        {
                            if (Global.input.isHeldMenu(Control.MenuLeft))
                            {
                                Options.main.disableChat = false;
                            }
                            else if (Global.input.isHeldMenu(Control.MenuRight))
                            {
                                Options.main.disableChat = true;
                            }
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "DISABLE CHAT: " + Helpers.boolYesNo(Options.main.disableChat), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Set to Yes to disable sending and receiving\nchat messages in online matches."),

                    // Double dash
                    new MenuOption(30, startY,
                        () =>
                        {
                            if (Global.input.isHeldMenu(Control.MenuLeft))
                            {
                                Options.main.disableDoubleDash = false;
                            }
                            else if (Global.input.isHeldMenu(Control.MenuRight))
                            {
                                Options.main.disableDoubleDash = true;
                            }
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "DISABLE DOUBLE TAP DASH: " + Helpers.boolYesNo(Options.main.disableDoubleDash), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Disables ability to dash by quickly\ntapping LEFT or RIGHT twice."),

                    // Mash progress
                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightBool(ref Options.main.showMashProgress);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "SHOW MASH PROGRESS: " + Helpers.boolYesNo(Options.main.showMashProgress), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "When hit by moves that can be mashed out of, like grabs,\nshows the mash progress above your head."),

                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightBool(ref Options.main.killOnLoadoutChange);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "KILL ON LOADOUT CHANGE: " + Helpers.boolYesNo(Options.main.killOnLoadoutChange), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "If Yes, will instantly die on loadout change mid-match.\nIf No, on next death loadout changes will apply."),

                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightBool(ref Options.main.killOnCharChange);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "KILL ON CHARACTER CHANGE: " + Helpers.boolYesNo(Options.main.killOnCharChange), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "If Yes, will instantly die on character change mid-match.\nIf No, on next death character change will apply."),

                    new MenuOption(30, startY,
                        () =>
                        {
                            if (Global.input.isHeldMenu(Control.MenuLeft))
                            {
                                Options.main.networkTimeoutSeconds = Helpers.clamp(Options.main.networkTimeoutSeconds - 0.1f, 1, 5);
                            }
                            else if (Global.input.isHeldMenu(Control.MenuRight))
                            {
                                Options.main.networkTimeoutSeconds = Helpers.clamp(Options.main.networkTimeoutSeconds + 0.1f, 1, 5);
                            }
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "MATCHMAKING TIMEOUT: " + Options.main.networkTimeoutSeconds.ToString("0.0") + " seconds", pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "How long match search will take before erroring out.\nIf always erroring out in match search, try increasing this."),

                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightBool(ref Options.main.enableDeveloperConsole);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "ENABLE DEV CONSOLE: " + Helpers.boolYesNo(Options.main.enableDeveloperConsole), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "If enabled, press BACKQUOTE to open the dev-console in-match.\nSee the game website for a list of commands."),
                };
            }
            else if (charNum == 0)
            {
                menuOptions = new List<MenuOption>()
                {
                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightInc(ref Options.main.gridModeX, 0, 2);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "WEAPON SWITCH GRID MODE: " + gridModeToStr(Options.main.gridModeX), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Enable Grid Mode for weapon switch in certain or all modes.\nIn Grid Mode, hold WEAPON L/R and use ARROW KEYS to switch weapon."),

                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightInc(ref Options.main.hyperChargeSlot, 0, 2);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "HYPER CHARGE SLOT: " + (Options.main.hyperChargeSlot + 1), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Weapon slot number which Hyper Charge uses."),

                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightBool(ref Options.main.gigaCrushSpecial);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "G.CRUSH DOWN SPECIAL: " + Helpers.boolYesNo(Options.main.gigaCrushSpecial), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "If Yes, you can perform Giga Crush by pressing DOWN + SPECIAL,\nbut you lose the ability to switch to Giga Crush manually."),
                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightBool(ref Options.main.novaStrikeSpecial);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "N.STRIKE SIDE SPECIAL: " + Helpers.boolYesNo(Options.main.novaStrikeSpecial), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "If Yes, you can perform Nova Strike by pressing ARROW KEY + SPECIAL,\nbut you lose the ability to switch to Nova Strike manually."),
                    /*
                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightInc(ref Options.main.xSpecialOrdering, 0, 5);
                        },
                        (Point pos, int index) =>
                        {
                            string s = "H.Buster,G.Crush,N.Strike";
                            if (Options.main.xSpecialOrdering == 1) s = "G.Crush,H.Buster,N.Strike";
                            if (Options.main.xSpecialOrdering == 2) s = "G.Crush,N.Strike,H.Buster";
                            if (Options.main.xSpecialOrdering == 3) s = "H.Buster,N.Strike,G.Crush";
                            if (Options.main.xSpecialOrdering == 4) s = "N.Strike,G.Crush,H.Buster";
                            if (Options.main.xSpecialOrdering == 5) s = "N.Strike,H.Buster,G.Crush";
                            Helpers.drawTextStd(TCat.Option, "Special Slot Order: " + s, pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        })
                    */
                };
            }
            else if (charNum == 1)
            {
                menuOptions = new List<MenuOption>()
                {
                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightBool(ref Options.main.swapAirAttacks);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "SWAP AIR ATTACKS: " + Helpers.boolYesNo(Options.main.swapAirAttacks), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "You can swap the inputs for air slash attack (default ATTACK),\nand Kuuenbu (air spin attack, default SPECIAL)."),
                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightBool(ref Options.main.showGigaAttackCooldown);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "SHOW GIGA COOLDOWN: " + Helpers.boolYesNo(Options.main.showGigaAttackCooldown), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "If Yes, shows a cooldown circle of giga moves like Rakuhouha."),
                };

            }
            else if (charNum == 2)
            {
                menuOptions = new List<MenuOption>()
                {
                    // Swap goliath inputs
                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightBool(ref Options.main.swapGoliathInputs);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "SWAP GOLIATH SHOOT: " + Helpers.boolYesNo(Options.main.swapGoliathInputs), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "You can swap the inputs for Goliath buster (default WEAPON L/R),\nand missiles (default SPECIAL)."),

                    // Block ride armor scroll
                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightBool(ref Options.main.blockMechSlotScroll);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "BLOCK MECH SCROLL: " + Helpers.boolYesNo(Options.main.blockMechSlotScroll), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Prevents ability to scroll to the Ride Armor slot.\nYou will only be able to switch to it by pressing 3."),

                    // Weapon Ordering
                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightInc(ref Options.main.weaponOrderingVile, 0, 1);
                        },
                        (Point pos, int index) =>
                        {
                            string s = "F.Runner,Vulcan,R.Armors";
                            if (Options.main.weaponOrderingVile == 1) s = "Vulcan,F.Runner,R.Armors";
                            Helpers.drawTextStd(TCat.Option, "WEAPON ORDER: " + s, pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Choose the order in which Vile's weapons are arranged."),

                    // MK5 Ride control
                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightBool(ref Options.main.mk5PuppeteerHoldOrToggle);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "VILE V RIDE CONTROL: " + (Options.main.mk5PuppeteerHoldOrToggle ? "Hold" : "Toggle"), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "If set to Hold, Vile V will control the Mech as long as\nWEAPON L/R is held."),

                    // Lock Cannon Air
                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightBool(ref Options.main.lockInAirCannon);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "LOCK IN AIR CANNON: " + (Options.main.lockInAirCannon? "Yes" : "No"), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "If No, Front Runner and Fat Boy cannons will not\nroot Vile in the air when shot."),
                };
            }
            else if (charNum == 3)
            {
                menuOptions = new List<MenuOption>()
                {
                    // Axl Use Mouse Aim
                    new MenuOption(30, startY,
                        () =>
                        {
                            if (Global.input.isPressedMenu(Control.MenuLeft))
                            {
                                Options.main.axlAimMode = 0;
                            }
                            else if (Global.input.isPressedMenu(Control.MenuRight))
                            {
                                Options.main.axlAimMode = 2;
                            }
                        },
                        (Point pos, int index) =>
                        {
                            string aimMode = "Directional";
                            if (Options.main.axlAimMode == 1) aimMode = "Directional";
                            else if (Options.main.axlAimMode == 2) aimMode = "Cursor";
                            Helpers.drawTextStd(TCat.Option, "AIM MODE: " + aimMode, pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Change Axl's aim controls to either use\nARROW KEYS (Directional) or mouse aim (Cursor)."),

                    // Axl Mouse sensitivity
                    new MenuOption(30, startY,
                        () =>
                        {
                            if (Global.input.isHeldMenu(Control.MenuLeft))
                            {
                                Options.main.aimSensitivity = Helpers.clamp(Options.main.aimSensitivity - 0.01f, 0, 1);
                            }
                            else if (Global.input.isHeldMenu(Control.MenuRight))
                            {
                                Options.main.aimSensitivity = Helpers.clamp(Options.main.aimSensitivity + 0.01f, 0, 1);
                            }
                        },
                        (Point pos, int index) =>
                        {
                            var setting100 = (int)Math.Round(Options.main.aimSensitivity * 100);
                            Helpers.drawTextStd(TCat.Option, "AIM SENSITIVITY: " + setting100.ToString(), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Change aim sensitivity (for Cursor aim mode only.)"),

                    // Axl Lock On
                    new MenuOption(30, startY,
                        () =>
                        {
                            if (Global.input.isHeldMenu(Control.MenuLeft))
                            {
                                Options.main.lockOnSound = false;
                            }
                            else if (Global.input.isHeldMenu(Control.MenuRight))
                            {
                                Options.main.lockOnSound = true;
                            }
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "AUTO AIM: " + Helpers.boolYesNo(Options.main.lockOnSound), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Enable/disable auto-aim (for Directional aim mode only.)"),

                    // Axl Backwards Aim Invert
                    new MenuOption(30, startY,
                        () =>
                        {
                            if (Global.input.isHeldMenu(Control.MenuLeft))
                            {
                                Options.main.aimAnalog = false;
                            }
                            else if (Global.input.isHeldMenu(Control.MenuRight))
                            {
                                Options.main.aimAnalog = true;
                            }
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "ANALOG STICK AIM: " + Helpers.boolYesNo(Options.main.aimAnalog), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "If Yes, enables 360 degree aim if binding Axl aim controls\nto a controller analog stick."),
                    
                    // Aim key function
                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightInc(ref Options.main.aimKeyFunction, 0, 2);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "AIM KEY FUNCTION: " + aimKeyFunctionToStr(Options.main.aimKeyFunction), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Change the behavior of Axl's \"aim key\" (default SHIFT)."),

                    // Aim key toggle
                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightBool(ref Options.main.aimKeyToggle);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "AIM KEY BEHAVIOR: " + (Options.main.aimKeyToggle ? "Toggle" : "Hold"), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Change whether Axl's \"aim key\" is toggle or hold based."),

                    // Diag aim movement
                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightBool(ref Options.main.moveInDiagAim);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "MOVE IN DIAGONAL AIM: " + Helpers.boolYesNo(Options.main.moveInDiagAim), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "If Yes, Axl can move when aiming diagonally, otherwise he is\nlocked in place."),

                    // Axl Separate aim crouch
                    new MenuOption(30, startY,
                        () =>
                        {
                            if (Global.input.isHeldMenu(Control.MenuLeft))
                            {
                                Options.main.axlSeparateAimDownAndCrouch = false;
                            }
                            else if (Global.input.isHeldMenu(Control.MenuRight))
                            {
                                Options.main.axlSeparateAimDownAndCrouch = true;
                            }
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "SEPARATE AIM DOWN & CROUCH: " + Helpers.boolYesNo(Options.main.axlSeparateAimDownAndCrouch), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "If Yes, aim down and crouch bindings will not conflict,\nbut you will need to bind the Axl Crouch control to something else."),

                    // Grid mode Axl
                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightInc(ref Options.main.gridModeAxl, 0, 2);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "WEAPON SWITCH GRID MODE: " + gridModeToStr(Options.main.gridModeAxl), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Enables Grid Mode for Axl, which works the same way as X's."),

                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightBool(ref Options.main.showRollCooldown);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "SHOW ROLL COOLDOWN: " + Helpers.boolYesNo(Options.main.showRollCooldown), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "If enabled, shows a cooldown circle above Axl's head\nafter Dodge Roll is used, indicating time until next available roll."),

                };
            }
            else if (charNum == 4)
            {
                menuOptions = new List<MenuOption>()
                {
                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightInc(ref Options.main.sigmaWeaponSlot, 0, 2);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "SIGMA SLOT: " + (Options.main.sigmaWeaponSlot + 1).ToString(), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Changes the position of the Sigma slot in Sigma's hotbar."),
                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightBool(ref Options.main.puppeteerHoldOrToggle);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "PUPPETEER CONTROL: " + (Options.main.puppeteerHoldOrToggle ? "Hold" : "Toggle"), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "If set to Hold, Puppeteer Sigma will control a Maverick as long as\nWEAPON L/R is held."),
                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightBool(ref Options.main.maverickStartFollow);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "MAVERICK START MODE: " + (Options.main.maverickStartFollow ? "Follow" : "Hold Position"), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "Change whether Mavericks will follow Sigma, or hold position,\nafter summoned."),
                    new MenuOption(30, startY,
                        () =>
                        {
                            Helpers.menuLeftRightBool(ref Options.main.puppeteerCancel);
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Option, "PUPPETEER CANCEL: " + (Options.main.puppeteerCancel ? "Yes" : "No"), pos.x, pos.y, fontSize: fontSize, selected: selectedArrowPosY == index, optionPadding: 26);
                        },
                        "If set to Yes, Mavericks will revert to their idle state\nwhen switched to in Puppeteer mode."),
                };
            }

            for (int i = 0; i < menuOptions.Count; i++)
            {
                menuOptions[i].pos.y = startY + lineH * i;
            }
        }

        private string fontTypeToString(int fontType)
        {
            if (fontType == 0) return "Bitmap";
            else if (fontType == 1) return "Hybrid";
            else return "Vector";
        }

        public static void setPresetQuality(int graphicsPreset)
        {
            if (graphicsPreset >= 3) return;
            Options.main.graphicsPreset = graphicsPreset;
            Options.main.fontType = 0;  //(graphicsPreset == 0 ? 0 : 1);
            Options.main.particleQuality = graphicsPreset;
            Options.main.enablePostProcessing = (graphicsPreset > 0);
            Options.main.disableShaders = (graphicsPreset == 0);
            Options.main.useOptimizedAssets = (graphicsPreset <= 1);
            Options.main.enableMapSprites = (graphicsPreset > 0);
            Options.main.saveToFile();
        }

        public static void inferPresetQuality(uint textureSize)
        {
            string presetMessage = "Based on your detected video card texture size of {0}, a Graphics setting of {1} has been automatically selected.\n\nYou can change this in the Settings menu.";
            if (textureSize <= 1024)
            {
                setPresetQuality(0);
                //Helpers.showMessageBox(string.Format(presetMessage, textureSize, "Low"), "Graphics settings preset");
            }
            else if (textureSize <= 2048)
            {
                setPresetQuality(1);
                //Helpers.showMessageBox(string.Format(presetMessage, textureSize, "Medium"), "Graphics settings preset");
            }
            else
            {
                setPresetQuality(2);
                //Helpers.showMessageBox(string.Format(presetMessage, textureSize, "High"), "Graphics settings preset");
            }
        }

        private string qualityToString(int quality)
        {
            if (quality == 0) return "Low";
            else if (quality == 1) return "Medium";
            else if (quality == 2) return "High";
            else return "Custom";
        }

        private string aimKeyFunctionToStr(int aimKeyFunction)
        {
            if (aimKeyFunction == 0) return "Aim backwards/backpedal";
            else if (aimKeyFunction == 1) return "Lock position";
            else return "Lock aim";
        }

        string gridModeToStr(int gridMode)
        {
            if (gridMode == 0) return "No";
            if (gridMode == 1) return "1v1 Only";
            if (gridMode == 2) return "Always";
            return "Error";
        }

        public bool isRegionDisabled()
        {
            if (inGame) return true;
            return Global.regions == null || Global.regions.Count < 2;
        }

        public Color getColor()
        {
            return inGame ? Helpers.Gray : Color.White;
        }

        public Color getVideoSettingColor()
        {
            if (Options.main.graphicsPreset < 3) return Helpers.Gray;
            return inGame ? Helpers.Gray : Color.White;
        }

        public void update()
        {
            if (!isGraphics && charNum == null)
            {
                frames++;
                if (frames > 240)
                {
                    frames = 0;
                    Global.updateRegionPings();
                }
            }

            if (isChangingName)
            {
                blinkTime += Global.spf;
                if (blinkTime >= 1f) blinkTime = 0;

                playerName = Helpers.getTypedString(playerName, Global.maxPlayerNameLength);

                if (Global.input.isPressed(Key.Enter) && !string.IsNullOrWhiteSpace(playerName.Trim()))
                {
                    isChangingName = false;
                    Options.main.playerName = Helpers.censor(playerName).Trim();
                    Options.main.saveToFile();
                }

                return;
            }

            if (Global.input.isPressedMenu(Control.MenuUp))
            {
                selectedArrowPosY--;
                if (selectedArrowPosY < 0)
                {
                    selectedArrowPosY = menuOptions.Count - 1;
                    if (isGraphics && Options.main.graphicsPreset < 3) selectedArrowPosY = presetYPos;
                }
                Global.playSound("menu");
            }
            else if (Global.input.isPressedMenu(Control.MenuDown))
            {
                selectedArrowPosY++;
                if (selectedArrowPosY > menuOptions.Count - 1)
                {
                    selectedArrowPosY = 0;
                }
                if (isGraphics && Options.main.graphicsPreset < 3 && selectedArrowPosY > presetYPos) selectedArrowPosY = 0;
                Global.playSound("menu");
            }

            menuOptions[selectedArrowPosY].update();
            helpText = menuOptions[selectedArrowPosY].configureMessage;

            if (Global.input.isPressedMenu(Control.MenuBack))
            {
                if ((Options.main.xLoadout.weapon1 == Options.main.xLoadout.weapon2 && Options.main.xLoadout.weapon1 >= 0) || 
                    (Options.main.xLoadout.weapon1 == Options.main.xLoadout.weapon3 && Options.main.xLoadout.weapon2 >= 0) || 
                    (Options.main.xLoadout.weapon2 == Options.main.xLoadout.weapon3 && Options.main.xLoadout.weapon3 >= 0))
                {
                    Menu.change(new ErrorMenu(new string[] { "Error: same weapon selected twice" }, this));
                    return;
                }

                if (Options.main.axlLoadout.weapon2 == Options.main.axlLoadout.weapon3)
                {
                    Menu.change(new ErrorMenu(new string[] { "Error: same weapon selected twice" }, this));
                    return;
                }

                Options.main.saveToFile();

                /*
                if (oldWindowScale != Options.main.windowScale)
                {
                    Global.changeWindowSize(Options.main.windowScale);
                }
                */

                if (oldFullscreen != Options.main.fullScreen || oldWindowScale != Options.main.windowScale || oldMaxFPS != Options.main.maxFPS ||  oldDisableShaders != Options.main.disableShaders || oldEnablePostprocessing != Options.main.enablePostProcessing || 
                    oldUseOptimizedAssets != Options.main.useOptimizedAssets || oldParticleQuality != Options.main.particleQuality || oldIntegerFullscreen != Options.main.integerFullscreen || oldVsync != Options.main.vsync)
                {
                    Menu.change(new ErrorMenu(new string[] { "Note: options were changed that", "require restart to apply." }, previous));
                }
                else
                {
                    Menu.change(previous);
                }
            }
        }

        public string getWindowedResolution()
        {
            if (Options.main.windowScale == 1) return "298x224";
            else if (Options.main.windowScale == 2) return "596x448";
            else if (Options.main.windowScale == 3) return "894x672";
            else if (Options.main.windowScale == 4) return "1192x896";
            else if (Options.main.windowScale == 5) return "1490x1120";
            else if (Options.main.windowScale == 6) return "1788x1344";
            else throw new Exception("Invalid window scale.");
        }

        public string helpText = "";
        public void render()
        {
            float cursorPos = 20;
            if (isGraphics && selectedArrowPosY > presetYPos)
            {
                cursorPos = 32;
            }
            if (!inGame)
            {
                DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
                DrawWrappers.DrawTextureHUD(Global.textures["cursor"], cursorPos, startY + (selectedArrowPosY * lineH) - 2);
            }
            else
            {
                DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, Helpers.MenuBgColor, 0, ZIndex.HUD + 200, false);
                Global.sprites["cursor"].drawToHUD(0, cursorPos, startY + (selectedArrowPosY * lineH) + 3);
            }

            string subtitle = "GENERAL SETTINGS";
            if (isGraphics) subtitle = "GRAPHICS SETTINGS";
            else if (charNum == 0) subtitle = "X SETTINGS";
            else if (charNum == 1) subtitle = "ZERO SETTINGS";
            else if (charNum == 2) subtitle = "VILE SETTINGS";
            else if (charNum == 3) subtitle = "AXL SETTINGS";
            else if (charNum == 4) subtitle = "SIGMA SETTINGS";
            Helpers.drawTextStd(TCat.Title, subtitle, Global.halfScreenW, 15, Alignment.Center, fontSize: 32);
            Helpers.drawTextStd(TCat.BotHelp, "Left/Right: Change", Global.halfScreenW, 200, Alignment.Center, fontSize: 24);
            Helpers.drawTextStd(TCat.BotHelp, "[Z]: Save and Back", Global.halfScreenW, 210, Alignment.Center, fontSize: 24);

            for (int i = 0; i < menuOptions.Count; i++)
            {
                menuOptions[i].render(menuOptions[i].pos, i);
            }

            float rectY = 180;
            if (!string.IsNullOrEmpty(helpText))
            {
                DrawWrappers.DrawRect(10, rectY - 15, Global.screenW - 10, rectY + 15, true, new Color(0, 0, 0, 224), 0.5f, ZIndex.HUD, false, outlineColor: Color.White);
                float yOff = 0;
                if (Options.main.fontType == 2) yOff = -5;
                Helpers.drawTextStd(helpText, Global.halfScreenW, rectY + yOff, alignment: Alignment.Center, vAlignment: VAlignment.Center, fontSize: 16, style: Text.Styles.Italic, lineMargin: 24);
            }

            if (isChangingName)
            {
                float top = Global.screenH * 0.4f;

                //DrawWrappers.DrawRect(5, top - 20, Global.screenW - 5, top + 60, true, new Color(0, 0, 0, 224), 0, ZIndex.HUD, false);
                DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, new Color(0, 0, 0, 224), 0, ZIndex.HUD, false);
                Helpers.drawTextStd("Type in a multiplayer name", Global.screenW / 2, top, alignment: Alignment.Center);

                float xPos = Global.screenW * 0.33f;
                Helpers.drawTextStd(playerName, xPos, 20 + top, alignment: Alignment.Left);
                if (blinkTime >= 0.5f)
                {
                    float width = Helpers.measureTextStd(TCat.Default, playerName).x;
                    Helpers.drawTextStd("<", xPos + width + 3, 20 + top, alignment: Alignment.Left);
                }

                Helpers.drawTextStd("Press Enter to continue", Global.screenW / 2, 40 + top, alignment: Alignment.Center);
            }
        }
    }
}
