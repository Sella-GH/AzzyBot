using System;
using System.ComponentModel;
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

public sealed class ConfigCommands
{
    [Command("config"), RequireGuild, RequirePermissions(DiscordPermissions.None, DiscordPermissions.Administrator)]
    public sealed class Config(DbActions db, ILogger<Config> logger)
    {
        private readonly DbActions _db = db;
        private readonly ILogger<Config> _logger = logger;

        [Command("config-azuracast"), Description("Configure the settings of the AzuraCast module.")]
        public async ValueTask SetAzuraCastAsync
            (
            CommandContext context,
            [Description("Enter the api key of your azuracast installation.")] string apiKey = "",
            [Description("Enter the url of your api endpoint, like: https://demo.azuracast.com/api")] Uri? apiUrl = null,
            [Description("Enter the station id of your azuracast station.")] int stationId = 0,
            [Description("Select a channel to get music requests when a request is not found on the server."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel? requestsChannel = null,
            [Description("Select a channel to get notifications when your azuracast installation is down."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel? outagesChannel = null,
            [Description("Enable or disable the showing of the playlist in the nowplaying embed.")] bool showPlaylistInNowPlaying = false
            )
        {
            _logger.CommandRequested(nameof(SetAzuraCastAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.SetAzuraCastEntityAsync(guildId, apiKey, apiUrl, stationId, requestsChannel?.Id ?? 0, outagesChannel?.Id ?? 0, showPlaylistInNowPlaying);
            await _db.SetGuildEntityAsync(guildId);

            if (!string.IsNullOrWhiteSpace(apiKey) || apiUrl is not null)
            {
                await context.DeleteResponseAsync();
                await context.FollowupAsync("Your settings were saved and sensitive data has been encrypted. Your message was also deleted for security reasons.");
            }
            else
            {
                await context.EditResponseAsync("Your settings were saved successfully.");
            }
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
            _logger.CommandRequested(nameof(SetAzuraCastChecksAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.SetAzuraCastChecksEntityAsync(guildId, fileChanges, serverStatus, updates, updatesChangelog);

            await context.EditResponseAsync("Your settings were saved successfully.");
        }

        [Command("config-core")]
        public async ValueTask SetCoreAsync(CommandContext context, [Description("Select a channel to get notifications when the bot runs into an issue."), ChannelTypes(DiscordChannelType.Text)] DiscordChannel? errorChannel = null)
        {
            _logger.CommandRequested(nameof(SetCoreAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.SetGuildEntityAsync(guildId, errorChannel?.Id ?? 0);

            await context.EditResponseAsync("Your settings were saved successfully.");
        }

        [Command("get-settings"), Description("Get all configured settings per direct message.")]
        public async ValueTask GetSettingsAsync(CommandContext context)
        {
            _logger.CommandRequested(nameof(GetSettingsAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            string guildName = context.Guild.Name;
            DiscordMember member = context.Member ?? throw new InvalidOperationException("Member is null");

            AzuraCastEntity azuraCast = await _db.GetAzuraCastEntityAsync(guildId);
            AzuraCastChecksEntity checks = await _db.GetAzuraCastChecksEntityAsync(guildId);
            GuildsEntity guild = await _db.GetGuildEntityAsync(guildId);

            DiscordEmbed guildEmbed = EmbedBuilder.BuildGetSettingsGuildEmbed(guildName, guild);
            DiscordEmbed azuraEmbed = EmbedBuilder.BuildGetSettingsAzuraEmbed(azuraCast);
            DiscordEmbed azuraChecksEmbed = EmbedBuilder.BuildGetSettingsAzuraChecksEmbed(checks);

            await using DiscordMessageBuilder messageBuilder = new();
            messageBuilder.AddEmbeds([guildEmbed, azuraEmbed, azuraChecksEmbed]);

            await member.SendMessageAsync(messageBuilder);

            await context.EditResponseAsync("I sent you an overview with all the settings in private. Be aware of sensitive data.");
        }

        [Command("reset-settings"), Description("Reset all of your settings to the default values.")]
        public async ValueTask ResetSettingsAsync(CommandContext context)
        {
            _logger.CommandRequested(nameof(ResetSettingsAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");
            await _db.RemoveGuildEntityAsync(guildId);
            await _db.AddGuildEntityAsync(guildId);

            await context.EditResponseAsync("Your settings were reset successfully.");
        }
    }
}
