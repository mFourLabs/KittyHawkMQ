
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Messages;
using KittyHawk.MqttLib.Plugins.Logging;

namespace KittyHawk.MqttLib.Net
{
    internal delegate Stream GetStreamHandler(TcpClient tcpClient, SslProtocols encryption);

    internal class ConnectedClientInfo : IDisposable
    {
        public TcpClient TcpClient { get; set; }
        public int Port { get; set; }
        public Stream Stream { get; set; }
        public SslProtocols Encryption { get; set; }
        public string ClientUid { get; set; }

        private Timer _closeConnectionTimer;
        private int _timeout;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeout">Timeout time in seconds</param>
        /// <param name="disconnectCallback"></param>
        public void StartConnectionTimer(int timeout, TimerCallback disconnectCallback)
        {
            // Per spec, disconnect after 1.5X the timeout time
            _timeout = timeout*1500;
            _closeConnectionTimer = new Timer(disconnectCallback, ClientUid, _timeout, _timeout);
        }

        public void ResetTimeout()
        {
            if (_closeConnectionTimer != null)
            {
                _closeConnectionTimer.Change(_timeout, _timeout);
            }
        }

        public void Dispose()
        {
            if (_closeConnectionTimer != null)
            {
                _closeConnectionTimer.Dispose();
            }
            Stream.Close();
            TcpClient.Close();
        }
    }

    internal class iOSSocketWorker : IDisposable
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, ConnectedClientInfo> _connectedClients = new Dictionary<string, ConnectedClientInfo>();
        private CancellationTokenSource _tokenSource;
        private bool _disposed = false;

        public iOSSocketWorker(ILogger logger)
        {
            _logger = logger;

            // Start the receiver thread
            ClientReceiverThreadProc();
        }

        // Callback handlers
        private NetworkReceiverEventHandler _messageReceivedHandler;
        private ClientDisconnectedHandler _clientDisconnectedHandler;

        public void OnMessageReceived(NetworkReceiverEventHandler handler)
        {
            _messageReceivedHandler = handler;
        }

        public void OnClientTimeout(ClientDisconnectedHandler handler)
        {
            _clientDisconnectedHandler = handler;
        }

        private GetStreamHandler _getStreamHandler;

        public void OnGetStream(GetStreamHandler handler)
        {
            _getStreamHandler = handler;
        }

        public bool IsEncrypted(string clientUid)
        {
            lock (_connectedClients)
            {
                if (_connectedClients.ContainsKey(clientUid))
                {
                    return _connectedClients[clientUid].Encryption != SslProtocols.None;
                }
                return false;
            }
        }

        // ISocketAdapter helpers
        public bool IsConnected(string clientUid)
        {
            if (clientUid == null)
            {
                return false;
            }

            lock (_connectedClients)
            {
                return _connectedClients.ContainsKey(clientUid);
            }
        }

