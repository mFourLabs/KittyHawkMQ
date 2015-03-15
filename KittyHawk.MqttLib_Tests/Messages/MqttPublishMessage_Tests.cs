using System;
using System.Text;
using KittyHawk.MqttLib.Messages;
using KittyHawk.MqttLib.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KittyHawk.MqttLib_Tests.Messages
{
    [TestClass]
    public class MqttPublishMessage_Tests
    {
        [TestMethod]
        public void CanCreateFromMessageBuilder()
        {
            string topic = "Tests/MqttPublishMessage/Test1";
            var msgBuilder = new MqttPublishMessageBuilder()
            {
                QualityOfService = QualityOfService.AtMostOnce,
                TopicName = topic
            };

            Assert.AreEqual(MessageType.Publish, msgBuilder.MessageType);
            Assert.AreEqual(QualityOfService.AtMostOnce, msgBuilder.QualityOfService);
            Assert.AreEqual(0, msgBuilder.Payload.Length);

            var msg = msgBuilder.GetMessage() as MqttPublishMessage;

            Assert.IsNotNull(msg);
            Assert.AreEqual(MessageType.Publish, msg.MessageType);
            Assert.AreEqual(false, msg.Duplicate);
            Assert.AreEqual(QualityOfService.AtMostOnce, msg.QualityOfService);
            Assert.AreEqual(false, msg.Retain);
            Assert.AreEqual(topic, msg.TopicName);
            Assert.AreEqual(0, msg.Payload.Length);
        }

        [TestMethod]
        public void CanReadProtocolHeader()
        {
            string topic = "Tests/MqttPublishMessage/Test1";
            var msgBuilder = new MqttPublishMessageBuilder()
            {
                Duplicate = true,
                QualityOfService = QualityOfService.ExactlyOnce,
                Retain = true,
                TopicName = topic
            };

            var msg = msgBuilder.GetMessage() as MqttPublishMessage;

            Assert.AreEqual(msgBuilder.Duplicate, msg.Duplicate);
            Assert.AreEqual(msgBuilder.QualityOfService, msg.QualityOfService);
            Assert.AreEqual(msgBuilder.Retain, msg.Retain);
            Assert.AreEqual(msgBuilder.TopicName, msg.TopicName);
        }

        [TestMethod]
        public void CanReadMessageId()
        {
            string topic = "Tests/MqttPublishMessage/Test1";
            int id = 42;
            var msgBuilder = new MqttPublishMessageBuilder()
            {
                TopicName = topic,
                MessageId = id,
                QualityOfService = QualityOfService.AtLeastOnce

            };

            var msg = msgBuilder.GetMessage() as MqttPublishMessage;

            Assert.AreEqual(id, msg.MessageId);
        }

        [TestMethod]
        public void TopicNameValidationCatchesUnsetTopicName()
        {
            var msgBuilder = new MqttPublishMessageBuilder()
            {
                MessageId = 42
            };

            try
            {
                var msg = msgBuilder.GetMessage() as MqttPublishMessage;
            }
            catch (ArgumentException)
            {
                return;
            }
            catch (Exception)
            {
                Assert.Fail("Incorrect exception type thrown.");
            }

            Assert.Fail("No exception thrown for invalid TopicName");
        }

        [TestMethod]
        public void MessageIdValidationCatchesOutOfRangeMessageId()
        {
            string topic = "Tests/MqttPublishMessage/Test1";
            var msgBuilder = new MqttPublishMessageBuilder()
            {
                TopicName = topic
            };

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
            string topic = "Tests/MqttPublishMessage/Test1";
            var msgBuilder = new MqttPublishMessageBuilder()
            {
                TopicName = topic
            };

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

        [TestMethod]
        public void CanReadBinaryPayload()
        {
            string topic = "Tests/MqttPublishMessage/Test1";
            int id = 42;
            byte[] payload = new[] { (byte)0x0, (byte)0x1, (byte)0x2, (byte)0x3, (byte)0x4, (byte)0x5 };

            var msgBuilder = new MqttPublishMessageBuilder()
            {
                TopicName = topic,
                MessageId = id,
                Payload = payload
            };

            var msg = msgBuilder.GetMessage() as MqttPublishMessage;

            Assert.AreEqual(payload.Length, msg.Payload.Length);
            Assert.AreEqual(payload[0], msg.Payload[0]);
            Assert.AreEqual(payload[payload.Length - 1], msg.Payload[payload.Length-1]);
        }

        [TestMethod]
        public void CanReadStringPayload()
        {
            string str = "This is a test.";
            byte[] payload = Encoding.UTF8.GetBytes(str);

            var msgBuilder = new MqttPublishMessageBuilder()
            {
                TopicName = "Tests/MqttPublishMessage/Test1",
                MessageId = 42,
                Payload = payload
            };

            var msg = msgBuilder.GetMessage() as MqttPublishMessage;

            string result = msg.StringPayload;
            Assert.AreEqual(str, result);
        }

        [TestMethod]
        public void CanReadInt16Payload()
        {
            int i = 23321;
            int pos = 0;
            var payloadBytes = new byte[2];
            Frame.EncodeInt16(i, payloadBytes, ref pos);

            var msgBuilder = new MqttPublishMessageBuilder()
            {
                TopicName = "Tests/MqttPublishMessage/Test1",
                MessageId = 42,
                Payload = payloadBytes
            };

            var msg = msgBuilder.GetMessage() as MqttPublishMessage;

            int result = msg.Int16Payload;
            Assert.AreEqual(i, result);
        }
    }
}
