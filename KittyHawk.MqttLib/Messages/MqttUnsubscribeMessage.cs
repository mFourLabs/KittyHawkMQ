
using KittyHawk.MqttLib.Collections;
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Utilities;

namespace KittyHawk.MqttLib.Messages
{
    public sealed class MqttUnsubscribeMessage : IMqttIdMessage
    {
        private readonly MqttMessageBase _msg;

        internal static MqttUnsubscribeMessage InternalDeserialize(byte[] buffer)
        {
            var msg = new MqttUnsubscribeMessage();
            msg._msg.MsgBuffer = buffer;
            msg.ReadPayload();
            return msg;
        }

        internal MqttUnsubscribeMessage()
        {
            _msg = new MqttMessageBase(MessageType.Unsubscribe);
        }

        #region IMqttMessage

        public byte[] MessageBuffer
        {
            get { return _msg.MsgBuffer; }
        }

        public MessageType MessageType
        {
            get { return _msg.MessageType; }
        }

        public bool Duplicate
        {
            get { return _msg.Duplicate; }
            set { _msg.Duplicate = value; }
        }

        public int Retries { get; set; }

        public QualityOfService QualityOfService
        {
            get { return _msg.QualityOfService; }
        }

        public bool Retain
        {
            get { return _msg.Retain; }
        }

        /// <summary>
        /// Does this message expect a response such as an ACK message?
        /// </summary>
        public MessageType ExpectedResponse
        {
            get
            {
                return MessageType.UnsubAck;
            }
        }

        public byte[] Serialize()
        {
            return _msg.Serialize();
        }

        #endregion

        private int _messageId;
        public int MessageId
        {
            get
            {
                if (!_msg.VariableHeaderRead)
                {
                    ReadVariableHeader();
                }
                return _messageId;
            }
        }

        private string[] _topicNames;
        public string[] TopicNames
        {
            get
            {
                if (!_msg.PayloadRead)
                {
                    ReadPayload();
                }
                return _topicNames;
            }
        }

        private int ReadVariableHeader()
        {
            // Variable header could potentially be read by more than one thread but we're OK with that.
            // Just trying to guard against multiple threads reading in the variables at the same time.
            lock (_msg.SyncLock)
            {
                int pos;
                _msg.ReadRemainingLength(out pos);

                _messageId = Frame.DecodeInt16(_msg.MsgBuffer, ref pos);

                _msg.VariableHeaderRead = true;
                return pos;
            }
        }

        private void ReadPayload()
        {
            lock (_msg.SyncLock)
            {
                if (!_msg.PayloadRead)
                {
                    int pos = ReadVariableHeader();

                    var nameArray = new AutoExpandingArray();
                    while (pos < _msg.MsgBuffer.Length)
                    {
                        string topicName = Frame.DecodeString(_msg.MsgBuffer, ref pos);
                        nameArray.Add(topicName);
                    }

                    _topicNames = new string[nameArray.Count];
                    for (int i = 0; i < nameArray.Count; i++)
                    {
                        _topicNames[i] = (string)nameArray.GetAt(i);
                    }

                    _msg.PayloadRead = true;
                }
            }
        }
    }
}
