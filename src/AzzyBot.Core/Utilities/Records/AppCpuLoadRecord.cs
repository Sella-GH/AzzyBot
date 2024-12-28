namespace AzzyBot.Core.Utilities.Records;

/// <summary>
/// Represents the CPU load of the hardware.
/// </summary>
public sealed record AppCpuLoadRecord
{
    /// <summary>
    /// The CPU load for the last minute.
    /// </summary>
    public double OneMin { get; init; }

    /// <summary>
    /// The CPU load for the last five minutes.
    /// </summary>
    public double FiveMin { get; init; }

    /// <summary>
    /// The CPU load for the last fifteen minutes.
    /// </summary>
    public double FifteenMin { get; init; }

    public AppCpuLoadRecord(double oneMin, double fiveMin, double fifteenMin)
    {
        OneMin = oneMin;
        FiveMin = fiveMin;
        FifteenMin = fifteenMin;
    }
}
