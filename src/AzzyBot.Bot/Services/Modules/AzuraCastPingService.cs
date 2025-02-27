using System;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;

using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.Modules;

public sealed class AzuraCastPingService(ILogger<AzuraCastPingService> logger, AzuraCastApiService azuraCast, DbActions dbActions, DiscordBotService discordBotService)
{
    private readonly ILogger<AzuraCastPingService> _logger = logger;
    private readonly AzuraCastApiService _azuraCast = azuraCast;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = discordBotService;
    private const string ValidCertNeeded = "The certificate for AzuraCast instance **URI** is self-signed and therefore not valid!\nYou need a valid HTTPS certificate for your AzuraCast instance so AzzyBot can safely connect to it.";

    public async Task PingInstanceAsync(AzuraCastEntity azuraCast)
    {
        ArgumentNullException.ThrowIfNull(azuraCast);

        Uri uri = new(Crypto.Decrypt(azuraCast.BaseUrl));
        AzuraStatusRecord? status = null;
        try
        {
            status = await _azuraCast.GetInstanceStatusAsync(uri);
        }
        catch (HttpRequestException ex)
        {
            string message = $"AzuraCast instance **{uri}** is **down**!";
            if (ex.InnerException is AuthenticationException)
            {
                _logger.BackgroundServiceInstanceStatus(azuraCast.GuildId, azuraCast.Id, "invalid because of a self-signed certificate");
                message = ValidCertNeeded.Replace("**URI**", uri.ToString(), StringComparison.OrdinalIgnoreCase);
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
            _logger.BackgroundServiceInstanceStatus(azuraCast.GuildId, azuraCast.Id, "unkown or offline");
        }

        await _dbActions.UpdateAzuraCastChecksAsync(azuraCast.Guild.UniqueId, lastServerStatusCheck: true);
    }
}
