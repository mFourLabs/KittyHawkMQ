using System;
using KittyHawk.MqttLib.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KittyHawk.MqttLib_Tests.Messages
{
#if SKIP
    [TestClass]
    public class MqttReceivedMessageBuilder_Tests
    {
        [TestMethod]
        public void CanCreateMessageFromValidFrame()
        {
            var msgBuilder = new MqttReceivedMessageBuilder();
            msgBuilder.Append(0x28);
            msgBuilder.Append(0x02);
            msgBuilder.Append(0xFF);
            msgBuilder.Append(0xFF);

            Assert.IsTrue(msgBuilder.FrameComplete);

            var msg = msgBuilder.GetMessage();
            Assert.AreEqual(typeof(MqttConnectAckMessage), msg.GetType());
            Assert.AreEqual(MessageType.ConnAck, msg.MessageType);
            Assert.AreEqual(true, msg.Duplicate);
            Assert.AreEqual(QualityOfService.AtMostOnce, msg.QualityOfService);
            Assert.AreEqual(false, msg.Retain);
            Assert.IsNotNull(msg.Buffer);
            Assert.IsTrue(msg.Buffer.Length == 3);
        }

        [TestMethod]
        public void CannotCreateFromIncompleteFrame()
        {
            var msgBuilder = new MqttReceivedMessageBuilder();
            msgBuilder.Append(0x28);
            msgBuilder.Append(0x02);
            msgBuilder.Append(0xFF);

            Assert.IsFalse(msgBuilder.FrameComplete);

            MqttMessageBase msg = null;
            try
            {
                msg = msgBuilder.GetMessage();
            }
            catch (MessageFrameException)
            {
            }
            catch (Exception)
            {
                Assert.Fail("Invalid Exception type.");
            }

            Assert.IsNull(msg, "GetMessage succeeded but should have failed.");
        }

        [TestMethod]
        public void CannotAddBytesToCompleteFrame()
        {
            var msgBuilder = new MqttReceivedMessageBuilder();
            msgBuilder.Append(0x28);
            msgBuilder.Append(0x02);
            msgBuilder.Append(0xFF);
            msgBuilder.Append(0xFF);

            try
            {
                msgBuilder.Append(0xFF);
            }
            catch (MessageFrameException)
            {
                return;
            }
            catch (Exception)
            {
                Assert.Fail("Invalid Exception type.");
            }

            Assert.Fail("No exception thrown.");
        }
    }
#endif
}
