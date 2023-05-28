using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline
{
    public class MathF
    {
        public const float PI = 3.14159274f;

        public static float Abs(float num)
        {
            return Math.Abs(num);
        }

        public static float Cos(float num)
        {
            return (float)Math.Cos(num);
        }

        public static float Sin(float num)
        {
            return (float)Math.Sin(num);
        }

        public static int Round(float num)
        {
            return (int)Math.Round(num);
        }

        public static int Sign(float num)
        {
            return Math.Sign(num);
        }

        public static int Clamp(int val, int min, int max)
        {
            if (val < min) val = min;
            if (val > max) val = max;
            return val;
        }

        public static float Clamp(float val, float min, float max)
        {
            if (val < min) val = min;
            if (val > max) val = max;
            return val;
        }

        public static float Atan2(float y, float x)
        {
            return (float)Math.Atan2(y, x);
        }

        public static int Ceiling(float num)
        {
            return (int)Math.Ceiling(num);
        }

        public static int Floor(float v)
        {
            return (int)Math.Floor(v);
        }

        public static float Pow(float v1, int v2)
        {
            return (float)Math.Pow(v1, v2);
        }

        public static float Sqrt(float v)
        {
            return (float)Math.Sqrt(v);
        }
    }
}
