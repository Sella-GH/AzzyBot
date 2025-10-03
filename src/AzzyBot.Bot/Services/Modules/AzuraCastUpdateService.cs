using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

using AzzyBot.Bot.Resources;
using AzzyBot.Bot.Utilities;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using DSharpPlus.Entities;

namespace AzzyBot.Bot.Services.Modules;

public sealed class AzuraCastUpdateService(AzuraCastApiService azuraCastApiService, DbActions dbActions, DiscordBotService botService, UpdaterService updaterService, WebRequestService webRequest)
{
    private readonly AzuraCastApiService _azuraCastApiService = azuraCastApiService;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;
    private readonly UpdaterService _updaterService = updaterService;
    private readonly WebRequestService _webRequest = webRequest;

    public async Task CheckForAzuraCastUpdatesAsync(AzuraCastEntity azuraCast, bool forced = false)
    {
        ArgumentNullException.ThrowIfNull(azuraCast);

        string apiKey = Crypto.Decrypt(azuraCast.AdminApiKey);
        Uri baseUrl = new(Crypto.Decrypt(azuraCast.BaseUrl));

        string? body;
        try
        {
            body = await _azuraCastApiService.GetUpdatesAsync(baseUrl, apiKey);
            if (string.IsNullOrEmpty(body))
            {
                await _botService.SendMessageAsync(azuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **administrative updates** endpoint.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException or TaskCanceledException)
        {
            // The server is probably down or something, check if user has uptime monitoring enabled
            // If yes then just exit silently
            if (azuraCast.Checks.ServerStatus)
                return;

            // If no we notify the user that something went wrong
            await _botService.SendMessageAsync(azuraCast.Preferences.NotificationChannelId, $"Failed to check for updates: {ex.Message}");
            return;
        }

        AzuraUpdateRecord? update;
        try
        {
            update = JsonSerializer.Deserialize(body, JsonSourceGen.Default.AzuraUpdateRecord);
        }
        catch (JsonException ex)
        {
            AzuraUpdateErrorRecord? errorRecord = JsonSerializer.Deserialize(body, JsonSourceGen.Default.AzuraUpdateErrorRecord) ?? throw new InvalidOperationException($"Failed to deserialize body: {body}", ex);
            await _botService.SendMessageAsync(azuraCast.Preferences.NotificationChannelId, $"Failed to check for updates: {errorRecord.FormattedMessage}");
            return;
        }

        if (update is null)
        {
            await _botService.SendMessageAsync(azuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **administrative updates** endpoint.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
            return;
        }

        if (!update.NeedsReleaseUpdate && !update.NeedsRollingUpdate)
        {
            await _dbActions.UpdateAzuraCastChecksAsync(azuraCast.Guild.UniqueId, updateNotificationCounter: 0, lastUpdateCheck: true);
            return;
        }

        AzuraCastChecksEntity checks = azuraCast.Checks;
        if (!forced && !UpdaterService.CheckUpdateNotification(checks.UpdateNotificationCounter, checks.LastUpdateCheck))
            return;

        await _dbActions.UpdateAzuraCastChecksAsync(azuraCast.Guild.UniqueId, updateNotificationCounter: checks.UpdateNotificationCounter + 1, lastUpdateCheck: true);

        List<DiscordEmbed> embeds = new(2)
        {
            EmbedBuilder.BuildAzuraCastUpdatesAvailableEmbed(update)
        };

        if (azuraCast.Checks.UpdatesShowChangelog)
        {
            string? onlineChangelog = null;
            if (update.NeedsReleaseUpdate)
            {
                onlineChangelog = await _webRequest.GetWebAsync(new(UriStrings.AzuraCastStableRawUrl), _updaterService.GitHubHeaders);
                if (!string.IsNullOrEmpty(onlineChangelog))
                    onlineChangelog = onlineChangelog.Split("---")[1];
            }

            embeds.Add(EmbedBuilder.BuildAzuraCastUpdatesChangelogEmbed(update.NeedsRollingUpdate, onlineChangelog));
        }

        await _botService.SendMessageAsync(azuraCast.Preferences.NotificationChannelId, embeds: embeds);
    }
}
