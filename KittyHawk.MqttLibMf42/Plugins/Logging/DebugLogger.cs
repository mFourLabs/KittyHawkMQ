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
                LogMessageHandlerDebug(module, level, message);
            }
        }

        public void LogException(string module, LogLevel level, string message, Exception exception)
        {
            if (level <= ActiveLogLevel)
            {
                LogExceptionHandlerDebug(module, level, message, exception);
            }
        }

        [Conditional("DEBUG")]
        private void LogMessageHandlerDebug(string module, LogLevel level, string message)
        {
            Microsoft.SPOT.Debug.Print(DateTime.Now + "|" + module + "|" + level + "|" + message);
        }

        [Conditional("DEBUG")]
        private void LogExceptionHandlerDebug(string module, LogLevel level, string message, Exception exception)
        {
            Microsoft.SPOT.Debug.Print(DateTime.Now + "|" + module + "|" + level + "|" + message + " " + exception);
        }
    }
}
