using System;
using System.Threading.Tasks;

using AzzyBot.Bot.Services.Interfaces;
using AzzyBot.Core.Logging;

using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using DSharpPlus.Commands.Exceptions;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Exceptions;

using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.DiscordEvents;

public sealed class DiscordCommandsErrorHandler(ILogger<DiscordCommandsErrorHandler> logger, IDiscordBotService botService)
{
    private readonly ILogger<DiscordCommandsErrorHandler> _logger = logger;
    private readonly IDiscordBotService _botService = botService;

    public async Task CommandErroredAsync(CommandsExtension c, CommandErroredEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(c);
        ArgumentNullException.ThrowIfNull(e);

        _logger.CommandsError();
        _logger.CommandsErrorType(e.Exception.GetType().Name);

        Exception ex = e.Exception;
        DateTimeOffset now = DateTimeOffset.Now;

        if (e.Context is not SlashCommandContext slashContext)
        {
            await _botService.LogExceptionAsync(ex, now);
            return;
        }

        switch (ex)
        {
            case ChecksFailedException checksFailed:
                await _botService.RespondToChecksExceptionAsync(checksFailed, slashContext);
                break;

            case DiscordException:
                await _botService.LogExceptionAsync(ex, now, slashContext, ((DiscordException)e.Exception).JsonMessage);
                break;

            default:
                await _botService.LogExceptionAsync(ex, now, slashContext);
                break;
        }
    }
}
