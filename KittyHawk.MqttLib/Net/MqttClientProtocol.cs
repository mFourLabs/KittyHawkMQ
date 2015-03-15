
using System;
#if !(MF_FRAMEWORK_VERSION_V4_3 || MF_FRAMEWORK_VERSION_V4_2)
using System.Threading.Tasks;
#endif
using KittyHawk.MqttLib.Client;
using KittyHawk.MqttLib.Collections;
using KittyHawk.MqttLib.Messages;
using KittyHawk.MqttLib.Plugins.Logging;
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Utilities;

namespace KittyHawk.MqttLib.Net
{
    internal class MqttClientProtocol : MqttProtocolBase
    {
        // Used to send ocassional Ping messages to remote device
        private readonly ThreadSafeTimeoutTimer _keepAliveTimer;

        // Saves all client topic subscriptions
        private readonly AutoExpandingArray _subscriptionClients = new AutoExpandingArray();
        private string _clientUid;

        public MqttClientProtocol(ILogger logger, ISocketAdapter socket) :
            base(logger, socket)
        {
            _keepAliveTimer = new ThreadSafeTimeoutTimer(MqttProtocolInformation.Settings.KeepAliveTime);
            _keepAliveTimer.Timeout += KeepAliveTimerOnTimeout;
        }

        public void AddSubscriptionClient(SubscriptionClient subClient)
        {
            lock (_subscriptionClients)
            {
                _subscriptionClients.Add(subClient);
            }
        }

        public void RemoveSubscriptionClient(SubscriptionClient subClient)
        {
            lock (_subscriptionClients)
            {
                _subscriptionClients.Remove(subClient);
            }
        }

        protected override void OnTcpConnectAsyncCompleted(SocketEventArgs eventArgs, object eventData)
        {
            if (eventArgs.SocketException == null)
            {
                _clientUid = eventArgs.ClientUid;
                // In case the client changed global settings, refresh the timeouts on every connect
                _keepAliveTimer.Reset(MqttProtocolInformation.Settings.KeepAliveTime);
            }
        }

        protected override void OnSendMessageAsyncCompleted(string clientUid, IMqttMessage message, Exception exception)
        {
            MessageType messageType = message.MessageType;

            if (messageType == MessageType.Connect && exception == null)
            {
                _keepAliveTimer.Start();
            }
            else
            {
                _keepAliveTimer.Reset();
            }
        }

        protected override void OnPublishReceived(MqttNetEventArgs args)
        {
            bool handled = false;
            lock (_subscriptionClients)
            {
                // Send to subscriber clients first
#if !(MF_FRAMEWORK_VERSION_V4_3 || MF_FRAMEWORK_VERSION_V4_2 || WINDOWS_PHONE)
                Parallel.For(0, _subscriptionClients.Count, i =>
#else
                for (int i = 0; i < _subscriptionClients.Count; i++)
#endif
                {
                    var client = (SubscriptionClient)_subscriptionClients.GetAt(i);
                    try
                    {
                        bool clientHandled = client.NotifyPublishReceived(args.Message as MqttPublishMessage);
                        handled = handled | clientHandled;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException("Protocol", LogLevel.Error, "Exception occured while sending publish event to subscriber.", ex);
                    }
                }
#if !(MF_FRAMEWORK_VERSION_V4_3 || MF_FRAMEWORK_VERSION_V4_2 || WINDOWS_PHONE)
                );
#endif
            }

            if (!handled)
            {
                base.OnPublishReceived(args);
            }
        }

        public override void CloseConnection(string clientUid)
        {
            _keepAliveTimer.Stop();
            base.CloseConnection(clientUid);
        }

        private void KeepAliveTimerOnTimeout(object sender, object data)
        {
            Logger.LogMessage("Protocol", LogLevel.Verbose, "Keep alive timer sending PingReq. KeepAliveTimer instance=" + _keepAliveTimer.GetHashCode());
            var msgBuilder = new MqttPingRequestMessageBuilder();
            SendMessageAsync(msgBuilder, null, _clientUid);
        }
    }
}
