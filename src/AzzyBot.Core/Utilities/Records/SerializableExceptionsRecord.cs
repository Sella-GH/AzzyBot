using System;

namespace AzzyBot.Core.Utilities.Records;

public sealed record SerializableExceptionsRecord
{
    public string? Type { get; init; }
    public string? Message { get; init; }
    public string? StackTrace { get; init; }
    public string? Source { get; init; }
    public SerializableExceptionsRecord? InnerException { get; init; }

    public SerializableExceptionsRecord(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex, nameof(ex));

        Type = ex.GetType().ToString();
        Message = ex.Message;
        StackTrace = ex.StackTrace;
        Source = ex.Source;
        InnerException = (ex.InnerException is not null) ? new SerializableExceptionsRecord(ex.InnerException) : null;
    }
}
