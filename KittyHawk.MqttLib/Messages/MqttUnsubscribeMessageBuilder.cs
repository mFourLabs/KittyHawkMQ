
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Utilities;

namespace KittyHawk.MqttLib.Messages
{
    public sealed class MqttUnsubscribeMessageBuilder : IMqttMessageBuilder
    {
        private readonly MqttMessageBuilderBase _bldr;

        public MqttUnsubscribeMessageBuilder()
        {
            _bldr = new MqttMessageBuilderBase();
            _bldr.MessageType = MessageType.Unsubscribe;
            QualityOfService = QualityOfService.AtLeastOnce;
            TopicNames = new string[0];
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

        public string[] TopicNames { get; set; }

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
            return MqttUnsubscribeMessage.InternalDeserialize(initializedBuffer);
        }

        #endregion

        private int CalcMessageLength()
        {
            int length = 0;

            // Variable header
            length += 2;    // MessageID

            // Payload
            for (int i = 0; i < TopicNames.Length; i++)
            {
                length += 2;    // String length number
                length += TopicNames[i].Length;  // String length
            }

            return length;
        }

        private void PopulateBuffer(byte[] buffer, int pos)
        {
            // Variable header
            Frame.EncodeInt16(MessageId, buffer, ref pos);

            // Payload
            for (int i = 0; i < TopicNames.Length; i++)
            {
                var item = TopicNames[i];
                Frame.EncodeString(item, buffer, ref pos);
            }
        }
    }
}
