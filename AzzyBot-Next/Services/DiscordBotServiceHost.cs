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
    private readonly DiscordShardedClient _shardedClient;

    public DiscordBotServiceHost(IConfiguration config, ILogger<DiscordBotServiceHost> logger, ILoggerFactory loggerFactory)
    {
        _configuration = config;
        _logger = logger;

        string botToken = _configuration["BotToken"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(botToken))
        {
            _logger.BotTokenInvalid();
            Environment.Exit(1);
        }

        DiscordConfiguration discordConfig = new()
        {
            Intents = DiscordIntents.AllUnprivileged,
            LoggerFactory = loggerFactory,
            Token = botToken,
            TokenType = TokenType.Bot
        };

        _shardedClient = new(discordConfig);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _shardedClient.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _shardedClient.StopAsync();
    }
}
