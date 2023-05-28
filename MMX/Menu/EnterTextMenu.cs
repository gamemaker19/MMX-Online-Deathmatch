using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SFML.Window.Keyboard;

namespace MMXOnline
{
    public class EnterTextMenu : IMainMenu
    {
        public string message;
        public string text = "";
        public float blinkTime = 0;
        Action<string> submitAction;
        public int maxLength;
        public bool allowEmpty;

        public EnterTextMenu(string message, int maxLength, Action<string> submitAction, bool allowEmpty = false)
        {
            this.message = message;
            this.maxLength = maxLength;
            this.submitAction = submitAction;
            this.allowEmpty = allowEmpty;
        }

        public void update()
        {
            blinkTime += Global.spf;
            if (blinkTime >= 1f) blinkTime = 0;

            text = Helpers.getTypedString(text, maxLength);

            if (Global.input.isPressed(Key.Enter) && (allowEmpty || !string.IsNullOrWhiteSpace(text.Trim())))
            {
                text = Helpers.censor(text);
                submitAction(text);
            }
        }

        public void render()
        {
            float top = Global.screenH * 0.4f;

            DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, Color.Black, 0, ZIndex.HUD, false);
            Helpers.drawTextStd(message, Global.screenW / 2, top, alignment: Alignment.Center);

            float xPos = Global.screenW * 0.33f;
            Helpers.drawTextStd(text, xPos, 20 + top, alignment: Alignment.Left);
            if (blinkTime >= 0.5f)
            {
                float width = Helpers.measureTextStd(TCat.Default, text).x;
                Helpers.drawTextStd("<", xPos + width + 3, 20 + top, alignment: Alignment.Left);
            }

            Helpers.drawTextStd("Press Enter to continue", Global.screenW / 2, 40 + top, alignment: Alignment.Center);
        }

    }
}
