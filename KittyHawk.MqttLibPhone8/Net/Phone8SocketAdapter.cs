using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Messages;
using KittyHawk.MqttLib.Plugins.Logging;

namespace KittyHawk.MqttLib.Net
{
    internal sealed class Phone8SocketAdapter : ISocketAdapter
    {
        private const int IoCanceledHResult = -2147023901;     // 0x800703E3;
        private StreamSocket _socket;
        private CancellationTokenSource _tokenSource;
        private SocketProtectionLevel _encryptionLevel;
        private int _isReceving;
        private readonly ILogger _logger;
        private string _clientUid;

        public Phone8SocketAdapter(ILogger logger)
        {
            _logger = logger;
        }

        public bool IsEncrypted(string clientUid)
        {
            return _encryptionLevel != SocketProtectionLevel.PlainSocket;
        }

        public bool IsConnected(string clientUid)
        {
            return _tokenSource != null && !_tokenSource.IsCancellationRequested;
        }

        public void JoinDisconnect(string clientUid)
        {
            // No impl for WinRT
        }

        public void Disconnect(string clientUid)
        {
            if (_tokenSource != null)
            {
                _tokenSource.Cancel();
            }
        }

        public async void ConnectAsync(string ipOrHost, int port, SocketEventArgs args)
        {
            _socket = new StreamSocket();
            var server = new HostName(ipOrHost);

            // TCP timeouts in WinRT are excessive, shorten them here using task cancellation
            var cts = new CancellationTokenSource();

            try
            {
                cts.CancelAfter(MqttProtocolInformation.Settings.NetworkTimeout * 1000);
                _logger.LogMessage("Socket", LogLevel.Verbose, string.Format("Authenticating client certificate with remote host CN={0}", server.CanonicalName));
                await _socket.ConnectAsync(server, port.ToString(), GetSocketProtectionLevel(args.EncryptionLevel)).AsTask(cts.Token);
                _clientUid = args.ClientUid;
                StartReceiving();
            }
            catch (TaskCanceledException)
            {
                args.SocketException = new IOException("Timeout error while trying to connect.");
                _clientUid = null;
                _socket.Dispose();
                _socket = null;
            }
            catch (Exception ex)
            {
                args.SocketException = ex;
                _clientUid = null;
                _socket.Dispose();
                _socket = null;
            }
            args.Complete();
        }

        public void WriteAsync(SocketEventArgs args)
        {
            if (_socket == null)
            {
                args.SocketException = new InvalidOperationException("No server connection has been established.");
                args.Complete();
                return;
            }

            try
            {
                // Write data to the socket
                var writer = new DataWriter(_socket.OutputStream);
                writer.WriteBytes(args.MessageToSend.Serialize());
                writer.StoreAsync().Completed += (info, status) =>
                {
                    writer.DetachStream();
                    writer.Dispose();

                    if (args.MessageToSend is IMqttIdMessage)
                    {
                        var msgWithId = args.MessageToSend as IMqttIdMessage;
                        _logger.LogMessage("Socket", LogLevel.Verbose, string.Format("Sent message type '{0}', ID={1} to server.", msgWithId.MessageType, msgWithId.MessageId));
                    }
                    else
                    {
                        _logger.LogMessage("Socket", LogLevel.Verbose, string.Format("Sent message type '{0}' to server.", args.MessageToSend.MessageType));
                    }

                    if (info.Status == AsyncStatus.Error)
                    {
                        args.SocketException = info.ErrorCode;
                    }
                    args.Complete();
                };
            }
            catch (Exception ex)
            {
                args.SocketException = ex;
                args.Complete();
            }
        }

        private async void StartReceiving()
        {
            if (_socket == null)
            {
                throw new InvalidOperationException("No server connection has been established.");
            }

            if (Interlocked.CompareExchange(ref _isReceving, 1, 0) == 1)
            {
                return;
            }

            try
            {
                _tokenSource = new CancellationTokenSource();
                var token = _tokenSource.Token;

                while (true)
                {
                    byte[] buffer = null;
                    try
                    {
                        buffer = await ReadFromInputStreamAsync(_socket, token);
                    }
                    catch (Exception ex)
                    {
                        if (ex is TaskCanceledException || ex.HResult == IoCanceledHResult)
                        {
                            _logger.LogMessage("Socket", LogLevel.Verbose, "Network receive is terminating due to cancellation request.");
                            break;
                        }

                        ProcessException(ex);
                    }

                    if (buffer != null && buffer.Length > 0)
                    {
                        ProcessBuffer(buffer);
                    }

                    if (token.IsCancellationRequested)
                    {
                        _logger.LogMessage("Socket", LogLevel.Verbose, "Network receive is terminating due to cancellation request.");
                        break;
                    }

                    _logger.LogMessage("Socket", LogLevel.Verbose, "Receving task looping...");
                }
            }
            finally
            {
                Interlocked.CompareExchange(ref _isReceving, 0, 1);
            }
        }

