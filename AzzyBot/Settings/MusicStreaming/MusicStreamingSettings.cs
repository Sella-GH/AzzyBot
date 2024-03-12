using System;
using System.Globalization;

namespace AzzyBot.Settings.MusicStreaming;

internal sealed class MusicStreamingSettings : BaseSettings
{
    internal static bool MusicStreamingSettingsLoaded { get; private set; }
    internal static bool AutoDisconnect { get; private set; }
    internal static int AutoDisconnectTime { get; private set; }

    internal static bool LoadMusicStreaming()
    {
        ArgumentNullException.ThrowIfNull(Config);

        AutoDisconnect = Convert.ToBoolean(Config["MusicStreaming:AutoDisconnect"], CultureInfo.InvariantCulture);
        AutoDisconnectTime = Convert.ToInt32(Config["MusicStreaming:AutoDisconnectTime"], CultureInfo.InvariantCulture);

        return MusicStreamingSettingsLoaded = CheckSettings(typeof(MusicStreamingSettings));
    }
}
