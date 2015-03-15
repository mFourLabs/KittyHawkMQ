using System;
using KittyHawk.MqttLib.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using KittyHawk.MqttLib;

namespace KittyHawk.MqttLib_Tests.Messages
{
    [TestClass]
    public class MqttConnectMessage_Tests
    {
        [TestMethod]
        public void CanCreateFromConnectMessageBuilder()
        {
            var msgBuilder = new MqttConnectMessageBuilder
            {
                Duplicate = false,
                QualityOfService = QualityOfService.ExactlyOnce,
                Retain = true,
                ClientId = "A_Device"
            };

            Assert.AreEqual(MessageType.Connect, msgBuilder.MessageType);

            var msg = msgBuilder.GetMessage();
            Assert.AreEqual(typeof(MqttConnectMessage), msg.GetType());
            Assert.AreEqual(MessageType.Connect, msg.MessageType);
            Assert.AreEqual(false, msg.Duplicate);
            Assert.AreEqual(QualityOfService.ExactlyOnce, msg.QualityOfService);
            Assert.AreEqual(true, msg.Retain);
        }

        [TestMethod]
        public void CanReadProtocolHeader()
        {
            var msgBuilder = new MqttConnectMessageBuilder
            {
                Duplicate = false,
                QualityOfService = QualityOfService.ExactlyOnce,
                Retain = true,
                ClientId = "A_Device"
            };

            var msg = msgBuilder.GetMessage() as MqttConnectMessage;

            Assert.AreEqual(msgBuilder.Duplicate, msg.Duplicate);
            Assert.AreEqual(msgBuilder.QualityOfService, msg.QualityOfService);
            Assert.AreEqual(msgBuilder.Retain, msg.Retain);
            Assert.AreEqual(msgBuilder.ClientId, msg.ClientId);
        }

        [TestMethod]
        public void CanReadConnectFlagsOpt1()
        {
            var msgBuilder = new MqttConnectMessageBuilder
            {
                Duplicate = false,
                QualityOfService = QualityOfService.ExactlyOnce,
                Retain = true,
                CleanSession = true,
                WillQualityOfService = QualityOfService.ExactlyOnce,
                WillRetainFlag = true,
                ClientId = "A_Device",
                UserName = "None"
            };
            
            var msg = msgBuilder.GetMessage() as MqttConnectMessage;

            Assert.AreEqual(true, msg.CleanSession);
            Assert.AreEqual(false, msg.WillFlag);
            Assert.AreEqual(QualityOfService.ExactlyOnce, msg.WillQualityOfService);
            Assert.AreEqual(true, msg.WillRetain);
            Assert.AreEqual(false, msg.PasswordFlag);
            Assert.AreEqual(true, msg.UserNameFlag);
        }

        [TestMethod]
        public void CanReadConnectFlagsOpt2()
        {
            var msgBuilder = new MqttConnectMessageBuilder
            {
                Duplicate = false,
                QualityOfService = QualityOfService.ExactlyOnce,
                Retain = true,
                CleanSession = false,
                WillQualityOfService = QualityOfService.AtLeastOnce,
                WillRetainFlag = false,
                ClientId = "A_Device",
                WillTopic = "a/b/c/d",
                Password = "None"
           };

            var msg = msgBuilder.GetMessage() as MqttConnectMessage;

            Assert.AreEqual(false, msg.CleanSession);
            Assert.AreEqual(true, msg.WillFlag);
            Assert.AreEqual(QualityOfService.AtLeastOnce, msg.WillQualityOfService);
            Assert.AreEqual(false, msg.WillRetain);
            Assert.AreEqual(true, msg.PasswordFlag);
            Assert.AreEqual(false, msg.UserNameFlag);
        }

        [TestMethod]
        public void CanReadDefaultKeepAliveTime()
        {
            var msgBuilder = new MqttConnectMessageBuilder
            {
                Duplicate = false,
                QualityOfService = QualityOfService.ExactlyOnce,
                Retain = true,
                CleanSession = false,
                WillQualityOfService = QualityOfService.AtLeastOnce,
                WillRetainFlag = false,
                ClientId = "A_Device",
                Password = "None"
            };

            var msg = msgBuilder.GetMessage() as MqttConnectMessage;

            Assert.AreEqual(MqttProtocolInformation.Settings.KeepAliveTime, msg.KeepAliveTime);
        }

        [TestMethod]
        public void CanReadUserSetKeepAliveTime()
        {
            var msgBuilder = new MqttConnectMessageBuilder
            {
                Duplicate = false,
                QualityOfService = QualityOfService.ExactlyOnce,
                Retain = true,
                CleanSession = false,
                WillQualityOfService = QualityOfService.AtLeastOnce,
                WillRetainFlag = false,
                KeepAliveTime = 10*60,
                ClientId = "A_Device",
                Password = "None"
            };

            var msg = msgBuilder.GetMessage() as MqttConnectMessage;

            Assert.AreEqual(10 * 60, msg.KeepAliveTime);
        }

