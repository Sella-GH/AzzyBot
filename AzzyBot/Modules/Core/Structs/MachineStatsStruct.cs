namespace AzzyBot.Modules.Core.Structs;

internal readonly struct CpuLoadStruct
{
    internal double OneMin { get; }
    internal double FiveMin { get; }
    internal double FifteenMin { get; }

    internal CpuLoadStruct(double oneMin, double fiveMin, double fifteenMin)
    {
        OneMin = oneMin;
        FiveMin = fiveMin;
        FifteenMin = fifteenMin;
    }
}

internal readonly struct MemoryUsageStruct
{
    internal double Total { get; }
    internal double Used { get; }

    internal MemoryUsageStruct(double total, double used)
    {
        Total = total;
        Used = used;
    }
}

internal readonly struct NetworkSpeedStruct
{
    internal double Received { get; }
    internal double Transmitted { get; }

    internal NetworkSpeedStruct(double received, double transmitted)
    {
        Received = received;
        Transmitted = transmitted;
    }
}

internal readonly struct NetworkStatsStruct
{
    internal long Received { get; }
    internal long Transmitted { get; }

    internal NetworkStatsStruct(long received, long transmitted)
    {
        Received = received;
        Transmitted = transmitted;
    }
}
