using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Bot.Services.Modules;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class AzuraStatusPingJob(AzuraCastPingService pingService, DbActions dbActions, DiscordBotService botService) : IJob
{
    private readonly AzuraCastPingService _pingService = pingService;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        try
        {
            IReadOnlyList<GuildEntity> guilds = await _dbActions.GetGuildsAsync(loadEverything: true);
            if (!guilds.Any())
                return;

            foreach (GuildEntity guild in guilds.Where(guild => guild.AzuraCast?.Checks.ServerStatus is true))
            {
                await _pingService.PingInstanceAsync(guild.AzuraCast!);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException or TaskCanceledException)
        {
            await _botService.LogExceptionAsync(ex, DateTimeOffset.Now);
        }
    }
}
