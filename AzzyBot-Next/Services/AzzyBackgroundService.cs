using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Utilities.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

public sealed class AzzyBackgroundService(IHostApplicationLifetime applicationLifetime, ILogger<AzzyBackgroundService> logger, AzuraCastFileService azuraCastFileService, AzuraCastUpdateService updaterService)
{
    private readonly ILogger<AzzyBackgroundService> _logger = logger;
    private readonly AzuraCastFileService _azuraCastFileService = azuraCastFileService;
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
                await _azuraCastFileService.QueueFileChangesChecksAsync();
                break;

            case AzuraCastChecks.CheckForOnlineStatus:
                break;

            case AzuraCastChecks.CheckForUpdates:
                await _updaterService.QueueAzuraCastUpdatesAsync();
                break;
        }
    }
}
