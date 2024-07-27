using System;
using System.Globalization;
using System.Linq;
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

public sealed class FeatureAvailableCheck(ILogger<FeatureAvailableCheck> logger, DbActions dbActions) : IContextCheck<FeatureAvailableCheckAttribute>
{
    private readonly ILogger<FeatureAvailableCheck> _logger = logger;
    private readonly DbActions _dbActions = dbActions;

    public async ValueTask<string?> ExecuteCheckAsync(FeatureAvailableCheckAttribute attribute, CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(attribute, nameof(attribute));
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

        if (context is SlashCommandContext ctx && ctx.Interaction.ResponseState is DiscordInteractionResponseState.Unacknowledged)
            await context.DeferResponseAsync();

        int stationId = 0;
        if (context is SlashCommandContext slash)
            stationId = Convert.ToInt32(slash.Interaction.Data.Options.Single(o => o.Name is "station").Value, CultureInfo.InvariantCulture);

        switch (attribute.Feature)
        {
            case AzuraCastFeatures.FileUploading:
                AzuraCastStationPreferencesEntity? azuraCastStation = await _dbActions.GetAzuraCastStationPreferencesAsync(context.Guild.Id, stationId);
                if (azuraCastStation is null)
                {
                    _logger.DatabaseAzuraCastStationPreferencesNotFound(context.Guild.Id, 0, stationId);
                    return "AzuraCast station is null!";
                }

                if (azuraCastStation.FileUploadChannelId is not 0)
                    return null;

                return "File uploading is not available for this station!";

            default:
                return "Feature not found!";
        }
    }
}
