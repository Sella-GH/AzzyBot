namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

public sealed record AzuraFileComplianceRecord
{
    public bool IsCompliant { get; init; }
    public bool TitleCompliance { get; init; }
    public bool PerformerCompliance { get; init; }

    public AzuraFileComplianceRecord(bool isCompliant, bool titleCompliance, bool performerCompliance)
    {
        IsCompliant = isCompliant;
        TitleCompliance = titleCompliance;
        PerformerCompliance = performerCompliance;
    }
}
