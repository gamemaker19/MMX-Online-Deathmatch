using Newtonsoft.Json;
using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class PreJoinOrHostMenu : IMainMenu
    {
        public int selectY;
        public Point optionPos1;
        public Point optionPos2;
        public const int lineH = 15;
        public MainMenu prevMenu;
        public bool isJoin;
        public const float startX = 120;
        public int state;
        public float state1Time;

        public PreJoinOrHostMenu(MainMenu prevMenu, bool isJoin)
        {
            this.prevMenu = prevMenu;
            this.isJoin = isJoin;
            optionPos1 = new Point(40, 70);
            optionPos2 = new Point(40, 70 + lineH);
        }

        public void update()
        {
            if (state == 0)
            {
                Helpers.menuUpDown(ref selectY, 0, 1);
                if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
                {
                    if (selectY == 0)
                    {
                        state = 1;
                    }
                    else
                    {
                        IMainMenu nextMenu = null;
                        if (isJoin) nextMenu = new JoinMenu(true);
                        else nextMenu = new HostMenu(prevMenu, null, false, true);

                        Menu.change(nextMenu);
                    }
                }
                else if (Global.input.isPressedMenu(Control.MenuBack))
                {
                    Menu.change(prevMenu);
                }
            }
            else if (state == 1)
            {
                if (Global.regions.Count == 0)
                {
                    state = 0;
                    Menu.change(new ErrorMenu(new string[]
                    { 
                        "No multiplayer regions configured.",
                        "Please add a region name/ip to region.txt",
                        "in game or MMXOD folder, then restart the game.",
                    }, this));
                }
                else
                {
                    state1Time = 0;
                    state = 0;
                    IMainMenu nextMenu = null;
                    if (isJoin) nextMenu = new JoinMenu(false);
                    else nextMenu = new HostMenu(prevMenu, null, false, false);

                    if (canPlayOnline(out string[] warningMessage))
                    {
                        if (warningMessage != null)
                        {
                            Menu.change(new ErrorMenu(warningMessage, nextMenu));
                        }
                        else
                        {
                            Menu.change(nextMenu);
                        }
                    }
                }
            }
        }

        private string[] getOutdatedClientMessage(decimal version, decimal serverVersion)
        {
            if (serverVersion == decimal.MaxValue)
            {
                return new string[] { "Could not connect to server.", "The region may be down.", "Try changing your region in Settings." };
            }
            return new string[]
            {
                string.Format(CultureInfo.InvariantCulture, "Your version of the game (v{0}) is outdated.", version),
                string.Format(CultureInfo.InvariantCulture, "Please update to the new version (v{0})", serverVersion),
                "for online play."
            };
        }

        private string[] getOutdatedClientMessage2(decimal version, decimal serverVersion)
        {
            if (serverVersion == decimal.MaxValue)
            {
                return new string[] { "Could not connect to server.", "The region may be down.", "Try changing your region in Settings." };
            }
            return new string[]
            {
                string.Format(CultureInfo.InvariantCulture, "Your version of the game (v{0}) is too new.", version),
                string.Format(CultureInfo.InvariantCulture, "Please revert to the version (v{0})", serverVersion),
                "for online play."
            };
        }

        public bool canPlayOnline(out string[] warningMessage)
        {
            warningMessage = null;

            string deviceId = Global.deviceId;
            if (string.IsNullOrEmpty(deviceId))
            {
                Menu.change(new ErrorMenu(new string[] { "Error in fetching device id.", "You cannot play online." }, new MainMenu()));
                return false;
            }
            if (!Global.checkBan)
            {
                var response = Global.matchmakingQuerier.send(Options.main.getRegion().ip, "CheckBan:" + deviceId, "CheckBan");
                if (response != null)
                {
                    Global.checkBan = true;
                    if (response != "") Global.banEntry = JsonConvert.DeserializeObject<BanEntry>(response);
                }
            }
            if (!Global.checkBan)
            {
                Menu.change(new ErrorMenu(new string[] { "Unable to connect to server in region.txt." }, new MainMenu()));
                return false;
            }
            if (Global.banEntry != null)
            {
                string banEndDateStr = "Never";
                if (Global.banEntry.bannedUntil != null) banEndDateStr = Global.banEntry.bannedUntil.ToString();

                if (Global.banEntry.banType == 0)
                {
                    var banLines = new string[]
                    {
                        "You are currently banned from online play!",
                        "Reason: " + Global.banEntry.reason,
                        "Ban end date: " + banEndDateStr,
                        "Appeal to an admin of the server."
                    };
                    Menu.change(new ErrorMenu(banLines, new MainMenu()));
                    return false;
                }
                else if (Global.banEntry.banType == 1)
                {
                    var banLines = new string[]
                    {
                        "ALERT: Currently banned from chat/voting.",
                        "Reason: " + Global.banEntry.reason,
                        "Ban end date: " + banEndDateStr,
                        "Appeal to an admin of the server."
                    };
                    warningMessage = banLines;
                }
                else if (Global.banEntry.banType == 2)
                {
                    var banLines = new string[]
                    {
                        "You are warned for bad online conduct.",
                        "Reason: " + Global.banEntry.reason,
                        "Further misconduct will result in a ban.",
                        "Appeal to an admin of the server."
                    };
                    warningMessage = banLines;
                }
            }

            return true;
        }

        public void render()
        {
            DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
            //DrawWrappers.DrawTextureMenu(Global.textures["cursor"], 20, topLeft.y + ySpace + (selectArrowPosY * ySpace));
            Global.sprites["cursor"].drawToHUD(0, startX - 10, 73 + (selectY * lineH));

            Helpers.drawTextStd(TCat.Title, "SELECT OPTION", Global.screenW * 0.5f, 20, Alignment.Center, fontSize: 40);

            if (state == 0)
            {
                Helpers.drawTextStd(TCat.Option, "INTERNET", startX, optionPos1.y, fontSize: 24, selected: selectY == 0);
            }
            else
            {
                Helpers.drawTextStd(TCat.Option, "LOADING...", startX, optionPos1.y, fontSize: 24, selected: selectY == 0);
            }

            int msgPos = 140;
            DrawWrappers.DrawLine(10, msgPos - 20, Global.screenW - 10, msgPos - 20, Color.White, 0.5f, ZIndex.HUD, isWorldPos: false);
            Helpers.drawTextStd(TCat.Default, "NOTICE", Global.halfScreenW, msgPos - 14, Alignment.Center, fontSize: 24);
            Helpers.drawTextStd(TCat.Default, "Official servers have been shut down.", Global.halfScreenW, msgPos, Alignment.Center, fontSize: 18);
            Helpers.drawTextStd(TCat.Default, "See link below for self hosting guide:", Global.halfScreenW, msgPos + 10, Alignment.Center, fontSize: 18);
            Helpers.drawTextStd(TCat.Default, "https://gamemaker19.github.io/MMXOnlineDesktop/decom.html", Global.halfScreenW, msgPos + 20, Alignment.Center, fontSize: 18);
            DrawWrappers.DrawLine(10, msgPos + 32, Global.screenW - 10, msgPos + 32, Color.White, 0.5f, ZIndex.HUD, isWorldPos: false);

            Helpers.drawTextStd(TCat.Option, "LAN", startX, optionPos2.y, fontSize: 24, selected: selectY == 1);

            Helpers.drawTextStd(TCat.BotHelp, "[X]: Choose, [Z]: Back", Global.halfScreenW, 200, Alignment.Center, fontSize: 24);
        }
    }
}
