using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class Menu
    {
        public static IMainMenu mainMenu;

        public static bool inMenu { get { return mainMenu != null; } }
        public static bool inChat { get { return chatMenu != null && chatMenu.typingChat; } }

        public static bool inControlMenu { get { return mainMenu is ControlMenu; } }

        public static ChatMenu chatMenu
        {
            get
            {
                ChatMenu chatMenu = Global.level?.gameMode?.chatMenu;
                return chatMenu;
            }
        }

        public static void change(IMainMenu newMenu)
        {
            //if (Global.level?.mainPlayer?.isUsingSubTank() == true) return;
            mainMenu = newMenu;
        }

        public static void exit()
        {
            mainMenu = null;
        }

        public static void update()
        {
            if (chatMenu != null)
            {
                chatMenu.update();
                if (chatMenu.typingChat) return;
                if (chatMenu.recentlyExited) return;
            }
            if (mainMenu != null && Global.level?.gameMode?.drawingScoreboard != true)
            {
                mainMenu.update();
            }
        }

        public static void render()
        {
            if (chatMenu != null)
            {
                chatMenu.render();
            }
            if (mainMenu != null && Global.level?.gameMode?.drawingScoreboard != true)
            {
                mainMenu.render();
            }
        }

        public static bool isHostEndMenu()
        {
            return mainMenu is HostMenu || mainMenu is ConfirmLeaveMenu;
        }

    }
}
