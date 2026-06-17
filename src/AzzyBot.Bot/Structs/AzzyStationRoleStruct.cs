using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace AzzyBot.Bot.Structs;

[StructLayout(LayoutKind.Auto)]
public readonly struct AzzyStationRoleStruct : IEquatable<AzzyStationRoleStruct>
{
    public required ulong Id { get; init; }
    public required string Name { get; init; }

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is AzzyStationRoleStruct other && Equals(other);

    public bool Equals(AzzyStationRoleStruct other)
        => Id == other.Id && Name == other.Name;

    public override int GetHashCode()
        => HashCode.Combine(Id, Name);

    public static bool operator ==(in AzzyStationRoleStruct left, in AzzyStationRoleStruct right)
        => left.Equals(right);

    public static bool operator !=(in AzzyStationRoleStruct left, in AzzyStationRoleStruct right)
        => !left.Equals(right);
}
