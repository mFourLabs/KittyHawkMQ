
using KittyHawk.MqttLib.Interfaces;
using KittyHawk.MqttLib.Settings;

namespace KittyHawk.MqttLib
{
    public static class MqttProtocolInformation
    {
        private static readonly MqttSettings _currentSettings = new MqttPlatformSettings();

        public static string ProtocolName
        {
            get
            {
                return _currentSettings.ProtocolName;
            }
        }

        public static int MajorVersion
        {
            get
            {
                return _currentSettings.MajorVersion;
            }
        }

        public static int MinorVersion
        {
            get
            {
                return _currentSettings.MinorVersion;
            }
        }

        public static IProtocolSettings Settings
        {
            get
            {
                return _currentSettings;
            }
        }

        internal static IInternalSettings InternalSettings
        {
            get
            {
                return _currentSettings;
            }
        }
    }
}
