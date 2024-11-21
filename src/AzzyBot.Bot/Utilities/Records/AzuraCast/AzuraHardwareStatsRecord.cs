using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

/// <summary>
/// Represents the hardware stats for an AzuraCast instance.
/// </summary>
public sealed record AzuraHardwareStatsRecord
{
    /// <summary>
    /// The ping to the server.
    /// </summary>
    public long Ping { get; set; }

    /// <summary>
    /// The server's cpu data.
    /// </summary>
    [JsonPropertyName("cpu")]
    public required AzuraCpuData Cpu { get; init; }

    /// <summary>
    /// The server's memory data.
    /// </summary>
    [JsonPropertyName("memory")]
    public required AzuraMemoryData Memory { get; init; }

    /// <summary>
    /// The server's disk data.
    /// </summary>
    [JsonPropertyName("disk")]
    public required AzuraDiskData Disk { get; init; }

    /// <summary>
    /// A list of the server's network data.
    /// </summary>
    [JsonPropertyName("network")]
    public required IReadOnlyList<AzuraNetworkData> Network { get; init; }
}

/// <summary>
/// Represents the cpu data for an AzuraCast instance.
/// </summary>
public sealed record AzuraCpuData
{
    /// <summary>
    /// The total cpu data for the server.
    /// </summary>
    [JsonPropertyName("total")]
    public required AzuraCpuTotalData Total { get; init; }

    /// <summary>
    /// The cpu data per core for the server.
    /// </summary>
    [JsonPropertyName("cores")]
    public required IReadOnlyList<AzuraCoreData> Cores { get; init; }

    /// <summary>
    /// The load data for the server.
    /// </summary>
    [JsonPropertyName("load")]
    public required IReadOnlyList<double> Load { get; init; }
}

/// <summary>
/// Represents the total cpu data for an AzuraCast instance.
/// </summary>
public record AzuraCpuTotalData
{
    /// <summary>
    /// The total cpu usage for the server.
    /// </summary>
    [JsonPropertyName("usage")]
    public required string Usage { get; init; }

    /// <summary>
    /// The total cpu waiting for io time for the server.
    /// </summary>
    [JsonPropertyName("io_wait")]
    public required string IoWait { get; init; }

    /// <summary>
    /// The total cpu steal time for the server.
    /// </summary>
    [JsonPropertyName("steal")]
    public required string Steal { get; init; }
}

#pragma warning disable S2094 // Classes should not be empty - Required for deserialization
/// <summary>
/// Represents the cpu data per core for an AzuraCast instance.
/// </summary>
public sealed record AzuraCoreData : AzuraCpuTotalData;
#pragma warning restore S2094 // Classes should not be empty - Required for deserialization

/// <summary>
/// Represents the memory data for an AzuraCast instance.
/// </summary>
public sealed record AzuraMemoryData
{
    /// <summary>
    /// The total memory data for the server.
    /// </summary>
    [JsonPropertyName("readable")]
    public required AzuraReadableMemoryData Readable { get; init; }
}

/// <summary>
/// Represents the disk data for an AzuraCast instance.
/// </summary>
public sealed record AzuraDiskData
{
    /// <summary>
    /// The total disk data for the server.
    /// </summary>
    [JsonPropertyName("readable")]
    public required AzuraReadableDiskData Readable { get; init; }
}

/// <summary>
/// Represents the readable disk data for an AzuraCast instance.
/// </summary>
public record AzuraReadableDiskData
{
    /// <summary>
    /// The total disk space for the server.
    /// </summary>
    [JsonPropertyName("total")]
    public required string Total { get; init; }

    /// <summary>
    /// The free disk space for the server.
    /// </summary>
    [JsonPropertyName("free")]
    public required string Free { get; init; }

    /// <summary>
    /// The used disk space for the server.
    /// </summary>
    [JsonPropertyName("used")]
    public required string Used { get; init; }
}

/// <summary>
/// Represents the readable memory data for an AzuraCast instance.
/// </summary>
public sealed record AzuraReadableMemoryData : AzuraReadableDiskData
{
    /// <summary>
    /// The cached memory for the server.
    /// </summary>
    [JsonPropertyName("cached")]
    public required string Cached { get; init; }
}

/// <summary>
/// Represents the network data for an AzuraCast instance.
/// </summary>
public sealed record AzuraNetworkData
{
    /// <summary>
    /// The interface name for the network data.
    /// </summary>
    [JsonPropertyName("interface_name")]
    public required string InterfaceName { get; set; }

    /// <summary>
    /// The received data for the network.
    /// </summary>
    [JsonPropertyName("received")]
    public required AzuraTransmissionData Received { get; set; }

    /// <summary>
    /// The transmitted data for the network.
    /// </summary>
    [JsonPropertyName("transmitted")]
    public required AzuraTransmissionData Transmitted { get; set; }
}

/// <summary>
/// Represents the transmission speed data for an AzuraCast instance.
/// </summary>
public sealed record AzuraSpeedData
{
    /// <summary>
    /// The speed of the transmission.
    /// </summary>
    [JsonPropertyName("readable")]
    public required string Readable { get; init; }
}

/// <summary>
/// Represents the transmission data for an AzuraCast instance.
/// </summary>
public sealed record AzuraTransmissionData
{
    /// <summary>
    /// The speed of the transmission.
    /// </summary>
    [JsonPropertyName("speed")]
    public required AzuraSpeedData Speed { get; init; }
}
