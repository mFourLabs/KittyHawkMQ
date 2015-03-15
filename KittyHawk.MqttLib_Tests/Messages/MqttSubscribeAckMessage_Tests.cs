using System;
using KittyHawk.MqttLib.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KittyHawk.MqttLib_Tests.Messages
{
    [TestClass]
    public class MqttSubscribeAckMessage_Tests
    {
        [TestMethod]
        public void CanCreateFromMessageBuilder()
        {
            int id = 42;
            var msgBuilder = new MqttSubscribeAckMessageBuilder()
            {
                MessageId = id
            };

            var msg = msgBuilder.GetMessage() as MqttSubscribeAckMessage;
            Assert.AreEqual(id, msg.MessageId);
        }

        [TestMethod]
        public void CanReadQoSLevels()
        {
            int id = 42;
            var msgBuilder = new MqttSubscribeAckMessageBuilder()
            {
                MessageId = id
            };

            msgBuilder.QoSLevels.Add(QualityOfService.AtLeastOnce);
            msgBuilder.QoSLevels.Add(QualityOfService.AtMostOnce);
            msgBuilder.QoSLevels.Add(QualityOfService.ExactlyOnce);
            Assert.AreEqual(3, msgBuilder.QoSLevels.Count);

            var msg = msgBuilder.GetMessage() as MqttSubscribeAckMessage;

            Assert.AreEqual(3, msg.QoSLevels.Count);
            Assert.AreEqual(QualityOfService.AtLeastOnce, msg.QoSLevels.GetAt(0));
            Assert.AreEqual(QualityOfService.AtMostOnce, msg.QoSLevels.GetAt(1));
            Assert.AreEqual(QualityOfService.ExactlyOnce, msg.QoSLevels.GetAt(2));
        }
    }
}
