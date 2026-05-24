using System;
using System.Runtime.InteropServices;

using DSharpPlus.Entities;

namespace AzzyBot.Bot.Structs;

[StructLayout(LayoutKind.Auto)]
public readonly struct AzzyInactiveGuildStruct : IEquatable<AzzyInactiveGuildStruct>
{
    public DiscordGuild Guild { get; init; }
    public bool NoConfig { get; init; }
    public bool NoLegals { get; init; }

    public override bool Equals(object? obj)
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
