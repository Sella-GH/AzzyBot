namespace AzzyBot.Bot.Utilities.Records;

public sealed record AzzyDiscordEmbedRecord
{
    public string Description { get; init; }
    public bool IsInline { get; init; }

    public AzzyDiscordEmbedRecord(string description, bool isInline = false)
    {
        Description = description;
        IsInline = isInline;
    }
}
