namespace AzzyBot.Core.Models;

/// <summary>
/// Represents the network speed of the hardware.
/// </summary>
public sealed record class AppNetworkSpeedModel
{
    /// <summary>
    /// The amount of data received.
    /// </summary>
    public double Received { get; init; }

    /// <summary>
    /// The amount of data transmitted.
    /// </summary>
    public double Transmitted { get; init; }

    public AppNetworkSpeedModel(double received, double transmitted)
    {
        Received = received;
        Transmitted = transmitted;
    }
}
