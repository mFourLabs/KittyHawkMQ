
using KittyHawk.MqttLib.Collections;
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Utilities;

namespace KittyHawk.MqttLib.Messages
{
    public sealed class MqttSubscribeAckMessageBuilder : IMqttMessageBuilder
    {
        private readonly MqttMessageBuilderBase _bldr;

        public MqttSubscribeAckMessageBuilder()
        {
            _bldr = new MqttMessageBuilderBase();
            _bldr.MessageType = MessageType.SubAck;
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

        private QualityOfServiceCollection _qoSLevels = new QualityOfServiceCollection();
        public QualityOfServiceCollection QoSLevels
        {
            get
            {
                return _qoSLevels;
            }
            set
            {
                _qoSLevels = value;
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
            return MqttSubscribeAckMessage.InternalDeserialize(initializedBuffer);
        }

        #endregion

        private int CalcMessageLength()
        {
            int length = 0;

            // Variable header
            length += 2;    // MessageID

            // Payload
            length += QoSLevels.Count;

            return length;
        }

        private void PopulateBuffer(byte[] buffer, int pos)
        {
            // Variable header
            Frame.EncodeInt16(MessageId, buffer, ref pos);

            // Payload
            for (int i = 0; i < QoSLevels.Count; i++)
            {
                var item = QoSLevels.GetAt(i);
                buffer[pos] = (byte)item;
                pos++;
            }
        }
    }
}
