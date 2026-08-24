using System.Threading;
using System.Threading.Tasks;

using AzzyBot.Bot.Logging;
using AzzyBot.Bot.Services.Interfaces;
using AzzyBot.Bot.Settings;

using DSharpPlus;
using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzzyBot.Bot.Services;

public sealed class DiscordBotServiceHost(ILogger<DiscordBotServiceHost> logger, IOptions<DiscordStatusSettings> settings, IDiscordBotService botService, DiscordClient client) : IDiscordBotServiceHost
{
    private readonly ILogger<DiscordBotServiceHost> _logger = logger;
    private readonly DiscordStatusSettings _settings = settings.Value;
    private readonly IDiscordBotService _botService = botService;
    private readonly DiscordClient _client = client;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DiscordActivity activity = _botService.SetBotStatusActivity(_settings.Activity, _settings.Doing, _settings.StreamUrl);
        DiscordUserStatus status = _botService.SetBotStatusUserStatus(_settings.Status);

        await _client.ConnectAsync(activity, status);

        string invite = _client.CurrentApplication.GenerateOAuthUri(redirectUri: null, new(DiscordPermission.AttachFiles, DiscordPermission.EmbedLinks, DiscordPermission.SendMessages, DiscordPermission.ViewChannel), DiscordOAuthScope.ApplicationsCommands, DiscordOAuthScope.Bot);
        _logger.InviteUrl(invite);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _client.DisconnectAsync();
    }
}
