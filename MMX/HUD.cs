using SFML.Audio;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static SFML.Graphics.Text;
using static SFML.Window.Sensor;

namespace MMXOnline
{
    public enum MMXFont
    {
        Menu,
        Select,
        Blue,
        Red,
        Title,
        Gray,
        White,
        Outline
    }

    public class BatchDrawable : Transformable, Drawable
    {
        public VertexArray vertices;
        public Texture texture;

        public BatchDrawable(Texture texture)
        {
            vertices = new VertexArray();
            vertices.PrimitiveType = PrimitiveType.Quads;
            this.texture = texture;
        }

        public void Draw(RenderTarget target, RenderStates states)
        {
            states.Transform *= Transform;
            states.Texture = texture;
            target.Draw(vertices, states);
        }
    }

    // A lot of the HUD drawing code is very messy and duct-taped and could be cleaned up but this would take a lot of effort due to 100's or even thousands of places calling the functions so it may be best to just leave it as is.
    // For example the standards of font sizes and scaling in parameters are inconsistent, there are hacks to multiply it in one function call and divide it back to the original value in a later one, etc.
    // For example, text drawing is split between Helpers.cs and DrawWrappers.cs, would be better if it was one consistent place
    public partial class DrawWrappers
    {
        public static View hudView;
        public static Font font;
        public static List<Action> deferredTextDraws = new List<Action>();
        public static void initHUD()
        {
            hudView = new View(new Vector2f(Global.halfScreenW, Global.halfScreenH), new Vector2f(Global.screenW, Global.screenH));
            string fontPath = Global.assetPath + "assets/fonts/Mega Man X.ttf";
            font = new Font(fontPath);
        }
        private static void drawToHUD(Drawable drawable)
        {
            Global.window.SetView(hudView);
            Global.window.Draw(drawable);
            Global.window.SetView(Global.view);
        }

        public static void addToVertexArray(BatchDrawable bd, SFML.Graphics.Sprite sprite)
        {
            float sx = sprite.TextureRect.Left;
            float sy = sprite.TextureRect.Top;
            float sw = sprite.TextureRect.Width;
            float sh = sprite.TextureRect.Height;
            float dx = sprite.Position.X;
            float dy = sprite.Position.Y;
            float scale = sprite.Scale.X;
            Color color = sprite.Color;

            float width = sw * scale;
            float height = sh * scale;

            Vertex vertex1 = new Vertex(new Vector2f(dx, dy), color);
            Vertex vertex2 = new Vertex(new Vector2f(dx, dy + height), color);
            Vertex vertex3 = new Vertex(new Vector2f(dx + width, dy + height), color);
            Vertex vertex4 = new Vertex(new Vector2f(dx + width, dy), color);

            vertex1.TexCoords = new Vector2f(sx, sy);
            vertex2.TexCoords = new Vector2f(sx, sy + sh);
            vertex3.TexCoords = new Vector2f(sx + sw, sy + sh);
            vertex4.TexCoords = new Vector2f(sx + sw, sy);

            bd.vertices.Append(vertex1);
            bd.vertices.Append(vertex2);
            bd.vertices.Append(vertex3);
            bd.vertices.Append(vertex4);
        }

        public const float defaultLineMargin = 2;

