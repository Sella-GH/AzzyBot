﻿using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Settings;
using AzzyBot.Utilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

internal sealed class CoreServiceHost : BaseService, IHostedService
{
    internal readonly bool _isActivated;
    private readonly ILogger<CoreServiceHost> _logger;
    private readonly AzzyBotSettings _settings;

    public CoreServiceHost(AzzyBotSettings settings, ILogger<CoreServiceHost> logger)
    {
        _settings = settings;
        _logger = logger;
        CheckSettings(_settings.DiscordStatus, _logger);

        _isActivated = true;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        string name = AzzyStatsGeneral.GetBotName;
        string version = AzzyStatsGeneral.GetBotVersion;
        string os = AzzyStatsGeneral.GetOperatingSystem;
        string arch = AzzyStatsGeneral.GetOsArchitecture;

        _logger.BotStarting(name, version, os, arch);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.BotStopping();

        return Task.CompletedTask;
    }
}