using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class AzzyBotInactiveGuildJob(DbActions dbActions, DiscordBotService botService) : IJob
{
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        try
        {
            IReadOnlyList<GuildEntity> guilds = await _dbActions.ReadGuildsAsync(loadGuildPrefs: true);
            if (!guilds.Any())
                return;

            //await _botService.HandleInactiveGuildsAsync(guilds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException or TaskCanceledException)
        {
            await _botService.LogExceptionAsync(ex, DateTimeOffset.Now);
        }
    }
}
