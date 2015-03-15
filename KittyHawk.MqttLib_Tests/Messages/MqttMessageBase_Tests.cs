using KittyHawk.MqttLib.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KittyHawk.MqttLib_Tests.Messages
{
    [TestClass]
    public class MqttMessageBase_Tests
    {
        [TestMethod]
        public void MesssageHeadersCanBeRead()
        {
            var msg1 = new MqttMessageBase(MessageType.Connect)
            {
                Duplicate = false,
                QualityOfService = QualityOfService.AtMostOnce,
                Retain = false
            };
            Assert.AreEqual(MessageType.Connect, msg1.MessageType);
            Assert.AreEqual(QualityOfService.AtMostOnce, msg1.QualityOfService);
            Assert.AreEqual(false, msg1.Duplicate);
            Assert.AreEqual(false, msg1.Retain);

            var msg2 = new MqttMessageBase(MessageType.ConnAck)
            {
                Duplicate = true,
                QualityOfService = QualityOfService.ExactlyOnce,
                Retain = true
            };
            Assert.AreEqual(MessageType.ConnAck, msg2.MessageType);
            Assert.AreEqual(QualityOfService.ExactlyOnce, msg2.QualityOfService);
            Assert.AreEqual(true, msg2.Duplicate);
            Assert.AreEqual(true, msg2.Retain);
        }
    }
}
