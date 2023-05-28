using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildTools
{
    public class Sprite
    {
        public string name;
        public string spritesheetPath;
        public List<Frame> frames;
    }

    public class Frame
    {
        public Rect rect;
        public string spriteName;

        public Frame() { }

        public Frame(Frame cloneFrom)
        {
            rect = new Rect()
            {
                topLeftPoint = new Point(cloneFrom.rect.topLeftPoint.x, cloneFrom.rect.topLeftPoint.y),
                botRightPoint = new Point(cloneFrom.rect.botRightPoint.x, cloneFrom.rect.botRightPoint.y),
            };
            spriteName = cloneFrom.spriteName;
        }

        // Make sure this stays in sync with the same method in Sprite.cs
        public bool needsX3BusterCorrectionRight()
        {
            return spriteName.Contains("mmx_shoot") || spriteName.Contains("mmx_run_shoot") || spriteName.Contains("mmx_fall_shoot") || spriteName.Contains("mmx_jump_shoot") 
                || spriteName.Contains("mmx_dash_shoot") || spriteName.Contains("mmx_ladder_shoot") || spriteName.Contains("mmx_up_dash_shoot") || spriteName.Contains("mmx_wall_kick_shoot");
        }

        public bool needsX3BusterCorrectionLeft()
        {
            return spriteName.Contains("mmx_wall_slide_shoot");
        }


        [JsonIgnore]
        public Frame oldFrame;
    }

    public class Rect
    {
        public Point topLeftPoint;
        public Point botRightPoint;

        public int w { get { return botRightPoint.x - topLeftPoint.x; } }
        public int h { get { return botRightPoint.y - topLeftPoint.y; } }

        public int getSize()
        {
            return (2 * w) + (2 * h);
        }
    }

    public class Point
    {
        public int x;
        public int y;

        public Point(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public class SplitSpritesheetData
    {
        public int count;
        public int curNum;
        public SplitSpritesheetData(int count)
        {
            this.count = count;
        }
        public string getNewPathAndIncrement(string path)
        {
            curNum = (curNum + 1) % count;
            string numStr = (curNum + 1).ToString();
            if (numStr == "1") numStr = "";
            return Path.GetFileNameWithoutExtension(path) + numStr + ".png";
        }
    }

    // This class is responsible for optimizing spritesheets to <= 1024x1024 size in the build. Otherwise not all users can load them.
    // Might be slow on old machines. Might need to optimize the optimizer...
    public static class SpriteOptimizer
    {
        static string path = "../../../../LevelEditor/assets/";

        static string spriteFolderPath { get { return path + "sprites/"; } }
        static string spritesheetFolderPath { get { return path + "spritesheets/"; } }

        static string oSpriteFolderPath { get { return path + "sprites_optimized/"; } }
        static string oSpritesheetFolderPath { get { return path + "spritesheets_optimized/"; } }

        const int x3BusterPadding = 7;

        static readonly Dictionary<string, SplitSpritesheetData> spritesheetsToSplit = new Dictionary<string, SplitSpritesheetData>()
        {
            { "zero.png", new SplitSpritesheetData(3) },
            { "mavericks.png", new SplitSpritesheetData(3) },
            { "mavericksX2.png", new SplitSpritesheetData(3) },
            { "mavericksX3.png", new SplitSpritesheetData(4) },
            { "sigma_viral.png", new SplitSpritesheetData(2) },
        };

        public static void DoWork()
        {
            if (Directory.Exists(oSpriteFolderPath))
            {
                Directory.Delete(oSpriteFolderPath, true);
            }
            Directory.CreateDirectory(oSpriteFolderPath);

            if (Directory.Exists(oSpritesheetFolderPath))
            {
                Directory.Delete(oSpritesheetFolderPath, true);
            }
            Directory.CreateDirectory(oSpritesheetFolderPath);

            // Every time a non spritesheet file is added (i.e. palettes, etc), you must add it to this list 
            string[] copyOverOnly = { 
	            "hyperAxlPalette.png",
	            "hyperZeroPalette.png",
                "hyperBusterZeroPalette.png",
                "boomerkTrailPalette.png",
	            "paletteTexture.png",
	            "cStingPalette.png",
	            "FontBlue.png",
	            "FontGrayscale.png",
	            "FontMenu.png",
	            "FontMenuSelected.png",
	            "FontRed.png",
	            "FontTitle.png",
	            "paletteWheelGator.png",
	            "palettePossessed.png",
	            "paletteViralSigma.png",
	            "wspongeCharge.png",
	            "wspongeChargeRed.png",
	            "paletteChimera.png",
	            "paletteFrog.png",
	            "paletteHawk.png",
	            "paletteHighway2Background.png",
	            "paletteHighway2Parallax.png",
                "paletteKangaroo.png",
                "paletteNightmareZero.png",
                "paletteSigma3Shield.png",
                "paletteVoltCatfishCharge.png",
                "paletteSigma2Parallax.png",
            };

            var parentChild = new Dictionary<string, string[]>
            {
                { "XDefault.png", new string[] { "XBody.png", "XBody2.png", "XBody3.png", "XArm.png", "XArm2.png", "XArm3.png", "XBoots.png", "XBoots2.png", "XBoots3.png", "XHelmet.png", "XHelmet2.png", "XHelmet3.png", "XUltimate.png", "XUP.png", "XUPGlow.png" } },
                { "axl.png", new string[] { "axlArm.png" } },
            };
            string[] spriteExceptions = { "wsponge_angry_start" };

            var spriteList = new List<Sprite>();
            var spritesheetToFrames = new Dictionary<string, List<Frame>>();

            // Populate the spritesheet to frames mapping
            foreach (var spritePath in Directory.GetFiles(spriteFolderPath).ToList())
            {
                Sprite sprite = JsonConvert.DeserializeObject<Sprite>(File.ReadAllText(spritePath));
                sprite.name = Path.GetFileNameWithoutExtension(spritePath);

                if (spriteExceptions.Contains(sprite.name))
                {
                    CopyFile(spritePath, oSpritesheetFolderPath + "/" + sprite.name + ".json");
                    continue;
                }

                foreach (var frame in sprite.frames)
                {
                    frame.spriteName = sprite.name;
                }

                if (sprite.spritesheetPath.Contains("/") || sprite.spritesheetPath.Contains("\\"))
                {
                    throw new Exception("spritesheet path can't have slashes");
                }

                if (spritesheetsToSplit.ContainsKey(sprite.spritesheetPath))
                {
                    var ssData = spritesheetsToSplit[sprite.spritesheetPath];
                    sprite.spritesheetPath = ssData.getNewPathAndIncrement(sprite.spritesheetPath);
                }

                spriteList.Add(sprite);

                if (!spritesheetToFrames.ContainsKey(sprite.spritesheetPath))
                {
                    spritesheetToFrames[sprite.spritesheetPath] = new List<Frame>();
                }

                spritesheetToFrames[sprite.spritesheetPath].AddRange(sprite.frames);
            }

            var usedKeys = new List<string>();
            // For each spritesheet, take its frames and put it into a new compressed spritesheet
            foreach (var key in spritesheetToFrames.Keys.ToList())
            {
                string fileNameKey = Path.GetFileName(key);
                if (usedKeys.Contains(fileNameKey))
                {
                    throw new Exception("The key " + fileNameKey + " already exists!");
                }
                usedKeys.Add(fileNameKey);

                // Sort frames by area
                spritesheetToFrames[key] = spritesheetToFrames[key].OrderBy(f => f.rect.h).ToList();

                // Arrange in rows
                int maxRowHeight = 0;
                int maxWidth = 1024;

                int paddingW = 1;
                int paddingH = 4;
                int currentX = 0;
                int currentY = 0;

                var frames = spritesheetToFrames[key];
                foreach (var frame in frames)
                {
                    int oLeftPaddingW = paddingW;
                    if (frame.needsX3BusterCorrectionLeft())
                    {
                        oLeftPaddingW = x3BusterPadding;
                    }

                    int oRightPaddingW = paddingW;
                    if (frame.needsX3BusterCorrectionRight())
                    {
                        oRightPaddingW = x3BusterPadding;
                    }

                    frame.oldFrame = new Frame(frame);

                    int w = frame.rect.w;
                    int h = frame.rect.h;

                    if (currentX + oRightPaddingW + oLeftPaddingW + w > maxWidth)
                    {
                        currentX = 0;
                        currentY += maxRowHeight + paddingH + paddingH;
                        maxRowHeight = 0;
                    }

                    if (h > maxRowHeight)
                    {
                        maxRowHeight = h;
                    }

                    frame.rect.topLeftPoint = new Point(currentX + oLeftPaddingW, currentY + paddingH);
                    frame.rect.botRightPoint = new Point(currentX + oLeftPaddingW + w, currentY + paddingH + h);

                    currentX += oRightPaddingW + oLeftPaddingW + w;
                }
            }

            // Directly copy over the files not to be compressed
            foreach (var copyOverSprite in copyOverOnly)
            {
                string oldPath = spritesheetFolderPath + copyOverSprite;
                string newPath = oSpritesheetFolderPath + copyOverSprite;
                CopyFile(oldPath, newPath);
            }

            // Save all sprites with updated frame data
            foreach (var sprite in spriteList)
            {
                string savePath = oSpriteFolderPath + sprite.name + ".json";
                string oldSpritePath = spriteFolderPath + sprite.name + ".json";
                var oldJson = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(oldSpritePath));

                oldJson.spritesheetPath = sprite.spritesheetPath;
                for (int i = 0; i < sprite.frames.Count; i++)
                {
                    oldJson.frames[i].rect.topLeftPoint.x = sprite.frames[i].rect.topLeftPoint.x;
                    oldJson.frames[i].rect.topLeftPoint.y = sprite.frames[i].rect.topLeftPoint.y;
                    oldJson.frames[i].rect.botRightPoint.x = sprite.frames[i].rect.botRightPoint.x;
                    oldJson.frames[i].rect.botRightPoint.y = sprite.frames[i].rect.botRightPoint.y;
                }

                string json = JsonConvert.SerializeObject(oldJson);
                File.WriteAllText(savePath, json);
            }

            // Save the compressed bitmap images with the new frame data
            foreach (var kvp in spritesheetToFrames)
            {
                string spritesheet = Path.GetFileName(kvp.Key);
                List<Frame> frames = kvp.Value;
                processFrames(spritesheet, frames);
                string[] children = parentChild.ContainsKey(spritesheet) ? parentChild[spritesheet] : new string[] { };
                foreach (string child in children)
                {
                    processFrames(child, frames);
                }
            }
        }

        private static void CopyFile(string oldPath, string newPath)
        {
            if (File.Exists(newPath))
            {
                File.Delete(newPath);
            }
            File.Copy(oldPath, newPath);
        }

        static bool spritesheetsMatch(string s1, string s2)
        {
            string s1Base = s1.Replace(".png", "");
            string s2Base = s2.Replace(".png", "");
            return
                s1Base == s2Base + "1" ||
                s1Base == s2Base + "2" ||
                s1Base == s2Base + "3" ||
                s1Base == s2Base + "4" ||
                s1Base == s2Base + "5" ||
                s1Base == s2Base + "6" ||
                s1Base == s2Base + "7" ||
                s1Base == s2Base + "8" ||
                s1Base == s2Base + "9";
        }

        static void processFrames(string spritesheet, List<Frame> frames)
        {
            string matchingSpritesheet = spritesheetsToSplit.Keys.FirstOrDefault(k => spritesheetsMatch(spritesheet, k));
            if (string.IsNullOrEmpty(matchingSpritesheet))
            {
                matchingSpritesheet = spritesheet;
            }

            string oldSpritesheetPath = spritesheetFolderPath + Path.GetFileName(matchingSpritesheet);
            string savePath = oSpritesheetFolderPath + Path.GetFileName(spritesheet);
            var oldBitmap = new Bitmap(oldSpritesheetPath);

            int newWidth = frames.Max(f => f.rect.botRightPoint.x);
            int newHeight = frames.Max(f => f.rect.botRightPoint.y);
            var newBitmap = new Bitmap(newWidth, newHeight);

            if (newWidth > 1024 || newHeight > 1024)
            {
                throw new Exception("Width/height of spritesheet " + spritesheet + " too big!");
            }

            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                foreach (var frame in frames)
                {
                    int leftPadding = 0;
                    if (frame.needsX3BusterCorrectionLeft())
                    {
                        leftPadding = x3BusterPadding;
                    }

                    int rightPadding = 0;
                    if (frame.needsX3BusterCorrectionRight())
                    {
                        rightPadding = x3BusterPadding;
                    }
                    int topPadding = 0;
                    if (spritesheet.Contains("XUltimate"))
                    {
                        topPadding = 3;
                    }

                    g.DrawImage(oldBitmap,
                        new Rectangle(frame.rect.topLeftPoint.x - leftPadding, frame.rect.topLeftPoint.y - topPadding, frame.rect.w + rightPadding + leftPadding, frame.rect.h + topPadding),
                        frame.oldFrame.rect.topLeftPoint.x - leftPadding,
                        frame.oldFrame.rect.topLeftPoint.y - topPadding,
                        frame.oldFrame.rect.w + rightPadding + leftPadding,
                        frame.oldFrame.rect.h + topPadding,
                        GraphicsUnit.Pixel);
                }
            }

            newBitmap.Save(savePath);
            newBitmap.Dispose();
            oldBitmap.Dispose();
        }
    }
}
