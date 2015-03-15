using KittyHawk.MqttLib.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KittyHawk.MqttLib_Tests.Messages
{
    [TestClass]
    public class MqttConnectAckMessage_Tests
    {
        [TestMethod]
        public void CanCreateFromMessageBuilder()
        {
            var msgBuilder = new MqttConnectAckMessageBuilder()
            {
                ConnectReturnCode = ConnectReturnCode.Accepted
            };

            var msg = msgBuilder.GetMessage() as MqttConnectAckMessage;
            Assert.AreEqual(ConnectReturnCode.Accepted, msg.ConnectReturnCode);
        }
    }
}
