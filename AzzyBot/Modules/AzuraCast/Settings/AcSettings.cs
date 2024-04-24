using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Modules.AzuraCast.Enums;
using AzzyBot.Modules.Core;

namespace AzzyBot.Modules.AzuraCast.Settings;

internal sealed class AcSettings : BaseSettings
{
    internal static bool AzuraCastSettingsLoaded { get; private set; }
    internal static bool AzuraCastApiKeyIsValid { get; private set; }
    internal static string AzuraApiKey { get; private set; } = string.Empty;
    internal static string AzuraApiUrl { get; private set; } = string.Empty;
    internal static int AzuraStationKey { get; private set; }
    internal static bool Ipv6Available { get; private set; }
    internal static ulong MusicRequestsChannelId { get; private set; }
    internal static ulong OutagesChannelId { get; private set; }
    internal static bool ShowPlaylistsInNowPlaying { get; private set; }
    internal static bool AutomaticChecksFileChanges { get; private set; }
    internal static bool AutomaticChecksServerPing { get; private set; }
    internal static bool AutomaticChecksUpdates { get; private set; }
    internal static bool AutomaticChecksUpdatesShowChangelog { get; private set; }

    internal static async Task<bool> LoadAzuraCastAsync()
    {
        ArgumentNullException.ThrowIfNull(Config);

        LoggerBase.LogInfo(LoggerBase.GetLogger, "Loading AzuraCast Settings", null);

        AzuraApiKey = Config["AzuraCast:AzuraApiKey"] ?? string.Empty;
        AzuraApiUrl = Config["AzuraCast:AzuraApiUrl"] ?? string.Empty;

        if (AzuraApiUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            string host = new Uri(AzuraApiUrl).Host;
            if (IPAddress.TryParse(host, out IPAddress? addr) && addr is not null)
            {
                LoggerBase.LogError(LoggerBase.GetLogger, "You cannot use an IP-Address if you enabled HTTPS!", null);

                return false;
            }
        }

        AzuraStationKey = Convert.ToInt32(Config["AzuraCast:AzuraStationKey"], CultureInfo.InvariantCulture);
        Ipv6Available = Convert.ToBoolean(Config["AzuraCast:Ipv6Available"], CultureInfo.InvariantCulture);
        MusicRequestsChannelId = Convert.ToUInt64(Config["AzuraCast:MusicRequestsChannelId"], CultureInfo.InvariantCulture);
        OutagesChannelId = Convert.ToUInt64(Config["AzuraCast:OutagesChannelId"], CultureInfo.InvariantCulture);
        ShowPlaylistsInNowPlaying = Convert.ToBoolean(Config["AzuraCast:ShowPlaylistsInNowPlaying"], CultureInfo.InvariantCulture);
        AutomaticChecksFileChanges = Convert.ToBoolean(Config["AzuraCast:AutomaticChecks:FileChanges"], CultureInfo.InvariantCulture);
        AutomaticChecksServerPing = Convert.ToBoolean(Config["AzuraCast:AutomaticChecks:ServerPing"], CultureInfo.InvariantCulture);
        AutomaticChecksUpdates = Convert.ToBoolean(Config["AzuraCast:AutomaticChecks:Updates"], CultureInfo.InvariantCulture);
        AutomaticChecksUpdatesShowChangelog = Convert.ToBoolean(Config["AzuraCast:AutomaticChecks:UpdatesShowChangelog"], CultureInfo.InvariantCulture);

        AzuraCastApiKeyIsValid = await CheckIfApiKeyIsValidAsync();
        if (!AzuraCastApiKeyIsValid)
            LoggerBase.LogWarn(LoggerBase.GetLogger, "AzuraCast api key is not valid!", null);

        return AzuraCastSettingsLoaded = CheckSettings(typeof(AcSettings));
    }

    private static async Task<bool> CheckIfApiKeyIsValidAsync()
    {
        if (string.IsNullOrWhiteSpace(AzuraApiKey))
            return false;

        Dictionary<string, string> headers = new()
        {
            ["accept"] = "application/json",
            ["X-API-Key"] = AzuraApiKey
        };

        string url = string.Join("/", AzuraApiUrl, AcApiEnum.station, AzuraStationKey, AcApiEnum.files);

        try
        {
            HttpClient client = (Ipv6Available) ? CoreWebRequests.GetHttpClient(headers) : CoreWebRequests.GetHttpClientV4(headers);

            using (HttpResponseMessage response = await client.GetAsync(new Uri(url)))
            {
                if (response.StatusCode is not HttpStatusCode.OK)
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, ex.Message, null);
            throw;
        }
    }
}
