using System;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Logging;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Commands;

internal sealed class ConfigCommands
{
    [Command("config")]
    [RequireGuild]
    [RequirePermissions(DiscordPermissions.None, DiscordPermissions.Administrator)]
    internal sealed class Config(DbActions db, ILogger<Config> logger)
    {
        private readonly DbActions _db = db;
        private readonly ILogger<Config> _logger = logger;

        [Command("azuracast")]
        public async ValueTask ConfigSetAzuraCastAsync(CommandContext context, string apiKey = "", Uri? apiUrl = null, int stationId = 0, [ChannelTypes(DiscordChannelType.Text)] DiscordChannel? requestsChannel = null, [ChannelTypes(DiscordChannelType.Text)] DiscordChannel? outagesChannel = null, bool showPlaylistInNowPlaying = false)
        {
            _logger.CommandRequested(nameof(ConfigSetAzuraCastAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");

            await _db.SetAzuraCastEntityAsync(guildId, apiKey, apiUrl, stationId, requestsChannel?.Id ?? 0, outagesChannel?.Id ?? 0, showPlaylistInNowPlaying);

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

        [Command("azuracast-checks")]
        public async ValueTask ConfigSetAzuraCastChecksAsync(CommandContext context, bool fileChanges = false, bool serverStatus = false, bool updates = false, bool updatesChangelog = false)
        {
            _logger.CommandRequested(nameof(ConfigSetAzuraCastChecksAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            ulong guildId = context.Guild?.Id ?? throw new InvalidOperationException("Guild is null");

            await _db.SetAzuraCastChecksEntityAsync(guildId, fileChanges, serverStatus, updates, updatesChangelog);

            await context.EditResponseAsync("Your settings were saved successfully.");
        }
    }
}
