using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Utilities.Enums;
using AzzyBot.Core.Logging;
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

    public async Task StartAzuraCastBackgroundServiceAsync(AzuraCastChecks checks, ulong guildId = 0, int stationId = 0)
    {
        _logger.BackgroundServiceStart();

        if (_cancellationToken.IsCancellationRequested)
            return;

        switch (checks)
        {
            case AzuraCastChecks.CheckForApiPermissions:
                if (guildId == 0)
                {
                    await _apiService.QueueApiPermissionChecksAsync();
                }
                else
                {
                    await _apiService.QueueApiPermissionChecksAsync(guildId, stationId);
                }

                break;

            case AzuraCastChecks.CheckForFileChanges:
                if (guildId == 0)
                {
                    await _fileService.QueueFileChangesChecksAsync();
                }
                else
                {
                    await _fileService.QueueFileChangesChecksAsync(guildId, stationId);
                }

                break;

            case AzuraCastChecks.CheckForOnlineStatus:
                if (guildId == 0)
                {
                    await _pingService.QueueInstancePingAsync();
                }
                else
                {
                    await _pingService.QueueInstancePingAsync(guildId);
                }

                break;

            case AzuraCastChecks.CheckForUpdates:
                if (guildId == 0)
                {
                    await _updaterService.QueueAzuraCastUpdatesAsync();
                }
                else
                {
                    await _updaterService.QueueAzuraCastUpdatesAsync(guildId);
                }

                break;
        }
    }
}
