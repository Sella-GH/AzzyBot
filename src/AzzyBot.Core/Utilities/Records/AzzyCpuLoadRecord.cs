namespace AzzyBot.Core.Utilities.Records;

public sealed record AzzyCpuLoadRecord
{
    public double OneMin { get; init; }
    public double FiveMin { get; init; }
    public double FifteenMin { get; init; }

    public AzzyCpuLoadRecord(double oneMin, double fiveMin, double fifteenMin)
    {
        OneMin = oneMin;
        FiveMin = fiveMin;
        FifteenMin = fifteenMin;
    }
}
