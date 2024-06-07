using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Commands.Autocompletes;
using AzzyBot.Database;
using AzzyBot.Database.Entities;
using AzzyBot.Logging;
using AzzyBot.Services.Modules;
using AzzyBot.Utilities;
using AzzyBot.Utilities.Encryption;
using AzzyBot.Utilities.Records.AzuraCast;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Commands;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "DSharpPlus best practice")]
public sealed class AzuraCastCommands
{
    [Command("azuracast"), RequireGuild, RequirePermissions(DiscordPermissions.None, DiscordPermissions.Administrator)]
    public sealed class AzuraCastGroup(ILogger<AzuraCastGroup> logger, AzuraCastService azuraCast, DbActions dbActions)
    {
        private readonly ILogger<AzuraCastGroup> _logger = logger;
        private readonly AzuraCastService _azuraCast = azuraCast;
        private readonly DbActions _dbActions = dbActions;

        [Command("hardware-stats"), Description("Get the hardware stats of the running server.")]
        public async ValueTask GetHardwareStatsAsync(CommandContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(GetHardwareStatsAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            GuildsEntity guild = await _dbActions.GetGuildAsync(context.Guild.Id);
            AzuraCastEntity azuraCast = guild.AzuraCast ?? throw new InvalidOperationException("AzuraCast is null");
            string apiKey = Crypto.Decrypt(azuraCast.AdminApiKey);
            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);

            HardwareStatsRecord hardwareStats = await _azuraCast.GetHardwareStatsAsync(new(baseUrl), apiKey);

            DiscordEmbed embed = EmbedBuilder.BuildAzuraCastHardwareStatsEmbed(hardwareStats);

            await context.EditResponseAsync(embed);
        }
    }

    [Command("music"), RequireGuild]
    public sealed class MusicGroup(ILogger<MusicGroup> logger, AzuraCastService azuraCast, DbActions dbActions)
    {
        private readonly ILogger<MusicGroup> _logger = logger;
        private readonly AzuraCastService _azuraCast = azuraCast;
        private readonly DbActions _dbActions = dbActions;

        [Command("now-playing"), Description("Get the currently playing song on the station.")]
        public async ValueTask GetNowPlayingAsync
            (
            CommandContext context,
            [Description("The station of which you want to see what's played."), SlashAutoCompleteProvider(typeof(AzuraCastStationsAutocomplete))] int stationId
            )
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(context.Guild, nameof(context.Guild));

            _logger.CommandRequested(nameof(GetNowPlayingAsync), context.User.GlobalName);

            await context.DeferResponseAsync();

            GuildsEntity guild = await _dbActions.GetGuildAsync(context.Guild.Id);
            AzuraCastEntity azuraCast = guild.AzuraCast ?? throw new InvalidOperationException("AzuraCast is null");
            AzuraCastStationEntity station = azuraCast.Stations.FirstOrDefault(s => s.StationId == stationId) ?? throw new InvalidOperationException("Station is null");
            string baseUrl = Crypto.Decrypt(azuraCast.BaseUrl);

            NowPlayingDataRecord nowPlaying = await _azuraCast.GetNowPlayingAsync(new(baseUrl), stationId);

            string? playlistName = null;
            if (station.ShowPlaylistInNowPlaying)
            {
                string apiKey = Crypto.Decrypt(station.ApiKey);
                IReadOnlyList<PlaylistRecord> playlist = await _azuraCast.GetPlaylistsAsync(new(baseUrl), apiKey, stationId);
                playlistName = playlist.Where(p => p.Name == nowPlaying.NowPlaying.Playlist).Select(p => p.Name).FirstOrDefault();
            }

            DiscordEmbed embed = EmbedBuilder.BuildMusicNowPlayingEmbed(nowPlaying, playlistName);

            await context.EditResponseAsync(embed);
        }
    }
}
