
using System;
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Messages;
using KittyHawk.MqttLib.Utilities;

namespace KittyHawk.MqttLib.Collections
{
    internal delegate void MessageStoreEventHandler(object sender, MessageStoreEventArgs args);

    internal class MessageStoreEventArgs
    {
        public object EventData { get; set; }
        public IMqttMessage FailedMessage { get; set; }
        public int Retries { get; set; }
        public string ClientUid { get; set; }
    }

    /// <summary>
    /// This is where all pending messages expecting a response are stored. Each message is given a built-in timer that
    /// fires the MessageTimeout event if not removed from the store ahead of the timer timeout. If the timer fires,
    /// the message is automatically remove from the store.
    /// </summary>
    internal class MessageStore
    {
        private readonly object _lock = new object();
        private readonly AutoExpandingArray _pendingMessageData = new AutoExpandingArray();

        private class MessageStoreData : IDisposable
        {
            public IMqttMessage Message { get; set; } 
            public int MessageId { get; set; }
            public object EventData { get; set; }
            public TimeoutTimer ResponseTimer { get; set; }
            public int Retries { get; set; }
            public string ClientUid { get; set; }

            public void Dispose()
            {
                if (ResponseTimer != null)
                {
                    ResponseTimer.Dispose();
                    ResponseTimer = null;
                }
            }
        }

        /// <summary>
        /// Fired when a message in the store has timed out.
        /// </summary>
        public event MessageStoreEventHandler MessageTimeout;

        public int Count
        {
            get { return _pendingMessageData.Count; }
        }

        /// <summary>
        /// Saves a message into the store and begins the response timer. If the message is not pulled from the store
        /// before the response timer ends, the MessageTimeout event is fired.
        /// </summary>
        /// <param name="message">The message sent to the remote endpoint. I.e. Publish, Subscribe, etc.</param>
        /// <param name="eventData">Client defined data associated with the message.</param>
        /// <param name="clientUid">The socket connection context.</param>
        public void Add(IMqttMessage message, object eventData, string clientUid)
        {
            var storeData = new MessageStoreData
            {
                Message = message,
                MessageId = MqttMessageBase.GetMessageIdOrDefault(message),
                EventData = eventData,
                ResponseTimer = new TimeoutTimer(MqttProtocolInformation.Settings.NetworkTimeout),
                ClientUid = clientUid
            };

            lock (_lock)
            {
                _pendingMessageData.Add(storeData);
            }

            storeData.ResponseTimer.TimeOutData = storeData;
            storeData.ResponseTimer.Timeout += ResponseTimerOnTimeout;
            storeData.ResponseTimer.Start();
        }

        /// <summary>
        /// Finds the original message for the given response, pulls it out of the store, stops the response timeout
        /// and returns the client defined event data
        /// </summary>
        /// <param name="responseMessage">The expected response message for the stored message. I.e. PubAck, SubAck, etc.</param>
        /// <param name="clientUid">The connection context.</param>
        /// <returns></returns>
        public object Remove(IMqttMessage responseMessage, string clientUid)
        {
            return Remove(responseMessage.MessageType, MqttMessageBase.GetMessageIdOrDefault(responseMessage), clientUid);
        }

        /// <summary>
        /// Finds the original message for the given response, pulls it out of the store, stops the response timeout
        /// and returns the client defined event data
        /// </summary>
        /// <param name="responseMessageType">The expected response message type the stored message. I.e. PubAck, SubAck, etc.</param>
        /// <param name="messageId">The expected response message ID the stored message.</param>
        /// <param name="clientUid">The connection context.</param>
        /// <returns></returns>
        public object Remove(MessageType responseMessageType, int messageId, string clientUid)
        {
            if (responseMessageType == MessageType.None)
            {
                return null;
            }

            lock (_lock)
            {
                int index = FindMessageIndex(responseMessageType, messageId, clientUid);
                if (index >= 0)
                {
                    var data = (MessageStoreData)_pendingMessageData.GetAt(index);
                    data.ResponseTimer.Stop();
                    _pendingMessageData.RemoveAt(index);
                    return data.EventData;
                }
                return null;
            }
        }

        public void Clear(string clientUid)
        {
            lock (_lock)
            {
                for (int i = 0; i < _pendingMessageData.Count; i++)
                {
                    var test = (MessageStoreData)_pendingMessageData.GetAt(i);
                    if (test.ClientUid == clientUid)
                    {
                        _pendingMessageData.RemoveAt(i);
                    }
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                // Underlying array will call IDispose on remaining items which will cancel timers.
                _pendingMessageData.Clear();
            }
        }

        private void ResponseTimerOnTimeout(object sender, object data)
        {
            var storeData = (MessageStoreData)data;
            Remove(storeData);

            if (MessageTimeout != null)
            {
                MessageTimeout(this, new MessageStoreEventArgs
                {
                    EventData = storeData.EventData,
                    FailedMessage = storeData.Message,
                    Retries = storeData.Retries,
                    ClientUid = storeData.ClientUid
                });
            }
        }

        private void Remove(MessageStoreData data)
        {
            lock (_lock)
            {
                for (int i = 0; i < _pendingMessageData.Count; i++)
                {
                    var test = (MessageStoreData)_pendingMessageData.GetAt(i);
                    if (test == data)
                    {
                        _pendingMessageData.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        private int FindMessageIndex(MessageType messageType, int messageId, string clientUid)
        {
            for (int i = 0; i < _pendingMessageData.Count; i++)
            {
                var data = (MessageStoreData)_pendingMessageData.GetAt(i);
                if (data.MessageId == messageId && data.Message.ExpectedResponse == messageType && data.ClientUid == clientUid)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
