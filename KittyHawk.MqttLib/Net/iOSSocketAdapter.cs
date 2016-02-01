using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Plugins.Logging;

namespace KittyHawk.MqttLib.Net
{
    internal class iOSSocketAdapter : ISocketAdapter
    {
        private string _remoteHost;
        private readonly ILogger _logger;
        private readonly iOSSocketWorker _socketWorker;

        public iOSSocketAdapter(ILogger logger)
        {
            _logger = logger;
            _socketWorker = new iOSSocketWorker(_logger);
            _socketWorker.OnGetStream(GetStream);
        }

        public bool IsEncrypted(string clientUid)
        {
            return _socketWorker.IsEncrypted(clientUid);
        }

        // ISocketAdapter helpers
        public bool IsConnected(string clientUid)
        {
            return _socketWorker.IsConnected(clientUid);
        }

        public void JoinDisconnect(string clientUid)
        {
            // No impl for Win32
        }

        public async void ConnectAsync(string ipOrHost, int port, SocketEventArgs args)
        {
            TcpClient tcpClient;
            IPAddress ip;
            Task connectTask;

            if (IPAddress.TryParse(ipOrHost, out ip))
            {
                _remoteHost = "";
                var endPoint = new IPEndPoint(ip, port);
                tcpClient = CreateSocket(endPoint.AddressFamily);
                connectTask = tcpClient.ConnectAsync(endPoint.Address, port);
            }
            else
            {
                _remoteHost = ipOrHost;
                tcpClient = CreateSocket(AddressFamily.Unspecified);
                connectTask = tcpClient.ConnectAsync(ipOrHost, port);
            }

            await connectTask.ContinueWith(task =>
            {
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
                else
                {
                    _socketWorker.ConnectTcpClient(tcpClient, port, args.EncryptionLevel, args.ClientUid);
                }
                args.Complete();
            });
        }

        public void WriteAsync(SocketEventArgs args)
        {
            _socketWorker.WriteAsync(args);
        }

        public void Dispose()
        {
            _socketWorker.DisconnectAll();
            _socketWorker.Dispose();
        }

        public void Disconnect(string clientUid)
        {
            _socketWorker.Disconnect(clientUid);
            JoinDisconnect(clientUid);
        }

        public void OnMessageReceived(NetworkReceiverEventHandler handler)
        {
            _socketWorker.OnMessageReceived(handler);
        }

        private TcpClient CreateSocket(AddressFamily addressFamily)
        {
            TcpClient client;

            if (addressFamily == AddressFamily.Unspecified)
            {
                client = new TcpClient();
            }
            else
            {
                client = new TcpClient(addressFamily);
            }

            client.NoDelay = true;
            return client;
        }

        private Stream GetStream(TcpClient tcpClient, SslProtocols encryption)
        {
            if (encryption != SslProtocols.None)
            {
                _logger.LogMessage("Socket", LogLevel.Verbose, string.Format("Establishing channel encryption {0}.", encryption));
                var sslStream = new SslStream(tcpClient.GetStream(), false, ValidateServerCertificate, null);

                try
                {
                    _logger.LogMessage("Socket", LogLevel.Verbose, string.Format("Authenticating client certificate with remote host CN={0}.", _remoteHost));
                    sslStream.AuthenticateAsClient(_remoteHost,
                        null,
                        encryption,
                        false);
                }
                catch (IOException ex)
                {
                    // Bug in .NET code - handle gracefully
                    if (ex.HResult == -2146232800)
                    {
                        throw new IOException("Reconnecting to the same broker in a continuous session using a the secure client is not supported. Restart the process to reconnect or switch to a non-secure client.", ex);
                    }
                    throw;
                }

                return sslStream;
            }
            else
            {
                return tcpClient.GetStream();
            }
        }

        private static bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                Debug.WriteLine("SOCKET: Certificate error: {0}", sslPolicyErrors.ToString());
                return false;
            }

            Debug.WriteLine("SOCKET: No Certificate errors.");
            return true;
        }
    }
}
