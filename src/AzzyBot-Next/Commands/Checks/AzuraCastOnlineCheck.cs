﻿using System;
using System.Threading.Tasks;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using AzzyBot.Core.Logging;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Commands.Checks;

public sealed class AzuraCastOnlineCheck(ILogger<AzuraCastOnlineCheck> logger, DbActions dbActions) : IContextCheck<AzuraCastOnlineCheckAttribute>
{
    private readonly DbActions _dbActions = dbActions;

    public async ValueTask<string?> ExecuteCheckAsync(AzuraCastOnlineCheckAttribute attribute, CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

        if (context is SlashCommandContext ctx && ctx.Interaction.ResponseState is DiscordInteractionResponseState.Unacknowledged)
            await context.DeferResponseAsync();

        AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id);
        if (azuraCast is null)
        {
            logger.DatabaseAzuraCastNotFound(context.Guild.Id);
            return "AzuraCast is null!";
        }

        if (azuraCast.IsOnline)
            return null;

        return "Offline";
    }
}
