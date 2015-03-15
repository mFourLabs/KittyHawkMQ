
using System;
using System.IO;

namespace KittyHawk.MqttLib.Messages
{
    internal delegate void PopulateBufferDelegate(byte[] buffer, int pos);

    internal class MqttMessageBuilderBase
    {
        /// <summary>
        /// MQTT message type: CONNECT, PUBLISH, SUBSCRIBE, etc.
        /// </summary>
        public MessageType MessageType { get; set; }

        /// <summary>
        /// Duplicate delivery flag.
        /// </summary>
        public bool Duplicate { get; set; }

        /// <summary>
        /// QoS value.
        /// </summary>
        public QualityOfService QualityOfService { get; set; }

        /// <summary>
        /// Retain flag.
        /// </summary>
        public bool Retain { get; set; }

        /// <summary>
        /// Create the message buffer and initialize it.
        /// Populate the message buffer using the supplied delegate. Buffer pos is set to the byte immediately following the Remaining Length value.
        /// The first byte to populate begins with the Variable Header.
        /// </summary>
        /// <param name="msgLength"></param>
        /// <param name="populateBuffer"></param>
        internal byte[] CreateInitializedMessageBuffer(int msgLength, PopulateBufferDelegate populateBuffer)
        {
            if (msgLength > MqttProtocolInformation.Settings.MaxMessageSize)
            {
                throw new IOException("Message length exceeded max MQTT message size of " + MqttProtocolInformation.Settings.MaxMessageSize.ToString());
            }

            // Get the length of the Remaining Length frame
            int arraySize;
            byte[] remainingLenBytes = GetRemainingLengthBytes(msgLength, out arraySize);

            // Now that we know the total message size, lets create the buffer
            byte[] buffer = new byte[msgLength + arraySize + 1];
            buffer[0] = CreateHeader();
            Array.Copy(remainingLenBytes, 0, buffer, 1, arraySize);

            populateBuffer(buffer, arraySize + 1);
            return buffer;
        }

        private byte CreateHeader()
        {
            byte header = 0;
            header |= (byte)((byte)MessageType << MessageHeader.MESSAGE_TYPE_START);

            header &= (byte)~MessageHeader.QOS_FLAG_MASK;
            header |= (byte)((byte)QualityOfService << MessageHeader.QOS_FLAG_START);

            if (Duplicate)
            {
                header |= MessageHeader.DUPLICATE_FLAG_MASK;
            }
            else
            {
                header &= (byte)~MessageHeader.DUPLICATE_FLAG_MASK;
            }

            if (Retain)
            {
                header |= MessageHeader.RETAIN_FLAG_MASK;
            }
            else
            {
                header &= (byte)~MessageHeader.RETAIN_FLAG_MASK;
            }

            return header;
        }

        /// <summary>
        /// Get the buffer representation of the Remaining Length chunk for the specified message length.
        /// When completed, dataSize will be size of the data in the returned array.
        /// </summary>
        /// <param name="messageLength"></param>
        /// <param name="dataSize"></param>
        /// <returns></returns>
        private byte[] GetRemainingLengthBytes(int messageLength, out int dataSize)
        {
            var rm = new byte[4];
            dataSize = 0;

            do
            {
                rm[dataSize] = (byte)(messageLength % 128);
                messageLength = messageLength / 128;

                // If there are more digits to encode, set the top bit of the digit
                if (messageLength > 0)
                {
                    rm[dataSize] |= 0x80;
                }
                dataSize++;
            } while (messageLength > 0);

            return rm;
        }
    }
}
