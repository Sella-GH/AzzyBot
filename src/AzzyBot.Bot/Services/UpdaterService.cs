using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;

using AzzyBot.Bot.Logging;
using AzzyBot.Bot.Models;
using AzzyBot.Bot.Services.Interfaces;
using AzzyBot.Bot.Settings;
using AzzyBot.Bot.Utilities;
using AzzyBot.Core.Utilities;
using AzzyBot.Data.Services.Interfaces;

using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzzyBot.Bot.Services;

public sealed class UpdaterService(ILogger<UpdaterService> logger, IOptions<AzzyBotSettings> botSettings, IOptions<CoreUpdaterSettings> updaterSettings, IDbActions dbActions, IDiscordBotService botService, IWebRequestService webService) : IUpdaterService
{
    private readonly ILogger<UpdaterService> _logger = logger;
    private readonly AzzyBotSettings _botSettings = botSettings.Value;
    private readonly CoreUpdaterSettings _updaterSettings = updaterSettings.Value;
    private readonly IDbActions _dbActions = dbActions;
    private readonly IDiscordBotService _botService = botService;
    private readonly IWebRequestService _webService = webService;
    private DateTimeOffset _lastAzzyUpdateNotificationTime = DateTimeOffset.MinValue;
    private string _lastOnlineVersion = string.Empty;
    private int _azzyNotifyCounter;
    private readonly Uri _latestUrl = new("https://github.com/Sella-GH/AzzyBot/releases/latest");
    private readonly Uri _latestApiUrl = new("https://api.github.com/repos/Sella-GH/AzzyBot/releases/latest");
    private readonly Uri _previewApiUrl = new("https://api.github.com/repos/Sella-GH/AzzyBot/releases");

    private readonly Dictionary<string, string> _headers = new(1)
    {
        ["User-Agent"] = SoftwareStats.GetAppName
    };

    public IReadOnlyDictionary<string, string> GitHubHeaders
        => _headers;

    public async Task CheckForAzzyUpdatesAsync()
    {
        string localVersion = SoftwareStats.GetAppVersion;
        bool isPreview = localVersion.Contains("-preview", StringComparison.OrdinalIgnoreCase);

        string? body = await _webService.GetWebAsync((isPreview) ? _previewApiUrl : _latestApiUrl, _headers, true);
        if (string.IsNullOrEmpty(body))
        {
            _logger.OnlineVersionEmpty();
            return;
        }

        AzzyUpdateModel? updaterModel = (isPreview) ? JsonSerializer.Deserialize(body, JsonSourceGen.Default.ListAzzyUpdateModel)?[0] : JsonSerializer.Deserialize(body, JsonSourceGen.Default.AzzyUpdateModel);
        if (updaterModel is null)
        {
            _logger.OnlineVersionUnserializable();
            return;
        }

        await _dbActions.UpdateAzzyBotAsync(updateLastUpdateCheck: true);

        string onlineVersion = updaterModel.Name;
        if (localVersion == onlineVersion)
            return;

        if (!DateTimeOffset.TryParse(updaterModel.CreatedAt, CultureInfo.CurrentCulture, out DateTimeOffset releaseDate))
            releaseDate = DateTimeOffset.Now;

        await SendUpdateMessageAsync(onlineVersion, releaseDate, updaterModel.Body);
    }

    private async Task SendUpdateMessageAsync(string updateVersion, DateTimeOffset releaseDate, string changelog)
    {
        if (_lastOnlineVersion != updateVersion)
        {
            _lastAzzyUpdateNotificationTime = DateTimeOffset.MinValue;
            _azzyNotifyCounter = 0;
        }

        if (!Misc.CheckUpdateNotification(_azzyNotifyCounter, _lastAzzyUpdateNotificationTime))
            return;

        _lastAzzyUpdateNotificationTime = DateTimeOffset.UtcNow;
        _lastOnlineVersion = updateVersion;
        _azzyNotifyCounter++;

        _logger.UpdateAvailable(updateVersion);

        List<DiscordEmbed> embeds = new(3)
        {
            EmbedBuilder.BuildAzzyUpdatesAvailableEmbed(updateVersion, releaseDate, _latestUrl)
        };

        if (_updaterSettings.DisplayChangelog && !string.IsNullOrEmpty(changelog))
            embeds.Add(EmbedBuilder.BuildAzzyUpdatesChangelogEmbed(changelog, _latestUrl));

        if (_updaterSettings.DisplayInstructions)
            embeds.Add(EmbedBuilder.BuildAzzyUpdatesInstructionsEmbed());

        DiscordGuild? discordGuild = _botService.GetDiscordGuild();
        if (discordGuild is null)
        {
            _logger.DiscordItemNotFound(nameof(DiscordGuild), _botSettings.ServerId);
            return;
        }

        ulong channelId = _botSettings.NotificationChannelId;
        if (channelId is 0)
        {
            DiscordChannel? discordChannel = await discordGuild.GetSystemChannelAsync();
            if (discordChannel is null)
            {
                _logger.DiscordItemNotFound(nameof(DiscordChannel), _botSettings.ServerId);
                return;
            }

            channelId = discordChannel.Id;
        }

        await _botService.SendMessageAsync(channelId, embeds: embeds);
    }
}
