using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace AzzyBot.Core.Utilities.Records;

/// <summary>
/// Represents a serializable exception record.
/// </summary>
public sealed record SerializableExceptionsRecord
{
    /// <summary>
    /// The source of the exception.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Source { get; init; }

    /// <summary>
    /// The type of the exception.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; init; }

    /// <summary>
    /// The message of the exception.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; init; }

    /// <summary>
    /// The additional information of the exception.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AdditionalInfo { get; init; }

    /// <summary>
    /// The stack trace of the exception.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? StackTrace { get; init; }

    /// <summary>
    /// The inner exception of the exception.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SerializableExceptionsRecord? InnerException { get; init; }

    public SerializableExceptionsRecord(Exception ex, string? info = null)
    {
        ArgumentNullException.ThrowIfNull(ex);

        Source = ex.Source;
        Type = ex.GetType().ToString();
        Message = ex.Message;
        AdditionalInfo = info;
        StackTrace = ex.StackTrace?.Split(Environment.NewLine).Select(static s => s.TrimStart()).ToList();
        InnerException = (ex.InnerException is not null) ? new SerializableExceptionsRecord(ex.InnerException) : null;
    }
}
