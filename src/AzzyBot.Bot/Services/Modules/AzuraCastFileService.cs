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
using AzzyBot.Core.Services.Interfaces;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data.Entities;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.Modules;

public sealed class AzuraCastFileService(ILogger<AzuraCastFileService> logger, IQueuedBackgroundTask taskQueue, AzuraCastApiService azuraCast, DiscordBotService discordBotService)
{
    private readonly ILogger<AzuraCastFileService> _logger = logger;
    private readonly IQueuedBackgroundTask _taskQueue = taskQueue;
    private readonly AzuraCastApiService _azuraCast = azuraCast;
    private readonly DiscordBotService _botService = discordBotService;

    public async Task QueueFileChangesChecksAsync(IAsyncEnumerable<GuildEntity> guilds)
    {
        ArgumentNullException.ThrowIfNull(guilds, nameof(guilds));

        _logger.BackgroundServiceWorkItem(nameof(QueueFileChangesChecks));

        await foreach (GuildEntity guild in guilds)
        {
            if (guild.AzuraCast?.IsOnline is true)
            {
                foreach (AzuraCastStationEntity station in guild.AzuraCast!.Stations.Where(s => s.Checks.FileChanges))
                {
                    _ = Task.Run(async () => await _taskQueue.QueueBackgroundWorkItemAsync(async ct => await CheckForFileChangesAsync(station, ct)));
                }
            }
        }
    }

    public void QueueFileChangesChecks(GuildEntity guild, int stationId = 0)
    {
        ArgumentNullException.ThrowIfNull(guild, nameof(guild));
        ArgumentNullException.ThrowIfNull(guild.AzuraCast, nameof(guild.AzuraCast));

        _logger.BackgroundServiceWorkItem(nameof(QueueFileChangesChecks));

        IEnumerable<AzuraCastStationEntity> stations = guild.AzuraCast.Stations.Where(s => s.Checks.FileChanges);
        if (stationId is not 0)
        {
            AzuraCastStationEntity? station = stations.FirstOrDefault(s => s.StationId == stationId);
            if (station is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(guild.UniqueId, guild.AzuraCast.Id, stationId);
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

    private async Task CheckForFileChangesAsync(AzuraCastStationEntity station, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(_azuraCast.FilePath))
            Directory.CreateDirectory(_azuraCast.FilePath);

        try
        {
            string baseUrl = Crypto.Decrypt(station.AzuraCast.BaseUrl);
            string apiKey = (string.IsNullOrWhiteSpace(station.ApiKey)) ? station.AzuraCast.AdminApiKey : station.ApiKey;

            IEnumerable<AzuraFilesRecord> onlineFiles = await _azuraCast.GetFilesOnlineAsync(new(Crypto.Decrypt(station.AzuraCast.BaseUrl)), Crypto.Decrypt(apiKey), station.StationId);
            IEnumerable<AzuraFilesRecord> localFiles = await _azuraCast.GetFilesLocalAsync(station.AzuraCast.GuildId, station.AzuraCastId, station.Id, station.StationId);
            AzuraStationRecord azuraStation = await _azuraCast.GetStationAsync(new(baseUrl), station.StationId);

            await CheckIfFilesWereModifiedAsync(onlineFiles, localFiles, station, azuraStation.Name, station.Preferences.RequestsChannelId);
        }
        catch (OperationCanceledException)
        {
            _logger.OperationCanceled(nameof(CheckForFileChangesAsync));
        }
    }

    private async Task CheckIfFilesWereModifiedAsync(IEnumerable<AzuraFilesRecord> onlineFiles, IEnumerable<AzuraFilesRecord> localFiles, AzuraCastStationEntity station, string stationName, ulong channelId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channelId, nameof(channelId));
        ArgumentException.ThrowIfNullOrWhiteSpace(stationName, nameof(stationName));

        HashSet<AzuraFilesRecord> onlineHashSet = new(onlineFiles, new AzuraFileComparer());
        HashSet<AzuraFilesRecord> localHashSet = new(localFiles, new AzuraFileComparer());

        IReadOnlyList<AzuraFilesRecord> addedFiles = onlineHashSet.Except(localHashSet).ToList();
        IReadOnlyList<AzuraFilesRecord> removedFiles = localHashSet.Except(onlineHashSet).ToList();
        if (addedFiles.Count is 0 && removedFiles.Count is 0)
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

        DiscordEmbed embed = EmbedBuilder.BuildAzuraCastFileChangesEmbed(stationName, addedFiles.Count, removedFiles.Count);
        await _botService.SendMessageAsync(channelId, $"Changes in the files of station **{stationName}** detected. Check the details below.", [embed], paths);
        await FileOperations.WriteToFileAsync(Path.Combine(_azuraCast.FilePath, $"{station.AzuraCast.GuildId}-{station.AzuraCastId}-{station.Id}-{station.StationId}-files.json"), JsonSerializer.Serialize(onlineFiles, FileOperations.JsonOptions));
    }
}
