namespace AzzyBot.Web.Utilities.Records;

public sealed record AzzyBotWebSettingsRecord
{
    public AzzyBotWebHttpsRecord? Https { get; init; }
}

public sealed record AzzyBotWebHttpsRecord
{
    public string? CertificatePath { get; init; }
    public string? CertificateKeyPath { get; init; }
}
