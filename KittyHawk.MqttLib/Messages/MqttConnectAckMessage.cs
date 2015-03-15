
using System;
using KittyHawk.MqttLib.Interfaces;

namespace KittyHawk.MqttLib.Messages
{
    public sealed class MqttConnectAckMessage : IMqttMessage
    {
        private readonly MqttMessageBase _msg;

        internal static MqttConnectAckMessage InternalDeserialize(byte[] buffer)
        {
            var msg = new MqttConnectAckMessage();
            msg._msg.MsgBuffer = buffer;
            msg.ReadVariableHeader();
            return msg;
        }

        internal MqttConnectAckMessage()
        {
            _msg = new MqttMessageBase(MessageType.ConnAck);
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

        private ConnectReturnCode _connectReturnCode;
        public ConnectReturnCode ConnectReturnCode
        {
            get
            {
                if (!_msg.VariableHeaderRead)
                {
                    ReadVariableHeader();
                }
                return _connectReturnCode;
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
                byte b1 = _msg.MsgBuffer[pos++];    // Skip past reserved header byte
                byte b2 = _msg.MsgBuffer[pos++];

                if (b2 < (byte)ConnectReturnCode.Unknown)
                {
                    _connectReturnCode = (ConnectReturnCode)b2;
                }
                else
                {
                    _connectReturnCode = ConnectReturnCode.Unknown;
                }

                _msg.VariableHeaderRead = true;
                return pos;
            }
        }
    }
}
