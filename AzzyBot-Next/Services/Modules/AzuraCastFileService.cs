using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

public sealed class AzuraCastFileService(IHostApplicationLifetime applicationLifetime, ILogger<AzuraCastFileService> logger, IQueuedBackgroundTask taskQueue, AzuraCastApiService azuraCast, DbActions dbActions, DiscordBotService discordBotService)
{
    private readonly ILogger<AzuraCastFileService> _logger = logger;
    private readonly IQueuedBackgroundTask _taskQueue = taskQueue;
    private readonly AzuraCastApiService _azuraCast = azuraCast;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = discordBotService;
    private readonly CancellationToken _cancellationToken = applicationLifetime.ApplicationStopping;

    public void StartAzuraCastFileService()
    {
        _logger.AzuraCastFileServiceStart();

        if (_cancellationToken.IsCancellationRequested)
            return;

        Task.Run(async () => await _taskQueue.QueueBackgroundWorkItemAsync(BuildWorkItemAsync));
    }

    private async ValueTask BuildWorkItemAsync(CancellationToken cancellationToken)
    {
        _logger.AzuraCastFileServiceWorkItem();

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            foreach (GuildsEntity guild in await _dbActions.GetGuildsAsync())
            {
                if (guild.AzuraCast is null)
                    continue;

                AzuraCastEntity azuraCast = guild.AzuraCast;
                foreach (AzuraCastStationEntity station in azuraCast.Stations)
                {
                    IReadOnlyList<FilesRecord> onlineFiles = await _azuraCast.GetFilesOnlineAsync(new(Crypto.Decrypt(azuraCast.BaseUrl)), Crypto.Decrypt(station.ApiKey), station.StationId);
                    IReadOnlyList<FilesRecord> localFiles = await _azuraCast.GetFilesLocalAsync(station.Id, station.StationId);

                    await CheckIfFilesWereModifiedAsync(onlineFiles, localFiles, station.Id, station.StationId, station.Name, azuraCast.NotificatioChannelId);
                }
            }
        }
        catch (OperationCanceledException)
        { }

        return;
    }

    private async ValueTask CheckIfFilesWereModifiedAsync(IReadOnlyList<FilesRecord> onlineFiles, IReadOnlyList<FilesRecord> localFiles, int stationDbId, int stationId, string stationName, ulong channelId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationDbId, nameof(stationDbId));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stationId, nameof(stationId));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channelId, nameof(channelId));
        ArgumentException.ThrowIfNullOrWhiteSpace(stationName, nameof(stationName));

        HashSet<FilesRecord> onlineHashSet = new(onlineFiles, new FileComparer());
        HashSet<FilesRecord> localHashSet = new(localFiles, new FileComparer());

        List<FilesRecord> addedFiles = onlineHashSet.Except(localHashSet).ToList();
        List<FilesRecord> removedFiles = localHashSet.Except(onlineHashSet).ToList();

        if (addedFiles.Count == 0 && removedFiles.Count == 0)
            return;

        string addedFileName = Path.Combine(_azuraCast.FilePath, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}-{stationDbId}-{stationId}-added.txt");
        string removedFileName = Path.Combine(_azuraCast.FilePath, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}-{stationDbId}-{stationId}-removed.txt");

        StringBuilder added = new();
        StringBuilder removed = new();

        foreach (FilesRecord record in addedFiles)
            added.AppendLine(record.Path);

        foreach (FilesRecord record in removedFiles)
            removed.AppendLine(record.Path);

        await FileOperations.WriteToFileAsync(addedFileName, added.ToString());
        await FileOperations.WriteToFileAsync(removedFileName, removed.ToString());

        DiscordEmbed embed = EmbedBuilder.BuildAzuraCastFileChangesEmbed(stationName, addedFiles.Count, removedFiles.Count);
        await _botService.SendMessageAsync(channelId, $"Changes in the files of station {stationName} detected. Check the details below.", [embed], [addedFileName, removedFileName]);
    }
}
