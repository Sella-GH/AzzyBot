using System;
using System.Diagnostics.CodeAnalysis;

namespace AzzyBot.Bot.Utilities.Structs;

[SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "Not relevant here.")]
[SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "Not relevant here.")]
public readonly struct EmbedAuthorStruct(string? name, string? url, string? iconUrl) : IEquatable<EmbedAuthorStruct>
{
    public string? Name { get; } = name;
    public string? Url { get; } = url;
    public string? IconUrl { get; } = iconUrl;

    public override bool Equals(object? obj)
        => obj is EmbedAuthorStruct other && Equals(other);

    public bool Equals(EmbedAuthorStruct other)
        => Name == other.Name &&
            Url == other.Url &&
            IconUrl == other.IconUrl;

    public override int GetHashCode()
        => HashCode.Combine(Name, Url, IconUrl);

    public static bool operator ==(EmbedAuthorStruct? left, EmbedAuthorStruct? right)
        => left?.Equals(right) is true;

    public static bool operator !=(EmbedAuthorStruct? left, EmbedAuthorStruct? right)
        => !left?.Equals(right) is true;
}
