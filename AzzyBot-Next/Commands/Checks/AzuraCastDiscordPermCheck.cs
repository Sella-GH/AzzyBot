using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Services;
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

        if (context is SlashCommandContext ctx && ctx.Interaction.ResponseState is DiscordInteractionResponseState.Unacknowledged)
            await context.DeferResponseAsync();

        int stationId = Convert.ToInt32(context.Arguments.SingleOrDefault(o => o.Key.Name is "station_id" && o.Value is not null).Value, CultureInfo.InvariantCulture);
        ulong guildId = context.Guild.Id;
        GuildsEntity? guild = await _dbActions.GetGuildAsync(guildId);
        if (guild is null)
            return "Guild is null!";

        AzuraCastEntity? azuraCast = guild.AzuraCast;
        if (azuraCast is null)
            return "AzuraCast is null!";

        AzuraCastStationEntity? station = new();
        if (context.Command.FullName.StartsWith("config modify-azuracast-station", StringComparison.OrdinalIgnoreCase))
        {
            station = azuraCast.Stations.FirstOrDefault(s => s.StationId == stationId);
            if (station is null)
                return "Station is null!";
        }

        string? result = null;
        IEnumerable<DiscordRole> userRoles = context.Member.Roles;
        foreach (AzuraCastDiscordPerm perm in attribute.Perms)
        {
            result = CheckPermission(perm, guild, azuraCast, station, userRoles.ToList(), context.Command.FullName);
            if (result is not null)
                break;
        }

        return result;
    }

    private string? CheckPermission(AzuraCastDiscordPerm perm, GuildsEntity guild, AzuraCastEntity azuraCast, AzuraCastStationEntity station, IReadOnlyList<DiscordRole> userRoles, string commandName)
    {
        bool isInstanceAdmin = userRoles.Contains(_botService.GetDiscordRole(guild.UniqueId, azuraCast.InstanceAdminRoleId));
        bool isStationAdmin = false;
        bool isStationDj = false;

        switch (commandName)
        {
            case "azuracast export-playlists":
            case "azuracast force-api-permission-check":
            case "azuracast force-cache-refresh":
            case "azuracast start-station":
            case "azuracast stop-station":
            case "azuracast toggle-song-requests":
            case "azuracast update-instance":
            case "config add-azuracast-station":
            case "config add-azuracast-station-mount":
            case "config modify-azuracast-station":
            case "config modify-azuracast-station-checks":
            case "config delete-azuracast-station-mount":
                isStationAdmin = userRoles.Contains(_botService.GetDiscordRole(guild.UniqueId, station.StationAdminRoleId));
                break;

            case "dj skip-song":
            case "dj switch-playlist":
                isStationDj = userRoles.Contains(_botService.GetDiscordRole(guild.UniqueId, station.StationDjRoleId));
                break;
        }

        if (perm == AzuraCastDiscordPerm.InstanceAdminGroup && isInstanceAdmin)
            return null;

        if (perm == AzuraCastDiscordPerm.StationAdminGroup && (isInstanceAdmin || isStationAdmin))
            return null;

        if (perm == AzuraCastDiscordPerm.StationDJGroup && (isInstanceAdmin || isStationAdmin || isStationDj))
            return null;

        if (perm == AzuraCastDiscordPerm.InstanceAdminGroup)
        {
            return "Instance";
        }
        else if (perm == AzuraCastDiscordPerm.StationAdminGroup)
        {
            return $"Station:{station.Id}";
        }
        else if (perm == AzuraCastDiscordPerm.StationDJGroup)
        {
            return $"DJ:{station.Id}";
        }

        return "Invalid permission!";
    }
}
