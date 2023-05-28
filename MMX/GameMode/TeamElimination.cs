using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class TeamElimination : GameMode
    {
        public TeamElimination(Level level, int playingTo, int? timeLimit) : base(level, timeLimit)
        {
            this.playingTo = playingTo;
            isTeamMode = true;
            if (remainingTime == null)
            {
                remainingTime = 300;
                startTimeLimit = remainingTime;
            }
        }

        public override void drawTopHUD()
        {
            if (level.is1v1())
            {
                draw1v1TopHUD();
                return;
            }

            var redPlayersStillAlive = level.players.Where(p => !p.isSpectator && p.deaths < playingTo && p.alliance == redAlliance).ToList();
            var bluePlayersStillAlive = level.players.Where(p => !p.isSpectator && p.deaths < playingTo && p.alliance == blueAlliance).ToList();
            int lives = playingTo - level.mainPlayer.deaths;
            Helpers.drawTextStd(TCat.HUD, "Lives: " + lives.ToString(), 5, 5, Alignment.Left, fontSize: (uint)32);
            Helpers.drawTextStd(TCat.HUD, "Alive: ", 5, 15, Alignment.Left, fontSize: (uint)32);
            Helpers.drawTextStd(TCat.HUDColored, (redPlayersStillAlive.Count).ToString(), 62, 15, Alignment.Left, fontSize: (uint)32, outlineColor: Helpers.DarkRed);
            Helpers.drawTextStd(TCat.HUDColored, "/" + (bluePlayersStillAlive.Count).ToString(), 70, 15, Alignment.Left, fontSize: (uint)32, outlineColor: Helpers.DarkBlue);

            if (virusStarted != 1)
            {
                drawTimeIfSet(40);
            }
            else
            {
                drawVirusTime(40);
            }
        }

        public override void checkIfWinLogic()
        {
            if (level.time < 15) return;

            var redPlayersStillAlive = level.players.Where(p => !p.isSpectator && p.deaths < playingTo && p.alliance == redAlliance).ToList();
            var bluePlayersStillAlive = level.players.Where(p => !p.isSpectator && p.deaths < playingTo && p.alliance == blueAlliance).ToList();

            if (redPlayersStillAlive.Count > 0 && bluePlayersStillAlive.Count == 0)
            {
                matchOverResponse = new RPCMatchOverResponse()
                {
                    winningAlliances = new HashSet<int>() { redAlliance },
                    winMessage = "Victory!",
                    winMessage2 = "Red team wins",
                    loseMessage = "You lost!",
                    loseMessage2 = "Red team wins"
                };
            }
            else if (bluePlayersStillAlive.Count > 0 && redPlayersStillAlive.Count == 0)
            {
                matchOverResponse = new RPCMatchOverResponse()
                {
                    winningAlliances = new HashSet<int>() { blueAlliance },
                    winMessage = "Victory!",
                    winMessage2 = "Blue team wins",
                    loseMessage = "You lost!",
                    loseMessage2 = "Blue team wins"
                };
            }
            else if (remainingTime <= 0 && virusStarted >= 3)
            {
                matchOverResponse = new RPCMatchOverResponse()
                {
                    winningAlliances = new HashSet<int>() { },
                    winMessage = "Stalemate!",
                    loseMessage = "Stalemate!"
                };
            }
        }

        public override void drawScoreboard()
        {
            base.drawTeamScoreboard();
        }
    }
}
