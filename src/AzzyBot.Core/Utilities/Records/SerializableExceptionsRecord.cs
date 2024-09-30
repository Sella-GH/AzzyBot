using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace AzzyBot.Core.Utilities.Records;

public sealed record SerializableExceptionsRecord
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Source { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AdditionalInfo { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? StackTrace { get; init; }

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
