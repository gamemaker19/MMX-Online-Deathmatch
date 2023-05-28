using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{

    public class VictoryPoint : Actor
    {
        public VictoryPoint(Point pos) :
            base("victory_point", pos, null, true, false)
        {
            useGravity = false;
        }

        public override void update()
        {
            base.update();
        }

        public override void onStart()
        {
            var hit = Global.level.raycast(pos.addxy(0, -10), pos.addxy(0, 60), new List<Type>() { typeof(Wall), typeof(Ladder) });
            if (hit?.hitData?.hitPoint != null)
            {
                pos = (Point)hit.hitData.hitPoint;
                changePos(pos);
            }
        }

        public override void onCollision(CollideData other)
        {
            base.onCollision(other);
            if (!Global.isHost) return;
            if (Global.level.gameMode.isOver) return;
            if (other.otherCollider?.flag == (int)HitboxFlag.Hitbox) return;

            if (other.gameObject is Character)
            {
                var chr = other.gameObject as Character;
                onWin(chr.player);
            }
            else if (other.gameObject is RideArmor)
            {
                var rideArmor = other.gameObject as RideArmor;
                if (rideArmor.character != null && !rideArmor.character.isVileMK5)
                {
                    onWin(rideArmor.character.player);
                }
            }
            else if (other.gameObject is RideChaser)
            {
                var rideChaser = other.gameObject as RideChaser;
                if (rideChaser.character != null)
                {
                    onWin(rideChaser.character.player);
                }
            }
        }

        bool once;
        public void onWin(Player player)
        {
            if (!once) once = true;
            else return;

            string winMessage = "You won!";
            string loseMessage = "You lost!";
            string loseMessage2 = player.name + " wins";

            var matchOverResponse = new RPCMatchOverResponse()
            {
                winningAlliances = new HashSet<int>() { player.alliance },
                winMessage = winMessage,
                loseMessage = loseMessage,
                loseMessage2 = loseMessage2
            };

            Global.level.gameMode.matchOverRpc(matchOverResponse);
            Global.serverClient?.rpc(RPC.matchOver, JsonConvert.SerializeObject(matchOverResponse));
        }
    }
}
