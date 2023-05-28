using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class SelectVileArmorMenu : IMainMenu
    {
        public int selectArrowPosY;
        public IMainMenu prevMenu;

        public Point optionPos1 = new Point(25, 40);
        public Point optionPos2 = new Point(25, 80);
        public Point optionPos3 = new Point(25, 110);
        public Point optionPos4 = new Point(25, 170);

        public SelectVileArmorMenu(IMainMenu prevMenu)
        {
            this.prevMenu = prevMenu;
        }

        public void update()
        {
            var mainPlayer = Global.level.mainPlayer;

            if (!Global.level.server.disableHtSt && Global.input.isPressedMenu(Control.MenuLeft))
            {
                UpgradeMenu.onUpgradeMenu = true;
                Menu.change(new UpgradeMenu(prevMenu));
                return;
            }

            Helpers.menuUpDown(ref selectArrowPosY, 0, 1);

            if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
            {
                if (selectArrowPosY == 0)
                {
                    if (!mainPlayer.frozenCastle && mainPlayer.scrap >= Character.frozenCastleCost)
                    {
                        mainPlayer.frozenCastle = true;
                        Global.playSound("ching");
                        mainPlayer.scrap -= Character.frozenCastleCost;
                    }
                }
                else if (selectArrowPosY == 1)
                {
                    if (!mainPlayer.speedDevil && mainPlayer.scrap >= Character.speedDevilCost)
                    {
                        mainPlayer.speedDevil = true;
                        Global.playSound("ching");
                        mainPlayer.scrap -= Character.speedDevilCost;
                    }
                }
            }
            else if (Global.input.isPressedMenu(Control.MenuBack))
            {
                Menu.change(prevMenu);
            }
        }

        public void render()
        {
            var mainPlayer = Global.level.mainPlayer;
            var gameMode = Global.level.gameMode;

            DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, Helpers.MenuBgColor, 0, ZIndex.HUD + 200, false);

            Global.sprites["menu_viledefault"].drawToHUD(0, 230, 115);

            if (!Global.level.server.disableHtSt && Global.frameCount % 60 < 30)
            {
                Helpers.drawTextStd(TCat.Option, "<", 12, Global.halfScreenH, Alignment.Center, fontSize: 32);
                Helpers.drawTextStd(TCat.Option, "Items", 12, Global.halfScreenH + 15, Alignment.Center, fontSize: 20);
            }

            if (mainPlayer.speedDevil) Global.sprites["menu_vilespeeddevil"].drawToHUD(0, 230, 115);
            if (mainPlayer.frozenCastle) Global.sprites["menu_vilefrozencastle"].drawToHUD(0, 230, 115);

            Global.sprites["cursor"].drawToHUD(0, optionPos1.x - 8, optionPos1.y + 4 + selectArrowPosY * 40);

            Helpers.drawTextStd(TCat.Title, "Vile Armor", Global.screenW * 0.5f, 8, Alignment.Center, fontSize: 48);
            Helpers.drawTextStd("Scrap: " + mainPlayer.scrap, Global.screenW * 0.5f, 25, Alignment.Center, fontSize: 24);

            Helpers.drawTextStd(TCat.Option, "Frozen Castle", optionPos1.x, optionPos1.y, fontSize: 24, color: mainPlayer.frozenCastle ? Helpers.Gray : Color.White, selected: selectArrowPosY == 0);
            Helpers.drawTextStd(string.Format(" ({0} scrap)", Character.frozenCastleCost), optionPos1.x + 80, optionPos1.y, fontSize: 24, color: mainPlayer.frozenCastle ? Helpers.Gray : (mainPlayer.scrap < Character.frozenCastleCost ? Color.Red : Color.Green));
            Helpers.drawTextStd("By utilizing a thin layer of ice,", optionPos1.x + 5, optionPos1.y + 11, fontSize: 18, color: mainPlayer.frozenCastle ? Helpers.Gray : Color.White);
            Helpers.drawTextStd("this armor reduces damage by 1/8.", optionPos1.x + 5, optionPos1.y + 18, fontSize: 18, color: mainPlayer.frozenCastle ? Helpers.Gray : Color.White);

            Helpers.drawTextStd(TCat.Option, "Speed Devil", optionPos2.x, optionPos2.y, fontSize: 24, color: mainPlayer.speedDevil ? Helpers.Gray : Color.White, selected: selectArrowPosY == 1);
            Helpers.drawTextStd(string.Format(" ({0} scrap)", Character.speedDevilCost), optionPos2.x + 80, optionPos2.y, fontSize: 24, color: mainPlayer.speedDevil ? Helpers.Gray : (mainPlayer.scrap < Character.speedDevilCost ? Color.Red : Color.Green));
            Helpers.drawTextStd("A layer of atmospheric pressure", optionPos2.x + 5, optionPos2.y + 11, fontSize: 18, color: mainPlayer.speedDevil ? Helpers.Gray : Color.White);
            Helpers.drawTextStd("increases movement speed by 10%.", optionPos2.x + 5, optionPos2.y + 18, fontSize: 18, color: mainPlayer.speedDevil ? Helpers.Gray : Color.White);

            Helpers.drawTextStd(TCat.BotHelp, "Left/Right: Change Armor", Global.halfScreenW, 208, Alignment.Center, fontSize: 16);
            Helpers.drawTextStd(TCat.BotHelp, "[X]: Upgrade, [C]: Unupgrade, [Z]: Back", Global.halfScreenW, 214, Alignment.Center, fontSize: 16);
        }

    }
}
