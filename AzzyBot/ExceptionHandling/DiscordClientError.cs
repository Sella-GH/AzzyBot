using System;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Modules.Core.Settings;
using AzzyBot.Modules.Core.Strings;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;

namespace AzzyBot.ExceptionHandling;

internal static class DiscordClientError
{
    internal static async Task DiscordErrorAsync(DiscordClient c, ClientErrorEventArgs e)
    {
        Exception ex = e.Exception;

        switch (ex)
        {
            case RateLimitException:
                LoggerBase.LogCrit(LoggerBase.GetLogger, $"{ex}\n{((DiscordException)e.Exception).JsonMessage}", null);
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
