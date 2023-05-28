using System;

namespace BuildTools
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            SpriteOptimizer.DoWork();
            //PortCustomMap.Port("Forest_3", "forest3");
#else
            try
            {
                SpriteOptimizer.DoWork();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
                throw;
            }
#endif
        }
    }
}
