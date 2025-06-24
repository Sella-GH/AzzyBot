using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using Npgsql;

namespace AzzyBot.Data.Settings;

public sealed record DatabaseSettings
{
    [Required, StringLength(32, MinimumLength = 32, ErrorMessage = $"The {nameof(EncryptionKey)} must contain exactly 32 characters!")]
    public required string EncryptionKey { get; set; }

    public string? NewEncryptionKey { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Host { get; set; } = "AzzyBot-Db";

    [Range(0, ushort.MaxValue, ErrorMessage = "The database port is out of range. Please choose one between 0 and 65535."), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Port { get; set; } = 5432;

    [StringLength(63, MinimumLength = 1, ErrorMessage = "Your database username is not accepted. Please choose one between 1 and 63 characters."), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string User { get; set; } = "azzybot";

    [StringLength(1000, MinimumLength = 1, ErrorMessage = "Your database password is not accepted. Please choose one between 1 and 1000 characters."), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Password { get; set; } = "thisIsAzzyB0!P@ssw0rd";

    [StringLength(63, MinimumLength = 1, ErrorMessage = "Your database name is not accepted. Please choose one between 1 and 63 characters."), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string DatabaseName { get; set; } = "azzybot";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool UseSsl { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public SslMode SslMode { get; set; } = SslMode.Prefer;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public SslNegotiation SslNegotiation { get; set; } = SslNegotiation.Postgres;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? SslRootCert { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? SslCert { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? SslPassword { get; set; }
}
