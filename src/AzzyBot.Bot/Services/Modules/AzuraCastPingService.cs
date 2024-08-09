using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data;
using AzzyBot.Data.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.Modules;

public sealed class AzuraCastPingService(ILogger<AzuraCastPingService> logger, AzuraCastApiService azuraCast, DbActions dbActions, DiscordBotService discordBotService)
{
    private readonly ILogger<AzuraCastPingService> _logger = logger;
    private readonly AzuraCastApiService _azuraCast = azuraCast;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = discordBotService;

    public async Task PingInstanceAsync(AzuraCastEntity azuraCast, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(azuraCast, nameof(azuraCast));

        try
        {
            Uri uri = new(Crypto.Decrypt(azuraCast.BaseUrl));
            AzuraStatusRecord? status = null;
            try
            {
                status = await _azuraCast.GetInstanceStatusAsync(uri);
            }
            catch (HttpRequestException)
            {
                _logger.BackgroundServiceInstanceStatus(azuraCast.GuildId, azuraCast.Id, "offline");

                await _dbActions.UpdateAzuraCastAsync(azuraCast.Guild.UniqueId, isOnline: false);
                await _botService.SendMessageAsync(azuraCast.Preferences.OutagesChannelId, $"AzuraCast instance **{uri}** is **down**!");
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

            await _dbActions.UpdateAzuraCastChecksAsync(azuraCast.Guild.UniqueId, lastServerStatusCheck: DateTime.UtcNow);
        }
        catch (OperationCanceledException)
        {
            _logger.OperationCanceled(nameof(PingInstanceAsync));
        }
    }
}
