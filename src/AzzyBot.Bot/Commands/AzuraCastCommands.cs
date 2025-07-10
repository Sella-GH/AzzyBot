using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using AzzyBot.Bot.Commands.Autocompletes;
using AzzyBot.Bot.Commands.Checks;
using AzzyBot.Bot.Commands.Choices;
using AzzyBot.Bot.Services;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Utilities;
using AzzyBot.Bot.Utilities.Enums;
using AzzyBot.Bot.Utilities.Helpers;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Core.Utilities.Enums;
using AzzyBot.Core.Utilities.Helpers;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "DSharpPlus best practice")]
public sealed class AzuraCastCommands
{
    [Command("azuracast"), RequireGuild, RequirePermissions(botPermissions: [], userPermissions: [DiscordPermission.Administrator]), ModuleActivatedCheck([AzzyModules.LegalTerms, AzzyModules.AzuraCast])]
    public sealed class AzuraCastGroup(ILogger<AzuraCastGroup> logger, AzuraCastApiService azuraCastApi, AzuraCastFileService azuraCastFile, AzuraCastPingService azuraCastPing, AzuraCastUpdateService azuraCastUpdate, DbActions dbActions, DiscordBotService botService, MusicStreamingService musicStreaming)
    {
        private readonly ILogger<AzuraCastGroup> _logger = logger;
        private readonly AzuraCastApiService _azuraCastApi = azuraCastApi;
        private readonly AzuraCastFileService _azuraCastFile = azuraCastFile;
        private readonly AzuraCastPingService _azuraCastPing = azuraCastPing;
        private readonly AzuraCastUpdateService _azuraCastUpdate = azuraCastUpdate;
        private readonly DbActions _dbActions = dbActions;
        private readonly DiscordBotService _botService = botService;
        private readonly MusicStreamingService _musicStreaming = musicStreaming;

