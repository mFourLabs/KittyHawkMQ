
namespace KittyHawk.MqttLib.Settings
{
    internal class MqttPlatformSettings : MqttSettings
    {
        /// <summary>
        /// .Net Micro Framework Specific Settings
        /// </summary>
        public MqttPlatformSettings()
        {
            MaxMessageSize = 4096*4;
        }
    }
}
