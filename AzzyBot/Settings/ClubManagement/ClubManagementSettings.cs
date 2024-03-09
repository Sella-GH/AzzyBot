using System;
using System.Globalization;

namespace AzzyBot.Settings.ClubManagement;

internal sealed class ClubManagementSettings : BaseSettings
{
    internal static bool ClubManagementSettingsLoaded { get; private set; }
    internal static bool AutomaticClubClosingCheck { get; private set; }
    internal static TimeSpan ClubClosingTimeStart { get; private set; } = TimeSpan.Zero;
    internal static TimeSpan ClubClosingTimeEnd { get; private set; } = TimeSpan.Zero;
    internal static int AzuraAllSongsPlaylist { get; private set; }
    internal static int AzuraClosedPlaylist { get; private set; }
    internal static ulong ClubNotifyChannelId { get; private set; }
    internal static ulong StaffRoleId { get; private set; }
    internal static ulong CloserRoleId { get; private set; }
    internal static ulong EventsRoleId { get; private set; }

    internal static bool LoadClubManagement()
    {
        ArgumentNullException.ThrowIfNull(Config);

        AutomaticClubClosingCheck = Convert.ToBoolean(Config["ClubManagement:AutomaticClubClosingCheck"], CultureInfo.InvariantCulture);

        if (TimeSpan.TryParse(Config["ClubManagement:ClubClosingTimeStart"], out TimeSpan closeStart))
            ClubClosingTimeStart = closeStart;

        if (TimeSpan.TryParse(Config["ClubManagement:ClubClosingTimeEnd"], out TimeSpan closeEnd))
            ClubClosingTimeEnd = closeEnd;

        AzuraAllSongsPlaylist = Convert.ToInt32(Config["ClubManagement:AzuraAllSongsPlaylist"], CultureInfo.InvariantCulture);
        AzuraClosedPlaylist = Convert.ToInt32(Config["ClubManagement:AzuraClosedPlaylist"], CultureInfo.InvariantCulture);
        ClubNotifyChannelId = Convert.ToUInt64(Config["ClubManagement:ClubNotifyChannelId"], CultureInfo.InvariantCulture);
        StaffRoleId = Convert.ToUInt64(Config["ClubManagement:StaffRoleId"], CultureInfo.InvariantCulture);
        CloserRoleId = Convert.ToUInt64(Config["ClubManagement:CloserRoleId"], CultureInfo.InvariantCulture);
        EventsRoleId = Convert.ToUInt64(Config["ClubManagement:EventsRoleId"], CultureInfo.InvariantCulture);

        return ClubManagementSettingsLoaded = CheckSettings(typeof(ClubManagementSettings));
    }
}
