using Lidgren.Network;
using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace MMXOnline
{
    [ProtoContract]
    public class ServerPlayer
    {
        [ProtoMember(1)] public string name;
        [ProtoMember(2)] public int id;
        [ProtoMember(3)] public bool isHost;
        [ProtoMember(4)] public int charNum;
        [ProtoMember(5)] public int alliance;
        [ProtoMember(6)] public int? preferredAlliance;
        [ProtoMember(7)] public bool joinedLate;
        [ProtoMember(8)] public string deviceId;
        [ProtoMember(9)] public int kills;
        [ProtoMember(10)] public int deaths;
        [ProtoMember(11)] public int ping;
        [ProtoMember(12)] public bool isBot;
        [ProtoMember(13)] public bool isSpectator;
        [ProtoMember(14)] public int? autobalanceAlliance;
        [ProtoMember(15)] public int? startPing;

        [JsonIgnore]
        public NetConnection connection;

        [JsonIgnore]
        public bool alreadyAutobalanced;

        public ServerPlayer() { }

        public ServerPlayer(string name, int id, bool isHost, int charNum, int? preferredAlliance, string deviceId, NetConnection connection, int? startPing)
        {
            this.name = name;
            this.id = id;
            this.isHost = isHost;
            this.charNum = charNum;
            this.preferredAlliance = preferredAlliance;
            this.connection = connection;
            this.deviceId = deviceId;
            this.startPing = startPing;
        }

        public ServerPlayer clone()
        {
            return (ServerPlayer)MemberwiseClone();
        }
    }

    public class JoinServerResponse
    {
        public Server server;
        public JoinServerResponse(Server server)
        {
            this.server = server;
        }
        // Last player joined is always the requester
        public ServerPlayer getLastPlayer()
        {
            var players = server.players;
            for (int i = players.Count - 1; i >= 0; i--)
            {
                if (!players[i].isBot)
                {
                    return players[i];
                }
            }
            return null;
        }
    }
}
