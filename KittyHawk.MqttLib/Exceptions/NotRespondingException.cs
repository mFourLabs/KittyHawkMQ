
using System;

namespace KittyHawk.MqttLib.Exceptions
{
#if WIN_PCL
    internal
#else
    public
#endif
    class NotRespondingException : Exception
    {
#if (MF_FRAMEWORK_VERSION_V4_3 || MF_FRAMEWORK_VERSION_V4_2)
        public int HResult { get; private set; }
#endif

        public NotRespondingException(string message)
            : base(message)
        {
            this.HResult = ErrorCode.ToHResult(ErrorCode.ErrorDevNotExist);
        }
    }
}
