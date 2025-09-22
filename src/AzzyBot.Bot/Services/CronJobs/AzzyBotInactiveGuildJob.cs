using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Bot.Services.Modules;
using AzzyBot.Data.Entities;

using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class AzzyBotInactiveGuildJob(CoreService coreService, DiscordBotService botService) : IJob
{
    private readonly CoreService _coreService = coreService;
    private readonly DiscordBotService _botService = botService;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        try
        {
            Dictionary<GuildEntity, string> unusedGuilds = await _coreService.CheckUnusedGuildsAsync();
            if (unusedGuilds.Count is 0)
                return;

            await _coreService.NotifyUnusedGuildsAsync(unusedGuilds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException or TaskCanceledException)
        {
            await _botService.LogExceptionAsync(ex, DateTimeOffset.Now);
        }
    }
}
