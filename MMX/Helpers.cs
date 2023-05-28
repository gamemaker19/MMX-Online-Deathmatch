using ProtoBuf;
using SFML.Graphics;
using SFML.System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using static SFML.Graphics.Text;

namespace MMXOnline
{
    public class Helpers
    {
        public static Color Gray { get { return new Color(128, 128, 128); } }
        public static Color DarkRed { get { return new Color(192, 0, 0); } }
        public static Color DarkBlue { get { return new Color(0, 0, 192); } }
        public static Color MenuBgColor { get { return new Color(0, 0, 0, 224); } }
        public static Color FadedIconColor = new Color(0, 0, 0, 164);
        public static Color LoadoutBorderColor = new Color(138, 192, 255);
        public static Color DarkGreen
        {
            get
            {
                if (Global.level == null) return new Color(64, 255, 64);
                //return new Color(0, (byte)(200 + (MathF.Sin(Global.time * 4) * 55)), (byte)(63 + (MathF.Sin(Global.time * 4) * 63)));
                return new Color(0, 209, 63);
            }
        }

        public static List<Type> wallTypeList = new List<Type> { typeof(Wall) };

        public static void decrementTime(ref float num)
        {
            num = clampMin0(num - Global.spf);
        }

        public static float clampMin0(float num)
        {
            if (num < 0) return 0;
            return num;
        }

