using System;
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
        string environment = AzzyStatsSoftware.GetBotEnvironment;
        bool isDev = environment == Environments.Development;
        bool isDocker = AzzyStatsHardware.CheckIfDocker;
        bool forceDebug = (isDocker) ? (Environment.GetEnvironmentVariable("FORCE_DEBUG") == "true") : (args?.Length > 0 && args.Contains("-forceDebug"));

        if (isDocker)
        {
            // Give the database time to start up
            await Task.Delay(TimeSpan.FromSeconds(30));
        }

        // https://stackoverflow.com/a/71786309
        // It works!
        /*
        if (AzzyStatsHardware.CheckIfMacOs && (!isDev || isDocker))
        {
            await Console.Error.WriteLineAsync("This bot does not support macOS.");
            await Console.Error.WriteLineAsync("Please use another platform for it, as this one can't handle the security requirements of the AES encryption standard.");
            return;
        }
        */

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
        appBuilder.Services.AzzyBotStats(isDev && !isDocker);
        appBuilder.Services.AzzyBotServices(isDev, isDocker);

        #endregion Add services

        using IHost app = appBuilder.Build();
        app.ApplyDbMigrations();
        await app.StartAsync();
        await app.WaitForShutdownAsync();
    }
}
