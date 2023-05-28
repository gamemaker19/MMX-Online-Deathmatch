using SFML.Graphics;
using System;
using static SFML.Window.Keyboard;

namespace MMXOnline
{
    public class ConfirmLeaveMenu : IMainMenu
    {
        public int selectY;
        public Point optionPos1 = new Point(80, 70);
        public Point optionPos2 = new Point(80, 90);
        public Point optionPos5 = new Point(50, 170);
        public IMainMenu prevMenu;
        public string message;
        public Action yesAction;
        public uint fontSize;

        public ConfirmLeaveMenu(IMainMenu prevMenu, string message, Action yesAction, uint fontSize = 30)
        {
            this.prevMenu = prevMenu;
            this.message = message;
            this.yesAction = yesAction;
            this.fontSize = fontSize;
        }

        public void update()
        {
            Helpers.menuUpDown(ref selectY, 0, 1);
            if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
            {
                if (selectY == 0)
                {
                    Menu.change(prevMenu);
                }
                else if (selectY == 1)
                {
                    yesAction.Invoke();
                }
            }
            else if (Global.input.isPressedMenu(Control.MenuBack))
            {
                Menu.change(prevMenu);
            }
        }

        public void render()
        {
            DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, Helpers.MenuBgColor, 0, ZIndex.HUD + 200, false);

            if (message.Contains("\n"))
            {
                var lines = message.Split('\n');
                int i = 0;
                foreach (var line in lines)
                {
                    Helpers.drawTextStd(TCat.Title, line, Global.screenW * 0.5f, 20 + i * 10, Alignment.Center, fontSize: 30);
                    i++;
                }
            }
            else
            {
                Helpers.drawTextStd(TCat.Title, message, Global.screenW * 0.5f, 20, Alignment.Center, fontSize: fontSize);
            }
            Global.sprites["cursor"].drawToHUD(0, 70, 76 + (selectY * 20));

            Helpers.drawTextStd(TCat.Option, "No", optionPos1.x, optionPos1.y, selected: selectY == 0);
            Helpers.drawTextStd(TCat.Option, "Yes", optionPos2.x, optionPos2.y, selected: selectY == 1);
            
            Helpers.drawTextStd(TCat.BotHelp, "[X]: Choose, [Z]: Back", Global.halfScreenW, optionPos5.y + 20, Alignment.Center, fontSize: 24);
        }
    }
}
