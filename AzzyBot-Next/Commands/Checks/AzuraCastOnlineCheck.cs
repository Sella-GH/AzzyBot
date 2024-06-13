using System;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;

namespace AzzyBot.Commands.Checks;

public sealed class AzuraCastOnlineCheck(DbActions dbActions) : IContextCheck<AzuraCastOnlineCheckAttribute>
{
    private readonly DbActions _dbActions = dbActions;

    public async ValueTask<string?> ExecuteCheckAsync(AzuraCastOnlineCheckAttribute attribute, CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

        await context.DeferResponseAsync();

        ulong guildId = context.Guild.Id;
        GuildsEntity guild = await _dbActions.GetGuildAsync(guildId);
        if (guild is null)
            return null;

        AzuraCastEntity? azuraCast = guild.AzuraCast;
        if (azuraCast is null)
            return null;

        if (azuraCast.IsOnline)
            return "Online";

        return "Offline";
    }
}
