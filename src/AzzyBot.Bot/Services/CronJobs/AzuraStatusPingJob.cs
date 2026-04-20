using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Bot.Services.Modules;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using Microsoft.Extensions.Logging;

using NCronJob;

namespace AzzyBot.Bot.Services.CronJobs;

public sealed class AzuraStatusPingJob(ILogger<AzuraStatusPingJob> logger, AzuraCastPingService pingService, DbActions dbActions, DiscordBotService botService) : IJob
{
    private readonly ILogger<AzuraStatusPingJob> _logger = logger;
    private readonly AzuraCastPingService _pingService = pingService;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;

    public async Task RunAsync(IJobExecutionContext context, CancellationToken token)
    {
        try
        {
            IReadOnlyList<AzuraCastEntity> azuraCasts = await _dbActions.ReadAzuraCastsAsync(loadChecks: true, loadPrefs: true, loadGuild: true);
            if (!azuraCasts.Any())
                return;

            foreach (AzuraCastEntity azuraCast in azuraCasts.Where(a => a.Checks.ServerStatus))
            {
                try
                {
                    await _pingService.PingInstanceAsync(azuraCast);
                }
                catch (Exception ex) when (IsExpectedStatusException(ex))
                {
                    if (Uri.TryCreate(Crypto.Decrypt(azuraCast.BaseUrl), UriKind.Absolute, out Uri? instanceUri))
                    {
                        _logger.WebRequestExpectedFailure(HttpMethod.Get, instanceUri, ex.Message);
                    }
                    else
                    {
                        _logger.BackgroundServiceInstanceStatus(azuraCast.GuildId, azuraCast.Id, "invalid because of malformed url");
                    }

                    await _dbActions.UpdateAzuraCastChecksAsync(azuraCast.Guild.UniqueId, lastServerStatusCheck: true);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException or TaskCanceledException)
        {
            await _botService.LogExceptionAsync(ex, DateTimeOffset.Now);
        }
    }

    private static bool IsExpectedStatusException(Exception ex)
    {
        if (ex is TaskCanceledException)
            return true;

        return ex is HttpRequestException requestException &&
            requestException.StatusCode is not null &&
            IsServerDownStatus(requestException.StatusCode.Value);
    }

    private static bool IsServerDownStatus(HttpStatusCode status)
    {
        return (int)status switch
        {
            502 or
            503 or
            504 or
            520 or
            521 or
            522 or
            523 or
            524 or
            525 or
            526 or
            530 => true,
            _ => false
        };
    }
}
