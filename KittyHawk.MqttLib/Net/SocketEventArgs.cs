
using System;
using KittyHawk.MqttLib.Interfaces;

namespace KittyHawk.MqttLib.Net
{
    internal delegate void SocketOperationCompleteHandler(SocketEventArgs args);

    internal sealed class SocketEventArgs
    {
        private SocketOperationCompleteHandler _completedHandler;

        public Exception SocketException { get; set; }
        public IMqttMessage MessageToSend { get; set; }
        public string AdditionalErrorInfo { get; set; }
        public string ClientUid { get; set; }
        public SocketEncryption EncryptionLevel { get; set; }

        public void OnOperationComplete(SocketOperationCompleteHandler handler)
        {
            _completedHandler = handler;
        }

        public void Complete()
        {
            if (_completedHandler != null)
            {
                _completedHandler(this);
            }
        }

        public SocketEventArgs Clone()
        {
            var arg = new SocketEventArgs();
            arg._completedHandler = _completedHandler;
            arg.SocketException = SocketException;
            arg.MessageToSend = MessageToSend;
            arg.AdditionalErrorInfo = AdditionalErrorInfo;
            arg.ClientUid = ClientUid;
            arg.EncryptionLevel = EncryptionLevel;
            return arg;
        }
    }
}
