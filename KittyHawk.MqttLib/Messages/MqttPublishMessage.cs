
using System;
using System.Text;
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Utilities;

namespace KittyHawk.MqttLib.Messages
{
    public sealed class MqttPublishMessage : IMqttIdMessage
    {
        private readonly MqttMessageBase _msg;

        internal static MqttPublishMessage InternalDeserialize(byte[] buffer)
        {
            var msg = new MqttPublishMessage();
            msg._msg.MsgBuffer = buffer;
            msg.ReadVariableHeader();
            return msg;
        }

        internal MqttPublishMessage()
        {
            _msg = new MqttMessageBase(MessageType.Publish);
        }

        internal MqttPublishMessage Clone()
        {
            return InternalDeserialize((byte[])_msg.MsgBuffer.Clone());
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
            internal set { _msg.Retain = value; }
        }

        /// <summary>
        /// Does this message expect a response such as an ACK message?
        /// </summary>
        public MessageType ExpectedResponse
        {
            get
            {
                if (QualityOfService == QualityOfService.AtMostOnce)
                {
                    return MessageType.None;
                }
                else if (QualityOfService == QualityOfService.AtLeastOnce)
                {
                    return MessageType.PubAck;
                }
                else
                {
                    return MessageType.PubRec;
                }
            }
        }

        public byte[] Serialize()
        {
            return _msg.Serialize();
        }

        #endregion

        public object Tag { get; set; }

        private string _topicName;
        public string TopicName
        {
            get
            {
                if (!_msg.VariableHeaderRead)
                {
                    ReadVariableHeader();
                }
                return _topicName;
            }
        }

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

        private int _payloadIndex = 0;
        private byte[] _payload;
        /// <summary>
        /// Gets the payload as a raw byte array.
        /// </summary>
        public byte[] Payload
        {
            get
            {
                if (!_msg.PayloadRead)
                {
                    ReadPayload();
                }
                return _payload;
            }
        }

        /// <summary>
        /// Returns the payload as a string. Note: this assumes the payload represents MQTT encoded string data.
        /// </summary>
        public string StringPayload
        {
            get
            {
                if (!_msg.VariableHeaderRead)
                {
                    ReadVariableHeader();
                }

                var sb = new StringBuilder();

                var encoder = new UTF8Encoding();
                char[] chars = encoder.GetChars(_msg.MsgBuffer, _payloadIndex, _msg.MsgBuffer.Length - _payloadIndex);
                sb.Append(chars);

                return sb.ToString();
            }
        }

        /// <summary>
        /// Returns the payload as an Int16. Note: this assumes the payload represents MQTT encoded Int16 data.
        /// </summary>
        public int Int16Payload
        {
            get
            {
                if (!_msg.VariableHeaderRead)
                {
                    ReadVariableHeader();
                }

                int pos = _payloadIndex;
                return Frame.DecodeInt16(_msg.MsgBuffer, ref pos);
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

                _topicName = Frame.DecodeString(_msg.MsgBuffer, ref pos);

                if (QualityOfService == QualityOfService.AtLeastOnce || QualityOfService == QualityOfService.ExactlyOnce)
                {
                    _messageId = Frame.DecodeInt16(_msg.MsgBuffer, ref pos);
                }

                _msg.VariableHeaderRead = true;
                _payloadIndex = pos;
                return pos;
            }
        }

        private void ReadPayload()
        {
            lock (_msg.SyncLock)
            {
                if (!_msg.PayloadRead)
                {
                    ReadVariableHeader();

                    // Copy the payload
                    int length = _msg.MsgBuffer.Length - _payloadIndex;
                    _payload = new byte[length];
                    Array.Copy(_msg.MsgBuffer, _payloadIndex, _payload, 0, length);

                    _msg.PayloadRead = true;
                }
            }

        }
    }
}
