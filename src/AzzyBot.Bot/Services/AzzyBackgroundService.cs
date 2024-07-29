using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Utilities.Enums;
using AzzyBot.Core.Extensions;
using AzzyBot.Core.Logging;
using AzzyBot.Data.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services;

public sealed class AzzyBackgroundService(IHostApplicationLifetime applicationLifetime, ILogger<AzzyBackgroundService> logger, AzuraCastApiService azuraCastApiService, AzuraCastFileService azuraCastFileService, AzuraCastPingService azuraCastPingService, AzuraCastUpdateService updaterService)
{
    private readonly ILogger<AzzyBackgroundService> _logger = logger;
    private readonly AzuraCastApiService _apiService = azuraCastApiService;
    private readonly AzuraCastFileService _fileService = azuraCastFileService;
    private readonly AzuraCastPingService _pingService = azuraCastPingService;
    private readonly AzuraCastUpdateService _updaterService = updaterService;
    private readonly CancellationToken _cancellationToken = applicationLifetime.ApplicationStopping;

    public async Task StartAzuraCastBackgroundServiceAsync(AzuraCastChecks checks, IAsyncEnumerable<GuildEntity> guilds, int stationId = 0)
    {
        ArgumentNullException.ThrowIfNull(guilds, nameof(guilds));

        _logger.BackgroundServiceStart();

        if (_cancellationToken.IsCancellationRequested)
            return;

        GuildEntity? guild = null;
        if (await guilds.ContainsOneItemAsync())
        {
            await using IAsyncEnumerator<GuildEntity> enumerator = guilds.GetAsyncEnumerator();
            guild = (await enumerator.MoveNextAsync()) ? enumerator.Current : null;
        }

        switch (checks)
        {
            case AzuraCastChecks.CheckForApiPermissions:
                if (guild is null)
                {
                    await _apiService.QueueApiPermissionChecksAsync(guilds);
                }
                else
                {
                    _apiService.QueueApiPermissionChecks(guild, stationId);
                }

                break;

            case AzuraCastChecks.CheckForFileChanges:
                if (guild is null)
                {
                    await _fileService.QueueFileChangesChecksAsync(guilds);
                }
                else
                {
                    _fileService.QueueFileChangesChecks(guild, stationId);
                }

                break;

            case AzuraCastChecks.CheckForOnlineStatus:
                if (guild is null)
                {
                    await _pingService.QueueInstancePingAsync(guilds);
                }
                else
                {
                    _pingService.QueueInstancePing(guild);
                }

                break;

            case AzuraCastChecks.CheckForUpdates:
                if (guild is null)
                {
                    await _updaterService.QueueAzuraCastUpdatesAsync(guilds);
                }
                else
                {
                    _updaterService.QueueAzuraCastUpdates(guild);
                }

                break;
        }
    }
}
