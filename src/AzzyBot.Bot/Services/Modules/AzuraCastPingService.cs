using System;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;

using AzzyBot.Bot.Logging;
using AzzyBot.Bot.Models.AzuraCast;
using AzzyBot.Bot.Services.Interfaces;
using AzzyBot.Bot.Services.Modules.Interfaces;
using AzzyBot.Core.Utilities;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services.Interfaces;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.Modules;

public sealed class AzuraCastPingService(ILogger<AzuraCastPingService> logger, IAzuraCastApiService azuraCast, IDbActions dbActions, IDiscordBotService discordBotService) : IAzuraCastPingService
{
    private readonly ILogger<AzuraCastPingService> _logger = logger;
    private readonly IAzuraCastApiService _azuraCast = azuraCast;
    private readonly IDbActions _dbActions = dbActions;
    private readonly IDiscordBotService _botService = discordBotService;

    public async Task PingInstanceAsync(AzuraCastEntity azuraCast)
    {
        ArgumentNullException.ThrowIfNull(azuraCast);

        Uri uri = new(Crypto.Decrypt(azuraCast.BaseUrl));
        AzuraStatusModel? status = null;
        try
        {
            status = await _azuraCast.GetInstanceStatusAsync(uri);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            string message = $"AzuraCast instance **{uri}** is **down**!";
            if (ex.InnerException is AuthenticationException)
            {
                _logger.BackgroundServiceInstanceStatus(azuraCast.GuildId, azuraCast.Id, "invalid because of a self-signed certificate");
                message = $"The certificate for AzuraCast instance **{uri}** is self-signed and therefore not valid!\nYou need a valid HTTPS certificate for your AzuraCast instance so I can safely connect to it.";
            }
            // Because somebody actually managed it to provide a malformed URL...
            else if (uri.OriginalString != uri.GetLeftPart(UriPartial.Authority))
            {
                _logger.BackgroundServiceInstanceStatus(azuraCast.GuildId, azuraCast.Id, "invalid because of malformed url");
                message = $"I am **unable to establish a connection** to your AzuraCast instance **{uri}**! Make sure you only provide the URL to your instance (e.g. https://demo.azuracast.com) without anything behind!";
            }
            else
            {
                _logger.BackgroundServiceInstanceStatus(azuraCast.GuildId, azuraCast.Id, "offline");
            }

            await _dbActions.UpdateAzuraCastAsync(azuraCast.Guild.UniqueId, isOnline: false);
            await _botService.SendMessageAsync(azuraCast.Preferences.OutagesChannelId, message);
        }

        if (status is not null)
        {
            _logger.BackgroundServiceInstanceStatus(azuraCast.GuildId, azuraCast.Id, "online");

            if (!azuraCast.IsOnline)
            {
                await _dbActions.UpdateAzuraCastAsync(azuraCast.Guild.UniqueId, isOnline: true);
                await _botService.SendMessageAsync(azuraCast.Preferences.OutagesChannelId, $"AzuraCast instance **{uri}** is **up** again!");
            }
        }
        else
        {
            _logger.BackgroundServiceInstanceStatus(azuraCast.GuildId, azuraCast.Id, "unknown or offline");
        }

        await _dbActions.UpdateAzuraCastChecksAsync(azuraCast.Guild.UniqueId, updateLastServerStatusCheck: true);
    }
}
