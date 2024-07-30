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
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Utilities;
using AzzyBot.Bot.Utilities.Enums;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Extensions;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "DSharpPlus best practice")]
public sealed class ConfigCommands
{
    [Command("config"), RequireGuild, RequirePermissions(DiscordPermissions.None, DiscordPermissions.Administrator)]
    public sealed class ConfigGroup(ILogger<ConfigGroup> logger, AzuraCastApiService azuraCast, AzzyBackgroundService backgroundService, DbActions dbActions)
    {
        private readonly ILogger<ConfigGroup> _logger = logger;
        private readonly AzuraCastApiService _azuraCast = azuraCast;
        private readonly AzzyBackgroundService _backgroundService = backgroundService;
        private readonly DbActions _dbActions = dbActions;

        [Command("add-azuracast"), Description("Add an AzuraCast instance to your server. This is a requirement to use the features.")]
        public async ValueTask AddAzuraCastAsync
        (
            CommandContext context,
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

            ulong guildId = context.Guild.Id;
            GuildEntity? guild = null;
            IAsyncEnumerable<GuildEntity> guilds = _dbActions.GetGuildAsync(guildId, loadEverything: true);
            await foreach (GuildEntity itGuild in guilds)
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
                return;
            }

            if (!guild.ConfigSet)
            {
                await context.DeleteResponseAsync();
                await context.FollowupAsync("You have to set the core settings first!");
                return;
            }

            AzuraCastEntity? azuraCast = guild.AzuraCast;
            if (azuraCast is not null)
            {
                _logger.DatabaseAzuraCastNotFound(guildId);
                await context.DeleteResponseAsync();
                await context.FollowupAsync("AzuraCast is already set up for your server.");
                return;
            }

            await _dbActions.AddAzuraCastAsync(guildId, url, apiKey, instanceAdminGroup.Id, notificationChannel.Id, outagesChannel.Id, serverStatus is 1, updates is 1, updatesChangelog is 1);

            await context.DeleteResponseAsync();
            await context.FollowupAsync("Your AzuraCast installation was added successfully and private data has been encrypted.");

            guilds = _dbActions.GetGuildAsync(guildId, loadEverything: true);
            if (!await guilds.ContainsOneItemAsync())
            {
                _logger.DatabaseGuildNotFound(guildId);
                return;
            }

            await _backgroundService.StartAzuraCastBackgroundServiceAsync(AzuraCastChecks.CheckForOnlineStatus, guilds);
        }

        [Command("add-azuracast-station"), Description("Add an AzuraCast station to your instance."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask AddAzuraCastStationAsync
        (
            CommandContext context,
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
                await context.FollowupAsync("You have to select an admin group first!");
                return;
            }

            if (requestsChannel is null)
            {
                await context.DeleteResponseAsync();
                await context.FollowupAsync("You have to select a request channel first!");
                return;
            }

            await _dbActions.AddAzuraCastStationAsync(context.Guild.Id, station, adminGroup.Id, requestsChannel.Id, showPlaylist is 1, fileChanges is 1, uploadChannel?.Id, uploadPath, apiKey, djGroup?.Id);

            await context.DeleteResponseAsync();
            await context.FollowupAsync("Your station was added successfully and private data has been encrypted.");

            ulong guildId = context.Guild.Id;
            GuildEntity? guild = null;
            IAsyncEnumerable<GuildEntity> guilds = _dbActions.GetGuildAsync(guildId, loadEverything: true);
            await foreach (GuildEntity itGuild in guilds)
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
                return;
            }

            if (guild.AzuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                return;
            }

            guilds = _dbActions.GetGuildAsync(guildId, loadEverything: true);
            if (!await guilds.ContainsOneItemAsync())
            {
                _logger.DatabaseGuildNotFound(guildId);
                return;
            }

            if (guild.AzuraCast!.IsOnline)
                await _backgroundService.StartAzuraCastBackgroundServiceAsync(AzuraCastChecks.CheckForFileChanges, guilds, station);
        }

