using AzzyBot.Settings;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

internal sealed class CoreService
{
    private readonly ILogger<CoreService> _logger;
    private readonly AzzyBotSettings _settings;

    public CoreService(AzzyBotSettings settings, ILogger<CoreService> logger)
    {
        _settings = settings;
        _logger = logger;
    }
}
