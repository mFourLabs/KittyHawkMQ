
namespace KittyHawk.MqttLib.Interfaces
{
    interface INetworkStream
    {
        int Available { get; }
        int Receive(byte[] buffer, int offset, int size);
        void Send(byte[] buffer);
        void Close();
    }
}
