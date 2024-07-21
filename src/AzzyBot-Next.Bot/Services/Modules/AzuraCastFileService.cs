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
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.Modules;

public sealed class AzuraCastFileService(ILogger<AzuraCastFileService> logger, IQueuedBackgroundTask taskQueue, AzuraCastApiService azuraCast, DbActions dbActions, DiscordBotService discordBotService)
{
    private readonly ILogger<AzuraCastFileService> _logger = logger;
    private readonly IQueuedBackgroundTask _taskQueue = taskQueue;
    private readonly AzuraCastApiService _azuraCast = azuraCast;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = discordBotService;

    public async Task QueueFileChangesChecksAsync()
    {
        _logger.BackgroundServiceWorkItem(nameof(QueueFileChangesChecksAsync));

        IReadOnlyList<GuildEntity> guilds = await _dbActions.GetGuildsAsync(true);
        foreach (AzuraCastEntity azuraCast in guilds.Where(g => g.AzuraCast?.IsOnline == true).Select(g => g.AzuraCast!))
        {
            foreach (AzuraCastStationEntity station in azuraCast.Stations.Where(s => s.Checks.FileChanges))
            {
                _ = Task.Run(async () => await _taskQueue.QueueBackgroundWorkItemAsync(async ct => await CheckForFileChangesAsync(station, ct)));
            }
        }
    }

    public async Task QueueFileChangesChecksAsync(ulong guildId, int stationId = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(guildId, nameof(guildId));

        _logger.BackgroundServiceWorkItem(nameof(QueueFileChangesChecksAsync));

        GuildEntity? guild = await _dbActions.GetGuildAsync(guildId, true);
        if (guild is null || guild.AzuraCast is null)
        {
            _logger.DatabaseGuildNotFound(guildId);
            return;
        }

        IEnumerable<AzuraCastStationEntity> stations = guild.AzuraCast.Stations.Where(s => s.Checks.FileChanges);
        if (stationId is not 0)
        {
            AzuraCastStationEntity? station = stations.FirstOrDefault(s => s.StationId == stationId);
            if (station is null)
            {
                _logger.DatabaseAzuraCastStationNotFound(guildId, guild.AzuraCast.Id, stationId);
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
            string apiKey = (string.IsNullOrWhiteSpace(station.ApiKey)) ? station.AzuraCast.AdminApiKey : station.ApiKey;

            IReadOnlyList<AzuraFilesRecord> onlineFiles = await _azuraCast.GetFilesOnlineAsync(new(Crypto.Decrypt(station.AzuraCast.BaseUrl)), Crypto.Decrypt(apiKey), station.StationId);
            IReadOnlyList<AzuraFilesRecord> localFiles = await _azuraCast.GetFilesLocalAsync(station.AzuraCast.GuildId, station.AzuraCastId, station.Id, station.StationId);

            await CheckIfFilesWereModifiedAsync(onlineFiles, localFiles, station, Crypto.Decrypt(station.Name), station.Preferences.RequestsChannelId);
        }
        catch (OperationCanceledException)
        {
            _logger.OperationCanceled(nameof(CheckForFileChangesAsync));
        }
    }

    private async Task CheckIfFilesWereModifiedAsync(IReadOnlyList<AzuraFilesRecord> onlineFiles, IReadOnlyList<AzuraFilesRecord> localFiles, AzuraCastStationEntity station, string stationName, ulong channelId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channelId, nameof(channelId));
        ArgumentException.ThrowIfNullOrWhiteSpace(stationName, nameof(stationName));

        HashSet<AzuraFilesRecord> onlineHashSet = new(onlineFiles, new AzuraFileComparer());
        HashSet<AzuraFilesRecord> localHashSet = new(localFiles, new AzuraFileComparer());

        List<AzuraFilesRecord> addedFiles = onlineHashSet.Except(localHashSet).ToList();
        List<AzuraFilesRecord> removedFiles = localHashSet.Except(onlineHashSet).ToList();

        if (addedFiles.Count == 0 && removedFiles.Count == 0)
            return;

        _logger.BackgroundServiceStationFilesChanged(station.AzuraCastId, station.Id, station.StationId);

        string addedFileName = Path.Combine(_azuraCast.FilePath, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fffffff}-{station.AzuraCast.GuildId}-{station.AzuraCastId}-{station.Id}-{station.StationId}-added.txt");
        string removedFileName = Path.Combine(_azuraCast.FilePath, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fffffff}-{station.AzuraCast.GuildId}-{station.AzuraCastId}-{station.Id}-{station.StationId}-removed.txt");
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
        await FileOperations.WriteToFileAsync(Path.Combine(_azuraCast.FilePath, $"{station.AzuraCast.GuildId}-{station.AzuraCastId}-{station.Id}-{station.StationId}-files.json"), JsonSerializer.Serialize(onlineFiles, FileOperations.JsonOptions));
    }
}
