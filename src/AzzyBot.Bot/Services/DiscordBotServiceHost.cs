using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Bot.Settings;
using AzzyBot.Core.Logging;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzzyBot.Bot.Services;

public sealed class DiscordBotServiceHost(ILogger<DiscordBotServiceHost> logger, IOptions<DiscordStatusSettings> settings, DiscordClient client) : IHostedService
{
    private readonly ILogger<DiscordBotServiceHost> _logger = logger;
    private readonly DiscordStatusSettings _settings = settings.Value;
    private readonly DiscordClient _client = client;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DiscordActivity activity = DiscordBotService.SetBotStatusActivity(_settings.Activity, _settings.Doing, _settings.StreamUrl);
        DiscordUserStatus status = DiscordBotService.SetBotStatusUserStatus(_settings.Status);

        await _client.ConnectAsync(activity, status);

        string invite = _client.CurrentApplication.GenerateOAuthUri(null, new(DiscordPermission.AttachFiles, DiscordPermission.SendMessages, DiscordPermission.ViewChannel), DiscordOAuthScope.ApplicationsCommands, DiscordOAuthScope.Bot);
        _logger.InviteUrl(invite);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _client.DisconnectAsync();
    }
}
