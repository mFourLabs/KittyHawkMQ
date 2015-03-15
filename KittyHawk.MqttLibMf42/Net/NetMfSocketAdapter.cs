using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using KittyHawk.MqttLib.Plugins.Logging;
using Microsoft.SPOT.Net.Security;
using KittyHawk.MqttLib.Exceptions;
using KittyHawk.MqttLib.Utilities;
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Messages;

namespace KittyHawk.MqttLib.Net
{
    internal class NetMfSocketAdapter : ISocketAdapter
    {
        private Socket _socket;
        private readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);
        private readonly object _streamCreationLock = new object();
        private readonly object _receiverThreadCreationLock = new object();
        private readonly X509Certificate _caCert;
        private SslProtocols _encryptionLevel;
        private INetworkStream _stream;
        private string _remoteHost;
        private readonly ILogger _logger;
        private string _clientUid;

        // Receiver thread
        private Thread _receiverThread;

        // Writer thread parameters
        private Thread _writerThread;
        private SocketEventArgs _writerEventArgs;
        private readonly AutoResetEvent _writerThreadReady = new AutoResetEvent(false);
        private readonly AutoResetEvent _writerThreadWrite = new AutoResetEvent(false);

        public NetMfSocketAdapter(ILogger logger) :
            this(logger, null)
        {
        }

        public NetMfSocketAdapter(ILogger logger, X509Certificate caCertificate)
        {
            _logger = logger;
            _caCert = caCertificate;
        }

        public bool IsEncrypted(string clientUid)
        {
            return _encryptionLevel != SslProtocols.None;
        }

        public bool IsConnected(string clientUid)
        {
            lock (_receiverThreadCreationLock)
            {
                return _receiverThread != null && _receiverThread.IsAlive;
            }
        }

        public void JoinDisconnect(string clientUid)
        {
            // Throws InvalidOperationException when called from keep alive timer timeout. Don't know why but probably don't need it.

            //if (_receiverThread != null && _receiverThread.IsAlive && _receiverThread.ThreadState != ThreadState.Unstarted)
            //{
            //    _receiverThread.Join();
            //}
            //if (_writerThread != null && _writerThread.IsAlive && _writerThread.ThreadState != ThreadState.Unstarted)
            //{
            //    _writerThread.Join();
            //}
        }

        public void Disconnect(string clientUid)
        {
            _stopEvent.Set();

            // Kick the writer thread and make it exit. Must be done after _stopEvent has been set.
            _writerThreadReady.WaitOne();
            _writerThreadWrite.Set();
        }

        public void ConnectAsync(string ipOrHost, int port, SocketEventArgs args)
        {
            var t = new Thread(() =>
            {
                IPEndPoint ep;
                try
                {
                    ep = ResolveIpAddress(ipOrHost, port);
                }
                catch (Exception ex)
                {
                    args.SocketException = ex;
                    args.AdditionalErrorInfo = "Unable to resolve ip address or host name.";
                    args.Complete();
                    return;
                }

                _encryptionLevel = GetSslProtocol(args.EncryptionLevel);
                _clientUid = args.ClientUid;

                try
                {
                    CreateSocketAndConnect(ep);
                    StartReceiving();
                }
                catch (Exception ex)
                {
                    _clientUid = null;
                    args.SocketException = ex;
                }
                finally
                {
                    args.Complete();
                }
            });

            // Start the writer thread
            _writerThread = new Thread(WriteMessageWorker);
            _writerThread.Start();

            // Go connect
            t.Start();
        }

        /// <summary>
        /// This method helps overcome what appears to be a problem in .NET MF v4.2 where Socket.Connect()
        /// may never return.
        /// </summary>
        /// <param name="ep"></param>
        private void CreateSocketAndConnect(IPEndPoint ep)
        {
            Exception exception = null;
            int retries = 1;

            while (retries > 0)
            {
                var t = new Thread(() =>
                {
                    try
                    {
                        CreateSocket(ep.GetAddressFamily());
                        _logger.LogMessage("Socket", LogLevel.Verbose, "Attempting connection to " + ep.Address);
                        _socket.Connect(ep);
                        _logger.LogMessage("Socket", LogLevel.Information, "Successfully connected to " + ep.Address);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogMessage("Socket", LogLevel.Information, "Connection attempt failed for " + ep.Address + " Exception=" + ex);
                        exception = ex;
                    }
                });
                t.Start();

                if (!t.Join(10000))
                {
                    // If we timed out, we hit the bug. Try again.
                    retries--;
                }
                else
                {
                    if (exception != null)
                    {
                        throw exception;
                    }
                    break;
                }
            }
        }

