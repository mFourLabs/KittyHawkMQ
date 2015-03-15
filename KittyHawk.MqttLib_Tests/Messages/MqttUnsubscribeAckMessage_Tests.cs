using System;
using KittyHawk.MqttLib.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KittyHawk.MqttLib_Tests.Messages
{
    [TestClass]
    public class MqttUnsubscribeAckMessage_Tests
    {
        [TestMethod]
        public void CanCreateFromMessageBuilder()
        {
            int id = 42;
            var msgBuilder = new MqttUnsubscribeAckMessageBuilder()
            {
                MessageId = id
            };

            var msg = msgBuilder.GetMessage() as MqttUnsubscribeAckMessage;
            Assert.AreEqual(id, msg.MessageId);
        }
    }
}
