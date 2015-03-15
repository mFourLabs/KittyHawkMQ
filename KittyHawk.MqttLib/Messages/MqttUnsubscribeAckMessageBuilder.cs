
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Utilities;

namespace KittyHawk.MqttLib.Messages
{
    public sealed class MqttUnsubscribeAckMessageBuilder : IMqttMessageBuilder
    {
        private readonly MqttMessageBuilderBase _bldr;

        public MqttUnsubscribeAckMessageBuilder()
        {
            _bldr = new MqttMessageBuilderBase();
            _bldr.MessageType = MessageType.UnsubAck;
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
            return MqttUnsubscribeAckMessage.InternalDeserialize(initializedBuffer);
        }

        #endregion

        private int CalcMessageLength()
        {
            int length = 0;

            // Variable header
            length += 2;    // MessageID

            return length;
        }

        private void PopulateBuffer(byte[] buffer, int pos)
        {
            // Variable header
            Frame.EncodeInt16(MessageId, buffer, ref pos);
        }
    }
}
