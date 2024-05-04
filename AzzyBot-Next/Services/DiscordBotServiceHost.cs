using System;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Logging;
using DSharpPlus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Services;

internal sealed class DiscordBotServiceHost : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DiscordBotServiceHost> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly DiscordShardedClient _shardedClient;

    public DiscordBotServiceHost(IConfiguration config, ILogger<DiscordBotServiceHost> logger, ILoggerFactory loggerFactory)
    {
        _configuration = config;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _shardedClient = new(GetDiscordConfig());
    }

    private DiscordConfiguration GetDiscordConfig()
    {
        string botToken = _configuration["BotToken"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(botToken))
        {
            _logger.BotTokenInvalid();
            Environment.Exit(1);
        }

        return new()
        {
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers,
            LoggerFactory = _loggerFactory,
            Token = botToken,
            TokenType = TokenType.Bot
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _shardedClient.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _shardedClient.StopAsync();
    }
}
