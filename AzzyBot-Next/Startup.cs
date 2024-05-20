using System;
using System.IO;
using System.Threading.Tasks;
using AzzyBot.Extensions;
using AzzyBot.Utilities;
using Microsoft.Extensions.Hosting;

namespace AzzyBot;

public static class Startup
{
    public static async Task Main(string[] args)
    {
        string environment = AzzyStatsSoftware.GetBotEnvironment;
        bool isDev = environment == Environments.Development;
        bool isDocker = AzzyStatsHardware.CheckIfDocker;
        bool forceDebug;

        if (isDocker)
        {
            forceDebug = Environment.GetEnvironmentVariable("FORCE_DEBUG") == "true";
        }
        else
        {
            forceDebug = args?.Length > 0 && args[0] is "-forceDebug";
        }

        HostApplicationBuilderSettings appSettings = new()
        {
            ContentRootPath = Directory.GetCurrentDirectory(),
            DisableDefaults = true,
            EnvironmentName = (isDev) ? Environments.Development : Environments.Production
        };
        HostApplicationBuilder appBuilder = Host.CreateApplicationBuilder(appSettings);

        #region Add logging

        appBuilder.Logging.AzzyBotLogging(isDev, forceDebug);

        #endregion Add logging

        #region Add services

        appBuilder.Services.AzzyBotSettings(isDev, isDocker);
        appBuilder.Services.AzzyBotStats();
        appBuilder.Services.AzzyBotServices();

        #endregion Add services

        if (isDocker)
        {
            // Give the database time to start up
            await Task.Delay(TimeSpan.FromSeconds(3));
        }

        using IHost app = appBuilder.Build();
        app.ApplyDbMigrations();
        await app.StartAsync();
        await app.WaitForShutdownAsync();
    }
}
