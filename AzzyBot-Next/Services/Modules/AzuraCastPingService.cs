using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Logging;
using AzzyBot.Services.Interfaces;
using AzzyBot.Utilities.Encryption;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

public sealed class AzuraCastPingService(ILogger<AzuraCastPingService> logger, IQueuedBackgroundTask taskQueue, DbActions dbActions, DiscordBotService discordBotService, WebRequestService webRequestService)
{
    private readonly ILogger<AzuraCastPingService> _logger = logger;
    private readonly IQueuedBackgroundTask _taskQueue = taskQueue;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = discordBotService;
    private readonly WebRequestService _webRequestService = webRequestService;

    public async ValueTask QueueInstancePingAsync()
    {
        List<GuildsEntity> guilds = await _dbActions.GetGuildsAsync();
        foreach (AzuraCastEntity azuraCast in guilds.Where(g => g.AzuraCast?.Checks.ServerStatus == true).Select(g => g.AzuraCast!))
        {
            _ = Task.Run(async () => await _taskQueue.QueueBackgroundWorkItemAsync(async ct => await PingInstanceAsync(azuraCast, ct)));
        }
    }

    public async ValueTask QueueInstancePingAsync(ulong guildId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(guildId, nameof(guildId));

        _logger.BackgroundServiceWorkItem(nameof(QueueInstancePingAsync));

        GuildsEntity guild = await _dbActions.GetGuildAsync(guildId);
        if (guild.AzuraCast is null)
            return;

        if (guild.AzuraCast.Checks.ServerStatus)
            _ = Task.Run(async () => await _taskQueue.QueueBackgroundWorkItemAsync(async ct => await PingInstanceAsync(guild.AzuraCast, ct)));
    }

    private async ValueTask PingInstanceAsync(AzuraCastEntity azuraCast, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(azuraCast, nameof(azuraCast));
        ArgumentNullException.ThrowIfNull(azuraCast.Guild, nameof(azuraCast.Guild));

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            Uri url = new($"{Crypto.Decrypt(azuraCast.BaseUrl)}/api");
            string response = string.Empty;

            try
            {
                response = await _webRequestService.GetWebAsync(url);
            }
            catch (HttpRequestException)
            {
                _logger.BackgroundServiceInstanceStatus(azuraCast.Id, "offline");

                await _dbActions.UpdateAzuraCastAsync(azuraCast.Guild.UniqueId, null, null, null, null, false);
                await _botService.SendMessageAsync(azuraCast.OutagesChannelId, $"AzurCast instance, **{Crypto.Decrypt(azuraCast.BaseUrl)}**, is not reachable!");
            }

            if (!string.IsNullOrWhiteSpace(response))
            {
                _logger.BackgroundServiceInstanceStatus(azuraCast.Id, "online");

                if (!azuraCast.IsOnline)
                {
                    await _dbActions.UpdateAzuraCastAsync(azuraCast.Guild.UniqueId, null, null, null, null, true);
                    await _botService.SendMessageAsync(azuraCast.OutagesChannelId, $"AzurCast instance, **{Crypto.Decrypt(azuraCast.BaseUrl)}**, is reachable again!");
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
