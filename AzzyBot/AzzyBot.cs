using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.ExceptionHandling;
using AzzyBot.Logging;
using AzzyBot.Modules;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Settings;
using AzzyBot.Strings;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Lavalink4NET;
using Lavalink4NET.Extensions;
using Lavalink4NET.InactivityTracking;
using Lavalink4NET.InactivityTracking.Extensions;
using Lavalink4NET.InactivityTracking.Trackers.Idle;
using Lavalink4NET.InactivityTracking.Trackers.Users;
using Lavalink4NET.Integrations.LyricsJava.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace AzzyBot;

internal sealed class AzzyBot
{
    private static DiscordClient? DiscordClient;
    private static SlashCommandsExtension? SlashCommands;
    private static IAudioService? AudioService;
    private static IServiceCollection? ServiceCollection;
    private static readonly ILogger<AzzyBot> Logger = LoggerBase.GetLogger;

    internal static string GetDiscordClientAvatarUrl => DiscordClient?.CurrentUser.AvatarUrl ?? throw new InvalidOperationException("DiscordClient is null");
    internal static ulong GetDiscordClientId => DiscordClient?.CurrentUser.Id ?? throw new InvalidOperationException("DiscordClient is null");
    internal static string GetDiscordClientUserName => DiscordClient?.CurrentUser.Username ?? throw new InvalidOperationException("DiscordClient is null");
    internal static IReadOnlyDictionary<ulong, DiscordGuild> GetDiscordClientGuilds => DiscordClient?.Guilds ?? throw new InvalidOperationException("DiscordClient is null");
    internal static ILogger<BaseDiscordClient> GetDiscordClientLogger => DiscordClient?.Logger ?? throw new InvalidOperationException("DiscordClient is null");
    internal static string GetDiscordClientVersion => DiscordClient?.VersionString ?? throw new InvalidOperationException("DiscordClient is null");
    internal static IAudioService GetAudioService => AudioService ?? throw new InvalidOperationException("AudioService is null");

