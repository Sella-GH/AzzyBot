﻿using System;
using System.Threading.Tasks;
using AzzyBot.ExceptionHandling;
using AzzyBot.Modules.AzuraCast;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.MusicStreaming.Settings;
using AzzyBot.Modules.MusicStreaming.Strings;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Modules.MusicStreaming;

internal sealed class MsCommands : ApplicationCommandModule
{
    [SlashCommandGroup("player", "Player commands")]
    [SlashRequireGuild]
    internal sealed class PlayerCommandGroup : ApplicationCommandModule
    {
        [SlashCommand("disconnect", "Disconnect the bot from your voice channel")]
        internal static async Task PlayerDisconnectCommandAsync(InteractionContext ctx)
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "PlayerDisconnectCommandAsync requested");

            DiscordMember member = ctx.Member;

            if (!CoreDiscordCommands.CheckIfUserIsInVoiceChannel(member))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(MsStringBuilder.GetCommandsDisconnectVoiceRequired).AsEphemeral());
                return;
            }

            if (!CoreDiscordCommands.CheckIfBotIsInVoiceChannel(member, ctx.Client.CurrentUser.Id))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(MsStringBuilder.GetCommandsDisconnectVoiceBotIsDisc).AsEphemeral());
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (await MsLavalink.DisconnectAsync(ctx))
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(MsStringBuilder.GetCommandsDisconnectVoiceSuccess));
        }

        [SlashCommand("join", "Joins the bot into your voice channel")]
        internal static async Task PlayerJoinCommandAsync(InteractionContext ctx)
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "PlayerJoinCommandAsync requested");

            DiscordMember member = ctx.Member;

            if (!CoreDiscordCommands.CheckIfUserIsInVoiceChannel(member))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(MsStringBuilder.GetCommandsJoinVoiceRequired).AsEphemeral());
                return;
            }

            if (CoreDiscordCommands.CheckIfBotIsInVoiceChannel(member, ctx.Client.CurrentUser.Id))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(MsStringBuilder.GetCommandsJoinVoiceBotIsThere).AsEphemeral());
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (await MsLavalink.JoinMusicAsync(ctx))
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(MsStringBuilder.GetCommandsJoinVoiceSuccess));
        }

        [SlashCommand("set-volume", "Changes the volume of the player")]
        internal static async Task PlayerSetVolumeCommandAsync(InteractionContext ctx, [Option("volume", "The new volume value between 0 and 100")] double volume, [Option("reset", "Resets the volume to 100%")] bool reset = false)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(volume, nameof(volume));
            ExceptionHandler.LogMessage(LogLevel.Debug, "PlayerSetVolumeCommandAsync requested");

            if (!CoreDiscordCommands.CheckIfUserIsInVoiceChannel(ctx.Member))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(MsStringBuilder.GetCommandsSetVolumeVoiceRequired).AsEphemeral());
                return;
            }

            if (volume is > 100 or < 0)
                await ctx.CreateResponseAsync(MsStringBuilder.GetCommandsSetVolumeVoiceInvalid, true);

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (await MsLavalink.SetVolumeAsync(ctx, (float)volume, reset))
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(MsStringBuilder.GetCommandsSetVolumeVoiceSuccess((reset) ? 100 : Math.Round(volume, 2))));
        }

        [SlashCommand("show-lyrics", "Shows you the lyrics of the current played song")]
        internal static async Task PlayerShowLyricsCommandAsync(InteractionContext ctx)
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "PlayerShowLyricsCommandAsync requested");

            if (!MsSettings.ActivateLyrics || string.IsNullOrWhiteSpace(MsSettings.GeniusApiKey) || MsSettings.GeniusApiKey is "empty")
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(MsStringBuilder.GetCommandsShowLyricsModuleRequired).AsEphemeral());
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!await AzuraCastModule.CheckIfMusicServerIsOnlineAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AcEmbedBuilder.BuildServerIsOfflineEmbed(ctx.Client.CurrentUser.Username, ctx.Client.CurrentUser.AvatarUrl, false)));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(await MsLavalink.GetSongLyricsAsync(ctx)));
        }

        [SlashCommand("start", "Starts the music stream into your voice channel")]
        internal static async Task PlayerStartCommandAsync(InteractionContext ctx)
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "PlayerStartCommandAsync requested");

            if (!CoreDiscordCommands.CheckIfUserIsInVoiceChannel(ctx.Member))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(MsStringBuilder.GetCommandsStartVoiceRequired).AsEphemeral());
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!await AzuraCastModule.CheckIfMusicServerIsOnlineAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AcEmbedBuilder.BuildServerIsOfflineEmbed(ctx.Client.CurrentUser.Username, ctx.Client.CurrentUser.AvatarUrl, false)));
                return;
            }

            if (await MsLavalink.PlayMusicAsync(ctx))
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(MsStringBuilder.GetCommandsStartVoiceMusicPlaying));
        }

        [SlashCommand("stop", "Stops the music stream from playing")]
        internal static async Task PlayerStopCommandAsync(InteractionContext ctx, [Option("disconnect", "Should the bot disconnect after?")] bool disconnect = false)
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "PlayerStopCommandAsync requested");

            if (!CoreDiscordCommands.CheckIfUserIsInVoiceChannel(ctx.Member))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(MsStringBuilder.GetCommandsStopVoiceRequired).AsEphemeral());
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (await MsLavalink.StopMusicAsync(ctx, disconnect))
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(MsStringBuilder.GetCommandsStopVoiceMusicPlaying));
        }
    }
}
