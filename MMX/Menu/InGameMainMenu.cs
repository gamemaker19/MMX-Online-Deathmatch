using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using static SFML.Window.Keyboard;

namespace MMXOnline
{
    public class InGameMainMenu : IMainMenu
    {
        public static int selectY = 0;
        public Point optionPos1 = new Point(80, 50);
        public Point optionPos2 = new Point(80, 70);
        public Point optionPos3 = new Point(80, 90);
        public Point optionPos4 = new Point(80, 110);
        public Point optionPos5 = new Point(80, 130);
        public Point optionPos6 = new Point(80, 150);
        public Point optionPos7 = new Point(80, 170);
        public const float startX = 92;

        public InGameMainMenu()
        {
        }

        public Player mainPlayer { get { return Global.level.mainPlayer; } }

        public void update()
        {
            if (!mainPlayer.canUpgradeXArmor())
            {
                UpgradeMenu.onUpgradeMenu = true;
            }

            Helpers.menuUpDown(ref selectY, 0, 6);
            if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
            {
                if (selectY == 0)
                {
                    if (isSelWepDisabled()) return;
                    if (Global.level.mainPlayer.realCharNum == 4)
                    {
                        Menu.change(new SelectSigmaWeaponMenu(this, true));
                    }
                    else if (Global.level.mainPlayer.realCharNum == 3)
                    {
                        Menu.change(new SelectAxlWeaponMenu(this, true));
                    }
                    else if (Global.level.mainPlayer.realCharNum == 2)
                    {
                        Menu.change(new SelectVileWeaponMenu(this, true));
                    }
                    else if (Global.level.mainPlayer.realCharNum == 1)
                    {
                        Menu.change(new SelectZeroWeaponMenu(this, true));
                    }
                    else
                    {
                        Menu.change(new SelectWeaponMenu(this, true));
                    }
                }
                else if (selectY == 1)
                {
                    if (isSelArmorDisabled()) return;
                    if (Global.level.mainPlayer.realCharNum == 0 || Global.level.mainPlayer.realCharNum == 2)
                    {
                        if (UpgradeMenu.onUpgradeMenu && !Global.level.server.disableHtSt)
                        {
                            Menu.change(new UpgradeMenu(this));
                        }
                        else if (Global.level.mainPlayer.realCharNum == 0)
                        {
                            Menu.change(new UpgradeArmorMenu(this));
                        }
                        else if (Global.level.mainPlayer.realCharNum == 2)
                        {
                            Menu.change(new SelectVileArmorMenu(this));
                        }
                    }
                    else
                    {
                        if (!Global.level.server.disableHtSt)
                        {
                            Menu.change(new UpgradeMenu(this));
                        }
                    }
                }
                else if (selectY == 2)
                {
                    if (isSelCharDisabled()) return;
                    Menu.change(new SelectCharacterMenu(this, Global.level.is1v1(), Global.serverClient == null, true, false, Global.level.gameMode.isTeamMode, Global.isHost, () => { Menu.exit(); }));
                }
                else if (selectY == 3)
                {
                    if (isMatchOptionsDisabled()) return;
                    Menu.change(new MatchOptionsMenu(this));
                }
                else if (selectY == 4)
                {
                    Menu.change(new PreControlMenu(this, true));
                }
                else if (selectY == 5)
                {
                    Menu.change(new PreOptionsMenu(this, true));
                }
                else if (selectY == 6)
                {
                    Menu.change(new ConfirmLeaveMenu(this, "Are you sure you want to leave?", () =>
                    {
                        Global._quickStart = false;
                        Global.leaveMatchSignal = new LeaveMatchSignal(LeaveMatchScenario.LeftManually, null, null);
                    }));
                }
            }
            else if (Global.input.isPressedMenu(Control.MenuBack))
            {
                Menu.exit();
            }
        }

        public bool isSelWepDisabled()
        {
            return Global.level.is1v1();
        }

        public bool isSelArmorDisabled()
        {
            if (Global.level.is1v1()) return true;
            if (mainPlayer.realCharNum == 2) return false;
            if (Global.level.server.disableHtSt)
            {
                if (mainPlayer.realCharNum != 0) return Global.level.server.disableHtSt;
                if (mainPlayer.canUpgradeXArmor())
                {
                    return false;
                }
                else
                {
                    return Global.level.server.disableHtSt;
                }
            }
            return false;
        }

        public bool isSelCharDisabled()
        {
            if (Global.level.isElimination()) return true;

            if (Global.level.server?.customMatchSettings?.redSameCharNum > -1)
            {
                if (Global.level.gameMode.isTeamMode && Global.level.mainPlayer.alliance == GameMode.redAlliance)
                {
                    return true;
                }
            }
            if (Global.level.server?.customMatchSettings?.sameCharNum > -1)
            {
                if (!Global.level.gameMode.isTeamMode || Global.level.mainPlayer.alliance == GameMode.blueAlliance)
                {
                    return true;
                }
            }

            return false;
        }

        public bool isMatchOptionsDisabled()
        {
            return false;
        }

        public void render()
        {
            DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, Helpers.MenuBgColor, 0, ZIndex.HUD + 200, false);
            Helpers.drawTextStd(TCat.Title, "Menu", Global.screenW * 0.5f, 20, Alignment.Center, fontSize: 48);

            Global.sprites["cursor"].drawToHUD(0, startX - 10, optionPos1.y + 6 + (selectY * 20));

            Helpers.drawTextStd(TCat.Option, "Edit Loadout", startX, optionPos1.y, color: !isSelWepDisabled() ? Color.White : Helpers.Gray, selected: selectY == 0);
            Helpers.drawTextStd(TCat.Option, "Upgrade Menu", startX, optionPos2.y, color: !isSelArmorDisabled() ? Color.White : Helpers.Gray, selected: selectY == 1);
            Helpers.drawTextStd(TCat.Option, "Switch Character", startX, optionPos3.y, color: !isSelCharDisabled() ? Color.White : Helpers.Gray, selected: selectY == 2);
            Helpers.drawTextStd(TCat.Option, "Match Options", startX, optionPos4.y, color: !isMatchOptionsDisabled() ? Color.White : Helpers.Gray, selected: selectY == 3);
            Helpers.drawTextStd(TCat.Option, "Controls", startX, optionPos5.y, selected: selectY == 4);
            Helpers.drawTextStd(TCat.Option, "Settings", startX, optionPos6.y, selected: selectY == 5);
            Helpers.drawTextStd(TCat.Option, "Leave Match", startX, optionPos7.y, selected: selectY == 6);
            Helpers.drawTextStd(TCat.BotHelp, "[X]: Choose, [ESC]: Cancel", Global.halfScreenW, 200, Alignment.Center, fontSize: 24);
        }
    }
}
