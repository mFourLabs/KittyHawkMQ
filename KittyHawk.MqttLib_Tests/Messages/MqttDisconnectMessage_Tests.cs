using System;
using KittyHawk.MqttLib.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KittyHawk.MqttLib_Tests.Messages
{
    [TestClass]
    public class MqttDisconnectMessage_Tests
    {
        [TestMethod]
        public void CanCreateFromMessageBuilder()
        {
            var msgBuilder = new MqttDisconnectMessageBuilder();

            Assert.AreEqual(MessageType.Disconnect, msgBuilder.MessageType);
            var msg = msgBuilder.GetMessage();
            Assert.AreEqual(typeof(MqttDisconnectMessage), msg.GetType());
            Assert.AreEqual(MessageType.Disconnect, msg.MessageType);
        }
    }
}
