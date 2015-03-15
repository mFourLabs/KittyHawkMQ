
using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using KittyHawk.MqttLib.Collections;
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Messages;
using KittyHawk.MqttLib.Net;
using KittyHawk.MqttLib.Plugins.Logging;
using KittyHawk.MqttLib.Utilities;

namespace KittyHawk.MqttLib.Client
{
    public sealed class MqttClient : IDisposable
    {
        private readonly MqttClientProtocol _mqtt;
        private readonly SocketEncryption _encryptionLevel;
        private readonly ILogger _logger;
        private string _clientUid;

        public event MqttConnectMessageEventHandler ConnectionComplete;
        public event MqttMessageEventHandler DisconnectComplete;
        public event MqttSubscribeMessageEventHandler SubscribeComplete;
        public event MqttMessageEventHandler UnsubscribeComplete;
        public event MqttMessageEventHandler PublishMessageComplete;
        public event MqttMessageEventHandler PingComplete;
        public event MqttPublishMessageEventHandler PublishReceived;
        public event MqttMessageEventHandler NetworkError;

        /// <summary>
        /// Create an MQTT client that does not use encryption for communications.
        /// </summary>
        /// <returns>An MQTT client ready to make connections to a broker.</returns>
        public static MqttClient CreateClient()
        {
            var logManager = new LogCompositor();
            ActiveClientCollection.Instance.TestCanAddNewClient();
            // Each platform has its own implementation of Sockets and reader threads.
            var client = new MqttClient(new NetMfSocketAdapter(logManager), logManager, SocketEncryption.None);
            ActiveClientCollection.Instance.AddClient(client);
            return client;
        }

        /// <summary>
        /// Create an MQTT client the uses SSL encryption for communications.
        /// </summary>
        /// <param name="encryptionLevel">The encryption level to use.</param>
        /// <param name="caCertificate">The certificate for client authorities to use for authentication.</param>
        /// <returns></returns>
        public static MqttClient CreateSecureClient(SocketEncryption encryptionLevel, X509Certificate caCertificate)
        {
            var logManager = new LogCompositor();
            ActiveClientCollection.Instance.TestCanAddNewClient();
            // Each platform has its own implementation of Sockets and reader threads.
            var client = new MqttClient(new NetMfSocketAdapter(logManager, caCertificate), logManager, encryptionLevel);
            ActiveClientCollection.Instance.AddClient(client);
            return client;
        }

        internal MqttClient(ISocketAdapter socket, ILogger logger, SocketEncryption encryptionLevel)
        {
            _logger = logger;
            _encryptionLevel = encryptionLevel;
            _mqtt = new MqttClientProtocol(_logger, socket);
            _mqtt.ConnectComplete += MqttOnConnectComplete;
            _mqtt.SubscribeComplete += MqttOnSubscribeComplete;
            _mqtt.SendMessageComplete += MqttOnSendMessageComplete;
            _mqtt.PublishReceived += MqttOnPublishReceived;
            _mqtt.NetworkError += MqttOnNetworkError;
        }

        public bool IsConnected
        {
            get { return _mqtt.IsConnected(_clientUid); }
        }

        public bool IsEncrypted
        {
            get { return _encryptionLevel != SocketEncryption.None; }
        }

        /// <summary>
        /// Connect to a broker.
        /// </summary>
        /// <param name="clientId">The MQTT client ID</param>
        /// <param name="ipOrHost">An IP address or host name</param>
        /// <returns></returns>
        public void ConnectAsync(string clientId, string ipOrHost)
        {
            int port = IsEncrypted ? MqttProtocolInformation.Settings.SecurePort : MqttProtocolInformation.Settings.Port;
            var bldr = new MqttConnectMessageBuilder
            {
                ClientId = clientId
            };
            ConnectWithMessageAsync(bldr, ipOrHost, port);
        }

