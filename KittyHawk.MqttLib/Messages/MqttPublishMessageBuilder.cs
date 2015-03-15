
using System;
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Utilities;

namespace KittyHawk.MqttLib.Messages
{
    public sealed class MqttPublishMessageBuilder : IMqttMessageBuilder
    {
        private readonly MqttMessageBuilderBase _bldr;

        public MqttPublishMessageBuilder()
        {
            _bldr = new MqttMessageBuilderBase();
            _bldr.MessageType = MessageType.Publish;
            QualityOfService = QualityOfService.AtMostOnce;
            Retain = false;
            Payload = new byte[0];  // Start payload with 0-length array, not null array
        }

        private string _topicName;
        public string TopicName
        {
            get
            {
                return _topicName;
            }
            set
            {
                DataValidation.ValidateStringAndLength(value, "TopicName");
                _topicName = value;
            }
        }

        private int _messageId = 1; // Default to 1 since 0 is invalid
        public int MessageId
        {
            get
            {
                return _messageId;
            }
            set
            {
                DataValidation.ValidateMessageId(value);
                _messageId = value;
            }
        }

        public byte[] Payload { get; set; }

        #region IMqttMessageBuilder

        public MessageType MessageType
        {
            get { return _bldr.MessageType; }
        }

        public bool Duplicate
        {
            get
            {
                return _bldr.Duplicate;
            }
            set
            {
                _bldr.Duplicate = value;
            }
        }

        public QualityOfService QualityOfService
        {
            get
            {
                return _bldr.QualityOfService;
            }
            set
            {
                _bldr.QualityOfService = value;
            }
        }

        public bool Retain
        {
            get
            {
                return _bldr.Retain;
            }
            set
            {
                _bldr.Retain = value;
            }
        }

        public IMqttMessage GetMessage()
        {
            byte[] initializedBuffer = _bldr.CreateInitializedMessageBuffer(CalcMessageLength(), PopulateBuffer);
            return MqttPublishMessage.InternalDeserialize(initializedBuffer);
        }

        #endregion

        private int CalcMessageLength()
        {
            int length = 0;

            // Variable header
            DataValidation.ValidateStringAndLength(TopicName, "TopicName");
            length += TopicName.Length + 2;

            if (QualityOfService == QualityOfService.AtLeastOnce || QualityOfService == QualityOfService.ExactlyOnce)
            {
                length += 2;
            }

            // Payload
            length += Payload.Length;

            return length;
        }

        private void PopulateBuffer(byte[] buffer, int pos)
        {
            Frame.EncodeString(TopicName, buffer, ref pos);

            if (QualityOfService == QualityOfService.AtLeastOnce || QualityOfService == QualityOfService.ExactlyOnce)
            {
                Frame.EncodeInt16(MessageId, buffer, ref pos);
            }

            Array.Copy(Payload, 0, buffer, pos, Payload.Length);
        }
    }
}
