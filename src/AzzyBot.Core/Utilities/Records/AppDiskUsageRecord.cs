namespace AzzyBot.Core.Utilities.Records;

/// <summary>
/// Represents the disk usage of the hardware.
/// </summary>
public sealed record AppDiskUsageRecord
{
    /// <summary>
    /// The total size of the disk.
    /// </summary>
    public double TotalSize { get; init; }

    /// <summary>
    /// The total free space on the disk.
    /// </summary>
    public double TotalFreeSpace { get; init; }

    /// <summary>
    /// The total used space on the disk.
    /// </summary>
    public double TotalUsedSpace { get; init; }

    public AppDiskUsageRecord(double totalSize, double totalFreeSpace, double totalUsedSpace)
    {
        TotalSize = totalSize;
        TotalFreeSpace = totalFreeSpace;
        TotalUsedSpace = totalUsedSpace;
    }
}
