using System.Threading.Tasks;
using AzzyBot.Commands.Attributes;
using AzzyBot.ExceptionHandling;
using AzzyBot.Modules.AzuraCast;
using AzzyBot.Modules.AzuraCast.Autocomplete;
using AzzyBot.Modules.AzuraCast.Settings;
using AzzyBot.Modules.ClubManagement.Settings;
using AzzyBot.Modules.ClubManagement.Strings;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Settings;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Modules.ClubManagement;

internal sealed class CmCommands : ApplicationCommandModule
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
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AcEmbedBuilder.BuildServerIsOfflineEmbed(ctx.Client.CurrentUser.Username, ctx.Client.CurrentUser.AvatarUrl, false)));
                return;
            }

            if (!AcSettings.AzuraCastApiKeyIsValid)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AcEmbedBuilder.BuildApiKeyNotValidEmbed((await CoreDiscordCommands.GetMemberAsync(CoreSettings.OwnerUserId, ctx.Guild)).Mention)));
                return;
            }

            if (CmModule.ClubClosingInitiated)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(CmStringBuilder.CommandCloseClubAlreadyInitiated));
                return;
            }

            if (!await CmModule.CheckIfClubIsOpenAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(CmStringBuilder.CommandCloseClubClubIsAlreadyClosed));
                return;
            }

            await CmClubControls.CloseClubAsync();
            await CoreDiscordCommands.RemoveUserRoleAsync(ctx.Member, CmSettings.CloserRoleId);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(CmStringBuilder.CommandCloseClubClubClosed));

            await CmClubControls.SendClubClosingStatisticsAsync(await AzzyBot.SendMessageAsync(CmSettings.ClubNotifyChannelId, string.Empty, [CmEmbedBuilder.BuildCloseClubEmbed(CoreDiscordCommands.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl, false)]));
        }

        [SlashCommand("open-club", "Select a playlist and open the club")]
        internal static async Task StaffOpenClubCommandAsync(InteractionContext ctx, [Autocomplete(typeof(AcPlaylistAutocomplete))][Option("playlist", "Select a playlist for the day")] string playlistId, [Option("custom-text", "Add some custom text to your opening message")] string slogan = "")
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "StaffOpenClubCommand requested");

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!await AzuraCastModule.CheckIfMusicServerIsOnlineAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AcEmbedBuilder.BuildServerNotAvailableEmbed(CoreDiscordCommands.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl)));
                return;
            }

            if (!AcSettings.AzuraCastApiKeyIsValid)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AcEmbedBuilder.BuildApiKeyNotValidEmbed((await CoreDiscordCommands.GetMemberAsync(CoreSettings.OwnerUserId, ctx.Guild)).Mention)));
                return;
            }

            if (await CmModule.CheckIfClubIsOpenAsync() && !CmModule.ClubClosingInitiated)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(CmStringBuilder.CommandOpenClubAlreadyOpen));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(CmStringBuilder.CommandOpenClubClubOpened(await CmClubControls.OpenClubAsync(playlistId))));

            await AzzyBot.SendMessageAsync(CmSettings.ClubNotifyChannelId, CoreDiscordCommands.GetRole(CmSettings.EventsRoleId, ctx.Guild).Mention, [CmEmbedBuilder.BuildOpenClubEmbed(CoreDiscordCommands.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl, slogan.Trim())], true);
        }
    }
}
