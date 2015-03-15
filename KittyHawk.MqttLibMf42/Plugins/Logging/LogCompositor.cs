using System;

namespace KittyHawk.MqttLib.Plugins.Logging
{
    internal class LogCompositor : ILogger
    {
        private readonly ILogger _theLogger;

        internal LogLevel AllLevels = LogLevel.Verbose;

        public LogCompositor()
        {
            ActiveLogLevel = AllLevels;
            _theLogger = new DebugLogger();
            _theLogger.ActiveLogLevel = ActiveLogLevel;
        }

        public string Name
        {
            get { return "LogManager"; }
        }

        public LogLevel ActiveLogLevel { get; set; }

        public void LogMessage(string module, LogLevel level, string message)
        {
            if (level <= ActiveLogLevel)
            {
                _theLogger.LogMessage(module, level, message);
            }
        }

        public void LogException(string module, LogLevel level, string message, Exception exception)
        {
            if (level <= ActiveLogLevel)
            {
                _theLogger.LogException(module, level, message, exception);
            }
        }
    }
}
