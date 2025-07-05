using System;

using AzzyBot.Bot.Commands;
using AzzyBot.Bot.Commands.Checks;
using AzzyBot.Bot.Commands.Converters;
using AzzyBot.Bot.Services;
using AzzyBot.Bot.Services.CronJobs;
using AzzyBot.Bot.Services.DiscordEvents;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Settings;
using AzzyBot.Bot.Settings.Validators;
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
            const string everyHour = "0 */1 * * *";
            const string every6Hours = "0 */6 * * *";
            const string every12Hours = "0 */12 * * *";
            const string everyDay = "0 0 */1 * *";
#endif
            o.AddJob<AzuraCheckApiPermissionsJob>(j => j.WithCronExpression(every12Hours).WithName(nameof(AzuraCheckApiPermissionsJob)));
            o.AddJob<AzuraCheckFileChangesJob>(j => j.WithCronExpression(everyHour).WithName(nameof(AzuraCheckFileChangesJob)));
            o.AddJob<AzuraCheckUpdatesJob>(j => j.WithCronExpression(every6Hours).WithName(nameof(AzuraCheckUpdatesJob)));
            o.AddJob<AzuraPersistentNowPlayingJob>(j => j.WithCronExpression(everyMinute).WithName(nameof(AzuraPersistentNowPlayingJob)));
            o.AddJob<AzuraRequestJob>(); // This job is not intended to be run at a certain time, it will only be requested!
            o.AddJob<AzuraStatusPingJob>(j => j.WithCronExpression(every15Minutes).WithName(nameof(AzuraStatusPingJob)));
            o.AddJob<AzzyBotCheckPermissionsJob>(j => j.WithCronExpression(every12Hours).WithName(nameof(AzzyBotCheckPermissionsJob)));
            o.AddJob<AzzyBotUpdateCheckJob>(j => j.WithCronExpression(every6Hours).WithName(nameof(AzzyBotUpdateCheckJob)));
            o.AddJob<DatabaseCleaningJob>(j => j.WithCronExpression(everyDay).WithName(nameof(DatabaseCleaningJob))).RunAtStartup();
            o.AddJob<LogfileCleaningJob>(j => j.WithCronExpression(everyDay).WithName(nameof(LogfileCleaningJob)).WithParameter(logDays)).RunAtStartup();
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
