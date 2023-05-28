using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class PreOptionsMenu : IMainMenu
    {
        public int selectY;
        public Point optionPos1;
        public Point optionPos2;
        public Point optionPos3;
        public Point optionPos4;
        public Point optionPos5;
        public Point optionPos6;
        public Point optionPos7;
        public const int lineH = 15;
        public IMainMenu prevMenu;
        public string message;
        public Action yesAction;
        public bool inGame;
        public bool isAxl;
        public float startX = 100;

        public PreOptionsMenu(IMainMenu prevMenu, bool inGame)
        {
            this.prevMenu = prevMenu;
            this.inGame = inGame;
            optionPos1 = new Point(40, 70);
            optionPos2 = new Point(40, 70 + lineH);
            optionPos3 = new Point(40, 70 + lineH * 2);
            optionPos4 = new Point(40, 70 + lineH * 3);
            optionPos5 = new Point(40, 70 + lineH * 4);
            optionPos6 = new Point(40, 70 + lineH * 5);
            optionPos7 = new Point(40, 70 + lineH * 6);
        }

        public void update()
        {
            Helpers.menuUpDown(ref selectY, 0, 6);
            if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
            {
                int? charNum = null;
                bool isGraphics = selectY == 1;
                if (selectY == 2) charNum = 0;
                if (selectY == 3) charNum = 1;
                if (selectY == 4) charNum = 2;
                if (selectY == 5) charNum = 3;
                if (selectY == 6) charNum = 4;

                Menu.change(new OptionsMenu(this, inGame, charNum, isGraphics));
            }
            else if (Global.input.isPressedMenu(Control.MenuBack))
            {
                Menu.change(prevMenu);
            }
        }

        public void render()
        {
            if (!inGame)
            {
                DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
                //DrawWrappers.DrawTextureMenu(Global.textures["cursor"], 20, topLeft.y + ySpace + (selectArrowPosY * ySpace));
                Global.sprites["cursor"].drawToHUD(0, startX - 10, 73 + (selectY * lineH));
            }
            else
            {
                DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, Helpers.MenuBgColor, 0, ZIndex.HUD + 200, false);
                Global.sprites["cursor"].drawToHUD(0, startX - 10, 73 + (selectY * lineH));
            }

            Helpers.drawTextStd(TCat.Title, "SELECT SETTINGS TO CONFIGURE", Global.screenW * 0.5f, 20, Alignment.Center, fontSize: 32);
            
            Helpers.drawTextStd(TCat.Option, "GENERAL SETTINGS", startX, optionPos1.y, fontSize: 24, selected: selectY == 0);
            Helpers.drawTextStd(TCat.Option, "GRAPHICS SETTINGS", startX, optionPos2.y, fontSize: 24, selected: selectY == 1);
            Helpers.drawTextStd(TCat.Option, "X SETTINGS", startX, optionPos3.y, fontSize: 24, selected: selectY == 2);
            Helpers.drawTextStd(TCat.Option, "ZERO SETTINGS", startX, optionPos4.y, fontSize: 24, selected: selectY == 3);
            Helpers.drawTextStd(TCat.Option, "VILE SETTINGS", startX, optionPos5.y, fontSize: 24, selected: selectY == 4);
            Helpers.drawTextStd(TCat.Option, "AXL SETTINGS", startX, optionPos6.y, fontSize: 24, selected: selectY == 5);
            Helpers.drawTextStd(TCat.Option, "SIGMA SETTINGS", startX, optionPos7.y, fontSize: 24, selected: selectY == 6);

            Helpers.drawTextStd(TCat.BotHelp, "[X]: Choose, [Z]: Back", Global.halfScreenW, 200, Alignment.Center, fontSize: 24);
        }
    }
}
