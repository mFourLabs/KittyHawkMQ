
using System;
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Messages;

namespace KittyHawk.MqttLib.Net
{
    public delegate void MqttMessageEventHandler(object sender, MqttMessageEventArgs args);
    public delegate void MqttConnectMessageEventHandler(object sender, MqttConnectMessageEventArgs args);
    public delegate void MqttSubscribeMessageEventHandler(object sender, MqttSubscribeMessageEventArgs args);
    public delegate void MqttPublishMessageEventHandler(object sender, MqttPublishMessageEventArgs args);
    internal delegate void MqttCommunicationEventHandler(object sender, MqttNetEventArgs args);
    internal delegate void ClientDisconnectedHandler(string clientUid, ClientDisconnectedReason reason);

    internal sealed class MqttNetEventArgs
    {
        public Exception Exception { get; set; }
        public string AdditionalErrorInfo { get; set; }
        public IMqttMessage Message { get; set; }
        public object EventData { get; set; }
        public string ClientUid { get; set; }
    }

    public sealed class MqttMessageEventArgs
    {
        public Exception Exception { get; set; }
        public string AdditionalErrorInfo { get; set; }
        public IMqttMessage Message { get; set; }
    }

    public sealed class MqttConnectMessageEventArgs
    {
        public Exception Exception { get; set; }
        public string AdditionalErrorInfo { get; set; }
        public MqttConnectAckMessage Message { get; set; }
    }

    public sealed class MqttSubscribeMessageEventArgs
    {
        public Exception Exception { get; set; }
        public string AdditionalErrorInfo { get; set; }
        public MqttSubscribeAckMessage Message { get; set; }
    }

    public sealed class MqttPublishMessageEventArgs
    {
        public Exception Exception { get; set; }
        public string AdditionalErrorInfo { get; set; }
        public MqttPublishMessage Message { get; set; }
    }
}
