﻿namespace AzzyBot.Utilities.Records;

public sealed record CpuLoadRecord
{
    public double OneMin { get; init; }
    public double FiveMin { get; init; }
    public double FifteenMin { get; init; }

    public CpuLoadRecord(double oneMin, double fiveMin, double fifteenMin)
    {
        OneMin = oneMin;
        FiveMin = fiveMin;
        FifteenMin = fifteenMin;
    }
}
