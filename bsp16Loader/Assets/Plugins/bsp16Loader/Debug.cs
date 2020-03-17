using RuntimeDebugDraw;

namespace bsp
{
    partial class BSP30Map
    {
        private class Debug
        {
            public static bool debug = false;
            [Conditional(Tag.logging)]
            public static void Log2(object text)
            {
                if (debug)
                    UnityEngine.Debug.Log(text);
            }
            public static void LogError(object P0)
            {
                //if (debug)
                UnityEngine.Debug.LogError(P0);
                //else
                //    UnityEngine.Debug.Log(P0);

            }
            [Conditional(Tag.logging)]
            public static void Log(object text)
            {
                //if (debug)
                UnityEngine.Debug.Log(text);
            }
        }
    }
}
