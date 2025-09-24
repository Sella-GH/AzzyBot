using System;

using DSharpPlus.Entities;

namespace AzzyBot.Bot.Utilities.Structs;

public readonly struct AzzyInactiveGuildStruct(DiscordGuild guild, bool config, bool legals) : IEquatable<AzzyInactiveGuildStruct>
{
    public DiscordGuild Guild { get; } = guild;
    public bool NoConfig { get; } = config;
    public bool NoLegals { get; } = legals;

    public override bool Equals(object? obj)
        => obj is AzzyInactiveGuildStruct other && Equals(other);

    public bool Equals(AzzyInactiveGuildStruct other)
        => Guild == other.Guild &&
            NoConfig == other.NoConfig &&
            NoLegals == other.NoLegals;

    public override int GetHashCode()
        => HashCode.Combine(Guild, NoConfig, NoLegals);

    public static bool operator ==(AzzyInactiveGuildStruct? left, AzzyInactiveGuildStruct? right)
        => left?.Equals(right) is true;

    public static bool operator !=(AzzyInactiveGuildStruct? left, AzzyInactiveGuildStruct? right)
        => !left?.Equals(right) is true;
}
