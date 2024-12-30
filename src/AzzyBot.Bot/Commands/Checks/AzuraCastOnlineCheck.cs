using System;
using System.Threading.Tasks;

using AzzyBot.Core.Logging;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands.Checks;

public sealed class AzuraCastOnlineCheck(ILogger<AzuraCastOnlineCheck> logger, DbActions dbActions) : IContextCheck<AzuraCastOnlineCheckAttribute>
{
    private readonly DbActions _dbActions = dbActions;

    public async ValueTask<string?> ExecuteCheckAsync(AzuraCastOnlineCheckAttribute attribute, CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Guild);

        if (context is SlashCommandContext ctx && ctx.Interaction.ResponseState is DiscordInteractionResponseState.Unacknowledged)
            await context.DeferResponseAsync();

        AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id);
        if (azuraCast is null)
        {
            logger.DatabaseAzuraCastNotFound(context.Guild.Id);
            return "AzuraCast is null!";
        }

        return (azuraCast.IsOnline) ? null : "Offline";
    }
}
