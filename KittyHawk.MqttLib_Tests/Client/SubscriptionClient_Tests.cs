using System.Threading;
using KittyHawk.MqttLib.Client;
using KittyHawk.MqttLib.Collections;
using KittyHawk.MqttLib.Messages;
using KittyHawk.MqttLib.Net;
using KittyHawk.MqttLib.Plugins.Logging;
using KittyHawk.MqttLib_Tests.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KittyHawk.MqttLib_Tests.Client
{
    [TestClass]
    public class SubscriptionClient_Tests
    {
        [TestMethod]
        public void OnMessageCallbackGetsCalledWithCorrectParams()
        {
            var are = new AutoResetEvent(false);
            var ip = "1.1.1.1";
            var port = 1883;
            var eventData = "Test data";
            string connectionKey = "123";
            var moqSocket = new MoqSocket();
            var protocol = new MqttClientProtocol(new LogCompositor(), moqSocket);
            var bldr = new MqttConnectMessageBuilder
            {
                ClientId = "UnitTest"
            };

            protocol.ConnectAsync(bldr, ip, port, SocketEncryption.None, eventData);

            var subscriptionItem = new SubscriptionItem
            {
                TopicName = "a/b/c",
                QualityOfService = QualityOfService.AtMostOnce
            };
            var subClient = new SubscriptionClient(protocol, subscriptionItem, connectionKey);

            subClient.OnMessage(msg =>
            {
                Assert.IsNotNull(msg);
                Assert.IsTrue(msg.MessageType == MessageType.Publish);
                Assert.AreEqual(msg.Payload[0], 0x00);
                Assert.AreEqual(msg.Payload[1], 0x01);
                Assert.AreEqual(msg.Payload[2], 0x02);
                are.Set();
            });

            moqSocket.ReceiveMessage(new MqttPublishMessageBuilder
            {
                TopicName = "a/b/c",
                Payload = new byte[] { 0x00, 0x01, 0x02 }
            });

            if (!are.WaitOne(5000))
            {
                Assert.Fail("OnMessage callback not called.");
            }
        }

        [TestMethod]
        public void OnMessageCallbackGetsCalledWithWildcardTopics1()
        {
            var are = new AutoResetEvent(false);
            var ip = "1.1.1.1";
            var port = 1883;
            var eventData = "Test data";
            string connectionKey = "123";
            var moqSocket = new MoqSocket();
            var protocol = new MqttClientProtocol(new LogCompositor(), moqSocket);
            var bldr = new MqttConnectMessageBuilder
            {
                ClientId = "UnitTest"
            };

            protocol.ConnectAsync(bldr, ip, port, SocketEncryption.None, eventData);

            var subscriptionItem = new SubscriptionItem
            {
                TopicName = "a/b/+",
                QualityOfService = QualityOfService.AtMostOnce
            };
            var subClient = new SubscriptionClient(protocol, subscriptionItem, connectionKey);

            subClient.OnMessage(msg =>
            {
                Assert.IsNotNull(msg);
                Assert.IsTrue(msg.MessageType == MessageType.Publish);
                Assert.AreEqual(msg.Payload[0], 0x00);
                Assert.AreEqual(msg.Payload[1], 0x01);
                Assert.AreEqual(msg.Payload[2], 0x02);
                are.Set();
            });

            moqSocket.ReceiveMessage(new MqttPublishMessageBuilder
            {
                TopicName = "a/b/c",
                Payload = new byte[] { 0x00, 0x01, 0x02 }
            });

            if (!are.WaitOne(5000))
            {
                Assert.Fail("OnMessage callback not called.");
            }
        }

        [TestMethod]
        public void OnMessageCallbackGetsCalledWithWildcardTopics2()
        {
            var are = new AutoResetEvent(false);
            var ip = "1.1.1.1";
            var port = 1883;
            var eventData = "Test data";
            string connectionKey = "123";
            var moqSocket = new MoqSocket();
            var protocol = new MqttClientProtocol(new LogCompositor(), moqSocket);
            var bldr = new MqttConnectMessageBuilder
            {
                ClientId = "UnitTest"
            };

            protocol.ConnectAsync(bldr, ip, port, SocketEncryption.None, eventData);

            var subscriptionItem = new SubscriptionItem
            {
                TopicName = "a/+/c",
                QualityOfService = QualityOfService.AtMostOnce
            };
            var subClient = new SubscriptionClient(protocol, subscriptionItem, connectionKey);

            subClient.OnMessage(msg =>
            {
                Assert.IsNotNull(msg);
                Assert.IsTrue(msg.MessageType == MessageType.Publish);
                Assert.AreEqual(msg.Payload[0], 0x00);
                Assert.AreEqual(msg.Payload[1], 0x01);
                Assert.AreEqual(msg.Payload[2], 0x02);
                are.Set();
            });

            moqSocket.ReceiveMessage(new MqttPublishMessageBuilder
            {
                TopicName = "a/b/c",
                Payload = new byte[] { 0x00, 0x01, 0x02 }
            });

            if (!are.WaitOne(5000))
            {
                Assert.Fail("OnMessage callback not called.");
            }
        }

        [TestMethod]
        public void OnMessageCallbackGetsCalledWithWildcardTopics3()
        {
            var are = new AutoResetEvent(false);
            var ip = "1.1.1.1";
            var port = 1883;
            var eventData = "Test data";
            string connectionKey = "123";
            var moqSocket = new MoqSocket();
            var protocol = new MqttClientProtocol(new LogCompositor(), moqSocket);
            var bldr = new MqttConnectMessageBuilder
            {
                ClientId = "UnitTest"
            };

            protocol.ConnectAsync(bldr, ip, port, SocketEncryption.None, eventData);

            var subscriptionItem = new SubscriptionItem
            {
                TopicName = "a/b/#",
                QualityOfService = QualityOfService.AtMostOnce
            };
            var subClient = new SubscriptionClient(protocol, subscriptionItem, connectionKey);

            subClient.OnMessage(msg =>
            {
                Assert.IsNotNull(msg);
                Assert.IsTrue(msg.MessageType == MessageType.Publish);
                Assert.AreEqual(msg.Payload[0], 0x00);
                Assert.AreEqual(msg.Payload[1], 0x01);
                Assert.AreEqual(msg.Payload[2], 0x02);
                are.Set();
            });

            moqSocket.ReceiveMessage(new MqttPublishMessageBuilder
            {
                TopicName = "a/b/c",
                Payload = new byte[] { 0x00, 0x01, 0x02 }
            });

            if (!are.WaitOne(5000))
            {
                Assert.Fail("OnMessage callback not called.");
            }
        }

        [TestMethod]
        public void IsClosedCorrectlyReflectsStateOfObject()
        {
            var ip = "1.1.1.1";
            var port = 1883;
            var eventData = "Test data";
            string connectionKey = "123";
            var moqSocket = new MoqSocket();
            var protocol = new MqttClientProtocol(new LogCompositor(), moqSocket);
            var bldr = new MqttConnectMessageBuilder
            {
                ClientId = "UnitTest"
            };

            protocol.ConnectAsync(bldr, ip, port, SocketEncryption.None, eventData);

            var subscriptionItem = new SubscriptionItem
            {
                TopicName = "a/b/+",
                QualityOfService = QualityOfService.AtMostOnce
            };

            var subClient = new SubscriptionClient(protocol, subscriptionItem, connectionKey);

            Assert.IsTrue(subClient.IsClosed);

            subClient.OnMessage(msg =>
            {
                Assert.IsNotNull(msg);
                Assert.IsTrue(msg.MessageType == MessageType.Publish);
            });

            Assert.IsFalse(subClient.IsClosed);
            subClient.Close();
            Assert.IsTrue(subClient.IsClosed);
        }

        [TestMethod]
        public void OnMessageCallbackGetsCalledAfterLateConnection()
        {
            var are = new AutoResetEvent(false);
            var ip = "1.1.1.1";
            var port = 1883;
            var eventData = "Test data";
            string connectionKey = "123";
            var moqSocket = new MoqSocket();
            var protocol = new MqttClientProtocol(new LogCompositor(), moqSocket);
            var bldr = new MqttConnectMessageBuilder
            {
                ClientId = "UnitTest"
            };

            var subscriptionItem = new SubscriptionItem
            {
                TopicName = "a/b/c",
                QualityOfService = QualityOfService.AtMostOnce
            };
            var subClient = new SubscriptionClient(protocol, subscriptionItem, connectionKey);

            subClient.OnMessage(msg =>
            {
                Assert.IsNotNull(msg);
                Assert.IsTrue(msg.MessageType == MessageType.Publish);
                Assert.AreEqual(msg.Payload[0], 0x00);
                Assert.AreEqual(msg.Payload[1], 0x01);
                Assert.AreEqual(msg.Payload[2], 0x02);
                are.Set();
            });

            protocol.ConnectAsync(bldr, ip, port, SocketEncryption.None, eventData);

            moqSocket.ReceiveMessage(new MqttPublishMessageBuilder
            {
                TopicName = "a/b/c",
                Payload = new byte[] { 0x00, 0x01, 0x02 }
            });

            if (!are.WaitOne(5000))
            {
                Assert.Fail("OnMessage callback not called.");
            }
        }

        [TestMethod]
        public void OnMessageCallbackDoesNotGetCalledAfterClose()
        {
            var are = new AutoResetEvent(false);
            var ip = "1.1.1.1";
            var port = 1883;
            var eventData = "Test data";
            string connectionKey = "123";
            var moqSocket = new MoqSocket();
            var protocol = new MqttClientProtocol(new LogCompositor(), moqSocket);
            var bldr = new MqttConnectMessageBuilder
            {
                ClientId = "UnitTest"
            };

            protocol.ConnectAsync(bldr, ip, port, SocketEncryption.None, eventData);

            var subscriptionItem = new SubscriptionItem
            {
                TopicName = "a/b/c",
                QualityOfService = QualityOfService.AtMostOnce
            };
            var subClient = new SubscriptionClient(protocol, subscriptionItem, connectionKey);

            subClient.OnMessage(msg =>
            {
                Assert.Fail("OnMessage callback was called after Close call.");
                are.Set();
            });
            
            subClient.Close();

            moqSocket.ReceiveMessage(new MqttPublishMessageBuilder
            {
                TopicName = "a/b/c",
                Payload = new byte[] { 0x00, 0x01, 0x02 }
            });

            are.WaitOne(3000);
        }
    }
}
