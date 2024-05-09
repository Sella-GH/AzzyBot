using System;
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
        IHostBuilder builder = Host.CreateDefaultBuilder();

        // Add logging
        builder.ConfigureLogging(logging =>
        {
            logging.AddConsole();
            logging.AddFilter("Microsoft.Extensions.Hosting", LogLevel.Warning);
            logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
            logging.AddSimpleConsole(config =>
            {
                config.ColorBehavior = LoggerColorBehavior.Enabled;
                config.IncludeScopes = true;
                config.SingleLine = true;
                config.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                config.UseUtcTimestamp = true;
            });
            logging.SetMinimumLevel((isDev || forceDebug) ? LogLevel.Debug: LogLevel.Information);
        });

        // Need to register as Singleton first
        // Otherwise DI doesn't work properly
        builder.ConfigureServices(services =>
        {
            // Configure the settings
            services.AddSingleton(_ =>
            {
                ConfigurationBuilder builder = new();
                string settingsFile = (isDev) ? "AzzyBotSettings-Dev.json" : "AzzyBotSettings.json";

                builder.Sources.Clear();
                builder.AddJsonFile(Path.Combine("Settings", settingsFile), false, false);

                IConfiguration config = builder.Build();
                AzzyBotSettings? settings = config.Get<AzzyBotSettings>();
                if (settings is null)
                {
                    Console.WriteLine("No bot configuration found! Please set your settings.");
                    Environment.Exit(1);
                }

                return settings;
            });

            // Enable or disable modules based on the settings
            //IServiceProvider serviceProvider = services.BuildServiceProvider();
            //AzzyBotSettings settings = serviceProvider.GetRequiredService<AzzyBotSettings>();

            services.AddSingleton<CoreServiceHost>();
            services.AddHostedService(s => s.GetRequiredService<CoreServiceHost>());

            services.AddSingleton<DiscordBotService>();

            services.AddSingleton<DiscordBotServiceHost>();
            services.AddHostedService(s => s.GetRequiredService<DiscordBotServiceHost>());

            services.AddSingleton<WebRequestService>();
            services.AddSingleton<UpdaterService>();
            services.AddSingleton<TimerServiceHost>();
            services.AddHostedService(s => s.GetRequiredService<TimerServiceHost>());
        });

        builder.UseConsoleLifetime();
        builder.UseEnvironment(environment.ToString());

        await builder.RunConsoleAsync();
    }
}
