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
        ArgumentNullException.ThrowIfNull(attribute, nameof(attribute));
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));
        ArgumentNullException.ThrowIfNull(context.Channel, nameof(context.Channel));

        if (context is SlashCommandContext ctx && ctx.Interaction.ResponseState is DiscordInteractionResponseState.Unacknowledged)
            await context.DeferResponseAsync();

        int stationId = Convert.ToInt32(context.Arguments.SingleOrDefault(o => o.Key.Name is "station" && o.Value is not null).Value, CultureInfo.InvariantCulture);
        AzuraCastStationEntity? station = await _dbActions.GetAzuraCastStationAsync(context.Guild.Id, stationId);
        if (station is null)
        {
            _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, 0, stationId);
            return "AzuraCastStation is null!";
        }

        ulong channelId;
        switch (context.Command.Name)
        {
            case "upload-files":
                channelId = station.FileUploadChannelId;
                break;

            default:
                return null;
        }

        if (context.Channel.Id != channelId)
            return channelId.ToString(CultureInfo.InvariantCulture);

        return null;
    }
}
