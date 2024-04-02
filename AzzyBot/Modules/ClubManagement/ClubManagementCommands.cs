using System.Threading.Tasks;
using AzzyBot.Commands.Attributes;
using AzzyBot.ExceptionHandling;
using AzzyBot.Modules.AzuraCast;
using AzzyBot.Modules.AzuraCast.Autocomplete;
using AzzyBot.Modules.Core;
using AzzyBot.Strings.ClubManagement;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Modules.ClubManagement;

internal sealed class ClubManagementCommands : ApplicationCommandModule
{
    [SlashCommandGroup("staff", "Staff Commands")]
    [SlashRequireGuild]
    [RequireUserRole]
    internal sealed class StaffCommandGroup : ApplicationCommandModule
    {
        [SlashCommand("close-club", "Removes all active playlists and closes the club")]
        internal static async Task StaffCloseClubCommandAsync(InteractionContext ctx)
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "StaffCloseClubCommand requested");
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!await AzuraCastModule.CheckIfMusicServerIsOnlineAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AzuraCastEmbedBuilder.BuildServerIsOfflineEmbed(ctx.Client.CurrentUser.Username, ctx.Client.CurrentUser.AvatarUrl, false)));
                return;
            }

            if (ClubManagementModule.ClubClosingInitiated)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(ClubManagementStringBuilder.CommandCloseClubAlreadyInitiated));
                return;
            }

            if (!await ClubManagementModule.CheckIfClubIsOpenAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(ClubManagementStringBuilder.CommandCloseClubClubIsAlreadyClosed));
                return;
            }

            await ClubControls.CloseClubAsync();
            await CoreDiscordCommands.RemoveUserRoleAsync(ctx.Member, ClubManagementSettings.CloserRoleId);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(ClubManagementStringBuilder.CommandCloseClubClubClosed));

            await ClubControls.SendClubClosingStatisticsAsync(await Program.SendMessageAsync(ClubManagementSettings.ClubNotifyChannelId, string.Empty, ClubEmbedBuilder.BuildCloseClubEmbed(CoreDiscordCommands.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl, false)));
        }

        [SlashCommand("open-club", "Select a playlist and open the club")]
        internal static async Task StaffOpenClubCommandAsync(InteractionContext ctx, [Autocomplete(typeof(PlaylistAutocomplete))][Option("playlist", "Select a playlist for the day")] string playlistId, [Option("custom-text", "Add some custom text to your opening message")] string slogan = "")
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "StaffOpenClubCommand requested");

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!await AzuraCastModule.CheckIfMusicServerIsOnlineAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AzuraCastEmbedBuilder.BuildServerNotAvailableEmbed(CoreDiscordCommands.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl)));
                return;
            }

            if (await ClubManagementModule.CheckIfClubIsOpenAsync() && !ClubManagementModule.ClubClosingInitiated)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(ClubManagementStringBuilder.CommandOpenClubAlreadyOpen));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(ClubManagementStringBuilder.CommandOpenClubClubOpened(await ClubControls.OpenClubAsync(playlistId))));

            await Program.SendMessageAsync(ClubManagementSettings.ClubNotifyChannelId, CoreDiscordCommands.GetRole(ClubManagementSettings.EventsRoleId, ctx.Guild).Mention, ClubEmbedBuilder.BuildOpenClubEmbed(CoreDiscordCommands.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl, slogan.Trim()), true);
        }
    }
}
