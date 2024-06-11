using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Utilities.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

public sealed class AzzyBackgroundService(IHostApplicationLifetime applicationLifetime, ILogger<AzzyBackgroundService> logger, AzuraCastFileService azuraCastFileService, AzuraCastPingService azuraCastPingService, AzuraCastUpdateService updaterService)
{
    private readonly ILogger<AzzyBackgroundService> _logger = logger;
    private readonly AzuraCastFileService _fileService = azuraCastFileService;
    private readonly AzuraCastPingService _pingService = azuraCastPingService;
    private readonly AzuraCastUpdateService _updaterService = updaterService;
    private readonly CancellationToken _cancellationToken = applicationLifetime.ApplicationStopping;

    public async Task StartAzuraCastBackgroundServiceAsync(AzuraCastChecks checks)
    {
        _logger.BackgroundServiceStart();

        if (_cancellationToken.IsCancellationRequested)
            return;

        switch (checks)
        {
            case AzuraCastChecks.CheckForFileChanges:
                await _fileService.QueueFileChangesChecksAsync();
                break;

            case AzuraCastChecks.CheckForOnlineStatus:
                await _pingService.QueueStationPingAsync();
                break;

            case AzuraCastChecks.CheckForUpdates:
                await _updaterService.QueueAzuraCastUpdatesAsync();
                break;
        }
    }
}
