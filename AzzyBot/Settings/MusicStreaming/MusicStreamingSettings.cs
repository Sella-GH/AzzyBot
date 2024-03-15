using System;
using System.Globalization;

namespace AzzyBot.Settings.MusicStreaming;

internal sealed class MusicStreamingSettings : BaseSettings
{
    internal static bool MusicStreamingSettingsLoaded { get; private set; }
    internal static bool ActivateLyrics { get; private set; }
    internal static bool AutoDisconnect { get; private set; }
    internal static int AutoDisconnectTime { get; private set; }
    internal static string MountPointStub { get; private set; } = string.Empty;
    internal static string LavalinkPassword { get; private set; } = string.Empty;
    internal static bool DeleteLavalinkLogs { get; private set; }

    internal static bool LoadMusicStreaming()
    {
        ArgumentNullException.ThrowIfNull(Config);

        ActivateLyrics = Convert.ToBoolean(Config["MusicStreaming:ActivateLyrics"], CultureInfo.InvariantCulture);
        AutoDisconnect = Convert.ToBoolean(Config["MusicStreaming:AutoDisconnect"], CultureInfo.InvariantCulture);
        AutoDisconnectTime = Convert.ToInt32(Config["MusicStreaming:AutoDisconnectTime"], CultureInfo.InvariantCulture);
        MountPointStub = Config["MusicStreaming:MountPointStub"] ?? string.Empty;
        LavalinkPassword = Config["MusicStreaming:LavalinkPassword"] ?? string.Empty;
        DeleteLavalinkLogs = Convert.ToBoolean(Config["MusicStreaming:DeleteLavalinkLogs"], CultureInfo.InvariantCulture);

        return MusicStreamingSettingsLoaded = CheckSettings(typeof(MusicStreamingSettings));
    }
}
