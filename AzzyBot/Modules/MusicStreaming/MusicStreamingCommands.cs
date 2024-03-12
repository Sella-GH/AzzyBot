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

            await LavalinkService.PlayMusicAsync(ctx);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Track's playing"));
        }

        [SlashCommand("stop", "Stops the music stream from playing")]
        internal static async Task PlayerStopCommandAsync(InteractionContext ctx)
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "PlayerStopCommandAsync requested");

            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource);

            if (!await AzuraCastModule.CheckIfMusicServerIsOnlineAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AzuraCastEmbedBuilder.BuildServerIsOfflineEmbed(ctx.Client.CurrentUser.Username, ctx.Client.CurrentUser.AvatarUrl, false)));
                return;
            }

            await LavalinkService.StopMusicAsync(ctx);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Track's stopping"));
        }
    }
}
