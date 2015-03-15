
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Utilities;

namespace KittyHawk.MqttLib.Messages
{
    public sealed class MqttConnectMessageBuilder : IMqttMessageBuilder
    {
        private readonly MqttMessageBuilderBase _bldr;

        public MqttConnectMessageBuilder()
        {
            _bldr = new MqttMessageBuilderBase();
            _bldr.MessageType = MessageType.Connect;

            // Defaults
            KeepAliveTime = MqttProtocolInformation.Settings.KeepAliveTime;
            CleanSession = true;
        }

        public bool CleanSession { get; set; }
        public QualityOfService WillQualityOfService { get; set; }
        public bool WillRetainFlag { get; set; }

        private int _keepAliveTime;
        public int KeepAliveTime
        {
            get
            {
                return _keepAliveTime;
            }
            set
            {
                DataValidation.ValidateInt16(value, "KeepAliveTime");
                _keepAliveTime = value;
            }
        }

        private string _clientId;
        public string ClientId
        {
            get
            {
                return _clientId;
            }
            set
            {
                DataValidation.ValidateClientId(value);
                _clientId = value;
            }
        }

        private string _willTopic;
        public string WillTopic
        {
            get
            {
                return _willTopic;
            }
            set
            {
                if (value != null && value.Length > 0)
                {
                    DataValidation.ValidateStringAndLength(value, "Will Topic");
                    WillFlag = true;
                }
                else
                {
                    WillFlag = false;
                }
                _willTopic = value;
            }
        }

        private string _willMessage;
        public string WillMessage
        {
            get
            {
                return _willMessage;
            }
            set
            {
                if (value != null && value.Length > 0)
                {
                    DataValidation.ValidateString(value, "Will Message");
                }
                _willMessage = value;
            }
        }

        private string _userName;
        public string UserName
        {
            get
            {
                return _userName;
            }
            set
            {
                if (value != null && value.Length > 0)
                {
                    DataValidation.ValidateString(value, "User Name");
                    UserNameFlag = true;
                }
                else
                {
                    UserNameFlag = false;
                }
                _userName = value;
            }
        }

        private string _password;
        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                if (value != null && value.Length > 0)
                {
                    DataValidation.ValidateString(value, "Password");
                    PasswordFlag = true;
                }
                else
                {
                    PasswordFlag = false;
                }
                _password = value;
            }
        }

        // Properties that are set automatically based on other properties
        public bool WillFlag { get; private set; }
        public bool PasswordFlag { get; private set; }
        public bool UserNameFlag { get; private set; }

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
            return MqttConnectMessage.InternalDeserialize(initializedBuffer);
        }

        #endregion

        private int CalcMessageLength()
        {
            int length = 0;

            // Variable header

            // Protocol Name
            length += MqttProtocolInformation.ProtocolName.Length + 2;
            // Protocol Version + MqttConnectAsync Flags + Keep Alive Time
            length += 4;

            // Payload

            DataValidation.ValidateClientId(ClientId);
            length += ClientId.Length + 2;

            if (WillFlag)
            {
                DataValidation.ValidateStringAndLength(WillTopic, "Will Topic");
                length += WillTopic.Length + 2;
                length += (WillMessage == null ? 0 : WillMessage.Length) + 2;
            }

            if (UserNameFlag)
            {
                if (UserName != null && UserName.Length > 0)
                {
                    DataValidation.ValidateString(UserName, "User Name");
                    length += UserName.Length + 2;
                }
            }

            if (PasswordFlag)
            {
                if (Password != null && Password.Length > 0)
                {
                    DataValidation.ValidateString(Password, "Password");
                    length += Password.Length + 2;
                }
            }

            return length;
        }

        private void PopulateBuffer(byte[] buffer, int pos)
        {
            Frame.EncodeString(MqttProtocolInformation.ProtocolName, buffer, ref pos);
            buffer[pos++] = (byte)MqttProtocolInformation.MajorVersion;

            byte connectFlags = 0x0;
            connectFlags |= (byte)(CleanSession ? ConnectFlag.CleanSession : 0x00);
            connectFlags |= (byte)(WillFlag ? ConnectFlag.WillFlag : 0x00);
            connectFlags |= (byte)((byte)WillQualityOfService << Frame.GetBitPosition((byte)ConnectFlag.WillQos0));
            connectFlags |= (byte)(WillRetainFlag ? ConnectFlag.WillRetain : 0x00);
            connectFlags |= (byte)(PasswordFlag ? ConnectFlag.PasswordFlag : 0x00);
            connectFlags |= (byte)(UserNameFlag ? ConnectFlag.UserNameFlag : 0x00);
            buffer[pos++] = connectFlags;

            Frame.EncodeInt16(KeepAliveTime, buffer, ref pos);
            Frame.EncodeString(ClientId, buffer, ref pos);

            if (WillFlag)
            {
                Frame.EncodeString(WillTopic, buffer, ref pos);
                Frame.EncodeString(WillMessage ?? "", buffer, ref pos);
            }

            if (UserNameFlag)
            {
                if (UserName != null && UserName.Length > 0)
                {
                    Frame.EncodeString(UserName, buffer, ref pos);
                }
            }

            if (PasswordFlag)
            {
                if (Password != null && Password.Length > 0)
                {
                    Frame.EncodeString(Password, buffer, ref pos);
                }
            }
        }
    }
}