        /// <summary>
        /// Connect to a broker.
        /// </summary>
        /// <param name="clientId">The MQTT client ID</param>
        /// <param name="ipOrHost">An IP address or host name</param>
        /// <param name="port">The port number to use. Overrides default port in settings</param>
        /// <returns></returns>
        public void ConnectAsync(string clientId, string ipOrHost, int port)
        {
            var bldr = new MqttConnectMessageBuilder
            {
                ClientId = clientId
            };
            ConnectWithMessageAsync(bldr, ipOrHost, port);
        }

        /// <summary>
        /// Connect to a broker using a message builder instance.
        /// </summary>
        /// <param name="msgBuilder">A message builder instance with advanced connection parameters</param>
        /// <param name="ipOrHost">An IP address or host name</param>
        /// <returns></returns>
        public void ConnectWithMessageAsync(MqttConnectMessageBuilder msgBuilder, string ipOrHost)
        {
            int port = IsEncrypted ? MqttProtocolInformation.Settings.SecurePort : MqttProtocolInformation.Settings.Port;
            ConnectWithMessageAsync(msgBuilder, ipOrHost, port);
        }

        /// <summary>
        /// Connect to a broker using a message builder instance.
        /// </summary>
        /// <param name="msgBuilder">A message builder instance with advanced connection parameters</param>
        /// <param name="ipOrHost">An IP address or host name</param>
        /// <param name="port">The port number to use. Overrides default port in settings</param>
        /// <returns></returns>
        public void ConnectWithMessageAsync(MqttConnectMessageBuilder msgBuilder, string ipOrHost, int port)
        {
            _mqtt.ConnectAsync(msgBuilder, ipOrHost, port, _encryptionLevel, MessageType.Connect);
        }

        public void DisconnectAsync()
        {
            _mqtt.DisconnectAsync(MessageType.Disconnect, _clientUid);
        }

        public void PublishStringAsync(string topic, string payload)
        {
            // Encode the string into a byte array
            byte[] utf8Str = Encoding.UTF8.GetBytes(payload);

            PublishAsync(topic, utf8Str);
        }

        public void PublishStringAsync(string topic, string payload, QualityOfService qos, int messageId, bool retain)
        {
            // Encode the string into a byte array
            byte[] utf8Str = Encoding.UTF8.GetBytes(payload);

            PublishAsync(topic, utf8Str, qos, messageId, retain);
        }

        public void PublishUIntAsync(string topic, UInt16 payload)
        {
            // Encode the uint into a byte array
            int pos = 0;
            var payloadBytes = new byte[2];
            Frame.EncodeInt16(payload, payloadBytes, ref pos);

            PublishAsync(topic, payloadBytes);
        }

        public void PublishUIntAsync(string topic, UInt16 payload, QualityOfService qos, int messageId, bool retain)
        {
            // Encode the uint into a byte array
            int pos = 0;
            var payloadBytes = new byte[2];
            Frame.EncodeInt16(payload, payloadBytes, ref pos);

            PublishAsync(topic, payloadBytes, qos, messageId, retain);
        }

        public void PublishAsync(string topic, byte[] payload)
        {
            PublishAsync(topic, payload, QualityOfService.AtMostOnce, 1, false);
        }

        public void PublishAsync(string topic, byte[] payload, QualityOfService qos, int messageId, bool retain)
        {
            var msgBuilder = new MqttPublishMessageBuilder
            {
                MessageId = messageId,
                TopicName = topic,
                QualityOfService = qos,
                Retain = retain,
                Payload = payload
            };

            SendMessageAsync(msgBuilder);
        }

        public void SubscribeAsync(SubscriptionItem[] items, int messageId)
        {
            var msgBuilder = new MqttSubscribeMessageBuilder
            {
                MessageId = messageId,
                QualityOfService = QualityOfService.AtLeastOnce
            };

            foreach (var item in items)
            {
                msgBuilder.Subscriptions.Add(item);
            }

            SendMessageAsync(msgBuilder);
        }

        public void UnsubscribeAsync(string[] topics, int messageId)
        {
            var msgBuilder = new MqttUnsubscribeMessageBuilder
            {
                MessageId = messageId,
                TopicNames = topics
            };

            SendMessageAsync(msgBuilder);
        }

        public void PingAsync()
        {
            var msgBuilder = new MqttPingRequestMessageBuilder();
            SendMessageAsync(msgBuilder);
        }

