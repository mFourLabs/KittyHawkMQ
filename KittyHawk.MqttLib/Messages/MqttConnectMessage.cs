
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Utilities;

namespace KittyHawk.MqttLib.Messages
{
    public sealed class MqttConnectMessage : IMqttMessage
    {
        private readonly MqttMessageBase _msg;

        internal static MqttConnectMessage InternalDeserialize(byte[] buffer)
        {
            var msg = new MqttConnectMessage();
            msg._msg.MsgBuffer = buffer;
            msg.ReadPayload();
            return msg;
        }

        internal MqttConnectMessage()
        {
            _msg = new MqttMessageBase(MessageType.Connect);
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
                return MessageType.ConnAck;
            }
        }

        public byte[] Serialize()
        {
            return _msg.Serialize();
        }

        #endregion

        private string _protocolName;
        public string ProtocolName
        {
            get
            {
                if (!_msg.VariableHeaderRead)
                {
                    ReadVariableHeader();
                }
                return _protocolName;
            }
        }

        private int _protocolVersion;
        public int ProtocolVersion
        {
            get
            {
                if (!_msg.VariableHeaderRead)
                {
                    ReadVariableHeader();
                }
                return _protocolVersion;
            }
        }

        private byte _connectFlags;
        public bool CleanSession
        {
            get
            {
                if (!_msg.VariableHeaderRead)
                {
                    ReadVariableHeader();
                }
                return (_connectFlags & (byte)ConnectFlag.CleanSession) > 0;
            }
        }

        public bool WillFlag
        {
            get
            {
                if (!_msg.VariableHeaderRead)
                {
                    ReadVariableHeader();
                }
                return (_connectFlags & (byte)ConnectFlag.WillFlag) > 0;
            }
        }

        public QualityOfService WillQualityOfService
        {
            get
            {
                if (!_msg.VariableHeaderRead)
                {
                    ReadVariableHeader();
                }
                var qosSection = (byte)(_connectFlags & (byte)(ConnectFlag.WillQos0 | ConnectFlag.WillQos1));
                return (QualityOfService)(qosSection >> Frame.GetBitPosition((byte)ConnectFlag.WillQos0));
            }
        }

        public bool WillRetain
        {
            get
            {
                if (!_msg.VariableHeaderRead)
                {
                    ReadVariableHeader();
                }
                return (_connectFlags & (byte)ConnectFlag.WillRetain) > 0;
            }
        }

        public bool PasswordFlag
        {
            get
            {
                if (!_msg.VariableHeaderRead)
                {
                    ReadVariableHeader();
                }
                return (_connectFlags & (byte)ConnectFlag.PasswordFlag) > 0;
            }
        }

        public bool UserNameFlag
        {
            get
            {
                if (!_msg.VariableHeaderRead)
                {
                    ReadVariableHeader();
                }
                return (_connectFlags & (byte)ConnectFlag.UserNameFlag) > 0;
            }
        }

        private int _keepAliveTime;
        public int KeepAliveTime
        {
            get
            {
                if (!_msg.VariableHeaderRead)
                {
                    ReadVariableHeader();
                }
                return _keepAliveTime;
            }
        }

        private string _clientId;
        public string ClientId
        {
            get
            {
                if (!_msg.PayloadRead)
                {
                    ReadPayload();
                }
                return _clientId;
            }
        }

        private string _willTopic;
        public string WillTopic
        {
            get
            {
                if (!_msg.PayloadRead)
                {
                    ReadPayload();
                }
                return _willTopic;
            }
        }

        private string _willMessage;
        public string WillMessage
        {
            get
            {
                if (!_msg.PayloadRead)
                {
                    ReadPayload();
                }
                return _willMessage;
            }
        }

        private string _userName;
        public string UserName
        {
            get
            {
                if (!_msg.PayloadRead)
                {
                    ReadPayload();
                }
                return _userName;
            }
        }

        private string _password;
        public string Password
        {
            get
            {
                if (!_msg.PayloadRead)
                {
                    ReadPayload();
                }
                return _password;
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
                _protocolName = Frame.DecodeString(_msg.MsgBuffer, ref pos);
                _protocolVersion = _msg.MsgBuffer[pos++];
                _connectFlags = _msg.MsgBuffer[pos++];
                _keepAliveTime = Frame.DecodeInt16(_msg.MsgBuffer, ref pos);

                _msg.VariableHeaderRead = true;
                return pos;
            }
        }

        private void ReadPayload()
        {
            lock (_msg.SyncLock)
            {
                int pos = ReadVariableHeader();
                _clientId = Frame.DecodeString(_msg.MsgBuffer, ref pos);

                if (WillFlag)
                {
                    _willTopic = Frame.DecodeString(_msg.MsgBuffer, ref pos);
                    _willMessage = Frame.DecodeString(_msg.MsgBuffer, ref pos);
                }

                if (UserNameFlag)
                {
                    _userName = Frame.DecodeString(_msg.MsgBuffer, ref pos);
                }

                if (PasswordFlag)
                {
                    _password = Frame.DecodeString(_msg.MsgBuffer, ref pos);
                }

                _msg.PayloadRead = true;
            }
        }
    }
}
