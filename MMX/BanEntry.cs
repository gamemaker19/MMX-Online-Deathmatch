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
    public class BanEntry
    {
        [ProtoMember(1)] public string ipAddress;
        [ProtoMember(2)] public string deviceId;
        [ProtoMember(3)] public string reason;
        [ProtoMember(4)] public DateTime? bannedUntil;
        [ProtoMember(5)] public int banType;    // 0 = ban, 1 = mute + votekick ban, 2 = warning

        public BanEntry() { }

        public BanEntry(string ipAddress, string deviceId, string reason, DateTime? bannedUntil, int banType)
        {
            this.ipAddress = ipAddress;
            this.deviceId = deviceId;
            this.reason = reason;
            this.bannedUntil = bannedUntil;
            this.banType = banType;
        }

        public bool isBanned(string senderIp, string senderDeviceId, int banType)
        {
            if (this.banType != banType) return false;

            if (!string.IsNullOrEmpty(senderIp) && !string.IsNullOrEmpty(ipAddress) && ipAddress.ToUpperInvariant().Contains(senderIp.ToUpperInvariant()))
            {
                if (bannedUntil != null)
                {
                    return DateTime.UtcNow < bannedUntil.Value;
                }
                return true;
            }
            if (!string.IsNullOrEmpty(senderDeviceId) && !string.IsNullOrEmpty(deviceId) && deviceId.ToUpperInvariant().Contains(senderDeviceId.ToUpperInvariant()))
            {
                if (bannedUntil != null)
                {
                    return DateTime.UtcNow < bannedUntil.Value;
                }
                return true;
            }
            return false;
        }
    }

    public class BanToolRegion
    {
        public string ip;
        public string name;
        public BanToolRegion(string ip, string name)
        {
            this.ip = ip;
            this.name = name;
        }
    }

    public class BanRequest
    {
        public string dataBlobStr;
        public string reason;
        public int banType;
        public DateTime? bannedUntil;
        public BanRequest(string dataBlobStr, string reason, int banType, DateTime? bannedUntil)
        {
            this.dataBlobStr = dataBlobStr;
            this.reason = reason;
            this.banType = banType;
            this.bannedUntil = bannedUntil;
        }
    }

    public class BanResponse
    {
        public int banType;
        public string reason;
        public DateTime? bannedUntil;

        public BanResponse(int banType, string reason, DateTime? bannedUntil)
        {
            this.banType = banType;
            this.reason = reason;
            this.bannedUntil = bannedUntil;
        }

        public string getStatusString()
        {
            string banTypeString = "BANNED";
            if (banType == 1) banTypeString = "CHAT/VOTE BANNED";
            if (banType == 2) banTypeString = "WARNING ISSUED";

            string banDaysString = " (indefinite)";
            if (bannedUntil != null)
            {
                banDaysString = " (ends " + bannedUntil.ToString() + ")";
            }

            return banTypeString + banDaysString;
        }
    }

    public class ReportedPlayerDataBlob
    {
        public string name;
        public string ipAddress;
        public string deviceId;
        public ReportedPlayerDataBlob(string name, string ipAddress, string deviceId)
        {
            this.name = name;
            this.ipAddress = ipAddress;
            this.deviceId = deviceId;
        }
    }

    public class ReportedPlayer
    {
        public string name;
        public string dataBlob;
        public List<string> chatHistory;
        public string timestamp;
        public string description;

        public ReportedPlayer() { }

        public ReportedPlayer(string name, string ipAddress, string deviceId)
        {
            this.name = name;

            var dataBlob = new ReportedPlayerDataBlob(name, ipAddress, deviceId);
            string dataBlobJson = JsonConvert.SerializeObject(dataBlob);
            string encryptedBlob = "";
            if (!string.IsNullOrEmpty(Global.encryptionKey))
            {
                encryptedBlob = AesOperation.EncryptString(Global.encryptionKey, dataBlobJson);
            }

            this.dataBlob = encryptedBlob;
        }

        public string getFileName()
        {
            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();

            // Builds a string out of valid chars
            var validFilename = new string(name.Where(ch => !invalidFileNameChars.Contains(ch)).ToArray());

            return "report_" + validFilename + ".txt";
        }
    }
}
