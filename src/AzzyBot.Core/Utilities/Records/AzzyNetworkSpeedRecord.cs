namespace AzzyBot.Core.Utilities.Records;

public sealed record AzzyNetworkSpeedRecord
{
    public double Received { get; init; }
    public double Transmitted { get; init; }

    public AzzyNetworkSpeedRecord(double received, double transmitted)
    {
        Received = received;
        Transmitted = transmitted;
    }
}
