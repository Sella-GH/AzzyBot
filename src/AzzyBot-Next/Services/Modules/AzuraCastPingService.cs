using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Services.Interfaces;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using AzzyBot.Utilities.Records.AzuraCast;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

public sealed class AzuraCastPingService(ILogger<AzuraCastPingService> logger, IQueuedBackgroundTask taskQueue, AzuraCastApiService azuraCast, DbActions dbActions, DiscordBotService discordBotService)
{
    private readonly ILogger<AzuraCastPingService> _logger = logger;
    private readonly IQueuedBackgroundTask _taskQueue = taskQueue;
    private readonly AzuraCastApiService _azuraCast = azuraCast;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = discordBotService;

    public async ValueTask QueueInstancePingAsync()
    {
        IReadOnlyList<GuildsEntity> guilds = await _dbActions.GetGuildsAsync(true);
        foreach (AzuraCastEntity azuraCast in guilds.Where(g => g.AzuraCast?.Checks.ServerStatus == true).Select(g => g.AzuraCast!))
        {
            _ = Task.Run(async () => await _taskQueue.QueueBackgroundWorkItemAsync(async ct => await PingInstanceAsync(azuraCast, ct)));
        }
    }

    public async ValueTask QueueInstancePingAsync(ulong guildId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(guildId, nameof(guildId));

        _logger.BackgroundServiceWorkItem(nameof(QueueInstancePingAsync));

        GuildsEntity? guild = await _dbActions.GetGuildAsync(guildId, true);
        if (guild is null || guild.AzuraCast is null)
        {
            _logger.DatabaseGuildNotFound(guildId);
            return;
        }

        if (guild.AzuraCast.Checks.ServerStatus)
            _ = Task.Run(async () => await _taskQueue.QueueBackgroundWorkItemAsync(async ct => await PingInstanceAsync(guild.AzuraCast, ct)));
    }

    private async ValueTask PingInstanceAsync(AzuraCastEntity azuraCast, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(azuraCast, nameof(azuraCast));
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            Uri uri = new(Crypto.Decrypt(azuraCast.BaseUrl));
            AzuraStatusRecord? status = null;
            try
            {
                status = await _azuraCast.GetInstanceStatusAsync(uri);
            }
            catch (HttpRequestException)
            {
                _logger.BackgroundServiceInstanceStatus(azuraCast.Id, "offline");

                await _dbActions.UpdateAzuraCastAsync(azuraCast.Guild.UniqueId, null, null, null, null, null, false);
                await _botService.SendMessageAsync(azuraCast.OutagesChannelId, $"AzuraCast instance **{uri}** is **down**!");
            }

            if (status is not null)
            {
                _logger.BackgroundServiceInstanceStatus(azuraCast.Id, "online");

                if (!azuraCast.IsOnline)
                {
                    await _dbActions.UpdateAzuraCastAsync(azuraCast.Guild.UniqueId, null, null, null, null, null, true);
                    await _botService.SendMessageAsync(azuraCast.OutagesChannelId, $"AzuraCast instance **{uri}** is **up** again!");
                }
            }
            else
            {
                _logger.BackgroundServiceInstanceStatus(azuraCast.Id, "unkown or offline");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.OperationCanceled(nameof(PingInstanceAsync));
        }
    }
}
