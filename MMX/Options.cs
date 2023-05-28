using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MMXOnline
{
    public class Options
    {
        public string playerName;
        public float musicVolume = 1;
        public float soundVolume = 1;
        public bool showWeaponHUD = true;
        public int? regionIndex;
        public bool logTelemetry = true;
        public bool showFPS = false;
        public bool showInGameMenuHUD = true;
        public bool showSysReqPrompt = true;
        public bool enableDeveloperConsole;
        public bool disableChat;
        public int maxFPS = 60;
        public bool cheatWarningShown;
        public bool disableDoubleDash;
        public int preferredCharacter;
        public bool showMashProgress;
        public bool useOptimizedAssets = false;
        public bool killOnLoadoutChange = true;
        public bool killOnCharChange = true;
        public float networkTimeoutSeconds = 3;
        public bool autoCreateDocFolderPromptShown = false;

        public int getNetworkTimeoutMs()
        {
            networkTimeoutSeconds = Helpers.clamp(networkTimeoutSeconds, 1, 5);
            return MathF.Round(networkTimeoutSeconds * 1000);
        }

        // Video settings
        public bool fullScreen = false;
        public bool integerFullscreen;
        public int? graphicsPreset = null;  // 0 = low, 1 = medium, 2 = high, 3 = custom
        public uint windowScale = 4;
        public bool disableShaders;
        public bool vsync;
        public bool areShadersDisabled()
        {
            if (Global.disableShaderOverride) return true;
            return disableShaders;
        }
        public int textQuality = 0;
        public int fontType = 0; // 0 = bitmap only, 1 = bitmap + vector, 2 = vector only
        public bool enablePostProcessing = true;
        public int particleQuality = 0;
        public bool enableMapSprites = true;

        public bool lowQualityParticles() { return particleQuality == 0; }

        public bool shouldUseOptimizedAssets()
        {
            return Global.useOptimizedAssetsOverride ?? useOptimizedAssets;
        }

        // X
        public int gridModeX;
        public int hyperChargeSlot;
        public bool novaStrikeSpecial;
        public bool gigaCrushSpecial;
        public XLoadout xLoadout = new XLoadout();

        // Zero
        public bool swapAirAttacks;
        public bool showGigaAttackCooldown;
        public ZeroLoadout zeroLoadout = new ZeroLoadout();

        // Vile
        public int weaponOrderingVile;
        public bool swapGoliathInputs;
        public bool blockMechSlotScroll;
        public bool mk5PuppeteerHoldOrToggle;
        public bool lockInAirCannon = true;
        public VileLoadout vileLoadout = new VileLoadout();

        // Axl
        public int gridModeAxl;
        public bool aimAnalog = false;
        public float aimSensitivity = 0.5f;
        public int axlAimMode = 0;
        public bool lockOnSound = true;
        public bool backwardsAimInvert = false;
        public bool axlSeparateAimDownAndCrouch;
        public bool moveInDiagAim = false;
        public int aimKeyFunction = 0;  //0 = aim backwards, 1 = lock position, 2 = lock aim
        public bool aimKeyToggle = false;
        public bool showRollCooldown;
        public AxlLoadout axlLoadout = new AxlLoadout();

        // Sigma
        public int sigmaWeaponSlot;
        public bool puppeteerHoldOrToggle;
        public SigmaLoadout sigmaLoadout = new SigmaLoadout();
        public bool maverickStartFollow;
        public bool puppeteerCancel;

        private static Options _main;

        public static Options main
        {
            get
            {
                if (_main == null)
                {
                    string text = Helpers.ReadFromFile("options.txt");
                    if (string.IsNullOrEmpty(text))
                    {
                        _main = new Options();
                    }
                    else
                    {
                        try
                        {
                            _main = JsonConvert.DeserializeObject<Options>(text);
                        }
                        catch
                        {
                            throw new Exception("Your options.txt file is corrupted, or does no longer work with this version. Please delete it and launch the game again.");
                        }
                    }

                    _main.validate();

                    if (Global.debug)
                    {
                        _main.axlAimMode = Global.overrideAimMode ?? _main.axlAimMode;
                        _main.fullScreen = Global.overrideFullscreen ?? _main.fullScreen;
                        _main.maxFPS = MathF.Clamp(_main.maxFPS, 30, Global.fpsCap);
                        _main.fontType = Global.fontTypeOverride ?? _main.fontType;
                    }
                }

                return _main;
            }
        }

        public void validate()
        {
            if (playerName != null && playerName.Length > Global.maxPlayerNameLength)
            {
                playerName = playerName.Substring(0, Global.maxPlayerNameLength);
            }
            playerName = Helpers.censor(playerName);
            playerName = Regex.Replace(playerName, @"[^\u0000-\u007F]+", "?"); //Remove non ASCII chars to prevent possible issues

            hyperChargeSlot = Helpers.clamp(hyperChargeSlot, 0, 2);
            sigmaWeaponSlot = Helpers.clamp(sigmaWeaponSlot, 0, 2);
            preferredCharacter = Helpers.clamp(preferredCharacter, 0, 4);

            xLoadout.validate();
            zeroLoadout.validate();
            vileLoadout.validate();
            axlLoadout.validate();
            sigmaLoadout.validate();
        }

        public static bool isValidLANIP(string LANIPPrefix)
        {
            if (!LANIPPrefix.EndsWith(".")) return false;
            string fullIP = LANIPPrefix + "1";
            return IPAddress.TryParse(fullIP, out _);
        }

        public string getSpecialAirAttack()
        {
            return !swapAirAttacks ? "zero_attack_air2" : "zero_attack_air";
        }

        public string getAirAttack()
        {
            if (Global.level?.mainPlayer?.isSigma == true) return "attack_air";
            return swapAirAttacks ? "attack_air2" : "attack_air";
        }

        public bool useMouseAim
        {
            get { return axlAimMode == 2; }
        }

        public void saveToFile()
        {
            string text = JsonConvert.SerializeObject(_main);
            Helpers.WriteToFile("options.txt", text);
        }

        public Region getRegion()
        {
            if (Global.regions == null || Global.regions.Count == 0) return null;
            if (regionIndex == null || regionIndex.Value >= Global.regions.Count)
            {
                regionIndex = 0;
            }
            return Global.regions[regionIndex.Value];
        }

        public Region getRegionOrDefault()
        {
            if (Global.regions == null || Global.regions.Count == 0) return null;
            if (regionIndex == null)
            {
                return Global.regions.ElementAtOrDefault(0);
            }
            if (regionIndex.Value >= Global.regions.Count)
            {
                regionIndex = 0;
            }
            return Global.regions[regionIndex.Value];
        }

        public bool isDeveloperConsoleEnabled()
        {
            if (Global.debug)
            {
                return true;
            }
            else
            {
                return enableDeveloperConsole;
            }
        }
    }
}
