using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AzzyBot.Commands.Autocompletes;
using AzzyBot.Commands.Checks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Logging;
using AzzyBot.Services.Modules;
using AzzyBot.Utilities;
using AzzyBot.Utilities.Encryption;
using AzzyBot.Utilities.Enums;
using AzzyBot.Utilities.Records.AzuraCast;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Commands;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "DSharpPlus best practice")]
public sealed class AzuraCastCommands
{
    [Command("azuracast"), RequireGuild, RequirePermissions(DiscordPermissions.None, DiscordPermissions.Administrator), ModuleActivatedCheck(AzzyModules.AzuraCast)]
    public sealed class AzuraCastGroup(ILogger<AzuraCastGroup> logger, AzuraCastApiService azuraCast, AzzyBackgroundService backgroundService, DbActions dbActions)
    {
        private readonly ILogger<AzuraCastGroup> _logger = logger;
        private readonly AzuraCastApiService _azuraCast = azuraCast;
        private readonly AzzyBackgroundService _backgroundService = backgroundService;
        private readonly DbActions _dbActions = dbActions;

        [Command("force-cache-refresh"), Description("Force the bot to refresh it's local song cache for a specific station."), AzuraCastOnlineCheck]
        public async ValueTask ForceCacheRefreshAsync
        (
            CommandContext context,
            [Description("The station of which you want to refresh the cache."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int stationId
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(ForceCacheRefreshAsync), context.User.GlobalName);

            await _backgroundService.StartAzuraCastBackgroundServiceAsync(AzuraCastChecks.CheckForFileChanges, context.Guild.Id, stationId);

            await context.EditResponseAsync("I initiated the cache refresh, please wait a little for it to occur.");
        }

        [Command("force-online-check"), Description("Force the bot to check if the AzuraCast instance is online.")]
        public async ValueTask ForceOnlineCheckAsync(CommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(ForceOnlineCheckAsync), context.User.GlobalName);

            await _backgroundService.StartAzuraCastBackgroundServiceAsync(AzuraCastChecks.CheckForOnlineStatus, context.Guild.Id);

            await context.EditResponseAsync("I initiated the online check for the AzuraCast instance, please wait a little for the result.");
        }

        [Command("force-update-check"), Description("Force the bot to search for AzuraCast Updates."), AzuraCastOnlineCheck]
        public async ValueTask ForceUpdateCheckAsync(CommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(ForceUpdateCheckAsync), context.User.GlobalName);

            await _backgroundService.StartAzuraCastBackgroundServiceAsync(AzuraCastChecks.CheckForUpdates, context.Guild.Id);

            await context.EditResponseAsync("I initiated the check for AzuraCast Updates, please wait a little.\nThere won't be an answer if there are no updates available.");
        }

        [Command("hardware-stats"), Description("Get the hardware stats of the running server."), AzuraCastOnlineCheck]
        public async ValueTask GetHardwareStatsAsync(CommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(GetHardwareStatsAsync), context.User.GlobalName);

            GuildsEntity guild = await _dbActions.GetGuildAsync(context.Guild.Id);
            AzuraCastEntity azuraCast = guild.AzuraCast ?? throw new InvalidOperationException("AzuraCast is null");
            string apiKey = Crypto.Decrypt(azuraCast.AdminApiKey);
            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);

            AzuraHardwareStatsRecord hardwareStats = await _azuraCast.GetHardwareStatsAsync(new(baseUrl), apiKey);

            DiscordEmbed embed = EmbedBuilder.BuildAzuraCastHardwareStatsEmbed(hardwareStats);

            await context.EditResponseAsync(embed);
        }
    }

    [Command("music"), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
    public sealed class MusicGroup(ILogger<MusicGroup> logger, AzuraCastApiService azuraCast, DbActions dbActions)
    {
        private readonly ILogger<MusicGroup> _logger = logger;
        private readonly AzuraCastApiService _azuraCast = azuraCast;
        private readonly DbActions _dbActions = dbActions;

        [Command("now-playing"), Description("Get the currently playing song on the selected station.")]
        public async ValueTask GetNowPlayingAsync
            (
            CommandContext context,
            [Description("The station of which you want to see what's played."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int stationId
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(GetNowPlayingAsync), context.User.GlobalName);

            GuildsEntity guild = await _dbActions.GetGuildAsync(context.Guild.Id);
            AzuraCastEntity azuraCast = guild.AzuraCast ?? throw new InvalidOperationException("AzuraCast is null");
            AzuraCastStationEntity station = azuraCast.Stations.FirstOrDefault(s => s.StationId == stationId) ?? throw new InvalidOperationException("Station is null");
            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);

            AzuraNowPlayingDataRecord? nowPlaying = null;
            try
            {
                nowPlaying = await _azuraCast.GetNowPlayingAsync(new(baseUrl), stationId);
            }
            catch (HttpRequestException)
            {
                await context.EditResponseAsync("This station is currently offline.");
            }

            if (nowPlaying is null)
            {
                await context.EditResponseAsync("No song is currently playing.");
                return;
            }

            string? playlistName = null;
            if (station.ShowPlaylistInNowPlaying)
            {
                string apiKey = (!string.IsNullOrWhiteSpace(station.ApiKey)) ? Crypto.Decrypt(station.ApiKey) : Crypto.Decrypt(azuraCast.AdminApiKey);
                IReadOnlyList<AzuraPlaylistRecord> playlist = await _azuraCast.GetPlaylistsAsync(new(baseUrl), apiKey, stationId);
                playlistName = playlist.Where(p => p.Name == nowPlaying.NowPlaying.Playlist).Select(p => p.Name).FirstOrDefault();
            }

            DiscordEmbed embed = EmbedBuilder.BuildAzuraCastMusicNowPlayingEmbed(nowPlaying, playlistName);

            await context.EditResponseAsync(embed);
        }
    }
}
