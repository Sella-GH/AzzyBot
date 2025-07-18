﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using AzzyBot.Bot.Utilities.Enums;
using AzzyBot.Core.Logging;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands.Checks;

/// <summary>
/// Represents a context check that validates whether a user of a Discord command has the necessary permissions to execute it.
/// </summary>
/// <remarks>
/// This check ensures that specific commands are executed with specific permissions only defined in the database preferences.
/// If the command is executed by an unauthorized user it returns a string and fails.
/// </remarks>
public class AzuraCastDiscordPermCheck(ILogger<AzuraCastDiscordPermCheck> logger, DbActions dbActions) : IContextCheck<AzuraCastDiscordPermCheckAttribute>
{
    private readonly ILogger<AzuraCastDiscordPermCheck> _logger = logger;
    private readonly DbActions _dbActions = dbActions;

    [SuppressMessage("Style", "IDE0066:Convert switch statement to expression", Justification = "Better reading style")]
    public async ValueTask<string?> ExecuteCheckAsync(AzuraCastDiscordPermCheckAttribute attribute, CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Guild);
        ArgumentNullException.ThrowIfNull(context.Member);

        if (context is SlashCommandContext ctx && ctx.Interaction.ResponseState is DiscordInteractionResponseState.Unacknowledged)
            await context.DeferResponseAsync();

        int stationId = Convert.ToInt32(context.Arguments.SingleOrDefault(static o => o.Key.Name is "station" && o.Value is not null).Value, CultureInfo.InvariantCulture);
        AzuraCastEntity? azuraCast = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadPrefs: true, loadStations: true, loadStationPrefs: true);
        if (azuraCast is null)
        {
            _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
            return "AzuraCast is null!";
        }

        bool fillStation;
        switch (context.Command.FullName)
        {
            case "azuracast force-cache-refresh":
                fillStation = stationId is not 0;
                break;

            case "azuracast export-playlists":
            case "azuracast start-station":
            case "azuracast station-nowplaying-embed":
            case "azuracast stop-station":
            case "azuracast toggle-song-requests":
            case "config modify-azuracast-station":
            case "config modify-azuracast-station-checks":
            case "config delete-azuracast-station":
            case "dj delete-song-request":
            case "dj skip-song":
            case "dj switch-playlist":
                fillStation = true;
                break;

            default:
                fillStation = false;
                break;
        }

        AzuraCastStationEntity? station = new();
        if (fillStation)
        {
            station = azuraCast.Stations.SingleOrDefault(o => o.StationId == stationId);
            if (station is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, azuraCast.Id, stationId);
                return "Station is null!";
            }
        }

        string? result = null;
        IReadOnlyList<DiscordRole> guildRoles = await context.Guild.GetRolesAsync();
        IEnumerable<DiscordRole> userRoles = context.Member.Roles;
        foreach (AzuraCastDiscordPerm perm in attribute.Perms)
        {
            result = CheckPermission(perm, guildRoles, azuraCast, station, [.. userRoles], context.Command.FullName);
            if (result is not null)
                break;
        }

        return result;
    }

    private string? CheckPermission(AzuraCastDiscordPerm perm, IReadOnlyList<DiscordRole> guildRoles, AzuraCastEntity azuraCast, AzuraCastStationEntity station, IReadOnlyList<DiscordRole> userRoles, string commandName)
    {
        bool isInstanceAdmin = userRoles.Contains(guildRoles.FirstOrDefault(r => r.Id == azuraCast.Preferences.InstanceAdminRoleId));
        bool isStationAdmin = false;
        bool isStationDj = false;

        switch (commandName)
        {
            case "azuracast export-playlists":
            case "azuracast force-cache-refresh":
            case "azuracast start-station":
            case "azuracast station-nowplaying-embed":
            case "azuracast stop-station":
            case "azuracast toggle-song-requests":
            case "config modify-azuracast-station":
            case "config modify-azuracast-station-checks":
            case "config delete-azuracast-station":
                isStationAdmin = userRoles.Contains(guildRoles.FirstOrDefault(r => r.Id == station.Preferences.StationAdminRoleId));
                break;

            case "dj delete-song-request":
            case "dj skip-song":
            case "dj switch-playlist":
                isStationDj = userRoles.Contains(guildRoles.FirstOrDefault(r => r.Id == station.Preferences.StationDjRoleId));
                break;

            // This is only needed to suppress unnecessary warnings.
            // Will only be extended as needed.
            case "azuracast force-update-check":
                break;

            default:
                _logger.CommandNotFound(commandName);
                break;
        }

        if (perm is AzuraCastDiscordPerm.InstanceAdminGroup && isInstanceAdmin)
            return null;

        if (perm is AzuraCastDiscordPerm.StationAdminGroup && (isInstanceAdmin || isStationAdmin))
            return null;

        if (perm is AzuraCastDiscordPerm.StationDJGroup && (isInstanceAdmin || isStationAdmin || isStationDj))
            return null;

        if (perm is AzuraCastDiscordPerm.InstanceAdminGroup)
        {
            _logger.AzuraCastDiscordPermission(nameof(AzuraCastDiscordPerm.InstanceAdminGroup));
            return "Instance";
        }
        else if (perm is AzuraCastDiscordPerm.StationAdminGroup)
        {
            _logger.AzuraCastDiscordPermission(nameof(AzuraCastDiscordPerm.StationAdminGroup));
            return $"Station:{station.StationId}";
        }
        else if (perm is AzuraCastDiscordPerm.StationDJGroup)
        {
            _logger.AzuraCastDiscordPermission(nameof(AzuraCastDiscordPerm.StationDJGroup));
            return $"DJ:{station.StationId}";
        }

        _logger.AzuraCastDiscordPermission("Invalid permission!");
        return "Invalid permission!";
    }
}
