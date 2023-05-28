using Newtonsoft.Json;
using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SFML.Window.Keyboard;

namespace MMXOnline
{
    public class GetRegionResponse
    {
        public Region region;
        public List<Server> servers;
        public GetRegionResponse(Region region, List<Server> servers)
        {
            this.region = region;
            this.servers = servers;
        }
    }

    public class JoinMenu : IMainMenu
    {
        public bool refreshing = false;
        public bool networkError = false;
        public List<Server> allServers = new List<Server>();
        public int refreshFrames;
        public bool joinedLateDone;
        public LANIPHelper lanIPHelper;
        public bool isLAN;

        public HashSet<Region> failedRegions = new HashSet<Region>();

        public List<Server> publicServers
        {
            get
            {
                return allServers.Where(s => !s.hidden).ToList();
            }
        }

        public JoinMenu(bool isLAN)
        {
            this.isLAN = isLAN;
            if (!isLAN && !Global.regionPingTask.IsCompleted)
            {
                Global.regionPingTask.Wait();
            }
            queueRefresh();
        }

        public void queueRefresh()
        {
            refreshing = true;
            refreshFrames = 2;
        }

        public List<Region> getRegions()
        {
            if (!isLAN)
            {
                return Global.regions;
            }
            else
            {
                lock (Global.lanRegions)
                {
                    var tasks = new List<Task<Region>>();
                    foreach (var lanIP in getLANIPs())
                    {
                        if (!Global.lanRegions.Any(r => r.ip == lanIP))
                        {
                            tasks.Add(Region.tryCreateWithPingClient("LAN", lanIP));
                        }
                    }

                    Task.WaitAll(tasks.ToArray());

                    foreach (var task in tasks)
                    {
                        if (task.Result != null)
                        {
                            Global.lanRegions.Add(task.Result);
                        }
                    }
                }
                Global.updateLANRegionPings();
                return Global.lanRegions;
            }
        }

        public List<string> getLANIPs()
        {
            if (lanIPHelper == null) lanIPHelper = new LANIPHelper();
            return lanIPHelper.getIps();
        }

        // null = fail.
        public GetRegionResponse getRegionServers(Region region)
        {
            byte[] response = Global.matchmakingQuerier.send(region.ip, "GetServers");
            if (response.IsNullOrEmpty())
            {
                if (response == null && region.name != "LAN")
                {
                    return new GetRegionResponse(region, null);
                }
                else
                {
                    return new GetRegionResponse(region, new List<Server>());
                }
            }
            else
            {
                var serverList = Helpers.deserialize<List<Server>>(response);
                return new GetRegionResponse(region, serverList);
            }
        }

        public void refreshTaskMethod()
        {
            var tasks = new List<Task<GetRegionResponse>>();
            foreach (var region in getRegions())
            {
                tasks.Add(Task.Run(() => getRegionServers(region)));
            }

            Task.WaitAll(tasks.ToArray());
            foreach (var task in tasks)
            {
                if (task.Result.servers == null)
                {
                    failedRegions.Add(task.Result.region);
                }
                else
                {
                    allServers.AddRange(task.Result.servers);
                }
            }

            refreshing = false;
        }

        Task refreshTask;
        public void refresh()
        {
            allServers.Clear();
            failedRegions.Clear();

            refreshing = true;
            if (refreshTask == null || refreshTask.IsCompleted)
            {
                refreshTask = Task.Run(refreshTaskMethod);
            }

            if (selServerIndex >= allServers.Count)
            {
                selServerIndex = MathF.Clamp(allServers.Count - 1, 0, int.MaxValue);
            }
        }

