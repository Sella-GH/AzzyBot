﻿using System;
using System.IO;
using System.Threading.Tasks;
using AzzyBot.Enums;
using AzzyBot.Services;
using AzzyBot.Services.Modules;
using AzzyBot.Settings;
using AzzyBot.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace AzzyBot;

internal static class AzzyBot
{
    private static async Task Main(string[] args)
    {
        EnvironmentEnum environment = AzzyStatsGeneral.GetBotEnvironment;
        bool isDev = environment is EnvironmentEnum.Development;
        bool forceDebug = args.Length > 0 && args[0] is "-forceDebug";

        HostApplicationBuilderSettings appSettings = new()
        {
            DisableDefaults = true
        };
        HostApplicationBuilder appBuilder = Host.CreateApplicationBuilder(appSettings);

        appBuilder.Environment.ContentRootPath = Directory.GetCurrentDirectory();
        appBuilder.Environment.EnvironmentName = nameof(EnvironmentEnum.Production);
        if (isDev)
            appBuilder.Environment.EnvironmentName = nameof(EnvironmentEnum.Development);

        #region Add logging

        appBuilder.Logging.AddConsole();
        appBuilder.Logging.AddFilter("Microsoft.Extensions.Hosting", LogLevel.Warning);
        appBuilder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
        appBuilder.Logging.AddSimpleConsole(config =>
        {
            config.ColorBehavior = LoggerColorBehavior.Enabled;
            config.IncludeScopes = true;
            config.SingleLine = true;
            config.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
            config.UseUtcTimestamp = true;
        });
        appBuilder.Logging.SetMinimumLevel((isDev || forceDebug) ? LogLevel.Debug : LogLevel.Information);

        #endregion Add logging

        #region Add services

        appBuilder.Services.AddSingleton(_ =>
        {
            ConfigurationBuilder configBuilder = new();

            string settingsFile = (isDev) ? "AzzyBotSettings-Dev.json" : "AzzyBotSettings.json";

            configBuilder.Sources.Clear();
            configBuilder.AddJsonFile(Path.Combine("Settings", settingsFile), false, false);

            AzzyBotSettings? settings = configBuilder.Build().Get<AzzyBotSettings>();
            if (settings is null)
            {
                Console.Error.Write("No bot configuration found! Please set your settings.");
                Environment.Exit(1);
            }

            return settings;
        });

        // Enable or disable modules based on the settings
        //IServiceProvider serviceProvider = services.BuildServiceProvider();
        //AzzyBotSettings settings = serviceProvider.GetRequiredService<AzzyBotSettings>();

        // Need to register as Singleton first
        // Otherwise DI doesn't work properly
        appBuilder.Services.AddSingleton<CoreServiceHost>();
        appBuilder.Services.AddHostedService(s => s.GetRequiredService<CoreServiceHost>());

        appBuilder.Services.AddSingleton<DiscordBotService>();
        appBuilder.Services.AddSingleton<DiscordBotServiceHost>();
        appBuilder.Services.AddHostedService(s => s.GetRequiredService<DiscordBotServiceHost>());

        appBuilder.Services.AddSingleton<WebRequestService>();
        appBuilder.Services.AddSingleton<UpdaterService>();
        appBuilder.Services.AddSingleton<TimerServiceHost>();
        appBuilder.Services.AddHostedService(s => s.GetRequiredService<TimerServiceHost>());

        #endregion Add services

        IHost app = appBuilder.Build();
        await app.RunAsync();
    }
}
