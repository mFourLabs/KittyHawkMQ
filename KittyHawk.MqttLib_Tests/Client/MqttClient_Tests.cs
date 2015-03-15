using System;
using KittyHawk.MqttLib;
using KittyHawk.MqttLib.Client;
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Messages;
using KittyHawk.MqttLib.Net;
using KittyHawk.MqttLib.Plugins.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace KittyHawk.MqttLib_Tests.Client
{
    [TestClass]
    public class MqttClient_Tests
    {
        [TestMethod]
        public async void ConnectAsyncCallsSocketConnetWithGivenParams()
        {
            var ip = "1.1.1.1";
            var port = 1883;
            var socketMock = new Mock<ISocketAdapter>();
            var loggerMock = new Mock<ILogger>();
            var client = new MqttClient(socketMock.Object, loggerMock.Object, SocketEncryption.None);
            var bldr = new MqttConnectMessageBuilder
            {
                ClientId = "UnitTest"
            };

            await client.ConnectWithMessageAsync(bldr, ip, port);

            socketMock.Verify(socket => socket.ConnectAsync(
                It.Is<string>(s => s.Equals(ip)),   // Called with passed in IP address?
                It.Is<int>(i => i == port),         // Called with passed in port?
                It.Is<SocketEventArgs>(a => a != null)
                ),
                Times.Once());                      // Called once and only once
        }

        [TestMethod]
        public async void ConnectAsyncCallsSocketConnectWithDefaultParams()
        {
            var ip = "1.1.1.1";
            var socketMock = new Mock<ISocketAdapter>();
            var loggerMock = new Mock<ILogger>();
            var client = new MqttClient(socketMock.Object, loggerMock.Object, SocketEncryption.None);
            var bldr = new MqttConnectMessageBuilder
            {
                ClientId = "UnitTest",
                CleanSession = true,
                Duplicate = false,
                KeepAliveTime = 15,
                UserName = "Boris",
                Password = "password",
                QualityOfService = QualityOfService.ExactlyOnce
            };

            await client.ConnectWithMessageAsync(bldr, ip);

            socketMock.Verify(socket => socket.ConnectAsync(
                It.Is<string>(s => s.Equals(ip)),
                It.Is<int>(i => i == MqttProtocolInformation.Settings.Port),  // Called with default port?
                It.Is<SocketEventArgs>(a => a != null)
                ),
                Times.Once());
        }

        [TestMethod]
        public async void DisconnectAsyncCallsSocketSendMessageAsyncWithParams()
        {
            var socketMock = new Mock<ISocketAdapter>();
            var loggerMock = new Mock<ILogger>();
            var client = new MqttClient(socketMock.Object, loggerMock.Object, SocketEncryption.None);

            await client.DisconnectAsync();

            socketMock.Verify(socket => socket.WriteAsync(
                It.Is<SocketEventArgs>(a => a != null && a.MessageToSend.MessageType == MessageType.Disconnect)),
                Times.Once());
        }

        //[TestMethod]
        //public async void SendMessageAsyncCallsSocketSendMessageAsyncWithParams()
        //{
        //    var bldrMock = new Mock<IMqttMessageBuilder>();
        //    var msgMock = new Mock<IMqttMessage>();
        //    var socketMock = new Mock<ISocketAdapter>();
        //    var client = new MqttClient(socketMock.Object);

        //    bldrMock.Setup(bldr => bldr.GetMessage()).Returns(msgMock.Object);

        //    await client.SendMessageAsync(bldrMock.Object);

        //    socketMock.Verify(socket => socket.WriteAsync(
        //        It.Is<SocketEventArgs>(a => a != null && a.MessageToSend == msgMock.Object)),
        //        Times.Once());
        //}

        //[TestMethod]
        //public async void SendMessageAsyncFailsWhenSocketSendMessageFails()
        //{
        //    var bldrMock = new Mock<IMqttMessageBuilder>();
        //    var msgMock = new Mock<IMqttMessage>();
        //    var socketMock = new Mock<ISocketAdapter>();
        //    var client = new MqttClient(socketMock.Object);

        //    bldrMock.Setup(bldr => bldr.GetMessage()).Returns(msgMock.Object);
        //    socketMock.Setup(s => s.WriteAsync(It.IsNotNull<SocketEventArgs>())).Throws<ArgumentException>();

        //    try
        //    {
        //        await client.SendMessageAsync(bldrMock.Object);
        //    }
        //    catch (ArgumentException)
        //    {
        //        socketMock.Verify(socket => socket.WriteAsync(
        //            It.Is<SocketEventArgs>(a => a != null && a.MessageToSend == msgMock.Object)),
        //            Times.Exactly(MqttProtocolInformation.Settings.NetworkRetries + 1));
        //    }
        //    catch (Exception)
        //    {
        //        Assert.Fail("Invalid exception type thrown.");
        //    }

        //    Assert.Fail("Exception not thrown.");
        //}
    }
}
