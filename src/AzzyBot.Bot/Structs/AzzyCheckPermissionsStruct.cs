using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using AzzyBot.Data.Entities;

using DSharpPlus.Entities;

namespace AzzyBot.Bot.Structs;

[StructLayout(LayoutKind.Auto)]
public readonly struct AzzyCheckPermissionsStruct : IEquatable<AzzyCheckPermissionsStruct>
{
    public DiscordGuild? DiscordGuild { get; init; }
    public IReadOnlyList<ulong>? DiscordGuildIds { get; init; }
    public GuildEntity? GuildEntity { get; init; }

    public override bool Equals(object? obj)
        => obj is AzzyCheckPermissionsStruct other && Equals(other);

    public bool Equals(AzzyCheckPermissionsStruct other)
        => EqualityComparer<DiscordGuild?>.Default.Equals(DiscordGuild, other.DiscordGuild) &&
            EqualityComparer<IReadOnlyList<ulong>?>.Default.Equals(DiscordGuildIds, other.DiscordGuildIds) &&
            EqualityComparer<GuildEntity?>.Default.Equals(GuildEntity, other.GuildEntity);

    public override int GetHashCode()
        => HashCode.Combine(DiscordGuild, DiscordGuildIds, GuildEntity);

    public static bool operator==(in AzzyCheckPermissionsStruct left, in AzzyCheckPermissionsStruct right)
        => left.Equals(right);

    public static bool operator !=(in AzzyCheckPermissionsStruct left, in AzzyCheckPermissionsStruct right)
        => !left.Equals(right);
}