    private static async Task Main()
    {
        #region Add Logging

        LoggerBase.CreateLogger(CoreAzzyStatsGeneral.GetBotName);

        #endregion Add Logging

        #region Add Exception Handler

        LoggerBase.LogDebug(Logger, "Adding Exception Handler", null);
        AppDomain.CurrentDomain.UnhandledException += GlobalExceptionHandler;

        #endregion Add Exception Handler

        #region Add basic startup information

        LoggerBase.LogInfo(Logger, $"Starting {CoreAzzyStatsGeneral.GetBotName} in version {CoreAzzyStatsGeneral.GetBotVersion} on {CoreMisc.GetOperatingSystem}-{CoreMisc.GetOperatingSystemArch}", null);

        #endregion Add basic startup information

        #region Add OS Architecture Check

        LoggerBase.LogDebug(Logger, "Checking OS architecture", null);

        if (!CoreMisc.CheckCorrectArchitecture())
        {
            LoggerBase.LogError(Logger, "You can only run this bot with a 64-bit architecture OS!\nUse either an ARM64 or x64 based processor.", null);
            Environment.Exit(0);
        }

        LoggerBase.LogDebug(Logger, "OS architecture check passed", null);

        #endregion Add OS Architecture Check

        #region Initialize .json Settings

        LoggerBase.LogDebug(Logger, "Loading settings", null);
        await BaseSettings.LoadSettingsAsync();

        #endregion Initialize .json Settings

        #region Initialize client

        LoggerBase.LogDebug(Logger, "Creating DiscordClient", null);
        DiscordClient = InitializeBot();
        LoggerBase.LogInfo(Logger, "DiscordClient loaded", null);

        #endregion Initialize client

        #region Initialize the modules

        LoggerBase.LogDebug(Logger, "Adding modules", null);
        BaseModule.RegisterAllModules();

        #endregion Initialize the modules

        #region Initialize Strings

        LoggerBase.LogDebug(Logger, "Initializing strings", null);
        await BaseStringBuilder.LoadStringsAsync();

        #endregion Initialize Strings

        #region Initialize file lockings

        LoggerBase.LogDebug(Logger, "Registering all file locks", null);
        BaseModule.RegisterAllFileLocks();

        #endregion Initialize file lockings

        #region Initialize Slash Commands

        LoggerBase.LogDebug(Logger, "Initializing SlashCommands", null);
        SlashCommands = DiscordClient.UseSlashCommands();
        BaseModule.RegisterAllCommands(SlashCommands, CoreSettings.ServerId);
        LoggerBase.LogInfo(Logger, "SlashCommands added", null);

        #endregion Initialize Slash Commands

        #region Initialize Events

        LoggerBase.LogDebug(Logger, "Adding EventHandlers", null);
        AddEventHandlers();

        #endregion Initialize Events

        #region Initialize Interactivity

        if (ModuleStates.AzuraCast)
        {
            LoggerBase.LogDebug(Logger, "Configuring interactivity", null);
            DiscordClient.UseInteractivity(InitializeInteractivity());
        }

        #endregion Initialize Interactivity

        #region Initialize Processes

        if (ModuleStates.MusicStreaming)
        {
            LoggerBase.LogDebug(Logger, "Starting all processes", null);
            BaseModule.StartAllProcesses();
            await Task.Delay(3000);
            LoggerBase.LogInfo(Logger, "All processes started", null);
        }

        #endregion Initialize Processes

        #region Initialize Lavalink

        if (ModuleStates.MusicStreaming)
            await InitializeLavalink4NetAsync();

        #endregion Initialize Lavalink

        #region Connecting to Gateway

        LoggerBase.LogDebug(Logger, "Connecting to Discord Gateway", null);
        (UserStatus status, DiscordActivity activity) = InitBotStatus(CoreSettings.BotStatus, CoreSettings.BotActivity, CoreSettings.BotDoing, CoreSettings.BotStreamUrl);
        await DiscordClient.ConnectAsync(activity, status);
        LoggerBase.LogInfo(Logger, "Discord Gateway connection established", null);

        #endregion Connecting to Gateway

        #region Initialize Timers

        LoggerBase.LogDebug(Logger, "Starting timers", null);
        BaseModule.StartAllTimers();

        #endregion Initialize Timers

        #region Finalizing

        LoggerBase.LogInfo(Logger, $"{nameof(AzzyBot)} is ready", null);
        await Task.Delay(-1);

        #endregion Finalizing
    }

    private static void GlobalExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        Exception ex = (Exception)e.ExceptionObject;

