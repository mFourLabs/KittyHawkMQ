
using System;

namespace KittyHawk.MqttLib.Exceptions
{
    internal class ErrorContextException : Exception
    {
        public ErrorContextException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
