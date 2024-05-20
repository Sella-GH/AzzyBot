using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Utilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

public sealed class CoreServiceHost(ILogger<CoreServiceHost> logger) : IHostedService
{
    private readonly ILogger<CoreServiceHost> _logger = logger;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        string name = AzzyStatsSoftware.GetBotName;
        string version = AzzyStatsSoftware.GetBotVersion;
        string os = AzzyStatsHardware.GetSystemOs;
        string arch = AzzyStatsHardware.GetSystemOsArch;

        _logger.BotStarting(name, version, os, arch);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.BotStopping();

        return Task.CompletedTask;
    }
}
