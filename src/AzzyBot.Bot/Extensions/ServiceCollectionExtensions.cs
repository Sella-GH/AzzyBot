using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AzzyBot.Bot.Services;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Settings;
using AzzyBot.Core.Services.Interfaces;
using AzzyBot.Core.Services.Queues;
using AzzyBot.Core.Settings;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data;
using EntityFramework.Exceptions.PostgreSQL;
using Lavalink4NET.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzzyBot.Bot.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AzzyBotServices(this IServiceCollection services, bool isDev, bool isDocker)
    {
        IServiceProvider serviceProvider = services.BuildServiceProvider();
        AzzyBotSettingsRecord settings = serviceProvider.GetRequiredService<AzzyBotSettingsRecord>();

        // Set the encryption key
        Crypto.EncryptionKey = Encoding.UTF8.GetBytes(settings.Database!.EncryptionKey);

        // Need to register as Singleton first
        // Otherwise DI doesn't work properly
        services.AddSingleton<CoreServiceHost>();
        services.AddHostedService(s => s.GetRequiredService<CoreServiceHost>());

        string connectionString = GetConnectionString(isDev, settings.Database?.Host, settings.Database?.Port, settings.Database?.User, settings.Database?.Password, settings.Database?.DatabaseName);
        services.AddDbContextPool<AzzyDbContext>(o => o.UseNpgsql(connectionString).UseExceptionProcessor().EnableSensitiveDataLogging(isDev));
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

        services.AddLavalink();
        services.AddSingleton(s => s.GetRequiredService<DiscordBotServiceHost>().Client);
        services.ConfigureLavalink(config =>
        {
            Uri baseAddress = (isDocker) ? new("http://AzzyBot-Ms:2333") : new("http://localhost:2333");
            if (settings.MusicStreaming is not null)
            {
                if (!string.IsNullOrWhiteSpace(settings.MusicStreaming.LavalinkHost) && settings.MusicStreaming.LavalinkPort is not 0)
                {
                    baseAddress = new($"http://{settings.MusicStreaming.LavalinkHost}:{settings.MusicStreaming.LavalinkPort}");
                }
                else if (!string.IsNullOrWhiteSpace(settings.MusicStreaming.LavalinkHost) && settings.MusicStreaming.LavalinkPort is 0)
                {
                    baseAddress = new($"http://{settings.MusicStreaming.LavalinkHost}:2333");
                }
                else if (string.IsNullOrWhiteSpace(settings.MusicStreaming.LavalinkHost) && settings.MusicStreaming.LavalinkPort is not 0)
                {
                    baseAddress = (isDocker) ? new($"http://AzzyBot-Ms:{settings.MusicStreaming.LavalinkPort}") : new($"http://localhost:{settings.MusicStreaming.LavalinkPort}");
                }

                if (!string.IsNullOrWhiteSpace(settings.MusicStreaming.LavalinkPassword))
                    config.Passphrase = settings.MusicStreaming.LavalinkPassword;
            }

            config.BaseAddress = baseAddress;
            config.Label = "AzzyBot";
            config.ReadyTimeout = TimeSpan.FromSeconds(15);
            config.ResumptionOptions = new(TimeSpan.Zero);
        });
        services.AddSingleton<MusicStreamingService>();
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
            if (!HardwareStats.CheckIfLinuxOs)
                Console.ReadKey();

            Environment.Exit(1);
        }

        settings.SettingsFile = path;

        // Check settings if something is missing
        List<string> exclusions = new(11)
        {
            nameof(settings.Database.NewEncryptionKey),
            nameof(settings.DiscordStatus.StreamUrl),
            nameof(settings.MusicStreaming),
            nameof(settings.MusicStreaming.LavalinkHost),
            nameof(settings.MusicStreaming.LavalinkPort),
            nameof(settings.MusicStreaming.LavalinkPassword)
        };

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

        if (settings.Database!.EncryptionKey.Length is not 32)
        {
            Console.Error.WriteLine($"The {nameof(settings.Database.EncryptionKey)} must contain exactly 32 characters!");
            if (!HardwareStats.CheckIfLinuxOs)
                Console.ReadKey();

            Environment.Exit(1);
        }

        services.AddSingleton(settings);
    }

    public static void AzzyBotStats(this IServiceCollection services, bool isDev)
    {
        if (isDev)
        {
            services.AddSingleton(new AzzyBotStatsRecord("Unknown", DateTime.Now, 0));
            return;
        }

        string path = Path.Combine("Modules", "Core", "Files", "AzzyBotStats.json");

        AzzyBotStatsRecord? stats = GetConfiguration(path).Get<AzzyBotStatsRecord>();
        if (stats is null)
        {
            Console.Error.Write("There is something wrong with your configuration. Did you followed the installation instructions?");
            if (!HardwareStats.CheckIfLinuxOs)
                Console.ReadKey();

            Environment.Exit(1);
        }

        services.AddSingleton(stats);
    }

    private static string GetConnectionString(bool isDev, string? host, int? port, string? user, string? password, string? database)
    {
        if (string.IsNullOrWhiteSpace(host))
            host = "AzzyBot-Db";

        if (port is 0)
            port = 5432;

        if (string.IsNullOrWhiteSpace(user))
            user = "azzybot";

        // No password because it can be null when using non-docker
        if (string.IsNullOrWhiteSpace(password) && HardwareStats.CheckIfDocker)
            password = "thisIsAzzyB0!P@ssw0rd";

        if (string.IsNullOrWhiteSpace(database))
            database = (isDev) ? "azzybot-dev" : "azzybot";

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
