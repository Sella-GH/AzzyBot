namespace AzzyBot.Core.Models;

/// <summary>
/// Represents the memory usage of the hardware.
/// </summary>
public sealed record class AppMemoryUsageModel
{
    /// <summary>
    /// Total memory available.
    /// </summary>
    public double Total { get; init; }

    /// <summary>
    /// Memory used.
    /// </summary>
    public double Used { get; init; }

    public AppMemoryUsageModel(double total, double used)
    {
        Total = total;
        Used = used;
    }
}
