namespace AzzyBot.Utilities.Records;

internal sealed record DiscordEmbedRecord
{
    public string Description { get; init; }
    public bool IsInline { get; init; }

    public DiscordEmbedRecord(string description, bool isInline = false)
    {
        Description = description;
        IsInline = isInline;
    }
}
