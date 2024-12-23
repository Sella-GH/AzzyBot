using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AzzyBot.Bot.Services;
using AzzyBot.Bot.Services.Modules;
using AzzyBot.Bot.Utilities.Records.AzuraCast;
using AzzyBot.Core.Logging;
using AzzyBot.Core.Utilities.Encryption;
using AzzyBot.Data.Entities;
using AzzyBot.Data.Services;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Commands.Autocompletes;

public sealed class AzuraCastSystemLogAutocomplete(ILogger<AzuraCastSystemLogAutocomplete> logger, AzuraCastApiService azuraCastApi, AzuraCastPingService azuraCastPing, DbActions dbActions, DiscordBotService botService) : IAutoCompleteProvider
{
    private readonly ILogger<AzuraCastSystemLogAutocomplete> _logger = logger;
    private readonly AzuraCastApiService _azuraCastApi = azuraCastApi;
    private readonly AzuraCastPingService _azuraCastPing = azuraCastPing;
    private readonly DbActions _dbActions = dbActions;
    private readonly DiscordBotService _botService = botService;

    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Guild);

        AzuraCastEntity? azuraCast = await _dbActions.GetAzuraCastAsync(context.Guild.Id);
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
        AzuraSystemLogsRecord? systemLogs;
        try
        {
            systemLogs = await _azuraCastApi.GetSystemLogsAsync(baseUrl, apiKey);
            if (systemLogs is null)
            {
                await _botService.SendMessageAsync(azuraCast.Preferences.NotificationChannelId, $"I don't have the permission to access the administrative **system logs** endpoint.\n{AzuraCastApiService.AzuraCastPermissionsWiki}");
                return [];
            }
        }
        catch (HttpRequestException)
        {
            await _azuraCastPing.PingInstanceAsync(azuraCast);
            return [];
        }

        List<DiscordAutoCompleteChoice> results = new(25);
        foreach (AzuraSystemLogEntryRecord log in systemLogs.Logs)
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
