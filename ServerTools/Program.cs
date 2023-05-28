using MMXOnline;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ServerTools
{
    class ReportJson
    {
        public string name;
        public string dataBlob;
    }

    class DecryptedJson
    {
        public string name;
        public string ipAddress;
        public string deviceId;
    }

    class Program
    {
        static string baseFolderPath = @"C:\users\username\desktop\ServerTools\";
        static string secretPrefix;

        // Replace with real ips to be able to use this tool
        static string[] ips = { "127.0.0.1" };

        static MatchmakingQuerier matchmakingQuerier;
        
        static string encryptionKeyPath = baseFolderPath + "encryptionKey.txt";
        static string reportFolderPath = baseFolderPath + "reports";
        static string banListPath = baseFolderPath + "banlist.json";
        static string reportFilePath = baseFolderPath + "report.html";

        static int[] validCommands = { 1, 2, 3 };

        static void Main(string[] args)
        {
            secretPrefix = File.ReadAllText("secretPrefix.txt");
            matchmakingQuerier = new MatchmakingQuerier();
            Console.WriteLine("Commands:\n1: Update version\n2: Get ban list\n3: Update ban list\n");
            while (true)
            {
                Console.Write("Enter in command number: ");
                string commandStr = Console.ReadLine();

                if (!int.TryParse(commandStr, out int commandNum) || !validCommands.Contains(commandNum))
                {
                    Console.WriteLine("Invalid Command Number");
                    continue;
                }

                bool succeeded = ProcessCommand(commandNum, out string message);
                if (!succeeded)
                {
                    Console.WriteLine("Command failed: " + message);
                }
                else
                {
                    Console.WriteLine("Command succeeded");
                }
            }
        }

        static bool ProcessCommand(int command, out string message)
        {
            try
            {
                if (command == 1)
                {
                    Console.Write("Enter in a version number: ");
                    string versionStr = Console.ReadLine();

                    if (!decimal.TryParse(versionStr, out decimal version))
                    {
                        message = "Invalid version number";
                        return false;
                    }

                    message = UpdateVersion(version);
                    return true;
                }
                else if (command == 2)
                {
                    GenerateBanReport();
                    message = "Success";
                    return true;
                }
                else if (command == 3)
                {
                    string newBanList = File.ReadAllText(banListPath);
                    message = UpdateBanList(newBanList);
                    return true;
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }

            message = "Command not found";
            return false;
        }

        public static string UpdateVersion(decimal version)
        {
            foreach (var ip in ips)
            {
                string result = matchmakingQuerier.send(ip, secretPrefix + "updateversion:" + version.ToString(), "updateversion");
                if (result == null || result.StartsWith("fail:"))
                {
                    throw new Exception("ip " + ip + ": " + result.RemovePrefix("fail:"));
                }
            }
            return "Success";
        }

        public static string UpdateBanList(string newBanList)
        {
            foreach (var ip in ips)
            {
                string result = matchmakingQuerier.send(ip, secretPrefix + "updatebanlist:" + newBanList, "updatebanlist");
                if (result == null || result.StartsWith("fail:"))
                {
                    throw new Exception("ip " + ip + ": " + result.RemovePrefix("fail:"));
                }
            }
            return "Success";
        }

        public static void GenerateBanReport()
        {
            string result = matchmakingQuerier.send(ips[0], secretPrefix + "getbanlist", "getbanlist");
            if (result == null || string.IsNullOrEmpty(result))
            {
                throw new Exception(result.RemovePrefix("fail:"));
            }

            string key = File.ReadAllText(encryptionKeyPath);
            string currentBanList = result;
            var files = Directory.GetFiles(reportFolderPath, "*", SearchOption.AllDirectories).ToList();

            files = files.OrderBy((f) =>
            {
                return File.GetLastWriteTime(f);
            }).ToList();

            string futureDateStr = DateTime.UtcNow.AddDays(7).ToString("u", CultureInfo.InvariantCulture);

            string html = "";
            html += "<table style='display:inline-block' border='1px solid black'>";
            html += "<tr><td><b>Name</b></td><td><b>Name (decrypted)</b></td><td><b>IP</b></td><td><b>Device Id</b></td></tr>";
            foreach (var file in files)
            {
                html += "<tr>";
                var contents = File.ReadAllText(file);
                var reportJson = JsonConvert.DeserializeObject<ReportJson>(contents);
                string decrypted = AesOperation.DecryptString(key, reportJson.dataBlob);
                var decryptedJson = JsonConvert.DeserializeObject<DecryptedJson>(decrypted);
                html += "<td>" + reportJson.name + "</td>";
                html += "<td>" + decryptedJson.name + "</td>";
                html += "<td>" + decryptedJson.ipAddress + "</td>";
                html += "<td>" + decryptedJson.deviceId + "</td>";
                html += "</tr>";
            }
            html += "</table>";

            html += "<pre style='display:inline-block;vertical-align:top;margin:10px;'>";
            html += currentBanList;
            html += "</pre>";

            html += "<div><b>7 days from now:</b> " + futureDateStr + "</div>";

            html += "<textarea rows='20' cols='75'></textarea>";

            //html = System.Xml.Linq.XElement.Parse(html).ToString();
            File.WriteAllText(reportFilePath, html);
            File.WriteAllText(banListPath, currentBanList);
        }
    }
}
