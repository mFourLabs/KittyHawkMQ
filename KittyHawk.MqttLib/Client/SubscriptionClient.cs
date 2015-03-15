using System;
using System.Diagnostics;
using System.Threading.Tasks;
using KittyHawk.MqttLib.Collections;
using KittyHawk.MqttLib.Messages;
using KittyHawk.MqttLib.Net;
using KittyHawk.MqttLib.Utilities;

namespace KittyHawk.MqttLib.Client
{
    public delegate void OnMesageHandler(MqttPublishMessage msg);
    public delegate void OnErrorHandler(Exception ex);

    public sealed class SubscriptionClient
    {
        private readonly MqttClientProtocol _mqtt;
        private readonly SubscriptionItem _subscription;
        private OnMesageHandler _callback;
        private OnErrorHandler _errorCallback;
        private bool _subscribed;
        private string _clientUid;
        private bool _disposed;

        internal SubscriptionClient(MqttClientProtocol mqttClient, SubscriptionItem subscription, string clientUid)
        {
            _mqtt = mqttClient;
            _mqtt.SubscribeComplete += MqttOnSubscribeComplete;
            _mqtt.SendMessageComplete += MqttOnOperationComplete;
            _mqtt.ConnectComplete += MqttOnConnectComplete;
            _subscription = subscription;
            _clientUid = clientUid;
            _disposed = false;
        }

        /// <summary>
        /// Gets the state of this SubscriptionClient instance. If closed, it is not receiving publish events.
        /// </summary>
        public bool IsClosed
        {
            get { return _callback == null; }
        }

        /// <summary>
        /// Gets the topic name this SubscriptionClient is bound to.
        /// </summary>
        public string TopicName
        {
            get { return _subscription.TopicName; }
        }

        /// <summary>
        /// Set the message handler to be called when events are published to this topic.
        /// </summary>
        /// <param name="callback">Method to call when publish events are received for this topic.</param>
        public void OnMessage(OnMesageHandler callback)
        {
            // OK to hook callback before we are connected (in fact, recommended)
            _callback = callback;
            _mqtt.AddSubscriptionClient(this);

            if (_mqtt.IsConnected(_clientUid))
            {
                SubscribeToTopic();
            }
        }

        /// <summary>
        /// Set the message handler to be called when errors are encountered subscribing to the topic.
        /// </summary>
        /// <param name="callback"></param>
        public void OnSubscriptionError(OnErrorHandler callback)
        {
            _errorCallback = callback;
        }

        /// <summary>
        /// Closes this SubscriptionClient and stops receiving events.
        /// </summary>
        public async void Close()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _subscribed = false;
            _callback = null;
            _mqtt.RemoveSubscriptionClient(this);

            if (_mqtt.IsConnected(_clientUid))
            {
                try
                {
                    await UnsubscribeAsync(new[] { _subscription.TopicName }, 1);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(string.Format("SubscriptionClient({0}) failed to unsubscribe from topic. {1}", TopicName, ex.Message));
                }
            }
        }

        /// <summary>
        /// Notify this subscription of a received publish event.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>True if the subscription handled the event, false otherwise.</returns>
        internal bool NotifyPublishReceived(MqttPublishMessage msg)
        {
            if (Topic.IsTopicMatch(_subscription.TopicName, msg.TopicName) && _callback != null)
            {
                _callback(msg);
                return true;
            }
            return false;
        }

        private async void SubscribeToTopic()
        {
            try
            {
                MqttSubscribeAckMessage ack = await SubscribeAsync(new[] { _subscription }, 1);
                _subscribed = true;
            }
            catch (Exception ex)
            {
                if (_errorCallback != null)
                {
                    _errorCallback(ex);
                }
            }
        }

        private void MqttOnConnectComplete(object sender, MqttNetEventArgs args)
        {
            if (!_subscribed && _callback != null && args.Exception != null)
            {
                _clientUid = args.ClientUid;
                SubscribeToTopic();
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

        private Task<MqttSubscribeAckMessage> SubscribeAsync(SubscriptionItem[] items, int messageId)
        {
            var msgBuilder = new MqttSubscribeMessageBuilder
            {
                MessageId = messageId
            };

            foreach (var item in items)
            {
                msgBuilder.Subscriptions.Add(item);
            }

            var tcs = new TaskCompletionSource<MqttSubscribeAckMessage>();
            _mqtt.SendMessageAsync(msgBuilder, tcs, _clientUid);
            return tcs.Task;
        }

        private Task UnsubscribeAsync(string[] topics, int messageId)
        {
            var msgBuilder = new MqttUnsubscribeMessageBuilder
            {
                MessageId = messageId,
                TopicNames = topics
            };

            var tcs = new TaskCompletionSource<object>();
            _mqtt.SendMessageAsync(msgBuilder, tcs, _clientUid);
            return tcs.Task;
        }
    }
}
