namespace AzzyBot.Bot.Utilities.Records;

/// <summary>
/// Represents an embed field for a Discord message.
/// </summary>
public sealed record AzzyDiscordEmbedRecord
{
    /// <summary>
    /// The description of the field.
    /// </summary>
    public string Description { get; init; }

    /// <summary>
    /// Whether the field is inline.
    /// </summary>
    public bool IsInline { get; init; }

    public AzzyDiscordEmbedRecord(string description, bool isInline = false)
    {
        Description = description;
        IsInline = isInline;
    }
}