        public void WriteAsync(SocketEventArgs args)
        {
            if (_socket == null)
            {
                args.SocketException = new InvalidOperationException("No server connection has been established.");
                FireOnCompletedNewThread(args);
                return;
            }

            int waitResult = WaitHandle.WaitAny(new WaitHandle[] {_stopEvent, _writerThreadReady},
                MqttProtocolInformation.Settings.NetworkTimeout*1000, false);

            if (waitResult == WaitHandle.WaitTimeout || waitResult == 0)
            {
                args.SocketException = new Exception("Unable to send message type " + args.MessageToSend.MessageType + ". Client may be disconnecting.");
                FireOnCompletedNewThread(args);
                return;
            }

            _writerEventArgs = args;
            _writerThreadWrite.Set();
        }

        private void WriteMessageWorker()
        {
            while (true)
            {
                _writerThreadReady.Set();
                _writerThreadWrite.WaitOne();

                // Are we shutting down?
                if (_stopEvent.WaitOne(0, false))
                {
                    return;
                }

                try
                {
                    if (_socket == null)
                    {
                        throw new InvalidOperationException("No server connection has been established.");
                    }

                    string id = "N/A";
                    if (_writerEventArgs.MessageToSend is IMqttIdMessage)
                    {
                        id = ((IMqttIdMessage)_writerEventArgs.MessageToSend).MessageId.ToString();
                    }

                    byte[] sendBuffer = _writerEventArgs.MessageToSend.Serialize();
                    _logger.LogMessage("Socket", LogLevel.Verbose,
                        "Sending message type " + _writerEventArgs.MessageToSend.MessageType +
                        ", QOS=" + _writerEventArgs.MessageToSend.QualityOfService +
                        ", ID=" + id);

                    GetStream().Send(sendBuffer);
                }
                catch (ErrorContextException ex)
                {
                    string msg = "Error sending message " + _writerEventArgs.MessageToSend.MessageType + ". " + ex.Message;
                    if (ex.InnerException != null)
                    {
                        msg += ". Inner exception=" + ex.InnerException;
                    }
                    _logger.LogMessage("Socket", LogLevel.Verbose, msg);
                    _writerEventArgs.AdditionalErrorInfo = ex.Message;
                    _writerEventArgs.SocketException = ex.InnerException ?? ex;
                }
                catch (Exception ex)
                {
                    string msg = "Error sending message " + _writerEventArgs.MessageToSend.MessageType + ". " + ex.Message;
                    if (ex.InnerException != null)
                    {
                        msg += ". Inner exception=" + ex.InnerException;
                    }
                    _logger.LogMessage("Socket", LogLevel.Verbose, msg);
                    _writerEventArgs.SocketException = ex;
                }
                finally
                {
                    // If disconnecting, get off the writer thread so we can close it and join to it properly
                    var args = _writerEventArgs.Clone();
                    if (_writerEventArgs.MessageToSend.MessageType == MessageType.Disconnect)
                    {
                        FireOnCompletedNewThread(args.Clone());
                    }
                    else
                    {
                        args.Complete();
                    }
                }
            }
        }

        private void StartReceiving()
        {
            lock (_receiverThreadCreationLock)
            {
                if (_receiverThread != null && _receiverThread.IsAlive)
                {
                    return;
                }

                _receiverThread = new Thread(() =>
                {
                    _stopEvent.Reset();

                    // Main receiver loop
                    while (true)
                    {
                        byte[] buffer = null;
                        try
                        {
                            if (GetStream().Available > 0)
                            {
                                buffer = Read();
                            }
                        }
                        catch (Exception ex)
                        {
                            ProcessException(ex);
                        }

                        if (buffer != null && buffer.Length > 0)
                        {
                            ProcessBuffer(buffer);
                        }

                        if (_stopEvent.WaitOne(MqttProtocolInformation.InternalSettings.SocketReceiverThreadLoopDelay, false))
                        {
                            break;
                        }
                    }
                });
                _receiverThread.Start();
            }
        }

        private NetworkReceiverEventHandler _messageReceivedHandler;
        public void OnMessageReceived(NetworkReceiverEventHandler handler)
        {
            _messageReceivedHandler = handler;
        }

        private void MessageReceived(MqttNetEventArgs args)
        {
            _messageReceivedHandler(args);
        }

        public void Dispose()
        {
            if (_stream != null)
            {
                _logger.LogMessage("Socket", LogLevel.Verbose, "Closing existing network stream.");
                _stream.Close();    // also closes the socket
                _stream = null;
            }
            _socket = null;
        }

        private byte[] Read()
        {
            var header = new MqttFixedHeader();
            var headerByte = new byte[1];
            int receivedSize;
            INetworkStream stream = GetStream();

            // Read the fixed header
            do
            {
                receivedSize = stream.Receive(headerByte, 0, 1);
            } while (receivedSize > 0 && header.AppendByte(headerByte[0]));

            if (!header.IsComplete)
            {
                _logger.LogMessage("Socket", LogLevel.Error, "Header data invalid for incoming message.");
                throw new IOException("Unable to receive the MQTT fixed header.");
            }

            _logger.LogMessage("Socket", LogLevel.Verbose, "Begin reading payload for incoming message type: " + header.MessageType);

            // Create a buffer and read the remaining message
            var completeBuffer = header.CreateMessageBuffer();

            receivedSize = 0;
            while (receivedSize < header.RemainingLength)
            {
                receivedSize += stream.Receive(completeBuffer, header.HeaderSize + receivedSize, header.RemainingLength - receivedSize);
            }

            return completeBuffer;
        }

