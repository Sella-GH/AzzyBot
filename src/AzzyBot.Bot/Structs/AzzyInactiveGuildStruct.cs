using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using DSharpPlus.Entities;

namespace AzzyBot.Bot.Structs;

[StructLayout(LayoutKind.Auto)]
public readonly struct AzzyInactiveGuildStruct : IEquatable<AzzyInactiveGuildStruct>
{
    public required DiscordGuild Guild { get; init; }
    public required bool NoConfig { get; init; }
    public required bool NoLegals { get; init; }

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is AzzyInactiveGuildStruct other && Equals(other);

    public bool Equals(AzzyInactiveGuildStruct other)
        => Guild == other.Guild &&
            NoConfig == other.NoConfig &&
            NoLegals == other.NoLegals;

    public override int GetHashCode()
        => HashCode.Combine(Guild, NoConfig, NoLegals);

    public static bool operator ==(in AzzyInactiveGuildStruct left, in AzzyInactiveGuildStruct right)
        => left.Equals(right);

    public static bool operator !=(in AzzyInactiveGuildStruct left, in AzzyInactiveGuildStruct right)
        => !left.Equals(right);
}
