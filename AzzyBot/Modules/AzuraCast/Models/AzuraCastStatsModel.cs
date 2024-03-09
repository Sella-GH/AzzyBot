using System.Collections.Generic;
using Newtonsoft.Json;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class SystemData
{
    [JsonProperty("cpu")]
    public CpuData Cpu { get; set; } = new();

    [JsonProperty("memory")]
    public MemoryData Memory { get; set; } = new();

    [JsonProperty("disk")]
    public DiskData Disk { get; set; } = new();

    [JsonProperty("network")]
    public List<NetworkData> Network { get; set; } = [];
}

internal sealed class CpuData
{
    [JsonProperty("total")]
    public TotalData Total { get; set; } = new();

    [JsonProperty("cores")]
    public List<CoreData> Cores { get; set; } = [];

    [JsonProperty("load")]
    public List<double> Load { get; set; } = [];
}

internal class TotalData
{
    [JsonProperty("usage")]
    public string Usage { get; set; } = string.Empty;
}

internal sealed class CoreData : TotalData;

internal sealed class ByteData
{
    [JsonProperty("total")]
    public string Total { get; set; } = string.Empty;

    [JsonProperty("cached")]
    public string Cached { get; set; } = string.Empty;

    [JsonProperty("used")]
    public string Used { get; set; } = string.Empty;
}

internal sealed class MemoryData
{
    [JsonProperty("bytes")]
    public ByteData Bytes { get; set; } = new();
}

internal sealed class DiskData
{
    [JsonProperty("bytes")]
    public ByteData Bytes { get; set; } = new();
}

internal sealed class SpeedData
{
    [JsonProperty("bytes")]
    public string Bytes { get; set; } = string.Empty;
}

internal sealed class TransmissionData
{
    [JsonProperty("speed")]
    public SpeedData Speed { get; set; } = new();
}

internal sealed class NetworkData
{
    [JsonProperty("interface_name")]
    public string InterfaceName { get; set; } = string.Empty;

    [JsonProperty("received")]
    public TransmissionData Received { get; set; } = new();

    [JsonProperty("transmitted")]
    public TransmissionData Transmitted { get; set; } = new();
}
