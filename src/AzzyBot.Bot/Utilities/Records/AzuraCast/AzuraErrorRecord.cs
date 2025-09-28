using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

public sealed record AzuraErrorRecord
{
    /// <summary>
    /// The numeric code of the error.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; init; }

    /// <summary>
    /// The programmatic class of error.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; }

    /// <summary>
    /// The text description of the error.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; init; }

    /// <summary>
    /// The HTML-formatted text description of the error.
    /// </summary>
    [JsonPropertyName("formatted_message")]
    public string FormattedMessage { get; init; }

    public AzuraErrorRecord(int code, string type, string message, string formattedMessage)
    {
        Code = code;
        Type = type;
        Message = message;
        FormattedMessage = formattedMessage;
    }
}
