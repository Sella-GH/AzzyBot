﻿using System.Text.Json.Serialization;

namespace AzzyBot;

public sealed record AzuraRequestQueueItemRecord
{
    [JsonPropertyName("timestamp")]
    public required int Timestamp { get; init; }

    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("track")]
    public required AzuraTrackRecord Track { get; init; }
}
