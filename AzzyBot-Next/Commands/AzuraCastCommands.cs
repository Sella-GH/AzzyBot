using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AzzyBot.Commands.Autocompletes;
using AzzyBot.Commands.Checks;
using AzzyBot.Commands.Choices;
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
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
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

        [Command("export-playlists"), Description("Export all playlists from the selected AzuraCast station into a zip file."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
        public async ValueTask ExportPlaylistsAsync
        (
            CommandContext context,
            [Description("The station of which you want to export the playlists."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int stationId,
            [Description("Choose if you want to export the plalylist in M3U or PLS format."), SlashChoiceProvider(typeof(AzuraExportPlaylistProvider))] string format,
            [Description("Select the playlist you want to export."), SlashAutoCompleteProvider(typeof(AzuraCastPlaylistAutocomplete))] int? userPlaylist = null
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(ExportPlaylistsAsync), context.User.GlobalName);

            GuildsEntity guild = await _dbActions.GetGuildAsync(context.Guild.Id);
            AzuraCastEntity azuraCast = guild.AzuraCast ?? throw new InvalidOperationException("AzuraCast is null");
            AzuraCastStationEntity acStation = azuraCast.Stations.FirstOrDefault(s => s.StationId == stationId) ?? throw new InvalidOperationException("Station is null");
            string apiKey = (!string.IsNullOrWhiteSpace(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(azuraCast.AdminApiKey);
            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);
            string tempDir = Path.Combine(_azuraCast.FilePath, "Temp");
            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);

            List<string> filePaths = [];
            IReadOnlyList<AzuraPlaylistRecord> playlists = await _azuraCast.GetPlaylistsAsync(new(baseUrl), apiKey, stationId);
            if (userPlaylist is null)
            {
                foreach (AzuraPlaylistRecord playlist in playlists)
                {
                    Uri playlistUrl = (format is "m3u") ? playlist.Links.Export.M3U : playlist.Links.Export.PLS;
                    string fileName = Path.Combine(tempDir, $"{azuraCast.Id}-{acStation.Id}-{playlist.ShortName}.{format}");
                    filePaths.Add(fileName);
                    await _azuraCast.DownloadPlaylistAsync(playlistUrl, apiKey, fileName);
                }
            }
            else
            {
                AzuraPlaylistRecord playlist = playlists.FirstOrDefault(p => p.Id == userPlaylist) ?? throw new InvalidOperationException("Playlist not found");

                Uri playlistUrl = (format is "m3u") ? playlist.Links.Export.M3U : playlist.Links.Export.PLS;
                string fileName = Path.Combine(tempDir, $"{azuraCast.Id}-{acStation.Id}-{playlist.ShortName}.{format}");
                filePaths.Add(fileName);
                await _azuraCast.DownloadPlaylistAsync(playlistUrl, apiKey, fileName);
            }

            string zFileName = $"{azuraCast.Id}-{acStation.Id}-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{((filePaths.Count > 1) ? "Playlists" : "Playlist")}_{format}.zip";
            await FileOperations.CreateZipFileAsync(zFileName, _azuraCast.FilePath, tempDir);
            filePaths.Add(Path.Combine(_azuraCast.FilePath, zFileName));

            await using FileStream fileStream = new(Path.Combine(_azuraCast.FilePath, zFileName), FileMode.Open, FileAccess.Read);
            await using DiscordMessageBuilder builder = new();
            string message = ((filePaths.Count > 1) ? "Here are the playlists " : "Here is your desired playlist ") + $"from station **{Crypto.Decrypt(acStation.Name)}**";
            builder.WithContent(message).AddFile(zFileName, fileStream, AddFileOptions.CloseStream);
            await context.EditResponseAsync(builder);

            FileOperations.DeleteFiles(filePaths);
        }

        [Command("force-cache-refresh"), Description("Force the bot to refresh it's local song cache for a specific station."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
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

        [Command("force-online-check"), Description("Force the bot to check if the AzuraCast instance is online."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast)]
        public async ValueTask ForceOnlineCheckAsync(CommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(ForceOnlineCheckAsync), context.User.GlobalName);

            await _backgroundService.StartAzuraCastBackgroundServiceAsync(AzuraCastChecks.CheckForOnlineStatus, context.Guild.Id);

            await context.EditResponseAsync("I initiated the online check for the AzuraCast instance, please wait a little for the result.");
        }

        [Command("force-update-check"), Description("Force the bot to search for AzuraCast Updates."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
        public async ValueTask ForceUpdateCheckAsync(CommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(ForceUpdateCheckAsync), context.User.GlobalName);

            await _backgroundService.StartAzuraCastBackgroundServiceAsync(AzuraCastChecks.CheckForUpdates, context.Guild.Id);

            await context.EditResponseAsync("I initiated the check for AzuraCast Updates, please wait a little.\nThere won't be an answer if there are no updates available.");
        }

        [Command("hardware-stats"), Description("Get the hardware stats of the running server."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
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

        [Command("start-station"), Description("Start the selected station."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
        public async ValueTask StartStationAsync
        (
            CommandContext context,
            [Description("The station you want to start."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int stationId
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(StartStationAsync), context.User.GlobalName);

            GuildsEntity guild = await _dbActions.GetGuildAsync(context.Guild.Id);
            AzuraCastEntity azuraCast = guild.AzuraCast ?? throw new InvalidOperationException("AzuraCast is null");
            AzuraCastStationEntity station = azuraCast.Stations.FirstOrDefault(s => s.StationId == stationId) ?? throw new InvalidOperationException("Station is null");
            string apiKey = (!string.IsNullOrWhiteSpace(station.ApiKey)) ? Crypto.Decrypt(station.ApiKey) : Crypto.Decrypt(azuraCast.AdminApiKey);
            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);

            await _azuraCast.StartStationAsync(new(baseUrl), apiKey, stationId, context);

            await context.EditResponseAsync($"I started the station **{Crypto.Decrypt(station.Name)}**.");
        }

        [Command("stop-station"), Description("Stop the selected station."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
        public async ValueTask StopStationAsync
        (
            CommandContext context,
            [Description("The station you want to stop."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int stationId
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(StopStationAsync), context.User.GlobalName);

            GuildsEntity guild = await _dbActions.GetGuildAsync(context.Guild.Id);
            AzuraCastEntity azuraCast = guild.AzuraCast ?? throw new InvalidOperationException("AzuraCast is null");
            AzuraCastStationEntity station = azuraCast.Stations.FirstOrDefault(s => s.StationId == stationId) ?? throw new InvalidOperationException("Station is null");
            string apiKey = (!string.IsNullOrWhiteSpace(station.ApiKey)) ? Crypto.Decrypt(station.ApiKey) : Crypto.Decrypt(azuraCast.AdminApiKey);
            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);

            await _azuraCast.StopStationAsync(new(baseUrl), apiKey, stationId);

            await context.EditResponseAsync($"I stopped the station **{Crypto.Decrypt(station.Name)}**.");
        }

        [Command("toggle-song-requests"), Description("Enable or disable song requests for the selected station."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
        public async ValueTask ToggleSongRequestsAsync
        (
            CommandContext context,
            [Description("The station you want to toggle song requests for."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int stationId
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(ToggleSongRequestsAsync), context.User.GlobalName);

            GuildsEntity guild = await _dbActions.GetGuildAsync(context.Guild.Id);
            AzuraCastEntity azuraCast = guild.AzuraCast ?? throw new InvalidOperationException("AzuraCast is null");
            AzuraCastStationEntity station = azuraCast.Stations.FirstOrDefault(s => s.StationId == stationId) ?? throw new InvalidOperationException("Station is null");
            string apiKey = (!string.IsNullOrWhiteSpace(station.ApiKey)) ? Crypto.Decrypt(station.ApiKey) : Crypto.Decrypt(azuraCast.AdminApiKey);
            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);

            AzuraAdminStationConfigRecord stationConfig = await _azuraCast.GetStationAdminConfigAsync(new(baseUrl), apiKey, stationId);
            await _azuraCast.ModifyStationAdminConfigAsync(new(baseUrl), apiKey, stationId, stationConfig);

            await context.EditResponseAsync($"I {Misc.ReadableBool(!stationConfig.EnableRequests, ReadbleBool.EnabledDisabled, true)} song requests for station **{Crypto.Decrypt(station.Name)}**.");
        }
    }

    [Command("dj"), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
    public sealed class DjGroup(ILogger<DjGroup> logger, AzuraCastApiService azuraCast, DbActions dbActions)
    {
        private readonly ILogger<DjGroup> _logger = logger;
        private readonly AzuraCastApiService _azuraCast = azuraCast;
        private readonly DbActions _dbActions = dbActions;

        [Command("skip-song"), Description("Skips the current song of the selected station."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
        public async ValueTask SkipSongAsync
        (
            CommandContext context,
            [Description("The station of which you want to skip the song."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int stationId
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(SkipSongAsync), context.User.GlobalName);

            GuildsEntity guild = await _dbActions.GetGuildAsync(context.Guild.Id);
            AzuraCastEntity azuraCast = guild.AzuraCast ?? throw new InvalidOperationException("AzuraCast is null");
            AzuraCastStationEntity station = azuraCast.Stations.FirstOrDefault(s => s.StationId == stationId) ?? throw new InvalidOperationException("Station is null");
            string apiKey = (!string.IsNullOrWhiteSpace(station.ApiKey)) ? Crypto.Decrypt(station.ApiKey) : Crypto.Decrypt(azuraCast.AdminApiKey);
            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);

            AzuraNowPlayingDataRecord nowPlaying = await _azuraCast.GetNowPlayingAsync(new(baseUrl), stationId);
            if (nowPlaying.NowPlaying.Duration - nowPlaying.NowPlaying.Elapsed <= 5)
            {
                await context.EditResponseAsync("This song is almost over - please wait!");
                return;
            }

            await _azuraCast.SkipSongAsync(new(baseUrl), apiKey, stationId);

            await context.EditResponseAsync($"I skipped **{nowPlaying.NowPlaying.Song.Title}** by **{nowPlaying.NowPlaying.Song.Artist}**.");
        }

        [Command("switch-playlist"), Description("Switch the current playlist of the selected station."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
        public async ValueTask SwitchPlaylistAsync
        (
            CommandContext context,
            [Description("The station of which you want to switch the playlist."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int stationId,
            [Description("The playlist you want to switch to."), SlashAutoCompleteProvider(typeof(AzuraCastPlaylistAutocomplete))] int playlistId,
            [Description("Choose if you want to disable all other active playlists from the station. Defaults to Yes.")] bool removeOld = true
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(SwitchPlaylistAsync), context.User.GlobalName);

            GuildsEntity guild = await _dbActions.GetGuildAsync(context.Guild.Id);
            AzuraCastEntity azuraCast = guild.AzuraCast ?? throw new InvalidOperationException("AzuraCast is null");
            AzuraCastStationEntity station = azuraCast.Stations.FirstOrDefault(s => s.StationId == stationId) ?? throw new InvalidOperationException("Station is null");
            string apiKey = (!string.IsNullOrWhiteSpace(station.ApiKey)) ? Crypto.Decrypt(station.ApiKey) : Crypto.Decrypt(azuraCast.AdminApiKey);
            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);
            string stationName = Crypto.Decrypt(station.Name);

            List<AzuraPlaylistStateRecord> states = await _azuraCast.SwitchPlaylistsAsync(new(baseUrl), apiKey, stationId, playlistId, removeOld);
            StringBuilder message = new();
            message.AppendLine(CultureInfo.InvariantCulture, $"I switched the {((states.Count is 1) ? "playlist" : "playlists")} for **{stationName}**.");
            foreach (AzuraPlaylistStateRecord state in states)
            {
                message.AppendLine(CultureInfo.InvariantCulture, $"**{state.PlaylistName}** is now **{Misc.ReadableBool(state.PlaylistState, ReadbleBool.EnabledDisabled, true)}**.");
            }

            await context.EditResponseAsync(message.ToString());
        }
    }

    [Command("music"), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
    public sealed class MusicGroup(ILogger<MusicGroup> logger, AzuraCastApiService azuraCast, DbActions dbActions)
    {
        private readonly ILogger<MusicGroup> _logger = logger;
        private readonly AzuraCastApiService _azuraCast = azuraCast;
        private readonly DbActions _dbActions = dbActions;

        [Command("now-playing"), Description("Get the currently playing song on the selected station."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
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

        [Command("search-song"), Description("Search for a song on the selected station."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
        public async ValueTask SearchSongAsync
        (
            CommandContext context,
            [Description("The station of which you want to search for a song."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int stationId,
            [Description("The song you want to search for."), SlashAutoCompleteProvider(typeof(AzuraCastRequestAutocomplete))] string song
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(SearchSongAsync), context.User.GlobalName);

            GuildsEntity guild = await _dbActions.GetGuildAsync(context.Guild.Id);
            AzuraCastEntity azuraCast = guild.AzuraCast ?? throw new InvalidOperationException("AzuraCast is null");
            AzuraCastStationEntity station = azuraCast.Stations.FirstOrDefault(s => s.StationId == stationId) ?? throw new InvalidOperationException("Station is null");
            string apiKey = (!string.IsNullOrWhiteSpace(station.ApiKey)) ? Crypto.Decrypt(station.ApiKey) : Crypto.Decrypt(azuraCast.AdminApiKey);
            Uri baseUrl = new(Crypto.Decrypt(azuraCast.BaseUrl));

            AzuraRequestRecord songRequest = await _azuraCast.GetRequestableSongAsync(baseUrl, apiKey, stationId, song);
            DiscordEmbed embed = EmbedBuilder.BuildAzuraCastMusicSearchSongEmbed(songRequest);
            AzuraAdminStationConfigRecord stationConfig = await _azuraCast.GetStationAdminConfigAsync(baseUrl, apiKey, stationId);
            if (!stationConfig.EnableRequests)
            {
                await context.EditResponseAsync(embed);
                return;
            }

            DiscordButtonComponent button = new(DiscordButtonStyle.Success, "request_song", "Request Song");
            await using DiscordMessageBuilder builder = new();
            builder.AddEmbed(embed);
            builder.AddComponents(button);

            DiscordMessage message = await context.EditResponseAsync(builder);
            InteractivityResult<ComponentInteractionCreateEventArgs> result = await message.WaitForButtonAsync(context.User, TimeSpan.FromMinutes(1));

            if (!result.TimedOut)
            {
                await _azuraCast.RequestSongAsync(baseUrl, stationId, songRequest.RequestId);
                await context.FollowupAsync("I requested the song for you.");

                return;
            }

            await context.EditResponseAsync(embed);
        }
    }
}
