using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class TeamDeathMatch : GameMode
    {
        public TeamDeathMatch(Level level, int playingTo, int? timeLimit) : base(level, timeLimit)
        {
            this.playingTo = playingTo;
            isTeamMode = true;
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
