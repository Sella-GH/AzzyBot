using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Commands;
using AzzyBot.Logging;
using AzzyBot.Settings;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services;

internal sealed class DiscordBotServiceHost : IHostedService
{
    private readonly ILogger<DiscordBotServiceHost> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly AzzyBotSettings? _settings;
    private readonly DiscordShardedClient _shardedClient;

    public DiscordBotServiceHost(IConfiguration config, ILogger<DiscordBotServiceHost> logger, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
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

        foreach (CommandsExtension commandsExtension in commandsExtensions.Values)
        {
            commandsExtension.AddCommands(typeof(AzzyBot).Assembly);
            SlashCommandProcessor slashCommandProcessor = new();
            await commandsExtension.AddProcessorsAsync(slashCommandProcessor);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await RegisterCommandsAsync();
        await _shardedClient.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _shardedClient.StopAsync();
    }
}
