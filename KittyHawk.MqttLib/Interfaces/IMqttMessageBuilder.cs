
using KittyHawk.MqttLib.Messages;

namespace KittyHawk.MqttLib.Interfaces
{
    public interface IMqttMessageBuilder
    {
        MessageType MessageType { get; }
        bool Duplicate { get; set; }
        QualityOfService QualityOfService { get; set; }
        bool Retain { get; set; }
        IMqttMessage GetMessage();
    }
}
