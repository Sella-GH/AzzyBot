using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class SystemData
{
    [JsonPropertyName("cpu")]
    public CpuData Cpu { get; set; } = new();

    [JsonPropertyName("memory")]
    public MemoryData Memory { get; set; } = new();

    [JsonPropertyName("disk")]
    public DiskData Disk { get; set; } = new();

    [JsonPropertyName("network")]
    public List<NetworkData> Network { get; set; } = [];
}

internal sealed class CpuData
{
    [JsonPropertyName("total")]
    public TotalData Total { get; set; } = new();

    [JsonPropertyName("cores")]
    public List<CoreData> Cores { get; set; } = [];

    [JsonPropertyName("load")]
    public List<double> Load { get; set; } = [];
}

internal class TotalData
{
    [JsonPropertyName("usage")]
    public string Usage { get; set; } = string.Empty;
}

internal sealed class CoreData : TotalData;

internal sealed class ByteData
{
    [JsonPropertyName("total")]
    public string Total { get; set; } = string.Empty;

    [JsonPropertyName("cached")]
    public string Cached { get; set; } = string.Empty;

    [JsonPropertyName("used")]
    public string Used { get; set; } = string.Empty;
}

internal sealed class MemoryData
{
    [JsonPropertyName("bytes")]
    public ByteData Bytes { get; set; } = new();
}

internal sealed class DiskData
{
    [JsonPropertyName("bytes")]
    public ByteData Bytes { get; set; } = new();
}

internal sealed class SpeedData
{
    [JsonPropertyName("bytes")]
    public string Bytes { get; set; } = string.Empty;
}

internal sealed class TransmissionData
{
    [JsonPropertyName("speed")]
    public SpeedData Speed { get; set; } = new();
}

internal sealed class NetworkData
{
    [JsonPropertyName("interface_name")]
    public string InterfaceName { get; set; } = string.Empty;

    [JsonPropertyName("received")]
    public TransmissionData Received { get; set; } = new();

    [JsonPropertyName("transmitted")]
    public TransmissionData Transmitted { get; set; } = new();
}
