using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using KittyHawk.MqttLib.Plugins.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KittyHawk.MqttLib_Tests.Plugins.Logging
{
    [TestClass]
    public class LoggingManager_Tests
    {
        [TestMethod]
        public void LoggingToManagerWithNoLoggersDoesNotFail()
        {
            var mgr = new LogCompositor();
            mgr.LogMessage("", LogLevel.Verbose, "Log message");
            mgr.LogException("", LogLevel.Error, "Log exception", new Exception());
        }

        [TestMethod]
        public void LoggingManagerCallsOnLogCallbacks()
        {
            var mgr = new LogCompositor();
            mgr.AddLoggingProvider(typeof (PluginLogger1));

            Assert.AreEqual(1, mgr.Loggers.Count);

            mgr.LogMessage("", LogLevel.Verbose, "Log message");
            mgr.LogException("", LogLevel.Error, "Log exception", new Exception());

            var plugin = (PluginLogger1)mgr.Loggers.First();

            Assert.AreEqual(1, plugin.LogMessageCallCount);
            Assert.AreEqual(1, plugin.LogExceptionCallCount);
        }

        [TestMethod]
        public void LoggingManagerThrowsExceptionForNonILoggerTypes()
        {
            var mgr = new LogCompositor();

            try
            {
                mgr.AddLoggingProvider(typeof (PluginLogger2));
                Assert.Fail("No exception thrown with incorrect type.");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception)
            {
                Assert.Fail("Wrong exception type thrown with incorrect type.");
            }
        }
    }

    internal class PluginLogger1 : ILogger
    {
        public int LogMessageCallCount { get; set; }
        public int LogExceptionCallCount { get; set; }

        public void PlugingLogger1()
        {
        }

        public string Name
        {
            get { return "DebugLogger"; }
        }

        public LogLevel ActiveLogLevel { get; set; }

        public void LogMessage(string module, LogLevel level, string message)
        {
            LogMessageCallCount++;
            Debug.WriteLine("Called LogMessage.");
        }

        public void LogException(string module, LogLevel level, string message, Exception exception)
        {
            LogExceptionCallCount++;
            Debug.WriteLine("Called LogException.");
        }
    }

    internal class PluginLogger2
    {
        public int LogMessageCallCount { get; set; }
        public int LogExceptionCallCount { get; set; }

        public void PlugingLogger1()
        {
        }

        public void LogMessage(LogLevel level, string message)
        {
            LogMessageCallCount++;
            Debug.WriteLine("Called LogMessage.");
        }

        public void LogException(LogLevel level, Exception exception)
        {
            LogExceptionCallCount++;
            Debug.WriteLine("Called LogException.");
        }
    }
}
