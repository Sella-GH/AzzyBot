using System;
using System.Diagnostics.CodeAnalysis;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

/// <summary>
/// Represents the data for a custom queue item that was requested by a user.
/// </summary>
public sealed record AzuraCustomQueueItemRecord
{
    /// <summary>
    /// The guild ID of the guild that requested the song.
    /// </summary>
    public ulong GuildId { get; init; }

    /// <summary>
    /// The base URI of the AzuraCast instance.
    /// </summary>
    public Uri BaseUri { get; init; }

    /// <summary>
    /// The station ID of the station that the song is being requested for.
    /// </summary>
    public int StationId { get; init; }

    /// <summary>
    /// Requestable ID unique identifier
    /// </summary>
    public string RequestId { get; init; }

    /// <summary>
    /// The song's 32-character unique identifier hash
    /// </summary>
    public string SongId { get; init; }

    /// <summary>
    /// The timestamp of when the song was requested.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    [SuppressMessage("Roslynator", "RCS1231:Make parameter ref read-only", Justification = "This is a constructor and does not allow referencing.")]
    public AzuraCustomQueueItemRecord(ulong guildId, Uri uri, int stationId, string requestId, string songId, DateTimeOffset timestamp)
    {
        GuildId = guildId;
        BaseUri = uri;
        StationId = stationId;
        RequestId = requestId;
        SongId = songId;
        Timestamp = timestamp;
    }
}
