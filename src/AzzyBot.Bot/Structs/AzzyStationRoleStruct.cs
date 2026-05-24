using System;

namespace AzzyBot.Bot.Structs;

public readonly struct AzzyStationRoleStruct(ulong id, string name) : IEquatable<AzzyStationRoleStruct>
{
    public ulong Id { get; } = id;
    public string Name { get; } = name;

    public override bool Equals(object? obj)
        => obj is AzzyStationRoleStruct other && Equals(other);

    public bool Equals(AzzyStationRoleStruct other)
        => Id == other.Id && Name == other.Name;

    public override int GetHashCode()
        => HashCode.Combine(Id, Name);

    public static bool operator ==(AzzyStationRoleStruct? left, AzzyStationRoleStruct? right)
        => left?.Equals(right) is true;

    public static bool operator !=(AzzyStationRoleStruct? left, AzzyStationRoleStruct? right)
        => !left?.Equals(right) is true;
}
