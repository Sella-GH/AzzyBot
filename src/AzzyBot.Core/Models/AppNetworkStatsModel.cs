namespace AzzyBot.Core.Models;

/// <summary>
/// Represents the network statistics for the hardware.
/// </summary>
public sealed record class AppNetworkStatsModel
{
    /// <summary>
    /// The amount of data received.
    /// </summary>
    public long Received { get; init; }

    /// <summary>
    /// The amount of data transmitted.
    /// </summary>
    public long Transmitted { get; init; }

    public AppNetworkStatsModel(long received, long transmitted)
    {
        Received = received;
        Transmitted = transmitted;
    }
}