        [Command("export-playlists"), Description("Export all playlists from the selected AzuraCast station into a zip file."), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup]), AzuraCastOnlineCheck]
        public async ValueTask ExportPlaylistsAsync
        (
            SlashCommandContext context,
            [Description("The station of which you want to export the playlists."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("Choose if you want to export the playlist in M3U or PLS format."), SlashChoiceProvider<AzuraExportPlaylistProvider>] string format,
            [Description("Select the playlist you want to export."), SlashAutoCompleteProvider<AzuraCastPlaylistAutocomplete>] int? userPlaylist = null
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(ExportPlaylistsAsync), context.User.GlobalName);

            AzuraCastEntity? ac = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadPrefs: true, loadStations: true);
            if (ac is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = ac.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, ac.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string apiKey = (!string.IsNullOrEmpty(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(ac.AdminApiKey);
            string baseUrl = Crypto.Decrypt(ac.BaseUrl);
            string tempDir = Path.Combine(_azuraCastApi.FilePath, "Temp");
            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);

            IEnumerable<AzuraPlaylistRecord>? playlists = await _azuraCastApi.GetPlaylistsAsync(new(baseUrl), apiKey, station);
            if (playlists is null)
            {
                await context.EditResponseAsync(GeneralStrings.PermissionIssue);
                await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"I don't have the permission to access the **playlists** endpoint on station {station}.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }

            List<string> filePaths = [];
            if (userPlaylist is null)
            {
                foreach (AzuraPlaylistRecord playlist in playlists)
                {
                    Uri playlistUrl = (format is "m3u") ? playlist.Links.Export.M3U : playlist.Links.Export.PLS;
                    string fileName = Path.Combine(tempDir, $"{ac.Id}-{acStation.Id}-{playlist.ShortName}.{format}");
                    filePaths.Add(fileName);
                    await _azuraCastApi.DownloadPlaylistAsync(playlistUrl, apiKey, fileName);
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
                string fileName = Path.Combine(tempDir, $"{ac.Id}-{acStation.Id}-{playlist.ShortName}.{format}");
                filePaths.Add(fileName);
                await _azuraCastApi.DownloadPlaylistAsync(playlistUrl, apiKey, fileName);
            }

            string zFileName = $"{ac.Id}-{acStation.Id}-{DateTimeOffset.Now:yyyy-MM-dd_HH-mm-ss}_{((filePaths.Count > 1) ? "Playlists" : "Playlist")}_{format}.zip";
            await FileOperations.CreateZipFileAsync(zFileName, _azuraCastApi.FilePath, tempDir);
            filePaths.Add(Path.Combine(_azuraCastApi.FilePath, zFileName));

            await using FileStream fileStream = new(Path.Combine(_azuraCastApi.FilePath, zFileName), FileMode.Open, FileAccess.Read, FileShare.None);
            await using DiscordMessageBuilder builder = new();
            AzuraStationRecord? azuraStation = await _azuraCastApi.GetStationAsync(new(baseUrl), station);
            if (azuraStation is null)
            {
                await context.EditResponseAsync(GeneralStrings.PermissionIssue);
                await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"I don't have the permission to access the **station** endpoint on station {station}.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }

            string message = ((filePaths.Count > 1) ? "Here are the playlists " : "Here is your desired playlist ") + $"from station **{azuraStation.Name}**";
            builder.WithContent(message).AddFile(zFileName, fileStream, AddFileOptions.CloseStream);
            await context.EditResponseAsync(builder);

            FileOperations.DeleteFiles(filePaths);
        }

        [Command("force-api-permission-check"), Description("Force the bot to check if the entered api key has access to all required permissions."), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup]), AzuraCastOnlineCheck]
        public async ValueTask ForceApiPermissionCheckAsync
        (
            SlashCommandContext context,
            [Description("The station of which you want to check the api key."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int? station = null
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(ForceApiPermissionCheckAsync), context.User.GlobalName);

            AzuraCastEntity? dAzuraCast = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadStations: true, loadStationChecks: true);
            if (dAzuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            if (!station.HasValue)
            {
                await _azuraCastApi.CheckForApiPermissionsAsync(dAzuraCast);
            }
            else
            {
                AzuraCastStationEntity? dStation = dAzuraCast.Stations.FirstOrDefault(s => s.StationId == station);
                if (dStation is null)
                {
                    _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, dAzuraCast.Id, station.Value);
                    await context.EditResponseAsync(GeneralStrings.StationNotFound);
                    return;
                }

                await _azuraCastApi.CheckForApiPermissionsAsync(dStation);
            }

            await context.EditResponseAsync("I initiated the permission check.\nThere won't be another message if your permissions are set correctly.");
        }

        [Command("force-cache-refresh"), Description("Force the bot to refresh its local song cache for a specific station."), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup]), AzuraCastOnlineCheck]
        public async ValueTask ForceCacheRefreshAsync
        (
            SlashCommandContext context,
            [Description("The station of which you want to refresh the cache."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int? station = null
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(ForceCacheRefreshAsync), context.User.GlobalName);

            AzuraCastEntity? dAzuraCast = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadPrefs: true, loadStations: true, loadStationChecks: true, loadGuild: true);
            if (dAzuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            if (!station.HasValue)
            {
                foreach (AzuraCastStationEntity dStation in dAzuraCast.Stations)
                {
                    await _azuraCastFile.CheckForFileChangesAsync(dStation);
                }
            }
            else
            {
                AzuraCastStationEntity? dStation = dAzuraCast.Stations.FirstOrDefault(s => s.StationId == station);
                if (dStation is null)
                {
                    _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, dAzuraCast.Id, station.Value);
                    await context.EditResponseAsync(GeneralStrings.StationNotFound);
                    return;
                }

                await _azuraCastFile.CheckForFileChangesAsync(dStation);
            }

            await context.EditResponseAsync((!station.HasValue) ? "I initiated the cache refresh for all stations." : "I initiated the cache refresh for the selected station.");
        }

        [Command("force-online-check"), Description("Force the bot to check if the AzuraCast instance is online."), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask ForceOnlineCheckAsync(SlashCommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(ForceOnlineCheckAsync), context.User.GlobalName);

            AzuraCastEntity? dAzuraCast = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadPrefs: true, loadGuild: true);
            if (dAzuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            await context.EditResponseAsync("I initiated the online check for the AzuraCast instance.");
            await _azuraCastPing.PingInstanceAsync(dAzuraCast);
        }

        [Command("force-update-check"), Description("Force the bot to search for AzuraCast Updates."), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.InstanceAdminGroup]), AzuraCastOnlineCheck]
        public async ValueTask ForceUpdateCheckAsync(SlashCommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(ForceUpdateCheckAsync), context.User.GlobalName);

            AzuraCastEntity? dAzuraCast = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadChecks: true, loadPrefs: true, loadGuild: true);
            if (dAzuraCast is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            await context.EditResponseAsync("I initiated the check for AzuraCast Updates.\nThere won't be another message if there are no updates available.");
            await _azuraCastUpdate.CheckForAzuraCastUpdatesAsync(dAzuraCast, true);
        }

        [Command("get-system-logs"), Description("Get the system logs of the AzuraCast instance."), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.InstanceAdminGroup]), AzuraCastOnlineCheck]
        public async ValueTask GetSystemLogsAsync
        (
            SlashCommandContext context,
            [Description("The system log you want to see."), SlashAutoCompleteProvider<AzuraCastSystemLogAutocomplete>] string logName
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(GetSystemLogsAsync), context.User.GlobalName);

            AzuraCastEntity? ac = await _dbActions.ReadAzuraCastAsync(context.Guild.Id);
            if (ac is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            string apiKey = Crypto.Decrypt(ac.AdminApiKey);
            string baseUrl = Crypto.Decrypt(ac.BaseUrl);
            AzuraSystemLogRecord? systemLog = await _azuraCastApi.GetSystemLogAsync(new(baseUrl), apiKey, logName);
            if (systemLog is null)
            {
                await context.EditResponseAsync(GeneralStrings.SystemLogEmpty);
                return;
            }

            string fileName = $"{ac.Id}_{logName}_{DateTimeOffset.Now:yyyy-MM-dd_hh-mm-ss-fffffff}.log";
            string filePath = await FileOperations.CreateTempFileAsync(systemLog.Content, fileName);
            await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            await using DiscordMessageBuilder builder = new();
            builder.WithContent($"Here is the requested system log ({logName}).");
            builder.AddFile(fileName, fileStream, AddFileOptions.CloseStream);
            await context.EditResponseAsync(builder);

            FileOperations.DeleteFile(filePath);
        }

        [Command("hardware-stats"), Description("Get the hardware stats of the running server."), AzuraCastOnlineCheck]
        public async ValueTask GetHardwareStatsAsync(SlashCommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(GetHardwareStatsAsync), context.User.GlobalName);

            AzuraCastEntity? ac = await _dbActions.ReadAzuraCastAsync(context.Guild.Id);
            if (ac is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            string apiKey = Crypto.Decrypt(ac.AdminApiKey);
            string baseUrl = Crypto.Decrypt(ac.BaseUrl);

            AzuraHardwareStatsRecord? hardwareStats = await _azuraCastApi.GetHardwareStatsAsync(new(baseUrl), apiKey);
            if (hardwareStats is null)
            {
                await context.EditResponseAsync(GeneralStrings.PermissionIssue);
                await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"I don't have the permission to access the **administrative server stats** endpoint.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }

            DiscordEmbed embed = EmbedBuilder.BuildAzuraCastHardwareStatsEmbed(hardwareStats);

            await context.EditResponseAsync(embed);
        }

        [Command("start-station"), Description("Start the selected station."), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup]), AzuraCastOnlineCheck]
        public async ValueTask StartStationAsync
        (
            SlashCommandContext context,
            [Description("The station you want to start."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(StartStationAsync), context.User.GlobalName);

            AzuraCastEntity? ac = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadStations: true);
            if (ac is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = ac.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, ac.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string apiKey = (!string.IsNullOrEmpty(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(ac.AdminApiKey);
            string baseUrl = Crypto.Decrypt(ac.BaseUrl);

            await _azuraCastApi.StartStationAsync(new(baseUrl), apiKey, station, context);

            AzuraStationRecord? azuraStation = await _azuraCastApi.GetStationAsync(new(baseUrl), station);
            if (azuraStation is null)
            {
                await context.EditResponseAsync(GeneralStrings.PermissionIssue);
                await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"I don't have the permission to access the **administrative station** endpoint on station {station}.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }

            await context.FollowupAsync($"I started the station **{azuraStation.Name}**.");
        }

        [Command("station-nowplaying-embed"), Description("Configure the channel where the now playing embed should be sent. Leave empty to remove it."), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup]), AzuraCastOnlineCheck]
        public async ValueTask SetStationNowPlayingEmbedAsync
        (
            SlashCommandContext context,
            [Description("The station you want to set the now playing embed for."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("The channel where the now playing embed should be sent.")] DiscordChannel? channel = null
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(SetStationNowPlayingEmbedAsync), context.User.GlobalName);

            string response = string.Empty;
            if (channel is null)
            {
                AzuraCastStationPreferencesEntity? preferences = await _dbActions.ReadAzuraCastStationPreferencesAsync(context.Guild.Id, station);
                if (preferences is null)
                {
                    response = GeneralStrings.StationNotFound;
                }
                else if (preferences.NowPlayingEmbedChannelId is 0)
                {
                    response = "There is no now playing channel set for this station.";
                }
                else
                {
                    DiscordChannel? currChannel = await _botService.GetDiscordChannelAsync(preferences.NowPlayingEmbedChannelId);
                    if (currChannel is null)
                    {
                        response = "The currently set now playing channel does not exist anymore.";
                    }
                    else
                    {
                        DiscordMessage? currMessage = await currChannel.GetMessageAsync(preferences.NowPlayingEmbedMessageId);
                        if (currMessage is not null)
                        {
                            try
                            {
                                await currMessage.DeleteAsync();
                            }
                            catch (NotFoundException)
                            {
                                response = "The currently set now playing message for this station does not exist anymore.";
                            }
                        }
                    }
                }
            }

            await _dbActions.UpdateAzuraCastStationPreferencesAsync(context.Guild.Id, station, nowPlayingEmbedChannelId: channel?.Id ?? 0, nowPlayingEmbedMessageId: 0);

            if (string.IsNullOrEmpty(response))
            {
                response = (channel is null)
                    ? "I removed the now playing embed message and the configuration for the channel of this station."
                    : $"I set the now playing embed channel to **{channel.Mention}** for this station.";
            }

            await context.EditResponseAsync(response);
        }

        [Command("stop-station"), Description("Stop the selected station."), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup]), AzuraCastOnlineCheck]
        public async ValueTask StopStationAsync
        (
            SlashCommandContext context,
            [Description("The station you want to stop."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(StopStationAsync), context.User.GlobalName);

            AzuraCastEntity? ac = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadStations: true);
            if (ac is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = ac.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, ac.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string apiKey = (!string.IsNullOrEmpty(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(ac.AdminApiKey);
            string baseUrl = Crypto.Decrypt(ac.BaseUrl);
            AzuraStationRecord? azuraStation = await _azuraCastApi.GetStationAsync(new(baseUrl), station);
            if (azuraStation is null)
            {
                await context.EditResponseAsync(GeneralStrings.PermissionIssue);
                await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"I don't have the permission to access the **administrative station** endpoint on station {station}.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }

            string stoppingMessage = $"I stopped the station **{azuraStation.Name}**.";

            if (await _musicStreaming.CheckIfPlayedMusicIsStationAsync(context, $"{Crypto.Decrypt(ac.BaseUrl)}/listen/{azuraStation.Shortcode}"))
            {
                await _musicStreaming.StopMusicAsync(context);

                DiscordMember? bot = await _botService.GetDiscordMemberAsync(context.Guild.Id);
                 if (bot?.VoiceState?.ChannelId is not null && await bot.VoiceState.GetChannelAsync() is DiscordChannel botChannel)
                    await botChannel.SendMessageAsync(GeneralStrings.VoiceStationStopped);

                await context.EditResponseAsync(GeneralStrings.StationUsersDisconnected);
                await Task.Delay(TimeSpan.FromSeconds(30));
            }

            await _azuraCastApi.StopStationAsync(new(baseUrl), apiKey, station);

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

        [Command("toggle-song-requests"), Description("Enable or disable song requests for the selected station."), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup]), AzuraCastOnlineCheck]
        public async ValueTask ToggleSongRequestsAsync
        (
            SlashCommandContext context,
            [Description("The station you want to toggle song requests for."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(ToggleSongRequestsAsync), context.User.GlobalName);

            AzuraCastEntity? ac = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadStations: true);
            if (ac is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = ac.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, ac.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string apiKey = (!string.IsNullOrEmpty(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(ac.AdminApiKey);
            string baseUrl = Crypto.Decrypt(ac.BaseUrl);

            AzuraAdminStationConfigRecord? stationConfig = await _azuraCastApi.GetStationAdminConfigAsync(new(baseUrl), apiKey, station);
            if (stationConfig is null)
            {
                await context.EditResponseAsync(GeneralStrings.PermissionIssue);
                await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"I don't have the permission to access the **administrative station** endpoint on station {station}.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }

            stationConfig.EnableRequests = !stationConfig.EnableRequests;
            await _azuraCastApi.ModifyStationAdminConfigAsync(new(baseUrl), apiKey, station, stationConfig);

            await context.EditResponseAsync($"I {Misc.GetReadableBool(stationConfig.EnableRequests, ReadableBool.EnabledDisabled, true)} song requests for station **{stationConfig.Name}**.");
        }

        [Command("update-instance"), Description("Update the AzuraCast instance to the latest version."), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.InstanceAdminGroup]), AzuraCastOnlineCheck]
        public async ValueTask UpdateInstanceAsync(SlashCommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(UpdateInstanceAsync), context.User.GlobalName);

            AzuraCastEntity? ac = await _dbActions.ReadAzuraCastAsync(context.Guild.Id);
            if (ac is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            string apiKey = Crypto.Decrypt(ac.AdminApiKey);
            string baseUrl = Crypto.Decrypt(ac.BaseUrl);

            string? body = await _azuraCastApi.GetUpdatesAsync(new(baseUrl), apiKey);
            if (string.IsNullOrEmpty(body))
            {
                await context.EditResponseAsync(GeneralStrings.PermissionIssue);
                await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"I don't have the permission to access the **administrative updates** endpoint.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }

            AzuraUpdateRecord? update = null;
            try
            {
                update = JsonSerializer.Deserialize(body, JsonDeserializationSourceGen.Default.AzuraUpdateRecord);
            }
            catch (JsonException ex)
            {
                AzuraUpdateErrorRecord? errorRecord = JsonSerializer.Deserialize(body, JsonDeserializationSourceGen.Default.AzuraUpdateErrorRecord) ?? throw new InvalidOperationException($"Failed to deserialize body: {body}", ex);
                await context.EditResponseAsync(GeneralStrings.InstanceUpdateError);
                await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"Failed to check for updates: {errorRecord.FormattedMessage}");
                return;
            }

            if (update is null)
            {
                await context.EditResponseAsync(GeneralStrings.PermissionIssue);
                await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"I don't have the permission to access the **administrative updates** endpoint.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }

            if (!update.NeedsReleaseUpdate && !update.NeedsRollingUpdate)
            {
                await context.EditResponseAsync(GeneralStrings.InstanceUpToDate);
                return;
            }

            await context.EditResponseAsync("I initiated the update for the AzuraCast instance. Please wait a little until it restarts.");

            await _azuraCastApi.UpdateInstanceAsync(new(baseUrl), apiKey);

            await context.FollowupAsync("The update was successful. Your instance is fully ready again.");
        }
    }

    [Command("dj"), RequireGuild, ModuleActivatedCheck([AzzyModules.LegalTerms, AzzyModules.AzuraCast]), AzuraCastOnlineCheck]
    public sealed class DjGroup(ILogger<DjGroup> logger, AzuraCastApiService azuraCast, DbActions dbActions, DiscordBotService botService)
    {
        private readonly ILogger<DjGroup> _logger = logger;
        private readonly AzuraCastApiService _azuraCast = azuraCast;
        private readonly DbActions _dbActions = dbActions;
        private readonly DiscordBotService _botService = botService;

        [Command("add-internal-song-request"), Description("Adds an internal song request which will be played ASAP. This bypasses the usual api."), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationDJGroup, AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask AddInternalSongRequestAsync
        (
            SlashCommandContext context,
            [Description("The station of which you want to add the song request."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("The song you want to request."), SlashAutoCompleteProvider<AzuraCastRequestAutocomplete>] string song
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(AddInternalSongRequestAsync), context.User.GlobalName);

            AzuraCastEntity? ac = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadStations: true);
            if (ac is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = ac.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, ac.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string apiKey = (!string.IsNullOrEmpty(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(ac.AdminApiKey);
            Uri baseUrl = new(Crypto.Decrypt(ac.BaseUrl));

            AzuraAdminStationConfigRecord? stationConfig = await _azuraCast.GetStationAdminConfigAsync(baseUrl, apiKey, station);
            if (stationConfig is null)
            {
                await context.EditResponseAsync(GeneralStrings.PermissionIssue);
                await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"I don't have the permission to access the **administrative station** endpoint on station {station}.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }

            if (!stationConfig.IsEnabled)
            {
                await context.EditResponseAsync(GeneralStrings.StationOffline);
                return;
            }

            IEnumerable<AzuraFilesRecord>? songs = await _azuraCast.GetFilesOnlineAsync<AzuraFilesRecord>(baseUrl, apiKey, station);
            if (songs is null)
            {
                await context.EditResponseAsync(GeneralStrings.PermissionIssue);
                await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"I don't have the permission to access the **files** endpoint on station {station}.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }

            AzuraFilesRecord? songData = songs.FirstOrDefault(s => s.SongId == song);
            if (songData is null)
            {
                await context.EditResponseAsync(GeneralStrings.SongRequestNotFound);
                return;
            }

            await _azuraCast.RequestInternalSongAsync(baseUrl, apiKey, station, songData.Path);
            await _dbActions.CreateAzuraCastStationRequestAsync(context.Guild.Id, station, songData.SongId, isInternal: true);

            AzuraRequestRecord? request = await _azuraCast.GetRequestableSongAsync(baseUrl, apiKey, station, songData.SongId);
            if (request is not null)
            {
                DiscordEmbed embed = EmbedBuilder.BuildAzuraCastMusicSearchSongEmbed(request, isQueued: false, isPlayed: false);
                await context.EditResponseAsync("You're sneaky! But I slid in the song quietly.", embed);
                return;
            }

            await context.EditResponseAsync($"You're sneaky! I slid in the song **{songData.Title}** by **{songData.Artist}**.");
        }

        [Command("delete-song-request"), Description("Delete a song request from the selected station."), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationDJGroup, AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask DeleteSongRequestAsync
        (
            SlashCommandContext context,
            [Description("The station of which you want to delete the song request."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("The request id of the song you want to delete."), SlashAutoCompleteProvider<AzuraCastRequestAutocomplete>] int requestId = 0
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(DeleteSongRequestAsync), context.User.GlobalName);

            AzuraCastEntity? ac = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadStations: true);
            if (ac is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = ac.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, ac.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string apiKey = (!string.IsNullOrEmpty(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(ac.AdminApiKey);
            string baseUrl = Crypto.Decrypt(ac.BaseUrl);

            await _azuraCast.DeleteStationSongRequestAsync(new(baseUrl), apiKey, station, requestId);

            if (requestId is 0)
            {
                await context.EditResponseAsync("I deleted all song requests.");
                return;
            }

            await context.EditResponseAsync($"I deleted the song request with the id **{requestId}**.");
        }

        [Command("skip-song"), Description("Skips the current song of the selected station."), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationDJGroup, AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask SkipSongAsync
        (
            SlashCommandContext context,
            [Description("The station of which you want to skip the song."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(SkipSongAsync), context.User.GlobalName);

            AzuraCastEntity? ac = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadStations: true);
            if (ac is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = ac.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, ac.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            if (acStation.LastSkipTime.AddSeconds(30) > DateTimeOffset.UtcNow)
            {
                await context.EditResponseAsync(GeneralStrings.SkipToFast);
                return;
            }

            string apiKey = (!string.IsNullOrEmpty(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(ac.AdminApiKey);
            string baseUrl = Crypto.Decrypt(ac.BaseUrl);

            AzuraNowPlayingDataRecord? nowPlaying;
            try
            {
                nowPlaying = await _azuraCast.GetNowPlayingAsync(new(baseUrl), station);
                if (nowPlaying is null)
                    throw new HttpRequestException("NowPlaying is null");
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
            {
                await context.EditResponseAsync(GeneralStrings.StationOffline);
                return;
            }

            await _azuraCast.SkipSongAsync(new(baseUrl), apiKey, station);

            await _dbActions.UpdateAzuraCastStationAsync(context.Guild.Id, station, null, null, true);

            await context.EditResponseAsync($"I skipped **{nowPlaying.NowPlaying.Song.Title}** by **{nowPlaying.NowPlaying.Song.Artist}**.");
        }

        [Command("switch-playlist"), Description("Switch the current playlist of the selected station."), AzuraCastDiscordPermCheck([AzuraCastDiscordPerm.StationDJGroup, AzuraCastDiscordPerm.StationAdminGroup, AzuraCastDiscordPerm.InstanceAdminGroup])]
        public async ValueTask SwitchPlaylistAsync
        (
            SlashCommandContext context,
            [Description("The station of which you want to switch the playlist."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("The playlist you want to switch to."), SlashAutoCompleteProvider<AzuraCastPlaylistAutocomplete>] int playlistId,
            [Description("Choose if you want to disable all other active playlists from the station. Defaults to Yes."), SlashChoiceProvider<BooleanYesNoStateProvider>] int removeOld = 0
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(SwitchPlaylistAsync), context.User.GlobalName);

            AzuraCastEntity? ac = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadStations: true);
            if (ac is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = ac.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, ac.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string apiKey = (!string.IsNullOrEmpty(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(ac.AdminApiKey);
            string baseUrl = Crypto.Decrypt(ac.BaseUrl);
            AzuraStationRecord? azuraStation = await _azuraCast.GetStationAsync(new(baseUrl), station);
            if (azuraStation is null)
            {
                await context.EditResponseAsync(GeneralStrings.PermissionIssue);
                await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"I don't have the permission to access the **station** endpoint on station {station}.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }

            List<AzuraPlaylistStateRecord>? states = await _azuraCast.SwitchPlaylistsAsync(new(baseUrl), apiKey, station, playlistId, removeOld is 1);
            if (states is null)
            {
                await context.EditResponseAsync(GeneralStrings.PermissionIssue);
                await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"I don't have the permission to access the **playlists** endpoint on station {station}.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }

            StringBuilder message = new();
            message.AppendLine(CultureInfo.InvariantCulture, $"I switched the {((states.Count is 1) ? "playlist" : "playlists")} for **{azuraStation.Name}**.");
            foreach (AzuraPlaylistStateRecord state in states)
            {
                message.AppendLine(CultureInfo.InvariantCulture, $"**{state.PlaylistName}** is now **{Misc.GetReadableBool(state.PlaylistState, ReadableBool.EnabledDisabled, true)}**.");
            }

            await context.EditResponseAsync(message.ToString());
        }
    }

    [Command("music"), RequireGuild, ModuleActivatedCheck([AzzyModules.LegalTerms, AzzyModules.AzuraCast]), AzuraCastOnlineCheck]
    public sealed class MusicGroup(ILogger<MusicGroup> logger, AzuraCastApiService azuraCast, CronJobManager cronJobManager, DbActions dbActions, DiscordBotService botService, WebRequestService webRequest)
    {
        private readonly ILogger<MusicGroup> _logger = logger;
        private readonly AzuraCastApiService _azuraCast = azuraCast;
        private readonly CronJobManager _cronJobManager = cronJobManager;
        private readonly DbActions _dbActions = dbActions;
        private readonly DiscordBotService _botService = botService;
        private readonly WebRequestService _webRequest = webRequest;

        [Command("get-song-history"), Description("Get the song history of the selected station.")]
        public async ValueTask GetSongHistoryAsync
        (
            SlashCommandContext context,
            [Description("The station of which you want to see the song history."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("The date of which you want to see the song history in the format YYYY-MM-DD.")] string? date = null
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(GetSongHistoryAsync), context.User.GlobalName);

            DateTimeOffset dateTime;
            if (date is null)
            {
                dateTime = DateTime.Today;
            }
            else if (!DateTimeOffset.TryParse(date, CultureInfo.CurrentCulture, out dateTime))
            {
                await context.EditResponseAsync(GeneralStrings.DateFormatInvalid);
                return;
            }

            AzuraCastEntity? ac = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadStations: true);
            if (ac is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = ac.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, ac.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string baseUrl = Crypto.Decrypt(ac.BaseUrl);
            string apiKey = (!string.IsNullOrEmpty(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(ac.AdminApiKey);
            string dateString = dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string dateStringFile = dateTime.ToString("yyyy-MM-dd_HH-mm-ss-fffffff", CultureInfo.InvariantCulture);

            IEnumerable<AzuraStationHistoryItemRecord>? history = await _azuraCast.GetStationHistoryAsync(new(baseUrl), apiKey, station, dateTime, dateTime.AddDays(1));
            if (history is null)
            {
                await context.EditResponseAsync(GeneralStrings.PermissionIssue);
                await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"I don't have the permission to access the **reports** endpoint on station {station}.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }

            if (!history.Any())
            {
                await context.EditResponseAsync($"There is no song history for **{dateString}**.");
                return;
            }

            AzuraStationRecord? azuraStation = await _azuraCast.GetStationAsync(new(baseUrl), station);
            if (azuraStation is null)
            {
                await context.EditResponseAsync(GeneralStrings.PermissionIssue);
                await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"I don't have the permission to access the **station** endpoint on station {station}.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }

            IEnumerable<AzuraStationHistoryExportRecord> exportHistory = [.. history.Select(h => new AzuraStationHistoryExportRecord() { Date = dateString, PlayedAt = Converter.ConvertFromUnixTime(h.PlayedAt), Song = h.Song, SongRequest = h.IsRequest, Streamer = h.Streamer, Playlist = h.Playlist }).Reverse()];
            string fileName = $"{ac.GuildId}-{ac.Id}-{acStation.Id}-{acStation.StationId}_SongHistory_{dateStringFile}.csv";
            string filePath = await FileOperations.CreateCsvFileAsync(exportHistory, fileName);
            await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            await using DiscordMessageBuilder builder = new();
            builder.WithContent($"Here is the song history for station **{azuraStation.Name}** on **{dateString}**.");
            builder.AddFile(fileName, fileStream, AddFileOptions.CloseStream);
            await context.EditResponseAsync(builder);

            FileOperations.DeleteFile(filePath);
        }

        [Command("get-songs-in-playlist"), Description("Get all songs in the selected playlist.")]
        public async ValueTask GetSongsInPlaylistAsync
        (
            SlashCommandContext context,
            [Description("The station of which you want to see the songs in the playlist."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("The playlist you want to see the songs from."), SlashAutoCompleteProvider<AzuraCastPlaylistAutocomplete>] int playlistId
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(GetSongsInPlaylistAsync), context.User.GlobalName);

            AzuraCastEntity? ac = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadStations: true);
            if (ac is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = ac.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, ac.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string baseUrl = Crypto.Decrypt(ac.BaseUrl);
            string apiKey = (!string.IsNullOrEmpty(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(ac.AdminApiKey);

            AzuraPlaylistRecord? playlist;
            try
            {
                playlist = await _azuraCast.GetPlaylistAsync(new(baseUrl), apiKey, station, playlistId);
                if (playlist is null)
                {
                    await context.EditResponseAsync(GeneralStrings.PermissionIssue);
                    await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"I don't have the permission to access the **playlists** endpoint on station {station}.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                    return;
                }
            }
            catch (HttpRequestException)
            {
                await context.EditResponseAsync(GeneralStrings.PlaylistNotFound);
                return;
            }

            IEnumerable<AzuraMediaItemRecord>? songs = await _azuraCast.GetSongsInPlaylistAsync(new(baseUrl), apiKey, station, playlist);
            if (songs is null)
            {
                await context.EditResponseAsync(GeneralStrings.PermissionIssue);
                await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"I don't have the permission to access the **playlists** endpoint on station {station}.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }

            if (!songs.Any())
            {
                await context.EditResponseAsync(GeneralStrings.PlaylistEmpty);
                return;
            }

            string fileName = $"{ac.GuildId}-{ac.Id}-{acStation.Id}-{acStation.StationId}_PlaylistSongs_{playlist.ShortName}.csv";
            string filePath = await FileOperations.CreateCsvFileAsync(songs, fileName);
            await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            await using DiscordMessageBuilder builder = new();
            builder.WithContent($"Here are the songs in playlist **{playlist.Name}**.");
            builder.AddFile(fileName, fileStream, AddFileOptions.CloseStream);
            await context.EditResponseAsync(builder);

            FileOperations.DeleteFile(filePath);
        }

        [Command("now-playing"), Description("Get the currently playing song on the selected station.")]
        public async ValueTask GetNowPlayingAsync
        (
            SlashCommandContext context,
            [Description("The station of which you want to see what's played."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(GetNowPlayingAsync), context.User.GlobalName);

            AzuraCastEntity? ac = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadStations: true, loadStationPrefs: true);
            if (ac is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = ac.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, ac.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string baseUrl = Crypto.Decrypt(ac.BaseUrl);

            AzuraNowPlayingDataRecord? nowPlaying;
            try
            {
                nowPlaying = await _azuraCast.GetNowPlayingAsync(new(baseUrl), station);
                if (nowPlaying is null)
                    throw new HttpRequestException("NowPlaying is null");
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
            {
                await context.EditResponseAsync(GeneralStrings.StationOffline);
                return;
            }

            string? playlistName = null;
            if (acStation.Preferences.ShowPlaylistInNowPlaying)
            {
                string apiKey = (!string.IsNullOrEmpty(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(ac.AdminApiKey);
                IEnumerable<AzuraPlaylistRecord>? playlist = await _azuraCast.GetPlaylistsAsync(new(baseUrl), apiKey, station);
                if (playlist is null)
                {
                    await context.EditResponseAsync(GeneralStrings.PermissionIssue);
                    await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"I don't have the permission to access the **playlists** endpoint on station {station}.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                    return;
                }

                playlistName = playlist.Where(p => p.Name == nowPlaying.NowPlaying.Playlist).Select(static p => p.Name).FirstOrDefault();
            }

            DiscordEmbed embed = EmbedBuilder.BuildAzuraCastMusicNowPlayingEmbed(nowPlaying, playlistName);

            await context.EditResponseAsync(embed);
        }

        [Command("search-song"), Description("Search for a song on the selected station."), AzuraCastDiscordChannelCheck]
        public async ValueTask SearchSongAsync
        (
            SlashCommandContext context,
            [Description("The station of which you want to search for a song."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("The song you want to search for."), SlashAutoCompleteProvider<AzuraCastRequestAutocomplete>] string song
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(SearchSongAsync), context.User.GlobalName);

            AzuraCastEntity? ac = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadStations: true, loadStationChecks: true);
            if (ac is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = ac.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, ac.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string apiKey = (!string.IsNullOrEmpty(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(ac.AdminApiKey);
            Uri baseUrl = new(Crypto.Decrypt(ac.BaseUrl));

            AzuraAdminStationConfigRecord? stationConfig = await _azuraCast.GetStationAdminConfigAsync(baseUrl, apiKey, station);
            if (stationConfig is null)
            {
                await context.EditResponseAsync(GeneralStrings.PermissionIssue);
                await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"I don't have the permission to access the **administrative station** endpoint on station {station}.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }

            AzuraRequestRecord? songRequest;
            if (stationConfig.EnableRequests)
            {
                songRequest = await _azuraCast.GetRequestableSongAsync(baseUrl, apiKey, station, song);
                if (songRequest is null)
                {
                    await context.EditResponseAsync(GeneralStrings.SongRequestNotFound);
                    return;
                }
            }
            else
            {
                // First we check if the instance is online
                // If not we check if the station allows file changes
                // If not we return that the we can't search
                AzuraSongDataRecord? songData = null;
                if (ac.IsOnline)
                {
                    songData = await _azuraCast.GetSongInfoAsync(baseUrl, apiKey, acStation, online: true, songId: song);
                }
                else if (acStation.Checks.FileChanges)
                {
                    songData = await _azuraCast.GetSongInfoAsync(baseUrl, apiKey, acStation, online: false, songId: song);
                }
                else
                {
                    await context.EditResponseAsync(GeneralStrings.SongRequestOffline);
                    return;
                }

                if (songData is null)
                {
                    await context.EditResponseAsync(GeneralStrings.SongRequestNotFound);
                    return;
                }

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
                IEnumerable<AzuraRequestQueueItemRecord>? requestsPlayed = await _azuraCast.GetStationRequestItemsAsync(baseUrl, apiKey, station, true);
                if (requestsPlayed is null)
                {
                    await context.EditResponseAsync(GeneralStrings.PermissionIssue);
                    await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"I don't have the permission to access the **reports** endpoint on station {station}.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                    return;
                }

                long threshold = Converter.ConvertToUnixTime(DateTimeOffset.UtcNow.AddMinutes(-stationConfig.RequestThreshold));
                isPlayed = requestsPlayed.Any(r => (r.Track.SongId == songRequest.Song.SongId || r.Track.UniqueId == songRequest.Song.UniqueId) && Converter.ConvertToUnixTime(r.Timestamp) >= threshold);
            }

            IEnumerable<AzuraStationQueueItemDetailedRecord>? stationQueue = await _azuraCast.GetStationQueueAsync(baseUrl, apiKey, station);
            IEnumerable<AzuraRequestQueueItemRecord>? requestsPending = await _azuraCast.GetStationRequestItemsAsync(baseUrl, apiKey, station, false);
            if (stationQueue is null || requestsPending is null)
            {
                await context.EditResponseAsync(GeneralStrings.PermissionIssue);
                await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"I don't have the permission to access the **queue** endpoint on station {station}.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }

            isQueued = stationQueue.Any(q => q.Song.SongId == songRequest.Song.SongId && q.Song.UniqueId == songRequest.Song.UniqueId);
            isRequested = requestsPending.Any(r => r.Track.SongId == songRequest.Song.SongId && r.Track.UniqueId == songRequest.RequestId); // Need to use RequestId because those are the same... dunno why

            DiscordEmbed embed = EmbedBuilder.BuildAzuraCastMusicSearchSongEmbed(songRequest, isQueued || isRequested, isPlayed);
            if (!stationConfig.IsEnabled || !stationConfig.EnableRequests || isQueued || isRequested || isPlayed)
            {
                await context.EditResponseAsync(embed);
                return;
            }

            DiscordButtonComponent button = new(DiscordButtonStyle.Success, $"request_song_{context.User.Id}_{DateTimeOffset.Now:yyyy-MM-dd_HH-mm-ss-fffffff}", "Request Song");
            await using DiscordMessageBuilder builder = new();
            builder.AddEmbed(embed);
            builder.AddActionRowComponent(button);

            DiscordMessage message = await context.EditResponseAsync(builder);
            InteractivityResult<ComponentInteractionCreatedEventArgs> result = await message.WaitForButtonAsync(context.User, TimeSpan.FromMinutes(1));
            if (!result.TimedOut)
            {
                AzuraCustomQueueItemRecord record = new(context.Guild.Id, baseUrl, station, songRequest.RequestId, songRequest.Song.SongId, DateTimeOffset.UtcNow);
                _cronJobManager.RunAzuraRequestJob(record);

                await using DiscordInteractionResponseBuilder interaction = new()
                {
                    Content = GeneralStrings.SongRequestQueued
                };
                await result.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, interaction);
                await context.EditResponseAsync(embed);

                return;
            }

            await context.EditResponseAsync(embed);
        }

        [Command("upload-files"), Description("Upload a file to the selected station."), RequirePermissions(botPermissions: [], userPermissions: [DiscordPermission.AttachFiles]), FeatureAvailableCheck(AzuraCastFeatures.FileUploading), AzuraCastDiscordChannelCheck]
        public async ValueTask UploadFilesAsync
        (
            SlashCommandContext context,
            [Description("The station you want to upload the file to."), SlashAutoCompleteProvider<AzuraCastStationsAutocomplete>] int station,
            [Description("The file you want to upload.")] DiscordAttachment file
        )
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(context.Guild);

            _logger.CommandRequested(nameof(UploadFilesAsync), context.User.GlobalName);

            if (file is null || string.IsNullOrWhiteSpace(file.FileName) || string.IsNullOrWhiteSpace(file.Url))
            {
                await context.EditResponseAsync(GeneralStrings.FileNotFound);
                return;
            }
            else if (file.FileSize > FileSizes.AzuraFileSize)
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

            AzuraCastEntity? ac = await _dbActions.ReadAzuraCastAsync(context.Guild.Id, loadStations: true, loadStationPrefs: true);
            if (ac is null)
            {
                _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
                await context.EditResponseAsync(GeneralStrings.InstanceNotFound);
                return;
            }

            AzuraCastStationEntity? acStation = ac.Stations.FirstOrDefault(s => s.StationId == station);
            if (acStation is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(context.Guild.Id, ac.Id, station);
                await context.EditResponseAsync(GeneralStrings.StationNotFound);
                return;
            }

            string apiKey = (!string.IsNullOrEmpty(acStation.ApiKey)) ? Crypto.Decrypt(acStation.ApiKey) : Crypto.Decrypt(ac.AdminApiKey);
            string baseUrl = Crypto.Decrypt(ac.BaseUrl);

            string filePath = Path.Combine(Path.GetTempPath(), $"{DateTimeOffset.Now:yyyy-MM-dd_HH-mm-ss-fffffff}_{ac.GuildId}-{ac.Id}-{acStation.Id}_{file.FileName}");
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

            string uploadPath = (string.IsNullOrEmpty(acStation.Preferences.FileUploadPath)) ? "/" : acStation.Preferences.FileUploadPath;

            AzuraFilesDetailedRecord? uploadedFile = await _azuraCast.UploadFileAsync<AzuraFilesDetailedRecord>(new(baseUrl), apiKey, station, filePath, file.FileName, uploadPath);
            AzuraStationRecord? azuraStation = await _azuraCast.GetStationAsync(new(baseUrl), station);
            if (uploadedFile is null || azuraStation is null)
            {
                await context.EditResponseAsync(GeneralStrings.PermissionIssue);
                await _botService.SendMessageAsync(ac.Preferences.NotificationChannelId, $"I don't have the permission to access the **files** endpoint on station {station}.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }

            string artUrl = $"{baseUrl}/api/{AzuraApiEndpoints.Station}/{acStation.StationId}/{AzuraApiEndpoints.Art}/{uploadedFile.UniqueId}";
            DiscordEmbed embed = EmbedBuilder.BuildAzuraCastUploadFileEmbed(uploadedFile, file.FileSize, azuraStation.Name, artUrl);

            await context.EditResponseAsync(embed);

            FileOperations.DeleteFile(filePath);
        }
    }
}
