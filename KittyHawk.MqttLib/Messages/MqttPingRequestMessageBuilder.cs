
using KittyHawk.MqttLib.Interfaces;

namespace KittyHawk.MqttLib.Messages
{
    public sealed class MqttPingRequestMessageBuilder : IMqttMessageBuilder
    {
        private readonly MqttMessageBuilderBase _bldr;

        public MqttPingRequestMessageBuilder()
        {
            _bldr = new MqttMessageBuilderBase();
            _bldr.MessageType = MessageType.PingReq;
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
            return MqttPingRequestMessage.InternalDeserialize(buffer);
        }

        #endregion

        private int CalcMessageLength()
        {
            return 0;
        }

        private void PopulateBuffer(byte[] buffer, int pos)
        {
            // No payload
        }
    }
}
