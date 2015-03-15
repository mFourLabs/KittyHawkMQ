
using System;
#if (MF_FRAMEWORK_VERSION_V4_3 || MF_FRAMEWORK_VERSION_V4_2)
using System.Collections;
#endif
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Utilities;

namespace KittyHawk.MqttLib.Settings
{
    internal class MqttSettings : IProtocolSettings, IInternalSettings
    {
        #region IProtocolSettings

        // NETMF: These are required to be const and not static readonly members
        private const int MaxMessageSizeDefault = 268435455;    // 256MB
        private const string ProtocolNameDefault = "MQIsdp";
        private const int MajorVersionDefault = 0x3;
        private const int MinorVersionDefault = 0x1;
        private const int PortDefault = 1883;
        private const int SecurePortDefault = 8883;
        private const int KeepAliveTimeDefault = 1 * 60;        // 1 minute(s), time between pings if no data sent
        private const int NetworkTimeoutDefault = 20;           // 20 seconds, time to wait for message response before giving up
        private const int MaxRetryCountDeafult = 0;             // Number of retries is no greater than this number (but could be less depending on KeepAliveTime)

        public string ProtocolName
        {
            get
            {
                return ProtocolNameDefault;
            }
        }

        public int MajorVersion
        {
            get
            {
                return MajorVersionDefault;
            }
        }

        public int MinorVersion
        {
            get
            {
                return MinorVersionDefault;
            }
        }

        private int _port = PortDefault;
        public int Port
        {
            get
            {
                return _port;
            }
            set
            {
                _port = value;
            }
        }

        private int _securePort = SecurePortDefault;
        public int SecurePort
        {
            get
            {
                return _securePort;
            }
            set
            {
                _securePort = value;
            }
        }

        private int _maxMessageSize = MaxMessageSizeDefault;
        public int MaxMessageSize
        {
            get
            {
                return _maxMessageSize;
            }
            set
            {
                DataValidation.ValidateMaxMessageSize(value);
                _maxMessageSize = value;
            }
        }

        private int _keepAliveTime = KeepAliveTimeDefault;
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

        private int _networkTimeout = NetworkTimeoutDefault;

        public int NetworkTimeout
        {
            get
            {
                return _networkTimeout;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("NetworkTimeout must be greater than 0 seconds.");
                }
                _networkTimeout = value;
            }
        }

        private int _maxRetryCount = MaxRetryCountDeafult;

        public int MaxRetryCount
        {
            get
            {
                return _maxRetryCount;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("MaxRetryCount must be greater than or equal to 0.");
                }
                _maxRetryCount = value;
            }
        }

#if (MF_FRAMEWORK_VERSION_V4_3 || MF_FRAMEWORK_VERSION_V4_2)
        private readonly Hashtable _hosts = new Hashtable();

        public Hashtable Hosts
        {
            get { return _hosts; }
        }
#endif
        #endregion

        #region IInternalSettings

        private const int SocketReceiverThreadLoopDelayDefault = 100;

        private int _socketReceiverThreadLoopDelay = SocketReceiverThreadLoopDelayDefault;
        public int SocketReceiverThreadLoopDelay
        {
            get { return _socketReceiverThreadLoopDelay; }
            set { _socketReceiverThreadLoopDelay = value; }
        }

        #endregion
    }
}
