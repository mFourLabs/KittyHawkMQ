
using System;

namespace KittyHawk.MqttLib.Messages
{
    public enum MessageType
    {
        Reserved0 = 0,
        Connect = 1,
        ConnAck = 2,
        Publish = 3,
        PubAck = 4,
        PubRec = 5,
        PubRel = 6,
        PubComp = 7,
        Subscribe = 8,
        SubAck = 9,
        Unsubscribe = 10,
        UnsubAck = 11,
        PingReq = 12,
        PingResp = 13,
        Disconnect = 14,
        Reserved15 = 15,
        None = 16
    }

    public enum QualityOfService
    {
        AtMostOnce = 0,
        AtLeastOnce = 1,
        ExactlyOnce = 2,
        Reserved3 = 3
    }

    public enum ConnectReturnCode
    {
        Accepted = 0x00,
        RefusedProtocolVersion = 0x01,
        RefusedIdentifierRejected = 0x02,
        RefusedServerUnavailable = 0x03,
        RefusedBadUserNameOrPassword = 0x04,
        RefusedNotAuthorized = 0x05,
        FutureUse06 = 0x06,
        FutureUse07 = 0x07,
        FutureUse08 = 0x08,
        FutureUse09 = 0x09,
        FutureUse10 = 0x0a,
        FutureUse11 = 0x0b,
        FutureUse12 = 0x0c,
        FutureUse13 = 0x0d,
        FutureUse14 = 0x0e,
        FutureUse15 = 0x0f,
        Unknown = 0x10,
    }

    [Flags]
    internal enum ConnectFlag
    {
        Reserved0 = 0x01,
        CleanSession = 0x02,
        WillFlag = 0x04,
        WillQos0 = 0x08,
        WillQos1 = 0x10,
        WillRetain = 0x20,
        PasswordFlag = 0x40,
        UserNameFlag = 0x80
    }

    internal sealed class MessageHeader
    {
        internal static byte MESSAGE_TYPE_START = 4;
        internal static byte MESSAGE_TYPE_MASK = 0xF0;
        internal static byte DUPLICATE_FLAG_MASK = 0x8;
        internal static byte QOS_FLAG_START = 1;
        internal static byte QOS_FLAG_MASK = 0x6;
        internal static byte RETAIN_FLAG_MASK = 0x1;
    }
}
