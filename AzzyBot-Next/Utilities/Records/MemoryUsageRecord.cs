namespace AzzyBot.Utilities.Records;

public sealed record MemoryUsageRecord
{
    public double Total { get; init; }
    public double Used { get; init; }

    public MemoryUsageRecord(double total, double used)
    {
        Total = total;
        Used = used;
    }
}
