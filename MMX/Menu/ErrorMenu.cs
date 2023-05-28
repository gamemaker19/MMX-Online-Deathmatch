using SFML.Graphics;

namespace MMXOnline
{
    public class ErrorMenu : IMainMenu
    {
        string[] error;
        IMainMenu prevMenu;
        bool inGame;

        public ErrorMenu(string error, IMainMenu prevMenu, bool inGame = false)
        {
            this.error = new string[] { error };
            this.prevMenu = prevMenu;
            this.inGame = inGame;
        }

        public ErrorMenu(string[] error, IMainMenu prevMenu, bool inGame = false)
        {
            this.error = error;
            this.prevMenu = prevMenu;
            this.inGame = inGame;
        }

        public void update()
        {
            if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
            {
                Menu.change(prevMenu);
            }
        }

        public void render()
        {
            float top = Global.screenH * 0.4f;

            if (!inGame) DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);

            DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, new Color(0, 0, 0, 224), 0, ZIndex.HUD, false);

            int i = 0;
            for (; i < error.Length; i++)
            {
                Helpers.drawTextStd(error[i], Global.screenW / 2, top + (i * 20), alignment: Alignment.Center, fontSize: 24);
            }
            Helpers.drawTextStd(TCat.BotHelp, "Press [X] to continue", Global.screenW / 2, top + (i * 20), alignment: Alignment.Center, fontSize: 24);
        }
    }
}
