using System;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Logging;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Commands;

internal sealed class ConfigCommands
{
    [Command("config")]
    [RequireGuild]
    [RequirePermissions(DiscordPermissions.None, DiscordPermissions.Administrator)]
    internal sealed class Config
    {
        [Command("set")]
        internal sealed class Set(DbActions db, ILogger<Set> logger)
        {
            private readonly DbActions _db = db;
            private readonly ILogger<Set> _logger = logger;

            [Command("azuracast")]
            public async ValueTask ConfigSetAzuraCastAsync(SlashCommandContext context, string apiUrl = "", string apiKey = "", int stationid = 0, [ChannelTypes(DiscordChannelType.Text)] DiscordChannel? requestsChannel = null, [ChannelTypes(DiscordChannelType.Text)] DiscordChannel? outagesChannel = null, bool ShowPlaylistsInNowPlaying = false)
            {
                _logger.CommandRequested(nameof(ConfigSetAzuraCastAsync), context.User.GlobalName);

                await context.DeferResponseAsync();

                DiscordGuild guild = context.Guild ?? throw new InvalidOperationException("Guild is null");

                await _db.SetAzuraCastEntityAsync(guild.Id, apiUrl, apiKey, stationid, requestsChannel?.Id ?? 0, outagesChannel?.Id ?? 0, ShowPlaylistsInNowPlaying);

                if (!string.IsNullOrWhiteSpace(apiKey) || !string.IsNullOrWhiteSpace(apiUrl))
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
            public async ValueTask ConfigSetAzuraCastChecksAsync(SlashCommandContext context, bool fileChanges = false, bool serverStatus = false, bool updates = false, bool updatesChangelog = false)
            {
                _logger.CommandRequested(nameof(ConfigSetAzuraCastChecksAsync), context.User.GlobalName);

                await context.DeferResponseAsync();

                DiscordGuild guild = context.Guild ?? throw new InvalidOperationException("Guild is null");

                await _db.SetAzuraCastChecksEntityAsync(guild.Id, fileChanges, serverStatus, updates, updatesChangelog);

                await context.EditResponseAsync("Your settings were saved successfully.");
            }
        }
    }
}