        public void Dispose()
        {
            _mqtt.Dispose();
            ActiveClientCollection.Instance.RemoveClient(this);
        }

        private void SendMessageAsync(IMqttMessageBuilder msgBuilder)
        {
            _mqtt.SendMessageAsync(msgBuilder, msgBuilder.MessageType, _clientUid);
        }

        private void MqttOnNetworkError(object sender, MqttMessageEventArgs args)
        {
            if (NetworkError != null)
            {
                NetworkError(this, args);
            }
        }

        private void MqttOnPublishReceived(object sender, MqttNetEventArgs args)
        {
            if (args.Message.MessageType == MessageType.Publish)
            {
                if (PublishReceived != null)
                {
                    PublishReceived(this, new MqttPublishMessageEventArgs
                    {
                        Exception = args.Exception,
                        Message = args.Message as MqttPublishMessage
                    });
                }
            }
        }

        private void MqttOnConnectComplete(object sender, MqttNetEventArgs args)
        {
            _clientUid = args.ClientUid;
            FireConnectionComplete(this, args);
        }

        private void MqttOnSubscribeComplete(object sender, MqttNetEventArgs args)
        {
            FireSubscribeComplete(this, args);
        }

        private void MqttOnSendMessageComplete(object sender, MqttNetEventArgs args)
        {
            if (args.EventData == null)
            {
                return;
            }

            var type = (MessageType)args.EventData;
            switch (type)
            {
                case MessageType.Unsubscribe:
                    FireUnsubscribeComplete(this, args);
                    break;

                case MessageType.Publish:
                    FirePublishMessageComplete(this, args);
                    break;

                case MessageType.Disconnect:
                    FireDisconnectCompleted(this, args);
                    break;

                case MessageType.PingReq:
                    FirePingComplete(this, args);
                    break;
            }
        }

        private void FireConnectionComplete(object sender, MqttNetEventArgs args)
        {
            if (ConnectionComplete != null)
            {
                ConnectionComplete(this, new MqttConnectMessageEventArgs()
                {
                    Exception = args.Exception,
                    AdditionalErrorInfo = args.AdditionalErrorInfo,
                    Message = args.Message as MqttConnectAckMessage
                });
            }
        }

        private void FireDisconnectCompleted(object sender, MqttNetEventArgs args)
        {
            if (DisconnectComplete != null)
            {
                DisconnectComplete(this, new MqttMessageEventArgs()
                {
                    Exception = args.Exception,
                    AdditionalErrorInfo = args.AdditionalErrorInfo,
                    Message = args.Message
                });
            }
        }

        private void FireSubscribeComplete(object sender, MqttNetEventArgs args)
        {
            if (SubscribeComplete != null)
            {
                SubscribeComplete(this, new MqttSubscribeMessageEventArgs()
                {
                    Exception = args.Exception,
                    AdditionalErrorInfo = args.AdditionalErrorInfo,
                    Message = args.Message as MqttSubscribeAckMessage
                });
            }
        }

        private void FireUnsubscribeComplete(object sender, MqttNetEventArgs args)
        {
            if (UnsubscribeComplete != null)
            {
                UnsubscribeComplete(this, new MqttMessageEventArgs()
                {
                    Exception = args.Exception,
                    AdditionalErrorInfo = args.AdditionalErrorInfo,
                    Message = args.Message
                });
            }
        }

        private void FirePingComplete(object sender, MqttNetEventArgs args)
        {
            if (PingComplete != null)
            {
                PingComplete(this, new MqttMessageEventArgs()
                {
                    Exception = args.Exception,
                    AdditionalErrorInfo = args.AdditionalErrorInfo,
                    Message = args.Message
                });
            }
        }

        private void FirePublishMessageComplete(object sender, MqttNetEventArgs args)
        {
            if (PublishMessageComplete != null)
            {
                PublishMessageComplete(this, new MqttMessageEventArgs()
                {
                    Exception = args.Exception,
                    AdditionalErrorInfo = args.AdditionalErrorInfo,
                    Message = args.Message
                });
            }
        }
    }
}