        public static int clamp(int val, int min, int max)
        {
            if (min > max) return val;
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        public static float clamp(float val, float min, float max)
        {
            if (min > max) return val;
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        public static float clampMin(float val, float min)
        {
            if (val < min) return min;
            return val;
        }

        public static float clampMax(float val, float max)
        {
            if (val > max) return max;
            return val;
        }

        public static string getTypedString(string str, int maxLength)
        {
            var pressedChar = Global.input.getKeyCharPressed();
            if (pressedChar != null)
            {
                if (pressedChar == Input.backspaceChar)
                {
                    if (str.Length > 0)
                    {
                        str = str.Substring(0, str.Length - 1);
                    }
                }
                else if (str.Length < maxLength)
                {
                    str += pressedChar;
                }
            }

            return str;
        }

        public static float clamp01(float val)
        {
            return clamp(val, 0, 1);
        }

        public static Color getAllianceColor(Player player)
        {
            if (player == null)
            {
                return Helpers.DarkBlue;
            }
            if (player.alliance == GameMode.blueAlliance)
            {
                return Helpers.DarkBlue;
            }
            else if (player.alliance == GameMode.redAlliance)
            {
                return Helpers.DarkRed;
            }
            else
            {
                return Helpers.DarkBlue;
            }
        }

        public static Color getAllianceColor()
        {
            return getAllianceColor(Global.level?.mainPlayer);
        }

        public static bool shouldUseVectorFont(TCat textCategory)
        {
            if ((textCategory == TCat.HUD || textCategory == TCat.Chat || textCategory == TCat.HUDColored) && Options.main.fontType != 0) return true;
            if (Options.main.fontType == 2) return true;
            return false;
        }

        public static bool shouldUseOutlineFont(TCat textCategory)
        {
            if (textCategory == TCat.HUD || textCategory == TCat.Chat) return true;
            return false;
        }

        public static float getWorldTextFontSize()
        {
            return 0.75f;
        }

        public static void drawTextStd(string textStr, float x, float y, Alignment alignment = Alignment.Left, bool outline = true, uint fontSize = 36, Color? color = null,
            Color? outlineColor = null, Styles style = Styles.Regular, float? outlineThickness = null, float alpha = 1,
            float lineMargin = DrawWrappers.defaultLineMargin, VAlignment vAlignment = VAlignment.Top, bool selected = false, bool useVectorFont = false, bool isWorldPos = false)
        {
            drawTextStd(TCat.Default, textStr, x, y, alignment, outline, fontSize, color, outlineColor, style, outlineThickness, alpha, lineMargin, vAlignment, useVectorFont, isWorldPos);
        }

        public static void drawTextStd(TCat textCategory, string textStr, float x, float y, Alignment alignment = Alignment.Left, bool outline = true, uint fontSize = 36, Color? color = null, 
            Color? outlineColor = null, Styles style = Styles.Regular, float? outlineThickness = null, float alpha = 1,
            float lineMargin = DrawWrappers.defaultLineMargin, VAlignment vAlignment = VAlignment.Top, bool selected = false, bool isWorldPos = false, int optionPadding = 0)
        {
            if (shouldUseVectorFont(textCategory))
            {
                if (color == null)
                {
                    color = new Color(255, 255, 255, Helpers.toColorByte(alpha));
                }
                else if (alpha != 1)
                {
                    color = new Color(color.Value.R, color.Value.G, color.Value.B, Helpers.toColorByte(alpha));
                }
                drawTextStdFont(textStr, x, y, alignment, outline, fontSize, color, outlineColor, style, outlineThickness, textCategory == TCat.HUD, null, isWorldPos);
                return;
            }

            // Pipeline step 1: auto-generate outlines if none passed in
            if (outlineThickness == null)
            {
                if (fontSize >= 36) outlineThickness = 4;
                else if (fontSize >= 24) outlineThickness = 3;
                else outlineThickness = 2;
            }

            // Pipeline step 2: sizes are "normalized" at 4x what they are supposed to be. This is a mistake but to avoid massive refactor, will "shim" it for now
            fontSize = normalizeFontSize(fontSize);
            outlineThickness = outlineThickness / 4f;

            // Pipeline step 3: misc
            if (textCategory == TCat.HUD)
            {
                if (Global.level?.gameMode != null && Global.level.gameMode.isTeamMode)
                {
                    outlineColor = getAllianceColor();
                }
                else
                {
                    outlineColor = Helpers.DarkBlue;
                }
            }

            MMXFont font = MMXFont.Gray;
            if (textCategory == TCat.Option)
            {
                font = MMXFont.Menu;
                var pieces = textStr.Split(": ");
                if (pieces.Length > 1)
                {
                    string padding = "";
                    if (optionPadding > 0)
                    {
                        padding = new string(' ', optionPadding - pieces[0].Length);
                    }
                    textStr = pieces[0].ToUpperInvariant() + ": " + padding + pieces[1];
                }
                else
                {
                    textStr = textStr.ToUpperInvariant();
                }
            }
            if (textCategory == TCat.OptionNoSplit)
            {
                font = MMXFont.Menu;
                textStr = textStr.ToUpperInvariant();
            }
            if (textCategory == TCat.BotHelp)
            {
                textStr = Helpers.menuControlText(textStr);
                font = MMXFont.Gray;
            }
            if (textCategory == TCat.Title)
            {
                font = MMXFont.Title;
                textStr = textStr.ToUpperInvariant();
            }
            if (textCategory == TCat.Default)
            {
                font = MMXFont.Gray;
            }
            if (textCategory == TCat.BotHelp)
            {
                font = MMXFont.Gray;
            }
            if (selected)
            {
                font = MMXFont.Select;
            }
            if (textCategory == TCat.Chat)
            {
                font = MMXFont.Gray;
            }

            if (outlineColor == Helpers.DarkBlue) font = MMXFont.Blue;
            if (outlineColor == Helpers.DarkRed) font = MMXFont.Red;

            DrawWrappers.DrawText(textStr, x, y, alignment, outline, fontSize / 8f, color, outlineColor, style, outlineThickness.Value, isWorldPos, 0, font, vAlignment: vAlignment, lineMargin: lineMargin, alpha: alpha);
        }

        public static string addPrefix(dynamic dStr, string prefix)
        {
            string str = dStr ?? "";
            if (!string.IsNullOrEmpty(str))
            {
                return prefix + str;
            }
            return str;
        }

        public static Point measureTextStd(TCat textCategory, string textStr, bool outline = true, uint fontSize = 36, float outlineThickness = 0)
        {
            /*
            if (shouldUseVectorFont(textCategory))
            {
                fontSize = normalizeFontSizeFont(fontSize);
                return DrawWrappers.measureTextStdFont(textStr, outlineThickness, fontSize);
            }
            */

            fontSize = normalizeFontSize(fontSize);
            return DrawWrappers.measureTextStd(textStr, outline, fontSize);
        }

        public static uint normalizeFontSize(uint fontSize)
        {
            fontSize = (uint)(fontSize / 4f);

            return fontSize;
        }

        public static void drawTextStdFont(string textStr, float x, float y, Alignment alignment = Alignment.Left,
            bool outline = true, uint fontSize = 36, Color? color = null, Color? outlineColor = null, Styles style = Styles.Regular,
            float? outlineThickness = null, bool matchAlliance = false, uint? lowQualityFontSize = null, bool isWorldPos = false)
        {
            // Pipeline step 1: auto-generate outlines if none passed in
            if (outlineThickness == null)
            {
                if (fontSize >= 36) outlineThickness = 4;
                else if (fontSize >= 24) outlineThickness = 3;
                else outlineThickness = 2;
            }

            // Pipeline step 2: sizes are "normalized" at 4x what they are supposed to be. This is a mistake but to avoid massive refactor, will "shim" it for now
            fontSize = normalizeFontSizeFont(fontSize);
            outlineThickness = outlineThickness / 4f;

            // Pipeline step 3: misc
            if (matchAlliance && Global.level?.gameMode != null && Global.level.gameMode.isTeamMode)
            {
                outlineColor = getAllianceColor();
            }

            DrawWrappers.DrawTextFont(textStr, x, y, alignment, outline, fontSize, color, outlineColor, style, outlineThickness.Value, isWorldPos, 0);
        }

        public static uint normalizeFontSizeFont(uint fontSize)
        {
            fontSize = (uint)(fontSize / 4f);
            
            return fontSize;
        }

        public static void drawWeaponSlotSymbol(float topLeftSlotX, float topLeftSlotY, string symbol)
        {
            drawTextStd(symbol, topLeftSlotX + 16, topLeftSlotY + 12, Alignment.Right, fontSize: 12);
        }

        static Random rnd = new Random();
        //Inclusive
        public static int randomRange(int start, int end)
        {
            int rndNum = rnd.Next(start, end + 1);
            return rndNum;
        }

        public static float randomRange(float start, float end)
        {
            double rndNum = rnd.NextDouble() * (end - start);
            rndNum += start;
            return (float)rndNum;
        }

        public static void tryWrap(Action action, bool isServer)
        {
#if !DEBUG
            try
            {
                action.Invoke();
            }
            catch (AccessViolationException) { throw; }
            catch (StackOverflowException) { throw; }
            catch (OutOfMemoryException) { throw; }
            catch (Exception e)
            {
                Logger.logException(e, isServer);
            }
#else
            action.Invoke();
#endif
        }

        public static List<T> getRandomSubarray<T>(List<T> list, int count)
        {
            if (count >= list.Count)
                count = list.Count - 1;

            int[] indexes = Enumerable.Range(0, list.Count).ToArray();

            List<T> results = new List<T>();

            for (int i = 0; i < count; i++)
            {
                int j = randomRange(i, list.Count - 1);

                int temp = indexes[i];
                indexes[i] = indexes[j];
                indexes[j] = temp;

                results.Add(list[indexes[i]]);
            }

            return results;
        }

        public static string[] openglversions = new string[]
        {
            "#version 110",
            "#version 120",
            "#version 130",
            "#version 140",
            "#version 150",
            "#version 330",
            "#version 400",
            "#version 410",
            "#version 420",
            "#version 430",
            "#version 440",
            "#version 450",
            "#version 460",
        };

        public const string noShaderSupportMsg = "The system does not support shaders.";

        // Very slow, only do once on startup
        public static Shader createShader(string shaderName)
        {
            string shaderCode = Global.shaderCodes[shaderName];

            var result = createShaderHelper(shaderCode, "");
            if (result != null) return result;

            if (!Shader.IsAvailable)
            {
                var ex = new Exception(noShaderSupportMsg);
                //Logger.logException(ex, false);
                throw ex;
            }

            var ex2 = new Exception("Could not load shaders after trying all possible opengl versions.");
            //Logger.logException(ex2, false);
            throw ex2;
        }

        // Very slow, only do once on startup
        private static Shader createShaderHelper(string shaderCode, string header)
        {
            if (!string.IsNullOrEmpty(header)) header += Environment.NewLine;
            byte[] byteArray = Encoding.ASCII.GetBytes(header + shaderCode);
            MemoryStream stream = new MemoryStream(byteArray);
            try
            {
                return new Shader(null, null, stream);
            }
            catch
            {
                stream.Dispose();
                return null;
            }
        }

        // Fast way to get a new shader wrapper that remembers SetUniform state while reusing the same base underlying shader
        public static ShaderWrapper cloneShaderSafe(string shaderName)
        {
            if (!Global.shaders.ContainsKey(shaderName))
            {
                return null;
            }
            return new ShaderWrapper(shaderName);
        }

        public static ShaderWrapper cloneGenericPaletteShader(string textureName)
        {
            var texture = Global.textures[textureName];
            var genericPaletteShader = cloneShaderSafe("genericPalette");
            genericPaletteShader?.SetUniform("paletteTexture", texture);
            genericPaletteShader?.SetUniform("palette", 1);
            genericPaletteShader?.SetUniform("rows", (float)texture.Size.Y);
            genericPaletteShader?.SetUniform("cols", (float)texture.Size.X);
            return genericPaletteShader;
        }

        public static ShaderWrapper cloneNightmareZeroPaletteShader(string textureName)
        {
            var texture = Global.textures[textureName];
            var genericPaletteShader = cloneShaderSafe("nightmareZero");
            genericPaletteShader?.SetUniform("paletteTexture", texture);
            genericPaletteShader?.SetUniform("palette", 1);
            genericPaletteShader?.SetUniform("rows", (float)texture.Size.Y);
            genericPaletteShader?.SetUniform("cols", (float)texture.Size.X);
            return genericPaletteShader;
        }

        public static float toZero(float num, float inc, int dir)
        {
            if (dir == 1)
            {
                num -= inc;
                if (num < 0) num = 0;
                return num;
            }
            else if (dir == -1)
            {
                num += inc;
                if (num > 0) num = 0;
                return num;
            }
            else
            {
                throw new Exception("Must pass in -1 or 1 for dir");
            }
        }

        public static float sind(float degrees)
        {
            var radians = degrees * MathF.PI / 180f;
            return MathF.Sin(radians);
        }

        public static float cosd(float degrees)
        {
            var radians = degrees * MathF.PI / 180f;
            return MathF.Cos(radians);
        }

        public static float moveTo(float num, float dest, float inc, bool snap = false)
        {
            float diff = dest - num;
            inc *= MathF.Sign(diff);
            if (snap && MathF.Abs(diff) < MathF.Abs(inc * 2))
            {
                return dest;
            }
            num += inc;
            return num;
        }

        public static float RoundEpsilon(float num)
        {
            var numRound = MathF.Round(num);
            var diff = MathF.Abs(numRound - num);
            if (diff < 0.0001f)
            {
                return numRound;
            }
            return num;
        }

        static int autoInc = 0;
        public static int getAutoIncId()
        {
            autoInc++;
            return autoInc;
        }

        //Expects angle and destAngle to be > 0 and < 360
        public static float lerpAngle(float angle, float destAngle, float timeScale)
        {
            var dir = 1;
            if (MathF.Abs(destAngle - angle) > 180)
            {
                dir = -1;
            }
            angle = angle + dir * (destAngle - angle) * timeScale;
            return to360(angle);
        }

        public static float lerp(float num, float dest, float timeScale)
        {
            return num + (dest - num) * timeScale;
        }

        public static float moveAngle(float angle, float destAngle, float timeScale, bool snap = false)
        {
            var dir = 1;
            if (MathF.Abs(destAngle - angle) > 180)
            {
                dir = -1;
            }
            angle = angle + dir * MathF.Sign(destAngle - angle) * timeScale;

            if (snap && MathF.Abs(destAngle - angle) < timeScale * 2)
            {
                angle = destAngle;
            }

            return to360(angle);
        }

        public static float to360(float angle)
        {
            if (angle < 0) angle += 360;
            if (angle > 360) angle -= 360;
            return angle;
        }

        // Given 2 angles, get the smallest difference between their values.
        // Math.Abs(angle1 - angle2) won't work in such cases as angle1 = 359 and angle2 = 0, the closest angle difference should be 1
        public static float getClosestAngleDiff(float angle1, float angle2)
        {
            angle1 = to360(angle1);
            angle2 = to360(angle2);
            float diff = Math.Abs(angle1 - angle2);
            if (diff > 180)
            {
                return 360 - diff;
            }
            return diff;
        }

        public static int incrementRange(int num, int min, int max)
        {
            num++;
            if (num >= max) num = min;
            return num;
        }

        public static int decrementRange(int num, int min, int max)
        {
            num--;
            if (num < min) num = max - 1;
            return num;
        }

        public static byte[] convertToBytes(short networkId)
        {
            return BitConverter.GetBytes(networkId);
        }

        public static byte[] convertToBytes(ushort networkId)
        {
            return BitConverter.GetBytes(networkId);
        }

        public static byte toColorByte(float value)
        {
            return (byte)(int)(255f * clamp01(value));
        }

        public static byte toByte(float value)
        {
            return (byte)(int)(value);
        }

        public static byte angleToByte(float netArmAngle)
        {
            float newAngle = netArmAngle;
            if (newAngle < 0) newAngle += 360;
            if (newAngle > 360) newAngle -= 360;
            newAngle /= 2;
            return (byte)((int)newAngle);
        }

        public static float byteToAngle(byte angleByte)
        {
            return (float)angleByte * 2;
        }

        public static byte dirToByte(int dir)
        {
            return (byte)(dir + 128);
        }

        public static int byteToDir(byte dirByte)
        {
            return (int)(dirByte - 128);
        }

        public static string menuControlText(string text, bool isController = false)
        {
            return "(" + controlText(text, isController) + ")";
        }

        public static string controlText(string text, bool isController = false)
        {
            if (isController) isController = Control.isJoystick();

            text = text.Replace("[X]", Control.getKeyOrButtonName(Control.MenuSelectPrimary, isController));
            text = text.Replace("[C]", Control.getKeyOrButtonName(Control.MenuSelectSecondary, isController));
            text = text.Replace("[ATTACK]", Control.getKeyOrButtonName(Control.Shoot, isController));
            text = text.Replace("[Z]", Control.getKeyOrButtonName(Control.MenuBack, isController));
            text = text.Replace("[D]", Control.getKeyOrButtonName(Control.Special1, isController));
            text = text.Replace("[ESC]", Control.getKeyOrButtonName(Control.MenuEnter, isController));
            text = text.Replace("[TAB]", Control.getKeyOrButtonName(Control.Scoreboard, isController));
            text = text.Replace("[JUMP]", Control.getKeyOrButtonName(Control.Jump, isController));
            text = text.Replace("[DASH]", Control.getKeyOrButtonName(Control.Dash, isController));
            text = text.Replace("[WeaponL]", Control.getKeyOrButtonName(Control.WeaponLeft, isController));
            text = text.Replace("[WeaponR]", Control.getKeyOrButtonName(Control.WeaponRight, isController));
            return text;
        }

        public static string removeMapSuffix(string mapName)
        {
            return mapName.Replace("_md", "").Replace("_1v1", "");
        }

        public static int getGridCoordKey(ushort x, ushort y)
        {
            return x << 16 | y;
        }

        public static void showMessageBox(string message, string caption)
        {
#if WINDOWS
            if (Global.window != null) Global.window.SetMouseCursorVisible(true);
            System.Windows.Forms.MessageBox.Show(message, caption);
            if (Global.window != null && Options.main != null) Global.window.SetMouseCursorVisible(!Options.main.fullScreen);
#else
            Console.WriteLine(caption + Environment.NewLine + message);
#endif
        }

        public static bool showMessageBoxYesNo(string message, string caption)
        {
#if WINDOWS
            if (Global.window != null) Global.window.SetMouseCursorVisible(true);
            System.Windows.Forms.DialogResult dialogResult = System.Windows.Forms.MessageBox.Show(message, caption, System.Windows.Forms.MessageBoxButtons.YesNo);
            if (Global.window != null && Options.main != null) Global.window.SetMouseCursorVisible(!Options.main.fullScreen);

            if (dialogResult == System.Windows.Forms.DialogResult.Yes)
            {
                return true;
            }
            else
            {
                return false;
            }
#else
            Console.WriteLine(caption + Environment.NewLine + message);
            return true;
#endif
        }

        public static void menuUpDown(ref int val, int minVal, int maxVal, bool wrap = true, bool playSound = true)
        {
            if (Global.input.isPressedMenu(Control.MenuUp))
            {
                menuDec(ref val, minVal, maxVal, wrap, playSound);
            }
            else if (Global.input.isPressedMenu(Control.MenuDown))
            {
                menuInc(ref val, minVal, maxVal, wrap, playSound);
            }
        }

        public static void menuDec(ref int val, int minVal, int maxVal, bool wrap = true, bool playSound = true)
        {
            val--;
            if (val < minVal)
            {
                val = wrap ? maxVal : minVal;
                if (wrap)
                {
                    Global.playSound("menu");
                }
            }
            else
            {
                Global.playSound("menu");
            }
        }

        public static void menuInc(ref int val, int minVal, int maxVal, bool wrap = true, bool playSound = true)
        {
            val++;
            if (val > maxVal)
            {
                val = wrap ? minVal : maxVal;
                if (wrap)
                {
                    Global.playSound("menu");
                }
            }
            else
            {
                Global.playSound("menu");
            }
        }

        public static void menuLeftRightInc(ref int val, int min, int max, bool wrap = false, bool playSound = false)
        {
            if (min == max) return;
            if (Global.input.isPressedMenu(Control.MenuLeft))
            {
                val--;
                if (val < min)
                {
                    val = wrap ? max : min;
                    if (wrap && playSound) Global.playSound("menu");
                }
                else
                {
                    if (playSound) Global.playSound("menu");
                }
            }
            else if (Global.input.isPressedMenu(Control.MenuRight))
            {
                val++;
                if (val > max)
                {
                    val = wrap ? min : max;
                    if (wrap && playSound) Global.playSound("menu");
                }
                else
                {
                    if (playSound) Global.playSound("menu");
                }
            }
        }

        public static void menuLeftRightBool(ref bool val)
        {
            if (Global.input.isPressedMenu(Control.MenuLeft))
            {
                val = false;
            }
            else if (Global.input.isPressedMenu(Control.MenuRight))
            {
                val = true;
            }
        }

        public static List<Weapon> sortWeapons(List<Weapon> weapons, int weaponOrdering)
        {
            if (weaponOrdering == 1)
            {
                if (weapons.Count == 3) return new List<Weapon>() { weapons[1], weapons[0], weapons[2] };
                else if (weapons.Count == 2) return new List<Weapon>() { weapons[1], weapons[0] };
            }
            return weapons;
        }

        public static string boolYesNo(bool b)
        {
            return b ? "Yes" : "No";
        }

        public static void debugLog(string message)
        {
            if (Global.debug)
            {
                Console.WriteLine(message);
            }
        }

        private static ProfanityFilter.ProfanityFilter _profanityFilter;
        public static ProfanityFilter.ProfanityFilter profanityFilter
        {
            get
            {
                if (_profanityFilter == null)
                {
                    _profanityFilter = new ProfanityFilter.ProfanityFilter(BadWords.badWords);
                    _profanityFilter.AllowList.Add("azazel");
                }
                return _profanityFilter;
            }
        } 
        public static string censor(string text)
        {
            var censored = profanityFilter.CensorString(text);
            return censored;
        }

        public static List<string> getFiles(string path, bool recursive, params string[] filters)
        {
            var files = new List<string>();
            if (Directory.Exists(path))
            {
                files = Directory.GetFiles(path, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToList();
            }

            return files.Where(f =>
            {
                if (filters == null) return true;
                foreach (var filter in filters)
                {
                    if (f.EndsWith(filter, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            }).Select(f => normalizePath(f)).ToList();
        }

        public static string getBaseDocumentsPath()
        {
            try
            {
                return normalizePath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            }
            catch
            {
                return null;
            }
        }

        public static string getMMXODDocumentsPath()
        {
            string myDocumentsPath = getBaseDocumentsPath();
            if (!string.IsNullOrEmpty(myDocumentsPath))
            {
                string fullPath = myDocumentsPath + "/MMXOD/";
                if (Directory.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            return "";
        }

        public static string normalizePath(string path)
        {
            return path.Replace('\\', '/').Replace("\\\\", "/");
        }

        public static bool FileExists(string filePath)
        {
            filePath = Global.writePath + filePath;
            if (File.Exists(filePath))
            {
                return true;
            }
            return false;
        }

        public static string ReadFromFile(string filePath)
        {
            filePath = Global.writePath + filePath;
            string text = "";
            if (File.Exists(filePath))
            {
                text = File.ReadAllText(filePath);
            }
            return text;
        }

        public static string WriteToFile(string filePath, string text)
        {
            filePath = Global.writePath + filePath;
            try
            {
                if (File.Exists(filePath))
                {
                    File.SetAttributes(filePath, FileAttributes.Normal);
                }
                File.WriteAllText(filePath, text);
                return null;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public void CreateFileWithDirectory(string filePath)
        {
            if (!File.Exists(filePath))
            {
                var parent = Directory.GetParent(filePath);
                Directory.CreateDirectory(parent.FullName);
                File.Create(filePath).Dispose();
            }
        }

        public static T deserialize<T>(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                return Serializer.Deserialize<T>(stream);
            }
        }

        public static byte[] serialize<T>(T obj)
        {
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, obj);
                return stream.ToArray();
            }
        }

        public static T cloneProtobuf<T>(T obj)
        {
            return deserialize<T>(serialize(obj));
        }

        public static bool getByteValue(byte byteValue, int index)
        {
            List<bool> bits = Convert.ToString(byteValue, 2).Select(s => s == '0' ? false : true).ToList();
            while (bits.Count < 8)
            {
                bits.Insert(0, false);
            }
            return bits[index];
        }

        public static void setByteValue(ref byte byteValue, int index, bool value)
        {
            List<char> bits = Convert.ToString(byteValue, 2).ToList();
            while (bits.Count < 8)
            {
                bits.Insert(0, '0');
            }
            bits[index] = value == true ? '1' : '0';
            byteValue = Convert.ToByte(string.Join("", bits), 2);
        }

        public static byte boolArrayToByte(bool[] boolArray)
        {
            string bitString = "";
            for (int i = 0; i < 8; i++)
            {
                bitString += boolArray[i] ? '1' : '0';
            }
            return Convert.ToByte(string.Join("", bitString), 2);
        }

        public static bool[] byteToBoolArray(byte byteValue)
        {
            List<bool> bits = Convert.ToString(byteValue, 2).Select(s => s == '0' ? false : true).ToList();
            while (bits.Count < 8)
            {
                bits.Insert(0, false);
            }
            return bits.ToArray();
        }

        public static Point? getClosestHitPoint(List<CollideData> hits, Point pos, params Type[] types)
        {
            hits.RemoveAll(h => !isOfClass(h.gameObject, types.ToList()));

            var points = new List<Point>();
            foreach (var hit in hits)
            {
                if (hit.hitData?.hitPoints != null)
                {
                    points.AddRange(hit.hitData.hitPoints);
                }
            }

            Point? bestPoint = null;
            float minDist = float.MaxValue;
            foreach (var point in points)
            {
                float dist = point.distanceTo(pos);
                if (dist < minDist)
                {
                    minDist = dist;
                    bestPoint = point;
                }
            }

            return bestPoint;
        }

        public static bool isOfClass(object go, Type type)
        {
            if (go == null) return false;
            if (go.GetType() == type || go.GetType().IsSubclassOf(type))
            {
                return true;
            }
            return false;
        }

        public static bool isOfClass(object go, List<Type> classNames)
        {
            if (classNames == null || classNames.Count == 0) return true;
            var found = false;
            foreach (var className in classNames)
            {
                if (go.GetType() == className || go.GetType().IsSubclassOf(className))
                {
                    found = true;
                    break;
                }
            }
            return found;
        }

        public static string getNetcodeModelString(NetcodeModel netcodeModel)
        {
            if (netcodeModel == NetcodeModel.FavorAttacker) return "Favor Attacker";
            if (netcodeModel == NetcodeModel.FavorDefender) return "Favor Defender";
            return "";
        }

        public static Color getPingColor(int? ping, int thresholdPing)
        {
            if (ping == null) return Color.White;
            if (ping >= thresholdPing) return Color.Red;
            else if (ping < thresholdPing && ping > thresholdPing * 0.5f) return Color.Yellow;
            return Color.Green;
        }

        public static byte boolToByte(bool boolean)
        {
            if (boolean) return 1;
            return 0;
        }

        public static bool byteToBool(byte value)
        {
            if (value == 1) return true;
            return false;
        }

        public static string getNthString(int place)
        {
            string placeStr = "";
            if (place == 1) placeStr = "1st";
            else if (place == 2) placeStr = "2nd";
            else if (place == 3) placeStr = "3rd";
            else placeStr = place.ToString() + "th";
            return placeStr;
        }

        public static SoundBufferWrapper getRandomMatchingVoice(Dictionary<string, SoundBufferWrapper> buffers, string soundKey, int charNum)
        {
            var voices = buffers.Values.ToList().FindAll(v => v.soundKey.Split('.')[0] == soundKey && (v.charNum == null || v.charNum.Value == charNum));
            return voices.GetRandomItem();
        }

        public static bool parseFileDotParam(string piece, char c, out int val)
        {
            val = 0;
            if (piece.Length == 0) return false;
            if (piece[0] != c) return false;
            if (piece == c.ToString())
            {
                val = 0;
                return true;
            }
            var rest = piece.Substring(1);
            if (int.TryParse(rest, out int result))
            {
                val = result;
                return true;
            }
            return false;
        }

        public static Point getTextureArraySize(Texture[,] textures)
        {
            uint w = 0;
            uint h = 0;
            for (int i = 0; i < textures.GetLength(0); i++)
            {
                if (textures[i, 0] == null) continue;
                h += textures[i, 0].Size.Y;
            }
            for (int i = 0; i < textures.GetLength(1); i++)
            {
                if (textures[0, i] == null) continue;
                w += textures[0, i].Size.X;
            }

            return new Point(w, h);
        }

        public static int convertDynamicToDir(dynamic dirDynamic)
        {
            string dirStr = (string)dirDynamic;
            if (dirStr == "left") return -1;
            if (dirStr == "right") return 1;
            if (dirStr == "up") return -1;
            if (dirStr == "down") return 1;
            return 0;
        }

        public static int invariantStringCompare(string a, string b)
        {
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }

        public static float progress(float done, float total)
        {
            return 1 - (done / total);
        }

        public static float twave(float time, float amplitude = 1, float period = 1)
        {
            return 4 * amplitude / period * MathF.Abs((((time - period / 4) % period) + period) % period - period / 2) - amplitude;
        }

        public static int SignOr1(float val)
        {
            int sign = MathF.Sign(val);
            if (sign == 0) sign = 1;
            return sign;
        }

        // -1 = less than, 0 = equal, 1 = greater than
        // Example order: 19.1, 19.2, ..., 19.9, 19.10, 19.11, ... 19.19, 19.20, 19.21 ... 
        public static int compareVersions(decimal versionA, decimal versionB)
        {
            string strA = versionA.ToString(CultureInfo.InvariantCulture);
            string strB = versionB.ToString(CultureInfo.InvariantCulture);

            int rightOfDotNumA = 0;
            var piecesA = strA.Split('.');
            if (piecesA.Length >= 2) int.TryParse(piecesA[1], out rightOfDotNumA);

            int rightOfDotNumB = 0;
            var piecesB = strB.Split('.');
            if (piecesB.Length >= 2) int.TryParse(piecesB[1], out rightOfDotNumB);

            if (rightOfDotNumA < rightOfDotNumB) return -1;
            else if (rightOfDotNumA > rightOfDotNumB) return 1;
            else return 0;
        }
    }
}
