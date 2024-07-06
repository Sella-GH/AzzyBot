using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Settings;
using AzzyBot.Utilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services.Modules;

public sealed class CoreServiceHost(ILogger<CoreServiceHost> logger, AzzyBotSettingsRecord settings) : IHostedService
{
    private readonly ILogger<CoreServiceHost> _logger = logger;
    private readonly AzzyBotSettingsRecord _settings = settings;
    private readonly Task _completed = Task.CompletedTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        string name = AzzyStatsSoftware.GetBotName;
        string version = AzzyStatsSoftware.GetBotVersion;
        string os = AzzyStatsHardware.GetSystemOs;
        string arch = AzzyStatsHardware.GetSystemOsArch;

        _logger.BotStarting(name, version, os, arch);

        LogfileCleaning();

        return _completed;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.BotStopping();

        return _completed;
    }

    private void LogfileCleaning()
    {
        _logger.LogfileCleaning();

        DateTime date = DateTime.Today.AddDays(-_settings.LogRetentionDays);
        string logPath = Path.Combine(Environment.CurrentDirectory, "Logs");
        int counter = 0;
        foreach (string logFile in Directory.GetFiles(logPath, "*.log").Where(f => File.GetLastWriteTime(f) < date))
        {
            File.Delete(logFile);
            counter++;
        }

        _logger.LogfileDeleted(counter);
    }
}
