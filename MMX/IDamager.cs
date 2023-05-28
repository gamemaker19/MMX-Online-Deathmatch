using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public interface IDamagable
    {
        void applyDamage(Player owner, int? weaponIndex, float damage, int? projId);
        Dictionary<string, float> projectileCooldown { get; set; }
        bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId);
        bool isInvincible(Player attacker, int? projId);
        bool canBeHealed(int healerAlliance);
        void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false);
    }

    public class DamageText
    {
        public string text;
        public float time;
        public Point pos;
        public Point offset;
        public bool isHeal;
        public DamageText(string text, float time, Point pos, Point offset, bool isHeal)
        {
            this.text = text;
            this.time = time;
            this.pos = pos;
            this.offset = offset;
            this.isHeal = isHeal;
        }
    }
}
