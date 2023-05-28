using Lidgren.Network;
using MMXOnline;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RelayServer
{
    public class Program
    {
        static TcpListener server = null;

        static NetPeerConfiguration config;
        static NetServer netServer;

        const string banListFile = "banlist.json";
        const string overrideVersionFile = "overrideversion.txt";
        const string encryptionKeyFile = "encryptionKey.txt";
        const string secretPrefixFile = "secretPrefix.txt";

        static void updateServer(bool isFirstTime)
        {
            try
            {
                if (File.Exists(banListFile))
                {
                    var banListJson = File.ReadAllText(banListFile);
                    Global.banList = JsonConvert.DeserializeObject<List<BanEntry>>(banListJson) ?? new List<BanEntry>();
                    if (!isFirstTime) Console.WriteLine("Updated ban list...");
                }

                if (File.Exists(overrideVersionFile))
                {
                    string overrideVersionStr = File.ReadAllText(overrideVersionFile);
                    overrideVersionStr = overrideVersionStr.Replace(",", ".");
                    if (decimal.TryParse(overrideVersionStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal overrideVersion))
                    {
                        if (Helpers.compareVersions(overrideVersion, Global.version) == 1)
                        {
                            Global.version = overrideVersion;
                            if (!isFirstTime) Console.WriteLine(string.Format("Updated version to v{0}...", Global.version));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("updateServer failed. Exception: " + e.Message);
            }
        }

        static void Main(string[] args)
        {
            updateServer(true);

            Console.WriteLine(string.Format("Starting matchmaking server (v{0})...", Global.version));

            if (File.Exists(encryptionKeyFile))
            {
                Global.encryptionKey = File.ReadAllText(encryptionKeyFile);
                if (string.IsNullOrEmpty(Global.encryptionKey))
                {
                    throw new Exception("encryption key can't be blank.");
                }
                Console.WriteLine(string.Format("Successfully read encryption key from encryptionKey.txt"));
            }
            else
            {
                Console.WriteLine(string.Format("No encryptionKey.txt file found. Reports and bans will not work. If you want this functionality, you should create one with a secure string as its content."));
            }

            if (File.Exists(secretPrefixFile))
            {
                Global.secretPrefix = File.ReadAllText(secretPrefixFile);
                if (string.IsNullOrEmpty(Global.secretPrefix))
                {
                    throw new Exception("secret prefix can't be blank.");
                }
                Console.WriteLine(string.Format("Successfully read secret prefix from secretPrefix.txt"));
            }
            else
            {
                Console.WriteLine(string.Format("No secretPrefix.txt file found. You should create one with a secure string as its content if you are running this as an internet server, or it will be less secure. Ignore if you are running this as a LAN relay server on a local area network."));
            }
            
            Thread thread = new Thread(udpMain);
            thread.Start();
            server = new TcpListener(IPAddress.Any, Global.basePort);
            server.Start();

            while (true)
            {
                Helpers.tryWrap(iteration, true);
            }
        }

        static void iteration()
        {
            using (TcpClient client = server.AcceptTcpClient())
            {
                using NetworkStream networkStream = client.GetStream();

                client.ReceiveTimeout = 5000;
                client.SendTimeout = 5000;

                string message = client.ReadStringMessage(networkStream);

                Helpers.debugLog("Client sent data message: " + message);

                if (message.StartsWith("CheckBan:"))
                {
                    string deviceId = message.RemovePrefix("CheckBan:");

                    string ipAddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                    BanEntry banEntry = Global.banList.FirstOrDefault(b => b.isBanned(ipAddress, deviceId, 0));
                    if (banEntry == null) banEntry = Global.banList.FirstOrDefault(b => b.isBanned(ipAddress, deviceId, 1));
                    if (banEntry == null) banEntry = Global.banList.FirstOrDefault(b => b.isBanned(ipAddress, deviceId, 2));

                    if (banEntry != null)
                    {
                        string banData = JsonConvert.SerializeObject(banEntry);
                        client.SendStringMessage("CheckBan:" + banData, networkStream);
                    }
                    else
                    {
                        client.SendStringMessage("CheckBan:", networkStream);
                    }
                }

                if (message == "GetServers")
                {
                    var serverBytes = Helpers.serialize(Server.servers.Keys.ToList());
                    client.SendMessage(serverBytes, networkStream);
                }
                else if (message.StartsWith("GetServer:"))
                {
                    string serverName = message.RemovePrefix("GetServer:");
                    Server server = null;
                    foreach (var s in Server.servers)
                    {
                        if (s.Key.name == serverName)
                        {
                            server = s.Key;
                            break;
                        }
                    }
                    byte[] serverBytes = new byte[] { };
                    if (server != null) serverBytes = Helpers.serialize(server);
                    client.SendMessage(serverBytes, networkStream);
                }
                else if (message == "GetVersion")
                {
                    client.SendStringMessage("GetVersion:" + Global.version.ToString(), networkStream);
                }
                else if (message.StartsWith("CreateServer"))
                {
                    var requestServerJson = message.RemovePrefix("CreateServer:");
                    var requestServer = JsonConvert.DeserializeObject<Server>(requestServerJson);
                    var server = Server.servers.Keys.Where(s => s.name == requestServer.name).FirstOrDefault();

                    if (Helpers.compareVersions(requestServer.gameVersion, Global.version) == -1)
                    {
                        client.SendStringMessage("CreateServer:fail:Outdated game version (update to v" + Global.version.ToString() + ")", networkStream);
                    }
                    else if (Server.servers.Count >= Global.maxServers)
                    {
                        client.SendStringMessage("CreateServer:fail:Too many concurrent servers (max " + Server.servers.Count.ToString() + ")", networkStream);
                    }
                    else if (server == null)
                    {
                        /*
                        if (requestServer.isCustomMap() && Server.servers.Keys.Any(s => !s.isLAN && !s.hidden && s.isCustomMap()))
                        {
                            client.SendStringMessage("CreateServer:fail:Too many public custom servers (max 1)", networkStream);
                        }
                        */

                        bool sameUserMatch = false;
                        foreach (var myServer in Server.servers.Keys)
                        {
                            string myServerIp = myServer?.host?.connection?.RemoteEndPoint?.Address?.ToString();
                            string connectingIp = ((IPEndPoint)client.Client?.RemoteEndPoint)?.Address?.ToString();
                            if (myServerIp == connectingIp)
                            {
                                sameUserMatch = true;
                                break;
                            }
                        }

                        if (sameUserMatch)
                        {
                            client.SendStringMessage("CreateServer:fail:Same user can't create more than 1 match", networkStream);
                        }
                        else
                        {
                            server = new Server(Global.version, requestServer.region, requestServer.name, requestServer.level, requestServer.shortLevelName, requestServer.gameMode, requestServer.playTo, requestServer.botCount, requestServer.maxPlayers, requestServer.timeLimit ?? 0,
                                requestServer.fixedCamera, requestServer.hidden, requestServer.netcodeModel, requestServer.netcodeModelPing, requestServer.isLAN, requestServer.mirrored, requestServer.useLoadout, requestServer.gameChecksum, 
                                requestServer.customMapChecksum, requestServer.customMapUrl, requestServer.extraCpuCharData, requestServer.customMatchSettings, requestServer.disableHtSt, requestServer.disableVehicles);
                            server.start();
                            Server.servers[server] = true;
                            client.SendStringMessage("CreateServer:" + JsonConvert.SerializeObject(server), networkStream);
                        }
                    }
                    else
                    {
                        client.SendStringMessage("CreateServer:fail:Server name already exists", networkStream);
                    }
                }
                else if (message == Global.secretPrefix + "updateserver")
                {
                    updateServer(false);
                }
                else if (message == Global.secretPrefix + "getbanlist")
                {
                    try
                    {
                        string banListJson = File.ReadAllText(banListFile);
                        client.SendStringMessage("getbanlist:" + banListJson, networkStream);
                    }
                    catch (Exception ex)
                    {
                        client.SendStringMessage("getbanlist:fail:" + ex.GetType().Name, networkStream);
                    }
                }
                else if (message.StartsWith(Global.secretPrefix + "updatebanlist"))
                {
                    string banListJson = message.RemovePrefix(Global.secretPrefix + "updatebanlist:");
                    try
                    {
                        JsonConvert.DeserializeObject<List<BanEntry>>(banListJson);
                        File.WriteAllText(banListFile, banListJson);
                        updateServer(false);
                    }
                    catch (Exception ex)
                    {
                        client.SendStringMessage("updatebanlist:fail:" + ex.GetType().Name, networkStream);
                    }
                    client.SendStringMessage("updatebanlist:Success", networkStream);
                }
                else if (message.StartsWith(Global.secretPrefix + "updateversion"))
                {
                    string versionString = message.RemovePrefix(Global.secretPrefix + "updateversion:");
                    try
                    {
                        decimal version = decimal.Parse(versionString);
                        File.WriteAllText(overrideVersionFile, versionString);
                        updateServer(false);
                    }
                    catch (Exception ex)
                    {
                        client.SendStringMessage("updateversion:fail:" + ex.GetType().Name, networkStream);
                    }
                    client.SendStringMessage("updateversion:Success", networkStream);
                }
                // BAN TOOL SECTION
                else if (message.StartsWith(Global.secretPrefix + "removeallmatches"))
                {
                    foreach (var server in Server.servers.Keys)
                    {
                        lock (server)
                        {
                            server.killServer = true;
                        }
                    }
                    client.SendStringMessage("removeallmatches:Success:", networkStream);
                }
                else if (message.StartsWith(Global.secretPrefix + "getbanstatusdatablob"))
                {
                    try
                    {
                        string dataBlobStr = message.RemovePrefix(Global.secretPrefix + "getbanstatusdatablob:");
                        var dataBlob = JsonConvert.DeserializeObject<ReportedPlayerDataBlob>(AesOperation.DecryptString(Global.encryptionKey, dataBlobStr));
                        var bannedPlayer = Global.banList.FirstOrDefault(b => b.ipAddress == dataBlob.ipAddress || b.deviceId == dataBlob.deviceId);
                        if (bannedPlayer != null)
                        {
                            //string encryptedIp = AesOperation.EncryptString(Global.encryptionKey, dataBlob.ipAddress);
                            //string encryptedDeviceId = AesOperation.EncryptString(Global.encryptionKey, dataBlob.deviceId);
                            var bannedPlayerResponse = new BanResponse(bannedPlayer.banType, bannedPlayer.reason, bannedPlayer.bannedUntil);
                            client.SendStringMessage("getbanstatusdatablob:Success:" + JsonConvert.SerializeObject(bannedPlayerResponse), networkStream);
                        }
                        else
                        {
                            client.SendStringMessage("getbanstatusdatablob:Success:", networkStream);
                        }
                    }
                    catch (Exception ex)
                    {
                        client.SendStringMessage("getbanstatusdatablob:Error:" + ex.GetType().Name, networkStream);
                    }
                }
                else if (message.StartsWith(Global.secretPrefix + "bandatablob"))
                {
                    try
                    {
                        string requestJson = message.RemovePrefix(Global.secretPrefix + "bandatablob:");
                        var banRequest = JsonConvert.DeserializeObject<BanRequest>(requestJson);
                        var dataBlob = JsonConvert.DeserializeObject<ReportedPlayerDataBlob>(AesOperation.DecryptString(Global.encryptionKey, banRequest.dataBlobStr));

                        var banListJson = File.ReadAllText(banListFile);
                        var banList = JsonConvert.DeserializeObject<List<BanEntry>>(banListJson) ?? new List<BanEntry>();

                        var bannedPlayer = banList.FirstOrDefault(b => b.ipAddress == dataBlob.ipAddress || b.deviceId == dataBlob.deviceId);
                        if (bannedPlayer != null)
                        {
                            client.SendStringMessage("bandatablob:Success", networkStream);
                            return;
                        }

                        banList.Add(new BanEntry(dataBlob.ipAddress, dataBlob.deviceId, banRequest.reason, banRequest.bannedUntil, banRequest.banType));

                        File.WriteAllText(banListFile, JsonConvert.SerializeObject(banList));
                        updateServer(false);
                        client.SendStringMessage("bandatablob:Success", networkStream);
                    }
                    catch (Exception ex)
                    {
                        client.SendStringMessage("bandatablob:Error:" + ex.GetType().Name, networkStream);
                    }
                }
                else if (message.StartsWith(Global.secretPrefix + "unbandatablob"))
                {
                    try
                    {
                        string request = message.RemovePrefix(Global.secretPrefix + "unbandatablob:");
                        var dataBlob = JsonConvert.DeserializeObject<ReportedPlayerDataBlob>(AesOperation.DecryptString(Global.encryptionKey, request));

                        var banListJson = File.ReadAllText(banListFile);
                        var banList = JsonConvert.DeserializeObject<List<BanEntry>>(banListJson) ?? new List<BanEntry>();

                        var bannedPlayer = banList.FirstOrDefault(b => b.ipAddress == dataBlob.ipAddress || b.deviceId == dataBlob.deviceId);
                        if (bannedPlayer == null)
                        {
                            client.SendStringMessage("unbandatablob:Success", networkStream);
                            return;
                        }

                        banList.RemoveAll(b => b == bannedPlayer);
                        File.WriteAllText(banListFile, JsonConvert.SerializeObject(banList));
                        updateServer(false);
                        client.SendStringMessage("unbandatablob:Success", networkStream);
                    }
                    catch (Exception ex)
                    {
                        client.SendStringMessage("unbandatablob:Error:" + ex.GetType().Name, networkStream);
                    }
                }
            }
        }

        static void udpMain()
        {
            config = new NetPeerConfiguration("matchmaking");
            config.MaximumConnections = 10000;
            config.MaximumTransmissionUnit = Global.maxUnconnectedMTUSize;
            config.Port = Global.basePort;
            config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
            netServer = new NetServer(config);
            netServer.Start();

            while (true)
            {
                Helpers.tryWrap(udpIteration, true);
            }
        }

        static void udpIteration()
        {
            netServer.MessageReceivedEvent.WaitOne();
            NetIncomingMessage im;
            while ((im = netServer.ReadMessage()) != null)
            {
                // handle incoming message
                switch (im.MessageType)
                {
                    case NetIncomingMessageType.UnconnectedData:
                        string message = im.ReadString();
                        Helpers.debugLog("Client sent data message: " + message);
                        if (message == "GetVersion")
                        {
                            NetOutgoingMessage om = netServer.CreateMessage();
                            om.Write("GetVersion:" + Global.version.ToString());
                            netServer.SendUnconnectedMessage(om, im.SenderEndPoint.Address.ToString(), im.SenderEndPoint.Port);
                        }
                        break;
                    default:
                        break;
                }
                netServer.Recycle(im);
            }
        }
    }
}