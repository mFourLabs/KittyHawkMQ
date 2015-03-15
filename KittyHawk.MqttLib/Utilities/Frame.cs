
using System;
using System.Text;

namespace KittyHawk.MqttLib.Utilities
{
    internal static class Frame
    {
        /// <summary>
        /// Return the bit position of the specified flag. Assumption: The flag passed in has a signle bit set.
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        public static int GetBitPosition(byte flag)
        {
            if (flag == 0x0)
            {
                throw new ArgumentOutOfRangeException("flag", "Parameter cannot be 0.");
            }

            int pos = 0;
            while (flag > 1)
            {
                flag /= 2;
                pos++;
            }
            return pos;
        }

        /// <summary>
        /// Encode a string for MQTT and append it to the byte array beginning at the StartAt dataSize.
        /// Returns the index to the byte after the encoded string.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="dest"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static void EncodeString(string str, byte[] dest, ref int pos)
        {
            byte[] utf8Str = Encoding.UTF8.GetBytes(str);

            dest[pos++] = (byte)((utf8Str.Length & 0xFF00) >> 8);
            dest[pos++] = (byte)(utf8Str.Length & 0x00FF);

            if (str.Length > 0)
            {
                Array.Copy(utf8Str, 0, dest, pos, utf8Str.Length);
            }   

            pos += utf8Str.Length;
        }

        /// <summary>
        /// Decode the UTF-8 string beginning at pos within the buffer. When complete, pos will point
        /// to the byte immediately after the string in the buffer.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static string DecodeString(byte[] src, ref int pos)
        {
            var encoder = new UTF8Encoding();
            var sb = new StringBuilder();
            int length = Frame.DecodeInt16(src, ref pos);

            if (length > 0)
            {
                int start = pos;
                pos += length;

                char[] chars = encoder.GetChars(src, start, length);
                sb.Append(chars);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Encode the 16 bit value into the frame buffer at the specified position.
        /// </summary>
        /// <param name="val"></param>
        /// <param name="dest"></param>
        /// <param name="pos"></param>
        public static void EncodeInt16(int val, byte[] dest, ref int pos)
        {
            dest[pos++] = (byte)(val >> 8);     // MSB
            dest[pos++] = (byte)(val & 0x00FF); // LSB
        }

        /// <summary>
        /// Read the 16 bit value in the frame buffer starting at the specified position.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static int DecodeInt16(byte[] src, ref int pos)
        {
            if (pos >= src.Length)
            {
                return 0;
            }
            byte msb = src[pos++];
            byte lsb = src[pos++];
            return (msb << 8) | lsb;
        }
    }
}
