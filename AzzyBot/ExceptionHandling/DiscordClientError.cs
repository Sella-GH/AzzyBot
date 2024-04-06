using System;
using System.Threading.Tasks;
using AzzyBot.Modules.Core;
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
                await ExceptionHandler.LogErrorAsync(ex, ((DiscordException)e.Exception).JsonMessage);
                await AzzyBot.SendMessageAsync(CoreSettings.ErrorChannelId, CoreStringBuilder.GetExceptionHandlingDiscordPermissions(c.CurrentUser.Username));
                break;

            default:
                if (e.Exception is not DiscordException)
                {
                    await ExceptionHandler.LogErrorAsync(ex);
                    break;
                }

                await ExceptionHandler.LogErrorAsync(ex, ((DiscordException)e.Exception).JsonMessage);
                break;
        }
    }
}
