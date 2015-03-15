
using System;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
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

        public event MqttMessageEventHandler NetworkError;
        public event MqttPublishMessageEventHandler UnhandledPublishReceived;

        /// <summary>
        /// Create an MQTT client that does not use encryption for communications.
        /// </summary>
        /// <returns>An MQTT client ready to make connections to a broker.</returns>
        public static MqttClient CreateClient()
        {
            var logManager = new LogCompositor();
            logManager.AddLoggingProvider(typeof(DebugLogger));
            ActiveClientCollection.Instance.TestCanAddNewClient();
            // Each platform has its own implementation of Sockets and reader threads.
            var client = new MqttClient(new WinRTSocketAdapter(logManager), logManager, SocketEncryption.None);
            ActiveClientCollection.Instance.AddClient(client);
            return client;
        }

        /// <summary>
        /// Create an MQTT client the uses SSL encryption for communications. If the certificate is private and not
        /// signed ay a trusted Certificate Authority, the certificate must be installed in the application's
        /// certificate store ahead of making a connection.
        /// </summary>
        /// <param name="encryption">The encryption level to use.</param>
        /// <returns>An MQTT client ready to make secure connections to a broker.</returns>
        public static MqttClient CreateSecureClient(SocketEncryption encryption)
        {
            var logManager = new LogCompositor();
            logManager.AddLoggingProvider(typeof(DebugLogger));
            ActiveClientCollection.Instance.TestCanAddNewClient();
            // Each platform has its own implementation of Sockets and reader threads.
            var client = new MqttClient(new WinRTSocketAdapter(logManager), logManager, encryption);
            ActiveClientCollection.Instance.AddClient(client);
            return client;
        }

        internal MqttClient(ISocketAdapter socket, ILogger logger, SocketEncryption encryptionLevel)
        {
            _logger = logger;
            _encryptionLevel = encryptionLevel;
            _mqtt = new MqttClientProtocol(logger, socket);
            _mqtt.ConnectComplete += MqttOnConnectComplete;
            _mqtt.SubscribeComplete += MqttOnSubscribeComplete;
            _mqtt.SendMessageComplete += MqttOnOperationComplete;
            _mqtt.NetworkError += MqttOnNetworkError;
            _mqtt.PublishReceived += MqttOnPublishReceived;
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
        /// Create a subscriber client on the specified topic.
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        public SubscriptionClient CreateSubscription(SubscriptionItem subscription)
        {
            var subClient = new SubscriptionClient(_mqtt, subscription, _clientUid);
            return subClient;
        }

        /// <summary>
        /// Connect to a broker.
        /// </summary>
        /// <param name="clientId">The MQTT client ID</param>
        /// <param name="ipOrHost">An IP address or host name</param>
        /// <returns>MqttConnectAckMessage representing the message returned from the broker</returns>
        public IAsyncOperation<MqttConnectAckMessage> ConnectAsync(string clientId, string ipOrHost)
        {
            int port = IsEncrypted ? MqttProtocolInformation.Settings.SecurePort : MqttProtocolInformation.Settings.Port;
            var bldr = new MqttConnectMessageBuilder
            {
                ClientId = clientId
            };
            return ConnectWithMessageAsync(bldr, ipOrHost, port);
        }

        /// <summary>
        /// Connect to a broker.
        /// </summary>
        /// <param name="clientId">The MQTT client ID</param>
        /// <param name="ipOrHost">An IP address or host name</param>
        /// <param name="port">The port number to use. Overrides default port in settings</param>
        /// <returns>MqttConnectAckMessage representing the message returned from the broker</returns>
        public IAsyncOperation<MqttConnectAckMessage> ConnectAsync(string clientId, string ipOrHost, int port)
        {
            var bldr = new MqttConnectMessageBuilder
            {
                ClientId = clientId
            };
            return ConnectWithMessageAsync(bldr, ipOrHost, port);
        }

        /// <summary>
        /// Connect to a broker using a message builder instance.
        /// </summary>
        /// <param name="msgBuilder">A message builder instance with advanced connection parameters</param>
        /// <param name="ipOrHost">An IP address or host name</param>
        /// <returns>MqttConnectAckMessage representing the message returned from the broker</returns>
        public IAsyncOperation<MqttConnectAckMessage> ConnectWithMessageAsync(MqttConnectMessageBuilder msgBuilder, string ipOrHost)
        {
            int port = IsEncrypted ? MqttProtocolInformation.Settings.SecurePort : MqttProtocolInformation.Settings.Port;
            return ConnectWithMessageAsync(msgBuilder, ipOrHost, port);
        }

        /// <summary>
        /// Connect to a broker using a message builder instance.
        /// </summary>
        /// <param name="msgBuilder">A message builder instance with advanced connection parameters</param>
        /// <param name="ipOrHost">An IP address or host name</param>
        /// <param name="port">The port number to use. Overrides default port in settings</param>
        /// <returns>MqttConnectAckMessage representing the message returned from the broker</returns>
        public IAsyncOperation<MqttConnectAckMessage> ConnectWithMessageAsync(MqttConnectMessageBuilder msgBuilder, string ipOrHost, int port)
        {
            var tcs = new TaskCompletionSource<MqttConnectAckMessage>();
            _mqtt.ConnectAsync(msgBuilder, ipOrHost, port, _encryptionLevel, tcs);
            return tcs.Task.AsAsyncOperation();
        }

        public IAsyncAction DisconnectAsync()
        {
            var tcs = new TaskCompletionSource<object>();
            _mqtt.DisconnectAsync(tcs, _clientUid);
            return tcs.Task.AsAsyncAction();
        }

        public IAsyncAction PublishStringAsync(string topic, string payload)
        {
            // Encode the string into a byte array
            byte[] utf8Str = Encoding.UTF8.GetBytes(payload);

            return PublishAsync(topic, utf8Str);
        }

        public IAsyncAction PublishStringAsync(string topic, string payload, QualityOfService qos, int messageId, bool retain)
        {
            // Encode the string into a byte array
            byte[] utf8Str = Encoding.UTF8.GetBytes(payload);

            return PublishAsync(topic, utf8Str, qos, messageId, retain);
        }

        public IAsyncAction PublishUIntAsync(string topic, UInt16 payload)
        {
            // Encode the uint into a byte array
            int pos = 0;
            var payloadBytes = new byte[2];
            Frame.EncodeInt16(payload, payloadBytes, ref pos);

            return PublishAsync(topic, payloadBytes);
        }

        public IAsyncAction PublishUIntAsync(string topic, UInt16 payload, QualityOfService qos, int messageId, bool retain)
        {
            // Encode the uint into a byte array
            int pos = 0;
            var payloadBytes = new byte[2];
            Frame.EncodeInt16(payload, payloadBytes, ref pos);

            return PublishAsync(topic, payloadBytes, qos, messageId, retain);
        }

        public IAsyncAction PublishAsync(
            string topic,
            [ReadOnlyArray]
            byte[] payload
            )
        {
            return PublishAsync(topic, payload, QualityOfService.AtMostOnce, 1, false);
        }

        public IAsyncAction PublishAsync(
            string topic,
            [ReadOnlyArray]
            byte[] payload,
            QualityOfService qos,
            int messageId,
            bool retain
            )
        {
            var msgBuilder = new MqttPublishMessageBuilder
            {
                QualityOfService = qos,
                MessageId = messageId,
                Retain = retain,
                TopicName = topic,
                Payload = payload
            };

            return SendMessageAsync(msgBuilder);
        }

        public IAsyncAction PingAsync()
        {
            var msgBuilder = new MqttPingRequestMessageBuilder();
            return SendMessageAsync(msgBuilder);
        }

        public void Dispose()
        {
            _mqtt.Dispose();
            ActiveClientCollection.Instance.RemoveClient(this);
        }

        private void MqttOnPublishReceived(object sender, MqttNetEventArgs args)
        {
            // For this platform, only publishes not handled by a subscription client will end up here
            if (args.Message.MessageType == MessageType.Publish)
            {
                if (UnhandledPublishReceived != null)
                {
                    UnhandledPublishReceived(this, new MqttPublishMessageEventArgs
                    {
                        Exception = args.Exception,
                        Message = args.Message as MqttPublishMessage
                    });
                }
            }
        }

        private void MqttOnNetworkError(object sender, MqttMessageEventArgs args)
        {
            if (NetworkError != null)
            {
                NetworkError(this, args);
            }
        }

        private void MqttOnConnectComplete(object sender, MqttNetEventArgs args)
        {
            var t = args.EventData as TaskCompletionSource<MqttConnectAckMessage>;
            if (t == null)
            {
                return;
            }

            if (args.Exception == null)
            {
                _clientUid = args.ClientUid;
                t.TrySetResult(args.Message as MqttConnectAckMessage);
            }
            else
            {
                t.TrySetException(args.Exception);
            }
        }

        private void MqttOnSubscribeComplete(object sender, MqttNetEventArgs args)
        {
            var t = args.EventData as TaskCompletionSource<MqttSubscribeAckMessage>;
            if (t == null)
            {
                return;
            }

            if (args.Exception == null)
            {
                t.TrySetResult(args.Message as MqttSubscribeAckMessage);
            }
            else
            {
                t.TrySetException(args.Exception);
            }
        }

        private void MqttOnOperationComplete(object sender, MqttNetEventArgs args)
        {
            var t = args.EventData as TaskCompletionSource<object>;
            if (t == null)
            {
                return;
            }

            if (args.Exception == null)
            {
                t.TrySetResult(null);
            }
            else
            {
                t.TrySetException(args.Exception);
            }
        }

        private IAsyncAction SendMessageAsync(IMqttMessageBuilder msgBuilder)
        {
            var tcs = new TaskCompletionSource<object>();
            _mqtt.SendMessageAsync(msgBuilder, tcs, _clientUid);
            return tcs.Task.AsAsyncAction();
        }
    }
}
