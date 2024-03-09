namespace AzzyBot.Modules.Core.Structs;

internal readonly struct DiscordEmbedStruct
{
    internal string Name { get; }
    internal string Description { get; }
    internal bool IsInline { get; }

    internal DiscordEmbedStruct(string name, string description, bool isInline)
    {
        Name = name;
        Description = description;
        IsInline = isInline;
    }
}
