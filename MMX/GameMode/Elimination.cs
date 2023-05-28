using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class Elimination : GameMode
    {
        public Elimination(Level level, int lives, int? timeLimit) : base(level, timeLimit)
        {
            playingTo = lives;
            if (remainingTime == null && !level.is1v1())
            {
                remainingTime = 300;
                startTimeLimit = remainingTime;
            }
        }

        public override void render()
        {
            base.render();
        }

        public override void checkIfWinLogic()
        {
            if (level.time < 10) return;

            Player winningPlayer = null;

            var playersStillAlive = level.players.Where(p => !p.isSpectator && p.deaths < playingTo).ToList();

            if (playersStillAlive.Count == 1)
            {
                winningPlayer = playersStillAlive[0];
            }

            if (remainingTime <= 0 && (virusStarted >= 3 || level.is1v1()) && winningPlayer == null)
            {
                matchOverResponse = new RPCMatchOverResponse()
                {
                    winningAlliances = new HashSet<int>() { },
                    winMessage = "Stalemate!",
                    loseMessage = "Stalemate!"
                };
            }
            else if (winningPlayer != null)
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
            if (level.is1v1())
            {
                draw1v1TopHUD();
                return;
            }

            var playersStillAlive = level.players.Where(p => !p.isSpectator && p.deaths < playingTo).ToList();
            int lives = playingTo - level.mainPlayer.deaths;
            var topText = "Lives: " + lives.ToString();
            var botText = "Alive: " + (playersStillAlive.Count).ToString();
            Helpers.drawTextStd(TCat.HUD, topText, 5, 5, Alignment.Left, fontSize: (uint)32);
            Helpers.drawTextStd(TCat.HUD, botText, 5, 15, Alignment.Left, fontSize: (uint)32);

            if (virusStarted != 1)
            {
                drawTimeIfSet(30);
            }
            else
            {
                drawVirusTime(30);
            }
        }

        public override void drawScoreboard()
        {
            base.drawScoreboard();
        }
    }
}
