using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Bot.Services.Interfaces;
using AzzyBot.Bot.Structs;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services.Interfaces;

using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class AzzyBotCheckPermissionsJob(IDbActions dbActions, IDiscordBotService botService) : IJob
{
    private readonly IDbActions _dbActions = dbActions;
    private readonly IDiscordBotService _botService = botService;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            if (context.Parameter is AzzyCheckPermissionsStruct permissionsStruct)
            {
                if (permissionsStruct.DiscordGuild is not null && permissionsStruct.DiscordGuildIds is not null)
                {
                    await _botService.CheckPermissionsAsync(permissionsStruct.DiscordGuild, [.. permissionsStruct.DiscordGuildIds]);
                    return;
                }

                if (permissionsStruct.GuildEntity is not null)
                {
                    await _botService.CheckPermissionsAsync(permissionsStruct.GuildEntity);
                    return;
                }
            }

            IReadOnlyList<GuildEntity> guilds = await _dbActions.ReadGuildsAsync(loadEverything: true);
            if (guilds.Count is 0)
                return;

            await _botService.CheckPermissionsAsync(guilds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException or TaskCanceledException)
        {
            await _botService.LogExceptionAsync(ex, DateTimeOffset.Now);
        }
    }
}
