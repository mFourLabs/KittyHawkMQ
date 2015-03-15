using System;
using System.Diagnostics;

namespace KittyHawk.MqttLib.Plugins.Logging
{
    internal class DebugLogger : ILogger
    {
        public DebugLogger()
        {
        }

        public string Name
        {
            get { return "DebugLogger"; }
        }

        public LogLevel ActiveLogLevel { get; set; }

        public void LogMessage(string module, LogLevel level, string message)
        {
            if (level <= ActiveLogLevel)
            {
                string levelStr = level.ToString("G");
                Debug.WriteLine(string.Format("{0:s}|{1,14}|{2,14}|{3}", DateTime.Now, module.PadRight(14), levelStr.PadRight(14), message));
            }
        }

        public void LogException(string module, LogLevel level, string message, Exception exception)
        {
            if (level <= ActiveLogLevel)
            {
                string levelStr = level.ToString("G");
                Debug.WriteLine(string.Format("{0:s}|{1,14}|{2,14}|{3} Exception={4}", DateTime.Now, module.PadRight(14), levelStr.PadRight(14), message, exception.Message));
            }
        }
    }
}
