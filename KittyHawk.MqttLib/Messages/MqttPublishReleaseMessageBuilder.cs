
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Utilities;

namespace KittyHawk.MqttLib.Messages
{
    public sealed class MqttPublishReleaseMessageBuilder : IMqttMessageBuilder
    {
        private readonly MqttMessageBuilderBase _bldr;

        public MqttPublishReleaseMessageBuilder()
        {
            _bldr = new MqttMessageBuilderBase();
            _bldr.MessageType = MessageType.PubRel;
            _bldr.QualityOfService = QualityOfService.AtLeastOnce;
        }

        private int _messageId;
        public int MessageId
        {
            get
            {
                return _messageId;
            }
            set
            {
                DataValidation.ValidateInt16(value, "MessageID");
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
            byte[] buffer = _bldr.CreateInitializedMessageBuffer(CalcMessageLength(), PopulateBuffer);
            return MqttPublishReleaseMessage.InternalDeserialize(buffer);
        }

        #endregion

        private int CalcMessageLength()
        {
            return 2;
        }

        private void PopulateBuffer(byte[] buffer, int pos)
        {
            Frame.EncodeInt16(MessageId, buffer, ref pos);
        }
    }
}
