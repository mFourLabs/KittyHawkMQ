
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Utilities;

namespace KittyHawk.MqttLib.Messages
{
    public sealed class MqttPublishAckMessage : IMqttIdMessage
    {
        private readonly MqttMessageBase _msg;

        internal static MqttPublishAckMessage InternalDeserialize(byte[] buffer)
        {
            var msg = new MqttPublishAckMessage();
            msg._msg.MsgBuffer = buffer;
            msg.ReadVariableHeader();
            return msg;
        }

        internal MqttPublishAckMessage()
        {
            _msg = new MqttMessageBase(MessageType.PubAck);
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
                return MessageType.None;
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

        private int ReadVariableHeader()
        {
            // Variable header could potentially be read by more than one thread but we're OK with that.
            // Just trying to guard against multiple threads reading in the variables at the same time.
            lock (_msg.SyncLock)
            {
                int pos;
                int length = _msg.ReadRemainingLength(out pos);

                if (length > 1)
                {
                    _messageId = Frame.DecodeInt16(_msg.MsgBuffer, ref pos);
                }

                _msg.VariableHeaderRead = true;
                return pos;
            }
        }
    }
}