        private NetworkReceiverEventHandler _messageReceivedHandler;
        public void OnMessageReceived(NetworkReceiverEventHandler handler)
        {
            _messageReceivedHandler = handler;
        }

        public void Dispose()
        {
            Disconnect(null);
            if (_socket != null)
            {
                _socket.Dispose();
            }
            _socket = null;
        }

        private async Task<byte[]> ReadFromInputStreamAsync(StreamSocket socket, CancellationToken token)
        {
            var header = new MqttFixedHeader();
            uint bytesRead;

            using (var reader = new DataReader(socket.InputStream))
            {
                reader.InputStreamOptions = InputStreamOptions.Partial;

                // Read the fixed header
                var headerByte = new byte[1];
                do
                {
                    bytesRead = await reader.LoadAsync(1);
                    if (bytesRead > 0)
                    {
                        reader.ReadBytes(headerByte);
                    }
                } while (bytesRead > 0 && header.AppendByte(headerByte[0]));
                reader.DetachStream();

                if (token.IsCancellationRequested)
                {
                    // Operation was cancelled
                    return null;
                }
                if (!header.IsComplete)
                {
                    _logger.LogMessage("Socket", LogLevel.Verbose, "Read header operation could not read header, aborting.");
                    return null;
                }

                _logger.LogMessage("Socket", LogLevel.Verbose, string.Format("Received message header type '{0}' from server.", header.MessageType));
            }

            var msgBuffer = header.CreateMessageBuffer();
            //_logger.LogMessage("Socket", LogLevel.Verbose,
            //    string.Format("Received message header=0x{0:X}, Remaining length={1}.", header.Buffer[0], header.RemainingLength));

            using (var reader = new DataReader(socket.InputStream))
            {
                if (header.RemainingLength > 0)
                {
                    // Create a buffer and read the remaining message
                    bytesRead = await reader.LoadAsync((uint)header.RemainingLength);
                    //_logger.LogMessage("Socket", LogLevel.Verbose,
                    //    string.Format("                              Bytes read=      {0}.", bytesRead));
                    if (bytesRead > 0)
                    {
                        var remainingBuffer = new byte[reader.UnconsumedBufferLength];
                        reader.ReadBytes(remainingBuffer);

                        // Merge the fixed header and remaining buffers together
                        Array.ConstrainedCopy(remainingBuffer, 0, msgBuffer, header.HeaderSize, remainingBuffer.Length);
                    }
                }
                reader.DetachStream();

                if (token.IsCancellationRequested)
                {
                    // Operation was cancelled
                    return null;
                }

                return msgBuffer;
            }
        }

        private async void ProcessBuffer(byte[] buffer)
        {
            await Windows.System.Threading.ThreadPool.RunAsync(state =>
            {
                var args = new MqttNetEventArgs {ClientUid = _clientUid};

                try
                {
                    // Process incomming messages
                    args.Message = MqttMessageDeserializer.Deserialize(buffer);
                    if (args.Message is IMqttIdMessage)
                    {
                        var msgWithId = args.Message as IMqttIdMessage;
                        _logger.LogMessage("Socket", LogLevel.Verbose, string.Format("Received message type '{0}', ID={1} from server.", msgWithId.MessageType, msgWithId.MessageId));
                    }
                    else
                    {
                        _logger.LogMessage("Socket", LogLevel.Verbose, string.Format("SOCKET: Received message type '{0}' from server.", args.Message.MessageType));
                    }
                }
                catch (Exception ex)
                {
                    args.Exception = ex;
                }

                _messageReceivedHandler(args);
            });
        }

        private async void ProcessException(Exception ex)
        {
            await Windows.System.Threading.ThreadPool.RunAsync(state => _messageReceivedHandler(new MqttNetEventArgs
            {
                Exception = ex,
                ClientUid = _clientUid
            }));
        }

        private SocketProtectionLevel GetSocketProtectionLevel(SocketEncryption encryption)
        {
            _encryptionLevel = SocketProtectionLevel.PlainSocket;

            switch (encryption)
            {
                case SocketEncryption.None:
                    _encryptionLevel = SocketProtectionLevel.PlainSocket;
                    break;
                case SocketEncryption.Ssl:
                    _encryptionLevel = SocketProtectionLevel.Ssl;
                    break;
                //case SocketEncryption.Tls10:
                //    IsEncrypted = true;
                //    _encryptionLevel = SocketProtectionLevel.Tls10;
                //    break;
                //case SocketEncryption.Tls11:
                //    IsEncrypted = true;
                //    _encryptionLevel = SocketProtectionLevel.Tls11;
                //    break;
                //case SocketEncryption.Tls12:
                //    IsEncrypted = true;
                //    _encryptionLevel = SocketProtectionLevel.Tls12;
                //    break;
            }

            return _encryptionLevel;
        }
    }
}
