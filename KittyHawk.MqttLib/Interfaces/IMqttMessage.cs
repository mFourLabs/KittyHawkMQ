
using KittyHawk.MqttLib.Messages;

namespace KittyHawk.MqttLib.Interfaces
{
    public interface IMqttMessage
    {
        MessageType MessageType { get; }
        bool Duplicate { get; set; }
        QualityOfService QualityOfService { get; }
        bool Retain { get; }
        int Retries { get; set; }
        byte[] MessageBuffer { get; }

        /// <summary>
        /// Does this message expect a response such as an ACK message?
        /// </summary>
        MessageType ExpectedResponse { get; }

        /// <summary>
        /// Convert the message into a byte array for streaming over the network
        /// </summary>
        /// <returns></returns>
        byte[] Serialize();
    }

    public interface IMqttIdMessage : IMqttMessage
    {
        int MessageId { get; }
    }
}
