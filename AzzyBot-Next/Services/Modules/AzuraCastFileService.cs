using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Logging;
using AzzyBot.Services.Interfaces;
using AzzyBot.Utilities.Encryption;
using AzzyBot.Utilities.Records.AzuraCast;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

public sealed class AzuraCastFileService(IHostApplicationLifetime applicationLifetime, ILogger<AzuraCastFileService> logger, IQueuedBackgroundTask taskQueue, AzuraCastApiService azuraCast, DbActions dbActions)
{
    private readonly ILogger<AzuraCastFileService> _logger = logger;
    private readonly IQueuedBackgroundTask _taskQueue = taskQueue;
    private readonly AzuraCastApiService _azuraCast = azuraCast;
    private readonly DbActions _dbActions = dbActions;
    private readonly CancellationToken _cancellationToken = applicationLifetime.ApplicationStopping;

    public void StartAzuraCastFileService()
    {
        _logger.AzuraCastFileServiceStart();

        Task.Run(async () => await BuildWorkItemAsync(_cancellationToken), _cancellationToken);
    }

    public async ValueTask BuildWorkItemAsync(CancellationToken cancellationToken)
    {
        _logger.AzuraCastFileServiceWorkItem();

        while (!cancellationToken.IsCancellationRequested)
        {
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


                    }
                }
            }
            catch (OperationCanceledException)
            { }
        }

        return;
    }
}
