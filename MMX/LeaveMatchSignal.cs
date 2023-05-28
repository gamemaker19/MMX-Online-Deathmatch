using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace MMXOnline
{
    public enum LeaveMatchScenario
    {
        LeftManually,
        MatchOver,
        ServerShutdown,
        Recreate,
        Rejoin,
        Kicked
    }

    public class LeaveMatchSignal
    {
        public LeaveMatchScenario leaveMatchScenario;
        public Server newServerData;
        public string kickReason;

        public LeaveMatchSignal(LeaveMatchScenario leaveMatchScenario, Server newServerData, string kickReason)
        {
            this.leaveMatchScenario = leaveMatchScenario;
            this.newServerData = newServerData;
            this.kickReason = kickReason;
        }

        public void createNewServer()
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            byte[] serverResponse = null;
            do
            {
                serverResponse = Global.matchmakingQuerier.send(newServerData.region.ip, "GetServer:" + newServerData.name, 1000);
                if (stopWatch.ElapsedMilliseconds > 5000)
                {
                    Menu.change(new ErrorMenu("Error: Could not create new server.", new MainMenu()));
                    return;
                }
            }
            while (!serverResponse.IsNullOrEmpty());

            HostMenu.createServer(SelectCharacterMenu.playerData.charNum, newServerData, null, true, new MainMenu(), out string errorMessage);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                // Retry once
                HostMenu.createServer(SelectCharacterMenu.playerData.charNum, newServerData, null, true, new MainMenu(), out errorMessage);
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    Menu.change(new ErrorMenu(errorMessage, new MainMenu()));
                }
            }
        }

        public void rejoinNewServer()
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            byte[] serverResponse;
            do
            {
                serverResponse = Global.matchmakingQuerier.send(newServerData.region.ip, "GetServer:" + newServerData.name, 1000);
                if (stopWatch.ElapsedMilliseconds > 5000)
                {
                    Menu.change(new ErrorMenu("Error: Could not rejoin new server.", new MainMenu()));
                    return;
                }
            }
            while (serverResponse.IsNullOrEmpty());

            Server server = Helpers.deserialize<Server>(serverResponse);

            JoinMenu.joinServer(server);
        }
    }
}
