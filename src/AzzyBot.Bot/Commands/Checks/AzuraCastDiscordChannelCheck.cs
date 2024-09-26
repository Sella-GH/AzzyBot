using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Core.Logging;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands.Checks;

public sealed class AzuraCastDiscordChannelCheck(ILogger<AzuraCastDiscordChannelCheck> logger, DbActions dbActions) : IContextCheck<AzuraCastDiscordChannelCheckAttribute>
{
    private readonly ILogger<AzuraCastDiscordChannelCheck> _logger = logger;
    private readonly DbActions _dbActions = dbActions;

    public async ValueTask<string?> ExecuteCheckAsync(AzuraCastDiscordChannelCheckAttribute attribute, CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Guild);
        ArgumentNullException.ThrowIfNull(context.Channel);

        if (context is SlashCommandContext ctx && ctx.Interaction.ResponseState is DiscordInteractionResponseState.Unacknowledged)
            await context.DeferResponseAsync();

        int stationId = Convert.ToInt32(context.Arguments.SingleOrDefault(static o => o.Key.Name is "station" && o.Value is not null).Value, CultureInfo.InvariantCulture);
        AzuraCastStationPreferencesEntity? prefs = await _dbActions.GetAzuraCastStationPreferencesAsync(context.Guild.Id, stationId);
        if (prefs is null)
        {
            _logger.DatabaseAzuraCastStationPreferencesNotFound(context.Guild.Id, 0, stationId);
            return "AzuraCast station preferences not found";
        }

        ulong channelId;
        switch (context.Command.FullName)
        {
            case "music search-song":
                channelId = prefs.RequestsChannelId;
                break;

            case "music upload-files":
                channelId = prefs.FileUploadChannelId;
                break;

            default:
                return null;
        }

        return (context.Channel.Id != channelId) ? channelId.ToString(CultureInfo.InvariantCulture) : null;
    }
}
