using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace KittyHawk.MqttLib.Plugins.Logging
{
    internal class LogCompositor : ILogger
    {
        internal readonly IList<ILogger> Loggers = new List<ILogger>();

        internal LogLevel AllLevels = LogLevel.Verbose;

        public LogCompositor()
        {
            ActiveLogLevel = AllLevels;
        }

        public string Name
        {
            get { return "LogManager"; }
        }

        public void AddLoggingProvider(Type type)
        {
            var interfaces = type.GetTypeInfo().ImplementedInterfaces;
            if (interfaces.Count(i => i == typeof (ILogger)) > 0)
            {
                var logger = Activator.CreateInstance(type) as ILogger;
                logger.ActiveLogLevel = ActiveLogLevel;
                Loggers.Add(logger);
            }
            else
            {
                throw new ArgumentException(string.Format("Type does not implement the ILogger interface: {0}", type));
            }
        }

        public void AddLoggingProvider(ILogger logger)
        {
            Loggers.Add(logger);
        }

        #region ILogger interface

        /// <summary>
        /// Global log level setting, restricts all loggers
        /// </summary>
        public LogLevel ActiveLogLevel { get; set; }

        public async void LogMessage(string module, LogLevel level, string message)
        {
            if (level <= ActiveLogLevel)
            {
                await Task.WhenAll(Loggers.Select(provider => Task.Factory.StartNew(() => provider.LogMessage(module, level, message))));
            }
        }

        public async void LogException(string module, LogLevel level, string message, Exception exception)
        {
            if (level <= ActiveLogLevel)
            {
                await Task.WhenAll(Loggers.Select(provider => Task.Factory.StartNew(() => provider.LogException(module, level, message, exception))));
            }
        }

        #endregion
    }
}
