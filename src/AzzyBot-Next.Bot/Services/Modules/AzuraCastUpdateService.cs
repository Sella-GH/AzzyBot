using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Bot.Utilities;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Services.Interfaces;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.Modules;

public sealed class AzuraCastUpdateService(ILogger<AzuraCastUpdateService> logger, IQueuedBackgroundTask taskQueue, AzuraCastApiService azuraCastApiService, DbActions dbActions, DiscordBotService botService)
{
    private readonly ILogger<AzuraCastUpdateService> _logger = logger;
    private readonly IQueuedBackgroundTask _taskQueue = taskQueue;
    private readonly AzuraCastApiService _azuraCastApiService = azuraCastApiService;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;

    public void QueueAzuraCastUpdates(IEnumerable<GuildEntity> guilds)
    {
        _logger.BackgroundServiceWorkItem(nameof(QueueAzuraCastUpdates));

        foreach (AzuraCastEntity azuraCast in guilds.Where(g => g.AzuraCast?.IsOnline == true && g.AzuraCast.Checks.Updates).Select(g => g.AzuraCast!))
        {
            _ = Task.Run(async () => await _taskQueue.QueueBackgroundWorkItemAsync(async ct => await CheckForAzuraCastUpdatesAsync(azuraCast, ct)));
        }
    }

    public void QueueAzuraCastUpdates(GuildEntity guild)
    {
        ArgumentNullException.ThrowIfNull(guild, nameof(guild));
        ArgumentNullException.ThrowIfNull(guild.AzuraCast, nameof(guild.AzuraCast));

        _logger.BackgroundServiceWorkItem(nameof(QueueAzuraCastUpdates));

        if (guild.AzuraCast.Checks.Updates)
            _ = Task.Run(async () => await _taskQueue.QueueBackgroundWorkItemAsync(async ct => await CheckForAzuraCastUpdatesAsync(guild.AzuraCast, ct)));
    }

    private async Task CheckForAzuraCastUpdatesAsync(AzuraCastEntity azuraCast, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            string apiKey = Crypto.Decrypt(azuraCast.AdminApiKey);

            AzuraUpdateRecord update = await _azuraCastApiService.GetUpdatesAsync(new(Crypto.Decrypt(azuraCast.BaseUrl)), apiKey);

            if (!update.NeedsReleaseUpdate && !update.NeedsRollingUpdate)
            {
                await _dbActions.UpdateAzuraCastChecksAsync(azuraCast.Guild.UniqueId, null, null, null, 0, DateTime.UtcNow);
                return;
            }

            AzuraCastChecksEntity checks = azuraCast.Checks;
            if (!UpdaterService.CheckUpdateNotification(checks.UpdateNotificationCounter, checks.LastUpdateCheck))
                return;

            await _dbActions.UpdateAzuraCastChecksAsync(azuraCast.Guild.UniqueId, null, null, null, checks.UpdateNotificationCounter + 1, DateTime.UtcNow);

            List<DiscordEmbed> embeds = [EmbedBuilder.BuildAzuraCastUpdatesAvailableEmbed(update)];
            if (azuraCast.Checks.UpdatesShowChangelog)
                embeds.Add(EmbedBuilder.BuildAzuraCastUpdatesChangelogEmbed(update.RollingUpdatesList, update.NeedsRollingUpdate));

            await _botService.SendMessageAsync(azuraCast.Preferences.NotificationChannelId, null, embeds);
        }
        catch (OperationCanceledException)
        {
            _logger.OperationCanceled(nameof(CheckForAzuraCastUpdatesAsync));
        }
    }
}
