﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AzzyBot.Commands.Autocompletes;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Logging;
using AzzyBot.Utilities;
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
    public sealed class ConfigGroup(DbActions db, ILogger<ConfigGroup> logger)
    {
        private readonly DbActions _db = db;
        private readonly ILogger<ConfigGroup> _logger = logger;

        [Command("add-azuracast"), Description("Configure AzuraCast for your server. This is a requirement to use the features.")]
        public async ValueTask AddAzuraCastAsync
            (
            CommandContext context,
            [Description("Set the base Url, an example: https://demo.azuracast.com/")] Uri url,
            [Description("Select a channel to get notifications when your azuracast installation is down."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel outagesChannel
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(AddAzuraCastAsync), context.User.GlobalName);

            if (outagesChannel is null)
            {
                await context.RespondAsync("You have to select an outages channel first!");
                return;
            }

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            GuildsEntity guild = await _db.GetGuildAsync(guildId);
            if (guild.AzuraCastSet)
            {
                await context.EditResponseAsync("AzuraCast is already set up for your server.");
                return;
            }

            await _db.AddAzuraCastAsync(guildId, url, outagesChannel.Id);

            await context.DeleteResponseAsync();
            await context.FollowupAsync("Your AzuraCast installation was added successfully and your data has been encrypted.");
        }

        [Command("add-azuracast-mount"), Description("Let's you add an AzuraCast mount point for streaming.")]
        public async ValueTask AddAzuraCastMountAsync
            (
            CommandContext context,
            [Description("Choose the station you want to add the mount."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("Enter the mount point name.")] string mountName,
            [Description("Enter the mount point stub.")] string mount
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(AddAzuraCastMountAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.AddAzuraCastMountPointAsync(guildId, station, mountName, mount);

            await context.EditResponseAsync("Your mount point was added successfully.");
        }

        [Command("add-azuracast-station"), Description("Let's you dd an AzuraCast station to manage.")]
        public async ValueTask AddAzuraCastStationAsync
            (
            CommandContext context,
            [Description("Enter the station id of your azuracast station.")] int station,
            [Description("Enter the name of the new station.")] string stationName,
            [Description("Enter the api key of your azuracast installation.")] string apiKey,
            [Description("Select a channel to get music requests when a request is not found on the server."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel requestsChannel,
            [Description("Enable or disable the preference of HLS streams if you add an able mount point.")] bool hls,
            [Description("Enable or disable the showing of the playlist in the nowplaying embed.")] bool showPlaylist,
            [Description("Enable or disable the automatic check if files have been changed.")] bool fileChanges,
            [Description("Enable or disable the automatic check if the AzuraCast instance of your server is down.")] bool serverStatus,
            [Description("Enable or disable the automatic check for AzuraCast updates.")] bool updates,
            [Description("Enable or disable the addition of the changelog to the posted AzuraCast updates.")] bool updatesChangelog
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(AddAzuraCastStationAsync), context.User.GlobalName);

            if (requestsChannel is null)
            {
                await context.RespondAsync("You have to select a request channel first!");
                return;
            }

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.AddAzuraCastStationAsync(guildId, station, stationName, apiKey, requestsChannel.Id, hls, showPlaylist, fileChanges, serverStatus, updates, updatesChangelog);

            await context.DeleteResponseAsync();
            await context.FollowupAsync("Your station was added successfully. Your station name and api key have been encrypted. Your request was also deleted for security reasons.");
        }

        [Command("delete-azuracast"), Description("Delete the existing AzuraCast setup.")]
        public async ValueTask DeleteAzuraCastAsync(CommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(DeleteAzuraCastAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.DeleteAzuraCastAsync(guildId);

            await context.EditResponseAsync("Your AzuraCast setup was deleted successfully.");
        }

        [Command("delete-azuracast-mount"), Description("Delete an existing AzuraCast mount from a station.")]
        public async ValueTask DeleteAzuraCastMountAsync
            (
            CommandContext context,
            [Description("Select the station of the mount point."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("Select the mount point you want to delete."), SlashAutoCompleteProvider<AzuraCastMountAutocomplete>] int mountId
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(DeleteAzuraCastMountAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.DeleteAzuraCastMountAsync(guildId, station, mountId);

            await context.EditResponseAsync("Your mount point was deleted successfully.");
        }

        [Command("delete-azuracast-station"), Description("Delete an existing station.")]
        public async ValueTask DeleteAzuraCastStationAsync
            (
            CommandContext context,
            [Description("Choose the station you want to delete."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(DeleteAzuraCastStationAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.DeleteAzuraCastStationAsync(guildId, station);

            await context.EditResponseAsync("Your station was deleted successfully.");
        }

        [Command("modify-azuracast"), Description("Modify the general AzuraCast settings.")]
        public async ValueTask UpdateAzuraCastAsync
            (
            CommandContext context,
            [Description("Update the base Url, an example: https://demo.azuracast.com/")] Uri? url = null,
            [Description("Update the channel to get notifications when your azuracast installation is down."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel? outagesChannel = null
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(UpdateAzuraCastAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.UpdateAzuraCastAsync(guildId, url, outagesChannel?.Id);

            await context.DeleteResponseAsync();
            await context.FollowupAsync("Your AzuraCast settings were saved successfully and have been encrypted.");
        }

        [Command("modify-azuracast-checks"), Description("Configure the automatic checks inside a station.")]
        public async ValueTask UpdateAzuraCastChecksAsync
            (
            CommandContext context,
            [Description("Choose the station you want to modify the checks."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("Enable or disable the automatic check if files have been changed.")] bool? fileChanges = null,
            [Description("Enable or disable the automatic check if the AzuraCast instance of your server is down.")] bool? serverStatus = null,
            [Description("Enable or disable the automatic check for AzuraCast updates.")] bool? updates = null,
            [Description("Enable or disable the addition of the changelog to the posted AzuraCast updates.")] bool? updatesChangelog = null
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(UpdateAzuraCastChecksAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.UpdateAzuraCastChecksAsync(guildId, station, fileChanges, serverStatus, updates, updatesChangelog);

            await context.EditResponseAsync("Your settings were saved successfully.");
        }

        [Command("modify-azuracast-station"), Description("Modify one station you already added.")]
        public async ValueTask UpdateAzuraCastStationAsync
            (
            CommandContext context,
            [Description("Choose the station you want to modify."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("Modify the station id.")] int? stationId = null,
            [Description("Modify the name.")] string? stationName = null,
            [Description("Modify the api key.")] string? apiKey = null,
            [Description("Modify the channel to get music requests when a request is not found on the server."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel? requestsChannel = null,
            [Description("Enable or disable the preference of HLS streams if you add an able mount point.")] bool? hls = null,
            [Description("Enable or disable the showing of the playlist in the nowplaying embed.")] bool? showPlaylist = null
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(UpdateAzuraCastStationAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.UpdateAzuraCastStationAsync(guildId, station, stationId, stationName, apiKey, requestsChannel?.Id, hls, showPlaylist);

            await context.DeleteResponseAsync();
            await context.FollowupAsync("Your settings were saved successfully. Your station name and api key have been encrypted. Your request was also deleted for security reasons.");
        }

        [Command("modify-core")]
        public async ValueTask UpdateCoreAsync(CommandContext context, [Description("Select a channel to get notifications when the bot runs into an issue."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel? errorChannel = null)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(UpdateCoreAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.UpdateGuildAsync(guildId, errorChannel?.Id ?? 0, null);

            await context.EditResponseAsync("Your settings were saved successfully.");
        }

        [Command("get-settings"), Description("Get all configured settings per direct message.")]
        public async ValueTask GetSettingsAsync(CommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(GetSettingsAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            string guildName = context.Guild.Name;
            DiscordMember member = context.Member ?? throw new InvalidOperationException("Member is null");

            GuildsEntity guild = await _db.GetGuildAsync(guildId);
            DiscordEmbed guildEmbed = EmbedBuilder.BuildGetSettingsGuildEmbed(guildName, guild);

            await using DiscordMessageBuilder messageBuilder = new();
            messageBuilder.AddEmbeds([guildEmbed]);

            if (guild.AzuraCast is not null)
            {
                AzuraCastEntity azuraCast = guild.AzuraCast;
                IReadOnlyList<DiscordEmbed> azuraEmbed = EmbedBuilder.BuildGetSettingsAzuraEmbed(azuraCast);

                messageBuilder.AddEmbeds(azuraEmbed);
            }

            await member.SendMessageAsync(messageBuilder);

            await context.EditResponseAsync("I sent you an overview with all the settings in private. Be aware of sensitive data.");
        }

        [Command("reset-settings"), Description("Reset all of your settings, you have to add everything again.")]
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
