using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

internal sealed class CoreService : BaseService, IHostedService
{
    internal readonly bool IsActivated;
    private readonly ILogger<CoreService> _logger;
    private readonly AzzyBotSettings _settings;

    public CoreService(AzzyBotSettings settings, ILogger<CoreService> logger)
    {
        _settings = settings;
        _logger = logger;
        CheckSettings(_settings.DiscordStatus, _logger);

        IsActivated = true;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
