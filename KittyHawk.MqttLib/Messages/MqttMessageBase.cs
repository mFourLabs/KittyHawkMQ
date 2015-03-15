
using System;
using KittyHawk.MqttLib.Interfaces;

namespace KittyHawk.MqttLib.Messages
{
    internal class MqttMessageBase : IMqttMessage
    {
        private const int DefaultMessageId = 1;

        /// <summary>
        /// If the specified message has an ID, it will return it. Otherwise return the pre-defined default ID.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static int GetMessageIdOrDefault(IMqttMessage message)
        {
            var msgWithId = message as IMqttIdMessage;
            return msgWithId == null ? DefaultMessageId : msgWithId.MessageId;
        }

        internal readonly object SyncLock = new object();
        internal bool VariableHeaderRead;
        internal
#if !(MF_FRAMEWORK_VERSION_V4_3 || MF_FRAMEWORK_VERSION_V4_2)
            volatile
#endif
            bool PayloadRead;

        internal byte[] MsgBuffer;

        internal protected MqttMessageBase()
        {
        }

        internal protected MqttMessageBase(MessageType msgType)
        {
            MsgBuffer = new byte[1];
            Header |= (byte)((byte)msgType << MessageHeader.MESSAGE_TYPE_START);     // set message type in MSB
        }

        #region IMqttMessage

        public byte[] MessageBuffer
        {
            get { return MsgBuffer; }
        }

        public MessageType MessageType
        {
            get
            {
                return MqttMessageDeserializer.ReadMessageTypeFromHeader(Header);
            }
        }

        public bool Duplicate
        {
            get
            {
                return (Header & MessageHeader.DUPLICATE_FLAG_MASK) > 0;
            }
            set
            {
                if (value)
                {
                    Header |= MessageHeader.DUPLICATE_FLAG_MASK;
                }
                else
                {
                    Header &= (byte)~MessageHeader.DUPLICATE_FLAG_MASK;
                }
            }
        }

        public int Retries { get; set; }

        public QualityOfService QualityOfService
        {
            get
            {
                byte val = (byte)(Header & MessageHeader.QOS_FLAG_MASK);
                return (QualityOfService)(val >> MessageHeader.QOS_FLAG_START);
            }
            internal set
            {
                Header &= (byte)~MessageHeader.QOS_FLAG_MASK;
                Header |= (byte)((byte)value << MessageHeader.QOS_FLAG_START);
            }
        }

        public bool Retain
        {
            get
            {
                return (Header & MessageHeader.RETAIN_FLAG_MASK) > 0;
            }
            internal set
            {
                if (value)
                {
                    Header |= MessageHeader.RETAIN_FLAG_MASK;
                }
                else
                {
                    Header &= (byte)~MessageHeader.RETAIN_FLAG_MASK;
                }
            }
        }

        /// <summary>
        /// Does this message expect a response such as an ACK message?
        /// </summary>
        public MessageType ExpectedResponse
        {
            get
            {
                return MessageType.None;
            }
        }

        /// <summary>
        /// Return a new byte array with entire frame serialized into it.
        /// </summary>
        /// <returns></returns>
        public byte[] Serialize()
        {
            var frame = new byte[MsgBuffer.Length];
            Array.Copy(MsgBuffer, 0, frame, 0, MsgBuffer.Length);
            return frame;
        }

        #endregion

        internal byte Header
        {
            get
            {
                return MsgBuffer[0];
            }
            set
            {
                MsgBuffer[0] = value;
            }
        }

        /// <summary>
        /// Sets the message buffer with remaining length, variable header, and payload.
        /// Note, buffer passed in should have byte[0] reserved for the header data which will be copied into it.
        /// </summary>
        /// <param name="newBuffer"></param>
        internal void SetMessageBuffer(byte[] newBuffer)
        {
            newBuffer[0] = Header;
            MsgBuffer = newBuffer;
        }

        /// <summary>
        /// Reads the Remaining Length value from the buffer and returns the index to the byte
        /// immediately after the Remaining Length.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        internal int ReadRemainingLength(out int pos)
        {
            pos = 1;            // Remaining Length byte starts here
            int multiplier = 1;
            int value = 0;
            int digit = 0;

            do
            {
                digit = MsgBuffer[pos++];
                value += (digit & 127) * multiplier;
                multiplier *= 128;
            } while ((digit & 128) != 0);

            return value;
        }
    }
}
