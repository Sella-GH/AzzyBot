using System;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

namespace AzzyBot.Commands.Checks;

public sealed class AzuraCastOnlineCheck(DbActions dbActions) : IContextCheck<AzuraCastOnlineCheckAttribute>
{
    private readonly DbActions _dbActions = dbActions;

    public async ValueTask<string?> ExecuteCheckAsync(AzuraCastOnlineCheckAttribute attribute, CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

        if (context is SlashCommandContext ctx && ctx.Interaction.ResponseState is DiscordInteractionResponseState.Unacknowledged)
            await context.DeferResponseAsync();

        ulong guildId = context.Guild.Id;
        GuildsEntity? guild = await _dbActions.GetGuildAsync(guildId);
        if (guild is null)
            return "Guild is null!";

        AzuraCastEntity? azuraCast = guild.AzuraCast;
        if (azuraCast is null)
            return "AzuraCast is null!";

        if (azuraCast.IsOnline)
            return null;

        return "Offline";
    }
}
