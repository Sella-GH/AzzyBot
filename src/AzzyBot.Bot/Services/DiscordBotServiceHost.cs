using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Bot.Settings;
using AzzyBot.Core.Logging;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services;

public sealed class DiscordBotServiceHost(ILogger<DiscordBotServiceHost> logger, AzzyBotSettingsRecord settings, DiscordClient client) : IHostedService
{
    private readonly ILogger<DiscordBotServiceHost> _logger = logger;
    private readonly AzzyBotSettingsRecord _settings = settings;
    private readonly DiscordClient _client = client;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DiscordActivity activity = DiscordBotService.SetBotStatusActivity(_settings.DiscordStatus?.Activity ?? 2, _settings.DiscordStatus?.Doing ?? "Music", _settings.DiscordStatus?.StreamUrl);
        DiscordUserStatus status = DiscordBotService.SetBotStatusUserStatus(_settings.DiscordStatus?.Status ?? 1);

        await _client.ConnectAsync(activity, status);

        string invite = _client.CurrentApplication.GenerateOAuthUri(null, DiscordPermissions.AccessChannels | DiscordPermissions.AttachFiles | DiscordPermissions.SendMessages, DiscordOAuthScope.ApplicationsCommands, DiscordOAuthScope.Bot);
        _logger.InviteUrl(invite);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _client.DisconnectAsync();
    }
}
