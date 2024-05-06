using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

internal sealed class CoreService(IConfiguration config, ILogger<CoreService> logger)
{
    private readonly IConfiguration _configuration = config;
    private readonly ILogger<CoreService> _logger = logger;
}
