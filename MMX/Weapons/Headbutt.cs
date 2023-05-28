using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class Headbutt : Weapon
    {
        public Headbutt(Player player) : base()
        {
            damager = new Damager(player, 2, 13, 0.5f);
            index = (int)WeaponIds.Headbutt;
            killFeedIndex = 64;
        }
    }
}
