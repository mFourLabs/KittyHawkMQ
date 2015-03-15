#if WIN_PCL
using System.Runtime.InteropServices.WindowsRuntime;
#endif
using KittyHawk.MqttLib.Interfaces;

namespace KittyHawk.MqttLib.Messages
{
    public sealed class MqttMessageDeserializer
    {
        public static IMqttMessage Deserialize(
#if WIN_PCL
            [ReadOnlyArray]
#endif
            byte[] buffer
            )
        {
            var msgType = ReadMessageTypeFromHeader(buffer[0]);
            IMqttMessage resultingMsg = null;

            switch (msgType)
            {
                case MessageType.Connect:
                    resultingMsg = MqttConnectMessage.InternalDeserialize(buffer);
                    break;

                case MessageType.ConnAck:
                    resultingMsg = MqttConnectAckMessage.InternalDeserialize(buffer);
                    break;

                case MessageType.Disconnect:
                    resultingMsg = MqttDisconnectMessage.InternalDeserialize(buffer);
                    break;

                case MessageType.PubAck:
                    resultingMsg = MqttPublishAckMessage.InternalDeserialize(buffer);
                    break;

                case MessageType.PubRec:
                    resultingMsg = MqttPublishReceivedMessage.InternalDeserialize(buffer);
                    break;

                case MessageType.PubRel:
                    resultingMsg = MqttPublishReleaseMessage.InternalDeserialize(buffer);
                    break;

                case MessageType.PubComp:
                    resultingMsg = MqttPublishCompleteMessage.InternalDeserialize(buffer);
                    break;

                case MessageType.Publish:
                    resultingMsg = MqttPublishMessage.InternalDeserialize(buffer);
                    break;

                case MessageType.Subscribe:
                    resultingMsg = MqttSubscribeMessage.InternalDeserialize(buffer);
                    break;

                case MessageType.SubAck:
                    resultingMsg = MqttSubscribeAckMessage.InternalDeserialize(buffer);
                    break;

                case MessageType.Unsubscribe:
                    resultingMsg = MqttUnsubscribeMessage.InternalDeserialize(buffer);
                    break;

                case MessageType.UnsubAck:
                    resultingMsg = MqttUnsubscribeAckMessage.InternalDeserialize(buffer);
                    break;

                case MessageType.PingReq:
                    resultingMsg = MqttPingRequestMessage.InternalDeserialize(buffer);
                    break;

                case MessageType.PingResp:
                    resultingMsg = MqttPingResponseMessage.InternalDeserialize(buffer);
                    break;
            }

            return resultingMsg;
        }

        public static MessageType ReadMessageTypeFromHeader(byte header)
        {
            byte val = (byte)(header & MessageHeader.MESSAGE_TYPE_MASK);
            return (MessageType)(val >> MessageHeader.MESSAGE_TYPE_START);
        }
    }
}