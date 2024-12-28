namespace AzzyBot.Core.Utilities.Records;

/// <summary>
/// Represents the network statistics for the hardware.
/// </summary>
public sealed record AppNetworkStatsRecord
{
    /// <summary>
    /// The amount of data received.
    /// </summary>
    public long Received { get; init; }

    /// <summary>
    /// The amount of data transmitted.
    /// </summary>
    public long Transmitted { get; init; }

    public AppNetworkStatsRecord(long received, long transmitted)
    {
        Received = received;
        Transmitted = transmitted;
    }
}
