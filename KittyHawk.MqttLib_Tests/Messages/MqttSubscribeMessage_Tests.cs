using System;
using KittyHawk.MqttLib.Collections;
using KittyHawk.MqttLib.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KittyHawk.MqttLib_Tests.Messages
{
    [TestClass]
    public class MqttSubscribeMessage_Tests
    {
        [TestMethod]
        public void CanCreateFromMessageBuilder()
        {
            var msgBuilder = new MqttSubscribeMessageBuilder()
            {
                MessageId = 42
            };

            Assert.AreEqual(MessageType.Subscribe, msgBuilder.MessageType);
            Assert.AreEqual(QualityOfService.AtLeastOnce, msgBuilder.QualityOfService);
            Assert.AreEqual(0, msgBuilder.Subscriptions.Count);

            var msg = msgBuilder.GetMessage() as MqttSubscribeMessage;

            Assert.IsNotNull(msg);
            Assert.AreEqual(MessageType.Subscribe, msg.MessageType);
            Assert.AreEqual(false, msg.Duplicate);
            Assert.AreEqual(QualityOfService.AtLeastOnce, msg.QualityOfService);
            Assert.AreEqual(false, msg.Retain);
            Assert.AreEqual(42, msg.MessageId);
            Assert.AreEqual(0, msg.Subscriptions.Count);
        }

        [TestMethod]
        public void CanReadProtocolHeader()
        {
            var msgBuilder = new MqttSubscribeMessageBuilder()
            {
                Duplicate = true,
                QualityOfService = QualityOfService.ExactlyOnce,
                Retain = true,
                MessageId = 42
            };

            var msg = msgBuilder.GetMessage() as MqttSubscribeMessage;

            Assert.AreEqual(msgBuilder.Duplicate, msg.Duplicate);
            Assert.AreEqual(msgBuilder.QualityOfService, msg.QualityOfService);
            Assert.AreEqual(msgBuilder.Retain, msg.Retain);
            Assert.AreEqual(42, msg.MessageId);
        }

        [TestMethod]
        public void CanReadSubscriptions()
        {
            var msgBuilder = new MqttSubscribeMessageBuilder()
            {
                Duplicate = true,
                QualityOfService = QualityOfService.ExactlyOnce,
                Retain = true,
                MessageId = 42
            };

            msgBuilder.Subscriptions.Add(new SubscriptionItem()
            {
                QualityOfService = QualityOfService.AtLeastOnce,
                TopicName = "a/b0"
            });
            msgBuilder.Subscriptions.Add(new SubscriptionItem()
            {
                QualityOfService = QualityOfService.AtLeastOnce,
                TopicName = "a/b1"
            });
            msgBuilder.Subscriptions.Add(new SubscriptionItem()
            {
                QualityOfService = QualityOfService.AtLeastOnce,
                TopicName = "a/b2"
            });

            var msg = msgBuilder.GetMessage() as MqttSubscribeMessage;

            Assert.AreEqual(3, msg.Subscriptions.Count);

            var item = msg.Subscriptions.GetAt(0);
            Assert.AreEqual(QualityOfService.AtLeastOnce, item.QualityOfService);
            Assert.AreEqual("a/b0", item.TopicName);

            item = msg.Subscriptions.GetAt(1);
            Assert.AreEqual(QualityOfService.AtLeastOnce, item.QualityOfService);
            Assert.AreEqual("a/b1", item.TopicName);

            item = msg.Subscriptions.GetAt(2);
            Assert.AreEqual(QualityOfService.AtLeastOnce, item.QualityOfService);
            Assert.AreEqual("a/b2", item.TopicName);
        }

        [TestMethod]
        public void MessageIdValidationCatchesOutOfRangeMessageId()
        {
            var msgBuilder = new MqttSubscribeMessageBuilder();

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
            var msgBuilder = new MqttSubscribeMessageBuilder();

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
