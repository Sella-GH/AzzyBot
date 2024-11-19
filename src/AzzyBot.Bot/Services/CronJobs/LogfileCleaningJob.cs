using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Core.Logging;
using Microsoft.Extensions.Logging;
using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class LogfileCleaningJob(ILogger<LogfileCleaningJob> logger) : IJob
{
    private readonly ILogger<LogfileCleaningJob> _logger = logger;

    public Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogfileCleanupStart();

        if (!Directory.Exists("Logs"))
            Directory.CreateDirectory("Logs");

        int logDays = Convert.ToInt32(context.Parameter, CultureInfo.InvariantCulture);
        List<string> files = Directory.GetFiles("Logs").Where(f => f.StartsWith("AzzyBot_", StringComparison.InvariantCultureIgnoreCase) && DateTimeOffset.UtcNow - File.GetLastWriteTimeUtc(f) > TimeSpan.FromDays(logDays)).ToList();
        foreach (string file in files)
        {
            File.Delete(file);
        }

        _logger.LogfileCleanupComplete(files.Count);

        return Task.CompletedTask;
    }
}
