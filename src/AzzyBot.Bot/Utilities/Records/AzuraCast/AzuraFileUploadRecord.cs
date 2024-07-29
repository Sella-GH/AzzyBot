using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

public sealed record AzuraFileUploadRecord
{
    [JsonPropertyName("path")]
    public string Path { get; init; }

    [JsonPropertyName("file")]
    public string File { get; init; }

    public AzuraFileUploadRecord(string path, string file)
    {
        Path = path;
        File = file;
    }
}
