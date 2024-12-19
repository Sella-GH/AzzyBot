using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzzyBot.Data.Settings;

public sealed record AppDatabaseSettings
{
    [Required, Length(32, 32, ErrorMessage = $"The {nameof(EncryptionKey)} must contain exactly 32 characters!")]
    public required string EncryptionKey { get; set; }

    [Length(32, 32, ErrorMessage = $"The {nameof(NewEncryptionKey)} must contain exactly 32 characters!")]
    public string? NewEncryptionKey { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Host { get; init; } = "AzzyBot-Db";

    [Range(0, ushort.MaxValue), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Port { get; init; } = 5432;

    [Length(1, 63), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string User { get; init; } = "azzybot";

    [Length(1, 1000), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Password { get; init; } = "thisIsAzzyB0!P@ssw0rd";

    [Length(1, 63), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string DatabaseName { get; init; } = "azzybot";
}
