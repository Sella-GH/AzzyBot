using System;
using System.Threading.Tasks;
using AzzyBot.Bot.Utilities.Enums;
using AzzyBot.Core.Logging;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands.Checks;

public sealed class ModuleActivatedCheck(ILogger<ModuleActivatedCheck> logger, DbActions dbActions) : IContextCheck<ModuleActivatedCheckAttribute>
{
    private readonly ILogger<ModuleActivatedCheck> _logger = logger;
    private readonly DbActions _dbActions = dbActions;

    public async ValueTask<string?> ExecuteCheckAsync(ModuleActivatedCheckAttribute attribute, CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Guild);

        if (context is SlashCommandContext ctx && ctx.Interaction.ResponseState is DiscordInteractionResponseState.Unacknowledged)
            await context.DeferResponseAsync();

        switch (attribute.Module)
        {
            case AzzyModules.AzuraCast:
                AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id);
                if (azuraCast is null)
                {
                    _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                    return "AzuraCast is null!";
                }

                return null;

            default:
                return "Module not found!";
        }
    }
}
