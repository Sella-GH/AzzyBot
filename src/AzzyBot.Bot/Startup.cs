using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Bot.Extensions;
using AzzyBot.Core.Extensions;
using AzzyBot.Core.Utilities;
using Microsoft.Extensions.Hosting;

namespace AzzyBot.Bot;

public static class Startup
{
    public static async Task Main(string[] args)
    {
        string environment = SoftwareStats.GetAppEnvironment;
        bool isDev = environment == Environments.Development;
        bool isDocker = HardwareStats.CheckIfDocker;
        bool forceDebug = (isDocker) ? (Environment.GetEnvironmentVariable("FORCE_DEBUG") is "true") : (args?.Length > 0 && args.Contains("-forceDebug"));
        bool forceTrace = (isDocker) ? (Environment.GetEnvironmentVariable("FORCE_TRACE") is "true") : (args?.Length > 0 && args.Contains("-forceTrace"));
        bool SkipWaiting = (isDocker) ? (Environment.GetEnvironmentVariable("SKIP_WAITING") is "true") : (args?.Length > 0 && args.Contains("-skipWaiting"));
        int logDays = int.Parse(Environment.GetEnvironmentVariable("LOG_RETENTION_DAYS") ?? "7", NumberStyles.Integer, CultureInfo.InvariantCulture);

        if (isDocker && !SkipWaiting)
        {
            // Give the database time to start up
            await Task.Delay(TimeSpan.FromSeconds(30));
        }

        HostApplicationBuilderSettings appSettings = new()
        {
            ContentRootPath = Directory.GetCurrentDirectory(),
            EnvironmentName = (isDev) ? Environments.Development : Environments.Production
        };

        HostApplicationBuilder appBuilder = Host.CreateEmptyApplicationBuilder(appSettings);

        #region Add logging

        appBuilder.Logging.AzzyBotLogging(isDev, forceDebug, forceTrace);

        #endregion Add logging

        #region Add configuration

        appBuilder.Configuration.AddAppConfiguration(isDev, isDocker);

        #endregion Add configuration

        #region Add services

        appBuilder.Services.AddAppSettings();
        appBuilder.Services.AzzyBotServices(isDev, isDocker, logDays);

        #endregion Add services

        using IHost app = appBuilder.Build();
        app.ApplyDbMigrations();
        await app.StartAsync();
        await app.WaitForShutdownAsync();
    }
}
