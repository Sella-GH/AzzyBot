using System.Threading.Tasks;
using AzzyBot.Commands.Attributes;
using AzzyBot.Logging;
using AzzyBot.Modules.AzuraCast.Autocomplete;
using AzzyBot.Modules.ClubManagement.Settings;
using AzzyBot.Modules.ClubManagement.Strings;
using AzzyBot.Modules.Core;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace AzzyBot.Modules.ClubManagement;

internal sealed class CmCommands : ApplicationCommandModule
{
    [SlashCommandGroup("staff", "Staff Commands")]
    [SlashRequireGuild]
    [RequireUserRole]
    [RequireMusicServerUp]
    [RequireMusicStationUp]
    [RequireAzuraApiKeyValid]
    internal sealed class StaffCommandGroup : ApplicationCommandModule
    {
        [SlashCommand("close-club", "Removes all active playlists and closes the club")]
        internal static async Task StaffCloseClubCommandAsync(InteractionContext ctx)
        {
            LoggerBase.LogInfo(LoggerBase.GetLogger, "StaffCloseClubCommand requested", null);

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

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
            await CoreDiscordChecks.RemoveUserRoleAsync(ctx.Member, CmSettings.CloserRoleId);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(CmStringBuilder.CommandCloseClubClubClosed));

            string userName = CoreDiscordChecks.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname);
            string avatarUrl = ctx.Member.AvatarUrl;
            DiscordEmbed embed = CmEmbedBuilder.BuildCloseClubEmbed(userName, avatarUrl, false);

            await CmClubControls.SendClubClosingStatisticsAsync(await AzzyBot.SendMessageAsync(CmSettings.ClubNotifyChannelId, string.Empty, [embed]));
        }

        [SlashCommand("open-club", "Select a playlist and open the club")]
        internal static async Task StaffOpenClubCommandAsync(InteractionContext ctx, [Autocomplete(typeof(AcPlaylistAutocomplete))][Option("playlist", "Select a playlist for the day")] string playlistId, [Option("custom-text", "Add some custom text to your opening message")] string slogan = "")
        {
            LoggerBase.LogInfo(LoggerBase.GetLogger, "StaffOpenClubCommand requested", null);

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (await CmModule.CheckIfClubIsOpenAsync() && !CmModule.ClubClosingInitiated)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(CmStringBuilder.CommandOpenClubAlreadyOpen));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(CmStringBuilder.CommandOpenClubClubOpened(await CmClubControls.OpenClubAsync(playlistId))));

            string userName = CoreDiscordChecks.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname);
            string avatarUrl = ctx.Member.AvatarUrl;
            DiscordRole role = CoreDiscordChecks.GetRole(CmSettings.EventsRoleId, ctx.Guild);
            DiscordEmbed embed = CmEmbedBuilder.BuildOpenClubEmbed(userName, avatarUrl, slogan.Trim());

            await AzzyBot.SendMessageAsync(CmSettings.ClubNotifyChannelId, role.Mention, [embed], true);
        }
    }
}
