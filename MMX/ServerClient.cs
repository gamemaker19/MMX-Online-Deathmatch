using Lidgren.Network;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace MMXOnline
{
    public class ServerClient
    {
        public NetClient client;
        public bool isHost;
        public ServerPlayer serverPlayer;
        Stopwatch packetLossStopwatch = new Stopwatch();
        Stopwatch gameLoopStopwatch = new Stopwatch();
        public long packetsReceived;

        private ServerClient(NetClient client, bool isHost)
        {
            this.client = client;
            this.isHost = isHost;
        }

        public static NetClient GetPingClient(string serverIp)
        {
            NetPeerConfiguration config = new NetPeerConfiguration("matchmaking");
            config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);
            config.AutoFlushSendQueue = true;
#if DEBUG
            config.SimulatedMinimumLatency = Global.simulatedLatency;
            config.SimulatedLoss = Global.simulatedPacketLoss;
            config.SimulatedDuplicatesChance = Global.simulatedDuplicates;
#endif
            var client = new NetClient(config);
            client.Start();
            NetOutgoingMessage hail = client.CreateMessage("a");
            client.Connect(serverIp, Global.basePort, hail);

            return client;
        }

        public static ServerClient Create(string serverIp, string serverName, int serverPort, ServerPlayer inputServerPlayer, out JoinServerResponse joinServerResponse, out string error)
        {
            error = null;
            NetPeerConfiguration config = new NetPeerConfiguration(serverName);
            config.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);
            config.AutoFlushSendQueue = false;
            config.ConnectionTimeout = Server.connectionTimeoutSeconds;
#if DEBUG
            config.SimulatedMinimumLatency = Global.simulatedLatency;
            config.SimulatedLoss = Global.simulatedPacketLoss;
            config.SimulatedDuplicatesChance = Global.simulatedDuplicates;
#endif
            var client = new NetClient(config);
            client.Start();
            NetOutgoingMessage hail = client.CreateMessage(JsonConvert.SerializeObject(inputServerPlayer));
            client.Connect(serverIp, serverPort, hail);

            var serverClient = new ServerClient(client, inputServerPlayer.isHost);

            int count = 0;
            while (count < 20)
            {
                serverClient.getMessages(out var messages, false);
                foreach (var message in messages)
                {
                    if (message.StartsWith("joinservertargetedresponse:"))
                    {
                        joinServerResponse = JsonConvert.DeserializeObject<JoinServerResponse>(message.RemovePrefix("joinservertargetedresponse:"));
                        serverClient.serverPlayer = joinServerResponse.getLastPlayer();
                        return serverClient;
                    }
                    else if (message.StartsWith("hostdisconnect:"))
                    {
                        var reason = message.Split(':')[1];
                        error = "Could not join: " + reason;
                        joinServerResponse = null;
                        serverClient.disconnect("client couldn't get response");
                        return null;
                    }
                }
                count++;
                Thread.Sleep(100);
            }

            error = "Failed to get connect response from server.";
            joinServerResponse = null;
            serverClient.disconnect("client couldn't get response");
            return null;
        }

        /*
        public void broadcast(string message)
        {
            send("broadcast:" + message);
        }

        public void sendToHost(string message)
        {
            send("host:" + message);
        }

        public void ping()
        {
            send("ping");
        }
        
        private void send(string message)
        {
            NetOutgoingMessage om = client.CreateMessage(message);
            client.SendMessage(om, NetDeliveryMethod.Unreliable);
            client.FlushSendQueue();
        }
        */

        float gameLoopLagTime;
        public bool isLagging()
        {
            //Global.debugString1 = packetLossStopwatch.ElapsedMilliseconds.ToString();
            if (packetLossStopwatch.ElapsedMilliseconds > 2000 || gameLoopLagTime > 0)
            {
                return true;
            }
            return false;
        }

        public void disconnect(string disconnectMessage)
        {
            client.Disconnect(disconnectMessage);
            flush();
        }

        public void flush()
        {
            client.FlushSendQueue();
        }

        public void rpc(RPC rpcTemplate, params byte[] arguments)
        {
            int rpcIndex = RPC.templates.IndexOf(rpcTemplate);
            if (rpcIndex == -1)
            {
                throw new Exception("RPC index not found!");
            }
            byte rpcIndexByte = (byte)rpcIndex;
            NetOutgoingMessage om = client.CreateMessage();
            om.Write(rpcIndexByte);
            om.Write((ushort)arguments.Length);
            om.Write(arguments);
            client.SendMessage(om, rpcTemplate.netDeliveryMethod);
        }

        public void rpc(RPC rpcTemplate, string message)
        {
            int rpcIndex = RPC.templates.IndexOf(rpcTemplate);
            if (rpcIndex == -1)
            {
                throw new Exception("RPC index not found!");
            }
            byte rpcIndexByte = (byte)rpcIndex;
            NetOutgoingMessage om = client.CreateMessage();
            om.Write(rpcIndexByte);
            om.Write(message);
            client.SendMessage(om, rpcTemplate.netDeliveryMethod);
        }

        public void getMessages(out List<string> stringMessages, bool invokeRpcs)
        {
            if (gameLoopStopwatch.ElapsedMilliseconds > 250 && Global.level?.time > 1)
            {
                gameLoopLagTime = gameLoopStopwatch.ElapsedMilliseconds / 1000f;
            }
            gameLoopStopwatch.Restart();
            Helpers.decrementTime(ref gameLoopLagTime);

            stringMessages = new List<string>();
            NetIncomingMessage im;
            while ((im = client.ReadMessage()) != null)
            {
                string text = "";
                // handle incoming message
                switch (im.MessageType)
                {
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.ErrorMessage:
                    case NetIncomingMessageType.WarningMessage:
                    case NetIncomingMessageType.VerboseDebugMessage:
                        text = im.ReadString();
                        //Global.logToConsole("Misc message: " + text);
                        break;
                    case NetIncomingMessageType.ConnectionLatencyUpdated:
                        //var latency = (int)MathF.Round(im.ReadFloat() * 1000);
                        //Global.logToConsole("Average latency: " + latency.ToString());
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        NetConnectionStatus status = (NetConnectionStatus)im.ReadByte();
                        string reason = im.ReadString();
                        if (status == NetConnectionStatus.Disconnected)
                        {
                            stringMessages.Add("hostdisconnect:" + reason);
                        }

                        break;
                    case NetIncomingMessageType.Data:

                        var rpcIndexByte = im.ReadByte();
                        var rpcTemplate = RPC.templates[rpcIndexByte];

                        if (rpcTemplate is RPCPeriodicServerPing)
                        {
                            packetLossStopwatch.Restart();
                            packetsReceived++;
                        }

                        if (!rpcTemplate.isString)
                        {
                            ushort argCount = BitConverter.ToUInt16(im.ReadBytes(2), 0);
                            var bytes = im.ReadBytes((int)argCount);
                            if (invokeRpcs)
                            {
                                Helpers.tryWrap(() => { rpcTemplate.invoke(bytes); }, false);
                            }
                        }
                        else
                        {
                            var message = im.ReadString();
                            if (invokeRpcs)
                            {
                                if (rpcTemplate is RPCJoinLateResponse)
                                {
                                    rpcTemplate.invoke(message);
                                }
                                else
                                {
                                    Helpers.tryWrap(() => { rpcTemplate.invoke(message); }, false);
                                }
                            }
                            stringMessages.Add(message);
                        }
                        
                        break;
                    default:
                        //Output("Unhandled type: " + im.MessageType + " " + im.LengthBytes + " bytes");
                        break;
                }
                client.Recycle(im);
            }
        }
    }
}
