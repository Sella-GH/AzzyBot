using System;
#if DOCKER_DEBUG || DOCKER
using System.Globalization;
#endif
using System.IO;
#if DEBUG || RELEASE
using System.Linq;
#endif
using System.Threading.Tasks;

using AzzyBot.Bot.Extensions;
using AzzyBot.Core.Extensions;
using AzzyBot.Core.Utilities;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NCronJob;

namespace AzzyBot.Bot;

public static class Startup
{
    public static async Task Main(string[] args)
    {
#pragma warning disable RCS0005 // Add blank line before #endregion
        #region Parse arguments

#if DEBUG || RELEASE
        string? logLevelArg = args?.FirstOrDefault(static a => a.StartsWith("-LogLevel", StringComparison.OrdinalIgnoreCase))?.Split('=')[1];
#endif

#if DEBUG
        LogLevel logLevel = Enum.Parse<LogLevel>(logLevelArg ?? "Debug", true);
#elif RELEASE
        LogLevel logLevel = Enum.Parse<LogLevel>(logLevelArg ?? "Information", true);
#endif

#if DEBUG || RELEASE
        const int logDays = 7;
#else
        bool skipWaiting = Environment.GetEnvironmentVariable("SKIP_WAITING") is "true";
        LogLevel logLevel = Enum.Parse<LogLevel>(Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Information", true);
        int logDays = int.Parse(Environment.GetEnvironmentVariable("LOG_RETENTION_DAYS") ?? "7", NumberStyles.Integer, CultureInfo.InvariantCulture);

        // Give the database time to start up
        if (!skipWaiting)
            await Task.Delay(TimeSpan.FromSeconds(30));
#endif
        #endregion Parse arguments
#pragma warning restore RCS0005 // Add blank line before #endregion

        #region Create host builder

        HostApplicationBuilderSettings appSettings = new()
        {
            ContentRootPath = Directory.GetCurrentDirectory(),
#if DEBUG || DOCKER_DEBUG
            EnvironmentName = Environments.Development
#else
            EnvironmentName = Environments.Production
#endif
        };

        HostApplicationBuilder appBuilder = Host.CreateEmptyApplicationBuilder(appSettings);

        #endregion Create host builder

        #region Add logging

        appBuilder.Logging.AzzyBotLogging(logLevel);

        #endregion Add logging

        #region Add configuration

#if DEBUG || DOCKER_DEBUG
        const string settingsFile = "AzzyBotSettings-Dev.json";
#elif DOCKER
        const string settingsFile = "AzzyBotSettings-Docker.json";
#else
        const string settingsFile = "AzzyBotSettings.json";
#endif
        string settingsFilePath = Path.Combine("Settings", settingsFile);

        appBuilder.Configuration.AddAppConfiguration(settingsFilePath);

        #endregion Add configuration

        #region Add services

        try
        {
            appBuilder.Services.AddAppSettings(settingsFilePath);
            appBuilder.Services.AzzyBotServices(logDays);
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
        await app.UseNCronJobAsync();
        await app.RunAsync();
    }
}
