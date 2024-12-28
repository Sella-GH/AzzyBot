namespace AzzyBot.Core.Utilities.Records;

/// <summary>
/// Represents the memory usage of the hardware.
/// </summary>
public sealed record AppMemoryUsageRecord
{
    /// <summary>
    /// Total memory available.
    /// </summary>
    public double Total { get; init; }

    /// <summary>
    /// Memory used.
    /// </summary>
    public double Used { get; init; }

    public AppMemoryUsageRecord(double total, double used)
    {
        Total = total;
        Used = used;
    }
}
