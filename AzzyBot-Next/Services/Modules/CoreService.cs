using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

internal sealed class CoreService : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CoreService> _logger;

    public CoreService(IConfiguration config, ILogger<CoreService> logger)
    {
        _configuration = config;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
