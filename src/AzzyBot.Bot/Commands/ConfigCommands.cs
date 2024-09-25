using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Bot.Commands.Autocompletes;
using AzzyBot.Bot.Commands.Checks;
using AzzyBot.Bot.Commands.Choices;
using AzzyBot.Bot.Services;
using AzzyBot.Bot.Services.BackgroundServices;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Utilities;
using AzzyBot.Bot.Utilities.Enums;
using AzzyBot.Bot.Utilities.Helpers;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "DSharpPlus best practice")]
public sealed class ConfigCommands
{
    [Command("config"), RequireGuild, RequirePermissions(DiscordPermissions.None, DiscordPermissions.Administrator)]
    public sealed class ConfigGroup(ILogger<ConfigGroup> logger, AzuraCastApiService azuraCast, AzuraChecksBackgroundTask backgroundService, DbActions dbActions, DiscordBotService botService)
    {
        private readonly ILogger<ConfigGroup> _logger = logger;
        private readonly AzuraCastApiService _azuraCast = azuraCast;
        private readonly AzuraChecksBackgroundTask _backgroundService = backgroundService;
        private readonly DbActions _dbActions = dbActions;
        private readonly DiscordBotService _botService = botService;

        [Command("add-azuracast"), Description("Add an AzuraCast instance to your server. This is a requirement to use the features.")]
        public async ValueTask AddAzuraCastAsync
        (
            SlashCommandContext context,
            [Description("Set the base Url, an example: https://demo.azuracast.com/")] Uri url,
            [Description("Add an administrator api key. It's enough when it has the permission to access system information.")] string apiKey,
            [Description("Select the group that has the admin permissions on this instance.")] DiscordRole instanceAdminGroup,
            [Description("Select a channel to get general notifications about your azuracast installation."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel notificationChannel,
            [Description("Select a channel to get notifications when your azuracast installation is down."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel outagesChannel,
            [Description("Enable or disable the automatic check if the AzuraCast instance of your server is down."), SlashChoiceProvider<BooleanEnableDisableStateProvider>] int serverStatus,
            [Description("Enable or disable the automatic check for AzuraCast updates."), SlashChoiceProvider<BooleanEnableDisableStateProvider>] int updates,
            [Description("Enable or disable the addition of the changelog to the posted AzuraCast updates."), SlashChoiceProvider<BooleanEnableDisableStateProvider>] int updatesChangelog
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(AddAzuraCastAsync), context.User.GlobalName);

            if (instanceAdminGroup is null)
            {
                await context.DeleteResponseAsync();
                await context.FollowupAsync(GeneralStrings.ConfigInstanceAdminMissing, true);
                return;
            }

            if (notificationChannel is null)
            {
                await context.DeleteResponseAsync();
                await context.FollowupAsync(GeneralStrings.ConfigInstanceNotificationChannelMissing, true);
                return;
            }

            if (outagesChannel is null)
            {
                await context.DeleteResponseAsync();
                await context.FollowupAsync(GeneralStrings.ConfigInstanceOutageChannelMissing, true);
                return;
            }

            await context.DeferResponseAsync();

            ulong guildId = context.Guild.Id;
            GuildEntity? guild = await _dbActions.GetGuildAsync(guildId, loadEverything: true).FirstOrDefaultAsync();
            if (guild is null)
            {
                _logger.DatabaseGuildNotFound(guildId);
                await context.DeleteResponseAsync();
                await context.FollowupAsync(GeneralStrings.GuildNotFound);
                return;
            }

            if (!guild.ConfigSet)
            {
                await context.DeleteResponseAsync();
                await context.FollowupAsync(GeneralStrings.CoreSettingsMissing);
                return;
            }

            AzuraCastEntity? ac = guild.AzuraCast;
            if (ac is not null)
            {
                _logger.DatabaseAzuraCastNotFound(guildId);
                await context.DeleteResponseAsync();
                await context.FollowupAsync(GeneralStrings.ConfigInstanceAlreadyExists);
                return;
            }

            await _dbActions.AddAzuraCastAsync(guildId, url, apiKey, instanceAdminGroup.Id, notificationChannel.Id, outagesChannel.Id, serverStatus is 1, updates is 1, updatesChangelog is 1);

            await context.DeleteResponseAsync();
            await context.FollowupAsync(GeneralStrings.ConfigInstanceAdded);

            guild = await _dbActions.GetGuildAsync(guildId, loadEverything: true).FirstOrDefaultAsync();
            if (guild is null)
            {
                _logger.DatabaseGuildNotFound(guildId);
                await context.DeleteResponseAsync();
                await context.FollowupAsync(GeneralStrings.GuildNotFound);
                return;
            }

            await _botService.CheckPermissionsAsync(context.Guild, [notificationChannel.Id, outagesChannel.Id]);
            _backgroundService.QueueInstancePing(guild);
        }

        [Command("add-azuracast-station"), Description("Add an AzuraCast station to your instance."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask AddAzuraCastStationAsync
        (
            SlashCommandContext context,
            [Description("Enter the station id of your azuracast station.")] int station,
            [Description("Select the group that has the admin permissions on this station.")] DiscordRole adminGroup,
            [Description("Select a channel to get music requests when a request is not found on the server."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel requestsChannel,
            [Description("Enable or disable the showing of the playlist in the nowplaying embed."), SlashChoiceProvider<BooleanEnableDisableStateProvider>] int showPlaylist,
            [Description("Enable or disable the automatic check if files have been changed."), SlashChoiceProvider<BooleanEnableDisableStateProvider>] int fileChanges,
            [Description("Select a channel where users are able to upload their own songs to your station."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel? uploadChannel = null,
            [Description("Enter a custom path where the user uploaded songs are stored. Like /Requests")] string? uploadPath = null,
            [Description("Enter the api key of the new station. This is optional if the admin one has the permission.")] string? apiKey = null,
            [Description("Select the group that has the dj permissions on this station.")] DiscordRole? djGroup = null
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(AddAzuraCastStationAsync), context.User.GlobalName);

            if (adminGroup is null)
            {
                await context.DeleteResponseAsync();
                await context.FollowupAsync(GeneralStrings.ConfigStationAdminMissing);
                return;
            }

            if (requestsChannel is null)
            {
                await context.DeleteResponseAsync();
                await context.FollowupAsync(GeneralStrings.ConfigStationRequestChannelMissing);
                return;
            }

            await _dbActions.AddAzuraCastStationAsync(context.Guild.Id, station, adminGroup.Id, requestsChannel.Id, showPlaylist is 1, fileChanges is 1, uploadChannel?.Id, uploadPath, apiKey, djGroup?.Id);

            await context.DeleteResponseAsync();
            await context.FollowupAsync(GeneralStrings.ConfigStationAdded);

            ulong guildId = context.Guild.Id;
            GuildEntity? guild = await _dbActions.GetGuildAsync(guildId, loadEverything: true).FirstOrDefaultAsync();
            if (guild is null)
            {
                _logger.DatabaseGuildNotFound(guildId);
                return;
            }

            if (guild.AzuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                return;
            }

            ulong[] channels = (uploadChannel is null) ? [requestsChannel.Id] : [requestsChannel.Id, uploadChannel.Id];
            await _botService.CheckPermissionsAsync(context.Guild, channels);

            if (guild.AzuraCast.IsOnline)
                _backgroundService.QueueFileChangesChecks(guild, station);
        }

        [Command("delete-azuracast"), Description("Delete the existing AzuraCast setup."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask DeleteAzuraCastAsync(SlashCommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(DeleteAzuraCastAsync), context.User.GlobalName);

            AzuraCastEntity? ac = await _dbActions.GetAzuraCastAsync(context.Guild.Id);
            if (ac is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            FileOperations.DeleteFiles(_azuraCast.FilePath, $"{ac.Id}-");
            await _dbActions.DeleteAzuraCastAsync(context.Guild.Id);

            await context.EditResponseAsync(GeneralStrings.ConfigInstanceDeleted);
        }

        [Command("delete-azuracast-station"), Description("Delete an existing AzuraCast station."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask DeleteAzuraCastStationAsync
        (
            SlashCommandContext context,
            [Description("Choose the station you want to delete."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(DeleteAzuraCastStationAsync), context.User.GlobalName);

            AzuraCastStationEntity? acStation = await _dbActions.GetAzuraCastStationAsync(context.Guild.Id, station, loadAzuraCast: true);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, 0, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            FileOperations.DeleteFile(Path.Combine(_azuraCast.FilePath, $"{acStation.AzuraCast.GuildId}-{acStation.AzuraCastId}-{acStation.Id}-{acStation.StationId}-files.json"));
            await _dbActions.DeleteAzuraCastStationAsync(station);

            await context.EditResponseAsync(GeneralStrings.ConfigStationDeleted);
        }

        [Command("modify-azuracast"), Description("Modify the general AzuraCast settings."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask UpdateAzuraCastAsync
        (
            SlashCommandContext context,
            [Description("Update the base Url, an example: https://demo.azuracast.com/")] Uri? url = null,
            [Description("Update the administrator api key. It's enough when it has the permission to access system info.")] string? apiKey = null,
            [Description("Update the group that has the admin permissions on this instance.")] DiscordRole? instanceAdminGroup = null,
            [Description("Update the channel to get general notifications about your azuracast instance."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel? notificationsChannel = null,
            [Description("Update the channel to get notifications when your azuracast instance is down."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel? outagesChannel = null
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(UpdateAzuraCastAsync), context.User.GlobalName);

            if (url is null && apiKey is null && instanceAdminGroup is null && notificationsChannel is null && outagesChannel is null)
            {
                await context.EditResponseAsync(GeneralStrings.ConfigParameterMissing);
                return;
            }

            if (url is not null || !string.IsNullOrWhiteSpace(apiKey))
                await _dbActions.UpdateAzuraCastAsync(context.Guild.Id, url, apiKey);

            if (instanceAdminGroup is not null || notificationsChannel is not null || outagesChannel is not null)
                await _dbActions.UpdateAzuraCastPreferencesAsync(context.Guild.Id, instanceAdminGroup?.Id, notificationsChannel?.Id, outagesChannel?.Id);

            await context.DeleteResponseAsync();
            await context.FollowupAsync(GeneralStrings.ConfigInstanceModified);

            ulong guildId = context.Guild.Id;
            GuildEntity? guild = await _dbActions.GetGuildAsync(guildId, loadEverything: true).FirstOrDefaultAsync();
            if (guild is null)
            {
                _logger.DatabaseGuildNotFound(guildId);
                return;
            }

            if (notificationsChannel is not null || outagesChannel is not null)
            {
                ulong[] channels = [];
                if (notificationsChannel is null && outagesChannel is not null)
                {
                    channels = [outagesChannel.Id];
                }
                else if (outagesChannel is null && notificationsChannel is not null)
                {
                    channels = [notificationsChannel.Id];
                }
                else if (notificationsChannel is not null && outagesChannel is not null)
                {
                    channels = [notificationsChannel.Id, outagesChannel.Id];
                }

                if (channels.Length is not 0)
                    await _botService.CheckPermissionsAsync(context.Guild, channels);
            }

            if (url is not null)
                _backgroundService.QueueInstancePing(guild);
        }

        [Command("modify-azuracast-checks"), Description("Modify the automatic checks for your AzuraCast instance."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask UpdateAzuraCastChecksAsync
        (
            SlashCommandContext context,
            [Description("Enable or disable the automatic check if the AzuraCast instance of your server is down."), SlashChoiceProvider<BooleanEnableDisableStateProvider>] int serverStatus = 0,
            [Description("Enable or disable the automatic check for AzuraCast updates."), SlashChoiceProvider<BooleanEnableDisableStateProvider>] int updates = 0,
            [Description("Enable or disable the addition of the changelog to the posted AzuraCast updates."), SlashChoiceProvider<BooleanEnableDisableStateProvider>] int updatesChangelog = 0
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(UpdateAzuraCastChecksAsync), context.User.GlobalName);

            if (serverStatus is 0 && updates is 0 && updatesChangelog is 0)
            {
                await context.EditResponseAsync(GeneralStrings.ConfigParameterMissing);
                return;
            }

            bool? enableServerStatus = null;
            bool? enableUpdates = null;
            bool? enableUpdatesChangelog = null;

            if (serverStatus is 1)
            {
                enableServerStatus = true;
            }
            else if (serverStatus is 2)
            {
                enableServerStatus = false;
            }

            if (updates is 1)
            {
                enableUpdates = true;
            }
            else if (updates is 2)
            {
                enableUpdates = false;
            }

            if (updatesChangelog is 1)
            {
                enableUpdatesChangelog = true;
            }
            else if (updatesChangelog is 2)
            {
                enableUpdatesChangelog = false;
            }

            await _dbActions.UpdateAzuraCastChecksAsync(context.Guild.Id, enableServerStatus, enableUpdates, enableUpdatesChangelog);

            await context.EditResponseAsync(GeneralStrings.ConfigInstanceModifiedChecks);

            if (serverStatus is 1 || updates is 1)
            {
                ulong guildId = context.Guild.Id;
                GuildEntity? guild = await _dbActions.GetGuildAsync(guildId, loadEverything: true).FirstOrDefaultAsync();
                if (guild is null)
                {
                    _logger.DatabaseGuildNotFound(guildId);
                    return;
                }

                if (guild.AzuraCast is null)
                {
                    _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                    return;
                }

                if (serverStatus is 1)
                    _backgroundService.QueueInstancePing(guild);

                if (updates is 1)
                    _backgroundService.QueueUpdates(guild);
            }
        }

        [Command("modify-azuracast-station"), Description("Modify one AzuraCast station you already added."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask UpdateAzuraCastStationAsync
        (
            SlashCommandContext context,
            [Description("Choose the station you want to modify."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("Modify the station id.")] int? stationId = null,
            [Description("Modify the api key.")] string? apiKey = null,
            [Description("Modify the group that has the admin permissions on this station.")] DiscordRole? adminGroup = null,
            [Description("Modify the group that has the dj permissions on this station.")] DiscordRole? djGroup = null,
            [Description("Modify the channel where users are able to upload their own songs to your station."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel? uploadChannel = null,
            [Description("Modify the channel to get music requests when a request is not found in the station."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel? requestsChannel = null,
            [Description("Modify the custom path where the user uploaded songs are stored. Like /Requests")] string? uploadPath = null,
            [Description("Enable or disable the showing of the playlist in the nowplaying embed."), SlashChoiceProvider<BooleanEnableDisableStateProvider>] int showPlaylist = 0
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(UpdateAzuraCastStationAsync), context.User.GlobalName);

            if (stationId is null && apiKey is null && adminGroup is null && djGroup is null && uploadChannel is null && string.IsNullOrWhiteSpace(uploadPath) && requestsChannel is null && showPlaylist is 0)
            {
                await context.EditResponseAsync(GeneralStrings.ConfigParameterMissing);
                return;
            }

            bool? showPlaylistInEmbed = null;

            if (showPlaylist is 1)
            {
                showPlaylistInEmbed = true;
            }
            else if (showPlaylist is 2)
            {
                showPlaylistInEmbed = false;
            }

            if (adminGroup is not null || djGroup is not null || uploadChannel is not null || requestsChannel is not null || !string.IsNullOrWhiteSpace(uploadPath) || showPlaylistInEmbed is not null)
            {
                await _dbActions.UpdateAzuraCastStationPreferencesAsync(context.Guild.Id, station, adminGroup?.Id, djGroup?.Id, uploadChannel?.Id, requestsChannel?.Id, uploadPath, showPlaylistInEmbed);

                ulong[] channels = [];
                if (requestsChannel is not null && uploadChannel is null)
                {
                    channels = [requestsChannel.Id];
                }
                else if (uploadChannel is not null && requestsChannel is null)
                {
                    channels = [uploadChannel.Id];
                }
                else if (requestsChannel is not null && uploadChannel is not null)
                {
                    channels = [requestsChannel.Id, uploadChannel.Id];
                }

                if (channels.Length is not 0)
                    await _botService.CheckPermissionsAsync(context.Guild, channels);
            }

            if (stationId.HasValue || !string.IsNullOrWhiteSpace(apiKey))
            {
                await _dbActions.UpdateAzuraCastStationAsync(context.Guild.Id, station, stationId, apiKey);

                ulong guildId = context.Guild.Id;
                GuildEntity? guild = await _dbActions.GetGuildAsync(guildId, loadEverything: true).FirstOrDefaultAsync();
                if (guild is null)
                {
                    _logger.DatabaseGuildNotFound(guildId);
                    await context.EditResponseAsync(GeneralStrings.GuildNotFound);
                    return;
                }

                if (guild.AzuraCast is null)
                {
                    _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                    await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                    return;
                }

                if (guild.AzuraCast.Stations.FirstOrDefault(s => s.Id == station) is null)
                {
                    _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, guild.AzuraCast.Id, station);
                    await context.EditResponseAsync(GeneralStrings.StationNotFound);
                    return;
                }

                if (guild.AzuraCast.IsOnline)
                    _backgroundService.QueueFileChangesChecks(guild, station);
            }

            await context.DeleteResponseAsync();
            await context.FollowupAsync(GeneralStrings.ConfigStationModified);
        }

        [Command("modify-azuracast-station-checks"), Description("Modify the automatic checks inside an AzuraCast station."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask UpdateAzuraCastStationChecksAsync
        (
            SlashCommandContext context,
            [Description("Choose the station you want to modify the checks."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("Enable or disable the automatic check if files have been changed."), SlashChoiceProvider<BooleanEnableDisableStateProvider>] int fileChanges = 0
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(UpdateAzuraCastStationChecksAsync), context.User.GlobalName);

            if (fileChanges is 0)
            {
                await context.EditResponseAsync(GeneralStrings.ConfigParameterMissing);
                return;
            }

            bool? enableFileChanges = null;
            if (fileChanges is 1)
            {
                enableFileChanges = true;
            }
            else if (fileChanges is 2)
            {
                enableFileChanges = false;
            }

            await _dbActions.UpdateAzuraCastStationChecksAsync(context.Guild.Id, station, enableFileChanges);

            await context.EditResponseAsync(GeneralStrings.ConfigStationModifiedChecks);

            if (fileChanges is 1)
            {
                ulong guildId = context.Guild.Id;
                GuildEntity? guild = await _dbActions.GetGuildAsync(guildId, loadEverything: true).FirstOrDefaultAsync();
                if (guild is null)
                {
                    _logger.DatabaseGuildNotFound(guildId);
                    return;
                }

                if (guild.AzuraCast is null)
                {
                    _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                    return;
                }

                if (guild.AzuraCast.Stations.FirstOrDefault(s => s.Id == station) is null)
                {
                    _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, guild.AzuraCast.Id, station);
                    return;
                }

                if (guild.AzuraCast.IsOnline)
                    _backgroundService.QueueFileChangesChecks(guild, station);
            }
        }

        [Command("modify-core"), Description("Modify the core settings of the bot.")]
        public async ValueTask UpdateCoreAsync
        (
            SlashCommandContext context,
            [Description("Select the role that has administrative permissions on the bot.")] DiscordRole? adminRole = null,
            [Description("Select a channel to get administrative notifications about the bot."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel? adminChannel = null,
            [Description("Select a channel to get notifications when the bot runs into an issue."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel? errorChannel = null
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(UpdateCoreAsync), context.User.GlobalName);

            if (adminRole is null && adminChannel is null && errorChannel is null)
            {
                await context.EditResponseAsync(GeneralStrings.ConfigParameterMissing);
                return;
            }

            await context.DeferResponseAsync();

            await _dbActions.UpdateGuildPreferencesAsync(context.Guild.Id, adminRole?.Id, adminChannel?.Id, errorChannel?.Id);

            await context.EditResponseAsync(GeneralStrings.CoreSettingsModified);

            ulong[] channels = [];
            if (adminChannel is not null && errorChannel is null)
            {
                channels = [adminChannel.Id];
            }
            else if (errorChannel is not null && adminChannel is null)
            {
                channels = [errorChannel.Id];
            }
            else if (adminChannel is not null && errorChannel is not null)
            {
                channels = [adminChannel.Id, errorChannel.Id];
            }

            if (channels.Length is not 0)
                await _botService.CheckPermissionsAsync(context.Guild, channels);
        }

        [Command("get-settings"), Description("Get all configured settings in a direct message.")]
        public async ValueTask GetSettingsAsync(SlashCommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));
            ArgumentNullException.ThrowIfNull(context.Member, nameof(context.Member));

            _logger.CommandRequested(nameof(GetSettingsAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild.Id;
            string guildName = context.Guild.Name;
            DiscordMember member = context.Member;
            GuildEntity? guild = null;
            await foreach (GuildEntity itGuild in _dbActions.GetGuildAsync(guildId, loadEverything: true))
            {
                if (itGuild.UniqueId == guildId)
                {
                    guild = itGuild;
                    break;
                }
            }

            if (guild is null)
            {
                _logger.DatabaseGuildNotFound(guildId);
                await context.EditResponseAsync(GeneralStrings.GuildNotFound);
                return;
            }

            IReadOnlyList<DiscordRole> roles = await context.Guild.GetRolesAsync();
            DiscordRole? adminRole = roles.FirstOrDefault(r => r.Id == guild.Preferences.AdminRoleId);
            DiscordEmbed guildEmbed = EmbedBuilder.BuildGetSettingsGuildEmbed(guildName, guild, $"{adminRole?.Name} ({adminRole?.Id})");

            await using DiscordMessageBuilder messageBuilder = new();
            messageBuilder.AddEmbeds([guildEmbed]);

            if (guild.AzuraCast is not null)
            {
                AzuraCastEntity ac = guild.AzuraCast;
                AzuraStationRecord? stationRecord;
                Dictionary<ulong, string> stationRoles = new(ac.Stations.Count);
                Dictionary<int, string> stationNames = new(ac.Stations.Count);
                foreach (AzuraCastStationEntity station in ac.Stations)
                {
                    DiscordRole? stationAdminRole = roles.FirstOrDefault(r => r.Id == station.Preferences.StationAdminRoleId);
                    DiscordRole? stationDjRole = roles.FirstOrDefault(r => r.Id == station.Preferences.StationDjRoleId);
                    stationRoles.Add(stationAdminRole?.Id ?? 0, stationAdminRole?.Name ?? "Name not found");
                    stationRoles.Add(stationDjRole?.Id ?? 0, stationDjRole?.Name ?? "Name not found");

                    stationRecord = await _azuraCast.GetStationAsync(new(Crypto.Decrypt(ac.BaseUrl)), station.StationId);
                    if (stationRecord is null)
                    {
                        await _botService.SendMessageAsync(guild.AzuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **station** ({station.StationId}) endpoint.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                        continue;
                    }

                    stationNames.Add(station.Id, stationRecord.Name);
                }

                DiscordRole? instanceAdminRole = roles.FirstOrDefault(r => r.Id == ac.Preferences.InstanceAdminRoleId);
                IEnumerable<DiscordEmbed> azuraEmbed = EmbedBuilder.BuildGetSettingsAzuraEmbed(ac, $"{instanceAdminRole?.Name} ({instanceAdminRole?.Id})", stationRoles, stationNames);

                messageBuilder.AddEmbeds(azuraEmbed);
            }

            await member.SendMessageAsync(messageBuilder);

            await context.EditResponseAsync(GeneralStrings.ConfigGet);
        }

        [Command("reset-settings"), Description("Reset all of your settings, you have to reconfigure everything again.")]
        public async ValueTask ResetSettingsAsync(SlashCommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(ResetSettingsAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            await _dbActions.DeleteGuildAsync(context.Guild.Id);
            await _dbActions.AddGuildAsync(context.Guild.Id);

            await context.EditResponseAsync(GeneralStrings.ConfigReset);
        }
    }
}
