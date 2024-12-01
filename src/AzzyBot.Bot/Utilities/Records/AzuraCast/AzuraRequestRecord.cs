using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

/// <summary>
/// Represents the data for an internal song request.
/// </summary>
public sealed record AzuraInternalRequestRecord
{
    /// <summary>
    /// The current directory of the request.
    /// </summary>
    [JsonPropertyName("current_directory")]
    public string CurrentDirectory { get; init; }

    /// <summary>
    /// The directories that are part of the request.
    /// </summary>
    [JsonPropertyName("dirs")]
    public IReadOnlyList<string> Directories { get; init; } = [];

    /// <summary>
    /// What the request is doing.
    /// </summary>
    [JsonPropertyName("do")]
    public string Do { get; init; }

    /// <summary>
    /// The files paths that are part of the request.
    /// </summary>
    [JsonPropertyName("files")]
    public IReadOnlyList<string> Files { get; init; }

    public AzuraInternalRequestRecord(string currDir, string doing, IReadOnlyList<string> files)
    {
        CurrentDirectory = currDir;
        Do = doing;
        Files = files;
    }
}

/// <summary>
/// Represents the data for a song request.
/// </summary>
public sealed record AzuraRequestRecord
{
    /// <summary>
    /// Requestable ID unique identifier
    /// </summary>
    [JsonPropertyName("request_id")]
    public required string RequestId { get; set; }

    /// <summary>
    /// The song that is attached to the identifier.
    /// </summary>
    [JsonPropertyName("song")]
    public required AzuraSongDataRecord Song { get; set; }
}

/// <summary>
/// Represents a song request inside the queue.
/// </summary>
public sealed record AzuraRequestQueueItemRecord
{
    /// <summary>
    /// The timestamp of when the song was requested.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public required int Timestamp { get; init; }

    /// <summary>
    /// The id of the request in the queue.
    /// </summary>
    [JsonPropertyName("id")]
    public required int Id { get; init; }

    /// <summary>
    /// A portion of data connected to the song requested.
    /// </summary>
    [JsonPropertyName("track")]
    public required AzuraTrackRecord Track { get; init; }
}
