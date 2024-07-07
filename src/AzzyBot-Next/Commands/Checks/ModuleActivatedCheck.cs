﻿using System;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Utilities.Enums;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

namespace AzzyBot.Commands.Checks;

public sealed class ModuleActivatedCheck(DbActions dbActions) : IContextCheck<ModuleActivatedCheckAttribute>
{
    private readonly DbActions _dbActions = dbActions;

    public async ValueTask<string?> ExecuteCheckAsync(ModuleActivatedCheckAttribute attribute, CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(attribute, nameof(attribute));
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

        if (context is SlashCommandContext ctx && ctx.Interaction.ResponseState is DiscordInteractionResponseState.Unacknowledged)
            await context.DeferResponseAsync();

        switch (attribute.Module)
        {
            case AzzyModules.AzuraCast:
                AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id);
                if (azuraCast is null)
                    return "AzuraCast is null!";

                return null;

            default:
                return "Module not found!";
        }
    }
}