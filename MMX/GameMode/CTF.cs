using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class CTF : GameMode
    {
        public CTF(Level level, int playingTo, int? timeLimit) : base(level, timeLimit)
        {
            this.playingTo = playingTo;
            isTeamMode = true;
        }

        public override void render()
        {
            base.render();
            drawObjectiveNavpoint("Capture", level.mainPlayer.alliance == redAlliance ? level.blueFlag.pos : level.redFlag.pos);
            if (level.mainPlayer.character?.flag != null)
            {
                drawObjectiveNavpoint("Return", level.mainPlayer.alliance == redAlliance ? level.redFlag.pedestal.pos : level.blueFlag.pedestal.pos);
            }
            else
            {
                drawObjectiveNavpoint("Defend", level.mainPlayer.alliance == redAlliance ? level.redFlag.pos : level.blueFlag.pos);
            }
        }

        public override void drawTopHUD()
        {
            drawTeamTopHUD();
        }

        public override void checkIfWinLogic()
        {
            checkIfWinLogicTeams();
        }

        public override void drawScoreboard()
        {
            drawTeamScoreboard();
        }
    }
}
