using System;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.Exceptions;

namespace AzzyBot.Bot.Services.DiscordEvents;

public sealed class DiscordErrorsHandler(DiscordBotService botService) : IClientErrorHandler
{
    private readonly DiscordBotService _botService = botService;

    public async ValueTask HandleEventHandlerError(string name, Exception exception, Delegate invokedDelegate, object sender, object args)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (_botService is null)
            return;

        DateTimeOffset now = DateTimeOffset.Now;

        switch (exception)
        {
            case RateLimitException:
                break;

            case BadRequestException:
            case NotFoundException:
            case RequestSizeException:
            case ServerErrorException:
            case UnauthorizedException:
                await _botService.LogExceptionAsync(exception, now);
                break;

            default:
                if (exception is not DiscordException)
                {
                    await _botService.LogExceptionAsync(exception, now);
                    break;
                }

                await _botService.LogExceptionAsync(exception, now, info: ((DiscordException)exception).JsonMessage);
                break;
        }
    }

    public async ValueTask HandleGatewayError(Exception exception)
        => await _botService.LogExceptionAsync(exception, DateTimeOffset.Now);
}
