using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static SFML.Graphics.Text;
using static SFML.Window.Keyboard;

namespace MMXOnline
{
    public class ControlMenu : IMainMenu
    {
        public int selectArrowPosY;
        public IMainMenu previous;
        public bool listenForKey = false;
        public int bindFrames = 0;
        public string error;
        public bool inGame;
        Dictionary<string, int?> mappingClone;
        public bool startedAsJoystick;
        public bool isController;
        public List<string[]> bindableControls;
        const float ySpace = 7;
        const uint fontSize = 24;
        public const float startX = 70;

        public int charNum;
        public int axlAimMode;

        public ControlMenu(IMainMenu mainMenu, bool inGame, bool isController, int charNum, int axlAimMode)
        {
            previous = mainMenu;
            this.inGame = inGame;
            this.isController = isController;
            this.charNum = charNum;
            this.axlAimMode = axlAimMode;

            if (isController)
            {
                mappingClone = new Dictionary<string, int?>(Control.getControllerMapping(charNum, axlAimMode, true));
                startedAsJoystick = true;
            }
            else
            {
                mappingClone = new Dictionary<string, int?>(Control.getKeyboardMapping(charNum, axlAimMode, true));
            }

            bindableControls = new List<string[]>()
            {
                new string[] { Control.Up, "Up" },
                new string[] { Control.Down, "Down" },
                new string[] { Control.Left, "Left" },
                new string[] { Control.Right, "Right" },
                new string[] { Control.Jump, "Jump" },
                new string[] { Control.Shoot, "Shoot" },
                new string[] { Control.Dash, "Dash" },
                new string[] { Control.Special1, "Special" },
                new string[] { Control.WeaponLeft, "WeaponL" },
                new string[] { Control.WeaponRight, "WeaponR" },
            };

            // General menu controls not to be overridden on characters
            if (charNum == -1)
            {
                bindableControls.Add(new string[] { Control.Scoreboard, "Scoreboard" });
                bindableControls.Add(new string[] { Control.MenuSelectPrimary, "Menu Select" });
                bindableControls.Add(new string[] { Control.MenuSelectSecondary, "Menu Secondary" });
                bindableControls.Add(new string[] { Control.MenuBack, "Menu Back" });
                bindableControls.Add(new string[] { Control.MenuUp, "Menu Up" });
                bindableControls.Add(new string[] { Control.MenuDown, "Menu Down" });
                bindableControls.Add(new string[] { Control.MenuLeft, "Menu Left" });
                bindableControls.Add(new string[] { Control.MenuRight, "Menu Right" });
                bindableControls.Add(new string[] { Control.MenuEnter, "In-Game Menu" });
                bindableControls.Add(new string[] { Control.AllChat, "All Chat" });
                bindableControls.Add(new string[] { Control.TeamChat, "Team Chat" });
                bindableControls.Add(new string[] { Control.Taunt, "Taunt" });
            }

            if (charNum == 4)
            {
                bindableControls.Add(new string[] { Control.SigmaCommand, "Command Button" });
            }

            // Axl specific settings
            if (charNum == 3)
            {
                if (axlAimMode == 0)
                {
                    bindableControls.Add(new string[] { Control.AimUp, "Aim Up" });
                    bindableControls.Add(new string[] { Control.AimDown, "Aim Down" });
                    bindableControls.Add(new string[] { Control.AimLeft, "Aim Left" });
                    bindableControls.Add(new string[] { Control.AimRight, "Aim Right" });
                    bindableControls.Add(new string[] { Control.AxlAimBackwards, "Aim Key" });
                    bindableControls.Add(new string[] { Control.AxlCrouch, "Crouch" });
                }
                else if (axlAimMode == 1)
                {
                    bindableControls.Add(new string[] { Control.AimAngleUp, "Aim Angle Up" });
                    bindableControls.Add(new string[] { Control.AimAngleDown, "Aim Angle Down" });
                    bindableControls.Add(new string[] { Control.AimAngleReset, "Aim Angle Reset" });
                    bindableControls.Add(new string[] { Control.AxlAimBackwards, "Aim Backwards" });
                    bindableControls.Add(new string[] { Control.AxlCrouch, "Crouch" });
                }
                else if (axlAimMode == 2)
                {
                    bindableControls.Add(new string[] { Control.AimUp, "Aim Up" });
                    bindableControls.Add(new string[] { Control.AimDown, "Aim Down" });
                    bindableControls.Add(new string[] { Control.AimLeft, "Aim Left" });
                    bindableControls.Add(new string[] { Control.AimRight, "Aim Right" });
                }
            }
        }

        public bool isBindingControl()
        {
            return listenForKey;
        }

        public void update()
        {
            if (isController && !Control.isJoystick())
            {
                Menu.change(previous);
                return;
            }

            if (!listenForKey && !string.IsNullOrEmpty(error))
            {
                if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
                {
                    error = null;
                }
                return;
            }

            if (listenForKey)
            {
                if (bindFrames > 0)
                {
                    bindFrames--;
                    if(bindFrames <= 0)
                    {
                        bindFrames = 0;
                        listenForKey = false;
                    }
                }
                return;
            }

            Helpers.menuUpDown(ref selectArrowPosY, 0, bindableControls.Count - 1);
            if (Global.input.isPressedMenu(Control.MenuBack))
            {
                /*
                if (mappingClone.Any(v => v.Key != Control.AimLock && v.Value == null))
                {
                    error = "Error: Missing binding(s).";
                    return;
                }
                */

                if (isController)
                {
                    Control.setControllerMapping(mappingClone, charNum, axlAimMode);
                }
                else
                {
                    Control.setKeyboardMapping(mappingClone, charNum, axlAimMode);
                }

                Control.saveToFile();

                Menu.change(previous);
            }
            else if (Global.input.isPressedMenu(Control.MenuSelectPrimary))
            {
                listenForKey = true;
            }
            else if (Global.input.isPressedMenu(Control.MenuSelectSecondary))
            {
                string inputName = bindableControls[selectArrowPosY][0];
                if (mappingClone.ContainsKey(inputName)) mappingClone[inputName] = null;
            }
        }

