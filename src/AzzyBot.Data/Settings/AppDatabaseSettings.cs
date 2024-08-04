using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AzzyBot.Data.Settings;

[SuppressMessage("Roslynator", "RCS1181:Convert comment to documentation comment", Justification = "Informational comment")]
public sealed record AppDatabaseSettings
{
    public required string EncryptionKey { get; set; } // 32 Characters
    public string? NewEncryptionKey { get; set; } // 32 Characters

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Host { get; init; } = "AzzyBot-Db";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Port { get; init; } = 5432;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? User { get; init; } = "azzybot";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Password { get; init; } = "thisIsAzzyB0!P@ssw0rd";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? DatabaseName { get; init; } = "azzybot";
}
