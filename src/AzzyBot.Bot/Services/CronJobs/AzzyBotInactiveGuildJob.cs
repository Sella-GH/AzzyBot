using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Utilities.Structs;
using AzzyBot.Core.Logging;
using AzzyBot.Data.Entities;

using Microsoft.Extensions.Logging;

using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class AzzyBotInactiveGuildJob(ILogger<AzzyBotInactiveGuildJob> logger, CoreService coreService, DiscordBotService botService) : IJob
{
    private readonly ILogger<AzzyBotInactiveGuildJob> _logger = logger;
    private readonly CoreService _coreService = coreService;
    private readonly DiscordBotService _botService = botService;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        _logger.DatabaseUnusedGuildsStart();

        try
        {
            IReadOnlyDictionary<GuildEntity, AzzyInactiveGuildStruct> unusedGuilds = await _coreService.CheckUnusedGuildsAsync();
            if (unusedGuilds.Count is 0)
                return;

            // Check whether the guilds are due for deletion
            // A guild is due for deletion if the ReminderLeaveDate is set and in the past but only if it is not DateTimeOffset.MinValue
            Dictionary<GuildEntity, AzzyInactiveGuildStruct> deletionGuilds = unusedGuilds
                .Where(static g => g.Key.ReminderLeaveDate != DateTimeOffset.MinValue && g.Key.ReminderLeaveDate <= DateTimeOffset.UtcNow)
                .ToDictionary(static kv => kv.Key, static kv => kv.Value);

            if (deletionGuilds.Count is not 0)
                await _coreService.DeleteUnusedGuildsAsync(deletionGuilds);

            Dictionary<GuildEntity, AzzyInactiveGuildStruct> reminderGuilds = unusedGuilds.Except(deletionGuilds).ToDictionary(static kv => kv.Key, static kv => kv.Value);
            if (reminderGuilds.Count is not 0)
                await _coreService.NotifyUnusedGuildsAsync(reminderGuilds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException or TaskCanceledException)
        {
            await _botService.LogExceptionAsync(ex, DateTimeOffset.Now);
        }
    }
}
