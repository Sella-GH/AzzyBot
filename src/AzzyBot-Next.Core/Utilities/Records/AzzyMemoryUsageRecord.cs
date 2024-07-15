namespace AzzyBot.Core.Utilities.Records;

public sealed record AzzyMemoryUsageRecord
{
    public double Total { get; init; }
    public double Used { get; init; }

    public AzzyMemoryUsageRecord(double total, double used)
    {
        Total = total;
        Used = used;
    }
}
