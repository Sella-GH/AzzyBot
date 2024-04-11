using System;
using System.Globalization;

namespace AzzyBot.Modules.AzuraCast.Settings;

internal sealed class AcSettings : BaseSettings
{
    internal static bool AzuraCastSettingsLoaded { get; private set; }
    internal static bool Ipv6Available { get; private set; }
    internal static bool AutomaticFileChangeCheck { get; private set; }
    internal static bool AutomaticServerPing { get; private set; }
    internal static bool AutomaticUpdateCheck { get; private set; }
    internal static string AzuraApiKey { get; private set; } = string.Empty;
    internal static string AzuraApiUrl { get; private set; } = string.Empty;
    internal static int AzuraStationKey { get; private set; }
    internal static ulong MusicRequestsChannelId { get; private set; }
    internal static ulong OutagesChannelId { get; private set; }

    internal static bool LoadAzuraCast()
    {
        ArgumentNullException.ThrowIfNull(Config);

        Console.Out.WriteLine("Loading AzuraCast Settings");

        Ipv6Available = Convert.ToBoolean(Config["AzuraCast:Ipv6Available"], CultureInfo.InvariantCulture);
        AutomaticFileChangeCheck = Convert.ToBoolean(Config["AzuraCast:AutomaticFileChangeCheck"], CultureInfo.InvariantCulture);
        AutomaticServerPing = Convert.ToBoolean(Config["AzuraCast:AutomaticServerPing"], CultureInfo.InvariantCulture);
        AutomaticUpdateCheck = Convert.ToBoolean(Config["AzuraCast:AutomaticUpdateCheck"], CultureInfo.InvariantCulture);
        AzuraApiKey = Config["AzuraCast:AzuraApiKey"] ?? string.Empty;
        AzuraApiUrl = Config["AzuraCast:AzuraApiUrl"] ?? string.Empty;
        AzuraStationKey = Convert.ToInt32(Config["AzuraCast:AzuraStationKey"], CultureInfo.InvariantCulture);
        MusicRequestsChannelId = Convert.ToUInt64(Config["AzuraCast:MusicRequestsChannelId"], CultureInfo.InvariantCulture);
        OutagesChannelId = Convert.ToUInt64(Config["AzuraCast:OutagesChannelId"], CultureInfo.InvariantCulture);

        return AzuraCastSettingsLoaded = CheckSettings(typeof(AcSettings));
    }
}
