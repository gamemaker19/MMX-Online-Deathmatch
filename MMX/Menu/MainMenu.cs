using Newtonsoft.Json;
using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SFML.Window.Keyboard;

namespace MMXOnline
{
    public class MainMenu : IMainMenu
    {
        public const int startPos = 105;
        public const int yDistance = 13;

        public int selectY;
        public Point optionPos1 = new Point(90, startPos);
        public Point optionPos2 = new Point(90, startPos + yDistance);
        public Point optionPos3 = new Point(90, startPos + yDistance*2);
        public Point optionPos4 = new Point(90, startPos + yDistance*3);
        public Point optionPos5 = new Point(90, startPos + yDistance*4);
        public Point optionPos6 = new Point(90, startPos + yDistance*5);
        public Point optionPos7 = new Point(90, startPos + yDistance*6);

        public float blinkTime = 0;

        public string playerName = "";
        public int state;

        public MainMenu()
        {
            if (string.IsNullOrWhiteSpace(Options.main.playerName))
            {
                state = 0;
            }
            else if (Options.main.regionIndex == null)
            {
                state = 1;
            }
            else
            {
                state = 3;
            }
        }

        float state1Time;
        public void update()
        {
            if (state == 0)
            {
                blinkTime += Global.spf;
                if (blinkTime >= 1f) blinkTime = 0;

                playerName = Helpers.getTypedString(playerName, Global.maxPlayerNameLength);

                if (Global.input.isPressed(Key.Enter) && !string.IsNullOrWhiteSpace(playerName.Trim()))
                {
                    Options.main.playerName = Helpers.censor(playerName).Trim();
                    Options.main.saveToFile();
                    state = 1;
                }
                return;
            }
            else if (state == 1)
            {
                state = 3;
                return;
            }

            if (Global.input.isPressed(Key.F1))
            {
                Menu.change(new TextExportMenu(new string[] { "Below is your checksum version:" }, "checksum", Global.checksum, this));
                return;
            }

            Helpers.menuUpDown(ref selectY, 0, 6);

            if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
            {
                // Before joining or creating make sure client is up to date
                if (selectY == 0 || selectY == 1)
                {
                    Menu.change(new PreJoinOrHostMenu(this, selectY == 0));
                }
                else if (selectY == 2)
                {
                    Menu.change(new HostMenu(this, null, true, true));
                }
                else if (selectY == 3)
                {
                    Menu.change(new PreLoadoutMenu(this));
                }
                else if (selectY == 4)
                {
                    Menu.change(new PreControlMenu(this, false));
                }
                else if (selectY == 5)
                {
                    Menu.change(new PreOptionsMenu(this, false));
                }
                else if (selectY == 6)
                {
                    System.Environment.Exit(1);
                }
            }

            if (Global.debug)
            {
                //DEBUGSTAGE
                if (Global.quickStartOnline)
                {
                    List<Server> servers = new List<Server>();
                    byte[] response = Global.matchmakingQuerier.send("127.0.0.1", "GetServers");
                    if (response.IsNullOrEmpty())
                    {
                        //networkError = true;
                    }
                    else
                    {
                        servers = Helpers.deserialize<List<Server>>(response);
                    }
                    if (servers == null || servers.Count == 0)
                    {
                        Global.skipCharWepSel = true;
                        var hostmenu = new HostMenu(this, null, false, false);
                        Menu.change(hostmenu);
                        Options.main.soundVolume = Global.quickStartOnlineHostSound;
                        Options.main.musicVolume = Global.quickStartOnlineHostMusic;
                        var serverData = new Server(Global.version, Options.main.getRegion(), "testserver", Global.quickStartOnlineMap, Global.quickStartOnlineMap, Global.quickStartOnlineGameMode, 100, Global.quickStartOnlineBotCount, 2, 300, false, false, 
                            Global.quickStartNetcodeModel, Global.quickStartNetcodePing, true, Global.quickStartMirrored, Global.quickStartTrainingLoadout, Global.checksum, null, null, SavedMatchSettings.mainOffline.extraCpuCharData, null, 
                            Global.quickStartDisableHtSt, Global.quickStartDisableVehicles);
                        HostMenu.createServer(Global.quickStartOnlineHostCharNum, serverData, null, false, new MainMenu(), out _);
                    }
                    else
                    {
                        Global.skipCharWepSel = true;
                        Options.main.soundVolume = Global.quickStartOnlineClientSound;
                        Options.main.musicVolume = Global.quickStartOnlineClientMusic;
                        var joinmenu = new JoinMenu(false);
                        Menu.change(joinmenu);
                    }
                }

                if (Global.input.isPressed(Key.Num1))
                {
                    Global.skipCharWepSel = true;
                    var hostmenu = new HostMenu(this, null, false, false);
                    Menu.change(hostmenu);
                    var serverData = new Server(Global.version, Options.main.getRegion(), "testserver", Global.quickStartOnlineMap, Global.quickStartOnlineMap, Global.quickStartOnlineGameMode, 100, Global.quickStartOnlineBotCount, 2, 300, false, false,
                        Global.quickStartNetcodeModel, Global.quickStartNetcodePing, true, Global.quickStartMirrored, Global.quickStartTrainingLoadout, Global.checksum, null, null, SavedMatchSettings.mainOffline.extraCpuCharData, null, Global.quickStartDisableHtSt, Global.quickStartDisableVehicles);
                    HostMenu.createServer(Global.quickStartCharNum, serverData, null, false, new MainMenu(), out _);
                }
                else if (Global.input.isPressed(Key.Num2))
                {
                    Global.skipCharWepSel = true;
                    var joinmenu = new JoinMenu(false);
                    Menu.change(joinmenu);
                }
                else if (Global.input.isPressed(Key.Num3))
                {
                    var offlineMenu = new HostMenu(this, null, true, false);
                    offlineMenu.mapSizeIndex = 0;
                    offlineMenu.mapIndex = offlineMenu.currentMapSizePool.IndexOf(offlineMenu.currentMapSizePool.FirstOrDefault(m => m.isTraining()));
                    offlineMenu.botCount = 1;
                    Menu.change(offlineMenu);
                }
                else if (Global.quickStart)
                {
                    var selectedLevel = Global.levelDatas.FirstOrDefault(ld => ld.Key == Global.quickStartMap).Value;
                    var scm = new SelectCharacterMenu(Global.quickStartCharNum);
                    var me = new ServerPlayer(Options.main.playerName, 0, true, Global.quickStartCharNum, Global.quickStartTeam, Global.deviceId, null, 0);
                    if (selectedLevel.name == "training" && GameMode.isStringTeamMode(Global.quickStartTrainingGameMode)) me.alliance = Global.quickStartTeam;
                    if (selectedLevel.name != "training" && GameMode.isStringTeamMode(Global.quickStartGameMode)) me.alliance = Global.quickStartTeam;

                    string gameMode = selectedLevel.name == "training" ? Global.quickStartTrainingGameMode : Global.quickStartGameMode;
                    int botCount = selectedLevel.name == "training" ? Global.quickStartTrainingBotCount : Global.quickStartBotCount;
                    bool disableVehicles = selectedLevel.name == "training" ? Global.quickStartDisableVehiclesTraining : Global.quickStartDisableVehicles;

                    var localServer = new Server(Global.version, null, null, selectedLevel.name, selectedLevel.shortName, gameMode, Global.quickStartPlayTo, botCount, selectedLevel.maxPlayers, 0, false, false,
                        NetcodeModel.FavorAttacker, 200, true, Global.quickStartMirrored, Global.quickStartTrainingLoadout, Global.checksum, selectedLevel.checksum, selectedLevel.customMapUrl, SavedMatchSettings.mainOffline.extraCpuCharData, null, 
                        Global.quickStartDisableHtSt, disableVehicles);
                    localServer.players = new List<ServerPlayer>() { me };

                    Global.level = new Level(localServer.getLevelData(), SelectCharacterMenu.playerData, localServer.extraCpuCharData, false);
                    Global.level.startLevel(localServer, false);
                }
            }
        }