        int frameCount;
        public void update()
        {
            if (!refreshing)
            {
                frameCount++;
                if (frameCount > 240)
                {
                    frameCount = 0;
                    if (isLAN)
                    {
                        Global.updateLANRegionPings();
                    }
                    else
                    {
                        Global.updateRegionPings();
                    }
                }
            }

            if (refreshFrames > 0)
            {
                refreshFrames--;
                if (refreshFrames <= 0)
                {
                    refreshFrames = 0;
                    refresh();
                }
            }

            if (publicServers.Count > 0)
            {
                Helpers.menuUpDown(ref selServerIndex, 0, publicServers.Count - 1);
                if (Global.input.isPressedMenu(Control.MenuSelectPrimary) || Global.quickStartOnline)
                {
                    var server = publicServers[selServerIndex];
                    Menu.change(new SelectCharacterMenu(this, server.level.EndsWith("1v1"), false, false, false, GameMode.isStringTeamMode(server.gameMode), false, () => joinServer(server)));
                }
            }

            if (publicServers.Count > 10)
            {
                rowHeight2 = 9;
            }
            else
            {
                rowHeight2 = 14;
            }

            if (Global.input.isPressedMenu(Control.MenuSelectSecondary))
            {
                queueRefresh();
            }
            else if (Global.input.isPressedMenu(Control.MenuBack))
            {
                Menu.change(new MainMenu());
            }
            else if (Global.input.isPressedMenu(Control.MenuEnter))
            {
                if (!isLAN)
                {
                    Menu.change(new EnterTextMenu("Enter private server name", 10, (string text) =>
                    {
                        Server server = allServers.Where(s => s.hidden && s.name == text).FirstOrDefault();
                        if (server != null)
                        {
                            Menu.change(new SelectCharacterMenu(this, server.level.EndsWith("1v1"), false, false, false, GameMode.isStringTeamMode(server.gameMode), false, () => joinServer(server)));
                            return;
                        }

                        // Fallback: try requesting the server name directly
                        foreach (var region in getRegions())
                        {
                            byte[] serverBytes = Global.matchmakingQuerier.send(region.ip, "GetServer:" + text);
                            if (!serverBytes.IsNullOrEmpty())
                            {
                                server = Helpers.deserialize<Server>(serverBytes);
                            }
                            else if (serverBytes == null)
                            {
                                Menu.change(new ErrorMenu(new string[] { "Error when looking up private match." }, this));
                                return;
                            }
                            if (server != null)
                            {
                                Menu.change(new SelectCharacterMenu(this, server.level.EndsWith("1v1"), false, false, false, GameMode.isStringTeamMode(server.gameMode), false, () => joinServer(server)));
                                return;
                            }
                        }

                        Menu.change(new ErrorMenu(new string[] { "Private server not found.", "Note: match names are case sensitive." }, this));
                    }));
                }
                else
                {
                    Menu.change(new EnterTextMenu("Enter IP Address", 15, (ipAddressStr) =>
                    {
                        if (ipAddressStr.IsValidIpAddress())
                        {
                            lock (Global.lanRegions)
                            {
                                if (!Global.lanRegions.Any(r => r.ip == ipAddressStr))
                                {
                                    Global.lanRegions.Add(new Region("LAN", ipAddressStr));
                                }
                            }
                            queueRefresh();
                            Menu.change(this);
                        }
                        else
                        {
                            Menu.change(new ErrorMenu(new string[] { "Invalid IP address." }, this));
                        }
                    }));
                }
            }
        }

