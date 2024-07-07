﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AzzyBot.Commands.Autocompletes;
using AzzyBot.Commands.Checks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Logging;
using AzzyBot.Services.Modules;
using AzzyBot.Utilities;
using AzzyBot.Utilities.Enums;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Commands;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "DSharpPlus best practice")]
public sealed class ConfigCommands
{
    [Command("config"), RequireGuild, RequirePermissions(DiscordPermissions.None, DiscordPermissions.Administrator)]
    public sealed class ConfigGroup(ILogger<ConfigGroup> logger, AzzyBackgroundService backgroundService, DbActions db)
    {
        private readonly ILogger<ConfigGroup> _logger = logger;
        private readonly AzzyBackgroundService _backgroundService = backgroundService;
        private readonly DbActions _db = db;

        [Command("add-azuracast"), Description("Add an AzuraCast instance to your server. This is a requirement to use the features.")]
        public async ValueTask AddAzuraCastAsync
            (
            CommandContext context,
            [Description("Set the base Url, an example: https://demo.azuracast.com/")] Uri url,
            [Description("Add an administrator api key. It's enough when it has the permission to access system information.")] string apiKey,
            [Description("Select the group that has the admin permissions on this instance.")] DiscordRole instanceAdminGroup,
            [Description("Select a channel to get general notifications about your azuracast installation."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel notificationChannel,
            [Description("Select a channel to get notifications when your azuracast installation is down."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel outagesChannel,
            [Description("Enable or disable the automatic check if the AzuraCast instance of your server is down.")] bool serverStatus,
            [Description("Enable or disable the automatic check for AzuraCast updates.")] bool updates,
            [Description("Enable or disable the addition of the changelog to the posted AzuraCast updates.")] bool updatesChangelog
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(AddAzuraCastAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            if (instanceAdminGroup is null)
            {
                await context.DeleteResponseAsync();
                await context.FollowupAsync("You have to select an instance admin group first!");
                return;
            }

            if (notificationChannel is null)
            {
                await context.DeleteResponseAsync();
                await context.FollowupAsync("You have to select a notification channel first!");
                return;
            }

            if (outagesChannel is null)
            {
                await context.DeleteResponseAsync();
                await context.FollowupAsync("You have to select an outages channel first!");
                return;
            }

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            GuildsEntity? guild = await _db.GetGuildAsync(guildId);
            if (guild is null)
            {
                await context.EditResponseAsync("Server not found in database.");
                return;
            }

            if (!guild.ConfigSet)
            {
                await context.DeleteResponseAsync();
                await context.FollowupAsync("You have to set the core settings first!");
                return;
            }

            if (guild.AzuraCast is not null)
            {
                await context.DeleteResponseAsync();
                await context.FollowupAsync("AzuraCast is already set up for your server.");
                return;
            }

            await _db.AddAzuraCastAsync(guildId, url, apiKey, instanceAdminGroup.Id, notificationChannel.Id, outagesChannel.Id, serverStatus, updates, updatesChangelog);

            await context.DeleteResponseAsync();
            await context.FollowupAsync("Your AzuraCast installation was added successfully and private data has been encrypted.");

            await _backgroundService.StartAzuraCastBackgroundServiceAsync(AzuraCastChecks.CheckForOnlineStatus, guild.UniqueId);
        }

        [Command("add-azuracast-station"), Description("Add an AzuraCast station to your instance."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask AddAzuraCastStationAsync
            (
            CommandContext context,
            [Description("Enter the station id of your azuracast station.")] int station,
            [Description("Enter the name of the new station.")] string stationName,
            [Description("Select the group that has the admin permissions on this station.")] DiscordRole adminGroup,
            [Description("Select a channel to get music requests when a request is not found on the server."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel requestsChannel,
            [Description("Enable or disable the preference of HLS streams if you add an able mount point.")] bool hls,
            [Description("Enable or disable the showing of the playlist in the nowplaying embed.")] bool showPlaylist,
            [Description("Enable or disable the automatic check if files have been changed.")] bool fileChanges,
            [Description("Enter the api key of the new station. This is optional if the admin one has the permission.")] string? apiKey = null,
            [Description("Select the group that has the dj permissions on this station.")] DiscordRole? djGroup = null
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(AddAzuraCastStationAsync), context.User.GlobalName);

            if (adminGroup is null)
            {
                await context.DeleteResponseAsync();
                await context.FollowupAsync("You have to select an admin group first!");
                return;
            }

            if (requestsChannel is null)
            {
                await context.DeleteResponseAsync();
                await context.FollowupAsync("You have to select a request channel first!");
                return;
            }

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.AddAzuraCastStationAsync(guildId, station, stationName, adminGroup.Id, requestsChannel.Id, hls, showPlaylist, fileChanges, apiKey, djGroup?.Id);

            await context.DeleteResponseAsync();
            await context.FollowupAsync("Your station was added successfully and private data has been encrypted.");
        }

        [Command("add-azuracast-station-mount"), Description("Add an AzuraCast mount point to the selected station."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask AddAzuraCastStationMountAsync
            (
            CommandContext context,
            [Description("Choose the station you want to add the mount."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("Enter the mount point name.")] string mountName,
            [Description("Enter the mount point stub.")] string mount
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(AddAzuraCastStationMountAsync), context.User.GlobalName);

            await _db.AddAzuraCastStationMountPointAsync(station, mountName, mount);

            await context.EditResponseAsync("Your mount point was added successfully.");
        }

        [Command("delete-azuracast"), Description("Delete the existing AzuraCast setup."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask DeleteAzuraCastAsync(CommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(DeleteAzuraCastAsync), context.User.GlobalName);

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.DeleteAzuraCastAsync(guildId);

            await context.EditResponseAsync("Your AzuraCast setup was deleted successfully.");
        }

        [Command("delete-azuracast-station"), Description("Delete an existing AzuraCast station."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask DeleteAzuraCastStationAsync
            (
            CommandContext context,
            [Description("Choose the station you want to delete."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(DeleteAzuraCastStationAsync), context.User.GlobalName);

            await _db.DeleteAzuraCastStationAsync(station);

            await context.EditResponseAsync("Your station was deleted successfully.");
        }

        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Needed in the mount autocomplete provider")]
        [Command("delete-azuracast-station-mount"), Description("Delete an existing AzuraCast mount point from a station."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask DeleteAzuraCastStationMountAsync
            (
            CommandContext context,
            [Description("Select the station of the mount point."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("Select the mount point you want to delete."), SlashAutoCompleteProvider<AzuraCastMountAutocomplete>] int mountId
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(DeleteAzuraCastStationMountAsync), context.User.GlobalName);

            await _db.DeleteAzuraCastMountAsync(mountId);

            await context.EditResponseAsync("Your mount point was deleted successfully.");
        }

        [Command("modify-azuracast"), Description("Modify the general AzuraCast settings."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask UpdateAzuraCastAsync
            (
            CommandContext context,
            [Description("Update the base Url, an example: https://demo.azuracast.com/")] Uri? url = null,
            [Description("Update the administrator api key. It's enough when it has the permission to access system info.")] string? apiKey = null,
            [Description("Update the group that has the admin permissions on this instance.")] DiscordRole? instanceAdminGroup = null,
            [Description("Update the channel to get general notifications about your azuracast instance."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel? notificationsChannel = null,
            [Description("Update the channel to get notifications when your azuracast instance is down."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel? outagesChannel = null
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            _logger.CommandRequested(nameof(UpdateAzuraCastAsync), context.User.GlobalName);
            if (url is null && apiKey is null && instanceAdminGroup is null && notificationsChannel is null && outagesChannel is null)
            {
                await context.RespondAsync("You have to provide at least one parameter to update.");
                return;
            }

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.UpdateAzuraCastAsync(guildId, url, apiKey, instanceAdminGroup?.Id, notificationsChannel?.Id, outagesChannel?.Id);

            await context.DeleteResponseAsync();
            await context.FollowupAsync("Your AzuraCast settings were saved successfully and private data has been encrypted.");

            if (url is not null)
                await _backgroundService.StartAzuraCastBackgroundServiceAsync(AzuraCastChecks.CheckForOnlineStatus, guildId);
        }

        [Command("modify-azuracast-checks"), Description("Modify the automatic checks for your AzuraCast instance."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask UpdateAzuraCastChecksAsync
            (
            CommandContext context,
            [Description("Enable or disable the automatic check if the AzuraCast instance of your server is down.")] bool? serverStatus = null,
            [Description("Enable or disable the automatic check for AzuraCast updates.")] bool? updates = null,
            [Description("Enable or disable the addition of the changelog to the posted AzuraCast updates.")] bool? updatesChangelog = null
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(UpdateAzuraCastChecksAsync), context.User.GlobalName);
            if (serverStatus is null && updates is null && updatesChangelog is null)
            {
                await context.RespondAsync("You have to provide at least one parameter to update.");
                return;
            }

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.UpdateAzuraCastChecksAsync(guildId, serverStatus, updates, updatesChangelog);

            await context.EditResponseAsync("Your settings were saved successfully.");
        }

        [Command("modify-azuracast-station"), Description("Modify one AzuraCast station you already added."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask UpdateAzuraCastStationAsync
            (
            CommandContext context,
            [Description("Choose the station you want to modify."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("Modify the station id.")] int? stationId = null,
            [Description("Modify the name.")] string? stationName = null,
            [Description("Modify the api key.")] string? apiKey = null,
            [Description("Modify the group that has the admin permissions on this station.")] DiscordRole? adminGroup = null,
            [Description("Modify the group that has the dj permissions on this station.")] DiscordRole? djGroup = null,
            [Description("Modify the channel to get music requests when a request is not found on the server."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel? requestsChannel = null,
            [Description("Enable or disable the preference of HLS streams if you add an able mount point.")] bool? hls = null,
            [Description("Enable or disable the showing of the playlist in the nowplaying embed.")] bool? showPlaylist = null
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(UpdateAzuraCastStationAsync), context.User.GlobalName);
            if (stationId is null && stationName is null && apiKey is null && adminGroup is null && djGroup is null && requestsChannel is null && hls is null && showPlaylist is null)
            {
                await context.RespondAsync("You have to provide at least one parameter to update.");
                return;
            }

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.UpdateAzuraCastStationAsync(guildId, station, stationId, stationName, apiKey, adminGroup?.Id, djGroup?.Id, requestsChannel?.Id, hls, showPlaylist);

            await context.DeleteResponseAsync();
            await context.FollowupAsync("Your settings were saved successfully and private data has been encrypted.");
        }

        [Command("modify-azuracast-station-checks"), Description("Modify the automatic checks inside an AzuraCast station."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask UpdateAzuraCastStationChecksAsync
            (
            CommandContext context,
            [Description("Choose the station you want to modify the checks."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("Enable or disable the automatic check if files have been changed.")] bool? fileChanges = null
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(UpdateAzuraCastStationChecksAsync), context.User.GlobalName);
            if (fileChanges is null)
            {
                await context.RespondAsync("You have to provide at least one parameter to update.");
                return;
            }

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.UpdateAzuraCastStationChecksAsync(guildId, station, fileChanges);

            await context.EditResponseAsync("Your settings were saved successfully.");
        }

        [Command("modify-core"), Description("Modify the core settings of the bot.")]
        public async ValueTask UpdateCoreAsync
            (
            CommandContext context,
            [Description("Select the role that has administrative permissions on the bot.")] DiscordRole? adminRole = null,
            [Description("Select a channel to get administrative notifications about the bot."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel? adminChannel = null,
            [Description("Select a channel to get notifications when the bot runs into an issue."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel? errorChannel = null
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(UpdateCoreAsync), context.User.GlobalName);
            if (adminRole is null && adminChannel is null && errorChannel is null)
            {
                await context.RespondAsync("You have to provide at least one parameter to update.");
                return;
            }

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.UpdateGuildAsync(guildId, adminRole?.Id, adminChannel?.Id, errorChannel?.Id);

            await context.EditResponseAsync("Your settings were saved successfully.");
        }

        [Command("get-settings"), Description("Get all configured settings in a direct message.")]
        public async ValueTask GetSettingsAsync(CommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(GetSettingsAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            string guildName = context.Guild.Name;
            DiscordMember member = context.Member ?? throw new InvalidOperationException("Member is null");

            GuildsEntity? guild = await _db.GetGuildAsync(guildId);
            if (guild is null)
            {
                await context.EditResponseAsync("Server not found in database.");
                return;
            }

            DiscordRole? adminRole = context.Guild.GetRole(guild.AdminRoleId);
            DiscordEmbed guildEmbed = EmbedBuilder.BuildGetSettingsGuildEmbed(guildName, guild, $"{adminRole?.Name} ({adminRole?.Id})");

            await using DiscordMessageBuilder messageBuilder = new();
            messageBuilder.AddEmbeds([guildEmbed]);

            if (guild.AzuraCast is not null)
            {
                AzuraCastEntity azuraCast = guild.AzuraCast;
                Dictionary<ulong, string> stationRoles = [];
                foreach (AzuraCastStationEntity station in azuraCast.Stations)
                {
                    DiscordRole? stationAdminRole = context.Guild.GetRole(station.StationAdminRoleId);
                    DiscordRole? stationDjRole = context.Guild.GetRole(station.StationDjRoleId);
                    stationRoles.Add(stationAdminRole?.Id ?? 0, stationAdminRole?.Name ?? string.Empty);
                    stationRoles.Add(stationDjRole?.Id ?? 0, stationDjRole?.Name ?? string.Empty);
                }

                DiscordRole? instanceAdminRole = context.Guild.GetRole(azuraCast.InstanceAdminRoleId);
                IReadOnlyList<DiscordEmbed> azuraEmbed = EmbedBuilder.BuildGetSettingsAzuraEmbed(azuraCast, $"{instanceAdminRole?.Name} ({instanceAdminRole?.Id})", stationRoles);

                messageBuilder.AddEmbeds(azuraEmbed);
            }

            await member.SendMessageAsync(messageBuilder);

            await context.EditResponseAsync("I sent you an overview with all the settings in private. Be aware of sensitive data.");
        }

        [Command("reset-settings"), Description("Reset all of your settings, you have to reconfigure everything again.")]
        public async ValueTask ResetSettingsAsync(CommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(ResetSettingsAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.DeleteGuildAsync(guildId);
            await _db.AddGuildAsync(guildId);

            await context.EditResponseAsync("Your settings were reset successfully.\nRemember that you have to set all the configurations again.");
        }
    }
}