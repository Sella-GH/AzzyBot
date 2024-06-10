using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Utilities.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

public sealed class AzuraCastBackgroundService(IHostApplicationLifetime applicationLifetime, ILogger<AzuraCastBackgroundService> logger, AzuraCastFileService azuraCastFileService)
{
    private readonly ILogger<AzuraCastBackgroundService> _logger = logger;
    private readonly AzuraCastFileService _azuraCastFileService = azuraCastFileService;
    private readonly CancellationToken _cancellationToken = applicationLifetime.ApplicationStopping;

    public async Task StartAzuraCastBackgroundServiceAsync(AzuraCastChecks checks)
    {
        _logger.AzuraCastBackgroundServiceStart();

        if (_cancellationToken.IsCancellationRequested)
            return;

        switch (checks)
        {
            case AzuraCastChecks.CheckForUpdates:
                break;

            case AzuraCastChecks.CheckForFileChanges:
                await _azuraCastFileService.QueueFileChangesChecksAsync();
                break;
        }
    }
}