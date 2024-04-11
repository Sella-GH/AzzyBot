using System;
using System.Globalization;

namespace AzzyBot.Modules.AzuraCast.Settings;

internal sealed class AcSettings : BaseSettings
{
    internal static bool AzuraCastSettingsLoaded { get; private set; }
    internal static bool Ipv6Available { get; private set; }
    internal static bool AutomaticChecksFileChanges { get; private set; }
    internal static bool AutomaticChecksServerPing { get; private set; }
    internal static bool AutomaticChecksUpdates { get; private set; }
    internal static bool AutomaticChecksUpdatesShowChangelog { get; private set; }
    internal static string AzuraApiKey { get; private set; } = string.Empty;
    internal static string AzuraApiUrl { get; private set; } = string.Empty;
    internal static int AzuraStationKey { get; private set; }
    internal static ulong MusicRequestsChannelId { get; private set; }
    internal static ulong OutagesChannelId { get; private set; }
    internal static bool ShowPlaylistsInNowPlaying { get; private set; }

    internal static bool LoadAzuraCast()
    {
        ArgumentNullException.ThrowIfNull(Config);

        Console.Out.WriteLine("Loading AzuraCast Settings");

        Ipv6Available = Convert.ToBoolean(Config["AzuraCast:Ipv6Available"], CultureInfo.InvariantCulture);
        AutomaticChecksFileChanges = Convert.ToBoolean(Config["AzuraCast:AutomaticChecks:FileChanges"], CultureInfo.InvariantCulture);
        AutomaticChecksServerPing = Convert.ToBoolean(Config["AzuraCast:AutomaticChecks:ServerPing"], CultureInfo.InvariantCulture);
        AutomaticChecksUpdates = Convert.ToBoolean(Config["AzuraCast:AutomaticChecks:Updates"], CultureInfo.InvariantCulture);
        AutomaticChecksUpdatesShowChangelog = Convert.ToBoolean(Config["AzuraCast:AutomaticChecks:UpdatesShowChangelog"], CultureInfo.InvariantCulture);
        AzuraApiKey = Config["AzuraCast:AzuraApiKey"] ?? string.Empty;
        AzuraApiUrl = Config["AzuraCast:AzuraApiUrl"] ?? string.Empty;
        AzuraStationKey = Convert.ToInt32(Config["AzuraCast:AzuraStationKey"], CultureInfo.InvariantCulture);
        MusicRequestsChannelId = Convert.ToUInt64(Config["AzuraCast:MusicRequestsChannelId"], CultureInfo.InvariantCulture);
        OutagesChannelId = Convert.ToUInt64(Config["AzuraCast:OutagesChannelId"], CultureInfo.InvariantCulture);
        ShowPlaylistsInNowPlaying = Convert.ToBoolean(Config["AzuraCast:ShowPlaylistsInNowPlaying"], CultureInfo.InvariantCulture);

        return AzuraCastSettingsLoaded = CheckSettings(typeof(AcSettings));
    }
}
