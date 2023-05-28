using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public enum RenderEffectType
    {
        Hit,
        Flash,
        StockedCharge,
        Invisible,
        InvisibleFlash,
        BlueShadow,
        RedShadow,
        Trail,
        GreenShadow,
        StockedSaber,
        BoomerKTrail,
        SpeedDevilTrail,
        StealthModeBlue,
        StealthModeRed,
        Shake,
    }

    public class RenderEffect
    {
        public RenderEffectType type;
        public float time;
        public float flashTime;
        public RenderEffect(RenderEffectType type, float flashTime = 0, float time = float.MaxValue)
        {
            this.type = type;
            this.flashTime = flashTime;
            this.time = time;
        }

        public bool isFlashing()
        {
            return time < flashTime; 
        }
    }
}
