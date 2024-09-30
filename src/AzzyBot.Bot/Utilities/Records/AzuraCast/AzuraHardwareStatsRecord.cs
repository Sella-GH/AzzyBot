using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

public sealed record AzuraHardwareStatsRecord
{
    public long Ping { get; set; }

    [JsonPropertyName("cpu")]
    public required AzuraCpuData Cpu { get; init; }

    [JsonPropertyName("memory")]
    public required AzuraMemoryData Memory { get; init; }

    [JsonPropertyName("disk")]
    public required AzuraDiskData Disk { get; init; }

    [JsonPropertyName("network")]
    public required IReadOnlyList<AzuraNetworkData> Network { get; init; }
}

public sealed record AzuraCpuData
{
    [JsonPropertyName("total")]
    public required AzuraCpuTotalData Total { get; init; }

    [JsonPropertyName("cores")]
    public required IReadOnlyList<AzuraCoreData> Cores { get; init; }

    [JsonPropertyName("load")]
    public required IReadOnlyList<double> Load { get; init; }
}

public record AzuraCpuTotalData
{
    [JsonPropertyName("usage")]
    public required string Usage { get; init; }

    [JsonPropertyName("io_wait")]
    public required string IoWait { get; init; }

    [JsonPropertyName("steal")]
    public required string Steal { get; init; }
}

#pragma warning disable S2094 // Classes should not be empty - Required for deserialization
public sealed record AzuraCoreData : AzuraCpuTotalData;
#pragma warning restore S2094 // Classes should not be empty - Required for deserialization

public sealed record AzuraMemoryData
{
    [JsonPropertyName("readable")]
    public required AzuraReadableMemoryData Readable { get; init; }
}

public sealed record AzuraDiskData
{
    [JsonPropertyName("readable")]
    public required AzuraReadableDiskData Readable { get; init; }
}

public record AzuraReadableDiskData
{
    [JsonPropertyName("total")]
    public required string Total { get; init; }

    [JsonPropertyName("free")]
    public required string Free { get; init; }

    [JsonPropertyName("used")]
    public required string Used { get; init; }
}

public sealed record AzuraReadableMemoryData : AzuraReadableDiskData
{
    [JsonPropertyName("cached")]
    public required string Cached { get; init; }
}

public sealed record AzuraNetworkData
{
    [JsonPropertyName("interface_name")]
    public required string InterfaceName { get; set; }

    [JsonPropertyName("received")]
    public required AzuraTransmissionData Received { get; set; }

    [JsonPropertyName("transmitted")]
    public required AzuraTransmissionData Transmitted { get; set; }
}

public sealed record AzuraSpeedData
{
    [JsonPropertyName("readable")]
    public required string Readable { get; init; }
}

public sealed record AzuraTransmissionData
{
    [JsonPropertyName("speed")]
    public required AzuraSpeedData Speed { get; init; }
}
