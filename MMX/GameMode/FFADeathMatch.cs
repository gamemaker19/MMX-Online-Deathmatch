using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class FFADeathMatch : GameMode
    {
        public FFADeathMatch(Level level, int killsToWin, int? timeLimit) : base(level, timeLimit)
        {
            playingTo = killsToWin;
        }

        public override void render()
        {
            base.render();
        }

        public override void checkIfWinLogic()
        {
            Player winningPlayer = null;

            if (remainingTime <= 0)
            {
                winningPlayer = level.players[0];
            }
            else
            {
                foreach (var player in level.players)
                {
                    if (player.kills >= playingTo)
                    {
                        winningPlayer = player;
                        break;
                    }
                }
            }

            if (winningPlayer != null)
            {
                string winMessage = "You won!";
                string loseMessage = "You lost!";
                string loseMessage2 = winningPlayer.name + " wins";

                matchOverResponse = new RPCMatchOverResponse()
                {
                    winningAlliances = new HashSet<int>() { winningPlayer.alliance },
                    winMessage = winMessage,
                    loseMessage = loseMessage,
                    loseMessage2 = loseMessage2
                };
            }
        }

        public override void drawTopHUD()
        {
            var placeStr = "";
            var place = level.players.IndexOf(level.mainPlayer) + 1;
            placeStr = Helpers.getNthString(place);
            var topText = "Leader: " + level.players[0].kills.ToString();
            var botText = "Kills: " + level.mainPlayer.kills.ToString() + "(" + placeStr + ")";
            Helpers.drawTextStd(TCat.HUD, topText, 5, 5, Alignment.Left, fontSize: (uint)32);
            Helpers.drawTextStd(TCat.HUD, botText, 5, 15, Alignment.Left, fontSize: (uint)32);

            drawTimeIfSet(30);
        }

        public override void drawScoreboard()
        {
            base.drawScoreboard();
        }
    }
}
