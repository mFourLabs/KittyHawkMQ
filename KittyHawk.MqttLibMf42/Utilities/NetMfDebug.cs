
using System.Diagnostics;

namespace KittyHawk.MqttLib.Utilities
{
    internal static class NetMfDebug
    {
        [ConditionalAttribute("DEBUG")]
        public static void Print(string text)
        {
            Microsoft.SPOT.Debug.Print(text);
        }

        [Conditional("TRACE")]
        public static void Trace(string text)
        {
            Microsoft.SPOT.Debug.Print(text);
        }
    }
}
