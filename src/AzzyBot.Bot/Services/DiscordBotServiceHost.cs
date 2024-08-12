using System;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Bot.Settings;
using AzzyBot.Core.Logging;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services;

public sealed class DiscordBotServiceHost(ILogger<DiscordBotServiceHost> logger, AzzyBotSettingsRecord settings, DiscordBotService botService, DiscordClient client) : IHostedService
{
    private readonly ILogger<DiscordBotServiceHost> _logger = logger;
    private readonly AzzyBotSettingsRecord _settings = settings;
    private readonly DiscordBotService _botService = botService;
    private readonly DiscordClient _client = client;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _client.ConnectAsync();

        _logger.BotReady();
        string invite = _client.CurrentApplication.GenerateOAuthUri(null, DiscordPermissions.AccessChannels | DiscordPermissions.AttachFiles | DiscordPermissions.SendMessages, [DiscordOAuthScope.ApplicationsCommands, DiscordOAuthScope.Bot]);
        _logger.InviteUrl(invite);

        // Wait 3 Seconds to let the client boot up
        await Task.Delay(3000, cancellationToken);

        int activity = _settings.DiscordStatus?.Activity ?? 2;
        string doing = _settings.DiscordStatus?.Doing ?? "Music";
        int status = _settings.DiscordStatus?.Status ?? 1;
        Uri? url = _settings.DiscordStatus?.StreamUrl;

        await _botService.SetBotStatusAsync(status, activity, doing, url);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _client.DisconnectAsync();
    }
}
