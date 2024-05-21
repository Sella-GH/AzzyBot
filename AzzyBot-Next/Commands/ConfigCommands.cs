using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Logging;
using AzzyBot.Utilities;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.ContextChecks;
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

        [Command("add-azuracast-mount"), Description("Let's you add an AzuraCast mount point for streaming.")]
        public async ValueTask AddAzuraCastMountAsync
            (
            CommandContext context,
            [Description("Enter the mount point name.")] string mountName,
            [Description("Enter the mount point stub.")] string mount
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(AddAzuraCastMountAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.AddAzuraCastMountPointAsync(guildId, mountName, mount);

            await context.EditResponseAsync("Your mount point was added successfully.");
        }

        [Command("add-azuracast-station"), Description("Let's you dd an AzuraCast station to manage.")]
        public async ValueTask AddAzuraCastStationAsync
            (
            CommandContext context,
            [Description("Enter the api key of your azuracast installation.")] string apiKey,
            [Description("Enter the url of your api endpoint, like: https://demo.azuracast.com/api")] Uri apiUrl,
            [Description("Enter the station id of your azuracast station.")] int stationId,
            [Description("Select a channel to get music requests when a request is not found on the server."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel requestsChannel,
            [Description("Select a channel to get notifications when your azuracast installation is down."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel outagesChannel,
            [Description("Enable or disable the preference of HLS streams if you add an able mount point.")] bool hlsStreaming,
            [Description("Enable or disable the showing of the playlist in the nowplaying embed.")] bool showPlaylistInNowPlaying
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(AddAzuraCastStationAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.AddAzuraCastEntityAsync(guildId, apiKey, apiUrl, stationId, requestsChannel?.Id ?? 0, outagesChannel?.Id ?? 0, hlsStreaming, showPlaylistInNowPlaying);

            await context.DeleteResponseAsync();
            await context.FollowupAsync("Your settings were saved and sensitive data has been encrypted. Your message was also deleted for security reasons.");
        }

        [Command("config-azuracast-checks"), Description("Configure the settings of the automatic checks inside the AzuraCast module.")]
        public async ValueTask SetAzuraCastChecksAsync
            (
            CommandContext context,
            [Description("Enable or disable the automatic check if files have been changed.")] bool fileChanges = false,
            [Description("Enable or disable the automatic check if the AzuraCast instance of your server is down.")] bool serverStatus = false,
            [Description("Enable or disable the automatic check for AzuraCast updates.")] bool updates = false,
            [Description("Enable or disable the addition of the changelog to the posted AzuraCast updates.")] bool updatesChangelog = false
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(SetAzuraCastChecksAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.SetAzuraCastChecksEntityAsync(guildId, fileChanges, serverStatus, updates, updatesChangelog);

            await context.EditResponseAsync("Your settings were saved successfully.");
        }

        [Command("config-core")]
        public async ValueTask SetCoreAsync(CommandContext context, [Description("Select a channel to get notifications when the bot runs into an issue."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel? errorChannel = null)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(SetCoreAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.SetGuildEntityAsync(guildId, errorChannel?.Id ?? 0);

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

            GuildsEntity guild = await _db.GetGuildEntityAsync(guildId);
            List<AzuraCastEntity> azuraCast = await _db.GetAzuraCastEntitiesAsync(guildId);
            AzuraCastChecksEntity checks = await _db.GetAzuraCastChecksEntityAsync(guildId);
            List<AzuraCastMountsEntity> mounts = await _db.GetAzuraCastMountsEntitiesAsync(guildId);

            DiscordEmbed guildEmbed = EmbedBuilder.BuildGetSettingsGuildEmbed(guildName, guild);
            IReadOnlyList<DiscordEmbed> azuraEmbed = EmbedBuilder.BuildGetSettingsAzuraEmbed(azuraCast);
            DiscordEmbed azuraChecks = EmbedBuilder.BuildGetSettingsAzuraChecksEmbed(checks);
            DiscordEmbed azuraMounts = EmbedBuilder.BuildGetSettingsAzuraMountsEmbed(mounts);

            await using DiscordMessageBuilder messageBuilder = new();
            messageBuilder.AddEmbeds([guildEmbed, .. azuraEmbed, azuraChecks, azuraMounts]);

            await member.SendMessageAsync(messageBuilder);

            await context.EditResponseAsync("I sent you an overview with all the settings in private. Be aware of sensitive data.");
        }

        [Command("reset-settings"), Description("Reset all of your settings to the default values.")]
        public async ValueTask ResetSettingsAsync(CommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            _logger.CommandRequested(nameof(ResetSettingsAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.RemoveGuildEntityAsync(guildId);
            await _db.AddGuildEntityAsync(guildId);

            await context.EditResponseAsync("Your settings were reset successfully.");
        }
    }
}
