using System;
using System.Threading.Tasks;

using AzzyBot.Data.Entities;
using AzzyBot.Data.Logging;
using AzzyBot.Data.Services.Interfaces;

using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands.Checks;

public sealed class AzuraCastOnlineCheck(ILogger<AzuraCastOnlineCheck> logger, IDbActions dbActions) : IContextCheck<AzuraCastOnlineCheckAttribute>
{
    private readonly ILogger<AzuraCastOnlineCheck> _logger = logger;
    private readonly IDbActions _dbActions = dbActions;

    public async ValueTask<string?> ExecuteCheckAsync(AzuraCastOnlineCheckAttribute attribute, CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Guild);

        if (context is SlashCommandContext ctx && ctx.Interaction.ResponseState is DiscordInteractionResponseState.Unacknowledged)
            await context.DeferResponseAsync();

        AzuraCastEntity? azuraCast = await _dbActions.ReadAzuraCastAsync(context.Guild.Id);
        if (azuraCast is null)
        {
            _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
            return "AzuraCast is null!";
        }

        return (azuraCast.IsOnline) ? null : "Offline";
    }
}
