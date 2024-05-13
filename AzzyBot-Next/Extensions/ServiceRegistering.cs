using System;
using System.IO;
using AzzyBot.Database;
using AzzyBot.Services;
using AzzyBot.Services.Modules;
using AzzyBot.Settings;
using AzzyBot.Utilities;
using AzzyBot.Utilities.Encryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace AzzyBot.Extensions;

internal static class ServiceRegistering
{
    internal static void AzzyBotLogging(this ILoggingBuilder logging, bool isDev = false, bool forceDebug = false)
    {
        logging.AddConsole();
        logging.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", (isDev || forceDebug) ? LogLevel.Debug : LogLevel.Warning);
        logging.AddFilter("Microsoft.EntityFrameworkCore.Database", (isDev || forceDebug) ? LogLevel.Debug : LogLevel.Warning);
        logging.AddFilter("Microsoft.EntityFrameworkCore.Migrations", (isDev || forceDebug) ? LogLevel.Debug : LogLevel.Information);
        logging.AddFilter("Microsoft.Extensions.Hosting", LogLevel.Warning);
        logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
        logging.AddSimpleConsole(config =>
        {
            config.ColorBehavior = LoggerColorBehavior.Enabled;
            config.IncludeScopes = true;
            config.SingleLine = true;
            config.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
        });
        logging.SetMinimumLevel((isDev || forceDebug) ? LogLevel.Debug : LogLevel.Information);
    }

    internal static void AzzyBotServices(this IServiceCollection services)
    {
        // Enable or disable modules based on the settings
        IServiceProvider serviceProvider = services.BuildServiceProvider();
        AzzyBotSettingsRecord settings = serviceProvider.GetRequiredService<AzzyBotSettingsRecord>();

        // Set the encryption key
        EncryptionHelper.Key = settings.EncryptionKey;

        // Need to register as Singleton first
        // Otherwise DI doesn't work properly
        services.AddSingleton<CoreServiceHost>();
        services.AddHostedService(s => s.GetRequiredService<CoreServiceHost>());

        string connectionString = GetConnectionString(settings.Database?.Host, settings.Database?.Port, settings.Database?.User, settings.Database?.Password, settings.Database?.DatabaseName);
        services.AddDbContextFactory<AzzyDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
        services.AddSingleton<DbActions>();

        services.AddSingleton<DiscordBotService>();
        services.AddSingleton<DiscordBotServiceHost>();
        services.AddHostedService(s => s.GetRequiredService<DiscordBotServiceHost>());

        services.AddSingleton<WebRequestService>();
        services.AddSingleton<UpdaterService>();
        services.AddSingleton<TimerServiceHost>();
        services.AddHostedService(s => s.GetRequiredService<TimerServiceHost>());
    }

    internal static void AzzyBotSettings(this IServiceCollection services, bool isDev = false)
    {
        string settingsFile = (isDev) ? "AzzyBotSettings-Dev.json" : "AzzyBotSettings.json";
        string path = Path.Combine("Settings", settingsFile);

        AzzyBotSettingsRecord? settings = GetConfiguration(path).Get<AzzyBotSettingsRecord>();
        if (settings is null)
        {
            Console.Error.Write("No bot configuration found! Please set your settings.");
            if (!AzzyStatsGeneral.CheckIfLinuxOs)
                Console.ReadKey();

            Environment.Exit(1);
        }

        services.AddSingleton(settings);
    }

    internal static void AzzyBotStats(this IServiceCollection services)
    {
        string path = Path.Combine("Modules", "Core", "Files", "AzzyBotStats.json");

        AzzyBotStatsRecord? stats = GetConfiguration(path).Get<AzzyBotStatsRecord>();
        if (stats is null)
        {
            Console.Error.Write("There is something wrong with your configuration. Did you followed the installation instructions?");
            if (!AzzyStatsGeneral.CheckIfLinuxOs)
                Console.ReadKey();

            Environment.Exit(1);
        }

        services.AddSingleton(stats);
    }

    private static string GetConnectionString(string? host, int? port, string? user, string? password, string? database)
    {
        if (string.IsNullOrWhiteSpace(host))
            host = "AzzyBot-Db";

        if (port is 0)
            port = 3306;

        if (string.IsNullOrWhiteSpace(user))
            user = "azzybot";

        // No password because it can be null when using non-docker
        if (string.IsNullOrWhiteSpace(password) && AzzyStatsGeneral.CheckIfDocker)
            password = "thisIsAzzyB0!P@ssw0rd";

        if (string.IsNullOrWhiteSpace(database))
            database = "azzybot";

        return $"Server={host};Port={port};User={user};Password={password};Database={database};";
    }

    private static IConfiguration GetConfiguration(string path)
    {
        ConfigurationBuilder configBuilder = new();

        configBuilder.Sources.Clear();
        configBuilder.AddJsonFile(path, false, false);

        return configBuilder.Build();
    }
}
