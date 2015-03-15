using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using KittyHawk.MqttLib.Interfaces;
using Microsoft.SPOT.Net.Security;

namespace KittyHawk.MqttLib.Net
{
    internal class SecureStream : INetworkStream
    {
        private readonly SslStream _sslStream;
        private readonly Socket _socket;

        public SecureStream(string host, Socket socket, X509Certificate ca, SslProtocols encryption)
        {
            _socket = socket;
            _sslStream = new SslStream(socket);
            _sslStream.AuthenticateAsClient(host, null, new[] { ca }, SslVerification.CertificateRequired, encryption);
        }

        public int Available
        {
            get { return _socket.Available; }
        }

        public int Receive(byte[] buffer, int offset, int size)
        {
            return _sslStream.Read(buffer, offset, size);
        }

        public void Send(byte[] buffer)
        {
            _sslStream.Write(buffer, 0, buffer.Length);
        }

        public void Close()
        {
            _sslStream.Close();
            _socket.Close();
        }
    }
}
