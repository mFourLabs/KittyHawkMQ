
using System;

namespace KittyHawk.MqttLib.Plugins.Logging
{
    [Flags]
    public enum LogLevel
    {
        None        = 0x0000,
        Critical    = 0x0001,
        Error       = 0x0002,
        Warning     = 0x0004,
        Information = 0x0008,
        Verbose     = 0x0010,
    }

    public interface ILogger
    {
        string Name { get; }
        LogLevel ActiveLogLevel { get; set; }
        void LogMessage(string module, LogLevel level, string message);
        void LogException(string module, LogLevel level, string message, Exception exception);
    }
}