        public static void joinServer(Server serverToJoin)
        {
            if (Helpers.compareVersions(Global.version, serverToJoin.gameVersion) == -1)
            {
                Menu.change(new ErrorMenu(new string[] { "Your game version is too old. Update to v" + serverToJoin.gameVersion.ToString() }, new MainMenu()));
                return;
            }
            else if (Helpers.compareVersions(Global.version, serverToJoin.gameVersion) == 1)
            {
                Menu.change(new ErrorMenu(new string[] { "The match game version (v" + serverToJoin.gameVersion.ToString() + ") is too old." }, new MainMenu()));
                return;
            }
            else if (Global.checksum != serverToJoin.gameChecksum)
            {
                Menu.change(new ErrorMenu(new string[] { "Client and server have different", "checksum version numbers.", "Yours: " + Global.checksum, "Theirs: " + serverToJoin.gameChecksum }, new MainMenu()));
                return;
            }
            else if (!string.IsNullOrEmpty(serverToJoin.customMapChecksum))
            {
                var myLevelChecksum = LevelData.getChecksumFromName(serverToJoin.level);
                if (string.IsNullOrEmpty(myLevelChecksum))
                {
                    string customMapUrl = serverToJoin.customMapUrl;
                    var errorLines = new List<string>()
                    {
                        "Custom map \"" + serverToJoin.level + "\"",
                        "not found in maps_custom folder."
                    };
                    if (!string.IsNullOrEmpty(customMapUrl))
                    {
                        errorLines.Add("Download the map below:");
                        Menu.change(new TextExportMenu(errorLines.ToArray(), "customMapUrl", customMapUrl, new MainMenu(), textSize: 18));
                    }
                    else
                    {
                        Menu.change(new ErrorMenu(errorLines.ToArray(), new MainMenu()));
                    }
                    
                    return;
                }
                else if (myLevelChecksum != serverToJoin.customMapChecksum)
                {
                    string customMapUrl = serverToJoin.customMapUrl;
                    var errorLines = new List<string>()
                    {
                        "Client and server custom map",
                        "checksums do not match.",
                    };
                    if (!string.IsNullOrEmpty(customMapUrl))
                    {
                        errorLines.Add("Re-download the map below:");
                        Menu.change(new TextExportMenu(errorLines.ToArray(), "customMapUrl", customMapUrl, new MainMenu(), textSize: 18));
                    }
                    else
                    {
                        Menu.change(new ErrorMenu(errorLines.ToArray(), new MainMenu()));
                    }
                    return;
                }
            }

            if (!Global.debug && !serverToJoin.hidden && serverToJoin.playerJoinCount.ContainsKey(Global.deviceId) && serverToJoin.playerJoinCount[Global.deviceId] > Server.maxRejoinCount + 1)
            {
                DateTime nextJoinTime = serverToJoin.playerLastJoinTime[Global.deviceId].AddMinutes(5);
                double minutes = (nextJoinTime - DateTime.UtcNow).TotalMinutes;
                if (minutes > 0)
                {
                    int min = (int)Math.Ceiling(minutes);
                    Menu.change(new ErrorMenu(new string[] { "You have joined/disconnected too many times.", "Can rejoin in " + min.ToString() + " minutes." }, new MainMenu()));
                    return;
                }
            }

            foreach (var banEntry in serverToJoin.kickedPlayers)
            {
                if (!banEntry.isBanned(null, Global.deviceId, 0)) continue;

                string nextJoinTime = "You cannot rejoin.";
                if (banEntry.bannedUntil != null)
                {
                    double minutes = (banEntry.bannedUntil.Value - DateTime.UtcNow).TotalMinutes;
                    nextJoinTime = string.Format("Can rejoin in {0} minutes.", (int)Math.Ceiling(minutes));
                }
                Menu.change(new ErrorMenu(new string[] { "You were kicked from this server!", nextJoinTime }, new MainMenu()));
                return;
            }

            string playerName = Options.main.playerName;
            
            var inputServerPlayer = new ServerPlayer(playerName, -1, false, SelectCharacterMenu.playerData.charNum, null, Global.deviceId, null, serverToJoin.region.getPing());
            Global.serverClient = ServerClient.Create(serverToJoin.region.ip, serverToJoin.name, serverToJoin.port, inputServerPlayer, out JoinServerResponse joinServerResponse, out string error);
            if (Global.serverClient == null)
            {
                Menu.change(new ErrorMenu(new string[] { error, "Please try rejoining." }, new JoinMenu(serverToJoin.isLAN)));
                return;
            }

            var players = joinServerResponse.server.players;
            var server = joinServerResponse.server;

            if (Global.serverClient.serverPlayer.joinedLate)
            {
                Global.level = new Level(server.getLevelData(), SelectCharacterMenu.playerData, server.extraCpuCharData, true);

                Global.level.startLevel(joinServerResponse.server, true);
                /*
                while (!Global.level.started)
                {
                    Global.serverClient.getMessages(out var messages, true);
                    Thread.Sleep(100);
                }
                */
            }
            else
            {
                Menu.change(new WaitMenu(new MainMenu(), server, false));
            }
        }

        public string topMsg = "";

        public float col1Pos = 20;
        public float col2Pos = 75;
        public float col3Pos = 145;
        public float col4Pos = 175;
        public float col5Pos = 225;
        public float col6Pos = 265;

        public float headerPos = 40;
        public float rowHeight = 20;
        public float rowHeight2 = 14;

        public int selServerIndex = 0;

