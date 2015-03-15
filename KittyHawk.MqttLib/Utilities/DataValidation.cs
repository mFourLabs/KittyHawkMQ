
using System;
using System.Text;
using KittyHawk.MqttLib.Settings;

namespace KittyHawk.MqttLib.Utilities
{
    internal static class DataValidation
    {
        internal static void ValidateInt16(int data, string name)
        {
            if (data < 0)
            {
                throw new ArgumentException(name + " cannot be negative.");
            }
            if (data > 0xFFFF)
            {
                throw new ArgumentException(name + " value is greater than the maximum allowable.");
            }
        }

        internal static void ValidateClientId(string data)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Client ID must be set to a non-empty string.");
            }
            if (data.Length > 23)
            {
                throw new ArgumentException("Client ID must be less than or equal to 23 characters.");
            }
            ValidateString(data, "Client ID");
        }

        internal static void ValidateStringAndLength(string data, string name)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException(name + " must be set to a non-empty string.");
            }
            ValidateString(data, name);
        }

        internal static void ValidateString(string data, string name)
        {
            if (data.Length > 0xFFFF)
            {
                throw new ArgumentException(name + " must be less than or equal to 64K characters.");
            }
            // string must only contain ASCII characters
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            if (data.Length != bytes.Length)
            {
                throw new ArgumentException(name + " must contain only ASCII characters.");
            }
        }

        private const int MaxMessageSizeDefault = 268435455;    // 256MB
        internal static void ValidateMaxMessageSize(int data)
        {
            if (data > MaxMessageSizeDefault)
            {
                throw new ArgumentException("MaxMessageSize value is greater than the maximum allowable.");
            }
            if (data <= 0)
            {
                throw new ArgumentException("MaxMessageSize value is less than the minimum allowable.");
            }
        }

        internal static void ValidateMessageId(int data)
        {
            if (data == 0)
            {
                throw new ArgumentException("0 in not a valid message ID.");
            }
            ValidateInt16(data, "MessageID");
        }
    }
}