        public static void DrawText(string textStr, float x, float y, Alignment alignment, bool outline, float fontSize,
            Color? color, Color? outlineColor, Text.Styles style, float outlineThickness, bool isWorldPos, long depth,
            MMXFont font = MMXFont.Menu, VAlignment vAlignment = VAlignment.Top, float lineMargin = defaultLineMargin, float alpha = 1)
        {
            if (string.IsNullOrEmpty(textStr)) return;
            bool deferred = false;

            if (isWorldPos)
            {
                if (Options.main.fontType != 0)
                {
                    DrawTextFont(textStr, x, y, alignment, outline, (uint)(fontSize * 8), color, outlineColor, style, outlineThickness, isWorldPos, depth);
                    return;
                }
                else
                {
                    if (Global.level?.server?.fixedCamera == true)
                    {
                        fontSize *= 0.5f;
                    }
                    if (color == Color.White && outlineColor == Helpers.DarkBlue)
                    {
                        font = MMXFont.Blue;
                    }
                    if (color == Color.White && outlineColor == Helpers.DarkRed)
                    {
                        font = MMXFont.Red;
                    }
                }
            }

            /*
            if (font == MMXFont.Blue || font == MMXFont.Red)
            {
                outlineColor = (font == MMXFont.Blue ? Helpers.DarkBlue : Helpers.DarkRed);
                font = MMXFont.White;
            }
            */

            if (font == MMXFont.White)
            {
                DrawText(textStr, x, y, alignment, false, fontSize, outlineColor, null, style, 1, isWorldPos, depth, MMXFont.Outline, vAlignment, lineMargin, alpha);
                return;
            }

            if (isWorldPos)
            {
                x = (x - Global.level.camX) / Global.viewSize;
                y = (y - Global.level.camY) / Global.viewSize;
                isWorldPos = false;
                deferred = true;
            }

            var twoMarks = new List<float>() { 0.33f, 0.66f };
            var threeMarks = new List<float>() { 0.25f, 0.5f, 0.75f };
            var textLines = textStr.Split('\n');

            string fontStr = "FontMenu";
            if (font == MMXFont.Select) fontStr = "FontMenuSelected";
            if (font == MMXFont.Blue) fontStr = "FontBlue";
            if (font == MMXFont.Red) fontStr = "FontRed";
            if (font == MMXFont.Title) fontStr = "FontTitle";
            if (font == MMXFont.Gray) fontStr = "FontGrayscale";
            if (color != null && color != Color.White && fontStr != "FontMenuSelected")
            {
                fontStr = "FontGrayscale";
            }
            if (font == MMXFont.White) fontStr = "FontWhite";
            if (font == MMXFont.Outline) fontStr = "FontOutline";
            var bitmapFontTexture = Global.textures[fontStr];
            var batchDrawable = new BatchDrawable(bitmapFontTexture);
            
            for (int j = 0; j < textLines.Length; j++)
            {
                string textLine = textLines[j];
                for (int i = 0; i < textLine.Length; i++)
                {
                    char c = textLine[i];
                    int charInt = c;
                    int wh = 11;
                    int padding = 2;
                    int whPadding = wh + padding;
                    int rx = charInt % 16;
                    int ry = charInt / 16;

                    var textSprite = new SFML.Graphics.Sprite(bitmapFontTexture, new IntRect(rx * whPadding, ry * whPadding, 11, 11));

                    float xPos = MathF.Round(x) + (i * fontSize * 8f) - 1;
                    float yPos = MathF.Round(y) + (j * fontSize * (8f + lineMargin)) - 1;
                    textSprite.Position = new Vector2f(xPos, yPos);
                    textSprite.Color = color ?? Color.White;

                    if (alpha != 1)
                    {
                        textSprite.Color = new Color(textSprite.Color.R, textSprite.Color.G, textSprite.Color.B, Helpers.toColorByte(alpha));
                    }

                    textSprite.Scale = new Vector2f(fontSize, fontSize);

                    Point textSize = measureTextStd(textStr, false, fontSize * 8, index: j, lineMargin: lineMargin);

                    if (alignment == Alignment.Center)
                    {
                        textSprite.Position = new Vector2f(textSprite.Position.X - textSize.x * 0.5f, textSprite.Position.Y);
                    }
                    else if (alignment == Alignment.Right)
                    {
                        textSprite.Position = new Vector2f(textSprite.Position.X - textSize.x, textSprite.Position.Y);
                    }

                    float yOff = -3;
                    if (vAlignment == VAlignment.Center)
                    {
                        float percent = 0f;
                        if (textLines.Length == 2) percent = twoMarks[j];
                        if (textLines.Length == 3) percent = threeMarks[j];
                        textSprite.Position = new Vector2f(textSprite.Position.X, yOff + y - (textSize.y * 0.5f) + (textSize.y * percent));
                    }

                    addToVertexArray(batchDrawable, textSprite);
                }
                // End text line characters loop
            }
            // End text lines loop

            if (isWorldPos)
            {
                DrawLayer drawLayer;
                if (!walDrawObjects.ContainsKey(depth))
                {
                    walDrawObjects[depth] = new DrawLayer();
                }
                drawLayer = walDrawObjects[depth];
                drawLayer.oneOffs.Add(new DrawableWrapper(null, batchDrawable));
            }
            else
            {
                if (!deferred)
                {
                    drawToHUD(batchDrawable);
                }
                else
                {
                    deferredTextDraws.Add(new Action(() => { drawToHUD(batchDrawable); }));
                }
            }
        }

