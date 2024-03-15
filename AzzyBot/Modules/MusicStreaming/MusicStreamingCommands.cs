using System;
using System.Threading.Tasks;
using AzzyBot.ExceptionHandling;
using AzzyBot.Modules.AzuraCast;
using AzzyBot.Modules.Core;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Modules.MusicStreaming;

internal sealed class MusicStreamingCommands : ApplicationCommandModule
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
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You have to be in a voice channel for this action!").AsEphemeral());
                return;
            }

            if (!CoreDiscordCommands.CheckIfBotIsInVoiceChannel(member, ctx.Client.CurrentUser.Id))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Bot is already disconnected").AsEphemeral());
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (await MusicStreamingLavalink.DisconnectAsync(ctx))
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Bot disconnecting"));
        }

        [SlashCommand("join", "Joins the bot into your voice channel")]
        internal static async Task PlayerJoinCommandAsync(InteractionContext ctx)
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "PlayerJoinCommandAsync requested");

            DiscordMember member = ctx.Member;

            if (!CoreDiscordCommands.CheckIfUserIsInVoiceChannel(member))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You have to be in a voice channel for this action!").AsEphemeral());
                return;
            }

            if (CoreDiscordCommands.CheckIfBotIsInVoiceChannel(member, ctx.Client.CurrentUser.Id))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Bot is already joined").AsEphemeral());
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (await MusicStreamingLavalink.JoinMusicAsync(ctx))
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Bot joining"));
        }

        [SlashCommand("set-volume", "Changes the volume of the player")]
        internal static async Task PlayerSetVolumeCommandAsync(InteractionContext ctx, [Option("volume", "The new volume between 1 and 100")] double volume, [Option("reset", "Resets the volume to 100%")] bool reset = false)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(volume, nameof(volume));
            ExceptionHandler.LogMessage(LogLevel.Debug, "PlayerSetVolumeCommandAsync requested");

            if (!CoreDiscordCommands.CheckIfUserIsInVoiceChannel(ctx.Member))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You have to be in a voice channel for this action!").AsEphemeral());
                return;
            }

            if (volume is > 100 or < 0)
                await ctx.CreateResponseAsync("Volume must be between 0 and 1000", true);

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!await AzuraCastModule.CheckIfMusicServerIsOnlineAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AzuraCastEmbedBuilder.BuildServerIsOfflineEmbed(ctx.Client.CurrentUser.Username, ctx.Client.CurrentUser.AvatarUrl, false)));
                return;
            }

            if (await MusicStreamingLavalink.SetVolumeAsync(ctx, (float)volume, reset))
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Volume changed to {((reset) ? 100 : Math.Round(volume, 2))}%"));
        }

        [SlashCommand("start", "Starts the music stream into your voice channel")]
        internal static async Task PlayerStartCommandAsync(InteractionContext ctx)
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "PlayerStartCommandAsync requested");

            if (!CoreDiscordCommands.CheckIfUserIsInVoiceChannel(ctx.Member))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You have to be in a voice channel for this action!").AsEphemeral());
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!await AzuraCastModule.CheckIfMusicServerIsOnlineAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AzuraCastEmbedBuilder.BuildServerIsOfflineEmbed(ctx.Client.CurrentUser.Username, ctx.Client.CurrentUser.AvatarUrl, false)));
                return;
            }

            if (await MusicStreamingLavalink.PlayMusicAsync(ctx))
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Track's playing"));
        }

        [SlashCommand("stop", "Stops the music stream from playing")]
        internal static async Task PlayerStopCommandAsync(InteractionContext ctx, [Option("disconnect", "Should the bot disconnect after?")] bool disconnect = false)
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "PlayerStopCommandAsync requested");

            if (!CoreDiscordCommands.CheckIfUserIsInVoiceChannel(ctx.Member))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You have to be in a voice channel for this action!").AsEphemeral());
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!await AzuraCastModule.CheckIfMusicServerIsOnlineAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AzuraCastEmbedBuilder.BuildServerIsOfflineEmbed(ctx.Client.CurrentUser.Username, ctx.Client.CurrentUser.AvatarUrl, false)));
                return;
            }

            if (await MusicStreamingLavalink.StopMusicAsync(ctx, disconnect))
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Track's stopping"));
        }
    }
}
