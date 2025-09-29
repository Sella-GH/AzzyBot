using System;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.EventArgs;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.DiscordEvents;

public sealed class DiscordMessageHandlers(ILogger<DiscordMessageHandlers> logger, DiscordBotService botService)
    : IEventHandler<ModalSubmittedEventArgs>
{
    private readonly ILogger _logger = logger;
    private readonly DiscordBotService _botService = botService;

    public async Task HandleEventAsync(DiscordClient sender, ModalSubmittedEventArgs eventArgs)
    {
        ArgumentNullException.ThrowIfNull(eventArgs);

        string modalId = eventArgs.Id;
        if (modalId.StartsWith($"modal_SendBotWideMessageAsync_", StringComparison.OrdinalIgnoreCase))
        {
            
        }
    }
}
