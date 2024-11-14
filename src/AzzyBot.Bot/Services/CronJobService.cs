using AzzyBot.Bot.Services.CronJobs;
using NCronJob;

namespace AzzyBot.Bot.Services;

public sealed class CronJobService(IRuntimeJobRegistry registry)
{
    private readonly IRuntimeJobRegistry _registry = registry;

    public void AddStartupJobs()
    {
        _registry.AddJob(b => b.AddJob<DatabaseCleanupJob>(j => j.WithCronExpression("0 0 * * *").WithName(nameof(DatabaseCleanupJob))));
        _registry.AddJob(b => b.AddJob<AzzyBotUpdateCheckJob>(j => j.WithCronExpression("0 */6 * * *").WithName(nameof(AzzyBotUpdateCheckJob))));
        _registry.AddJob(b => b.AddJob<AzzyBotGlobalChecksJob>(j => j.WithCronExpression("* */15 * * *").WithName(nameof(AzzyBotGlobalChecksJob))));
    }
}
