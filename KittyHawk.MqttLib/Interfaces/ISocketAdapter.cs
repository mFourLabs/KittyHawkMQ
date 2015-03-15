
using System;
using KittyHawk.MqttLib.Net;

namespace KittyHawk.MqttLib.Interfaces
{
    internal delegate void NetworkReceiverEventHandler(MqttNetEventArgs args);

    /// <summary>
    /// Socket interface abstraction to facilitate Windows Store/Windows Desktop/Microframework clients as well as unit testing.
    /// The clientUid is assigned to an MqttClient upon successfull connection.
    /// </summary>
    internal interface ISocketAdapter : IDisposable
    {
        /// <summary>
        /// True if this client is connected via a secure protocol.
        /// </summary>
        /// <param name="clientUid"></param>
        /// <returns></returns>
        bool IsEncrypted(string clientUid);

        /// <summary>
        /// Connect to an MQTT broker asynchronously. Result is communicated by calling the completion handler given
        /// in the SocketEventArgs.
        /// </summary>
        /// <param name="ipOrHost">IP address or host name of the broker.</param>
        /// <param name="port">Port number of the broker.</param>
        /// <param name="args">SocketEventArgs instance containing the completion callback and client id.</param>
        void ConnectAsync(string ipOrHost, int port, SocketEventArgs args);

        /// <summary>
        /// Asynchronously write the MqttMessage specified in the SocketEventArgs to the connected broker. Result is
        /// communicated by calling the completion handler in the given SocketEventArgs.
        /// </summary>
        /// <param name="args">SocketEventArgs instance containing the completion callback and client id.</param>
        void WriteAsync(SocketEventArgs args);

        /// <summary>
        /// Sets the callback to be invoked when messages are received. Note this is not an event per say, instead,
        /// set a completion handler here that will be invoked each time a message is received.
        /// </summary>
        /// <param name="handler">NetworkReceiverEventHandler instance containing the message received callback.</param>
        void OnMessageReceived(NetworkReceiverEventHandler handler);

        /// <summary>
        /// True if the specified client is currently connected.
        /// </summary>
        /// <param name="clientUid">The client id to be queried.</param>
        /// <returns></returns>
        bool IsConnected(string clientUid);

        /// <summary>
        /// Call blocks the current thread until the specified client has disconnected.
        /// </summary>
        /// <param name="clientUid"></param>
        void JoinDisconnect(string clientUid);

        /// <summary>
        /// Disconnects from the broker. Call is synchronous, the current thread is blocked until the client has
        /// disconnected.
        /// </summary>
        /// <param name="clientUid"></param>
        void Disconnect(string clientUid);
    }
}
