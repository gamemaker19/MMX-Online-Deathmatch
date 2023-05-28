using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MMXOnline
{
    public class KillFeedEntry
    {
        public Player killer;
        public Player assister;
        public Player victim;
        public int? weaponIndex;
        public string customMessage;
        public int customMessageAlliance = GameMode.blueAlliance;
        public float time = 0;
        public int? maverickKillFeedIndex;

        public KillFeedEntry(Player killer, Player assister, Player victim, int? weaponIndex, int? maverickKillFeedIndex = null)
        {
            this.killer = killer;
            this.assister = assister;
            this.victim = victim;
            this.weaponIndex = weaponIndex;
            this.maverickKillFeedIndex = maverickKillFeedIndex;
        }

        public KillFeedEntry(string message, int alliance, Player player = null)
        {
            customMessage = message;
            customMessageAlliance = alliance;
            victim = player;
            if (message.EndsWith("scored") || message.EndsWith("captured point"))
            {
                Global.playSound("ching");
            }
        }

        public void sendRpc()
        {
            var json = JsonConvert.SerializeObject(new RPCKillFeedEntryResponse(customMessage, customMessageAlliance, victim?.id));
            Global.serverClient?.rpc(RPC.sendKillFeedEntry, json);
        }

        public string rawString()
        {
            if (!string.IsNullOrEmpty(customMessage))
            {
                return customMessage;
            }
            return string.Format("Killer: {0}, assister: {1}, victim: {2}, weapon index: {3}",
                killer?.name ?? "", assister?.name ?? "", victim?.name ?? "", weaponIndex == null ? "" : weaponIndex.Value.ToString());
        }
    }
}
