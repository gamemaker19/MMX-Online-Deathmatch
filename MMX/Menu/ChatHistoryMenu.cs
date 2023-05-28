using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class ChatHistoryMenu : IMainMenu
    {
        IMainMenu prevMenu;
        int yOffset = 0;
        const int lines = 14;
        public ChatHistoryMenu(IMainMenu prevMenu)
        {
            this.prevMenu = prevMenu;
        }

        public int maxYOffset
        {
            get
            {
                return Math.Max(0, Global.level.gameMode.chatMenu.chatHistory.Count - lines);
            }
        }

        public void update()
        {
            if (Global.input.isPressedMenu(Control.MenuUp))
            {
                yOffset++;
                if (yOffset > maxYOffset) yOffset = maxYOffset;
            }
            else if (Global.input.isPressedMenu(Control.MenuDown))
            {
                yOffset--;
                if (yOffset < 0) yOffset = 0;
            }
            else if (Global.input.isPressedMenu(Control.MenuBack))
            {
                Menu.change(prevMenu);
            }
        }

        public void render()
        {
            DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, Helpers.MenuBgColor, 0, ZIndex.HUD + 200, false);
            Helpers.drawTextStd(TCat.Title, "Chat History", Global.screenW * 0.5f, 10, Alignment.Center, fontSize: 54);

            var ch = Global.level.gameMode.chatMenu.chatHistory;
            if (ch.Count > lines)
            {
                if (yOffset == maxYOffset)
                {
                    Helpers.drawTextStd("[No more]", 20, 30, Alignment.Left, fontSize: 24);
                }
                else
                {
                    Helpers.drawTextStd("...", 20, 30, Alignment.Left, fontSize: 24);
                }
            }
            
            int y = 0;
            for (int i = Math.Max(0, ch.Count - lines - yOffset); i < ch.Count - yOffset; i++)
            {
                string line = ch[i].getDisplayMessage();
                Helpers.drawTextStd(line, 20, 40 + (y * 12), fontSize: 24, color: Color.White);
                y++;
            }

            Helpers.drawTextStd(TCat.BotHelp, "Up/Down: Scroll, [Z]: Back", Global.halfScreenW, 210, Alignment.Center, fontSize: 24);
        }
    }
}
