namespace AzzyBot.Core.Utilities.Records;

public sealed record AppDiskUsageRecord
{
    public double TotalSize { get; init; }
    public double TotalFreeSpace { get; init; }
    public double TotalUsedSpace { get; init; }

    public AppDiskUsageRecord(double totalSize, double totalFreeSpace, double totalUsedSpace)
    {
        TotalSize = totalSize;
        TotalFreeSpace = totalFreeSpace;
        TotalUsedSpace = totalUsedSpace;
    }
}