        public void render()
        {
            string joinMenuImage = isLAN ? "joinlanmenutitle" : "joinmenutitle";

            DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
            // DrawWrappers.DrawTextureHUD(Global.textures[joinMenuImage], 0, 0);
            DrawWrappers.DrawTextureHUD(Global.textures["joinborder"], 0, 30);

            Helpers.drawTextStd(TCat.Title, "Join Match", Global.halfScreenW, 10, alignment: Alignment.Center, vAlignment: VAlignment.Center, fontSize: 48);

            Helpers.drawTextStd("Name", col1Pos, headerPos, outline: false, fontSize: 24);
            Helpers.drawTextStd("Map", col2Pos, headerPos, outline: false, fontSize: 24);
            Helpers.drawTextStd("Mode", col3Pos, headerPos, outline: false, fontSize: 24);
            Helpers.drawTextStd("Players", col4Pos, headerPos, outline: false, fontSize: 24);
            Helpers.drawTextStd("Region", col5Pos, headerPos, outline: false, fontSize: 24);
            Helpers.drawTextStd("Ping", col6Pos, headerPos, outline: false, fontSize: 24);

            if (refreshing)
            {
                Helpers.drawTextStd("Searching...", col1Pos, headerPos + rowHeight, outline: false);
            }
            else if (networkError)
            {
                Helpers.drawTextStd("Could not contact server.", col1Pos, headerPos + rowHeight, outline: false);
            }
            else if (publicServers.Count == 0)
            {
                Helpers.drawTextStd(isLAN ? "(No LAN matches found)" : "(No matches found)", col1Pos, headerPos + rowHeight, outline: false);
                if (isLAN)
                {
                    Helpers.drawTextStd(" Try connecting by IP (press ESC)", col1Pos, headerPos + rowHeight * 2, outline: false, fontSize: 24);
                }
            }
            else
            {
                var startServerRow = rowHeight + headerPos;
                for (int i = 0; i < publicServers.Count; i++)
                {
                    var server = publicServers[i];
                    Region region = null;
                    if (!isLAN) region = Global.regions.FirstOrDefault(r => r.ip == server.region.ip);
                    else region = Global.lanRegions.FirstOrDefault(r => r.ip == server.region.ip);
                    Helpers.drawTextStd(TCat.Option, server.name, col1Pos, startServerRow + (i * rowHeight2), outline: false, fontSize: 20, selected: selServerIndex == i);
                    Helpers.drawTextStd(TCat.Option, server.getMapShortName(), col2Pos, startServerRow + (i * rowHeight2), outline: false, fontSize: 20, selected: selServerIndex == i);
                    Helpers.drawTextStd(TCat.Option, GameMode.abbreviatedMode(server.gameMode), col3Pos, startServerRow + (i * rowHeight2), outline: false, fontSize: 20, selected: selServerIndex == i);
                    Helpers.drawTextStd(TCat.Option, getPlayerCountStr(server), col4Pos, startServerRow + (i * rowHeight2), outline: false, fontSize: 20, selected: selServerIndex == i);
                    Helpers.drawTextStd(TCat.Option, server.region.name, col5Pos, startServerRow + (i * rowHeight2), outline: false, fontSize: 20, selected: selServerIndex == i);
                    if (region != null) Helpers.drawTextStd(region.getDisplayPing(), col6Pos, startServerRow + (i * rowHeight2), outline: false, fontSize: 20, color: region.getPingColor());
                }
                DrawWrappers.DrawTextureHUD(Global.textures["cursor"], 12, startServerRow - 2 + (selServerIndex * rowHeight2));
            }

            if (failedRegions.Count > 0)
            {
                var failedRegionsList = failedRegions.Select(r => r.name);
                //failedRegionsList = new List<string>() { "EastUS", "WestUS", "Brazil" };
                var failedRegionsText = string.Join(", ", failedRegionsList);
                Helpers.drawTextStd("!Failed to get match list from regions: " + failedRegionsText + ".", col1Pos, 52, outline: false, fontSize: 14, style: Text.Styles.Italic, color: Color.Red);
            }

            if (!refreshing)
            {
                string escText = isLAN ? "[ESC]: Search by IP" : "[ESC]: Join private match";
                Helpers.drawTextStd(TCat.BotHelp, "[X]: Join, [C]: Refresh, [Z]: Back", Global.halfScreenW, 208, Alignment.Center, fontSize: 24);
                Helpers.drawTextStd(TCat.BotHelp, escText, Global.halfScreenW, 216, Alignment.Center, fontSize: 24);
            }
        }

        public string getPlayerCountStr(Server server)
        {
            var players = server.players;
            int botCount = players.Count(p => p.isBot);
            int humanCount = players.Count(p => !p.isBot);
            if (botCount == 0)
            {
                return humanCount + "/" + server.maxPlayers;
            }
            else
            {
                return players.Count + "(" + botCount + "b)/" + server.maxPlayers;
            }
        }
    }
}