        [TestMethod]
        public void KeepAliveTimeValidateCatchesOutOfRangeValue()
        {
            var msgBuilder = new MqttConnectMessageBuilder
            {
                Duplicate = false,
                QualityOfService = QualityOfService.ExactlyOnce,
                Retain = true,
                CleanSession = false,
                WillQualityOfService = QualityOfService.AtLeastOnce,
                WillRetainFlag = false,
                ClientId = "A_Device",
                Password = "None"
            };

            try
            {
                msgBuilder.KeepAliveTime = 0x1FFFF;
            }
            catch (ArgumentException)
            {
                return;
            }
            catch (Exception)
            {
                Assert.Fail("Incorrect exception type thrown.");
            }

            Assert.Fail("No exception thrown for out of range KeepAliveTime");
        }

        [TestMethod]
        public void CanReadClientId()
        {
            var msgBuilder = new MqttConnectMessageBuilder
            {
                Duplicate = false,
                QualityOfService = QualityOfService.ExactlyOnce,
                Retain = true,
                CleanSession = false,
                WillQualityOfService = QualityOfService.AtLeastOnce,
                WillRetainFlag = false,
                ClientId = "A_Device",
                Password = "None"
            };

            var msg = msgBuilder.GetMessage() as MqttConnectMessage;

            Assert.AreEqual("A_Device", msg.ClientId);
        }

        [TestMethod]
        public void ClientIdValidationCatchesNullValue()
        {
            var msgBuilder = new MqttConnectMessageBuilder
            {
                Duplicate = false,
                QualityOfService = QualityOfService.ExactlyOnce,
                Retain = true,
                CleanSession = false,
                WillQualityOfService = QualityOfService.AtLeastOnce,
                WillRetainFlag = false,
            };

            try
            {
                var msg1 = msgBuilder.GetMessage() as MqttConnectMessage;
            }
            catch (ArgumentException)
            {
                return;
            }
            catch (Exception)
            {
                Assert.Fail("Incorrect exception type thrown.");
            }

            Assert.Fail("No exception thrown for invalid ClientId");
        }

        [TestMethod]
        public void ClientIdValidationCatchesTooLongValue()
        {
            var msgBuilder = new MqttConnectMessageBuilder
            {
                Duplicate = false,
                QualityOfService = QualityOfService.ExactlyOnce,
                Retain = true,
                CleanSession = false,
                WillQualityOfService = QualityOfService.AtLeastOnce,
                WillRetainFlag = false,
            };

            try
            {
                msgBuilder.ClientId = "123456789012345678901234";
            }
            catch (ArgumentException)
            {
                return;
            }
            catch (Exception)
            {
                Assert.Fail("Incorrect exception type thrown.");
            }

            Assert.Fail("No exception thrown for invalid ClientId");
        }

        [TestMethod]
        public void CanReadWillTopicAndMessage()
        {
            var msgBuilder = new MqttConnectMessageBuilder
            {
                Duplicate = false,
                QualityOfService = QualityOfService.ExactlyOnce,
                Retain = false,
                CleanSession = false,
                WillQualityOfService = QualityOfService.AtLeastOnce,
                WillRetainFlag = false,
                ClientId = "A_Device",
                WillTopic = "a/b/c/d",
                WillMessage = "Something bad happened"
            };

            var msg = msgBuilder.GetMessage() as MqttConnectMessage;

            Assert.AreEqual("a/b/c/d", msg.WillTopic);
            Assert.AreEqual("Something bad happened", msg.WillMessage);
            Assert.AreEqual(true, msg.WillFlag);
        }

        [TestMethod]
        public void CanReadZeroLengthWillMessage()
        {
            var msgBuilder = new MqttConnectMessageBuilder
            {
                Duplicate = false,
                QualityOfService = QualityOfService.ExactlyOnce,
                Retain = false,
                CleanSession = false,
                WillQualityOfService = QualityOfService.AtLeastOnce,
                WillRetainFlag = false,
                ClientId = "A_Device",
                WillTopic = "a/b/c/d",
                WillMessage = ""
            };

            var msg = msgBuilder.GetMessage() as MqttConnectMessage;

            Assert.AreEqual("a/b/c/d", msg.WillTopic);
            Assert.AreEqual("", msg.WillMessage);
            Assert.AreEqual(true, msg.WillFlag);
        }

        [TestMethod]
        public void CanReadUserName()
        {
            var msgBuilder = new MqttConnectMessageBuilder
            {
                Duplicate = false,
                QualityOfService = QualityOfService.ExactlyOnce,
                Retain = false,
                CleanSession = false,
                WillQualityOfService = QualityOfService.AtLeastOnce,
                WillRetainFlag = false,
                ClientId = "A_Device",
                UserName = "Slartibartfast"
            };

            var msg = msgBuilder.GetMessage() as MqttConnectMessage;

            Assert.AreEqual("Slartibartfast", msg.UserName);
        }

        [TestMethod]
        public void CanReadUserNameAndPassword()
        {
            var msgBuilder = new MqttConnectMessageBuilder
            {
                Duplicate = false,
                QualityOfService = QualityOfService.ExactlyOnce,
                Retain = false,
                CleanSession = false,
                WillQualityOfService = QualityOfService.AtLeastOnce,
                WillRetainFlag = false,
                ClientId = "A_Device",
                UserName = "Slartibartfast",
                Password = "Magrathean"
            };

            var msg = msgBuilder.GetMessage() as MqttConnectMessage;

            Assert.AreEqual("Slartibartfast", msg.UserName);
            Assert.AreEqual("Magrathean", msg.Password);
        }
    }
}
