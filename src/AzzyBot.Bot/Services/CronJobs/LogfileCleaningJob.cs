using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Core.Logging;

using Microsoft.Extensions.Logging;

using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class LogfileCleaningJob(ILogger<LogfileCleaningJob> logger, DiscordBotService botService) : IJob
{
    private readonly ILogger<LogfileCleaningJob> _logger = logger;
    private readonly DiscordBotService _botService = botService;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogfileCleanupStart();

        try
        {
            if (!Directory.Exists("Logs"))
                Directory.CreateDirectory("Logs");

            if (context.Parameter is not int logDays)
                throw new InvalidOperationException($"{nameof(LogfileCleaningJob)} requires a parameter of type int. context.Parameter is {context.Parameter!.GetType()}");

            List<string> files = [.. Directory.EnumerateFiles("Logs").Where(f => f.Contains("AzzyBot_", StringComparison.OrdinalIgnoreCase) && DateTimeOffset.UtcNow - File.GetLastWriteTimeUtc(f) > TimeSpan.FromDays(logDays))];
            foreach (string file in files)
            {
                File.Delete(file);
            }

            _logger.LogfileCleanupComplete(files.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException or TaskCanceledException)
        {
            await _botService.LogExceptionAsync(ex, DateTimeOffset.Now);
        }
    }
}
