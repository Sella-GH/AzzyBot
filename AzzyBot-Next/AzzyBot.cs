using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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
        string environment = AzzyStatsGeneral.GetBotEnvironment;
        IHostBuilder builder = Host.CreateDefaultBuilder();
        List<string> requestedModules = [];

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
            logging.SetMinimumLevel((environment is "Development" || args[0] is "-forceDebug") ? LogLevel.Debug: LogLevel.Information);
        });

        // Need to register as Singleton first
        // Otherwise DI doesn't work properly
        builder.ConfigureServices(services =>
        {
            // Configure the settings
            services.AddSingleton(_ =>
            {
                ConfigurationBuilder builder = new();
                builder.Sources.Clear();
                string settingsFile = "AzzyBotSettings.json";
                if (environment is "Development")
                    settingsFile = "AzzyBotSettings-Dev.json";

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

            services.AddSingleton<CoreService>();
            services.AddSingleton<DiscordBotService>();

            services.AddSingleton<DiscordBotServiceHost>();
            services.AddHostedService(s => s.GetRequiredService<DiscordBotServiceHost>());
        });

        builder.UseConsoleLifetime();
        builder.UseEnvironment(environment);

        await builder.RunConsoleAsync();
    }
}