        private void ProcessBuffer(byte[] buffer)
        {
            var t = new Thread(() =>
            {
                var args = new MqttNetEventArgs { ClientUid = _clientUid };

                try
                {
                    // Process incomming messages
                    args.Message = MqttMessageDeserializer.Deserialize(buffer);

                    string id = "N/A";
                    if (args.Message is IMqttIdMessage)
                    {
                        id = ((IMqttIdMessage)args.Message).MessageId.ToString();
                    }
                    _logger.LogMessage("Socket", LogLevel.Verbose,
                        "Received message type " + args.Message.MessageType +
                        ", QOS=" + args.Message.QualityOfService +
                        ", ID=" + id);
                }
                catch (Exception ex)
                {
                    args.Exception = ex;
                }

                MessageReceived(args);
            });
            t.Start();
        }

        private void ProcessException(Exception ex)
        {
            var t = new Thread(() =>
            {
                var args = new MqttNetEventArgs { ClientUid = _clientUid };
                var ece = ex as ErrorContextException;

                if (ece != null)
                {
                    _logger.LogMessage("Socket", LogLevel.Verbose, "Processing exception " + ece.Message + ". Inner exception=" + ece.InnerException);
                    args.AdditionalErrorInfo = ece.Message;
                    args.Exception = ece.InnerException;
                }
                else
                {
                    _logger.LogMessage("Socket", LogLevel.Verbose, "Processing exception " + ex);
                    args.Exception = ex;
                }

                MessageReceived(args);
            });
            t.Start();
        }

        private void FireOnCompletedNewThread(SocketEventArgs args)
        {
            var t = new Thread(args.Complete);
            t.Start();
        }

        private void CreateSocket(AddressFamily addressFamily)
        {
            if (_stream != null)
            {
                _logger.LogMessage("Socket", LogLevel.Verbose, "Closing existing network stream.");
                _stream.Close();    // also closes the socket
                _stream = null;
                _socket = null;
            }
            _socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
            //_socket.ReceiveTimeout = MqttProtocolInformation.Settings.NetworkTimeout * 1000;
            //_socket.SendTimeout = MqttProtocolInformation.Settings.NetworkTimeout * 1000;
        }

        private INetworkStream GetStream()
        {
            if (_stream == null)
            {
                lock (_streamCreationLock)
                {
                    if (_stream == null)
                    {
                        if (_encryptionLevel == SslProtocols.None)
                        {
                            _stream = new UnsecureStream(_socket);
                        }
                        else
                        {
                            try
                            {
                                _stream = new SecureStream(_remoteHost, _socket, _caCert, _encryptionLevel);
                            }
                            catch (SocketException ex)
                            {
                                if (ex.ErrorCode == -1)
                                {
                                    var detail = new ErrorContextException("The remote certificate is invalid according to the validation procedure.", ex);
                                    throw detail;
                                }
                                throw;
                            }
                        }
                    }
                }
            }
            return _stream;
        }

        private IPEndPoint ResolveIpAddress(string ipOrHost, int port)
        {
            // Save hostname that user gave us for TLS hostname validation
            _remoteHost = ipOrHost;

            // IP look-up in user supplied hosts dictionary
            if (MqttProtocolInformation.Settings.Hosts.Contains(ipOrHost))
            {
                var value = MqttProtocolInformation.Settings.Hosts[ipOrHost] as string;
                if (value != null)
                {
                    ipOrHost = value;
                }
            }

            IPHostEntry hostEntry = Dns.GetHostEntry(ipOrHost);
            return new IPEndPoint(hostEntry.AddressList[0], port);
        }

        private SslProtocols GetSslProtocol(SocketEncryption encryption)
        {
            _encryptionLevel = SslProtocols.None;

            switch (encryption)
            {
                case SocketEncryption.None:
                    _encryptionLevel = SslProtocols.None;
                    break;
                case SocketEncryption.Tls10:
                    _encryptionLevel = SslProtocols.TLSv1;
                    break;
            }

            return _encryptionLevel;
        }
    }

    /// <summary>
    /// Extension methods for socket related classes.
    /// </summary>
    public static class SocketExtensions
    {
        public static AddressFamily GetAddressFamily(this IPEndPoint ep)
        {
            string ip = ep.Address.ToString();
            var digits = ip.Split(new[] {':'});
            
            // Shortest possible IPv6 address is the loopback address ::1
            if (digits.Length > 2)
            {
                return AddressFamily.InterNetworkV6;
            }
            return AddressFamily.InterNetwork;
        }
    }
}
