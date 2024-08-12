using System;
using System.Threading.Tasks;
using AzzyBot.Core.Logging;
using DSharpPlus.Commands;
using DSharpPlus.Commands.EventArgs;
using DSharpPlus.Commands.Exceptions;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Bot.Services.DiscordEvents;

public static class DiscordCommandsErrorHandler
{
    public static async Task CommandErroredAsync(CommandsExtension c, CommandErroredEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(c, nameof(c));
        ArgumentNullException.ThrowIfNull(e, nameof(e));

        IServiceProvider sp = c.ServiceProvider;
        ILogger logger = sp.GetRequiredService<ILogger>();
        DiscordBotService botService = sp.GetRequiredService<DiscordBotService>();

        logger.CommandsError();
        logger.CommandsErrorType(e.Exception.GetType().Name);

        Exception ex = e.Exception;
        DateTime now = DateTime.Now;
        ulong guildId = 0;
        if (e.Context.Guild is not null)
            guildId = e.Context.Guild.Id;

        if (e.Context is not SlashCommandContext slashContext)
        {
            await botService.LogExceptionAsync(ex, now, guildId: guildId);
            return;
        }

        switch (ex)
        {
            case ChecksFailedException checksFailed:
                await botService.RespondToChecksExceptionAsync(checksFailed, slashContext);
                break;

            case DiscordException:
                await botService.LogExceptionAsync(ex, now, slashContext, guildId, ((DiscordException)e.Exception).JsonMessage);
                break;

            default:
                await botService.LogExceptionAsync(ex, now, slashContext, guildId);
                break;
        }
    }
}
