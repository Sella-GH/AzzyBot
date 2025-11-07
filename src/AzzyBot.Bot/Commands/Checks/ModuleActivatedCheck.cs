using System;
using System.Threading.Tasks;

using AzzyBot.Bot.Utilities.Enums;
using AzzyBot.Bot.Utilities.Helpers;
using AzzyBot.Core.Logging;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

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

        foreach (AzzyModules module in attribute.Modules)
        {
            switch (module)
            {
                case AzzyModules.AzuraCast:
                    AzuraCastEntity? azuraCast = await _dbActions.ReadAzuraCastAsync(context.Guild.Id);
                    if (azuraCast is null)
                    {
                        _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                        return ModuleCheckMessages.AzuraCastIsNull;
                    }

                    return null;

                case AzzyModules.LegalTerms:
                    GuildEntity? guild = await _dbActions.ReadGuildAsync(context.Guild.Id);
                    if (guild is null)
                    {
                        _logger.DatabaseGuildNotFound(context.Guild.Id);
                        return ModuleCheckMessages.GuildIsNull;
                    }
                    else if (!guild.LegalsAccepted)
                    {
                        return ModuleCheckMessages.LegalsNotAccepted;
                    }

                    return null;

                default:
                    return ModuleCheckMessages.ModuleNotFound;
            }
        }

        return null;
    }
}
