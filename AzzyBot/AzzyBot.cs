using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.ExceptionHandling;
using AzzyBot.Modules;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Settings;
using AzzyBot.Modules.Core.Updater;
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
using Lavalink4NET.Integrations.LyricsJava.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot;

internal static class AzzyBot
{
    private static DiscordClient? DiscordClient;
    private static SlashCommandsExtension? SlashCommands;
    private static IAudioService? AudioService;
    private static IServiceCollection? ServiceCollection;

    internal static string GetDiscordClientAvatarUrl => DiscordClient?.CurrentUser.AvatarUrl ?? throw new InvalidOperationException("DiscordClient is null");
    internal static ulong GetDiscordClientId => DiscordClient?.CurrentUser.Id ?? throw new InvalidOperationException("DiscordClient is null");
    internal static string GetDiscordClientUserName => DiscordClient?.CurrentUser.Username ?? throw new InvalidOperationException("DiscordClient is null");
    internal static IReadOnlyDictionary<ulong, DiscordGuild> GetDiscordClientGuilds => DiscordClient?.Guilds ?? throw new InvalidOperationException("DiscordClient is null");
    internal static ILogger<BaseDiscordClient> GetDiscordClientLogger => DiscordClient?.Logger ?? throw new InvalidOperationException("DiscordClient is null");
    internal static string GetDiscordClientVersion => DiscordClient?.VersionString ?? throw new InvalidOperationException("DiscordClient is null");
    internal static IAudioService GetAudioService => AudioService ?? throw new InvalidOperationException("AudioService is null");

    private static async Task Main()
    {
        #region Add basic startup information

        await Console.Out.WriteLineAsync($"Starting {CoreAzzyStatsGeneral.GetBotName} in version {CoreAzzyStatsGeneral.GetBotVersion} on {CoreMisc.GetOperatingSystem}-{CoreMisc.GetOperatingSystemArch}");

        #endregion Add basic startup information

        #region Add OS Architecture Check

        await Console.Out.WriteLineAsync("Checking OS architecture");

        if (!CoreMisc.CheckCorrectArchitecture())
        {
            await Console.Error.WriteLineAsync("You can only run this bot with a 64-bit architecture OS!");
            await Console.Error.WriteLineAsync("Use either an ARM64 or x64 based processor!");
            Environment.Exit(0);
        }

        await Console.Out.WriteLineAsync("OS architecture check passed");

        #endregion Add OS Architecture Check

        #region Add Exception Handler

        await Console.Out.WriteLineAsync("Adding Exception Handler");
        AppDomain.CurrentDomain.UnhandledException += GlobalExceptionHandler;

        #endregion Add Exception Handler

        #region Initialize .json Settings

        await Console.Out.WriteLineAsync("Loading settings");
        await BaseSettings.LoadSettingsAsync();

        #endregion Initialize .json Settings

        #region Initialize client

        await Console.Out.WriteLineAsync("Creating DiscordClient");
        DiscordClient = InitializeBot();
        ExceptionHandler.LogMessage(LogLevel.Debug, "DiscordClient loaded");

        #endregion Initialize client

        #region Initialize the modules

        ExceptionHandler.LogMessage(LogLevel.Debug, "Adding modules");
        BaseModule.RegisterAllModules();

        #endregion Initialize the modules

        #region Initialize file lockings

        ExceptionHandler.LogMessage(LogLevel.Debug, "Registering all file locks");
        BaseModule.RegisterAllFileLocks();

        #endregion Initialize file lockings

        #region Initialize Slash Commands

        ExceptionHandler.LogMessage(LogLevel.Debug, "Initializing SlashCommands");
        SlashCommands = DiscordClient.UseSlashCommands();
        BaseModule.RegisterAllCommands(SlashCommands, CoreSettings.ServerId);

        #endregion Initialize Slash Commands

        #region Initialize Events

        ExceptionHandler.LogMessage(LogLevel.Debug, "Adding EventHandlers");
        AddEventHandlers();

        #endregion Initialize Events

        #region Initialize Interactivity

        ExceptionHandler.LogMessage(LogLevel.Debug, "Configuring interactivity");
        DiscordClient.UseInteractivity(InitializeInteractivity());

        #endregion Initialize Interactivity

        #region Initialize Processes

        ExceptionHandler.LogMessage(LogLevel.Debug, "Starting all processes");
        BaseModule.StartAllProcesses();
        await Task.Delay(3000);
        ExceptionHandler.LogMessage(LogLevel.Debug, "Started all processes");

        #endregion Initialize Processes

        #region Initialize Lavalink

        if (ModuleStates.MusicStreaming)
            await InitializeLavalink4NetAsync();

        #endregion Initialize Lavalink

        #region Connecting to Gateway

        ExceptionHandler.LogMessage(LogLevel.Debug, "Connecting to Discord Gateway");
        (UserStatus status, DiscordActivity activity) = InitBotStatus(CoreSettings.BotStatus, CoreSettings.BotActivity, CoreSettings.BotDoing, CoreSettings.BotStreamUrl);
        await DiscordClient.ConnectAsync(activity, status);

        #endregion Connecting to Gateway

        #region Initialize Strings

        ExceptionHandler.LogMessage(LogLevel.Debug, "Loading strings");
        await BaseStringBuilder.LoadStringsAsync();

        #endregion Initialize Strings

        #region Initialize Timers

        ExceptionHandler.LogMessage(LogLevel.Debug, "Starting timers");
        if (BaseSettings.ActivateTimers)
            BaseModule.StartAllGlobalTimers();

        #endregion Initialize Timers

        #region Check for updates

        ExceptionHandler.LogMessage(LogLevel.Debug, "Checking for updates");
        await Updates.CheckForUpdatesAsync();

        #endregion Check for updates

        #region Finalizing

        ExceptionHandler.LogMessage(LogLevel.Information, "Bot is ready");
        await Task.Delay(-1);

        #endregion Finalizing
    }

