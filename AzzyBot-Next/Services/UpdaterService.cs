using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Settings;
using AzzyBot.Utilities;
using AzzyBot.Utilities.Records;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services;

public sealed class UpdaterService(AzzyBotSettingsRecord settings, DiscordBotService botService, WebRequestService webService, ILogger<UpdaterService> logger)
{
    private readonly ILogger<UpdaterService> _logger = logger;
    private readonly AzzyBotSettingsRecord _settings = settings;
    private readonly DiscordBotService _botService = botService;
    private readonly WebRequestService _webService = webService;
    private DateTime _lastAzzyUpdateNotificationTime = DateTime.MinValue;
    private Version _lastOnlineVersion = new(0, 0, 0);
    private int _azzyNotifyCounter;
    private readonly Uri _gitHubUrl = new("https://api.github.com/repos/Sella-GH/AzzyBot/releases/latest");

    public async Task CheckForAzzyUpdatesAsync()
    {
        Version localVersion = new(AzzyStatsSoftware.GetBotVersion);

        Dictionary<string, string> headers = new()
        {
            ["User-Agent"] = AzzyStatsSoftware.GetBotName
        };

        string body = await _webService.GetWebAsync(_gitHubUrl, headers);
        if (string.IsNullOrWhiteSpace(body))
        {
            _logger.OnlineVersionEmpty();
            return;
        }

        UpdateRecord? updaterRecord = JsonSerializer.Deserialize<UpdateRecord>(body);

        if (updaterRecord is null)
        {
            _logger.OnlineVersionUnserializable();
            return;
        }

        Version onlineVersion = new(updaterRecord.Name);
        if (localVersion >= onlineVersion)
            return;

        if (!DateTime.TryParse(updaterRecord.CreatedAt, out DateTime releaseDate))
            releaseDate = DateTime.Now;

        await SendUpdateMessageAsync(onlineVersion, releaseDate, updaterRecord.Body);
    }

    private async Task SendUpdateMessageAsync(Version updateVersion, DateTime releaseDate, string changelog)
    {
        if (_lastOnlineVersion != updateVersion)
        {
            _lastAzzyUpdateNotificationTime = DateTime.MinValue;
            _azzyNotifyCounter = 0;
        }

        if (!ChecKUpdateNotification(_azzyNotifyCounter, _lastAzzyUpdateNotificationTime))
            return;

        _lastAzzyUpdateNotificationTime = DateTime.Now;
        _lastOnlineVersion = updateVersion;
        _azzyNotifyCounter++;

        _logger.UpdateAvailable(updateVersion);

        List<DiscordEmbed> embeds = [EmbedBuilder.BuildAzzyUpdatesAvailableEmbed(updateVersion, releaseDate, _gitHubUrl)];

        if (_settings.Updater.DisplayChangelog)
            embeds.Add(EmbedBuilder.BuildAzzyUpdatesChangelogEmbed(changelog, _gitHubUrl));

        if (_settings.Updater.DisplayInstructions)
            embeds.Add(EmbedBuilder.BuildAzzyUpdatesInstructionsEmbed());

        DiscordGuild? discordGuild = _botService.GetDiscordGuild();
        if (discordGuild is null)
            return;

        ulong channelId = _settings.NotificationChannelId;
        if (channelId is 0)
        {
            DiscordChannel? discordChannel = await discordGuild.GetSystemChannelAsync();
            if (discordChannel is null)
                return;

            channelId = discordChannel.Id;
        }

        await _botService.SendMessageAsync(channelId, null, embeds);
    }

    private static bool ChecKUpdateNotification(int notifyCounter, in DateTime lastNotificationTime)
    {
        DateTime now = DateTime.Now;
        bool dayNotification = false;
        bool halfDayNotification = false;
        bool quarterDayNotification = false;

        if (notifyCounter < 3 && now > lastNotificationTime.AddHours(23).AddMinutes(59))
        {
            dayNotification = true;
        }
        else if (notifyCounter >= 3 && now > lastNotificationTime.AddHours(11).AddMinutes(59))
        {
            halfDayNotification = true;
        }
        else if (notifyCounter >= 7 && now > lastNotificationTime.AddHours(5).AddMinutes(59))
        {
            quarterDayNotification = true;
        }

        return dayNotification || halfDayNotification || quarterDayNotification;
    }
}
