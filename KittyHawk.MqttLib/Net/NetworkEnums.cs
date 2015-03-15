namespace KittyHawk.MqttLib.Net
{
#if (MF_FRAMEWORK_VERSION_V4_3 || MF_FRAMEWORK_VERSION_V4_2)
    /// <summary>
    /// Specified the encryption method to be used on the socket.
    /// </summary>
    public enum SocketEncryption
    {
        None,
        Tls10,
    }
#elif NETFX_CORE
    /// <summary>
    /// Specified the encryption method to be used on the socket.
    /// </summary>
    public enum SocketEncryption
    {
        None,
        Tls10,
        Tls11,
        Tls12,
    }
#elif WINDOWS_PHONE
    /// <summary>
    /// Specified the encryption method to be used on the socket.
    /// </summary>
    public enum SocketEncryption
    {
        None,
        Ssl,
    }
#else
    /// <summary>
    /// Specified the encryption method to be used on the socket.
    /// </summary>
    public enum SocketEncryption
    {
        None,
        Ssl,
        Tls10,
        Tls11,
        Tls12,
    }
#endif

    public enum ClientDisconnectedReason
    {
        ClientRequested,
        KeepAliveTimeExpired,
        FailedToRespond
    }

    public enum MessageCompletedReason
    {
        Success,
        SocketError,
        ResponseTimeout
    }
}