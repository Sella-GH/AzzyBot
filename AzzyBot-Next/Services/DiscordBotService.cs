using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Services.Modules;
using AzzyBot.Settings;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services;

internal sealed class DiscordBotService : BaseService, IHostedService
{
    private readonly ILogger<DiscordBotService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly AzzyBotSettings? _settings;
    private readonly DiscordShardedClient _shardedClient;

    public DiscordBotService(IConfiguration config, ILogger<DiscordBotService> logger, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
    {
        _settings = config.Get<AzzyBotSettings>();
        _logger = logger;
        _loggerFactory = loggerFactory;
        _serviceProvider = serviceProvider;

        if (_settings is null)
        {
            _logger.UnableToParseSettings();
            Environment.Exit(1);
        }

        _shardedClient = new(GetDiscordConfig());
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(_settings, nameof(_settings));

        cancellationToken.ThrowIfCancellationRequested();
        await RegisterCommandsAsync();
        await _shardedClient.StartAsync();

        // Wait 3 Seconds to let the client boot up
        await Task.Delay(3000, cancellationToken);

        int activity = _settings.DiscordStatus.Activity;
        string doing = _settings.DiscordStatus.Doing;
        int status = _settings.DiscordStatus.Status;
        string? url = _settings.DiscordStatus.StreamUrl?.ToString();

        await SetBotStatusAsync(status, activity, doing, url);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _shardedClient.StopAsync();
    }

    internal async Task SetBotStatusAsync(int status, int activityType, string doing, string? url = null)
    {
        DiscordActivity activity = GetDiscordAtivity(activityType, doing, url);
        DiscordUserStatus userStatus = GetDiscordUserStatus(status);

        await ChangeBotStatusAsync(activity, userStatus);
    }

    private async Task ChangeBotStatusAsync(DiscordActivity activity, DiscordUserStatus userStatus) => await _shardedClient.UpdateStatusAsync(activity, userStatus);

    private static DiscordActivity GetDiscordAtivity(int type, string doing, string? url = null)
    {
        DiscordActivityType activityType = (DiscordActivityType)Enum.ToObject(typeof(DiscordActivityType), type);

        if (activityType.Equals(DiscordActivityType.Streaming) && string.IsNullOrWhiteSpace(url))
            activityType = DiscordActivityType.Playing;

        DiscordActivity activity = new(doing, activityType);

        if (activityType.Equals(DiscordActivityType.Streaming) && !string.IsNullOrWhiteSpace(url) && (url.Contains("twitch", StringComparison.OrdinalIgnoreCase) || url.Contains("youtube", StringComparison.OrdinalIgnoreCase)))
            activity.StreamUrl = url;

        return activity;
    }

    private static DiscordUserStatus GetDiscordUserStatus(int status) => (DiscordUserStatus)Enum.ToObject(typeof(DiscordUserStatus), status);

    private DiscordConfiguration GetDiscordConfig()
    {
        ArgumentNullException.ThrowIfNull(_settings, nameof(_settings));

        if (string.IsNullOrWhiteSpace(_settings.BotToken))
        {
            _logger.BotTokenInvalid();
            Environment.Exit(1);
        }

        return new()
        {
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers,
            LoggerFactory = _loggerFactory,
            Token = _settings.BotToken,
            TokenType = TokenType.Bot
        };
    }

    private async Task RegisterCommandsAsync()
    {
        ArgumentNullException.ThrowIfNull(_settings, nameof(_settings));

        IReadOnlyDictionary<int, CommandsExtension> commandsExtensions = await _shardedClient.UseCommandsAsync(new()
            {
                RegisterDefaultCommandProcessors = false,
                ServiceProvider = _serviceProvider
            });

        bool coreService = CheckIfServiceIsRegistered<CoreService>(_serviceProvider);

        foreach (CommandsExtension commandsExtension in commandsExtensions.Values)
        {
            //if (coreService)
                commandsExtension.AddCommands(typeof(AzzyBot).Assembly);

            SlashCommandProcessor slashCommandProcessor = new();
            await commandsExtension.AddProcessorsAsync(slashCommandProcessor);
        }
    }
}
