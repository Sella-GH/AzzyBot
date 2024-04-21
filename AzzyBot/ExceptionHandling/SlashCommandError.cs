using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Modules.Core.Strings;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using DSharpPlus.SlashCommands.EventArgs;

namespace AzzyBot.ExceptionHandling;

internal static class SlashCommandError
{
    internal static async Task SlashErrorAsync(SlashCommandsExtension _, SlashCommandErrorEventArgs e)
    {
        LoggerBase.LogWarn(LoggerBase.GetLogger, "Slash error occured!", null);

        if (e.Exception is SlashExecutionChecksFailedException ex)
        {
            bool isGuildCheck = false;
            bool isCooldown = false;
            bool isDefault = false;

            foreach (SlashCheckBaseAttribute check in ex.FailedChecks)
            {
                switch (check)
                {
                    case SlashRequireGuildAttribute:
                        isGuildCheck = true;
                        break;

                    case SlashCooldownAttribute:
                        isCooldown = true;
                        break;

                    default:
                        isDefault = true;
                        break;
                }
            }

            if (isGuildCheck)
            {
                await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(CoreStringBuilder.GetExceptionHandlingNotInGuild).AsEphemeral(true));
                LoggerBase.LogInfo(LoggerBase.GetLogger, $"User **{e.Context.User.Username}** tried to access the command **{e.Context.QualifiedName}** outside of a server!", null);
                return;
            }

            if (isCooldown)
            {
                await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(CoreStringBuilder.GetExceptionHandlingOnCooldown).AsEphemeral(true));
                return;
            }

            if (isDefault)
            {
                await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(CoreStringBuilder.GetExceptionHandlingDefault).AsEphemeral(true));
                return;
            }
        }
        else if (e.Exception is not DiscordException)
        {
            await LoggerExceptions.LogErrorAsync(e.Exception, e.Context);
        }
        else
        {
            await LoggerExceptions.LogErrorAsync(e.Exception, e.Context, ((DiscordException)e.Exception).JsonMessage);
        }
    }

    internal static async Task AutocompleteErrorAsync(SlashCommandsExtension _, AutocompleteErrorEventArgs e)
    {
        LoggerBase.LogWarn(LoggerBase.GetLogger, "Autocomplete error occured!", null);

        if (e.Exception is not DiscordException)
        {
            await LoggerExceptions.LogErrorAsync(e.Exception, e.Context);
        }
        else
        {
            await LoggerExceptions.LogErrorAsync(e.Exception, e.Context, ((DiscordException)e.Exception).JsonMessage);
        }
    }
}
