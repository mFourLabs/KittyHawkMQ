using System;
#if NETFX_CORE
using System.Runtime.InteropServices.WindowsRuntime;
#endif

namespace KittyHawk.MqttLib.Messages
{
    internal sealed class MqttFixedHeader
    {
        private readonly byte[] _buffer = new byte[5];
        private int _bufferPos;

        /// <summary>
        /// True if the fixed header is complete.
        /// </summary>
        public bool IsComplete { get; private set; }

        /// <summary>
        /// Gets the size of the fixed header.
        /// </summary>
        public int HeaderSize
        {
            get
            {
                if (!IsComplete)
                {
                    throw new InvalidOperationException("Header size cannot be determined until the fixed header is complete.");
                }
                return _bufferPos;
            }
        }

        public MessageType MessageType
        {
            get
            {
                return MqttMessageDeserializer.ReadMessageTypeFromHeader(_buffer[0]);
            }
        }

        public byte[] Buffer
        {
            get { return _buffer; }
        }

        /// <summary>
        /// Creates a correctly sized message buffer with the fixed header populated at the front. Buffer is ready to be
        /// filled with receivced data from the server. Start filling the buffer at position=HeaderSize.
        /// </summary>
        /// <returns>Returns a message buffer with the fixed header populated at the front.</returns>
        public byte[] CreateMessageBuffer()
        {
            if (!IsComplete)
            {
                throw new InvalidOperationException("Cannot prepend until the fixed header is complete.");
            }

            var msgBuffer = new byte[RemainingLength + HeaderSize];
            PrependFixedHeader(msgBuffer);
            return msgBuffer;
        }

        /// <summary>
        /// Prepend the fixed header data at the beginning of the specified buffer.
        /// </summary>
        /// <param name="msgBuffer"></param>
        private void PrependFixedHeader(
#if NETFX_CORE
            [WriteOnlyArray]
#endif
            byte[] msgBuffer
            )
        {
            for (int i = 0; i < HeaderSize; i++)
            {
                msgBuffer[i] = _buffer[i];
            }
        }

        /// <summary>
        /// Append the next byte to the fixed header.
        /// </summary>
        /// <param name="b"></param>
        /// <returns>Returns true if more bytes are expected. False if the header is complete.</returns>
        public bool AppendByte(byte b)
        {
            if (IsComplete)
            {
                throw new InvalidOperationException("The fixed header is complete. Cannot append more data to it.");
            }

            if (_bufferPos > 4)
            {
                throw new InvalidOperationException("Unexpected fixed header data.");
            }

            _buffer[_bufferPos] = b;
            _bufferPos++;

            IsComplete = ((_bufferPos != 1) && (b & 128) == 0);
            return !IsComplete;
        }

        /// <summary>
        /// Gets the remaining buffer length required to complete the MQTT message.
        /// </summary>
        public int RemainingLength
        {
            get
            {
                int pos = 1;            // Remaining Length byte starts here
                int multiplier = 1;
                int value = 0;
                int digit = 0;

                do
                {
                    digit = _buffer[pos++];
                    value += (digit & 127) * multiplier;
                    multiplier *= 128;
                } while ((digit & 128) != 0);

                return value;
            }
        }
    }
}