        public void bind(int key)
        {
            string inputName = bindableControls[selectArrowPosY][0];

            if (!mappingClone.ContainsKey(inputName) || mappingClone[inputName] != (int)key)
            {
                var keysToClear = new List<string>();
                foreach (var kvp in mappingClone)
                {
                    if (kvp.Key.StartsWith("menu", StringComparison.OrdinalIgnoreCase)) continue;
                    if (inputName.StartsWith("menu", StringComparison.OrdinalIgnoreCase)) continue;
                    if (kvp.Key.StartsWith("aim", StringComparison.OrdinalIgnoreCase)) continue;
                    if (inputName.StartsWith("aim", StringComparison.OrdinalIgnoreCase)) continue;
                    if (kvp.Key.StartsWith("sigmacommand", StringComparison.OrdinalIgnoreCase)) continue;
                    if (inputName.StartsWith("sigmacommand", StringComparison.OrdinalIgnoreCase)) continue;
                    // Jump and up are the only only other exceptions to the "can't bind multiple with one key" rule
                    if ((kvp.Key == Control.Jump && inputName == Control.Up) || kvp.Key == Control.Up && inputName == Control.Jump) continue;

                    if (kvp.Value == (int)key)
                    {
                        keysToClear.Add(kvp.Key);
                    }
                }
                foreach (var keyToClear in keysToClear)
                {
                    mappingClone[keyToClear] = null;
                }

                mappingClone[inputName] = (int)key;
            }
            
            bindFrames = 3;
            selectArrowPosY++;
            if (selectArrowPosY > bindableControls.Count - 1) selectArrowPosY = bindableControls.Count - 1;
        }

        public void render()
        {
            var topLeft = new Point(startX + 10, 28);
            int startYOff = 10;
            int cursorYOff = 6;

            if (!inGame)
            {
                DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
                DrawWrappers.DrawTextureHUD(Global.textures["cursor"], startX, topLeft.y + startYOff + (selectArrowPosY * ySpace) + cursorYOff);
            }
            else
            {
                DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, Helpers.MenuBgColor, 0, ZIndex.HUD + 200, false);
                Global.sprites["cursor"].drawToHUD(0, startX, topLeft.y + startYOff + (selectArrowPosY * ySpace) + cursorYOff + 5);
            }

            string subtitle = "GENERAL CONTROLS";
            if (charNum == 0) subtitle = "X CONTROLS";
            if (charNum == 1) subtitle = "ZERO CONTROLS";
            if (charNum == 2) subtitle = "VILE CONTROLS";
            if (charNum == 3)
            {
                if (axlAimMode == 0) subtitle = "AXL CONTROLS (DIRECTIONAL)";
                if (axlAimMode == 1) subtitle = "AXL CONTROLS (ANGULAR)";
                if (axlAimMode == 2) subtitle = "AXL CONTROLS (CURSOR)";
            }
            if (charNum == 4) subtitle = "SIGMA CONTROLS";

            Helpers.drawTextStd(TCat.Title, subtitle, Global.halfScreenW, 15, Alignment.Center, fontSize: 32);

            if (isController)
            {
                Helpers.drawTextStd("Setting controls for controller \"" + Control.getControllerName() + "\"", Global.halfScreenW, 32, alignment: Alignment.Center, fontSize: 16, style: Styles.Italic, color: Color.Yellow);
            }
            else
            {
                Helpers.drawTextStd("Setting controls for keyboard", Global.halfScreenW, 32, alignment: Alignment.Center, fontSize: 16, style: Styles.Italic, color: Color.Yellow);
            }

            for (int i = 0; i < bindableControls.Count; i++)
            {
                var bindableControl = bindableControls[i];
                string boundKeyDisplay = Control.getKeyOrButtonName(bindableControl[0], mappingClone, isController);
                if (string.IsNullOrEmpty(boundKeyDisplay) && charNum > -1)
                {
                    boundKeyDisplay = "(Inherit)";
                }
                Helpers.drawTextStd(TCat.Option, bindableControl[1].ToUpperInvariant() + ": " + boundKeyDisplay, topLeft.x, topLeft.y + startYOff + ySpace * (i + 1), fontSize: fontSize, selected: selectArrowPosY == i, optionPadding: 15);
            }

            if (!listenForKey)
            {
                Helpers.drawTextStd(Helpers.menuControlText("[X] = Bind, [C] = Unbind, [Z] = Save/Back", false), Global.halfScreenW, 210, Alignment.Center, fontSize: 24);
            }
            else
            {
                if (isController) Helpers.drawTextStd(TCat.BotHelp, "Press desired controller button to bind", Global.halfScreenW, 205, Alignment.Center, fontSize: 24);
                else Helpers.drawTextStd(TCat.BotHelp, "Press desired key to bind", Global.halfScreenW, 205, Alignment.Center, fontSize: 24);
            }

            if (!string.IsNullOrEmpty(error))
            {
                float top = Global.screenH * 0.4f;
                DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, new Color(0, 0, 0, 224), 0, ZIndex.HUD, false);
                Helpers.drawTextStd(error, Global.screenW / 2, top, alignment: Alignment.Center);
                Helpers.drawTextStd(TCat.BotHelp, "Press [X] to continue", Global.screenW / 2, 20 + top, alignment: Alignment.Center);
            }
        }
    }
}
