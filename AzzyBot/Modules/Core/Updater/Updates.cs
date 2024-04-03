using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AzzyBot.Modules.Core.Updater;

internal static class Updates
{
    private static DateTime LastNotificationTime;
    private static Version LastOnlineVersion = new(0, 0, 0);
    private static int UpdateNotifyCounter;

    internal static async Task CheckForUpdatesAsync()
    {
        const string gitHubUrl = "https://api.github.com/repos/Sella-GH/AzzyBot/releases/latest";
        Version localVersion = new(CoreAzzyStatsGeneral.GetBotVersion);

        Dictionary<string, string> headers = new()
        {
            ["User-Agent"] = CoreAzzyStatsGeneral.GetBotName
        };
        string body = await CoreWebRequests.GetWebAsync(gitHubUrl, headers);
        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("GitHub release version body is empty");

        UpdaterModel? updaterModel = JsonConvert.DeserializeObject<UpdaterModel>(body) ?? throw new InvalidOperationException("UpdaterModel is null");

        Version updateVersion = new(updaterModel.name);
        if (updateVersion == localVersion)
            return;

        if (!DateTime.TryParse(updaterModel.createdAt, out DateTime releaseDate))
            releaseDate = DateTime.Now;

        await SendUpdateMessageAsync(updateVersion, releaseDate, updaterModel.body);
    }

    private static async Task SendUpdateMessageAsync(Version updateVersion, DateTime releaseDate, string changelog)
    {
        DateTime now = DateTime.Now;
        bool dayNotification = false;
        bool halfDayNotification = false;
        bool quarterDayNotification = false;

        if (LastOnlineVersion != updateVersion)
        {
            LastNotificationTime = DateTime.MinValue;
            UpdateNotifyCounter = 0;
        }

        if (UpdateNotifyCounter < 3 && now > LastNotificationTime.AddHours(23).AddMinutes(59))
        {
            dayNotification = true;
        }
        else if (UpdateNotifyCounter < 7 && now > LastNotificationTime.AddHours(11).AddMinutes(59))
        {
            halfDayNotification = true;
        }
        else if (UpdateNotifyCounter > 7 && now > LastNotificationTime.AddHours(5).AddMinutes(59))
        {
            quarterDayNotification = true;
        }

        if (!dayNotification && !halfDayNotification && !quarterDayNotification)
            return;

        LastNotificationTime = now;
        LastOnlineVersion = updateVersion;
        UpdateNotifyCounter++;

        await AzzyBot.SendMessageAsync(CoreSettings.ErrorChannelId, string.Empty, [CoreEmbedBuilder.BuildUpdatesAvailableEmbed(updateVersion, releaseDate), CoreEmbedBuilder.BuildUpdatesAvailableChangelogEmbed(changelog)]);
    }
}
