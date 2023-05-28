using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class PreLoadoutMenu : IMainMenu
    {
        public int selectY;
        public Point optionPos1 = new Point(40, 70);
        public Point optionPos2 = new Point(40, 90);
        public Point optionPos3 = new Point(40, 110);
        public Point optionPos4 = new Point(40, 130);
        public Point optionPos5 = new Point(40, 150);
        public IMainMenu prevMenu;
        public string message;
        public Action yesAction;
        public bool inGame;
        public bool isAxl;
        public float startX = 108;

        public PreLoadoutMenu(IMainMenu prevMenu)
        {
            this.prevMenu = prevMenu;
            selectY = Options.main.preferredCharacter;
        }

        public void update()
        {
            Helpers.menuUpDown(ref selectY, 0, 4);

            if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
            {
                if (selectY == 0)
                {
                    Menu.change(new SelectWeaponMenu(this, false));
                }
                if (selectY == 1)
                {
                    Menu.change(new SelectZeroWeaponMenu(this, false));
                }
                if (selectY == 2)
                {
                    Menu.change(new SelectVileWeaponMenu(this, false));
                }
                if (selectY == 3)
                {
                    Menu.change(new SelectAxlWeaponMenu(this, false));
                }
                if (selectY == 4)
                {
                    Menu.change(new SelectSigmaWeaponMenu(this, false));
                }
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
                Global.sprites["cursor"].drawToHUD(0, startX - 10, 73 + (selectY * 20));
            }
            else
            {
                DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, Helpers.MenuBgColor, 0, ZIndex.HUD + 200, false);
                Global.sprites["cursor"].drawToHUD(0, startX - 10, 73 + (selectY * 20));
            }

            Helpers.drawTextStd(TCat.Title, "SELECT CHARACTER LOADOUT", Global.screenW * 0.5f, 20, Alignment.Center, fontSize: 32);

            Helpers.drawTextStd(TCat.Option, "X LOADOUT", startX, optionPos1.y, fontSize: 24, selected: selectY == 0);
            Helpers.drawTextStd(TCat.Option, "ZERO LOADOUT", startX, optionPos2.y, fontSize: 24, selected: selectY == 1);
            Helpers.drawTextStd(TCat.Option, "VILE LOADOUT", startX, optionPos3.y, fontSize: 24, selected: selectY == 2);
            Helpers.drawTextStd(TCat.Option, "AXL LOADOUT", startX, optionPos4.y, fontSize: 24, selected: selectY == 3);
            Helpers.drawTextStd(TCat.Option, "SIGMA LOADOUT", startX, optionPos5.y, fontSize: 24, selected: selectY == 4);

            Helpers.drawTextStd(TCat.BotHelp, "[X]: Choose, [Z]: Back", Global.halfScreenW, 200, Alignment.Center, fontSize: 24);
        }
    }
}
