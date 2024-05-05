namespace AzzyBot.Utilities.Records;

internal sealed record DiskUsageRecord
{
    public double TotalSize { get; init; }
    public double TotalFreeSpace { get; init; }
    public double TotalUsedSpace { get; init; }

    public DiskUsageRecord(double totalSize, double totalFreeSpace, double totalUsedSpace)
    {
        TotalSize = totalSize;
        TotalFreeSpace = totalFreeSpace;
        TotalUsedSpace = totalUsedSpace;
    }
}
