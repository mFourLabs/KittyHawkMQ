using System.Collections.Generic;
using System.Threading;
using KittyHawk.MqttLib;
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Messages;
using KittyHawk.MqttLib.Net;
using KittyHawk.MqttLib.Plugins.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace KittyHawk.MqttLib_Tests.Net
{
    [TestClass]
    public class MqttProtocol_Tests
    {
        [TestMethod]
        public void ConnectAsyncCallsConnectCompleteEventWithEventData()
        {
            var are = new AutoResetEvent(false);
            var ip = "1.1.1.1";
            var port = 1883;
            var eventData = "Test data";
            var moqSocket = new MoqSocket();
            var client = new MqttClientProtocol(new LogCompositor(), moqSocket);
            var bldr = new MqttConnectMessageBuilder
            {
                ClientId = "UnitTest"
            };

            MqttProtocolInformation.Settings.KeepAliveTime = 5*60;

            client.ConnectComplete += (sender, args) =>
            {
                Assert.AreSame(eventData, args.EventData);
                Assert.IsTrue(moqSocket.SentMessages.Contains(MessageType.Connect));
                are.Set();
            };

            client.ConnectAsync(bldr, ip, port, SocketEncryption.None, eventData);

            if (!are.WaitOne(5000))
            {
                Assert.Fail("ConnectComplete event not fired.");
            }
        }

        [TestMethod]
        public void DisconnectAsyncCallsDisconnectCompleteEventWithEventData()
        {
            var are = new AutoResetEvent(false);
            var eventData = "Test data";
            var moqSocket = new MoqSocket();
            var client = new MqttClientProtocol(new LogCompositor(), moqSocket);

            client.SendMessageComplete += (sender, args) =>
            {
                Assert.AreSame(eventData, args.EventData);
                Assert.IsTrue(moqSocket.SentMessages.Contains(MessageType.Disconnect));
                are.Set();
            };

            client.DisconnectAsync(eventData, null);

            if (!are.WaitOne(5000))
            {
                Assert.Fail("DisconnectComplete event not fired.");
            }
        }

        [TestMethod]
        public void ConnectDisconnectConnectSequenceDoesNotThrow()
        {
            var areConnect = new AutoResetEvent(false);
            var areDisconnect = new AutoResetEvent(false);
            var ip = "1.1.1.1";
            var port = 1883;
            var eventData = "Test data";
            var moqSocket = new MoqSocket();
            var client = new MqttClientProtocol(new LogCompositor(), moqSocket);
            var bldr = new MqttConnectMessageBuilder
            {
                ClientId = "UnitTest"
            };

            client.ConnectComplete += (sender, args) =>
            {
                Assert.AreSame(eventData, args.EventData);
                Assert.IsTrue(moqSocket.SentMessages.Contains(MessageType.Connect));
                areConnect.Set();
            };

            client.SendMessageComplete += (sender, args) =>
            {
                Assert.AreSame(eventData, args.EventData);
                Assert.IsTrue(moqSocket.SentMessages.Contains(MessageType.Disconnect));
                areDisconnect.Set();
            };

            client.ConnectAsync(bldr, ip, port, SocketEncryption.None, eventData);
            if (!areConnect.WaitOne(5000))
            {
                Assert.Fail("First ConnectComplete event did not fire.");
            }

            client.DisconnectAsync(eventData, null);
            if (!areDisconnect.WaitOne(5000))
            {
                Assert.Fail("First DisconnectComplete event did not fire.");
            }

            client.ConnectAsync(bldr, ip, port, SocketEncryption.None, eventData);
            if (!areConnect.WaitOne(5000))
            {
                Assert.Fail("Second ConnectComplete event did not fire.");
            }
        }

        [TestMethod]
        public void SubscribeCallsSendMessageCompleteEventWithEventData()
        {
            var are = new AutoResetEvent(false);
            var eventData = "Test data";
            var moqSocket = new MoqSocket();
            var client = new MqttClientProtocol(new LogCompositor(), moqSocket);
            var bldr = new MqttSubscribeMessageBuilder
            {
                MessageId = 42,
            };

            client.SubscribeComplete += (sender, args) =>
            {
                var msg = args.Message as MqttSubscribeAckMessage;
                Assert.IsNotNull(msg);
                Assert.AreEqual(bldr.MessageId, msg.MessageId);
                Assert.AreSame(eventData, args.EventData);
                Assert.IsTrue(moqSocket.SentMessages.Contains(MessageType.Subscribe));
                are.Set();
            };

            client.SendMessageAsync(bldr, eventData, null);

            if (!are.WaitOne(5000))
            {
                Assert.Fail("SendMessageComplete event not fired for Subscribe.");
            }
        }

        [TestMethod]
        public void SubscribeCallsNetworkErrorEventWhenNoResponseReceived()
        {
            var are = new AutoResetEvent(false);
            var eventData = "Test data";
            var moqSocket = new MoqSocket
            {
                DoNotRespond = true
            };

            MqttProtocolInformation.Settings.NetworkTimeout = 5; // 5 seconds
            var client = new MqttClientProtocol(new LogCompositor(), moqSocket);
            var bldr = new MqttSubscribeMessageBuilder
            {
                MessageId = 42,
            };

            client.SendMessageComplete += (sender, args) => Assert.Fail();
            client.NetworkError += (sender, args) =>
            {
                Assert.AreEqual(bldr.MessageType, args.Message.MessageType);
                are.Set();
            };

            client.SendMessageAsync(bldr, eventData, null);

            if (!are.WaitOne(MqttProtocolInformation.Settings.NetworkTimeout * 1000 + 5000))
            {
                Assert.Fail("NetworkError event not fired for Subscribe.");
            }
        }

        [TestMethod]
        public void UnsubscribeCallsSendMessageCompleteEventWithEventData()
        {
            var are = new AutoResetEvent(false);
            var eventData = "Test data";
            var moqSocket = new MoqSocket();
            var client = new MqttClientProtocol(new LogCompositor(), moqSocket);
            var bldr = new MqttUnsubscribeMessageBuilder
            {
                MessageId = 42,
            };

            client.SendMessageComplete += (sender, args) =>
            {
                var msg = args.Message as IMqttIdMessage;
                Assert.IsNotNull(msg);
                Assert.AreEqual(bldr.MessageId, msg.MessageId);
                Assert.AreSame(eventData, args.EventData);
                Assert.IsTrue(moqSocket.SentMessages.Contains(MessageType.Unsubscribe));
                are.Set();
            };

            client.SendMessageAsync(bldr, eventData, null);

            if (!are.WaitOne(5000))
            {
                Assert.Fail("SendMessageComplete event not fired for Unsubscribe.");
            }
        }

        [TestMethod]
        public void PingCallsSendMessageCompleteEventWithEventData()
        {
            var are = new AutoResetEvent(false);
            var eventData = "Test data";
            var moqSocket = new MoqSocket();
            var client = new MqttClientProtocol(new LogCompositor(), moqSocket);
            var bldr = new MqttPingRequestMessageBuilder();

            client.SendMessageComplete += (sender, args) =>
            {
                Assert.AreSame(eventData, args.EventData);
                Assert.IsTrue(moqSocket.SentMessages.Contains(MessageType.PingReq));
                are.Set();
            };

            client.SendMessageAsync(bldr, eventData, null);

            if (!are.WaitOne(5000))
            {
                Assert.Fail("SendMessageComplete event not fired for PingRequest.");
            }
        }

        [TestMethod]
        public void ConnectToFailedBrokerCallsNetworkErrorEventAfterTimeout()
        {
            var are = new AutoResetEvent(false);
            var eventData = "Test data";
            var moqSocket = new MoqSocket
            {
                DoNotRespond = true
            };

            MqttProtocolInformation.Settings.NetworkTimeout = 5; // 5 seconds
            var client = new MqttClientProtocol(new LogCompositor(), moqSocket);
            var bldr = new MqttConnectMessageBuilder
            {
                ClientId = "Unit-test"
            };

            client.SendMessageComplete += (sender, args) => Assert.Fail();
            client.NetworkError += (sender, args) => are.Set();

            client.SendMessageAsync(bldr, eventData, null);

            if (!are.WaitOne(MqttProtocolInformation.Settings.NetworkTimeout * 1000 + 5000))
            {
                Assert.Fail("NetworkError event not fired for Connect.");
            }
        }

        [TestMethod]
        public void PingToFailedBrokerCallsNetworkErrorEventAfterTimeout()
        {
            var are = new AutoResetEvent(false);
            var eventData = "Test data";
            var moqSocket = new MoqSocket
            {
                DoNotRespond = true
            };

            MqttProtocolInformation.Settings.NetworkTimeout = 5; // 5 seconds
            var client = new MqttClientProtocol(new LogCompositor(), moqSocket);
            var bldr = new MqttPingRequestMessageBuilder();

            client.SendMessageComplete += (sender, args) => Assert.Fail();
            client.NetworkError += (sender, args) => are.Set();

            client.SendMessageAsync(bldr, eventData, null);

            if (!are.WaitOne(MqttProtocolInformation.Settings.NetworkTimeout * 1000 + 5000))
            {
                Assert.Fail("NetworkError event not fired for PingRequest.");
            }
        }

        [TestMethod]
        public void PublishQosLevel0CallsSendMessageCompleteEventWithEventData()
        {
            var are = new AutoResetEvent(false);
            var eventData = "Test data";
            var moqSocket = new MoqSocket();
            var client = new MqttClientProtocol(new LogCompositor(), moqSocket);
            var bldr = new MqttPublishMessageBuilder
            {
                MessageId = 42,
                QualityOfService = QualityOfService.AtMostOnce,
                TopicName = "a/b/c"
            };

            client.SendMessageComplete += (sender, args) =>
            {
                Assert.AreSame(eventData, args.EventData);
                Assert.IsTrue(moqSocket.SentMessages.Contains(MessageType.Publish));
                Assert.IsFalse(moqSocket.SentMessages.Contains(MessageType.PubRec));
                Assert.IsFalse(moqSocket.SentMessages.Contains(MessageType.PubRel));
                Assert.IsFalse(moqSocket.SentMessages.Contains(MessageType.PubComp));
                are.Set();
            };

            client.SendMessageAsync(bldr, eventData, null);

            if (!are.WaitOne(5000))
            {
                Assert.Fail("SendMessageComplete event not fired for Publish (Qos=AtMostOnce).");
            }
        }

        [TestMethod]
        public void PublishQosLevel1CallsSendMessageCompleteEventWithEventData()
        {
            var are = new AutoResetEvent(false);
            var eventData = "Test data";
            var moqSocket = new MoqSocket();
            var client = new MqttClientProtocol(new LogCompositor(), moqSocket);
            var bldr = new MqttPublishMessageBuilder
            {
                MessageId = 42,
                QualityOfService = QualityOfService.AtLeastOnce,
                TopicName = "a/b/c"
            };

            client.SendMessageComplete += (sender, args) =>
            {
                var msg = args.Message as IMqttIdMessage;
                Assert.IsNotNull(msg);
                Assert.AreEqual(bldr.MessageId, msg.MessageId);
                Assert.AreSame(eventData, args.EventData);
                Assert.IsTrue(moqSocket.SentMessages.Contains(MessageType.Publish));
                Assert.IsFalse(moqSocket.SentMessages.Contains(MessageType.PubRec));
                Assert.IsFalse(moqSocket.SentMessages.Contains(MessageType.PubRel));
                Assert.IsFalse(moqSocket.SentMessages.Contains(MessageType.PubComp));
                are.Set();
            };

            client.SendMessageAsync(bldr, eventData, null);

            if (!are.WaitOne(5000))
            {
                Assert.Fail("SendMessageComplete event not fired for Publish (Qos=AtLeastOnce).");
            }
        }

        [TestMethod]
        public void PublishQosLevel2CallsSendMessageCompleteEventWithEventData()
        {
            var are = new AutoResetEvent(false);
            var eventData = "Test data";
            var moqSocket = new MoqSocket();
            var client = new MqttClientProtocol(new LogCompositor(), moqSocket);
            var bldr = new MqttPublishMessageBuilder
            {
                MessageId = 42,
                QualityOfService = QualityOfService.ExactlyOnce,
                TopicName = "a/b/c"
            };

            client.SendMessageComplete += (sender, args) =>
            {
                var msg = args.Message as IMqttIdMessage;
                Assert.IsNotNull(msg);
                Assert.AreEqual(bldr.MessageId, msg.MessageId);
                Assert.AreSame(eventData, args.EventData);
                Assert.IsTrue(moqSocket.SentMessages.Contains(MessageType.Publish));
                Assert.IsFalse(moqSocket.SentMessages.Contains(MessageType.PubRec));
                Assert.IsTrue(moqSocket.SentMessages.Contains(MessageType.PubRel));
                Assert.IsFalse(moqSocket.SentMessages.Contains(MessageType.PubComp));
                are.Set();
            };

            client.SendMessageAsync(bldr, eventData, null);

            if (!are.WaitOne(5000))
            {
                Assert.Fail("SendMessageComplete event not fired for Publish (Qos=ExactlyOnce).");
            }
        }

        [TestMethod]
        public void ReceivePublishQosLevel0CallsMessageReceived()
        {
            var are = new AutoResetEvent(false);
            var moqSocket = new MoqSocket();
            var client = new MqttClientProtocol(new LogCompositor(), moqSocket);
            var topic = "a/b/c";
            var bldr = new MqttPublishMessageBuilder
            {
                MessageId = 42,
                QualityOfService = QualityOfService.AtMostOnce,
                TopicName = topic
            };

            client.PublishReceived += (sender, args) =>
            {
                var pubMsg = args.Message as MqttPublishMessage;
                Assert.IsNotNull(pubMsg);
                Assert.AreEqual(topic, pubMsg.TopicName);
                Assert.AreEqual(0, moqSocket.SentMessages.Count);
                are.Set();
            };

            moqSocket.ReceiveMessage(bldr);

            if (!are.WaitOne(5000))
            {
                Assert.Fail("PublishReceived event not fired for received Publish (Qos=AtMostOnce).");
            }
        }

        [TestMethod]
        public void ReceivePublishQosLevel1CallsMessageReceived()
        {
            var are = new AutoResetEvent(false);
            var moqSocket = new MoqSocket();
            var client = new MqttClientProtocol(new LogCompositor(), moqSocket);
            var topic = "a/b/c";
            var bldr = new MqttPublishMessageBuilder
            {
                MessageId = 42,
                QualityOfService = QualityOfService.AtLeastOnce,
                TopicName = topic
            };

            client.PublishReceived += (sender, args) =>
            {
                var pubMsg = args.Message as MqttPublishMessage;
                Assert.IsNotNull(pubMsg);
                Assert.AreEqual(topic, pubMsg.TopicName);
                Assert.AreEqual(1, moqSocket.SentMessages.Count);
                Assert.IsTrue(moqSocket.SentMessages.Contains(MessageType.PubAck));
                are.Set();
            };

            moqSocket.ReceiveMessage(bldr);

            if (!are.WaitOne(5000))
            {
                Assert.Fail("PublishReceived event not fired for received Publish (Qos=AtLeastOnce).");
            }
        }

        [TestMethod]
        public void ReceivePublishQosLevel2CallsMessageReceived()
        {
            var are = new AutoResetEvent(false);
            var moqSocket = new MoqSocket();
            var client = new MqttClientProtocol(new LogCompositor(), moqSocket);
            var topic = "a/b/c";
            var id = 42;
            var bldr = new MqttPublishMessageBuilder
            {
                MessageId = id,
                QualityOfService = QualityOfService.ExactlyOnce,
                TopicName = topic
            };

            client.PublishReceived += (sender, args) =>
            {
                var pubMsg = args.Message as MqttPublishMessage;
                Assert.IsNotNull(pubMsg);
                Assert.AreEqual(topic, pubMsg.TopicName);
                Assert.AreEqual(2, moqSocket.SentMessages.Count);
                Assert.AreEqual(id, pubMsg.MessageId);
                Assert.IsTrue(moqSocket.SentMessages.Contains(MessageType.PubRec));
                Assert.IsTrue(moqSocket.SentMessages.Contains(MessageType.PubComp));
                are.Set();
            };

            moqSocket.ReceiveMessage(bldr);

            if (!are.WaitOne(5000))
            {
                Assert.Fail("PublishReceived event not fired for received Publish (Qos=ExactlyOnce).");
            }
        }
    }
}
