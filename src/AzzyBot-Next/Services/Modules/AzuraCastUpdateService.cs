using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Logging;
using AzzyBot.Services.Interfaces;
using AzzyBot.Utilities;
using AzzyBot.Utilities.Encryption;
using AzzyBot.Utilities.Records.AzuraCast;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

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

        List<GuildsEntity> guilds = await _dbActions.GetGuildsAsync();
        foreach (AzuraCastEntity azuraCast in guilds.Where(g => g.AzuraCast?.IsOnline == true && g.AzuraCast.Checks.Updates).Select(g => g.AzuraCast!))
        {
            _ = Task.Run(async () => await _taskQueue.QueueBackgroundWorkItemAsync(async ct => await CheckForAzuraCastUpdatesAsync(azuraCast, ct)));
        }
    }

    public async ValueTask QueueAzuraCastUpdatesAsync(ulong guildId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(guildId, nameof(guildId));

        _logger.BackgroundServiceWorkItem(nameof(QueueAzuraCastUpdatesAsync));

        GuildsEntity? guild = await _dbActions.GetGuildAsync(guildId);
        if (guild is null || guild.AzuraCast is null)
            return;

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
                return;

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
