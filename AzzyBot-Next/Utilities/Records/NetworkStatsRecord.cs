namespace AzzyBot.Utilities.Records;

internal sealed record NetworkStatsRecord
{
    public long Received { get; init; }
    public long Transmitted { get; init; }

    public NetworkStatsRecord(long received, long transmitted)
    {
        Received = received;
        Transmitted = transmitted;
    }
}