        LoggerBase.LogCrit(Logger, "Global exception found!", ex);
    }

    private static async Task GuildCreatedAsync(DiscordClient c, GuildCreateEventArgs e)
    {
        LoggerBase.LogInfo(Logger, "Bot joined a Guild!", null);

        if (c.Guilds.Count <= 1)
            return;

        LoggerBase.LogWarn(Logger, "The bot is now in 2 guilds, self-kick initiated.", null);
        await e.Guild.LeaveAsync();
    }

    private static async Task GuildDownloadedAsync(DiscordClient c, GuildDownloadCompletedEventArgs e)
    {
        if (e.Guilds.Count <= 1)
        {
            bool channelsExist = true;

            foreach (KeyValuePair<ulong, DiscordGuild> guild in e.Guilds)
            {
                channelsExist = BaseSettings.CheckIfChannelsExist(guild.Value);
            }

            if (!channelsExist)
            {
                LoggerBase.LogError(Logger, "At least one of the specificied channels in the settings file does not exist!", null);
                await BotShutdownAsync();
            }

            return;
        }

        LoggerBase.LogCrit(Logger, "The bot is joined on more guilds than one. Please remove the bot out of every guild until only 1 is left!", null);
        await BotShutdownAsync();
    }

    private static void AddEventHandlers()
    {
        ArgumentNullException.ThrowIfNull(DiscordClient);
        ArgumentNullException.ThrowIfNull(SlashCommands);

        DiscordClient.ClientErrored += DiscordClientError.DiscordErrorAsync;
        DiscordClient.GuildDownloadCompleted += GuildDownloadedAsync;
        DiscordClient.GuildCreated += GuildCreatedAsync;
        SlashCommands.SlashCommandErrored += SlashCommandError.SlashErrorAsync;
        SlashCommands.AutocompleteErrored += SlashCommandError.AutocompleteErrorAsync;
        AppDomain.CurrentDomain.ProcessExit += ProcessExit;
        Console.CancelKeyPress += ConsoleKeyShutdown;
    }

    private static void RemoveEventHandlers()
    {
        ArgumentNullException.ThrowIfNull(DiscordClient);
        ArgumentNullException.ThrowIfNull(SlashCommands);

        DiscordClient.ClientErrored -= DiscordClientError.DiscordErrorAsync;
        DiscordClient.GuildDownloadCompleted -= GuildDownloadedAsync;
        DiscordClient.GuildCreated -= GuildCreatedAsync;
        SlashCommands.SlashCommandErrored -= SlashCommandError.SlashErrorAsync;
        SlashCommands.AutocompleteErrored -= SlashCommandError.AutocompleteErrorAsync;
        AppDomain.CurrentDomain.ProcessExit -= ProcessExit;
        Console.CancelKeyPress -= ConsoleKeyShutdown;
    }

    private static async void ProcessExit(object? c, EventArgs e)
    {
        LoggerBase.LogInfo(Logger, "Process exit requested by AppDomain.CurrentDomain.ProcessExit", null);
        await BotShutdownAsync();
    }

    private static async void ConsoleKeyShutdown(object? c, ConsoleCancelEventArgs e)
    {
        LoggerBase.LogInfo(Logger, "Process exit requested by Console.CancelKeyPress", null);
        await BotShutdownAsync();
    }

    private static DiscordClient InitializeBot()
    {
        // DiscordBot config
        DiscordConfiguration config = new()
        {
            Token = CoreSettings.BotToken,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers,
            LoggerFactory = LoggerBase.GetLoggerFactory
        };

        return new(config);
    }

    /// <summary>
    /// Sets the bot's status asynchronously.
    /// </summary>
    /// <param name="status">The status of the bot. This should be an integer representation of the UserStatus enum.</param>
    /// <param name="type">The type of activity the bot is doing. This should be an integer representation of the ActivityType enum.</param>
    /// <param name="doing">A string describing what the bot is doing.</param>
    /// <param name="url">An optional URL for the bot's activity. This is only used if the bot's activity type is streaming.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    internal static async Task SetBotStatusAsync(int status, int type, string doing, string? url = null)
    {
        (UserStatus bStatus, DiscordActivity activity) = InitBotStatus(status, type, doing, url);
        await ChangeBotStatusAsync(bStatus, activity);
    }

    /// <summary>
    /// Initializes the bot's status.
    /// </summary>
    /// <param name="status">The status of the bot. This should be an integer representation of the UserStatus enum.</param>
    /// <param name="type">The type of activity the bot is doing. This should be an integer representation of the ActivityType enum.</param>
    /// <param name="doing">A string describing what the bot is doing.</param>
    /// <param name="url">An optional URL for the bot's activity. This is only used if the bot's activity type is streaming.</param>
    /// <returns>A tuple containing the UserStatus and DiscordActivity.</returns>
    private static (UserStatus, DiscordActivity) InitBotStatus(int status, int type, string doing, string? url = null)
    {
        UserStatus bStatus = (UserStatus)Enum.ToObject(typeof(UserStatus), status);
        ActivityType bType = (ActivityType)Enum.ToObject(typeof(ActivityType), type);

        // If bot is "streaming" and no url is given, change to playing
        if (bType.Equals(ActivityType.Streaming) && string.IsNullOrWhiteSpace(url))
            bType = ActivityType.Playing;

        DiscordActivity activity = new(doing, bType);

        // Only add this if the bot is "streaming" and it's a Twitch or YouTube url
        if (bType.Equals(ActivityType.Streaming) && !string.IsNullOrWhiteSpace(url) && (url.Contains("twitch", StringComparison.InvariantCultureIgnoreCase) || url.Contains("youtube", StringComparison.InvariantCultureIgnoreCase)))
            activity.StreamUrl = url;

        return (bStatus, activity);
    }

    /// <summary>
    /// Changes the bot's status asynchronously.
    /// </summary>
    /// <param name="status">The status of the bot. This should be a UserStatus enum.</param>
    /// <param name="activity">The activity of the bot. This should be a DiscordActivity object.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    private static async Task ChangeBotStatusAsync(UserStatus status, DiscordActivity activity)
    {
        ArgumentNullException.ThrowIfNull(DiscordClient);

        await DiscordClient.UpdateStatusAsync(activity, status);
    }

    private static InteractivityConfiguration InitializeInteractivity()
    {
        InteractivityConfiguration interactivityConfiguration = new()
        {
            ResponseBehavior = InteractionResponseBehavior.Ignore,
            ResponseMessage = "This is not a valid option!",
            Timeout = TimeSpan.FromMinutes(1)
        };

        return new(interactivityConfiguration);
    }

    private static async Task InitializeLavalink4NetAsync()
    {
        ArgumentNullException.ThrowIfNull(DiscordClient);

        LoggerBase.LogDebug(Logger, "Initializing Lavalink4NET", null);

        ServiceCollection = new ServiceCollection().AddLavalink().AddSingleton(DiscordClient).ConfigureLavalink(config =>
        {
            config.BaseAddress = (CoreAzzyStatsGeneral.GetBotName is "AzzyBot-Docker") ? new Uri("http://lavalink:2333") : new Uri("http://localhost:2333");
            config.Label = "AzzyBot";
            config.ReadyTimeout = TimeSpan.FromSeconds(15);
            config.ResumptionOptions = new(TimeSpan.Zero);
        });

        LogLevel level = LogLevel.Information;
        if (CoreAzzyStatsGeneral.GetBotName is "AzzyBot-Dev")
            level = LogLevel.Debug;

        ServiceCollection.AddLogging(x => x.AddConsole().SetMinimumLevel(level).AddSimpleConsole(options =>
        {
            options.ColorBehavior = LoggerColorBehavior.Enabled;
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
            options.UseUtcTimestamp = true;
        }));

        if (CoreModule.GetMusicStreamingInactivity())
        {
            ServiceCollection.AddInactivityTracking();
            ServiceCollection.ConfigureInactivityTracking(config =>
            {
                config.DefaultTimeout = TimeSpan.FromMinutes(CoreModule.GetMusicStreamingInactivityTime());
                config.TrackingMode = InactivityTrackingMode.Any;
                config.UseDefaultTrackers = true;
            });
            ServiceCollection.AddInactivityTracker<IdleInactivityTracker>();
            ServiceCollection.AddInactivityTracker<UsersInactivityTracker>();
            ServiceCollection.Configure<IdleInactivityTrackerOptions>(config => config.TrackNewPlayers = false);

            LoggerBase.LogDebug(Logger, "Applied inactivity tracking to Lavalink4NET", null);
        }

        IServiceProvider serviceProvider = ServiceCollection.BuildServiceProvider();

        foreach (IHostedService hostedService in serviceProvider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(new CancellationToken());
        }

        AudioService = serviceProvider.GetRequiredService<IAudioService>();

        if (CoreModule.GetMusicStreamingLyrics())
        {
            AudioService.UseLyricsJava();
            LoggerBase.LogDebug(Logger, "Applied Lyrics.Java to Lavalink4NET", null);
        }

        LoggerBase.LogInfo(Logger, "Lavalink4NET loaded", null);
    }

    internal static async Task BotShutdownAsync()
    {
        if (ModuleStates.MusicStreaming && ServiceCollection is not null)
        {
            IServiceProvider serviceProvider = ServiceCollection.BuildServiceProvider();

            foreach (IHostedService hostedService in serviceProvider.GetServices<IHostedService>())
            {
                await hostedService.StopAsync(new CancellationToken());
            }
        }

        BaseModule.StopAllTimers();
        LoggerBase.LogDebug(Logger, "Stopped all timers", null);

        BaseModule.StopAllProcesses();
        LoggerBase.LogInfo(Logger, "Stopped all processes", null);

        BaseModule.DisposeAllFileLocks();
        LoggerBase.LogDebug(Logger, "Disposed all file locks", null);

        if (DiscordClient is not null)
        {
            await DiscordClient.DisconnectAsync();
            DiscordClient.Dispose();
            DiscordClient = null;
            LoggerBase.LogInfo(Logger, "DiscordClient disposed", null);

            RemoveEventHandlers();
            LoggerBase.LogDebug(Logger, "Removed EventHandlers", null);
        }

        if (SlashCommands is not null)
        {
            SlashCommands.Dispose();
            SlashCommands = null;
            LoggerBase.LogInfo(Logger, "SlashCommands disposed", null);
        }

        LoggerBase.DisposeLogger();
        Environment.Exit(0);
    }

    /// <summary>
    /// Sends a message to a specific channel asynchronously.
    /// </summary>
    /// <param name="channelId">The ID of the channel to send the message to.</param>
    /// <param name="content">The text content of the message. This is optional.</param>
    /// <param name="embeds">A List of DiscordEmbeds to include in the message. This is optional.</param>
    /// <param name="mention">A boolean indicating whether to allow mentions in this message. This is optional.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    internal static async Task<DiscordMessage> SendMessageAsync(ulong channelId, string content = "", List<DiscordEmbed?>? embeds = null, bool mention = false)
    {
        ArgumentNullException.ThrowIfNull(DiscordClient);
        ArgumentOutOfRangeException.ThrowIfZero(channelId);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(embeds?.Count ?? 0, 10, nameof(embeds));

        DiscordMessageBuilder builder = new();

        if (!string.IsNullOrWhiteSpace(content))
            builder.WithContent(content);

        if (mention)
            builder.WithAllowedMentions([EveryoneMention.All, RepliedUserMention.All, RoleMention.All, UserMention.All]);

        if (embeds?.Count > 0)
            builder.AddEmbeds(embeds);

        DiscordChannel channel = await DiscordClient.GetChannelAsync(channelId);
        return await channel.SendMessageAsync(builder);
    }

    /// <summary>
    /// Sends a message with multiple attached files to a specific channel asynchronously.
    /// </summary>
    /// <param name="channelId">The ID of the channel to send the message to.</param>
    /// <param name="content">The text content of the message.</param>
    /// <param name="embed">A DiscordEmbed to include in the message.</param>
    /// <param name="fileNames">An array of file names to attach to the message.</param>
    /// <param name="mention">A boolean indicating whether to allow mentions in this message. This is optional.</param>
    /// <returns>A Task representing the asynchronous operation. The task result is a boolean indicating whether the operation was successful.</returns>
    /// <exception cref="IOException">Throws when the file can not be deleted.</exception>
    internal static async Task<bool> SendMessageAsync(ulong channelId, string content, DiscordEmbed embed, string[] fileNames, bool mention = false)
    {
        ArgumentNullException.ThrowIfNull(DiscordClient);
        ArgumentOutOfRangeException.ThrowIfZero(channelId);
        ArgumentNullException.ThrowIfNull(fileNames);
        ArgumentOutOfRangeException.ThrowIfZero(fileNames.Length);

        try
        {
            DiscordMessageBuilder builder = new();
            if (!string.IsNullOrWhiteSpace(content))
                builder.WithContent(content);

            if (mention)
                builder.WithAllowedMentions([EveryoneMention.All, RepliedUserMention.All, RoleMention.All, UserMention.All]);

            builder.AddEmbed(embed);

            List<FileStream> streams = [];
            foreach (string fileName in fileNames)
            {
                FileStream stream = new(fileName, FileMode.Open, FileAccess.Read);
                streams.Add(stream);
                builder.AddFile(Path.GetFileName(fileName), stream);
            }

            DiscordChannel channel = await DiscordClient.GetChannelAsync(channelId);
            await channel.SendMessageAsync(builder);

            foreach (FileStream stream in streams)
            {
                await stream.DisposeAsync();
            }

            foreach (string fileName in fileNames)
            {
                if (!CoreFileOperations.DeleteTempFile(fileName))
                    throw new IOException($"{fileName} couldn't be deleted!");
            }

            return true;
        }
        catch (DirectoryNotFoundException)
        { }
        catch (FileNotFoundException)
        { }
        catch (UnauthorizedAccessException)
        { }

        return false;
    }
}
