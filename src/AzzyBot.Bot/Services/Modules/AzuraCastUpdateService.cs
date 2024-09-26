using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Bot.Utilities;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.Modules;

public sealed class AzuraCastUpdateService(ILogger<AzuraCastUpdateService> logger, AzuraCastApiService azuraCastApiService, DbActions dbActions, DiscordBotService botService)
{
    private readonly ILogger<AzuraCastUpdateService> _logger = logger;
    private readonly AzuraCastApiService _azuraCastApiService = azuraCastApiService;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;

    public async Task CheckForAzuraCastUpdatesAsync(AzuraCastEntity azuraCast, CancellationToken cancellationToken, bool forced = false)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(azuraCast);

        try
        {
            string apiKey = Crypto.Decrypt(azuraCast.AdminApiKey);

            string? body = await _azuraCastApiService.GetUpdatesAsync(new(Crypto.Decrypt(azuraCast.BaseUrl)), apiKey);
            if (string.IsNullOrWhiteSpace(body))
            {
                await _botService.SendMessageAsync(azuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **administrative updates** endpoint.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }

            AzuraUpdateRecord? update = null;
            try
            {
                update = JsonSerializer.Deserialize<AzuraUpdateRecord>(body);
            }
            catch (JsonException ex)
            {
                AzuraUpdateErrorRecord? errorRecord = JsonSerializer.Deserialize<AzuraUpdateErrorRecord>(body) ?? throw new InvalidOperationException($"Failed to deserialize body: {body}", ex);
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
                await _dbActions.UpdateAzuraCastChecksAsync(azuraCast.Guild.UniqueId, updateNotificationCounter: 0, lastUpdateCheck: DateTime.UtcNow);
                return;
            }

            AzuraCastChecksEntity checks = azuraCast.Checks;
            if (!forced && !UpdaterService.CheckUpdateNotification(checks.UpdateNotificationCounter, checks.LastUpdateCheck))
                return;

            await _dbActions.UpdateAzuraCastChecksAsync(azuraCast.Guild.UniqueId, updateNotificationCounter: checks.UpdateNotificationCounter + 1, lastUpdateCheck: DateTime.UtcNow);

            List<DiscordEmbed> embeds = new(2)
            {
                EmbedBuilder.BuildAzuraCastUpdatesAvailableEmbed(update)
            };

            if (azuraCast.Checks.UpdatesShowChangelog)
                embeds.Add(EmbedBuilder.BuildAzuraCastUpdatesChangelogEmbed(update.RollingUpdatesList, update.NeedsRollingUpdate));

            await _botService.SendMessageAsync(azuraCast.Preferences.NotificationChannelId, embeds: embeds);
        }
        catch (OperationCanceledException)
        {
            _logger.OperationCanceled(nameof(CheckForAzuraCastUpdatesAsync));
        }
    }
}
