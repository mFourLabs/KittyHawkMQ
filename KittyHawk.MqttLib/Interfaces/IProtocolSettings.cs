

#if (MF_FRAMEWORK_VERSION_V4_3 || MF_FRAMEWORK_VERSION_V4_2)
using System.Collections;
#endif

namespace KittyHawk.MqttLib.Interfaces
{
    public interface IProtocolSettings
    {
        /// <summary>
        /// Port to use for non-ssl connections if not otherwise specified. Default is 1883.
        /// </summary>
        int Port { get; set; }

        /// <summary>
        /// Port to use for ssl connections if not otherwise specified. Default is 8883.
        /// </summary>
        int SecurePort { get; set; }

        /// <summary>
        /// Maximum size of an MQTT message. Default is spec max of 256MB.
        /// </summary>
        int MaxMessageSize { get; set; }

        /// <summary>
        /// Keep alive time in seconds. This is the maximum time interval between messages received from a client
        /// before the server considers it disconnected. (Note: server will actually wait 1.5X this value).
        /// </summary>
        int KeepAliveTime { get; set; }

        /// <summary>
        /// The number of seconds to wait for a reponse from an MQTT message that expects a response. After this
        /// time, a NetworkError event will be thrown.
        /// </summary>
        int NetworkTimeout { get; set; }

        /// <summary>
        /// Maximum number of times to retry sending a message. The actual number of retries could be less depending
        /// upon the KeepAliveTime.
        /// </summary>
        int MaxRetryCount { get; set; }

#if (MF_FRAMEWORK_VERSION_V4_3 || MF_FRAMEWORK_VERSION_V4_2)
        /// <summary>
        /// Host file entries for the .NET Micro Framework. Each item is a DictionaryEntry in the format
        /// Key=host name, Value=IP address
        /// </summary>
        Hashtable Hosts { get; }
#endif
    }
}
