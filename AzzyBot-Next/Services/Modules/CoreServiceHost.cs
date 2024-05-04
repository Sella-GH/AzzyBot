using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

internal sealed class CoreServiceHost : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CoreServiceHost> _logger;

    public CoreServiceHost(IConfiguration config, ILogger<CoreServiceHost> logger)
    {
        _configuration = config;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
