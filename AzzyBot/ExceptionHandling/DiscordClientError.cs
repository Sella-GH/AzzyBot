using System;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Modules.Core.Settings;
using AzzyBot.Modules.Core.Strings;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;

namespace AzzyBot.ExceptionHandling;

internal static class DiscordClientError
{
    internal static async Task DiscordErrorAsync(DiscordClient c, ClientErrorEventArgs e)
    {
        Exception ex = e.Exception;

        switch (ex)
        {
            case RateLimitException:
                ExceptionHandler.LogMessage(LogLevel.Critical, ex.ToString(), ((DiscordException)e.Exception).JsonMessage);
                break;

            case BadRequestException:
            case NotFoundException:
            case RequestSizeException:
            case ServerErrorException:
            case UnauthorizedException:
                await LoggerExceptions.LogErrorAsync(ex, ((DiscordException)e.Exception).JsonMessage);
                await AzzyBot.SendMessageAsync(CoreSettings.ErrorChannelId, CoreStringBuilder.GetExceptionHandlingDiscordPermissions(c.CurrentUser.Username));
                break;

            default:
                if (e.Exception is not DiscordException)
                {
                    await LoggerExceptions.LogErrorAsync(ex);
                    break;
                }

                await LoggerExceptions.LogErrorAsync(ex, ((DiscordException)e.Exception).JsonMessage);
                break;
        }
    }
}
