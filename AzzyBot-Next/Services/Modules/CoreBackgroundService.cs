using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Utilities.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

public sealed class CoreBackgroundService(IHostApplicationLifetime applicationLifetime, ILogger<CoreBackgroundService> logger, AzuraCastFileService azuraCastFileService)
{
    private readonly ILogger<CoreBackgroundService> _logger = logger;
    private readonly AzuraCastFileService _azuraCastFileService = azuraCastFileService;
    private readonly CancellationToken _cancellationToken = applicationLifetime.ApplicationStopping;

    public async Task StartAzuraCastBackgroundServiceAsync(AzuraCastChecks checks)
    {
        _logger.CoreBackgroundServiceStart();

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
                break;
        }
    }
}
