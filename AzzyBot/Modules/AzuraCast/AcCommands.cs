using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using AzzyBot.Commands.Attributes;
using AzzyBot.Logging;
using AzzyBot.Modules.AzuraCast.Autocomplete;
using AzzyBot.Modules.AzuraCast.Settings;
using AzzyBot.Modules.AzuraCast.Strings;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Settings;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace AzzyBot.Modules.AzuraCast;

internal sealed class AcCommands : ApplicationCommandModule
{
    [SlashCommandGroup("azuracast", "AzuraCast admin commands")]
    [SlashRequireGuild]
    internal sealed class AzuraCastCommandGroup : ApplicationCommandModule
    {
        [RequireUserRole]
        [RequireMusicServerUp]
        [SlashCommand("export-playlists", "Exports all available playlists into a .zip file")]
        internal static async Task AzuraCastExportPlaylistsCommandAsync(InteractionContext ctx)
        {
            LoggerBase.LogInfo(LoggerBase.GetLogger, "SwitchPlaylistsCommand requested", null);

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!AcSettings.AzuraCastApiKeyIsValid)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AcEmbedBuilder.BuildApiKeyNotValidEmbed((await CoreDiscordChecks.GetMemberAsync(CoreSettings.OwnerUserId, ctx.Guild)).Mention)));
                return;
            }

            string fileName = await AcServer.ExportPlaylistsAsFileAsync();

            if (string.IsNullOrWhiteSpace(fileName))
                throw new FileNotFoundException("ZipFile can't be found");

            FileStream stream = new(fileName, FileMode.Open, FileAccess.Read);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(AcStringBuilder.GetCommandsExportPlaylists).AddFile(Path.GetFileName(fileName), stream));

            await stream.DisposeAsync();

            if (!CoreFileOperations.DeleteTempFile(fileName))
                throw new IOException($"{fileName} couldn't be deleted!");
        }

        [RequireUserRole]
        [RequireMusicServerUp]
        [SlashCommand("force-cache-refresh", "Forces a refresh of the internal AzzyBot Music cache")]
        internal static async Task AzuraCastForceCacheRefreshCommandAsync(InteractionContext ctx)
        {
            LoggerBase.LogInfo(LoggerBase.GetLogger, "AzuraCastForceCacheRefreshCommand requested", null);

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!AcSettings.AzuraCastApiKeyIsValid)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AcEmbedBuilder.BuildApiKeyNotValidEmbed((await CoreDiscordChecks.GetMemberAsync(CoreSettings.OwnerUserId, ctx.Guild)).Mention)));
                return;
            }

            await AzuraCastModule.CheckIfFilesWereModifiedAsync();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(AcStringBuilder.GetCommandsForceCacheRefresh));
        }

        [RequireUserRole]
        [SlashCommand("ping", "Pings AzuraCast and returns general information")]
        internal static async Task AzuraCastPingCommandAsync(InteractionContext ctx)
        {
            LoggerBase.LogInfo(LoggerBase.GetLogger, "AzuraCastPingCommand requested", null);
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!AcSettings.AzuraCastApiKeyIsValid)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AcEmbedBuilder.BuildApiKeyNotValidEmbed((await CoreDiscordChecks.GetMemberAsync(CoreSettings.OwnerUserId, ctx.Guild)).Mention)));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(await AcStats.GetServerStatsAsync(ctx.Client.CurrentUser.Username, ctx.Client.CurrentUser.AvatarUrl)));
        }

        [RequireUserRole]
        [RequireMusicServerUp]
        [SlashCommand("switch-Playlists", "Switch the playlists according to your likes!")]
        internal static async Task AzuraCastSwitchPlaylistsCommandAsync(InteractionContext ctx, [Autocomplete(typeof(AcPlaylistAutocomplete))][Option("playlist", "Select a playlist to switch to")] string playlistId)
        {
            LoggerBase.LogInfo(LoggerBase.GetLogger, "SwitchPlaylistsCommand requested", null);

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!AcSettings.AzuraCastApiKeyIsValid)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AcEmbedBuilder.BuildApiKeyNotValidEmbed((await CoreDiscordChecks.GetMemberAsync(CoreSettings.OwnerUserId, ctx.Guild)).Mention)));
                return;
            }

            if (!AzuraCastModule.CheckIfPlaylistChangesAreAppropriate())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AcEmbedBuilder.BuildPlaylistChangesNotAllowedEmbed(CoreDiscordChecks.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl)));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(AcStringBuilder.GetCommandsChangePlaylist(await AcServer.SwitchPlaylistsAsync(playlistId))));
        }
    }

    [SlashCommandGroup("music", "Execute music related commands")]
    [SlashRequireGuild]
    internal sealed class MusicCommandGroup : ApplicationCommandModule
    {
        [RequireMusicServerUp]
        [SlashCommand("get-played-song-history", "Gets all songs played at the specified date and returns them as a .csv file.")]
        internal static async Task MusicGetPlayedSongHistoryCommandAsync(InteractionContext ctx, [Option("date", "Set the date in the following format YYYY-MM-DD. You can only go back up to 14 days")] string date)
        {
            LoggerBase.LogInfo(LoggerBase.GetLogger, "MusicGetSongsInPlaylistCommand requested", null);

            DateTime dateTime;
            if (string.IsNullOrWhiteSpace(date))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(AcStringBuilder.GetCommandsGetSongHistoryForgotDate));
                return;
            }
            else if (!DateTime.TryParse(date, out dateTime))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(AcStringBuilder.GetCommandsGetSongHistoryWrongDate));
                return;
            }
            else if (dateTime < DateTime.Today.AddDays(-14))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(AcStringBuilder.GetCommandsGetSongHistoryTooEarly));
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!AcSettings.AzuraCastApiKeyIsValid)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AcEmbedBuilder.BuildApiKeyNotValidEmbed((await CoreDiscordChecks.GetMemberAsync(CoreSettings.OwnerUserId, ctx.Guild)).Mention)));
                return;
            }

            string fileName = await AcServer.GetSongsPlayedAtDateAsync(dateTime);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(AcStringBuilder.GetCommandsGetSongHistoryNoOpening));
                return;
            }

            FileStream stream = new(fileName, FileMode.Open, FileAccess.Read);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(AcStringBuilder.GetCommandsGetSongHistoryHistoryFound).AddFile(Path.GetFileName(fileName), stream));

            await stream.DisposeAsync();

            if (!CoreFileOperations.DeleteTempFile(fileName))
                throw new IOException($"{fileName} couldn't be deleted!");
        }

        [RequireMusicServerUp]
        [SlashCommand("get-songs-in-playlist", "Gets all songs from the given playlist and returns them as a .csv file")]
        internal static async Task MusicGetSongsInPlaylistCommandAsync(InteractionContext ctx, [Autocomplete(typeof(AcPlaylistAutocomplete))][Option("playlist", "Select the playlist to get songs from")] string playlist)
        {
            LoggerBase.LogInfo(LoggerBase.GetLogger, "MusicGetSongsInPlaylistCommand requested", null);

            if (string.IsNullOrWhiteSpace(playlist))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(AcStringBuilder.GetCommandsExportPlaylistContentForgotPlaylist).AsEphemeral(true));
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!AcSettings.AzuraCastApiKeyIsValid)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AcEmbedBuilder.BuildApiKeyNotValidEmbed((await CoreDiscordChecks.GetMemberAsync(CoreSettings.OwnerUserId, ctx.Guild)).Mention)));
                return;
            }

            string fileName = await AcServer.GetSongsFromPlaylistAsync(playlist);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(AcStringBuilder.GetCommandsExportPlaylistContentNotFound));
                return;
            }

            FileStream stream = new(fileName, FileMode.Open, FileAccess.Read);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(AcStringBuilder.GetCommandsExportPlaylistContentFound).AddFile(Path.GetFileName(fileName), stream));

            await stream.DisposeAsync();

            if (!CoreFileOperations.DeleteTempFile(fileName))
                throw new IOException($"{fileName} couldn't be deleted!");
        }

        [RequireMusicServerUp]
        [SlashCommand("now-playing", "Shows the current played song")]
        internal static async Task MusicNowPlayingCommandAsync(InteractionContext ctx)
        {
            LoggerBase.LogInfo(LoggerBase.GetLogger, "MusicNowPlayingCommand requested", null);
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(await AcEmbedBuilder.BuildNowPlayingEmbedAsync(CoreDiscordChecks.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl, await AcServer.GetNowPlayingAsync())));
        }
    }

    [SlashCommandGroup("music-requests", "Execute commands which involve song requests")]
    [SlashRequireGuild]
    [SlashCooldown(1, 15, SlashCooldownBucketType.Global)]
    internal sealed class MusicRequestsCommandGroup : ApplicationCommandModule
    {
        [RequireMusicServerUp]
        [SlashCommand("check", "Checks if the song is available on the server")]
        internal static async Task MusicRequestsCheckCommandAsync(InteractionContext ctx, [Option("song-name", "Song name to search for")] string songName, [Option("artist-name", "Artist name for better results")] string artistName = "")
        {
            LoggerBase.LogInfo(LoggerBase.GetLogger, "MusicRequestsCheckCommand requested", null);

            if (string.IsNullOrWhiteSpace(songName))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(AcStringBuilder.GetCommandsFindSongForgotSong).AsEphemeral(true));
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!AcSettings.AzuraCastApiKeyIsValid)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AcEmbedBuilder.BuildApiKeyNotValidEmbed((await CoreDiscordChecks.GetMemberAsync(CoreSettings.OwnerUserId, ctx.Guild)).Mention)));
                return;
            }

            bool useOnline = await AcServer.CheckIfSongRequestsAreAllowedAsync();
            DiscordEmbed embed = await AcServer.CheckIfSongExistsAsync(songName.Trim(), artistName.Trim(), CoreDiscordChecks.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl, useOnline);

            if (embed.Description != AcStringBuilder.GetEmbedAzuraSearchSongRequestsAvaDesc || !useOnline)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                return;
            }

            DiscordButtonComponent button = new(ButtonStyle.Success, "request_song", "Request Song");
            DiscordMessage message = await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed).AddComponents(button));
            InteractivityResult<ComponentInteractionCreateEventArgs> result = await message.WaitForButtonAsync(ctx.Interaction.User);

            if (!result.TimedOut)
            {
                if (!AzuraCastModule.CheckIfSongRequestsAreAppropriate() || !await AcServer.CheckIfSongRequestsAreAllowedAsync())
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AcEmbedBuilder.BuildSongRequestsNotAllowedEmbed(CoreDiscordChecks.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl)));
                    return;
                }

                embed = await AcServer.CheckIfSongIsRequestableAsync(songName.Trim(), artistName.Trim(), CoreDiscordChecks.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));

                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [RequireMusicServerUp]
        [SlashCommand("favorite-songs", "Choose and request a user's favourite song")]
        internal static async Task MusicRequestsFavoriteSongsCommandAsync(InteractionContext ctx, [Autocomplete(typeof(AcFavoriteSongAutocomplete))][Option("User", "Select the user you want to hear the song from", true)] string user)
        {
            LoggerBase.LogInfo(LoggerBase.GetLogger, "MusicRequestsFavoriteSongsCommand requested", null);

            if (string.IsNullOrWhiteSpace(user))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(AcStringBuilder.GetCommandsFavoriteSongForgotUser).AsEphemeral(true));
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!AzuraCastModule.CheckIfSongRequestsAreAppropriate() || !await AcServer.CheckIfSongRequestsAreAllowedAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AcEmbedBuilder.BuildSongRequestsNotAllowedEmbed(CoreDiscordChecks.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl)));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(await AcServer.RequestFavouriteSongAsync(ctx.Member, await CoreDiscordChecks.GetMemberAsync(Convert.ToUInt64(user, CultureInfo.InvariantCulture), ctx.Guild))));
        }
    }
}
