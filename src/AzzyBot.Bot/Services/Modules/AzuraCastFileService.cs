using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Bot.Utilities;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.Modules;

public sealed class AzuraCastFileService(ILogger<AzuraCastFileService> logger, AzuraCastApiService azuraCast, DbActions dbActions, DiscordBotService discordBotService)
{
    private readonly ILogger<AzuraCastFileService> _logger = logger;
    private readonly AzuraCastApiService _azuraCast = azuraCast;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = discordBotService;

    public async Task CheckForFileChangesAsync(AzuraCastStationEntity station, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(station);

        if (!Directory.Exists(_azuraCast.FilePath))
            Directory.CreateDirectory(_azuraCast.FilePath);

        try
        {
            string baseUrl = Crypto.Decrypt(station.AzuraCast.BaseUrl);
            string apiKey = (string.IsNullOrWhiteSpace(station.ApiKey)) ? station.AzuraCast.AdminApiKey : station.ApiKey;

            IEnumerable<AzuraFilesRecord>? onlineFiles = await _azuraCast.GetFilesOnlineAsync<AzuraFilesRecord>(new(Crypto.Decrypt(station.AzuraCast.BaseUrl)), Crypto.Decrypt(apiKey), station.StationId);
            if (onlineFiles is null)
            {
                await _botService.SendMessageAsync(station.AzuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **files** endpoint on station ({station.StationId}).\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }

            IEnumerable<AzuraFilesRecord> localFiles = await _azuraCast.GetFilesLocalAsync(station.AzuraCast.GuildId, station.AzuraCastId, station.Id, station.StationId);
            AzuraStationRecord? azuraStation = await _azuraCast.GetStationAsync(new(baseUrl), station.StationId);
            if (azuraStation is null)
            {
                await _botService.SendMessageAsync(station.AzuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the **station** endpoint on station ({station.StationId}).\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return;
            }

            await CheckIfFilesWereModifiedAsync(onlineFiles, localFiles, station, azuraStation.Name, station.AzuraCast.Preferences.NotificationChannelId);
        }
        catch (OperationCanceledException)
        {
            _logger.OperationCanceled(nameof(CheckForFileChangesAsync));
        }
    }

    private async Task CheckIfFilesWereModifiedAsync(IEnumerable<AzuraFilesRecord> onlineFiles, IEnumerable<AzuraFilesRecord> localFiles, AzuraCastStationEntity station, string stationName, ulong channelId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stationName);

        HashSet<AzuraFilesRecord> onlineHashSet = new(onlineFiles, new AzuraFileComparer());
        HashSet<AzuraFilesRecord> localHashSet = new(localFiles, new AzuraFileComparer());

        List<AzuraFilesRecord> addedFiles = onlineHashSet.Except(localHashSet).ToList();
        List<AzuraFilesRecord> removedFiles = localHashSet.Except(onlineHashSet).ToList();
        bool filesChanged = addedFiles.Count is not 0 && removedFiles.Count is not 0;
        await _dbActions.UpdateAzuraCastStationChecksAsync(station.AzuraCast.Guild.UniqueId, station.StationId, lastFileChangesCheck: DateTime.UtcNow);
        if (!filesChanged)
            return;

        _logger.BackgroundServiceStationFilesChanged(station.AzuraCast.GuildId, station.AzuraCastId, station.Id, station.StationId);

        string addedFileName = Path.Combine(_azuraCast.FilePath, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fffffff}-{station.AzuraCast.GuildId}-{station.AzuraCastId}-{station.Id}-{station.StationId}-added.txt");
        string removedFileName = Path.Combine(_azuraCast.FilePath, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fffffff}-{station.AzuraCast.GuildId}-{station.AzuraCastId}-{station.Id}-{station.StationId}-removed.txt");
        StringBuilder added = new();
        StringBuilder removed = new();
        List<string> paths = new(addedFiles.Count + removedFiles.Count);

        if (addedFiles.Count is not 0)
        {
            foreach (AzuraFilesRecord record in addedFiles)
                added.AppendLine(record.Path);

            await FileOperations.WriteToFileAsync(addedFileName, added.ToString());
            paths.Add(addedFileName);
        }

        if (removedFiles.Count is not 0)
        {
            foreach (AzuraFilesRecord record in removedFiles)
                removed.AppendLine(record.Path);

            await FileOperations.WriteToFileAsync(removedFileName, removed.ToString());
            paths.Add(removedFileName);
        }

        await FileOperations.WriteToFileAsync(Path.Combine(_azuraCast.FilePath, $"{station.AzuraCast.GuildId}-{station.AzuraCastId}-{station.Id}-{station.StationId}-files.json"), JsonSerializer.Serialize(onlineFiles, FileOperations.JsonOptions));
        DiscordEmbed embed = EmbedBuilder.BuildAzuraCastFileChangesEmbed(stationName, addedFiles.Count, removedFiles.Count);
        await _botService.SendMessageAsync(channelId, $"Changes in the files of station **{stationName}** detected. Check the details below.", [embed], paths);
    }
}