        public void ConnectTcpClient(TcpClient tcpClient, int port, SocketEncryption encryption, string connectionKey)
        {
            var encryptionLevel = SslProtocols.None;

            switch (encryption)
            {
                case SocketEncryption.None:
                    encryptionLevel = SslProtocols.None;
                    break;
                case SocketEncryption.Ssl:
                    encryptionLevel = SslProtocols.Ssl3;
                    break;
                case SocketEncryption.Tls10:
                    encryptionLevel = SslProtocols.Tls;
                    break;
                case SocketEncryption.Tls11:
                    encryptionLevel = SslProtocols.Tls11;
                    break;
                case SocketEncryption.Tls12:
                    encryptionLevel = SslProtocols.Tls12;
                    break;
            }

            lock (_connectedClients)
            {
                _logger.LogMessage("Socket", LogLevel.Verbose, string.Format("Adding new TCP client: key={0}", connectionKey));
                _connectedClients.Add(connectionKey, new ConnectedClientInfo
                {
                    TcpClient = tcpClient,
                    Port = port,
                    Encryption = encryptionLevel,
                    ClientUid = connectionKey
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="oldConnectionKey"></param>
        /// <param name="timeout">Timeout time in seconds</param>
        public void ConnectMqttClient(string uid, string oldConnectionKey, int timeout)
        {
            // If timeout == 0, we never disconnect the client due to a keep alive timer
            if (timeout == 0)
            {
                return;
            }

            lock (_connectedClients)
            {
                if (_connectedClients.ContainsKey(oldConnectionKey))
                {
                    _logger.LogMessage("Socket", LogLevel.Verbose, string.Format("Converting TCP client (key={0}) to MQTT client (ClientUID={1})", oldConnectionKey, uid));

                    var connectionInfo = _connectedClients[oldConnectionKey];
                    connectionInfo.ClientUid = uid;
                    connectionInfo.StartConnectionTimer(timeout, DisconnectOnTimeout);

                    // Now that MQTT client is officially connected, track connection under client uid instead of hashcode
                    _connectedClients.Remove(oldConnectionKey);  // remove but do not Dispose!

                    // Note: It is possible that the client was never disconnected/removed before issuing another connect request
                    // Can also happen if client sends multiple connect requests
                    // If so, cleanup stale connection info
                    if (_connectedClients.ContainsKey(uid))
                    {
                        _logger.LogMessage("Socket", LogLevel.Verbose, "Recyling MQTT connection information for ClientUID=" + uid);
                        var stale = _connectedClients[uid];
                        stale.Dispose();
                        _connectedClients.Remove(uid);
                    }
                    else
                    {
                        _logger.LogMessage("Socket", LogLevel.Verbose, "Finished adding MQTT client (ClientUID=" + uid + ")");
                    }

                    // Now re-add new connection
                    _connectedClients.Add(uid, connectionInfo);
                }
            }
        }

        public void ResetConnectionTimer(string clientUid)
        {
            lock (_connectedClients)
            {
                if (_connectedClients.ContainsKey(clientUid))
                {
                    var connectionInfo = _connectedClients[clientUid];
                    connectionInfo.ResetTimeout();
                }
            }
        }

        public async void WriteAsync(SocketEventArgs args)
        {
            Stream stream = GetStreamForConnectionConext(args);

            if (stream == null)
            {
                // OnCompleted called in GetStreamForConnectionConext(), just return here.
                return;
            }

            // Here we are outside the lock, stream can be closed beneath us
            // Typical scenario is the client asynchronously disconnects before we're finished sending it a message.
            // For now, I'm OK with this. It's better than balling this up with locks.
            try
            {
                if (stream.CanWrite)
                {
                    byte[] sendBuffer = args.MessageToSend.Serialize();
                    await stream.WriteAsync(sendBuffer, 0, sendBuffer.Length).ContinueWith(task =>
                    {
                        stream.Flush();
                        if (args.MessageToSend is IMqttIdMessage)
                        {
                            var msgWithId = args.MessageToSend as IMqttIdMessage;
                            _logger.LogMessage("Socket", LogLevel.Verbose,
                                string.Format("Sent message type '{0}', ID={1}.", msgWithId.MessageType,
                                    msgWithId.MessageId));
                        }
                        else
                        {
                            _logger.LogMessage("Socket", LogLevel.Verbose,
                                string.Format("Sent message type '{0}'.", args.MessageToSend.MessageType));
                        }
                        if (task.IsFaulted)
                        {
                            if (task.Exception != null && task.Exception.InnerExceptions.Count > 0)
                            {
                                args.SocketException = task.Exception.InnerExceptions[0];
                            }
                            else
                            {
                                args.SocketException = new Exception("Unknown socket error.");
                            }
                        }
                        args.Complete();
                    });
                }
                else
                {
                    args.SocketException = new Exception("Connection state is invalid. Please try reconnecting.");
                    args.Complete();
                }
            }
            catch (ObjectDisposedException)
            {
                // Effectively ignoring this
                args.Complete();
            }
            catch (Exception ex)
            {
                args.SocketException =
                    new Exception("Unable to write to the TCP connection. See inner exception for details.", ex);
                args.Complete();
            }
        }

        public void Disconnect(string clientUid)
        {
            ConnectedClientInfo clientInfo = null;

            lock (_connectedClients)
            {
                if (_connectedClients.ContainsKey(clientUid))
                {
                    clientInfo = _connectedClients[clientUid];
                    _connectedClients.Remove(clientUid);
                }
            }

            if (clientInfo != null)
            {
                if (clientInfo.Stream != null)
                {
                    clientInfo.Stream.Close();
                }
                clientInfo.TcpClient.Close();
                clientInfo.Dispose();
            }
        }

        private void DisconnectOnTimeout(object key)
        {
            _logger.LogMessage("Socket", LogLevel.Verbose, "Disconnecting a client due to keep alive timer expiration. ClientUID=" + key);
            Disconnect((string)key);
            _clientDisconnectedHandler((string)key, ClientDisconnectedReason.KeepAliveTimeExpired);
        }

        public void DisconnectAllOnPort(int port)
        {
            lock (_connectedClients)
            {
                var clientsOnPort = _connectedClients.Where(kvp => kvp.Value.Port == port).ToList();

                Parallel.ForEach(clientsOnPort, kvp =>
                {
                    if (kvp.Value.Stream != null)
                    {
                        kvp.Value.Stream.Close();
                    }
                    kvp.Value.TcpClient.Close();
                });

                foreach (var kvp in clientsOnPort)
                {
                    kvp.Value.Dispose();
                    _connectedClients.Remove(kvp.Key);
                }
            }
        }

        public void DisconnectAll()
        {
            lock (_connectedClients)
            {
                Parallel.ForEach(_connectedClients.Values, clientInfo =>
                {
                    if (clientInfo.Stream != null)
                    {
                        clientInfo.Stream.Close();
                    }
                    clientInfo.TcpClient.Close();
                });

                _connectedClients.Clear();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
            }

            _tokenSource.Cancel();
            _disposed = true;
        }

        ~iOSSocketWorker()
        {
            Dispose(false);
        }

        private void ClientReceiverThreadProc()
        {
            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;

            var receiverThread = new Thread(() =>  
            {
                _logger.LogMessage("Socket", LogLevel.Verbose, "Starting receiver thread loop.");
                while (true)
                {
                    try
                    {
                        ConnectedClientInfo info = GetNextClient();
                        if (info != null)
                        {
                            PollClient(info, token);
                        }

                        if (_tokenSource.IsCancellationRequested)
                        {
                            throw new TaskCanceledException();
                        }

                        // Give up time slice before looping
                        Thread.Sleep(MqttProtocolInformation.InternalSettings.SocketReceiverThreadLoopDelay);
                    }
                    catch (ObjectDisposedException)
                    {
                        // Socket or stream closed underneath us because client disconnected. Just ignore
                        _logger.LogMessage("Socket", LogLevel.Verbose,
                            string.Format("Network receiver thread is terminating due to ObjectDisposedException."));
                        return;
                    }
                    catch (TaskCanceledException)
                    {
                        _logger.LogMessage("Socket", LogLevel.Verbose,
                            string.Format("Network receiver thread is terminating due to cancellation request."));
                        return;
                    }
                    catch (Exception ex)
                    {
                        ProcessException(ex);
                    }
                }
            });

            receiverThread.Start();
        }

        private int _nextCollectionIndex;
        private ConnectedClientInfo GetNextClient()
        {
            lock (_connectedClients)
            {
                //DateTime now = DateTime.Now;
                //if (now.Second == 0 && now.Millisecond < 20)
                //{
                //    _logger.LogMessage("Socket", LogLevel.Verbose, "Number of live connections=" + _connectedClients.Count);
                //}
                if (_connectedClients.Count == 0)
                    return null;

                if (_nextCollectionIndex >= _connectedClients.Count)
                    _nextCollectionIndex = 0;

                ConnectedClientInfo info = _connectedClients.Values.ElementAt(_nextCollectionIndex);
                _nextCollectionIndex++;
                return info;
            }
        }

        private void PollClient(ConnectedClientInfo info, CancellationToken token)
        {
            TcpClient tcpClient = info.TcpClient;

            byte[] buffer = null;

            if (tcpClient.Available > 0)
            {
                Stream stream = GetStreamForConnection(info);
                buffer = ReadFromInputStreamAsync(stream, info.ClientUid, token);
            }

            if (buffer != null && buffer.Length > 0)
            {
                ProcessBuffer(buffer, info);
            }
        }

        private byte[] ReadFromInputStreamAsync(Stream stream, string clientUid, CancellationToken token)
        {
            var header = new MqttFixedHeader();
            var headerByte = new byte[1];
            int receivedSize;

            // Read the fixed header
            do
            {
                receivedSize = stream.Read(headerByte, 0, headerByte.Length);
            } while (receivedSize > 0 && header.AppendByte(headerByte[0]));

            if (!header.IsComplete)
            {
                _logger.LogMessage("Socket", LogLevel.Error,
                    string.Format("Read header operation could not read header, aborting."));
                return null;
            }

            _logger.LogMessage("Socket", LogLevel.Verbose,
                string.Format("Received message header type '{0}' from client {1}.", header.MessageType, clientUid));
            //_logger.LogMessage("Socket", LogLevel.Warning,
            //    string.Format("Received message header=0x{0:X}, Remaining length={1}.", header.Buffer[0], header.RemainingLength));

            // Create a buffer and read the remaining message
            var completeBuffer = header.CreateMessageBuffer();

            receivedSize = 0;
            while (receivedSize < header.RemainingLength)
            {
                receivedSize += stream.Read(completeBuffer, header.HeaderSize + receivedSize, header.RemainingLength - receivedSize);
            }
            //_logger.LogMessage("Socket", LogLevel.Warning,
            //    string.Format("                              Bytes read=      {0}.", receivedSize));

            if (token.IsCancellationRequested)
            {
                // Operation was cancelled
                _logger.LogMessage("Socket", LogLevel.Verbose, string.Format("Read header operation cancelled."));
                return null;
            }

            return completeBuffer;
        }

        private void ProcessBuffer(byte[] buffer, ConnectedClientInfo info)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                // When receiving the ConnAck message, we are still using the ConnectionKey param
                // All other cases we've connected the client and use the ClientUid param
                var args = new MqttNetEventArgs
                {
                    ClientUid = info.ClientUid
                };

                try
                {
                    // Process incomming messages
                    args.Message = MqttMessageDeserializer.Deserialize(buffer);
                    if (args.Message is IMqttIdMessage)
                    {
                        var msgWithId = args.Message as IMqttIdMessage;
                        _logger.LogMessage("Socket", LogLevel.Verbose,
                            string.Format("Received message type '{0}', ID={1}, from client {2}.", msgWithId.MessageType,
                                msgWithId.MessageId, info.ClientUid));
                    }
                    else
                    {
                        _logger.LogMessage("Socket", LogLevel.Verbose,
                            string.Format("Received message type '{0}' from client {1}.", args.Message.MessageType, info.ClientUid));
                    }
                }
                catch (Exception ex)
                {
                    var outer = new Exception(string.Format("Error deserializing message from network buffer. Buffer may be corrupt. Details: {0}", ex.Message), ex);
                    args.Exception = outer;
                    _logger.LogMessage("Socket", LogLevel.Error, outer.Message);
                }

                if (_messageReceivedHandler != null)
                {
                    _messageReceivedHandler(args);
                }
            });
        }

        private void ProcessException(Exception ex)
        {
            ThreadPool.QueueUserWorkItem(state => _messageReceivedHandler(new MqttNetEventArgs
            {
                Exception = ex
            }));
        }

        private Stream GetStreamForConnectionConext(SocketEventArgs args)
        {
            Stream stream;
            lock (_connectedClients)
            {
                if (!_connectedClients.ContainsKey(args.ClientUid))
                {
                    args.SocketException = new InvalidOperationException("No remote connection has been established.");
                    args.Complete();
                    return null;
                }

                ConnectedClientInfo info = _connectedClients[args.ClientUid];

                try
                {
                    stream = GetStreamForConnection(info);
                }
                catch (Exception ex)
                {
                    args.SocketException = ex;
                    args.Complete();
                    return null;
                }
            }
            return stream;
        }

        private Stream GetStreamForConnection(ConnectedClientInfo info)
        {
            if (info.Stream == null)
            {
                lock (info)
                {
                    if (info.Stream == null)
                    {
                        info.Stream = _getStreamHandler(info.TcpClient, info.Encryption);
                    }
                }
            }
            return info.Stream;
        }
    }
}
