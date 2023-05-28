using ProtoBuf;
using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static SFML.Window.Keyboard;

namespace MMXOnline
{
    [ProtoContract]
    public class ExtraCpuCharData
    {
        [ProtoMember(1)] public List<PlayerCharData> cpuDatas;

        public ExtraCpuCharData()
        {
            cpuDatas = new List<PlayerCharData>();
        }
    }

    public class ConfigureCPUMenu : IMainMenu
    {
        public IMainMenu prevMenu;
        public List<MenuOption> menuOptions = new List<MenuOption>();
        public int selectArrowPosY;
        public const int startX = 30;
        public int startY = 30;
        public const int lineH = 10;
        public const uint fontSize = 24;

        public bool is1v1;
        public bool isOffline;
        public bool isInGame;
        public bool isInGameEndSelect;
        public bool isTeamMode;
        public bool isHost;
        
        public List<CharSelection> charSelections;

        public ConfigureCPUMenu(IMainMenu prevMenu, int cpuCount, bool is1v1, bool isOffline, bool isInGame, bool isInGameEndSelect, bool isTeamMode, bool isHost)
        {
            this.prevMenu = prevMenu;
            this.is1v1 = is1v1;
            this.isOffline = isOffline;
            this.isInGame = isInGame;
            this.isInGameEndSelect = isInGameEndSelect;
            this.isTeamMode = isTeamMode;
            this.isHost = isHost;

            int currentY = startY;
            if (cpuCount >= 9 && isTeamMode)
            {
                currentY -= 10;
            }

            SavedMatchSettings savedMatchSettings = isOffline ? SavedMatchSettings.mainOffline : SavedMatchSettings.mainOnline;

            while (savedMatchSettings.extraCpuCharData.cpuDatas.Count < cpuCount)
            {
                savedMatchSettings.extraCpuCharData.cpuDatas.Add(new PlayerCharData());
            }
            while (savedMatchSettings.extraCpuCharData.cpuDatas.Count > cpuCount)
            {
                savedMatchSettings.extraCpuCharData.cpuDatas.Pop();
            }
            
            charSelections = is1v1 ? CharSelection.selections1v1 : CharSelection.selections;
            charSelections = new List<CharSelection>(charSelections);
            charSelections.Insert(0, new CharSelection("Random", 0, 1, 0, "", 0));

            for (int i = 0; i < Math.Min(savedMatchSettings.extraCpuCharData.cpuDatas.Count, 9); i++)
            {
                var cpuData = savedMatchSettings.extraCpuCharData.cpuDatas[i];
                cpuData.uiSelectedCharIndex = Helpers.clamp(cpuData.uiSelectedCharIndex, 0, charSelections.Count - 1);

                bool forceEnable = (isOffline && i == 0);
                int iCopy = i;

                // CPU Character
                menuOptions.Add(
                    new MenuOption(startX + 30, currentY += lineH,
                        () =>
                        {
                            Helpers.menuLeftRightInc(ref cpuData.uiSelectedCharIndex, 0, charSelections.Count - 1);
                            cpuData.charNum = charSelections[cpuData.uiSelectedCharIndex].mappedCharNum;
                            cpuData.armorSet = charSelections[cpuData.uiSelectedCharIndex].mappedCharArmor;
                            cpuData.isRandom = charSelections[cpuData.uiSelectedCharIndex].name == "Random";
                        },
                        (Point pos, int index) =>
                        {
                            Helpers.drawTextStd(TCat.Title, "CPU" + (iCopy + 1).ToString(), pos.x - 40, pos.y, fontSize: fontSize, color: Color.White);
                            Helpers.drawTextStd(TCat.Option, "Character: " + charSelections[cpuData.uiSelectedCharIndex].name, pos.x, pos.y, fontSize: fontSize, color: Color.White, selected: index == selectArrowPosY);
                        })
                    );

                if (isTeamMode)
                {
                    // Team
                    menuOptions.Add(
                        new MenuOption(startX + 30, currentY += lineH,
                            () =>
                            {
                                Helpers.menuLeftRightInc(ref cpuData.alliance, -1, 1);
                            },
                            (Point pos, int index) =>
                            {
                                string allianceStr = "auto";
                                if (cpuData.alliance == GameMode.blueAlliance) allianceStr = "blue";
                                if (cpuData.alliance == GameMode.redAlliance) allianceStr = "red";
                                Helpers.drawTextStd(TCat.Option, "Team: " + allianceStr, pos.x, pos.y, fontSize: fontSize, color: Color.White, selected: index == selectArrowPosY);
                            })
                        );
                }
            }
        }

        public void update()
        {
            Helpers.menuUpDown(ref selectArrowPosY, 0, menuOptions.Count - 1);

            menuOptions[selectArrowPosY].update();

            if (Global.input.isPressedMenu(Control.MenuBack))
            {
                Menu.change(prevMenu);
                return;
            }
        }

        public void render()
        {
            if (!isInGame)
            {
                DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
            }
            else
            {
                DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, Helpers.MenuBgColor, 0, ZIndex.HUD + 200, false);
            }
            
            Helpers.drawTextStd(TCat.Title, "Configure CPU players", Global.halfScreenW, 7, fontSize: 48, alignment: Alignment.Center);
            DrawWrappers.DrawTextureHUD(Global.textures["cursor"], menuOptions[0].pos.x - 10, menuOptions[(int)selectArrowPosY].pos.y - 1);

            int i = 0;
            foreach (var menuOption in menuOptions)
            {
                menuOption.render(menuOption.pos, i);
                i++;
            }

            Helpers.drawTextStd(TCat.BotHelp, "Left/Right: Change, [Z]: Back", Global.screenW * 0.5f, 210, Alignment.Center, fontSize: 24);
        }
    }
}
