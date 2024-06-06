using System.Text.Json.Serialization;

namespace AzzyBot.Utilities.Records.AzuraCast;

public sealed record PlaylistRecord
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("is_enabled")]
    public bool IsEnabled { get; init; }

    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("short_name")]
    public string ShortName { get; init; } = string.Empty;
}