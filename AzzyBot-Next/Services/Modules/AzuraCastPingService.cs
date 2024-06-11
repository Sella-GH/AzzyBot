using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Logging;
using AzzyBot.Services.Interfaces;
using AzzyBot.Utilities.Encryption;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

public sealed class AzuraCastPingService(ILogger<AzuraCastPingService> logger, IQueuedBackgroundTask taskQueue, DbActions dbActions, DiscordBotService discordBotService, WebRequestService webRequestService)
{
    private readonly ILogger<AzuraCastPingService> _logger = logger;
    private readonly IQueuedBackgroundTask _taskQueue = taskQueue;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = discordBotService;
    private readonly WebRequestService _webRequestService = webRequestService;

    public async ValueTask QueueStationPingAsync()
    {
        List<GuildsEntity> guilds = await _dbActions.GetGuildsAsync();
        foreach (AzuraCastEntity azuraCast in guilds.Where(g => g.AzuraCast?.Checks.ServerStatus == true).Select(g => g.AzuraCast!))
        {
            _ = Task.Run(async () => await _taskQueue.QueueBackgroundWorkItemAsync(async ct => await PingInstanceAsync(azuraCast, ct)));
        }
    }

    private async ValueTask PingInstanceAsync(AzuraCastEntity azuraCast, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(azuraCast, nameof(azuraCast));
        ArgumentNullException.ThrowIfNull(azuraCast.Guild, nameof(azuraCast.Guild));

        _logger.BackgroundServiceWorkItem(nameof(PingInstanceAsync));

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            Uri url = new($"{Crypto.Decrypt(azuraCast.BaseUrl)}/api");
            string response = await _webRequestService.GetWebAsync(url);
            DiscordChannel? channel;

            if (!string.IsNullOrWhiteSpace(response))
            {
                if (!azuraCast.IsOnline)
                {
                    await _dbActions.UpdateAzuraCastAsync(azuraCast.Guild.UniqueId, null, null, null, null, true);

                    channel = await _botService.GetDiscordChannelAsync(azuraCast.OutagesChannelId);
                    if (channel is not null)
                        await channel.SendMessageAsync($"AzurCast instance, {Crypto.Decrypt(azuraCast.BaseUrl)}, is reachable again!");
                }

                return;
            }

            await _dbActions.UpdateAzuraCastAsync(azuraCast.Guild.UniqueId, null, null, null, null, false);

            channel = await _botService.GetDiscordChannelAsync(azuraCast.OutagesChannelId);
            if (channel is not null)
                await channel.SendMessageAsync($"AzurCast instance, {Crypto.Decrypt(azuraCast.BaseUrl)}, is not reachable!");
        }
        catch (OperationCanceledException)
        {
            _logger.OperationCanceled(nameof(PingInstanceAsync));
        }
    }
}
