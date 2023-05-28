using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class KingOfTheHill : GameMode
    {
        public KingOfTheHill(Level level, int? timeLimit) : base(level, timeLimit ?? 1)
        {
            isTeamMode = true;
            playingTo = 100;
        }

        public override void render()
        {
            base.render();
            if (level?.hill == null) return;

            drawObjectiveNavpoint(level.hill.alliance == level.mainPlayer.alliance ? "Defend" : "Attack", level.hill.pos);
        }

        public override void drawTopHUD()
        {
            int hudcpY = 0;

            ControlPoint hill = level?.hill;
            if (hill == null) return;

            Color textColor = Color.Green;

            string hillText = hill.getHillText();

            int allianceFactor = (hill.alliance == redAlliance ? 0 : 2);
            int hudCpFrameIndex = (hill.num - 1) + allianceFactor;
            if (hill.alliance == neutralAlliance) hudCpFrameIndex = 4;
            Global.sprites["hud_cp"].drawToHUD(hudCpFrameIndex, 5, 5 + hudcpY);
            float perc = hill.captureTime / hill.maxCaptureTime;

            if (hill.alliance == neutralAlliance)
            {
                perc = Math.Max(hill.redCaptureTime, hill.blueCaptureTime) / hill.maxCaptureTime;
            }

            int barsFull = MathF.Round(perc * 16f);
            for (int i = 0; i < barsFull; i++)
            {
                int hudCpBarIndex = 2;
                if (hill.alliance == redAlliance) hudCpBarIndex = 1;
                else if (hill.alliance == blueAlliance) hudCpBarIndex = 0;
                else
                {
                    hudCpBarIndex = hill.blueCaptureTime > 0 ? 1 : 0;
                }
                Global.sprites["hud_cp_bar"].drawToHUD(hudCpBarIndex, 5 + 17 + (i * 2), 5 + 3 + hudcpY);
            }

            Helpers.drawTextStd(TCat.HUD, hillText, 38, 10 + hudcpY, Alignment.Center, fontSize: (uint)17, color: textColor, outlineColor: Color.Black);
            hudcpY += 16;

            drawTimeIfSet(4 + hudcpY);
        }

        public override void checkIfWinLogic()
        {
            if (remainingTime > 0) return;

            ControlPoint hill = Global.level.hill;
            if (hill.captureTime > 0 || hill.blueCaptureTime > 0 || hill.redCaptureTime > 0) return;
            if (hill.contested()) return;

            if (hill.alliance == blueAlliance)
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
            else if (hill.alliance == redAlliance)
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
            else
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
            drawTeamScoreboard();
        }
    }
}
