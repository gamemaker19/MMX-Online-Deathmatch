using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BuildTools
{
    public class PortCustomMap
    {
        // Takes a custom map and ports it to an official map.
        public static void Port(string mapName, string renamedMap)
        {
            // Paths
            string assetPath = "../../../../LevelEditor/assets";
            string officialMapPath = assetPath + "/maps/" + renamedMap;
            string customMapPath = assetPath + "/maps_custom/" + mapName;

            // Create the map folder and copy files to it (except music)
            if (Directory.Exists(officialMapPath))
            {
                Directory.Delete(officialMapPath, true);
            }
            Directory.CreateDirectory(officialMapPath);
            var files = Directory.GetFiles(customMapPath);
            foreach (var file in files)
            {
                if (file.EndsWith(".ogg")) continue;
                string fileName = Path.GetFileName(file);
                CopyFile(file, officialMapPath + "/" + fileName);
            }

            // Handle sprites folder
            var spriteFiles = Directory.GetFiles(customMapPath + "/sprites");
            bool pngOnce = false;
            foreach (var file in spriteFiles)
            {
                string destFileName = Path.GetFileName(file);
                if (file.EndsWith(".json"))
                {
                    var spriteJson = File.ReadAllText(file);

                    var regex = new Regex(@"""customMapName"":( *?)""(.*?)"",");
                    spriteJson = regex.Replace(spriteJson, (match) =>
                    {
                        return "";
                    });

                    // Modify sprite json
                    dynamic spriteJsonObj = JsonConvert.DeserializeObject(spriteJson);
                    spriteJsonObj.spritesheetPath = "ms_" + renamedMap + ".png";
                    spriteJson = JsonConvert.SerializeObject(spriteJsonObj);

                    destFileName = "ms_" + renamedMap + "_" + destFileName;
                    File.WriteAllText(assetPath + "/sprites/" + destFileName, spriteJson);
                }
                else if (file.EndsWith(".png"))
                {
                    // Copy spritesheets after renaming
                    destFileName = "ms_" + renamedMap + ".png";
                    CopyFile(file, assetPath + "/spritesheets/" + destFileName);
                    if (pngOnce)
                    {
                        throw new Exception("Two spritesheets, not supported");
                    }
                    pngOnce = true;
                }
            }

            // Modify the map json
            string json = File.ReadAllText(assetPath + "/maps_custom/" + mapName + "/map.json");
            json = ModifyMap(json, mapName, renamedMap);
            File.WriteAllText(officialMapPath + "/map.json", json);

            // Modify the mirror map json
            string mirrorPath = assetPath + "/maps_custom/" + mapName + "/mirrored.json";
            if (File.Exists(mirrorPath))
            {
                json = File.ReadAllText(mirrorPath);
                json = ModifyMap(json, mapName, renamedMap);
                File.WriteAllText(officialMapPath + "/mirrored.json", json);
            }
        }

        public static string ModifyMap(string json, string mapName, string renamedMap)
        {
            json = json.Replace("\"" + mapName + "\"", "\"" + renamedMap + "\"");
            json = json.Replace("/" + mapName + "/", "/" + renamedMap + "/");
            json = json.Replace("maps_custom/", "maps/");
            json = json.Replace(mapName + ":", "ms_" + renamedMap + "_");
            json = json.Replace(mapName + "_mirrored", renamedMap + "_mirrored");
            return json;
        }

        private static void CopyFile(string oldPath, string newPath)
        {
            if (File.Exists(newPath))
            {
                File.Delete(newPath);
            }
            File.Copy(oldPath, newPath);
        }
    }
}
