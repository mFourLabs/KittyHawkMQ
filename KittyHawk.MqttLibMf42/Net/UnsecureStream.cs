
using System.Net.Sockets;
using KittyHawk.MqttLib.Interfaces;
using Microsoft.SPOT;

namespace KittyHawk.MqttLib.Net
{
    internal class UnsecureStream : INetworkStream
    {
        private readonly Socket _socket;

        public UnsecureStream(Socket socket)
        {
            _socket = socket;
        }

        public int Available
        {
            get { return _socket.Available; }
        }

        public int Receive(byte[] buffer, int offset, int size)
        {
            return _socket.Receive(buffer, offset, size, SocketFlags.None);
        }

        public void Send(byte[] buffer)
        {
            _socket.Send(buffer, SocketFlags.None);
        }

        public void Close()
        {
            _socket.Close();
        }
    }
}
