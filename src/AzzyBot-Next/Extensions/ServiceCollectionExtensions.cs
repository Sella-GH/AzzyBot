using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AzzyBot.Core.Services.Interfaces;
using AzzyBot.Core.Services.Queues;
using AzzyBot.Core.Settings;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data;
using AzzyBot.Services;
using AzzyBot.Services.Modules;
using AzzyBot.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzzyBot.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AzzyBotServices(this IServiceCollection services)
    {
        IServiceProvider serviceProvider = services.BuildServiceProvider();
        AzzyBotSettingsRecord settings = serviceProvider.GetRequiredService<AzzyBotSettingsRecord>();

        // Set the encryption key
        Crypto.EncryptionKey = Encoding.UTF8.GetBytes(settings.Database!.EncryptionKey);

        // Need to register as Singleton first
        // Otherwise DI doesn't work properly
        services.AddSingleton<CoreServiceHost>();
        services.AddHostedService(s => s.GetRequiredService<CoreServiceHost>());

        string connectionString = GetConnectionString(settings.Database?.Host, settings.Database?.Port, settings.Database?.User, settings.Database?.Password, settings.Database?.DatabaseName);
        services.AddPooledDbContextFactory<AzzyDbContext>(o => o.UseNpgsql(connectionString));
        services.AddSingleton<DbActions>();

        services.AddSingleton<DiscordBotService>();
        services.AddSingleton<DiscordBotServiceHost>();
        services.AddHostedService(s => s.GetRequiredService<DiscordBotServiceHost>());

        services.AddSingleton<WebRequestService>();
        services.AddSingleton<UpdaterService>();

        services.AddSingleton<AzuraCastApiService>();
        services.AddSingleton<AzuraCastFileService>();
        services.AddSingleton<AzuraCastPingService>();
        services.AddSingleton<AzuraCastUpdateService>();
        services.AddSingleton<AzzyBackgroundService>();
        services.AddSingleton<AzzyBackgroundServiceHost>();
        services.AddHostedService(s => s.GetRequiredService<AzzyBackgroundServiceHost>());
        services.AddSingleton<IQueuedBackgroundTask>(_ => new QueuedBackgroundTask());

        services.AddSingleton<TimerServiceHost>();
        services.AddHostedService(s => s.GetRequiredService<TimerServiceHost>());
    }

    public static void AzzyBotSettings(this IServiceCollection services, bool isDev = false, bool isDocker = false)
    {
        string settingsFile = "AzzyBotSettings.json";
        if (isDev)
        {
            settingsFile = "AzzyBotSettings-Dev.json";
        }
        else if (isDocker)
        {
            settingsFile = "AzzyBotSettings-Docker.json";
        }

        string path = Path.Combine("Settings", settingsFile);

        AzzyBotSettingsRecord? settings = GetConfiguration(path).Get<AzzyBotSettingsRecord>();
        if (settings is null)
        {
            Console.Error.Write("No bot configuration found! Please set your settings.");
            if (!AzzyStatsHardware.CheckIfLinuxOs)
                Console.ReadKey();

            Environment.Exit(1);
        }

        settings.SettingsFile = path;

        // Check settings if something is missing
        List<string> exclusions = [nameof(settings.Database.NewEncryptionKey), nameof(settings.DiscordStatus.StreamUrl)];
        if (isDocker)
        {
            exclusions.Add(nameof(settings.Database.Host));
            exclusions.Add(nameof(settings.Database.Port));
            exclusions.Add(nameof(settings.Database.User));
            exclusions.Add(nameof(settings.Database.Password));
            exclusions.Add(nameof(settings.Database.DatabaseName));
        }
        else
        {
            exclusions.Add(nameof(settings.Database.Password));
        }

        SettingsCheck.CheckSettings(settings, exclusions);

        if (settings.Database!.EncryptionKey.Length != 32)
        {
            Console.Error.WriteLine($"The {nameof(settings.Database.EncryptionKey)} must contain exactly 32 characters!");
            if (!AzzyStatsHardware.CheckIfLinuxOs)
                Console.ReadKey();

            Environment.Exit(1);
        }

        services.AddSingleton(settings);
    }

    public static void AzzyBotStats(this IServiceCollection services, bool isDev)
    {
        AzzyBotStatsRecord? stats = new("Unkown", DateTime.Now, 0);
        if (!isDev)
        {
            string path = Path.Combine("Modules", "Core", "Files", "AzzyBotStats.json");

            stats = GetConfiguration(path).Get<AzzyBotStatsRecord>();
            if (stats is null)
            {
                Console.Error.Write("There is something wrong with your configuration. Did you followed the installation instructions?");
                if (!AzzyStatsHardware.CheckIfLinuxOs)
                    Console.ReadKey();

                Environment.Exit(1);
            }
        }

        services.AddSingleton(stats);
    }

    private static string GetConnectionString(string? host, int? port, string? user, string? password, string? database)
    {
        if (string.IsNullOrWhiteSpace(host))
            host = "AzzyBot-Db";

        if (port is 0)
            port = 5432;

        if (string.IsNullOrWhiteSpace(user))
            user = "azzybot";

        // No password because it can be null when using non-docker
        if (string.IsNullOrWhiteSpace(password) && AzzyStatsHardware.CheckIfDocker)
            password = "thisIsAzzyB0!P@ssw0rd";

        if (string.IsNullOrWhiteSpace(database))
            database = "azzybot";

        return $"Host={host};Port={port};Database={database};Username={user};Password={password};";
    }

    private static IConfiguration GetConfiguration(string path)
    {
        ConfigurationBuilder configBuilder = new();

        configBuilder.Sources.Clear();
        configBuilder.AddJsonFile(path, false, false);

        return configBuilder.Build();
    }
}
