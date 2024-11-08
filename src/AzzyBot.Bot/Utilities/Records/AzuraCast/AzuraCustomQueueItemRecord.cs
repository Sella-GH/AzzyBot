using System;
using System.Diagnostics.CodeAnalysis;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

public sealed record AzuraCustomQueueItemRecord
{
    public ulong GuildId { get; init; }
    public Uri BaseUri { get; init; }
    public int StationId { get; init; }
    public string SongId { get; init; }
    public DateTimeOffset Timestamp { get; set; }

    [SuppressMessage("Roslynator", "RCS1231:Make parameter ref read-only", Justification = "This is a constructor and does not allow referencing.")]
    public AzuraCustomQueueItemRecord(ulong guildId, Uri uri, int stationId, string songId, DateTimeOffset timestamp)
    {
        GuildId = guildId;
        BaseUri = uri;
        StationId = stationId;
        SongId = songId;
        Timestamp = timestamp;
    }
}
