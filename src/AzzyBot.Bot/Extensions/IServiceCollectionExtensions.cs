using System;
using System.Collections.Generic;
using System.IO;
using AzzyBot.Bot.Services;
using AzzyBot.Bot.Services.BackgroundServices;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Settings;
using AzzyBot.Core.Services.BackgroundServices;
using AzzyBot.Core.Settings;
using AzzyBot.Core.Utilities;
using AzzyBot.Data.Extensions;
using Lavalink4NET.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzzyBot.Bot.Extensions;

public static class IServiceCollectionExtensions
{
    public static void AzzyBotServices(this IServiceCollection services, bool isDev, bool isDocker)
    {
        IServiceProvider serviceProvider = services.BuildServiceProvider();
        AzzyBotSettingsRecord settings = serviceProvider.GetRequiredService<AzzyBotSettingsRecord>();

        // Need to register as Singleton first
        // Otherwise DI doesn't work properly
        services.AddSingleton<CoreServiceHost>();
        services.AddHostedService(s => s.GetRequiredService<CoreServiceHost>());

        // Register the database services
        services.AzzyBotDataServices(isDev, settings.Database!.EncryptionKey, settings.Database.Host, settings.Database.Port, settings.Database.User, settings.Database.Password, settings.Database.DatabaseName);

        services.AddSingleton<DiscordBotService>();
        services.AddSingleton<DiscordBotServiceHost>();
        services.AddHostedService(s => s.GetRequiredService<DiscordBotServiceHost>());

        services.AddSingleton<QueuedBackgroundTask>();
        services.AddSingleton<QueuedBackgroundTaskHost>();
        services.AddHostedService(s => s.GetRequiredService<QueuedBackgroundTaskHost>());

        services.AddSingleton<WebRequestService>();
        services.AddSingleton<UpdaterService>();

        services.AddSingleton<AzuraCastApiService>();
        services.AddSingleton<AzuraCastFileService>();
        services.AddSingleton<AzuraCastPingService>();
        services.AddSingleton<AzuraCastUpdateService>();
        services.AddSingleton<AzuraChecksBackgroundTask>();
        services.AddSingleton<AzuraRequestBackgroundTask>();

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
            config.ReadyTimeout = TimeSpan.FromSeconds(30);
            config.ResumptionOptions = new(TimeSpan.Zero);
        });
        services.AddSingleton<MusicStreamingService>();
    }

    public static void AzzyBotSettings(this IServiceCollection services, bool isDev, bool isDocker)
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

        AzzyBotSettingsRecord? settings = Misc.GetConfiguration(path).Get<AzzyBotSettingsRecord>();
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
}
