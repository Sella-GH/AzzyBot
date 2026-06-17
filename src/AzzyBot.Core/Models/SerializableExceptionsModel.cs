using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace AzzyBot.Core.Models;

/// <summary>
/// Represents a serializable exception record.
/// </summary>
public sealed record class SerializableExceptionsModel
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
    /// The stack trace of the exception.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? StackTrace { get; init; }

    /// <summary>
    /// The inner exception of the exception.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SerializableExceptionsModel? InnerException { get; init; }

    public SerializableExceptionsModel(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        Source = ex.Source;
        Type = ex.GetType().ToString();
        Message = ex.Message;
        StackTrace = ex.StackTrace?.Split(Environment.NewLine).Select(static s => s.TrimStart()).ToList();
        InnerException = (ex.InnerException is not null) ? new SerializableExceptionsModel(ex.InnerException) : null;
    }
}
