namespace AzzyBot.Core.Utilities.Records;

public sealed record AppMemoryUsageRecord
{
    public double Total { get; init; }
    public double Used { get; init; }

    public AppMemoryUsageRecord(double total, double used)
    {
        Total = total;
        Used = used;
    }
}
