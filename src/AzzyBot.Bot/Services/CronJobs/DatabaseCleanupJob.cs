using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Data;
using DSharpPlus;
using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class DatabaseCleanupJob(DbMaintenance dbMaintenance, DiscordClient discordClient) : IJob
{
    private readonly DbMaintenance _dbMaintenance = dbMaintenance;
    private readonly DiscordClient _discordClient = discordClient;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
        => await _dbMaintenance.CleanupLeftoverGuildsAsync(_discordClient.Guilds);
}
