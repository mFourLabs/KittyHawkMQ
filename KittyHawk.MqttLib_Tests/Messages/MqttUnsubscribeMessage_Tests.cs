using System;
using KittyHawk.MqttLib.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KittyHawk.MqttLib_Tests.Messages
{
    [TestClass]
    public class MqttUnsubscribeMessage_Tests
    {
        [TestMethod]
        public void CanCreateFromMessageBuilder()
        {
            var msgBuilder = new MqttUnsubscribeMessageBuilder()
            {
                MessageId = 42
            };

            Assert.AreEqual(MessageType.Unsubscribe, msgBuilder.MessageType);
            Assert.AreEqual(QualityOfService.AtLeastOnce, msgBuilder.QualityOfService);
            Assert.AreEqual(0, msgBuilder.TopicNames.Length);

            var msg = msgBuilder.GetMessage() as MqttUnsubscribeMessage;

            Assert.IsNotNull(msg);
            Assert.AreEqual(MessageType.Unsubscribe, msg.MessageType);
            Assert.AreEqual(false, msg.Duplicate);
            Assert.AreEqual(QualityOfService.AtLeastOnce, msg.QualityOfService);
            Assert.AreEqual(false, msg.Retain);
            Assert.AreEqual(42, msg.MessageId);
            Assert.AreEqual(0, msg.TopicNames.Length);
        }

        [TestMethod]
        public void CanReadProtocolHeader()
        {
            var msgBuilder = new MqttUnsubscribeMessageBuilder()
            {
                Duplicate = true,
                QualityOfService = QualityOfService.ExactlyOnce,
                Retain = true,
                MessageId = 42
            };

            var msg = msgBuilder.GetMessage() as MqttUnsubscribeMessage;

            Assert.AreEqual(msgBuilder.Duplicate, msg.Duplicate);
            Assert.AreEqual(msgBuilder.QualityOfService, msg.QualityOfService);
            Assert.AreEqual(msgBuilder.Retain, msg.Retain);
            Assert.AreEqual(42, msg.MessageId);
        }

        [TestMethod]
        public void CanReadTopicNames()
        {
            var msgBuilder = new MqttUnsubscribeMessageBuilder()
            {
                Duplicate = true,
                QualityOfService = QualityOfService.ExactlyOnce,
                Retain = true,
                MessageId = 42
            };

            var topics = new string[]
            {
                "a/b0",
                "a/b1",
                "a/b2"
            };

            msgBuilder.TopicNames = topics;

            var msg = msgBuilder.GetMessage() as MqttUnsubscribeMessage;

            Assert.AreEqual(3, msg.TopicNames.Length);
            Assert.AreEqual("a/b0", msg.TopicNames[0]);
            Assert.AreEqual("a/b1", msg.TopicNames[1]);
            Assert.AreEqual("a/b2", msg.TopicNames[2]);
        }

        [TestMethod]
        public void MessageIdValidationCatchesOutOfRangeMessageId()
        {
            var msgBuilder = new MqttUnsubscribeMessageBuilder();

            try
            {
                msgBuilder.MessageId = 0x1FFFF;
            }
            catch (ArgumentException)
            {
                return;
            }
            catch (Exception)
            {
                Assert.Fail("Incorrect exception type thrown.");
            }

            Assert.Fail("No exception thrown for out of range MessageID");
        }

        [TestMethod]
        public void MessageIdValidationCatchesMessageIdEqualZero()
        {
            var msgBuilder = new MqttUnsubscribeMessageBuilder();

            try
            {
                msgBuilder.MessageId = 0;
            }
            catch (ArgumentException)
            {
                return;
            }
            catch (Exception)
            {
                Assert.Fail("Incorrect exception type thrown.");
            }

            Assert.Fail("No exception thrown for out of range MessageID");
        }
    }
}
