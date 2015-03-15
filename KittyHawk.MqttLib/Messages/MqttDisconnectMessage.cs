
using System;
using KittyHawk.MqttLib.Interfaces;

namespace KittyHawk.MqttLib.Messages
{
    public sealed class MqttDisconnectMessage : IMqttMessage
    {
        private readonly MqttMessageBase _msg;

        internal static MqttDisconnectMessage InternalDeserialize(byte[] buffer)
        {
            var msg = new MqttDisconnectMessage();
            msg._msg.MsgBuffer = buffer;
            return msg;
        }

        internal MqttDisconnectMessage()
        {
            _msg = new MqttMessageBase(MessageType.Disconnect);
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
    }
}
