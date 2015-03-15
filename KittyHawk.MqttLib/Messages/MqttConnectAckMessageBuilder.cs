
using KittyHawk.MqttLib.Interfaces;

namespace KittyHawk.MqttLib.Messages
{
    public sealed class MqttConnectAckMessageBuilder : IMqttMessageBuilder
    {
        private readonly MqttMessageBuilderBase _bldr;

        public MqttConnectAckMessageBuilder()
        {
            _bldr = new MqttMessageBuilderBase();
            _bldr.MessageType = MessageType.ConnAck;
        }

        public ConnectReturnCode ConnectReturnCode { get; set; }

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
            return MqttConnectAckMessage.InternalDeserialize(buffer);
        }

        #endregion

        private int CalcMessageLength()
        {
            return 2;
        }

        private void PopulateBuffer(byte[] buffer, int pos)
        {
            buffer[pos++] = 0;  // Reserved byte
            buffer[pos++] = (byte)ConnectReturnCode;
        }
    }
}
