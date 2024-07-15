namespace AzzyBot.Core.Utilities.Records;

public sealed record AzzyNetworkStatsRecord
{
    public long Received { get; init; }
    public long Transmitted { get; init; }

    public AzzyNetworkStatsRecord(long received, long transmitted)
    {
        Received = received;
        Transmitted = transmitted;
    }
}
