namespace AzzyBot.Core.Utilities.Records;

public sealed record AppNetworkStatsRecord
{
    public long Received { get; init; }
    public long Transmitted { get; init; }

    public AppNetworkStatsRecord(long received, long transmitted)
    {
        Received = received;
        Transmitted = transmitted;
    }
}