        [Command("delete-azuracast"), Description("Delete the existing AzuraCast setup."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask DeleteAzuraCastAsync(CommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(DeleteAzuraCastAsync), context.User.GlobalName);

            AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id);
            if (azuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync("AzuraCast not found in database.");
                return;
            }

            FileOperations.DeleteFiles(_azuraCast.FilePath, $"{azuraCast.Id}-");
            await _dbActions.DeleteAzuraCastAsync(context.Guild.Id);

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
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(DeleteAzuraCastStationAsync), context.User.GlobalName);

            AzuraCastStationEntity? acStation = await _dbActions.GetAzuraCastStationAsync(context.Guild.Id, station, loadAzuraCast: true);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, 0, station);
                await context.EditResponseAsync("Station not found in database.");
                return;
            }

            FileOperations.DeleteFile(Path.Combine(_azuraCast.FilePath, $"{acStation.AzuraCast.GuildId}-{acStation.AzuraCastId}-{acStation.Id}-{acStation.StationId}-files.json"));
            await _dbActions.DeleteAzuraCastStationAsync(station);

            await context.EditResponseAsync("Your station was deleted successfully.");
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
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(UpdateAzuraCastAsync), context.User.GlobalName);

            if (url is null && apiKey is null && instanceAdminGroup is null && notificationsChannel is null && outagesChannel is null)
            {
                await context.RespondAsync("You have to provide at least one parameter to update.");
                return;
            }

            if (url is not null || !string.IsNullOrWhiteSpace(apiKey))
                await _dbActions.UpdateAzuraCastAsync(context.Guild.Id, url, apiKey);

            if (instanceAdminGroup is not null || notificationsChannel is not null || outagesChannel is not null)
                await _dbActions.UpdateAzuraCastPreferencesAsync(context.Guild.Id, instanceAdminGroup?.Id, notificationsChannel?.Id, outagesChannel?.Id);

            await context.DeleteResponseAsync();
            await context.FollowupAsync("Your AzuraCast settings were saved successfully and private data has been encrypted.");

            ulong guildId = context.Guild.Id;
            GuildEntity? guild = null;
            IAsyncEnumerable<GuildEntity> guilds = _dbActions.GetGuildAsync(guildId, loadEverything: true);
            await foreach (GuildEntity itGuild in guilds)
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
                return;
            }

            guilds = _dbActions.GetGuildAsync(guildId, loadEverything: true);
            if (!await guilds.ContainsOneItemAsync())
            {
                _logger.DatabaseGuildNotFound(guildId);
                return;
            }

            if (url is not null)
                await _backgroundService.StartAzuraCastBackgroundServiceAsync(AzuraCastChecks.CheckForOnlineStatus, guilds);
        }

        [Command("modify-azuracast-checks"), Description("Modify the automatic checks for your AzuraCast instance."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask UpdateAzuraCastChecksAsync
        (
            CommandContext context,
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
                await context.RespondAsync("You have to provide at least one parameter to update.");
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

            await context.EditResponseAsync("Your settings were saved successfully.");
        }

        [Command("modify-azuracast-station"), Description("Modify one AzuraCast station you already added."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask UpdateAzuraCastStationAsync
        (
            CommandContext context,
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
                await context.RespondAsync("You have to provide at least one parameter to update.");
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

            if (stationId.HasValue || !string.IsNullOrWhiteSpace(apiKey))
                await _dbActions.UpdateAzuraCastStationAsync(context.Guild.Id, station, stationId, apiKey);

            if (adminGroup is not null || djGroup is not null || uploadChannel is not null || requestsChannel is not null || !string.IsNullOrWhiteSpace(uploadPath) || showPlaylistInEmbed is not null)
                await _dbActions.UpdateAzuraCastStationPreferencesAsync(context.Guild.Id, station, adminGroup?.Id, djGroup?.Id, uploadChannel?.Id, requestsChannel?.Id, uploadPath, showPlaylistInEmbed);

            await context.DeleteResponseAsync();
            await context.FollowupAsync("Your settings were saved successfully and private data has been encrypted.");
        }

        [Command("modify-azuracast-station-checks"), Description("Modify the automatic checks inside an AzuraCast station."), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask UpdateAzuraCastStationChecksAsync
        (
            CommandContext context,
            [Description("Choose the station you want to modify the checks."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("Enable or disable the automatic check if files have been changed."), SlashChoiceProvider<BooleanEnableDisableStateProvider>] int fileChanges = 0
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(UpdateAzuraCastStationChecksAsync), context.User.GlobalName);

            if (fileChanges is 0)
            {
                await context.RespondAsync("You have to provide at least one parameter to update.");
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
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(UpdateCoreAsync), context.User.GlobalName);

            if (adminRole is null && adminChannel is null && errorChannel is null)
            {
                await context.RespondAsync("You have to provide at least one parameter to update.");
                return;
            }

            await context.DeferResponseAsync();

            await _dbActions.UpdateGuildPreferencesAsync(context.Guild.Id, adminRole?.Id, adminChannel?.Id, errorChannel?.Id);

            await context.EditResponseAsync("Your settings were saved successfully.");
        }

        [Command("get-settings"), Description("Get all configured settings in a direct message.")]
        public async ValueTask GetSettingsAsync(CommandContext context)
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
                return;
            }

            DiscordRole? adminRole = context.Guild.GetRole(guild.Preferences.AdminRoleId);
            DiscordEmbed guildEmbed = EmbedBuilder.BuildGetSettingsGuildEmbed(guildName, guild, $"{adminRole?.Name} ({adminRole?.Id})");

            await using DiscordMessageBuilder messageBuilder = new();
            messageBuilder.AddEmbeds([guildEmbed]);

            if (guild.AzuraCast is not null)
            {
                AzuraCastEntity azuraCast = guild.AzuraCast;
                AzuraStationRecord stationRecord;
                Dictionary<ulong, string> stationRoles = [];
                Dictionary<int, string> stationNames = [];
                foreach (AzuraCastStationEntity station in azuraCast.Stations)
                {
                    DiscordRole? stationAdminRole = context.Guild.GetRole(station.Preferences.StationAdminRoleId);
                    DiscordRole? stationDjRole = context.Guild.GetRole(station.Preferences.StationDjRoleId);
                    stationRoles.Add(stationAdminRole?.Id ?? 0, stationAdminRole?.Name ?? string.Empty);
                    stationRoles.Add(stationDjRole?.Id ?? 0, stationDjRole?.Name ?? string.Empty);

                    stationRecord = await _azuraCast.GetStationAsync(new(Crypto.Decrypt(azuraCast.BaseUrl)), station.StationId);
                    stationNames.Add(station.Id, stationRecord.Name);
                }

                DiscordRole? instanceAdminRole = context.Guild.GetRole(azuraCast.Preferences.InstanceAdminRoleId);
                IEnumerable<DiscordEmbed> azuraEmbed = EmbedBuilder.BuildGetSettingsAzuraEmbed(azuraCast, $"{instanceAdminRole?.Name} ({instanceAdminRole?.Id})", stationRoles, stationNames);

                messageBuilder.AddEmbeds(azuraEmbed);
            }

            await member.SendMessageAsync(messageBuilder);

            await context.EditResponseAsync("I sent you an overview with all the settings in private. Be aware of sensitive data.");
        }

        [Command("reset-settings"), Description("Reset all of your settings, you have to reconfigure everything again.")]
        public async ValueTask ResetSettingsAsync(CommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(ResetSettingsAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            await _dbActions.DeleteGuildAsync(context.Guild.Id);
            await _dbActions.AddGuildAsync(context.Guild.Id);

            await context.EditResponseAsync("Your settings were reset successfully.\nRemember that you have to set all the configurations again.");
        }
    }
}