        // Fontsize scale is 8 for standard font size
        public static Point measureTextStd(string textStr, bool outline, float fontSize, float lineMargin = defaultLineMargin, int index = 0)
        {
            var lines = textStr.Split('\n');
            return new Point(lines[index].Length * fontSize, (lines.Length * fontSize) + ((lines.Length - 1) * lineMargin));
        }

        public static void DrawTextFont(string textStr, float x, float y, Alignment alignment, bool outline, uint fontSize,
            Color? color, Color? outlineColor, Text.Styles style, float outlineThickness, bool isWorldPos, long depth)
        {
            if (!string.IsNullOrEmpty(textStr) && textStr.Contains("\n"))
            {
                textStr = textStr.Replace("\n", "\n\n");
            }

            bool deferred = false;
            if (isWorldPos)
            {
                fontSize = (uint)(fontSize / (float)Global.viewSize);
                outlineThickness = outlineThickness / Global.viewSize;
                x = (x - Global.level.camX) / Global.viewSize;
                y = (y - Global.level.camY) / Global.viewSize;
                isWorldPos = false;
                deferred = true;
            }

            Vector2f scale = new Vector2f(0.25f, 0.25f);
            float boundsModifier = 1;
            if (!isWorldPos)
            {
                fontSize *= 4;
                outlineThickness *= 4;
                scale = new Vector2f(1f / 4, 1f / 4);
                boundsModifier = 1f / 4;
            }

            Text text = new Text(textStr, font, fontSize);
            text.Position = new Vector2f(MathF.Round(x), MathF.Round(y));
            text.FillColor = color ?? Color.White;
            text.Style = style;
            text.Scale = scale;

            if (outline)
            {
                text.OutlineColor = outlineColor ?? Helpers.DarkBlue;
                text.OutlineThickness = outlineThickness;
            }

            FloatRect bounds = text.GetLocalBounds();
            if (alignment == Alignment.Center)
            {
                text.Position = new Vector2f(MathF.Round(x - (bounds.Width * 0.5f * boundsModifier)), MathF.Round(y));
            }
            else if (alignment == Alignment.Right)
            {
                text.Position = new Vector2f(MathF.Round(x - (bounds.Width * boundsModifier)), MathF.Round(y));
            }

            if (!deferred)
            {
                drawToHUD(text);
            }
            else
            {
                deferredTextDraws.Add(new Action(() => { drawToHUD(text); }));
            }
        }

        public static Point measureTextStdFont(string textStr, float outlineThickness, uint fontSize)
        {
            Text text = new Text(textStr, font, fontSize);

            if (outlineThickness > 0)
            {
                text.OutlineColor = Helpers.DarkBlue;
                text.OutlineThickness = outlineThickness;
            }

            FloatRect bounds = text.GetLocalBounds();
            return new Point(bounds.Width, bounds.Height);
        }

        public static void DrawTextureHUD(Texture texture, float sx, float sy, float sw, float sh, float dx, float dy, float alpha = 1)
        {
            if (texture == null) return;
            var sprite = new SFML.Graphics.Sprite(texture, new IntRect((int)sx, (int)sy, (int)sw, (int)sh));
            sprite.Position = new Vector2f(dx, dy);
            sprite.Color = new Color(255, 255, 255, (byte)(int)(alpha * 255));
            drawToHUD(sprite);
        }

        public static void DrawTextureHUD(Texture texture, float x, float y)
        {
            if (texture == null) return;
            var sprite = new SFML.Graphics.Sprite(texture);
            sprite.Position = new Vector2f(x, y);
            drawToHUD(sprite);
        }

        public static void DrawTitleTexture(Texture texture)
        {
            if (texture == null) return;
            var sprite = new SFML.Graphics.Sprite(texture);
            sprite.Position = new Vector2f(-20, 0);
            sprite.Scale = new Vector2f(0.75f, 0.75f);
            drawToHUD(sprite);
        }
    }
}
