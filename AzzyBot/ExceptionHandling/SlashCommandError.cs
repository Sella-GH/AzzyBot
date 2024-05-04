using System;
using System.Threading.Tasks;
using AzzyBot.Commands.Attributes;
using AzzyBot.Logging;
using AzzyBot.Modules.AzuraCast;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Settings;
using AzzyBot.Modules.Core.Strings;
using AzzyBot.Modules.MusicStreaming.Strings;
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

        Exception ex = e.Exception;
        InteractionContext ctx = e.Context;

        if (ex is SlashExecutionChecksFailedException slashEx)
        {
            string commandName = ctx.QualifiedName;
            string userName = CoreDiscordChecks.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname);
            string userAvatarUrl = ctx.Member.AvatarUrl;

            bool isGuildCheck = false;
            bool isCooldown = false;
            bool isMusicServerUp = false;
            bool isAzuraApiKeyValid = false;
            bool isStationUp = false;
            bool isUserInVoice = false;
            bool isDefault = false;

            foreach (SlashCheckBaseAttribute check in slashEx.FailedChecks)
            {
                switch (check)
                {
                    case SlashRequireGuildAttribute:
                        isGuildCheck = true;
                        break;

                    case SlashCooldownAttribute:
                        isCooldown = true;
                        break;

                    case RequireMusicServerUp:
                        isMusicServerUp = true;
                        break;

                    case RequireAzuraApiKeyValid:
                        isAzuraApiKeyValid = true;
                        break;

                    case RequireMusicStationUp:
                        isStationUp = true;
                        break;

                    case RequireUserInVoice:
                        isUserInVoice = true;
                        break;

                    default:
                        isDefault = true;
                        break;
                }
            }

            if (isGuildCheck)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(CoreStringBuilder.GetExceptionHandlingNotInGuild).AsEphemeral(true));
                LoggerBase.LogInfo(LoggerBase.GetLogger, $"User **{userName}** tried to execute the command **{commandName}** outside of a server!", null);

                return;
            }

            if (isCooldown)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(CoreStringBuilder.GetExceptionHandlingOnCooldown).AsEphemeral(true));
                LoggerBase.LogInfo(LoggerBase.GetLogger, $"User **{userName}** tried to execute the command **{commandName}** but it's on cooldown!", null);

                return;
            }

            if (isMusicServerUp || isStationUp)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(AcEmbedBuilder.BuildServerNotAvailableEmbed(userName, userAvatarUrl)).AsEphemeral(true));
                LoggerBase.LogInfo(LoggerBase.GetLogger, $"User **{userName}** tried to execute the command **{commandName}** but the music server seems to be offline!", null);

                return;
            }

            if (isAzuraApiKeyValid)
            {
                DiscordMember member = await CoreDiscordChecks.GetMemberAsync(CoreSettings.OwnerUserId, ctx.Guild);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(AcEmbedBuilder.BuildApiKeyNotValidEmbed(member.Mention)).AsEphemeral(true));
                LoggerBase.LogInfo(LoggerBase.GetLogger, $"User **{userName}** tried to execute the command **{commandName}** but your AzuraCast API key seems to be invalid!", null);

                return;
            }

            if (isUserInVoice)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(MsStringBuilder.GetEmbedsPreconditionNotInVoice).AsEphemeral(true));
                LoggerBase.LogInfo(LoggerBase.GetLogger, $"User **{userName}** tried to execute the command **{commandName}** but is not in a voice channel!", null);

                return;
            }

            if (isDefault)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(CoreStringBuilder.GetExceptionHandlingDefault).AsEphemeral(true));
                LoggerBase.LogInfo(LoggerBase.GetLogger, $"User **{userName}** tried to execute the command **{commandName}** but doesn't have the permission for it!", null);

                return;
            }
        }
        else if (ex is not DiscordException)
        {
            await LoggerExceptions.LogErrorAsync(ex, ctx);
        }
        else
        {
            await LoggerExceptions.LogErrorAsync(ex, ctx, ((DiscordException)ex).JsonMessage);
        }
    }

    internal static async Task AutocompleteErrorAsync(SlashCommandsExtension _, AutocompleteErrorEventArgs e)
    {
        LoggerBase.LogWarn(LoggerBase.GetLogger, "Autocomplete error occured!", null);

        Exception ex = e.Exception;
        AutocompleteContext ctx = e.Context;

        if (ex is not DiscordException)
        {
            await LoggerExceptions.LogErrorAsync(ex, ctx);
        }
        else
        {
            await LoggerExceptions.LogErrorAsync(ex, ctx, ((DiscordException)ex).JsonMessage);
        }
    }
}
