using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using AzzyBot.Commands.Attributes;
using AzzyBot.ExceptionHandling;
using AzzyBot.Modules.AzuraCast.Autocomplete;
using AzzyBot.Modules.Core;
using AzzyBot.Strings.AzuraCast;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Modules.AzuraCast;

internal sealed class AzuraCastCommands : ApplicationCommandModule
{
    [SlashCommandGroup("azuraCast", "AzuraCast admin commands")]
    [SlashRequireGuild]
    internal sealed class AzuraCastCommandGroup : ApplicationCommandModule
    {
        [RequireUserRole]
        [SlashCommand("export-playlists", "Exports all available playlists into a .zip file")]
        internal static async Task AzuraCastExportPlaylistsCommandAsync(InteractionContext ctx)
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "SwitchPlaylistsCommand requested");

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!await AzuraCastModule.CheckIfMusicServerIsOnlineAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AzuraCastEmbedBuilder.BuildServerNotAvailableEmbed(CoreDiscordCommands.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl)));
                return;
            }

            string fileName = await AzuraCastServer.ExportPlaylistsAsFileAsync();

            if (string.IsNullOrWhiteSpace(fileName))
                throw new FileNotFoundException("ZipFile can't be found");

            FileStream stream = new(fileName, FileMode.Open, FileAccess.Read);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(AzuraCastStringBuilder.GetCommandsExportPlaylists).AddFile(Path.GetFileName(fileName), stream));

            await stream.DisposeAsync();

            if (!CoreFileOperations.DeleteTempFile(fileName))
                throw new IOException($"{fileName} couldn't be deleted!");
        }

        [RequireUserRole]
        [SlashCommand("force-cache-refresh", "Forces a refresh of the internal AzzyBot Music cache")]
        internal static async Task AzuraCastForceCacheRefreshCommandAsync(InteractionContext ctx)
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "AzuraCastForceCacheRefreshCommand requested");

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!await AzuraCastModule.CheckIfMusicServerIsOnlineAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AzuraCastEmbedBuilder.BuildServerNotAvailableEmbed(CoreDiscordCommands.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl)));
                return;
            }

            await AzuraCastModule.CheckIfFilesWereModifiedAsync();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(AzuraCastStringBuilder.GetCommandsForceCacheRefresh));
        }

        [RequireUserRole]
        [SlashCommand("ping", "Pings AzuraCast and returns general information")]
        internal static async Task AzuraCastPingCommandAsync(InteractionContext ctx)
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "AzuraCastPingCommand requested");
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(await AzuraCastStats.GetServerStatsAsync(ctx.Client.CurrentUser.Username, ctx.Client.CurrentUser.AvatarUrl)));
        }

        [SlashCommand("switch-Playlists", "Switch the playlists according to your likes!")]
        internal static async Task AzuraCastSwitchPlaylistsCommandAsync(InteractionContext ctx, [Autocomplete(typeof(PlaylistAutocomplete))][Option("playlist", "Select a playlist to switch to")] string playlistId)
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "SwitchPlaylistsCommand requested");

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!await AzuraCastModule.CheckIfMusicServerIsOnlineAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AzuraCastEmbedBuilder.BuildServerNotAvailableEmbed(CoreDiscordCommands.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl)));
                return;
            }

            if (!AzuraCastModule.CheckIfPlaylistChangesAreAppropriate())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AzuraCastEmbedBuilder.BuildPlaylistChangesNotAllowedEmbed(CoreDiscordCommands.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl)));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(AzuraCastStringBuilder.GetCommandsChangePlaylist(await AzuraCastServer.SwitchPlaylistsAsync(playlistId))));
        }
    }

    [SlashCommandGroup("music", "Execute music related commands")]
    [SlashRequireGuild]
    internal sealed class MusicCommandGroup : ApplicationCommandModule
    {
        [SlashCommand("get-played-song-history", "Gets all songs played at the specified date and returns them as a .csv file.")]
        internal static async Task MusicGetPlayedSongHistoryCommandAsync(InteractionContext ctx, [Option("date", "Set the date in the following format YYYY-MM-DD. You can only go back up to 14 days")] string date)
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "MusicGetSongsInPlaylistCommand requested");

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            DateTime dateTime;
            if (string.IsNullOrWhiteSpace(date))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(AzuraCastStringBuilder.GetCommandsGetSongHistoryForgotDate));
                return;
            }
            else if (!DateTime.TryParse(date, out dateTime))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(AzuraCastStringBuilder.GetCommandsGetSongHistoryWrongDate));
                return;
            }
            else if (dateTime < DateTime.Today.AddDays(-14))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(AzuraCastStringBuilder.GetCommandsGetSongHistoryTooEarly));
                return;
            }

            if (!await AzuraCastModule.CheckIfMusicServerIsOnlineAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AzuraCastEmbedBuilder.BuildServerNotAvailableEmbed(CoreDiscordCommands.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl)));
                return;
            }

            string fileName = await AzuraCastServer.GetSongsPlayedAtDateAsync(dateTime);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(AzuraCastStringBuilder.GetCommandsGetSongHistoryNoOpening));
                return;
            }

            FileStream stream = new(fileName, FileMode.Open, FileAccess.Read);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(AzuraCastStringBuilder.GetCommandsGetSongHistoryHistoryFound).AddFile(Path.GetFileName(fileName), stream));

            await stream.DisposeAsync();

            if (!CoreFileOperations.DeleteTempFile(fileName))
                throw new IOException($"{fileName} couldn't be deleted!");
        }

        [SlashCommand("get-songs-in-playlist", "Gets all songs from the given playlist and returns them as a .csv file")]
        internal static async Task MusicGetSongsInPlaylistCommandAsync(InteractionContext ctx, [Autocomplete(typeof(PlaylistAutocomplete))][Option("playlist", "Select the playlist to get songs from")] string playlist)
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "MusicGetSongsInPlaylistCommand requested");

            if (string.IsNullOrWhiteSpace(playlist))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(AzuraCastStringBuilder.GetCommandsExportPlaylistContentForgotPlaylist).AsEphemeral(true));
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!await AzuraCastModule.CheckIfMusicServerIsOnlineAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AzuraCastEmbedBuilder.BuildServerNotAvailableEmbed(CoreDiscordCommands.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl)));
                return;
            }

            string fileName = await AzuraCastServer.GetSongsFromPlaylistAsync(playlist);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(AzuraCastStringBuilder.GetCommandsExportPlaylistContentNotFound));
                return;
            }

            FileStream stream = new(fileName, FileMode.Open, FileAccess.Read);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(AzuraCastStringBuilder.GetCommandsExportPlaylistContentFound).AddFile(Path.GetFileName(fileName), stream));

            await stream.DisposeAsync();

            if (!CoreFileOperations.DeleteTempFile(fileName))
                throw new IOException($"{fileName} couldn't be deleted!");
        }

        [SlashCommand("now-playing", "Shows the current played song")]
        internal static async Task MusicNowPlayingCommandAsync(InteractionContext ctx)
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "MusicNowPlayingCommand requested");
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!await AzuraCastModule.CheckIfMusicServerIsOnlineAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AzuraCastEmbedBuilder.BuildServerNotAvailableEmbed(CoreDiscordCommands.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl)));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(await AzuraCastEmbedBuilder.BuildNowPlayingEmbedAsync(CoreDiscordCommands.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl, await AzuraCastServer.GetNowPlayingAsync())));
        }
    }

    [SlashCommandGroup("music-requests", "Execute commands which involve song requests")]
    [SlashRequireGuild]
    [SlashCooldown(1, 15, SlashCooldownBucketType.Global)]
    internal sealed class MusicRequestsCommandGroup : ApplicationCommandModule
    {
        [SlashCommand("check", "Checks if the song is available on the server")]
        internal static async Task MusicRequestsCheckCommandAsync(InteractionContext ctx, [Option("song-name", "Song name to search for")] string songName, [Option("artist-name", "Artist name for better results")] string artistName = "")
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "MusicRequestsCheckCommand requested");

            if (string.IsNullOrWhiteSpace(songName))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(AzuraCastStringBuilder.GetCommandsFindSongForgotSong).AsEphemeral(true));
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!await AzuraCastModule.CheckIfMusicServerIsOnlineAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AzuraCastEmbedBuilder.BuildServerNotAvailableEmbed(CoreDiscordCommands.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl)));
                return;
            }

            bool useOnline = await AzuraCastServer.CheckIfSongRequestsAreAllowedAsync();
            DiscordEmbed embed = await AzuraCastServer.CheckIfSongExistsAsync(songName.Trim(), artistName.Trim(), CoreDiscordCommands.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl, useOnline);

            if (embed.Description != AzuraCastStringBuilder.GetEmbedAzuraSearchSongRequestsAvaDesc || !useOnline)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                return;
            }

            DiscordButtonComponent button = new(ButtonStyle.Success, "request_song", "Request Song");
            DiscordMessage message = await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed).AddComponents(button));
            InteractivityResult<ComponentInteractionCreateEventArgs> result = await message.WaitForButtonAsync(ctx.Interaction.User);

            if (!result.TimedOut)
            {
                if (!AzuraCastModule.CheckIfSongRequestsAreAppropriate() || !await AzuraCastServer.CheckIfSongRequestsAreAllowedAsync())
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AzuraCastEmbedBuilder.BuildSongRequestsNotAllowedEmbed(CoreDiscordCommands.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl)));
                    return;
                }

                embed = await AzuraCastServer.CheckIfSongIsRequestableAsync(songName.Trim(), artistName.Trim(), CoreDiscordCommands.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));

                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("favorite-songs", "Choose and request a user's favourite song")]
        internal static async Task MusicRequestsFavoriteSongsCommandAsync(InteractionContext ctx, [Autocomplete(typeof(FavoriteSongAutocomplete))][Option("User", "Select the user you want to hear the song from", true)] string user)
        {
            ExceptionHandler.LogMessage(LogLevel.Debug, "MusicRequestsFavoriteSongsCommand requested");

            if (string.IsNullOrWhiteSpace(user))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent(AzuraCastStringBuilder.GetCommandsFavoriteSongForgotUser).AsEphemeral(true));
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!await AzuraCastModule.CheckIfMusicServerIsOnlineAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AzuraCastEmbedBuilder.BuildServerNotAvailableEmbed(CoreDiscordCommands.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl)));
                return;
            }

            if (!AzuraCastModule.CheckIfSongRequestsAreAppropriate() || !await AzuraCastServer.CheckIfSongRequestsAreAllowedAsync())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(AzuraCastEmbedBuilder.BuildSongRequestsNotAllowedEmbed(CoreDiscordCommands.GetBestUsername(ctx.Member.Username, ctx.Member.Nickname), ctx.Member.AvatarUrl)));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(await AzuraCastServer.RequestFavouriteSongAsync(ctx.Member, await CoreDiscordCommands.GetMemberAsync(Convert.ToUInt64(user, CultureInfo.InvariantCulture), ctx.Guild))));
        }
    }
}
