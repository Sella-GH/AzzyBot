using System;
using System.Collections.Generic;
using System.IO;
using AzzyBot.Bot.Commands;
using AzzyBot.Bot.Commands.Checks;
using AzzyBot.Bot.Commands.Converters;
using AzzyBot.Bot.Services;
using AzzyBot.Bot.Services.CronJobs;
using AzzyBot.Bot.Services.DiscordEvents;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Settings;
using AzzyBot.Core.Services.BackgroundServices;
using AzzyBot.Core.Settings;
using AzzyBot.Core.Utilities;
using AzzyBot.Data.Extensions;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.NamingPolicies;
using DSharpPlus.Extensions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Lavalink4NET.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        services.AddHostedService(static s => s.GetRequiredService<CoreServiceHost>());

        // Register the database services
        services.AzzyBotDataServices(isDev, settings.Database!.EncryptionKey, settings.Database.Host, settings.Database.Port, settings.Database.User, settings.Database.Password, settings.Database.DatabaseName);

        services.DiscordClient(settings.BotToken);
        services.DiscordClientCommands(settings);
        services.DiscordClientInteractivity();

        services.AddSingleton<DiscordBotService>();
        services.AddSingleton<DiscordBotServiceHost>();
        services.AddHostedService(static s => s.GetRequiredService<DiscordBotServiceHost>());

        services.AddSingleton<QueuedBackgroundTask>();
        services.AddSingleton<QueuedBackgroundTaskHost>();
        services.AddHostedService(static s => s.GetRequiredService<QueuedBackgroundTaskHost>());

        services.AddSingleton<WebRequestService>();
        services.AddSingleton<UpdaterService>();

        services.AddSingleton<AzuraCastApiService>();
        services.AddSingleton<AzuraCastFileService>();
        services.AddSingleton<AzuraCastPingService>();
        services.AddSingleton<AzuraCastUpdateService>();
        services.AddSingleton<AzuraChecksBackgroundTask>();
        services.AddSingleton<AzuraRequestBackgroundTask>();

        services.AddLavalink();
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

    private static void DiscordClient(this IServiceCollection services, string token)
    {
        services.AddDiscordClient(token, DiscordIntents.Guilds | DiscordIntents.GuildVoiceStates);
        services.ConfigureEventHandlers(static e => e.AddEventHandlers<DiscordGuildsHandler>(ServiceLifetime.Singleton));
    }

    private static void DiscordClientCommands(this IServiceCollection services, AzzyBotSettingsRecord settings)
    {
        services.AddSingleton<DiscordCommandsErrorHandler>();
        services.AddCommandsExtension((IServiceProvider sp, CommandsExtension c) =>
        {
            DiscordCommandsErrorHandler handler = sp.GetRequiredService<DiscordCommandsErrorHandler>();

            c.CommandErrored += handler.CommandErroredAsync;

            // Only add admin commands to the main server
            c.AddCommand(typeof(AdminCommands.AdminGroup), settings.ServerId);

            // These commands are for every server
            c.AddCommand(typeof(AzuraCastCommands.AzuraCastGroup));
            c.AddCommand(typeof(AzuraCastCommands.DjGroup));
            c.AddCommand(typeof(AzuraCastCommands.MusicGroup));
            c.AddCommand(typeof(ConfigCommands.ConfigGroup));
            c.AddCommand(typeof(CoreCommands.CoreGroup));
            c.AddCommand(typeof(MusicStreamingCommands.PlayerGroup));

            // Only add debug commands if it's a dev build
            if (SoftwareStats.GetAppEnvironment == Environments.Development)
                c.AddCommand(typeof(DebugCommands.DebugGroup), settings.ServerId);

            c.AddCheck<AzuraCastDiscordChannelCheck>();
            c.AddCheck<AzuraCastDiscordPermCheck>();
            c.AddCheck<AzuraCastOnlineCheck>();
            c.AddCheck<FeatureAvailableCheck>();
            c.AddCheck<ModuleActivatedCheck>();

            SlashCommandProcessor slashCommandProcessor = new(new SlashCommandConfiguration()
            {
                NamingPolicy = new KebabCaseNamingPolicy()
            });
            slashCommandProcessor.AddConverter<Uri>(new UriArgumentConverter());

            c.AddProcessor(slashCommandProcessor);
        },
        new CommandsConfiguration()
        {
            RegisterDefaultCommandProcessors = false,
            UseDefaultCommandErrorHandler = false
        });
    }

    private static void DiscordClientInteractivity(this IServiceCollection services)
    {
        InteractivityConfiguration config = new()
        {
            ResponseBehavior = InteractionResponseBehavior.Ignore,
            ResponseMessage = "This is not a valid option!",
            Timeout = TimeSpan.FromMinutes(15)
        };

        services.AddInteractivityExtension(config);
    }
}
