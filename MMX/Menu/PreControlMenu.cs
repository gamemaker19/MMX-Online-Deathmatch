using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class PreControlMenu : IMainMenu
    {
        public int selArrowPosY;
        public Point optionPos1 = new Point(40, 70);
        public Point optionPos2 = new Point(40, 90);
        public Point optionPos3 = new Point(40, 110);
        public Point optionPos5 = new Point(50, 170);
        public IMainMenu prevMenu;
        public string message;
        public Action yesAction;
        public bool inGame;
        public bool isAxl;

        public List<int> cursorToCharNum;

        public PreControlMenu(IMainMenu prevMenu, bool inGame)
        {
            this.prevMenu = prevMenu;
            this.inGame = inGame;
            cursorToCharNum = new List<int>() { -1, -1 };
        }

        public void update()
        {
            if (Global.input.isPressedMenu(Control.MenuLeft))
            {
                cursorToCharNum[selArrowPosY]--;
                if (cursorToCharNum[selArrowPosY] < -1)
                {
                    cursorToCharNum[selArrowPosY] = 5;
                }
            }
            else if (Global.input.isPressedMenu(Control.MenuRight))
            {
                cursorToCharNum[selArrowPosY]++;
                if (cursorToCharNum[selArrowPosY] > 5)
                {
                    cursorToCharNum[selArrowPosY] = 0;
                }
            }

            Helpers.menuUpDown(ref selArrowPosY, 0, 1);
            if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
            {
                getCharNumAndAimMode(cursorToCharNum[selArrowPosY], out int charNum, out int aimMode);
                if (selArrowPosY == 0)
                {
                    Menu.change(new ControlMenu(this, inGame, false, charNum, aimMode));
                }
                else if (selArrowPosY == 1)
                {
                    if (Control.isJoystick())
                    {
                        Menu.change(new ControlMenu(this, inGame, true, charNum, aimMode));
                    }
                }
            }
            else if (Global.input.isPressedMenu(Control.MenuBack))
            {
                Menu.change(prevMenu);
            }
        }

        private void getCharNumAndAimMode(int rawCharNum, out int charNum, out int aimMode)
        {
            charNum = rawCharNum;
            aimMode = 0;
            if (rawCharNum == 4)
            {
                charNum = 3;
                aimMode = 2;
            }
            if (rawCharNum == 5)
            {
                charNum = 4;
            }
        }

        public string getLeftRightStr(string str)
        {
            if (Global.frameCount % 60 < 30)
            {
                return "< " + str + " >";
            }
            return "  " + str + "  ";
        }

        public void render()
        {
            if (!inGame)
            {
                DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
                //DrawWrappers.DrawTextureMenu(Global.textures["cursor"], 20, topLeft.y + ySpace + (selectArrowPosY * ySpace));
                Global.sprites["cursor"].drawToHUD(0, optionPos1.x - 10, 73 + (selArrowPosY * 20));
            }
            else
            {
                DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, Helpers.MenuBgColor, 0, ZIndex.HUD + 200, false);
                Global.sprites["cursor"].drawToHUD(0, optionPos1.x - 10, 73 + (selArrowPosY * 20));
            }

            Helpers.drawTextStd(TCat.Title, "SELECT INPUT TO CONFIGURE", Global.screenW * 0.5f, 20, Alignment.Center, fontSize: 32);
            
            Helpers.drawTextStd(TCat.Option, getLeftRightStr("KEYBOARD " + getCharStr(0)), optionPos1.x, optionPos1.y, fontSize: 24, selected: selArrowPosY == 0);

            if (Control.isJoystick())
            {
                Helpers.drawTextStd(TCat.Option, getLeftRightStr("CONTROLLER " + getCharStr(1)), optionPos2.x, optionPos2.y, fontSize: 24, selected: selArrowPosY == 1);
            }
            else
            {
                Helpers.drawTextStd(TCat.Option, "CONTROLLER (NOT DETECTED)", optionPos2.x, optionPos2.y, color: Helpers.Gray, fontSize: 24, selected: selArrowPosY == 1);
            }

            Helpers.drawTextStd(Helpers.controlText("Use LEFT/RIGHT to switch character to configure controls for."), Global.halfScreenW, 130, Alignment.Center, fontSize: 16);
            Helpers.drawTextStd(Helpers.controlText("If a binding does not exist on a char-specific control config,"), Global.halfScreenW, 140, Alignment.Center, fontSize: 16);
            Helpers.drawTextStd(Helpers.controlText("it will fall back to the ALL config, if applicable."), Global.halfScreenW, 150, Alignment.Center, fontSize: 16);

            Helpers.drawTextStd(TCat.BotHelp, "[X]: Choose, [Z]: Back", Global.halfScreenW, optionPos5.y + 20, Alignment.Center, fontSize: 24);
        }

        public string getCharStr(int yPos)
        {
            int charNum = cursorToCharNum[yPos];
            if (charNum == -1) return "(ALL)";
            if (charNum == 0) return "(X)";
            if (charNum == 1) return "(ZERO)";
            if (charNum == 2) return "(VILE)";
            if (charNum == 3) return "(AXL, DIRECTIONAL)";
            if (charNum == 4) return "(AXL, CURSOR)";
            if (charNum == 5) return "(SIGMA)";
            return "(ERROR)";
        }
    }
}
