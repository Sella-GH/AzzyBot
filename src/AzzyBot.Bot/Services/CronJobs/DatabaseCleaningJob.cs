using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Data.Services.Interfaces;

using DSharpPlus;
using DSharpPlus.Entities;

using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class DatabaseCleaningJob(IDbMaintenance dbMaintenance, DiscordBotService botService, DiscordClient discordClient) : IJob
{
    private readonly IDbMaintenance _dbMaintenance = dbMaintenance;
    private readonly DiscordBotService _botService = botService;
    private readonly DiscordClient _discordClient = discordClient;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        try
        {
            IAsyncEnumerable<DiscordGuild> guilds = _discordClient.GetGuildsAsync(cancellationToken: token);

            await _dbMaintenance.CleanupLeftoverGuildsAsync(guilds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException or TaskCanceledException)
        {
            await _botService.LogExceptionAsync(ex, DateTimeOffset.Now);
        }
    }
}
