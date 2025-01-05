using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using AzzyBot.Bot.Extensions;
using AzzyBot.Core.Extensions;
using AzzyBot.Core.Utilities;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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

        string settingsFilePath = Path.Combine("Settings", GetAppSettingsPath(isDev, isDocker));

        appBuilder.Configuration.AddAppConfiguration(isDev, settingsFilePath);

        #endregion Add configuration

        #region Add services

        try
        {
            appBuilder.Services.AddAppSettings(settingsFilePath);
            appBuilder.Services.AzzyBotServices(isDev, isDocker, logDays);
        }
        catch (OptionsValidationException ex)
        {
            Console.WriteLine(ex.Message);
            if (!HardwareStats.CheckIfLinuxOs)
                Console.ReadKey();

            return;
        }

        #endregion Add services

        using IHost app = appBuilder.Build();
        app.ApplyDbMigrations();
        await app.RunAsync();
    }

    private static string GetAppSettingsPath(bool isDev, bool isDocker)
    {
        if (isDev)
        {
            return "AzzyBotSettings-Dev.json";
        }
        else if (isDocker)
        {
            return "AzzyBotSettings-Docker.json";
        }

        return "AzzyBotSettings.json";
    }
}
