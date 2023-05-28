using ProtoBuf;
using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    [ProtoContract]
    public class CustomMatchSettings
    {
        [ProtoMember(1)] public bool hyperModeMatch;
        [ProtoMember(2)] public int startScrap = 3;
        [ProtoMember(3)] public int startHeartTanks;
        [ProtoMember(4)] public int startSubTanks;
        [ProtoMember(5)] public int healthModifier = 1;
        [ProtoMember(6)] public int redHealthModifier = 1;
        [ProtoMember(7)] public int damageModifier = 1;
        [ProtoMember(8)] public int redDamageModifier = 1;
        [ProtoMember(9)] public int sameCharNum = -1;
        [ProtoMember(10)] public int redSameCharNum = -1;
        [ProtoMember(11)] public int maxHeartTanks = 8;
        [ProtoMember(12)] public int maxSubTanks = 4;
        [ProtoMember(13)] public int heartTankHp = 1;

        public CustomMatchSettings()
        {
        }
    }

    public class CustomMatchSettingsMenu : IMainMenu
    {
        public int selectArrowPosY;
        public const int startX = 30;
        public int startY = 50;
        public const int lineH = 10;
        public const uint fontSize = 24;
        public IMainMenu prevMenu;
        public bool inGame;
        public bool isOffline;
        public List<MenuOption> menuOptions = new List<MenuOption>();

        SavedMatchSettings savedMatchSettings { get { return isOffline ? SavedMatchSettings.mainOffline : SavedMatchSettings.mainOnline; } }

        public CustomMatchSettingsMenu(IMainMenu prevMenu, bool inGame, bool isOffline)
        {
            this.prevMenu = prevMenu;
            this.inGame = inGame;
            int currentY = startY;
            this.isOffline = isOffline;

            menuOptions.Add(
                new MenuOption(startX, currentY,
                    () =>
                    {
                        Helpers.menuLeftRightBool(ref savedMatchSettings.customMatchSettings.hyperModeMatch);
                    },
                    (Point pos, int index) =>
                    {
                        Helpers.drawTextStd(TCat.Option, "Hyper mode match (1v1): " + Helpers.boolYesNo(savedMatchSettings.customMatchSettings.hyperModeMatch), pos.x, pos.y, fontSize: fontSize, selected: selectArrowPosY == 0);
                    })
                );

            menuOptions.Add(
                new MenuOption(startX, currentY += lineH,
                    () =>
                    {
                        Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.startScrap, 0, 9999, true);
                    },
                    (Point pos, int index) =>
                    {
                        Helpers.drawTextStd(TCat.Option, "Start scrap: " + savedMatchSettings.customMatchSettings.startScrap.ToString(), pos.x, pos.y, fontSize: fontSize, selected: selectArrowPosY == 1);
                    })
                );

            menuOptions.Add(
                new MenuOption(startX, currentY += lineH,
                    () =>
                    {
                        Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.heartTankHp, 1, 2, true);
                    },
                    (Point pos, int index) =>
                    {
                        Helpers.drawTextStd(TCat.Option, "Heart tank HP: " + savedMatchSettings.customMatchSettings.heartTankHp.ToString(), pos.x, pos.y, fontSize: fontSize, selected: selectArrowPosY == 2);
                    })
                );

            menuOptions.Add(
                new MenuOption(startX, currentY += lineH,
                    () =>
                    {
                        Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.startHeartTanks, 0, 8, true);
                    },
                    (Point pos, int index) =>
                    {
                        Helpers.drawTextStd(TCat.Option, "Start heart tanks: " + savedMatchSettings.customMatchSettings.startHeartTanks.ToString(), pos.x, pos.y, fontSize: fontSize, selected: selectArrowPosY == 3);
                    })
                );

            menuOptions.Add(
                new MenuOption(startX, currentY += lineH,
                    () =>
                    {
                        Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.maxHeartTanks, 0, 8, true);
                    },
                    (Point pos, int index) =>
                    {
                        Helpers.drawTextStd(TCat.Option, "Max heart tanks: " + savedMatchSettings.customMatchSettings.maxHeartTanks.ToString(), pos.x, pos.y, fontSize: fontSize, selected: selectArrowPosY == 4);
                    })
                );

            menuOptions.Add(
                new MenuOption(startX, currentY += lineH,
                    () =>
                    {
                        Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.startSubTanks, 0, 4, true);
                    },
                    (Point pos, int index) =>
                    {
                        Helpers.drawTextStd(TCat.Option, "Start subtanks: " + savedMatchSettings.customMatchSettings.startSubTanks.ToString(), pos.x, pos.y, fontSize: fontSize, selected: selectArrowPosY == 5);
                    })
                );

            menuOptions.Add(
                new MenuOption(startX, currentY += lineH,
                    () =>
                    {
                        Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.maxSubTanks, 0, 4, true);
                    },
                    (Point pos, int index) =>
                    {
                        Helpers.drawTextStd(TCat.Option, "Max subtanks: " + savedMatchSettings.customMatchSettings.maxSubTanks.ToString(), pos.x, pos.y, fontSize: fontSize, selected: selectArrowPosY == 6);
                    })
                );

            menuOptions.Add(
                new MenuOption(startX, currentY += lineH,
                    () =>
                    {
                        Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.healthModifier, 1, 4);
                    },
                    (Point pos, int index) =>
                    {
                        Helpers.drawTextStd(TCat.Option, "Health modifier: " + (savedMatchSettings.customMatchSettings.healthModifier * 100).ToString() + "%", pos.x, pos.y, fontSize: fontSize, selected: selectArrowPosY == 7);
                    })
                );

            menuOptions.Add(
                new MenuOption(startX, currentY += lineH,
                    () =>
                    {
                        Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.redHealthModifier, 1, 4);
                    },
                    (Point pos, int index) =>
                    {
                        Helpers.drawTextStd(TCat.Option, "Red health modifier: " + (savedMatchSettings.customMatchSettings.redHealthModifier * 100).ToString() + "%", pos.x, pos.y, fontSize: fontSize, selected: selectArrowPosY == 8);
                    })
                );

            menuOptions.Add(
                new MenuOption(startX, currentY += lineH,
                    () =>
                    {
                        Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.damageModifier, 1, 4, true);
                    },
                    (Point pos, int index) =>
                    {
                        Helpers.drawTextStd(TCat.Option, "Damage modifier: " + (savedMatchSettings.customMatchSettings.damageModifier * 100).ToString() + "%", pos.x, pos.y, fontSize: fontSize, selected: selectArrowPosY == 9);
                    })
                );

            menuOptions.Add(
                new MenuOption(startX, currentY += lineH,
                    () =>
                    {
                        Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.redDamageModifier, 1, 4, true);
                    },
                    (Point pos, int index) =>
                    {
                        Helpers.drawTextStd(TCat.Option, "Red damage modifier: " + (savedMatchSettings.customMatchSettings.redDamageModifier * 100).ToString() + "%", pos.x, pos.y, fontSize: fontSize, selected: selectArrowPosY == 10);
                    })
                );

            menuOptions.Add(
                new MenuOption(startX, currentY += lineH,
                    () =>
                    {
                        Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.sameCharNum, -1, 4);
                    },
                    (Point pos, int index) =>
                    {
                        Helpers.drawTextStd(TCat.Option, "Mono character: " + getSameCharString(savedMatchSettings.customMatchSettings.sameCharNum), pos.x, pos.y, fontSize: fontSize, selected: selectArrowPosY == 11);
                    })
                );

            menuOptions.Add(
                new MenuOption(startX, currentY += lineH,
                    () =>
                    {
                        Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.redSameCharNum, -1, 4);
                    },
                    (Point pos, int index) =>
                    {
                        Helpers.drawTextStd(TCat.Option, "Red mono character: " + getSameCharString(savedMatchSettings.customMatchSettings.redSameCharNum), pos.x, pos.y, fontSize: fontSize, selected: selectArrowPosY == 12);
                    })
                );
        }

        public string getSameCharString(int charNum)
        {
            if (charNum == -1) return "No";
            return Character.charDisplayNames[charNum];
        }

        public void update()
        {
            Helpers.menuUpDown(ref selectArrowPosY, 0, menuOptions.Count - 1);
            if (Global.input.isPressedMenu(Control.MenuBack))
            {
                if (savedMatchSettings.customMatchSettings.maxHeartTanks < savedMatchSettings.customMatchSettings.startHeartTanks)
                {
                    Menu.change(new ErrorMenu(new string[] { "Error: Max heart tanks can't be", "less than start heart tanks." }, this));
                    return;
                }

                if (savedMatchSettings.customMatchSettings.maxSubTanks < savedMatchSettings.customMatchSettings.startSubTanks)
                {
                    Menu.change(new ErrorMenu(new string[] { "Error: Max sub tanks can't be", "less than start sub tanks." }, this));
                    return;
                }

                Menu.change(prevMenu);
            }

            menuOptions[selectArrowPosY].update();
        }

        public void render()
        {
            if (!inGame)
            {
                DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
                DrawWrappers.DrawTextureHUD(Global.textures["cursor"], menuOptions[selectArrowPosY].pos.x - 8, menuOptions[selectArrowPosY].pos.y - 1);
            }
            else
            {
                DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, Helpers.MenuBgColor, 0, ZIndex.HUD + 200, false);
                Global.sprites["cursor"].drawToHUD(0, menuOptions[selectArrowPosY].pos.x - 8, menuOptions[selectArrowPosY].pos.y + 5);
            }

            Helpers.drawTextStd(TCat.Title, "Custom Match Options", Global.halfScreenW, 15, fontSize: 48, alignment: Alignment.Center);

            int i = 0;
            foreach (var menuOption in menuOptions)
            {
                menuOption.render(menuOption.pos, i);
                i++;
            }

            Helpers.drawTextStd(TCat.BotHelp, "Left/Right: Change setting, [Z]: Back", Global.screenW * 0.5f, 200, Alignment.Center, fontSize: 24);
        }
    }
}
