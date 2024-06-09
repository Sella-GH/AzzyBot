using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

public sealed class AzuraCastFileService(IHostApplicationLifetime applicationLifetime, ILogger<AzuraCastFileService> logger, IQueuedBackgroundTask taskQueue, AzuraCastService azuraCast)
{
    private readonly ILogger<AzuraCastFileService> _logger = logger;
    private readonly IQueuedBackgroundTask _taskQueue = taskQueue;
    private readonly AzuraCastService _azuraCast = azuraCast;
    private readonly CancellationToken _cancellationToken = applicationLifetime.ApplicationStopping;

    public void StartAzuraCastFileService()
    {
        _logger.AzuraCastFileServiceStarted();

        return;
    }

    private async ValueTask BuildWorkItemAsync(CancellationToken cancellationToken)
    {
        _logger.AzuraCastFileServiceWorkItem();

        return;
    }
}
