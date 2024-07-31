namespace AzzyBot.Core.Utilities.Records;

public sealed record AppNetworkSpeedRecord
{
    public double Received { get; init; }
    public double Transmitted { get; init; }

    public AppNetworkSpeedRecord(double received, double transmitted)
    {
        Received = received;
        Transmitted = transmitted;
    }
}
