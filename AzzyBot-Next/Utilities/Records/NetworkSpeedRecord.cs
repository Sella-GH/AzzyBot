﻿namespace AzzyBot.Utilities.Records;

public sealed record NetworkSpeedRecord
{
    public double Received { get; init; }
    public double Transmitted { get; init; }

    public NetworkSpeedRecord(double received, double transmitted)
    {
        Received = received;
        Transmitted = transmitted;
    }
}
