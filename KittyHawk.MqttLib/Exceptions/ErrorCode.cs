
namespace KittyHawk.MqttLib.Exceptions
{
    internal static class ErrorCode
    {
        public static int ToHResult(int win32)
        {
            if (win32 <= 0)
            {
                return win32;
            }

            return (int)((uint)(win32 & 0x0000FFFF) | (uint)0x80000000);
        }

        public static int ErrorAccessDenied = 0x5;          // Access is denied.
        public static int ErrorDevNotExist = 0x37;          // The specified network resource or device is no longer available.
        public static int ErrorUnexpNetErr = 0x3B;          // An unexpected network error occurred.
        public static int ErrorNetworkAccessDenied = 0x41;  // Network access is denied.
    }
}
