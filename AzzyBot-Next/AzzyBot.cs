using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using AzzyBot.Services;
using AzzyBot.Services.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace AzzyBot;

internal sealed class AzzyBot
{
    private static async Task Main()
    {
        string environment = (Assembly.GetExecutingAssembly().GetName().Name?.ToString().EndsWith("Dev", StringComparison.Ordinal) ?? throw new InvalidOperationException("App has no name")) ? "Development" : "Production";
        IHostBuilder builder = Host.CreateDefaultBuilder();
        List<string> requestedModules = [];

        builder.ConfigureAppConfiguration(config =>
        {
            config.Sources.Clear();
            if (environment is "Production")
            {
                config.AddJsonFile(Path.Combine("Settings", "AzzyBotSettings.json"), false, false);
            }
            else
            {
                config.AddJsonFile(Path.Combine("Settings", "AzzyBotSettings-Dev.json"), false, false);
            }
        });

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
            logging.SetMinimumLevel((environment is "Production") ? LogLevel.Information : LogLevel.Debug);
        });

        // Need to register as Singleton first
        // Otherwise DI doesn't work properly
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<CoreService>();
            services.AddHostedService(s => s.GetRequiredService<CoreService>());

            services.AddSingleton<DiscordBotService>();
            services.AddHostedService(s => s.GetRequiredService<DiscordBotService>());
        });

        builder.UseConsoleLifetime();
        builder.UseEnvironment(environment);

        await builder.RunConsoleAsync();
    }
}
