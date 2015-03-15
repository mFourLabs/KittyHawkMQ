
using KittyHawk.MqttLib.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KittyHawk.MqttLib_Tests.Messages
{
    [TestClass]
    public class MqttPublishActMessage_Tests
    {
        [TestMethod]
        public void CanCreateFromMessageBuilder()
        {
            int id = 42;
            var msgBuilder = new MqttPublishAckMessageBuilder()
            {
                MessageId = id
            };

            var msg = msgBuilder.GetMessage() as MqttPublishAckMessage;
            Assert.AreEqual(id, msg.MessageId);
        }
    }
}
