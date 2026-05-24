using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using AzzyBot.Bot.Models.AzuraCast;
using AzzyBot.Bot.Services.Interfaces;
using AzzyBot.Bot.Services.Modules.Interfaces;
using AzzyBot.Core.Utilities;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Logging;
using AzzyBot.Data.Services.Interfaces;

using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands.Autocompletes;

public sealed class AzuraCastSystemLogAutocomplete(ILogger<AzuraCastSystemLogAutocomplete> logger, IAzuraCastApiService azuraCastApi, IAzuraCastPingService azuraCastPing, IDbActions dbActions, IDiscordBotService botService) : IAutoCompleteProvider
{
    private readonly ILogger<AzuraCastSystemLogAutocomplete> _logger = logger;
    private readonly IAzuraCastApiService _azuraCastApi = azuraCastApi;
    private readonly IAzuraCastPingService _azuraCastPing = azuraCastPing;
    private readonly IDbActions _dbActions = dbActions;
    private readonly IDiscordBotService _botService = botService;

    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Guild);

        AzuraCastEntity? azuraCast = await _dbActions.ReadAzuraCastAsync(context.Guild.Id);
        if (azuraCast is null)
        {
            _logger.DatabaseAzuraCastNotFound(context.Guild.Id);
            return [];
        }
        else if (!azuraCast.IsOnline)
        {
            return [];
        }

        string? search = context.UserInput;
        Uri baseUrl = new(Crypto.Decrypt(azuraCast.BaseUrl));
        string apiKey = Crypto.Decrypt(azuraCast.AdminApiKey);
        AzuraSystemLogsModel? systemLogs;
        try
        {
            systemLogs = await _azuraCastApi.GetSystemLogsAsync(baseUrl, apiKey);
            if (systemLogs is null)
            {
                await _botService.SendMessageAsync(azuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the administrative **system logs** endpoint.\n{_azuraCastApi.AzuraCastPermissionsWiki}");
                return [];
            }
        }
        catch (HttpRequestException)
        {
            await _azuraCastPing.PingInstanceAsync(azuraCast);
            return [];
        }

        List<DiscordAutoCompleteChoice> results = new(25);
        foreach (AzuraSystemLogEntryModel log in systemLogs.Logs)
        {
            if (results.Count is 25)
                break;

            if (!string.IsNullOrWhiteSpace(search) && !log.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            results.Add(new(log.Name, log.Key));
        }

        return results;
    }
}
