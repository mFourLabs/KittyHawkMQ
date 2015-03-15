using KittyHawk.MqttLib.Collections;
using KittyHawk.MqttLib.Messages;

namespace KittyHawk.MqttLib.Client
{
    internal sealed class SubscriptionClient
    {
        //private readonly MqttClient _mqtt;
        //private readonly SubscriptionItem _subscription;
        //private int _subMessageId;
        //private int _unsubMessageId;

        //public event MqttPublishMessageEventHandler OnMessage;
        //public event MqttMessageEventHandler CloseComplete;

        internal SubscriptionClient(MqttClient mqtt, SubscriptionItem subscription)
        {
        }
#if false
        public void ReceiveMessagesAsync()
        {
            _subMessageId = (new Random()).Next(Int16.MaxValue);
            _mqtt.SubscribeAsync(new[] { _subscription }, _subMessageId);    
        }

        public void CloseAsync()
        {
            if (_mqtt.IsConnected)
            {
                _mqtt.UnsubscribeAsync(new[] {_subscription.TopicName}, 1);
            }
        }
        private void FireOnMessage(MqttPublishMessage msg)
        {
            if (OnMessage != null)
            {
                OnMessage(this, new MqttPublishMessageEventArgs
                {
                    AdditionalErrorInfo = null,
                    Exception = null,
                    Message = msg
                });
            }
        }

        private void NotifyClient(MqttPublishMessage msg)
        {
            FireOnMessage(msg);
        }
#endif

        internal bool NotifyPublishReceived(MqttPublishMessage msg)
        {
            return false;
        }
    }
}
