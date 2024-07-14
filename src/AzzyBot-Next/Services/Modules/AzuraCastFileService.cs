using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Logging;
using AzzyBot.Services.Interfaces;
using AzzyBot.Utilities;
using AzzyBot.Utilities.Encryption;
using AzzyBot.Utilities.Records.AzuraCast;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

public sealed class AzuraCastFileService(ILogger<AzuraCastFileService> logger, IQueuedBackgroundTask taskQueue, AzuraCastApiService azuraCast, DbActions dbActions, DiscordBotService discordBotService)
{
    private readonly ILogger<AzuraCastFileService> _logger = logger;
    private readonly IQueuedBackgroundTask _taskQueue = taskQueue;
    private readonly AzuraCastApiService _azuraCast = azuraCast;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = discordBotService;

    public async ValueTask QueueFileChangesChecksAsync()
    {
        _logger.BackgroundServiceWorkItem(nameof(QueueFileChangesChecksAsync));

        IReadOnlyList<GuildsEntity> guilds = await _dbActions.GetGuildsAsync(true);
        foreach (AzuraCastEntity azuraCast in guilds.Where(g => g.AzuraCast?.IsOnline == true).Select(g => g.AzuraCast!))
        {
            foreach (AzuraCastStationEntity station in azuraCast.Stations.Where(s => s.Checks.FileChanges))
            {
                _ = Task.Run(async () => await _taskQueue.QueueBackgroundWorkItemAsync(async ct => await CheckForFileChangesAsync(station, ct)));
            }
        }
    }

    public async ValueTask QueueFileChangesChecksAsync(ulong guildId, int stationId = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(guildId, nameof(guildId));

        _logger.BackgroundServiceWorkItem(nameof(QueueFileChangesChecksAsync));

        GuildsEntity? guild = await _dbActions.GetGuildAsync(guildId, true);
        if (guild is null || guild.AzuraCast is null)
        {
            _logger.DatabaseItemNotFound($"{nameof(GuildsEntity)} and {nameof(AzuraCastEntity)}", guildId);
            return;
        }

        IEnumerable<AzuraCastStationEntity> stations = guild.AzuraCast.Stations.Where(s => s.Checks.FileChanges);
        if (stationId is not 0)
        {
            AzuraCastStationEntity? station = stations.FirstOrDefault(s => s.Id == stationId);
            if (station is null)
            {
                _logger.DatabaseItemNotFound(nameof(AzuraCastStationEntity), guildId);
                return;
            }

            _ = Task.Run(async () => await _taskQueue.QueueBackgroundWorkItemAsync(async ct => await CheckForFileChangesAsync(station, ct)));
        }
        else
        {
            foreach (AzuraCastStationEntity station in stations)
            {
                _ = Task.Run(async () => await _taskQueue.QueueBackgroundWorkItemAsync(async ct => await CheckForFileChangesAsync(station, ct)));
            }
        }
    }

    private async ValueTask CheckForFileChangesAsync(AzuraCastStationEntity station, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(_azuraCast.FilePath))
            Directory.CreateDirectory(_azuraCast.FilePath);

        try
        {
            string apiKey = (string.IsNullOrWhiteSpace(station.ApiKey)) ? station.AzuraCast.AdminApiKey : station.ApiKey;

            IReadOnlyList<AzuraFilesRecord> onlineFiles = await _azuraCast.GetFilesOnlineAsync(new(Crypto.Decrypt(station.AzuraCast.BaseUrl)), Crypto.Decrypt(apiKey), station.StationId);
            IReadOnlyList<AzuraFilesRecord> localFiles = await _azuraCast.GetFilesLocalAsync(station.Id, station.StationId);

            await CheckIfFilesWereModifiedAsync(onlineFiles, localFiles, station.Id, station.StationId, Crypto.Decrypt(station.Name), station.RequestsChannelId);
        }
        catch (OperationCanceledException)
        {
            _logger.OperationCanceled(nameof(CheckForFileChangesAsync));
        }
    }

    private async ValueTask CheckIfFilesWereModifiedAsync(IReadOnlyList<AzuraFilesRecord> onlineFiles, IReadOnlyList<AzuraFilesRecord> localFiles, int stationDbId, int stationId, string stationName, ulong channelId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationDbId, nameof(stationDbId));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channelId, nameof(channelId));
        ArgumentException.ThrowIfNullOrWhiteSpace(stationName, nameof(stationName));

        HashSet<AzuraFilesRecord> onlineHashSet = new(onlineFiles, new FileComparer());
        HashSet<AzuraFilesRecord> localHashSet = new(localFiles, new FileComparer());

        List<AzuraFilesRecord> addedFiles = onlineHashSet.Except(localHashSet).ToList();
        List<AzuraFilesRecord> removedFiles = localHashSet.Except(onlineHashSet).ToList();

        if (addedFiles.Count == 0 && removedFiles.Count == 0)
            return;

        _logger.BackgroundServiceStationFilesChanged(stationDbId, stationId);

        string addedFileName = Path.Combine(_azuraCast.FilePath, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}-{stationDbId}-{stationId}-added.txt");
        string removedFileName = Path.Combine(_azuraCast.FilePath, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}-{stationDbId}-{stationId}-removed.txt");
        StringBuilder added = new();
        StringBuilder removed = new();
        List<string> paths = [];

        if (addedFiles.Count > 0)
        {
            foreach (AzuraFilesRecord record in addedFiles)
                added.AppendLine(record.Path);

            await FileOperations.WriteToFileAsync(addedFileName, added.ToString());
            paths.Add(addedFileName);
        }

        if (removedFiles.Count > 0)
        {
            foreach (AzuraFilesRecord record in removedFiles)
                removed.AppendLine(record.Path);

            await FileOperations.WriteToFileAsync(removedFileName, removed.ToString());
            paths.Add(removedFileName);
        }

        DiscordEmbed embed = EmbedBuilder.BuildAzuraCastFileChangesEmbed(stationName, addedFiles.Count, removedFiles.Count);
        await _botService.SendMessageAsync(channelId, $"Changes in the files of station **{stationName}** detected. Check the details below.", [embed], paths);
        await FileOperations.WriteToFileAsync(Path.Combine(_azuraCast.FilePath, $"{stationDbId}-{stationId}-files.json"), JsonSerializer.Serialize(onlineFiles, FileOperations.JsonOptions));
    }
}