    private static void GlobalExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        Exception ex = (Exception)e.ExceptionObject;

        ExceptionHandler.LogMessage(LogLevel.Critical, "Global exception found!");
        ExceptionHandler.LogMessage(LogLevel.Critical, ex.Message);
        ExceptionHandler.LogMessage(LogLevel.Critical, ex.StackTrace ?? "No StackTrace available!");
    }

    private static async Task GuildCreatedAsync(DiscordClient c, GuildCreateEventArgs e)
    {
        ExceptionHandler.LogMessage(LogLevel.Information, "Bot joined a Guild!");

        if (c.Guilds.Count <= 1)
            return;

        ExceptionHandler.LogMessage(LogLevel.Information, "The bot is now in 2 guilds, self-kick initiated.");
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
                await BotShutdownAsync();

            return;
        }

        ExceptionHandler.LogMessage(LogLevel.Critical, "The bot is joined on more guilds than one. Please remove the bot out of every guild until only 1 is left!");
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
        ExceptionHandler.LogMessage(LogLevel.Information, "Process exit requested by AppDomain.CurrentDomain.ProcessExit");
        await BotShutdownAsync();
    }

    private static async void ConsoleKeyShutdown(object? c, ConsoleCancelEventArgs e)
    {
        ExceptionHandler.LogMessage(LogLevel.Information, "Process exit requested by Console.CancelKeyPress");
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
            MinimumLogLevel = (LogLevel)Enum.ToObject(typeof(LogLevel), CoreSettings.LogLevel),
            LogTimestampFormat = "yyyy-MM-dd HH:mm:ss"
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

        ExceptionHandler.LogMessage(LogLevel.Debug, "Initializing Lavalink4NET");
        ServiceCollection = new ServiceCollection().AddLavalink().AddSingleton(DiscordClient).ConfigureLavalink(config =>
        {
            config.BaseAddress = (CoreAzzyStatsGeneral.GetBotName is "AzzyBot-Docker") ? new Uri("http://lavalink:2333") : new Uri("http://localhost:2333");
            config.ReadyTimeout = TimeSpan.FromSeconds(15);
            config.ResumptionOptions = new(TimeSpan.Zero);
            config.Label = "AzzyBot";
        });

        ServiceCollection.AddLogging(x => x.AddConsole().SetMinimumLevel((LogLevel)Enum.ToObject(typeof(LogLevel), CoreSettings.LogLevel)));

        if (CoreModule.GetMusicStreamingInactivity())
        {
            ServiceCollection.AddInactivityTracking();
            ServiceCollection.ConfigureInactivityTracking(config =>
            {
                config.DefaultTimeout = TimeSpan.FromMinutes(CoreModule.GetMusicStreamingInactivityTime());
                config.TrackingMode = InactivityTrackingMode.Any;
                config.UseDefaultTrackers = true;
            });
            ServiceCollection.Configure<IdleInactivityTrackerOptions>(config => config.TrackNewPlayers = false);

            ExceptionHandler.LogMessage(LogLevel.Debug, "Applied inactivity tracking to Lavalink4NET");
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
            ExceptionHandler.LogMessage(LogLevel.Debug, "Applied Lyrics.Java to Lavalink4NET");
        }

        ExceptionHandler.LogMessage(LogLevel.Debug, "Lavalink4NET loaded");
    }

    private static async Task BotShutdownAsync()
    {
        if (ModuleStates.MusicStreaming)
        {
            IServiceProvider serviceProvider = ServiceCollection?.BuildServiceProvider() ?? throw new InvalidOperationException("No services started!");

            foreach (IHostedService hostedService in serviceProvider.GetServices<IHostedService>())
            {
                await hostedService.StopAsync(new CancellationToken());
            }
        }

        BaseModule.StopAllTimers();
        ExceptionHandler.LogMessage(LogLevel.Debug, "Stopped all timers");

        BaseModule.StopAllProcesses();
        ExceptionHandler.LogMessage(LogLevel.Debug, "Stopped all processes");

        BaseModule.DisposeAllFileLocks();
        ExceptionHandler.LogMessage(LogLevel.Debug, "Disposed all file locks");

        RemoveEventHandlers();
        ExceptionHandler.LogMessage(LogLevel.Debug, "Removed EventHandlers");

        if (SlashCommands is not null)
        {
            SlashCommands.Dispose();
            SlashCommands = null;

            ExceptionHandler.LogMessage(LogLevel.Debug, "SlashCommands disposed");
        }

        if (DiscordClient is not null)
        {
            await DiscordClient.DisconnectAsync();
            DiscordClient.Dispose();
            DiscordClient = null;

            await Console.Out.WriteLineAsync("DiscordClient disposed");
        }

        await Console.Out.WriteLineAsync("Ready for exit");
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
    /// Sends a message with an attached file to a specific channel asynchronously.
    /// </summary>
    /// <param name="channelId">The ID of the channel to send the message to.</param>
    /// <param name="content">The text content of the message.</param>
    /// <param name="embed">A DiscordEmbed to include in the message.</param>
    /// <param name="fileName">The name of the file to attach to the message.</param>
    /// <param name="mention">A boolean indicating whether to allow mentions in this message. This is optional.</param>
    /// <returns>A Task representing the asynchronous operation. The task result is a boolean indicating whether the operation was successful.</returns>
    /// <exception cref="IOException">Throws when the file can not be deleted.</exception>
    internal static async Task<bool> SendMessageAsync(ulong channelId, string content, DiscordEmbed embed, string fileName, bool mention = false)
    {
        ArgumentNullException.ThrowIfNull(DiscordClient);
        ArgumentOutOfRangeException.ThrowIfZero(channelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        try
        {
            FileStream stream = new(fileName, FileMode.Open, FileAccess.Read);

            DiscordMessageBuilder builder = new();
            if (!string.IsNullOrWhiteSpace(content))
                builder.WithContent(content);

            if (mention)
                builder.WithAllowedMentions([EveryoneMention.All, RepliedUserMention.All, RoleMention.All, UserMention.All]);

            builder.AddEmbed(embed);
            builder.AddFile(Path.GetFileName(fileName), stream);

            DiscordChannel channel = await DiscordClient.GetChannelAsync(channelId);
            await channel.SendMessageAsync(builder);
            await stream.DisposeAsync();

            return (!CoreFileOperations.DeleteTempFile(fileName))
                ? throw new IOException($"{fileName} couldn't be deleted!")
                : true;
        }
        catch (DirectoryNotFoundException)
        { }
        catch (FileNotFoundException)
        { }
        catch (UnauthorizedAccessException)
        { }

        return false;
    }

    /// <summary>
    /// Sends a message with multiple attached files to a specific channel asynchronously.
    /// </summary>
    /// <param name="channelId">The ID of the channel to send the message to.</param>
    /// <param name="content">The text content of the message.</param>
    /// <param name="embed">A DiscordEmbed to include in the message.</param>
    /// <param name="fileNames">An array of file names to attach to the message.</param>
    /// <returns>A Task representing the asynchronous operation. The task result is a boolean indicating whether the operation was successful.</returns>
    /// <exception cref="IOException">Throws when the file can not be deleted.</exception>
    internal static async Task<bool> SendMessageAsync(ulong channelId, string content, DiscordEmbed embed, string[] fileNames)
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
