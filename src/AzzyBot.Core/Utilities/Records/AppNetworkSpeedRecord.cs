namespace AzzyBot.Core.Utilities.Records;

/// <summary>
/// Represents the network speed of the hardware.
/// </summary>
public sealed record AppNetworkSpeedRecord
{
    /// <summary>
    /// The amount of data received.
    /// </summary>
    public double Received { get; init; }

    /// <summary>
    /// The amount of data transmitted.
    /// </summary>
    public double Transmitted { get; init; }

    public AppNetworkSpeedRecord(double received, double transmitted)
    {
        Received = received;
        Transmitted = transmitted;
    }
}
