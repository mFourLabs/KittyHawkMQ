using System.Threading;
using KittyHawk.MqttLib;
using KittyHawk.MqttLib.Collections;
using KittyHawk.MqttLib.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KittyHawk.MqttLib_Tests.Collections
{
    [TestClass]
    public class MesageStore_Tests
    {
        [TestMethod]
        public void RemoveItemReturnsEventData()
        {
            MqttProtocolInformation.Settings.NetworkTimeout = 4; // 4 seconds
            var store = new MessageStore();
            var eventData = "Test data";
            string connectionKey = "123";
            var msg = new MqttPublishMessageBuilder
            {
                MessageId = 42,
                QualityOfService = QualityOfService.AtLeastOnce,
                TopicName = "a/b/c"
            };

            store.Add(msg.GetMessage(), eventData, connectionKey);
            Thread.Sleep(1000);
            object testData = store.Remove(MessageType.PubAck, 42, connectionKey);
            Assert.AreSame(eventData, testData);
        }

        [TestMethod]
        public void AddRemoveItemDoesNotFireTimeoutEvent()
        {
            MqttProtocolInformation.Settings.NetworkTimeout = 4; // 4 seconds
            var store = new MessageStore();
            var are = new AutoResetEvent(false);
            var eventData = "Test data";
            string connectionKey = "123";
            var msg = new MqttPublishMessageBuilder
            {
                MessageId = 42,
                QualityOfService = QualityOfService.AtLeastOnce,
                TopicName = "a/b/c"
            };

            store.MessageTimeout += (sender, args) =>
            {
                Assert.Fail("MessageTimeout event fired but should not have.");
                are.Set();
            };

            store.Add(msg.GetMessage(), eventData, connectionKey);
            Thread.Sleep(1000);
            store.Remove(MessageType.PubAck, 42, connectionKey);
            if (!are.WaitOne(MqttProtocolInformation.Settings.NetworkTimeout*2))
            {
                Assert.AreEqual(0, store.Count);
            }
        }

        [TestMethod]
        public void AddItemWithoutRemovingItFiresTimeoutEvent()
        {
            MqttProtocolInformation.Settings.NetworkTimeout = 4; // 4 seconds
            var store = new MessageStore();
            var are = new AutoResetEvent(false);
            var eventData = "Test data";
            string connectionKey = "123";
            var msgBldr = new MqttPublishMessageBuilder
            {
                MessageId = 42,
                QualityOfService = QualityOfService.AtLeastOnce,
                TopicName = "a/b/c"
            };
            var msg = msgBldr.GetMessage();

            store.MessageTimeout += (sender, args) =>
            {
                Assert.AreEqual(msg, args.FailedMessage);
                Assert.AreEqual(eventData, args.EventData);
                are.Set();
            };

            store.Add(msg, eventData, connectionKey);
            if (!are.WaitOne(MqttProtocolInformation.Settings.NetworkTimeout*2*1000))
            {
                Assert.Fail("Timeout event did not fire.");
            }
        }
    }
}
