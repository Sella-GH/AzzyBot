using System.Threading.Tasks;
using AzzyBot.ExceptionHandling;
using AzzyBot.Modules.AzuraCast;
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

            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);

            if (await LavalinkService.DisconnectAsync(ctx))
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Bot disconnecting"));
        }

        [SlashCommand("join", "Joins the bot into your voice channel")]
        internal static async Task PlayerJoinCommandAsync(InteractionContext ctx)
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "PlayerJoinCommandAsync requested");

            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);

            if (await LavalinkService.JoinMusicAsync(ctx))
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Bot joining"));
        }

        [SlashCommand("start", "Starts the music stream into your voice channel")]
        internal static async Task PlayerStartCommandAsync(InteractionContext ctx)
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "PlayerStartCommandAsync requested");

            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);

            if (!await AzuraCastModule.CheckIfMusicServerIsOnlineAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AzuraCastEmbedBuilder.BuildServerIsOfflineEmbed(ctx.Client.CurrentUser.Username, ctx.Client.CurrentUser.AvatarUrl, false)));
                return;
            }

            if (await LavalinkService.PlayMusicAsync(ctx))
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Track's playing"));
        }

        [SlashCommand("stop", "Stops the music stream from playing")]
        internal static async Task PlayerStopCommandAsync(InteractionContext ctx, [Option("disconnect", "Should the bot disconnect after?")] bool disconnect = false)
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "PlayerStopCommandAsync requested");

            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);

            if (!await AzuraCastModule.CheckIfMusicServerIsOnlineAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AzuraCastEmbedBuilder.BuildServerIsOfflineEmbed(ctx.Client.CurrentUser.Username, ctx.Client.CurrentUser.AvatarUrl, false)));
                return;
            }

            if (await LavalinkService.StopMusicAsync(ctx, disconnect))
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Track's stopping"));
        }
    }
}
