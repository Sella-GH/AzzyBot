using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Services;
using AzzyBot.Utilities.Encryption;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

namespace AzzyBot;

public class AzuraCastDiscordPermCheck(DbActions dbActions, DiscordBotService discordBotService) : IContextCheck<AzuraCastDiscordPermCheckAttribute>
{
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = discordBotService;

    public async ValueTask<string?> ExecuteCheckAsync(AzuraCastDiscordPermCheckAttribute attribute, CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(attribute, nameof(attribute));
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));
        ArgumentNullException.ThrowIfNull(context.Member, nameof(context.Member));

        if (context is SlashCommandContext ctx)
        {
            switch (ctx.Interaction.ResponseState)
            {
                case DiscordInteractionResponseState.Unacknowledged:
                    await context.DeferResponseAsync();
                    return null;

                case DiscordInteractionResponseState.Replied:
                    return "Already replied";
            }
        }

        int stationId = Convert.ToInt32(context.Arguments.Single(o => o.Key.Name is "station_id" && o.Value is not null).Value, CultureInfo.InvariantCulture);
        ulong guildId = context.Guild.Id;
        GuildsEntity guild = await _dbActions.GetGuildAsync(guildId);
        if (guild is null)
            return "Guild is null!";

        AzuraCastEntity? azuraCast = guild.AzuraCast;
        if (azuraCast is null)
            return "AzuraCast is null!";

        AzuraCastStationEntity? station = azuraCast.Stations.FirstOrDefault(s => s.StationId == stationId);
        if (station is null)
            return "Station is null!";

        string userId = context.User.Id.ToString(CultureInfo.InvariantCulture);
        IEnumerable<DiscordRole> userRoles = context.Member.Roles;
        switch (attribute.Perm)
        {
            case AzuraCastDiscordPerm.InstanceOwner:
                if (userId == Crypto.Decrypt(azuraCast.InstanceOwner))
                    return null;

                break;

            case AzuraCastDiscordPerm.InstanceAdminGroup:
                if (userRoles.Contains(_botService.GetDiscordRole(guild.UniqueId, azuraCast.InstanceAdminGroup)))
                    return null;

                break;

            case AzuraCastDiscordPerm.StationOwner:
                if (userId == Crypto.Decrypt(station.StationOwner))
                    return null;

                break;

            case AzuraCastDiscordPerm.StationAdminGroup:
                if (userRoles.Contains(_botService.GetDiscordRole(guild.UniqueId, station.StationAdminGroup)))
                    return null;

                break;

            case AzuraCastDiscordPerm.StationDJGroup:
                if (userRoles.Contains(_botService.GetDiscordRole(guild.UniqueId, station.StationDjGroup)))
                    return null;

                break;
        }

        return "No permission";
    }
}
