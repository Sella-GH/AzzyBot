using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AzzyBot.Utilities.Records.AzuraCast;

public sealed record HardwareStatsRecord
{
    [JsonPropertyName("cpu")]
    public required CpuData Cpu { get; init; }

    [JsonPropertyName("memory")]
    public required MemoryData Memory { get; init; }

    [JsonPropertyName("disk")]
    public required DiskData Disk { get; init; }

    [JsonPropertyName("network")]
    public required IReadOnlyList<NetworkData> Network { get; init; }
}

public sealed record CpuData
{
    [JsonPropertyName("total")]
    public required CpuTotalData Total { get; init; }

    [JsonPropertyName("cores")]
    public required IReadOnlyList<CoreData> Cores { get; init; }

    [JsonPropertyName("load")]
    public required IReadOnlyList<double> Load { get; init; }
}

public record CpuTotalData
{
    [JsonPropertyName("usage")]
    public required string Usage { get; init; }

    [JsonPropertyName("io_wait")]
    public required string IoWait { get; init; }

    [JsonPropertyName("steal")]
    public required string Steal { get; init; }
}

public sealed record CoreData : CpuTotalData;

public sealed record MemoryData
{
    [JsonPropertyName("readable")]
    public required ReadableData Readable { get; init; }
}

public sealed record DiskData
{
    [JsonPropertyName("readable")]
    public required ReadableData Readable { get; init; }
}

public sealed record ReadableData
{
    [JsonPropertyName("total")]
    public required string Total { get; init; }

    [JsonPropertyName("free")]
    public required string Free { get; init; }

    [JsonPropertyName("cached")]
    public required string Cached { get; init; }

    [JsonPropertyName("used")]
    public required string Used { get; init; }
}

public sealed record NetworkData
{
    [JsonPropertyName("interface_name")]
    public required string InterfaceName { get; set; }

    [JsonPropertyName("received")]
    public required TransmissionData Received { get; set; }

    [JsonPropertyName("transmitted")]
    public required TransmissionData Transmitted { get; set; }
}

public sealed record SpeedData
{
    [JsonPropertyName("readable")]
    public required string Readable { get; init; }
}

public sealed record TransmissionData
{
    [JsonPropertyName("speed")]
    public required SpeedData Speed { get; init; }
}
