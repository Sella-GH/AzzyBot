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
using AzzyBot.Core.Extensions;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Services.BackgroundServices;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Core.Utilities.Enums;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "DSharpPlus best practice")]
public sealed class AzuraCastCommands
{
    [Command("azuracast"), RequireGuild, RequirePermissions(DiscordPermissions.None, DiscordPermissions.Administrator), ModuleActivatedCheck(AzzyModules.AzuraCast)]
    public sealed class AzuraCastGroup(ILogger<AzuraCastGroup> logger, AzuraCastApiService azuraCast, AzuraChecksBackgroundTask backgroundService, DbActions dbActions, MusicStreamingService musicStreaming)
    {
        private readonly ILogger<AzuraCastGroup> _logger = logger;
        private readonly AzuraCastApiService _azuraCast = azuraCast;
        private readonly AzuraChecksBackgroundTask _backgroundService = backgroundService;
        private readonly DbActions _dbActions = dbActions;
        private readonly MusicStreamingService _musicStreaming = musicStreaming;

        [Command("export-playlists"), Description("Export all playlists from the selected AzuraCast station into a zip file."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck, AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask ExportPlaylistsAsync
        (
            SlashCommandContext context,
            [Description("The station of which you want to export the playlists."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int station,
            [Description("Choose if you want to export the playlist in M3U or PLS format."), SlashChoiceProvider(typeof(AzuraExportPlaylistProvider))] string format,
            [Description("Select the playlist you want to export."), SlashAutoCompleteProvider(typeof(AzuraCastPlaylistAutocomplete))] int? userPlaylist = null
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(ExportPlaylistsAsync), context.User.GlobalName);

            AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id, loadStations: true);
            if (azuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = azuraCast.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, azuraCast.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string apiKey = (!string.IsNullOrWhiteSpace(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(azuraCast.AdminApiKey);
            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);
            string tempDir = Path.Combine(_azuraCast.FilePath, "Temp");
            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);

            IEnumerable<AzuraPlaylistRecord> playlists = await _azuraCast.GetPlaylistsAsync(new(baseUrl), apiKey, station);
            List<string> filePaths = [];
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
                AzuraPlaylistRecord? playlist = playlists.FirstOrDefault(p => p.Id == userPlaylist);
                if (playlist is null)
                {
                    await context.EditResponseAsync(GeneralStrings.PlaylistNotFound);
                    return;
                }

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
            AzuraStationRecord azuraStation = await _azuraCast.GetStationAsync(new(baseUrl), station);
            string message = ((filePaths.Count > 1) ? "Here are the playlists " : "Here is your desired playlist ") + $"from station **{azuraStation.Name}**";
            builder.WithContent(message).AddFile(zFileName, fileStream, AddFileOptions.CloseStream);
            await context.EditResponseAsync(builder);

            FileOperations.DeleteFiles(filePaths);
        }

        [Command("force-api-permission-check"), Description("Force the bot to check if the entered api key has access to all required permissions."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck, AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask ForceApiPermissionCheckAsync
        (
            SlashCommandContext context,
            [Description("The station of which you want to check the api key."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int? station = null
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(ForceApiPermissionCheckAsync), context.User.GlobalName);

            GuildEntity? dGuild = null;
            IAsyncEnumerable<GuildEntity> guild = _dbActions.GetGuildAsync(context.Guild.Id, loadEverything: true);
            if (await guild.ContainsOneItemAsync())
            {
                await using IAsyncEnumerator<GuildEntity> enumerator = guild.GetAsyncEnumerator();
                dGuild = (await enumerator.MoveNextAsync()) ? enumerator.Current : null;
                if (dGuild is null)
                {
                    _logger.DatabaseGuildNotFound(context.Guild.Id);
                    await context.EditResponseAsync(GeneralStrings.GuildNotFound);
                    return;
                }
            }
            else
            {
                _logger.DatabaseGuildNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.GuildNotFound);
                return;
            }

            if (station.HasValue)
            {
                _backgroundService.QueueApiPermissionChecks(dGuild, station.Value);
            }
            else
            {
                _backgroundService.QueueApiPermissionChecks(dGuild);
            }

            await context.EditResponseAsync("I initiated the permission check.\nThere won't be another message if your permissions are set correctly.");
        }

        [Command("force-cache-refresh"), Description("Force the bot to refresh it's local song cache for a specific station."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck, AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask ForceCacheRefreshAsync
        (
            SlashCommandContext context,
            [Description("The station of which you want to refresh the cache."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int station
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(ForceCacheRefreshAsync), context.User.GlobalName);

            GuildEntity? dGuild = null;
            IAsyncEnumerable<GuildEntity> guild = _dbActions.GetGuildAsync(context.Guild.Id, loadEverything: true);
            if (await guild.ContainsOneItemAsync())
            {
                await using IAsyncEnumerator<GuildEntity> enumerator = guild.GetAsyncEnumerator();
                dGuild = (await enumerator.MoveNextAsync()) ? enumerator.Current : null;
                if (dGuild is null)
                {
                    _logger.DatabaseGuildNotFound(context.Guild.Id);
                    await context.EditResponseAsync(GeneralStrings.GuildNotFound);
                    return;
                }
            }
            else
            {
                _logger.DatabaseGuildNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.GuildNotFound);
                return;
            }

            _backgroundService.QueueFileChangesChecks(dGuild, station);

            await context.EditResponseAsync("I initiated the cache refresh.");
        }

        [Command("force-online-check"), Description("Force the bot to check if the AzuraCast instance is online."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask ForceOnlineCheckAsync(SlashCommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(ForceOnlineCheckAsync), context.User.GlobalName);

            GuildEntity? dGuild = null;
            IAsyncEnumerable<GuildEntity> guild = _dbActions.GetGuildAsync(context.Guild.Id, loadEverything: true);
            if (await guild.ContainsOneItemAsync())
            {
                await using IAsyncEnumerator<GuildEntity> enumerator = guild.GetAsyncEnumerator();
                dGuild = (await enumerator.MoveNextAsync()) ? enumerator.Current : null;
                if (dGuild is null)
                {
                    _logger.DatabaseGuildNotFound(context.Guild.Id);
                    await context.EditResponseAsync(GeneralStrings.GuildNotFound);
                    return;
                }
            }
            else
            {
                _logger.DatabaseGuildNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.GuildNotFound);
                return;
            }

            _backgroundService.QueueInstancePing(dGuild);

            await context.EditResponseAsync("I initiated the online check for the AzuraCast instance.");
        }

        [Command("force-update-check"), Description("Force the bot to search for AzuraCast Updates."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck, AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask ForceUpdateCheckAsync(SlashCommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(ForceUpdateCheckAsync), context.User.GlobalName);

            GuildEntity? dGuild = null;
            IAsyncEnumerable<GuildEntity> guild = _dbActions.GetGuildAsync(context.Guild.Id, loadEverything: true);
            if (await guild.ContainsOneItemAsync())
            {
                await using IAsyncEnumerator<GuildEntity> enumerator = guild.GetAsyncEnumerator();
                dGuild = (await enumerator.MoveNextAsync()) ? enumerator.Current : null;
                if (dGuild is null)
                {
                    _logger.DatabaseGuildNotFound(context.Guild.Id);
                    await context.EditResponseAsync(GeneralStrings.GuildNotFound);
                    return;
                }
            }
            else
            {
                _logger.DatabaseGuildNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.GuildNotFound);
                return;
            }

            _backgroundService.QueueUpdates(dGuild);

            await context.EditResponseAsync("I initiated the check for AzuraCast Updates.\nThere won't be another message if there are no updates available.");
        }

        [Command("get-system-logs"), Description("Get the system logs of the AzuraCast instance."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck, AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask GetSystemLogsAsync
        (
            SlashCommandContext context,
            [Description("The system log you want to see."), SlashAutoCompleteProvider(typeof(AzuraCastSystemLogAutocomplete))] string logName
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(GetSystemLogsAsync), context.User.GlobalName);

            AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id);
            if (azuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            string apiKey = Crypto.Decrypt(azuraCast.AdminApiKey);
            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);
            AzuraSystemLogRecord? systemLog = await _azuraCast.GetSystemLogAsync(new(baseUrl), apiKey, logName);
            if (systemLog is null)
            {
                await context.EditResponseAsync(GeneralStrings.SystemLogEmpty);
                return;
            }

            string fileName = $"{azuraCast.Id}_{logName}_{DateTime.Now:yyyy-MM-dd_hh-mm-ss-fffffff}.log";
            string filePath = await FileOperations.CreateTempFileAsync(systemLog.Content, fileName);
            await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
            await using DiscordMessageBuilder builder = new();
            builder.WithContent($"Here is the requested system log ({logName}).");
            builder.AddFile(fileName, fileStream, AddFileOptions.CloseStream);
            await context.EditResponseAsync(builder);

            FileOperations.DeleteFile(filePath);
        }

        [Command("hardware-stats"), Description("Get the hardware stats of the running server."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
        public async ValueTask GetHardwareStatsAsync(SlashCommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(GetHardwareStatsAsync), context.User.GlobalName);

            AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id);
            if (azuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            string apiKey = Crypto.Decrypt(azuraCast.AdminApiKey);
            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);

            AzuraHardwareStatsRecord hardwareStats = await _azuraCast.GetHardwareStatsAsync(new(baseUrl), apiKey);

            DiscordEmbed embed = EmbedBuilder.BuildAzuraCastHardwareStatsEmbed(hardwareStats);

            await context.EditResponseAsync(embed);
        }

        [Command("start-station"), Description("Start the selected station."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck, AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask StartStationAsync
        (
            SlashCommandContext context,
            [Description("The station you want to start."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int station
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(StartStationAsync), context.User.GlobalName);

            AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id, loadStations: true);
            if (azuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = azuraCast.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, azuraCast.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string apiKey = (!string.IsNullOrWhiteSpace(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(azuraCast.AdminApiKey);
            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);

            await _azuraCast.StartStationAsync(new(baseUrl), apiKey, station, context);

            AzuraStationRecord azuraStation = await _azuraCast.GetStationAsync(new(baseUrl), station);

            await context.FollowupAsync($"I started the station **{azuraStation.Name}**.");
        }

        [Command("stop-station"), Description("Stop the selected station."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck, AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask StopStationAsync
        (
            SlashCommandContext context,
            [Description("The station you want to stop."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int station
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(StopStationAsync), context.User.GlobalName);

            AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id, loadStations: true);
            if (azuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = azuraCast.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, azuraCast.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string apiKey = (!string.IsNullOrWhiteSpace(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(azuraCast.AdminApiKey);
            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);
            AzuraStationRecord azuraStation = await _azuraCast.GetStationAsync(new(baseUrl), station);
            string stoppingMessage = $"I stopped the station **{azuraStation.Name}**.";

            if (await _musicStreaming.CheckIfPlayedMusicIsStationAsync(context, $"{Crypto.Decrypt(azuraCast.BaseUrl)}/listen/{azuraStation.Shortcode}"))
            {
                await _musicStreaming.StopMusicAsync(context, false);
                await context.EditResponseAsync(GeneralStrings.StationUsersDisconnected);
                await Task.Delay(TimeSpan.FromSeconds(30));
            }

            await _azuraCast.StopStationAsync(new(baseUrl), apiKey, station);

            DiscordMessage? message = await context.GetResponseAsync();
            if (message is not null)
            {
                await context.FollowupAsync(stoppingMessage);
            }
            else
            {
                await context.EditResponseAsync(stoppingMessage);
            }
        }

        [Command("toggle-song-requests"), Description("Enable or disable song requests for the selected station."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck, AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask ToggleSongRequestsAsync
        (
            SlashCommandContext context,
            [Description("The station you want to toggle song requests for."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int station
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(ToggleSongRequestsAsync), context.User.GlobalName);

            AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id, loadStations: true);
            if (azuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = azuraCast.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, azuraCast.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string apiKey = (!string.IsNullOrWhiteSpace(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(azuraCast.AdminApiKey);
            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);

            AzuraAdminStationConfigRecord stationConfig = await _azuraCast.GetStationAdminConfigAsync(new(baseUrl), apiKey, station);
            stationConfig.EnableRequests = !stationConfig.EnableRequests;
            await _azuraCast.ModifyStationAdminConfigAsync(new(baseUrl), apiKey, station, stationConfig);

            await context.EditResponseAsync($"I {Misc.GetReadableBool(stationConfig.EnableRequests, ReadableBool.EnabledDisabled, true)} song requests for station **{stationConfig.Name}**.");
        }

        [Command("update-instance"), Description("Update the AzuraCast instance to the latest version."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck, AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask UpdateInstanceAsync(SlashCommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(UpdateInstanceAsync), context.User.GlobalName);

            AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id);
            if (azuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            string apiKey = Crypto.Decrypt(azuraCast.AdminApiKey);
            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);

            AzuraUpdateRecord update = await _azuraCast.GetUpdatesAsync(new(baseUrl), apiKey);
            if (!update.NeedsReleaseUpdate && !update.NeedsRollingUpdate)
            {
                await context.EditResponseAsync(GeneralStrings.InstanceUpToDate);
                return;
            }

            await context.EditResponseAsync("I initiated the update for the AzuraCast instance. Please wait a little until it restarts.");

            await _azuraCast.UpdateInstanceAsync(new(baseUrl), apiKey);

            await context.FollowupAsync("The update was successful. Your instance is fully ready again.");
        }
    }

    [Command("dj"), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
    public sealed class DjGroup(ILogger<DjGroup> logger, AzuraCastApiService azuraCast, DbActions dbActions)
    {
        private readonly ILogger<DjGroup> _logger = logger;
        private readonly AzuraCastApiService _azuraCast = azuraCast;
        private readonly DbActions _dbActions = dbActions;

        [Command("delete-song-request"), Description("Delete a song request from the selected station."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck, AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationDJGroup, AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask DeleteSongRequestAsync
        (
            SlashCommandContext context,
            [Description("The station of which you want to delete the song request."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int station,
            [Description("The request id of the song you want to delete."), SlashAutoCompleteProvider(typeof(AzuraCastRequestAutocomplete))] int requestId = 0
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(DeleteSongRequestAsync), context.User.GlobalName);

            AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id, loadStations: true);
            if (azuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = azuraCast.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, azuraCast.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string apiKey = (!string.IsNullOrWhiteSpace(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(azuraCast.AdminApiKey);
            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);

            await _azuraCast.DeleteStationSongRequestAsync(new(baseUrl), apiKey, station, requestId);

            if (requestId is 0)
            {
                await context.EditResponseAsync("I deleted all song requests.");
                return;
            }

            await context.EditResponseAsync($"I deleted the song request with the id **{requestId}**.");
        }

        [Command("skip-song"), Description("Skips the current song of the selected station."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck, AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationDJGroup, AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask SkipSongAsync
        (
            SlashCommandContext context,
            [Description("The station of which you want to skip the song."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int station
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(SkipSongAsync), context.User.GlobalName);

            AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id, loadStations: true);
            if (azuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = azuraCast.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, azuraCast.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            if (acStation.LastSkipTime.AddSeconds(30) > DateTime.UtcNow)
            {
                await context.EditResponseAsync(GeneralStrings.SkipToFast);
                return;
            }

            string apiKey = (!string.IsNullOrWhiteSpace(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(azuraCast.AdminApiKey);
            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);

            AzuraNowPlayingDataRecord nowPlaying = await _azuraCast.GetNowPlayingAsync(new(baseUrl), station);
            if (nowPlaying.NowPlaying.Duration - nowPlaying.NowPlaying.Elapsed <= 15)
            {
                await context.EditResponseAsync(GeneralStrings.SkipAlmostOver);
                return;
            }

            await _azuraCast.SkipSongAsync(new(baseUrl), apiKey, station);

            await _dbActions.UpdateAzuraCastStationAsync(context.Guild.Id, station, null, null, DateTime.UtcNow);

            await context.EditResponseAsync($"I skipped **{nowPlaying.NowPlaying.Song.Title}** by **{nowPlaying.NowPlaying.Song.Artist}**.");
        }

        [Command("switch-playlist"), Description("Switch the current playlist of the selected station."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck, AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationDJGroup, AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask SwitchPlaylistAsync
        (
            SlashCommandContext context,
            [Description("The station of which you want to switch the playlist."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int station,
            [Description("The playlist you want to switch to."), SlashAutoCompleteProvider(typeof(AzuraCastPlaylistAutocomplete))] int playlistId,
            [Description("Choose if you want to disable all other active playlists from the station. Defaults to Yes."), SlashChoiceProvider<BooleanYesNoStateProvider>] int removeOld = 0
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(SwitchPlaylistAsync), context.User.GlobalName);

            AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id, loadStations: true);
            if (azuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = azuraCast.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, azuraCast.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string apiKey = (!string.IsNullOrWhiteSpace(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(azuraCast.AdminApiKey);
            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);
            AzuraStationRecord azuraStation = await _azuraCast.GetStationAsync(new(baseUrl), station);

            List<AzuraPlaylistStateRecord> states = await _azuraCast.SwitchPlaylistsAsync(new(baseUrl), apiKey, station, playlistId, removeOld is 1);
            StringBuilder message = new();
            message.AppendLine(CultureInfo.InvariantCulture, $"I switched the {((states.Count is 1) ? "playlist" : "playlists")} for **{azuraStation.Name}**.");
            foreach (AzuraPlaylistStateRecord state in states)
            {
                message.AppendLine(CultureInfo.InvariantCulture, $"**{state.PlaylistName}** is now **{Misc.GetReadableBool(state.PlaylistState, ReadableBool.EnabledDisabled, true)}**.");
            }

            await context.EditResponseAsync(message.ToString());
        }
    }

    [Command("music"), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
    public sealed class MusicGroup(ILogger<MusicGroup> logger, AzuraCastApiService azuraCast, AzuraRequestBackgroundTask requestBackgroundTask, QueuedBackgroundTask queue, DbActions dbActions, WebRequestService webRequest)
    {
        private readonly ILogger<MusicGroup> _logger = logger;
        private readonly AzuraCastApiService _azuraCast = azuraCast;
        private readonly AzuraRequestBackgroundTask _requestBackgroundTask = requestBackgroundTask;
        private readonly DbActions _dbActions = dbActions;
        private readonly QueuedBackgroundTask _queue = queue;
        private readonly WebRequestService _webRequest = webRequest;

        [Command("get-song-history"), Description("Get the song history of the selected station."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
        public async ValueTask GetSongHistoryAsync
        (
            SlashCommandContext context,
            [Description("The station of which you want to see the song history."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int station,
            [Description("The date of which you want to see the song history in the format YYYY-MM-DD.")] string? date = null
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(GetSongHistoryAsync), context.User.GlobalName);

            DateTime dateTime;
            if (date is null)
            {
                dateTime = DateTime.Today;
            }
            else if (!DateTime.TryParse(date, out dateTime))
            {
                await context.EditResponseAsync(GeneralStrings.DateFormatInvalid);
                return;
            }

            AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id, loadStations: true);
            if (azuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = azuraCast.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, azuraCast.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);
            string apiKey = (!string.IsNullOrWhiteSpace(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(azuraCast.AdminApiKey);
            string dateString = dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string dateStringFile = dateTime.ToString("yyyy-MM-dd_HH-mm-ss-fffffff", CultureInfo.InvariantCulture);

            IEnumerable<AzuraStationHistoryItemRecord> history = await _azuraCast.GetStationHistoryAsync(new(baseUrl), apiKey, station, dateTime, dateTime.AddDays(1));
            if (!history.Any())
            {
                await context.EditResponseAsync($"There is no song history for **{dateString}**.");
                return;
            }

            AzuraStationRecord azuraStation = await _azuraCast.GetStationAsync(new(baseUrl), station);
            IEnumerable<AzuraStationHistoryExportRecord> exportHistory = history.Select(h => new AzuraStationHistoryExportRecord() { Date = dateString, PlayedAt = Converter.ConvertFromUnixTime(h.PlayedAt), Song = h.Song, SongRequest = h.IsRequest, Streamer = h.Streamer, Playlist = h.Playlist }).Reverse().ToList();
            string fileName = $"{azuraCast.GuildId}-{azuraCast.Id}-{acStation.Id}-{acStation.StationId}_SongHistory_{dateStringFile}.csv";
            string filePath = await FileOperations.CreateCsvFileAsync(exportHistory, fileName);
            await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
            await using DiscordMessageBuilder builder = new();
            builder.WithContent($"Here is the song history for station **{Crypto.Decrypt(azuraStation.Name)}** on **{dateString}**.");
            builder.AddFile(fileName, fileStream, AddFileOptions.CloseStream);
            await context.EditResponseAsync(builder);

            FileOperations.DeleteFile(filePath);
        }

        [Command("get-songs-in-playlist"), Description("Get all songs in the selected playlist."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
        public async ValueTask GetSongsInPlaylistAsync
        (
            SlashCommandContext context,
            [Description("The station of which you want to see the songs in the playlist."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int station,
            [Description("The playlist you want to see the songs from."), SlashAutoCompleteProvider(typeof(AzuraCastPlaylistAutocomplete))] int playlistId
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(GetSongsInPlaylistAsync), context.User.GlobalName);

            AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id, loadStations: true);
            if (azuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = azuraCast.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, azuraCast.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);
            string apiKey = (!string.IsNullOrWhiteSpace(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(azuraCast.AdminApiKey);

            AzuraPlaylistRecord playlist;
            try
            {
                playlist = await _azuraCast.GetPlaylistAsync(new(baseUrl), apiKey, station, playlistId);
            }
            catch (HttpRequestException)
            {
                await context.EditResponseAsync(GeneralStrings.PlaylistNotFound);
                return;
            }

            IEnumerable<AzuraMediaItemRecord> songs = await _azuraCast.GetSongsInPlaylistAsync(new(baseUrl), apiKey, station, playlist);
            if (!songs.Any())
            {
                await context.EditResponseAsync(GeneralStrings.PlaylistEmpty);
                return;
            }

            string fileName = $"{azuraCast.GuildId}-{azuraCast.Id}-{acStation.Id}-{acStation.StationId}_PlaylistSongs_{playlist.ShortName}.csv";
            string filePath = await FileOperations.CreateCsvFileAsync(songs, fileName);
            await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
            await using DiscordMessageBuilder builder = new();
            builder.WithContent($"Here are the songs in playlist **{playlist.Name}**.");
            builder.AddFile(fileName, fileStream, AddFileOptions.CloseStream);
            await context.EditResponseAsync(builder);

            FileOperations.DeleteFile(filePath);
        }

        [Command("now-playing"), Description("Get the currently playing song on the selected station."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck]
        public async ValueTask GetNowPlayingAsync
        (
            SlashCommandContext context,
            [Description("The station of which you want to see what's played."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int station
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(GetNowPlayingAsync), context.User.GlobalName);

            AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id, loadStations: true, loadStationPrefs: true);
            if (azuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = azuraCast.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, azuraCast.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);

            AzuraNowPlayingDataRecord nowPlaying;
            try
            {
                nowPlaying = await _azuraCast.GetNowPlayingAsync(new(baseUrl), station);
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
            {
                await context.EditResponseAsync(GeneralStrings.StationOffline);
                return;
            }

            string? playlistName = null;
            if (acStation.Preferences.ShowPlaylistInNowPlaying)
            {
                string apiKey = (!string.IsNullOrWhiteSpace(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(azuraCast.AdminApiKey);
                IEnumerable<AzuraPlaylistRecord> playlist = await _azuraCast.GetPlaylistsAsync(new(baseUrl), apiKey, station);
                playlistName = playlist.Where(p => p.Name == nowPlaying.NowPlaying.Playlist).Select(static p => p.Name).FirstOrDefault();
            }

            DiscordEmbed embed = EmbedBuilder.BuildAzuraCastMusicNowPlayingEmbed(nowPlaying, playlistName);

            await context.EditResponseAsync(embed);
        }

        [Command("search-song"), Description("Search for a song on the selected station."), RequireGuild, ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck, AzuraCastDiscordChannelCheck]
        public async ValueTask SearchSongAsync
        (
            SlashCommandContext context,
            [Description("The station of which you want to search for a song."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int station,
            [Description("The song you want to search for."), SlashAutoCompleteProvider(typeof(AzuraCastRequestAutocomplete))] string song
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(SearchSongAsync), context.User.GlobalName);

            AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id, loadStations: true);
            if (azuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = azuraCast.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, azuraCast.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string apiKey = (!string.IsNullOrWhiteSpace(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(azuraCast.AdminApiKey);
            Uri baseUrl = new(Crypto.Decrypt(azuraCast.BaseUrl));

            AzuraAdminStationConfigRecord stationConfig = await _azuraCast.GetStationAdminConfigAsync(baseUrl, apiKey, station);
            AzuraRequestRecord songRequest;
            if (stationConfig.EnableRequests)
            {
                songRequest = await _azuraCast.GetRequestableSongAsync(baseUrl, apiKey, station, song);
            }
            else
            {
                AzuraSongDataRecord songData = await _azuraCast.GetSongInfoAsync(baseUrl, apiKey, acStation, false, song);
                songRequest = new()
                {
                    Song = songData,
                    RequestId = songData.UniqueId
                };
            }

            bool isQueued;
            bool isRequested;
            bool isPlayed = false;

            if (stationConfig.RequestThreshold is not 0)
            {
                IEnumerable<AzuraRequestQueueItemRecord> requestsPlayed = await _azuraCast.GetStationRequestItemsAsync(baseUrl, apiKey, station, true);
                long threshold = Converter.ConvertToUnixTime(DateTime.UtcNow.AddMinutes(-stationConfig.RequestThreshold));
                isPlayed = requestsPlayed.Any(r => (r.Track.SongId == songRequest.Song.SongId || r.Track.UniqueId == songRequest.Song.UniqueId) && r.Timestamp >= threshold);
            }

            IEnumerable<AzuraStationQueueItemDetailedRecord> queue = await _azuraCast.GetStationQueueAsync(baseUrl, apiKey, station);
            IEnumerable<AzuraRequestQueueItemRecord> requestsPending = await _azuraCast.GetStationRequestItemsAsync(baseUrl, apiKey, station, false);
            isQueued = queue.Any(q => q.Song.SongId == songRequest.Song.SongId && q.Song.UniqueId == songRequest.Song.UniqueId);
            isRequested = requestsPending.Any(r => r.Track.SongId == songRequest.Song.SongId && r.Track.UniqueId == songRequest.Song.UniqueId);

            DiscordEmbed embed = EmbedBuilder.BuildAzuraCastMusicSearchSongEmbed(songRequest, isQueued || isRequested, isPlayed);
            if (!azuraCast.IsOnline || !stationConfig.IsEnabled || !stationConfig.EnableRequests || isQueued || isRequested || isPlayed)
            {
                await context.EditResponseAsync(embed);
                return;
            }

            DiscordButtonComponent button = new(DiscordButtonStyle.Success, $"request_song_{context.User.Id}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fffffff}", "Request Song");
            await using DiscordMessageBuilder builder = new();
            builder.AddEmbed(embed);
            builder.AddComponents(button);

            DiscordMessage message = await context.EditResponseAsync(builder);
            InteractivityResult<ComponentInteractionCreatedEventArgs> result = await message.WaitForButtonAsync(context.User, TimeSpan.FromMinutes(1));
            if (!result.TimedOut)
            {
                azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id, loadStations: true);
                if (azuraCast is null)
                {
                    _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                    await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                    return;
                }

                acStation = azuraCast.Stations.FirstOrDefault(s => s.StationId == station);
                if (acStation is null)
                {
                    _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, azuraCast.Id, station);
                    await context.EditResponseAsync(GeneralStrings.StationNotFound);
                    return;
                }

                string response = string.Empty;
                DateTime lastRequest = acStation.LastRequestTime.AddSeconds(16);
                DateTime now = DateTime.UtcNow;
                if (lastRequest > now)
                {
                    AzuraCustomQueueItemRecord record = new(context.Guild.Id, baseUrl, station, songRequest.RequestId, DateTime.UtcNow);
                    _ = Task.Run(async () => await _queue.QueueBackgroundWorkItemAsync(async ct => await _requestBackgroundTask.CreateRequestAsync(record)));

                    response = GeneralStrings.SongRequestQueued;
                }
                else
                {
                    await _azuraCast.RequestSongAsync(baseUrl, station, songRequest.RequestId);
                    await _dbActions.UpdateAzuraCastStationAsync(context.Guild.Id, acStation.StationId, lastRequestTime: DateTime.UtcNow.AddSeconds(16));

                    response = GeneralStrings.SongRequested;
                }

                await using DiscordInteractionResponseBuilder interaction = new()
                {
                    Content = response
                };
                await result.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, interaction);
                await context.EditResponseAsync(embed);

                return;
            }

            await context.EditResponseAsync(embed);
        }

        [Command("upload-files"), Description("Upload a file to the selected station."), RequireGuild, RequirePermissions(DiscordPermissions.None, DiscordPermissions.AttachFiles), ModuleActivatedCheck(AzzyModules.AzuraCast), AzuraCastOnlineCheck, FeatureAvailableCheck(AzuraCastFeatures.FileUploading), AzuraCastDiscordChannelCheck]
        public async ValueTask UploadFilesAsync
        (
            SlashCommandContext context,
            [Description("The station you want to upload the file to."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int station,
            [Description("The file you want to upload.")] DiscordAttachment file
        )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));
            ArgumentNullException.ThrowIfNull(file, nameof(file));
            ArgumentException.ThrowIfNullOrWhiteSpace(file.FileName, nameof(file.FileName));
            ArgumentException.ThrowIfNullOrWhiteSpace(file.Url, nameof(file.Url));

            _logger.CommandRequested(nameof(UploadFilesAsync), context.User.GlobalName);

            if (file.FileSize > 52428800)
            {
                await context.EditResponseAsync(GeneralStrings.FileTooBig);
                return;
            }

            string[] allowedTypes = [".aac", ".flac", ".m4a", ".mp3", ".ogg", ".opus", ".wav"];
            if (!allowedTypes.Contains(Path.GetExtension(file.FileName)))
            {
                await context.EditResponseAsync($"The file type is not allowed. Please upload a file with the following extensions: {string.Join(", ", allowedTypes)}");
                return;
            }

            AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id, loadStations: true, loadStationPrefs: true);
            if (azuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = azuraCast.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, azuraCast.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string apiKey = (!string.IsNullOrWhiteSpace(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(azuraCast.AdminApiKey);
            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);

            string filePath = Path.Combine(Path.GetTempPath(), $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fffffff}_{azuraCast.GuildId}-{azuraCast.Id}-{acStation.Id}_{file.FileName}");
            await _webRequest.DownloadAsync(new(file.Url), filePath);

            AzuraFileComplianceRecord compliance = AzuraFileChecker.FileIsCompliant(filePath);
            if (!compliance.IsCompliant)
            {
                StringBuilder message = new();
                message.AppendLine("This file is not compliant with the requirements. Please fix the following issues:");
                if (!compliance.PerformerCompliance)
                    message.AppendLine("- The performers tag is missing.");

                if (!compliance.TitleCompliance)
                    message.AppendLine("- The title tag is missing.");

                await context.EditResponseAsync(message.ToString());
                FileOperations.DeleteFile(filePath);
                return;
            }

            string uploadPath = (string.IsNullOrWhiteSpace(acStation.Preferences.FileUploadPath)) ? "/" : acStation.Preferences.FileUploadPath;

            AzuraFilesDetailedRecord uploadedFile = await _azuraCast.UploadFileAsync<AzuraFilesDetailedRecord>(new(baseUrl), apiKey, station, filePath, file.FileName, uploadPath);
            AzuraStationRecord azuraStation = await _azuraCast.GetStationAsync(new(baseUrl), station);

            DiscordEmbed embed = EmbedBuilder.BuildAzuraCastUploadFileEmbed(uploadedFile, file.FileSize, azuraStation.Name);

            await context.EditResponseAsync(embed);

            FileOperations.DeleteFile(filePath);
        }
    }
}
