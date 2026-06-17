using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace AzzyBot.Bot.Structs;

[SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "Not relevant here.")]
[StructLayout(LayoutKind.Auto)]
public readonly struct EmbedAuthorStruct : IEquatable<EmbedAuthorStruct>
{
    public string? Name { get; init; }
    public string? Url { get; init; }
    public string? IconUrl { get; init; }

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is EmbedAuthorStruct other && Equals(other);

    public bool Equals(EmbedAuthorStruct other)
        => Name == other.Name &&
            Url == other.Url &&
            IconUrl == other.IconUrl;

    public override int GetHashCode()
        => HashCode.Combine(Name, Url, IconUrl);

    public static bool operator ==(in EmbedAuthorStruct left, in EmbedAuthorStruct right)
        => left.Equals(right);

    public static bool operator !=(in EmbedAuthorStruct left, in EmbedAuthorStruct right)
        => !left.Equals(right);
}
