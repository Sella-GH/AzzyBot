using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using Npgsql;

namespace AzzyBot.Data.Settings;

public sealed record DatabaseSettings
{
    /// <summary>
    /// The primary encryption key used to encrypt/decrypt sensitive data in the database.
    /// </summary>
    [Required, StringLength(32, MinimumLength = 32, ErrorMessage = $"The {nameof(EncryptionKey)} must contain exactly 32 characters!")]
    public required string EncryptionKey { get; set; }

    /// <summary>
    /// The new encryption key used to rotate the encryption key. Must be exactly 32 characters long.
    /// </summary>
    public string? NewEncryptionKey { get; set; }

    /// <summary>
    /// The hostname of the database server.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Host { get; set; } = "AzzyBot-Db";

    /// <summary>
    /// The port of the database server.
    /// </summary>
    [Range(0, ushort.MaxValue, ErrorMessage = "The database port is out of range. Please choose one between 0 and 65535."), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Port { get; set; } = 5432;

    /// <summary>
    /// The username used to connect to the database.
    /// </summary>
    [StringLength(63, MinimumLength = 1, ErrorMessage = "Your database username is not accepted. Please choose one between 1 and 63 characters."), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string User { get; set; } = "azzybot";

    /// <summary>
    /// The password used to connect to the database.
    /// </summary>
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "Your database password is not accepted. Please choose one between 1 and 1000 characters."), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Password { get; set; } = "thisIsAzzyB0!P@ssw0rd";

    /// <summary>
    /// The name of the database to connect to.
    /// </summary>
    [StringLength(63, MinimumLength = 1, ErrorMessage = "Your database name is not accepted. Please choose one between 1 and 63 characters."), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string DatabaseName { get; set; } = "azzybot";

    /// <summary>
    /// The version of the database server. Used to enable/disable certain features based on the version.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Version? DatabaseVersion { get; set; }

    /// <summary>
    /// Whether to use SSL to connect to the database.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool UseSsl { get; set; }

    /// <summary>
    /// The SSL mode to use when connecting to the database.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public SslMode SslMode { get; set; } = SslMode.Prefer;

    /// <summary>
    /// The SSL negotiation strategy to use when connecting to the database.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public SslNegotiation SslNegotiation { get; set; } = SslNegotiation.Postgres;

    /// <summary>
    /// The root certificate to use when connecting to the database over SSL.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? SslRootCert { get; set; }

    /// <summary>
    /// The client certificate to use when connecting to the database over SSL.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? SslCert { get; set; }

    /// <summary>
    /// The client certificate key to use when connecting to the database over SSL.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? SslPassword { get; set; }
}
