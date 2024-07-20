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

    public async ValueTask QueueAzuraCastUpdatesAsync()
    {
        _logger.BackgroundServiceWorkItem(nameof(QueueAzuraCastUpdatesAsync));

        IReadOnlyList<GuildEntity> guilds = await _dbActions.GetGuildsAsync(true);
        foreach (AzuraCastEntity azuraCast in guilds.Where(g => g.AzuraCast?.IsOnline == true && g.AzuraCast.Checks.Updates).Select(g => g.AzuraCast!))
        {
            _ = Task.Run(async () => await _taskQueue.QueueBackgroundWorkItemAsync(async ct => await CheckForAzuraCastUpdatesAsync(azuraCast, ct)));
        }
    }

    public async ValueTask QueueAzuraCastUpdatesAsync(ulong guildId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(guildId, nameof(guildId));

        _logger.BackgroundServiceWorkItem(nameof(QueueAzuraCastUpdatesAsync));

        GuildEntity? guild = await _dbActions.GetGuildAsync(guildId, true);
        if (guild is null || guild.AzuraCast is null)
        {
            _logger.DatabaseGuildNotFound(guildId);
            return;
        }

        if (guild.AzuraCast.Checks.Updates)
            _ = Task.Run(async () => await _taskQueue.QueueBackgroundWorkItemAsync(async ct => await CheckForAzuraCastUpdatesAsync(guild.AzuraCast, ct)));
    }

    private async ValueTask CheckForAzuraCastUpdatesAsync(AzuraCastEntity azuraCast, CancellationToken cancellationToken)
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

            await _botService.SendMessageAsync(azuraCast.NotificationChannelId, null, embeds);
        }
        catch (OperationCanceledException)
        {
            _logger.OperationCanceled(nameof(CheckForAzuraCastUpdatesAsync));
        }
    }
}
