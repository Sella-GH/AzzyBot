using System;
using System.Net;
using System.Net.Http;

using AzzyBot.Bot.Commands;
using AzzyBot.Bot.Commands.Checks;
using AzzyBot.Bot.Commands.Converters;
using AzzyBot.Bot.Services;
using AzzyBot.Bot.Services.CronJobs;
using AzzyBot.Bot.Services.DiscordEvents;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Settings;
using AzzyBot.Bot.Settings.Validators;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Records;
using AzzyBot.Data.Extensions;
using AzzyBot.Data.Settings;

using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.InteractionNamingPolicies;
using DSharpPlus.Extensions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;

using Lavalink4NET.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NCronJob;

namespace AzzyBot.Bot.Extensions;

public static class IServiceCollectionExtensions
{
    public static void AzzyBotServices(this IServiceCollection services, int logDays = 7)
    {
        IServiceProvider serviceProvider = services.BuildServiceProvider();
        DatabaseSettings dbSettings = serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value;
        AzzyBotSettings botSettings = serviceProvider.GetRequiredService<IOptions<AzzyBotSettings>>().Value;
        MusicStreamingSettings musicSettings = serviceProvider.GetRequiredService<IOptions<MusicStreamingSettings>>().Value;

        // Need to register as Singleton first
        // Otherwise DI doesn't work properly
        services.AddSingleton<CoreServiceHost>().AddHostedService(static s => s.GetRequiredService<CoreServiceHost>());

        // Register the database services
        services.AzzyBotDataServices(dbSettings);

        services.DiscordClient(botSettings.BotToken);
        services.DiscordClientCommands(botSettings);
        services.DiscordClientInteractivity();

        services.AddSingleton<DiscordBotService>();
        services.AddSingleton<DiscordBotServiceHost>().AddHostedService(static s => s.GetRequiredService<DiscordBotServiceHost>());
        services.AddSingleton<CoreService>();

        services.AddHttpClient(SoftwareStats.GetAppName, static c =>
        {
            c.DefaultRequestHeaders.UserAgent.Add(new(SoftwareStats.GetAppName, SoftwareStats.GetAppVersion));
            c.DefaultRequestVersion = HttpVersion.Version11;
            c.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
            c.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<WebRequestService>();
        services.AddSingleton<UpdaterService>();

        services.AddSingleton<AzuraCastApiService>();
        services.AddSingleton<AzuraCastFileService>();
        services.AddSingleton<AzuraCastPingService>();
        services.AddSingleton<AzuraCastUpdateService>();
        services.AddNCronJob(o =>
        {
#if DEBUG && !DOCKER_DEBUG
            const string everyMinute = "* * * * *";
            const string every15Minutes = "*/2 * * * *";
            const string everyHour = "*/3 * * * *";
            const string every6Hours = "*/4 * * * *";
            const string every12Hours = "*/5 * * * *";
            const string everyDay = "*/6 * * * *";
#else
            const string everyMinute = "* * * * *";
            const string every15Minutes = "*/15 * * * *";
            const string everyHour = "0 * * * *";
            const string every6Hours = "0 */6 * * *";
            const string every12Hours = "0 */12 * * *";
            const string everyDay = "0 0 * * *";
#endif
            o.AddJob<AzuraCheckApiPermissionsJob>(j => j.WithName(nameof(AzuraCheckApiPermissionsJob)).WithCronExpression(every12Hours));
            o.AddJob<AzuraCheckFileChangesJob>(j => j.WithName(nameof(AzuraCheckFileChangesJob)).WithCronExpression(everyHour));
            o.AddJob<AzuraCheckUpdatesJob>(j => j.WithName(nameof(AzuraCheckUpdatesJob)).WithCronExpression(every6Hours));
            o.AddJob<AzuraPersistentNowPlayingJob>(j => j.WithName(nameof(AzuraPersistentNowPlayingJob)).WithCronExpression(everyMinute));
            o.AddJob<AzuraRequestJob>(j => j.WithName(nameof(AzuraRequestJob))); // This job is not intended to be run at a certain time, it will only be requested!
            o.AddJob<AzuraStatusPingJob>(j => j.WithName(nameof(AzuraStatusPingJob)).WithCronExpression(every15Minutes));
            o.AddJob<AzzyBotCheckPermissionsJob>(j => j.WithName(nameof(AzzyBotCheckPermissionsJob)).WithCronExpression(every12Hours));
            o.AddJob<AzzyBotInactiveGuildJob>(j => j.WithName(nameof(AzzyBotInactiveGuildJob)).WithCronExpression(everyDay));
            o.AddJob<AzzyBotUpdateCheckJob>(j => j.WithName(nameof(AzzyBotUpdateCheckJob)).WithCronExpression(every6Hours));
            o.AddJob<DatabaseCleaningJob>(j => j.WithName(nameof(DatabaseCleaningJob)).WithCronExpression(everyDay).RunAtStartup());
            o.AddJob<LogfileCleaningJob>(j => j.WithName(nameof(LogfileCleaningJob)).WithCronExpression(everyDay).WithParameter(logDays).RunAtStartup());
            o.AddJob<MusicStreamingPersistentNowPlayingJob>(j => j.WithName(nameof(MusicStreamingPersistentNowPlayingJob)).WithCronExpression(everyMinute));
        });
        services.AddSingleton<CronJobManager>();

        services.AddLavalink();
        services.ConfigureLavalink(config =>
        {
#if DOCKER || DOCKER_DEBUG
            Uri baseAddress = new("http://AzzyBot-Ms:2333");
#else
            Uri baseAddress = new("http://localhost:2333");
#endif
            if (musicSettings is not null)
            {
                if (!string.IsNullOrWhiteSpace(musicSettings.LavalinkHost) && musicSettings.LavalinkPort is not 0)
                {
                    baseAddress = new($"http://{musicSettings.LavalinkHost}:{musicSettings.LavalinkPort}");
                }
                else if (!string.IsNullOrWhiteSpace(musicSettings.LavalinkHost) && musicSettings.LavalinkPort is 0)
                {
                    baseAddress = new($"http://{musicSettings.LavalinkHost}:2333");
                }
                else if (string.IsNullOrWhiteSpace(musicSettings.LavalinkHost) && musicSettings.LavalinkPort is not 0)
                {
#if DOCKER || DOCKER_DEBUG
                    baseAddress = new($"http://AzzyBot-Ms:{musicSettings.LavalinkPort}");
#else
                    baseAddress = new($"http://localhost:{musicSettings.LavalinkPort}");
#endif
                }

                if (!string.IsNullOrWhiteSpace(musicSettings.LavalinkPassword))
                    config.Passphrase = musicSettings.LavalinkPassword;
            }

            config.BaseAddress = baseAddress;
            config.Label = "AzzyBot";
            config.ReadyTimeout = TimeSpan.FromSeconds(30);
            config.ResumptionOptions = new(TimeSpan.Zero);
        });
        services.AddSingleton<MusicStreamingService>();
    }

    public static void AddAppSettings(this IServiceCollection services, string settingsFile)
    {
        services.AddSingleton<IValidateOptions<AzzyBotSettings>, AzzyBotSettingsValidator>()
            .AddOptionsWithValidateOnStart<AzzyBotSettings>()
            .BindConfiguration(nameof(AzzyBotSettings))
            .Configure(c => c.SettingsFile = settingsFile);

        services.AddSingleton<IValidateOptions<DatabaseSettings>, DatabaseSettingsValidator>()
            .AddOptionsWithValidateOnStart<DatabaseSettings>()
            .BindConfiguration(nameof(DatabaseSettings));

        IServiceProvider sp = services.BuildServiceProvider();
        DatabaseSettings dbSettings = sp.GetRequiredService<IOptions<DatabaseSettings>>().Value;
        if (!string.IsNullOrWhiteSpace(dbSettings.NewEncryptionKey) && dbSettings.NewEncryptionKey.Length is not 32)
            throw new ArgumentException($"The {nameof(DatabaseSettings.NewEncryptionKey)} must contain exactly 32 characters!");

        services.AddSingleton<IValidateOptions<DiscordStatusSettings>, DiscordStatusSettingsValidator>()
            .AddOptionsWithValidateOnStart<DiscordStatusSettings>()
            .BindConfiguration(nameof(DiscordStatusSettings));

        services.AddSingleton<IValidateOptions<MusicStreamingSettings>, MusicStreamingSettingsValidator>()
            .AddOptionsWithValidateOnStart<MusicStreamingSettings>()
            .BindConfiguration(nameof(MusicStreamingSettings));

        services.AddSingleton<IValidateOptions<CoreUpdaterSettings>, CoreUpdaterValidator>()
            .AddOptionsWithValidateOnStart<CoreUpdaterSettings>()
            .BindConfiguration(nameof(CoreUpdaterSettings));

        services.AddSingleton<IValidateOptions<AppStats>, AppStatsValidator>()
            .AddOptionsWithValidateOnStart<AppStats>()
            .BindConfiguration(nameof(AppStats));
    }

    private static void DiscordClient(this IServiceCollection services, string token)
    {
        services.AddDiscordClient(token, DiscordIntents.Guilds | DiscordIntents.GuildVoiceStates);
        services.ConfigureEventHandlers(static e => e.AddEventHandlers<DiscordGuildsHandler>(ServiceLifetime.Singleton));
    }

    private static void DiscordClientCommands(this IServiceCollection services, AzzyBotSettings settings)
    {
        services.AddSingleton<DiscordCommandsErrorHandler>();
        services.AddCommandsExtension((sp, c) =>
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
            c.AddCommand(typeof(ConfigCommands.LegalsGroup));
            c.AddCommand(typeof(CoreCommands.CoreGroup));
            c.AddCommand(typeof(MusicStreamingCommands.PlayerGroup));

#if DEBUG || DOCKER_DEBUG
            c.AddCommand(typeof(DebugCommands.DebugGroup), settings.ServerId);
#endif

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