        public MMXFont getFont(int index)
        {
            if (selectY == index) return MMXFont.Select;
            return MMXFont.Menu;
        }

        public void render()
        {
            float startX = 35;

            string selectionImage = "";
            if (selectY == 0) selectionImage = "joinserver";
            else if (selectY == 1) selectionImage = "hostserver";
            else if (selectY == 2) selectionImage = "vscpu";
            else if (selectY == 3) selectionImage = "loadout";
            else if (selectY == 4) selectionImage = "controls";
            else if (selectY == 5) selectionImage = "options";
            else if (selectY == 6) selectionImage = "quit";

            DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
            DrawWrappers.DrawTitleTexture(Global.textures["mainmenutitle"]);
            DrawWrappers.DrawTextureHUD(Global.textures["cursor"], startX - 10, startPos + 1 + (selectY * yDistance));
            DrawWrappers.DrawTextureHUD(Global.textures[selectionImage], 165, 107);
            DrawWrappers.DrawTextureHUD(Global.textures["mainmenubox"], 156, 98);

            Helpers.drawTextStd(TCat.Option, "JOIN MATCH", startX, optionPos1.y, selected: selectY == 0);
            Helpers.drawTextStd(TCat.Option, "CREATE MATCH", startX, optionPos2.y, selected: selectY == 1);
            Helpers.drawTextStd(TCat.Option, "VS. CPU", startX, optionPos3.y, selected: selectY == 2);
            Helpers.drawTextStd(TCat.Option, "LOADOUT", startX, optionPos4.y, selected: selectY == 3);
            Helpers.drawTextStd(TCat.Option, "CONTROLS", startX, optionPos5.y, selected: selectY == 4);
            Helpers.drawTextStd(TCat.Option, "SETTINGS", startX, optionPos6.y, selected: selectY == 5);
            Helpers.drawTextStd(TCat.Option, "QUIT", startX, optionPos7.y, selected: selectY == 6);
            
            Helpers.drawTextStd(TCat.BotHelp, "Up/down: Change selection, [X]: Choose", Global.screenW / 2, 209, Alignment.Center, fontSize: 24);

            if (state == 0)
            {
                float top = Global.screenH * 0.4f;

                //DrawWrappers.DrawRect(5, top - 20, Global.screenW - 5, top + 60, true, new Color(0, 0, 0), 0, ZIndex.HUD, false);
                DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, new Color(0, 0, 0), 0, ZIndex.HUD, false);
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
            else if (state == 1)
            {
                float top = Global.screenH * 0.25f;
                DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, new Color(0, 0, 0), 0, ZIndex.HUD, false);
                Helpers.drawTextStd("Loading...", Global.screenW / 2, top, alignment: Alignment.Center, fontSize: 24);
            }
            else
            {
                string versionText = "v" + Global.version;
                /*
                if (Helpers.compareVersions(Global.version, Global.serverVersion) == -1 && Global.serverVersion != decimal.MaxValue)
                {
                    versionText += "(Update available)";
                }
                */
                if (Global.checksum != Global.prodChecksum)
                {
                    versionText = $"{versionText}\n{Global.checksum}";
                }
                Helpers.drawTextStd(versionText, 2, 1, Alignment.Left, fontSize: 16);
            }
        }
    }
}
