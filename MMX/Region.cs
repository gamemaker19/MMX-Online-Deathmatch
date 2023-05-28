using Lidgren.Network;
using ProtoBuf;
using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MMXOnline
{
    [ProtoContract]
    public class Region
    {
        [ProtoMember(1)] public string name;
        [ProtoMember(2)] public string ip;
        [ProtoMember(3)] public int? ping;

        private NetClient pingClient;
        public NetClient getPingClient()
        {
            return pingClient;
        }

        public Region() { }

        public Region(string name, string ip)
        {
            this.name = name;
            this.ip = ip;
        }

        public static async Task<Region> tryCreateWithPingClient(string name, string ip)
        {
            if (string.IsNullOrEmpty(ip)) return null;

            var pingClient = ServerClient.GetPingClient(ip);
            await Task.Delay(1000);
            if (pingClient.ServerConnection == null)
            {
                return null;
            }
            return new Region(name, ip)
            {
                pingClient = pingClient
            };
        }

        public int? getPing()
        {
            if (ping == null)
            {
                return Global.regions.FirstOrDefault(r => r.ip == ip)?.ping;
            }

            return ping;
        }

        public void computePing()
        {
            if (pingClient == null)
            {
                pingClient = ServerClient.GetPingClient(ip);
            }

            int attempts = 0;
            while (pingClient.ServerConnection == null || pingClient.ServerConnection.AverageRoundtripTime == -1)
            {
                attempts++;
                if (attempts > 20) return;
                Thread.Sleep(100);
            }
            ping = (int)(pingClient.ServerConnection.AverageRoundtripTime * 1000);
        }

        public string getDisplayPing()
        {
            if (ping == null) return "?";
            return ping.Value.ToString();
        }

        public Color getPingColor()
        {
            return Helpers.getPingColor(ping, Global.defaultThresholdPing);
        }
    }
}
