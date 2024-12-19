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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NCronJob;

namespace AzzyBot.Bot.Extensions;

public static class IServiceCollectionExtensions
{
    public static void AzzyBotServices(this IServiceCollection services, bool isDev, bool isDocker, int logDays = 7)
    {
        IServiceProvider serviceProvider = services.BuildServiceProvider();
        DatabaseSettings dbSettings = serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value;
        AzzyBotSettings botSettings = serviceProvider.GetRequiredService<IOptions<AzzyBotSettings>>().Value;
        MusicStreamingSettings musicSettings = serviceProvider.GetRequiredService<IOptions<MusicStreamingSettings>>().Value;

        // Need to register as Singleton first
        // Otherwise DI doesn't work properly
        services.AddSingleton<CoreServiceHost>();
        services.AddHostedService(static s => s.GetRequiredService<CoreServiceHost>());

        // Register the database services
        services.AzzyBotDataServices(isDev, dbSettings.EncryptionKey, dbSettings.Host, dbSettings.Port, dbSettings.User, dbSettings.Password, dbSettings.DatabaseName);

        services.DiscordClient(botSettings.BotToken);
        services.DiscordClientCommands(botSettings);
        services.DiscordClientInteractivity();

        services.AddSingleton<DiscordBotService>();
        services.AddSingleton<DiscordBotServiceHost>();
        services.AddHostedService(static s => s.GetRequiredService<DiscordBotServiceHost>());

        services.AddSingleton<WebRequestService>();
        services.AddSingleton<UpdaterService>();

        services.AddSingleton<AzuraCastApiService>();
        services.AddSingleton<AzuraCastFileService>();
        services.AddSingleton<AzuraCastPingService>();
        services.AddSingleton<AzuraCastUpdateService>();
        services.AddNCronJob(o =>
        {
            o.AddJob<AzuraRequestJob>();
            o.AddJob<AzzyBotGlobalChecksJob>(j => j.WithCronExpression("*/15 * * * *").WithName(nameof(AzzyBotGlobalChecksJob))); // Every 15 minutes
            o.AddJob<LogfileCleaningJob>(j => j.WithCronExpression("0 0 */1 * *").WithName(nameof(LogfileCleaningJob)).WithParameter(logDays)); // Every day
        });
        services.AddSingleton<CronJobManager>();

        services.AddLavalink();
        services.ConfigureLavalink(config =>
        {
            Uri baseAddress = (isDocker) ? new("http://AzzyBot-Ms:2333") : new("http://localhost:2333");
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
                    baseAddress = (isDocker) ? new($"http://AzzyBot-Ms:{musicSettings.LavalinkPort}") : new($"http://localhost:{musicSettings.LavalinkPort}");
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

    public static void AddAppSettings(this IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<AzzyBotSettings>, AzzyBotSettingsValidator>().AddOptionsWithValidateOnStart<AzzyBotSettings>();
        services.AddSingleton<IValidateOptions<DatabaseSettings>, DatabaseSettingsValidator>().AddOptionsWithValidateOnStart<DatabaseSettings>();
        services.AddSingleton<IValidateOptions<DiscordStatusSettings>, DiscordStatusSettingsValidator>().AddOptionsWithValidateOnStart<DiscordStatusSettings>();
        services.AddSingleton<IValidateOptions<MusicStreamingSettings>, MusicStreamingSettingsValidator>().AddOptionsWithValidateOnStart<MusicStreamingSettings>();
        services.AddSingleton<IValidateOptions<CoreUpdaterSettings>, CoreUpdaterValidator>().AddOptionsWithValidateOnStart<CoreUpdaterSettings>();
        services.AddSingleton<IValidateOptions<AppStats>, AppStatsValidator>().AddOptionsWithValidateOnStart<AppStats>();

        services.AddOptions<AzzyBotSettings>().BindConfiguration(nameof(AzzyBotSettings));
        services.AddOptions<DatabaseSettings>().BindConfiguration(nameof(DatabaseSettings));
        services.AddOptions<DiscordStatusSettings>().BindConfiguration(nameof(DiscordStatusSettings));
        services.AddOptions<MusicStreamingSettings>().BindConfiguration(nameof(MusicStreamingSettings));
        services.AddOptions<CoreUpdaterSettings>().BindConfiguration(nameof(CoreUpdaterSettings));
        services.AddOptions<AppStats>().BindConfiguration(nameof(AppStats));
    }

    private static void DiscordClient(this IServiceCollection services, string token)
    {
        services.AddDiscordClient(token, DiscordIntents.Guilds | DiscordIntents.GuildVoiceStates);
        services.ConfigureEventHandlers(static e => e.AddEventHandlers<DiscordGuildsHandler>(ServiceLifetime.Singleton));
    }

    private static void DiscordClientCommands(this IServiceCollection services, AzzyBotSettings settings)
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
