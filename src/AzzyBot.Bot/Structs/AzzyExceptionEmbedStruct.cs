using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace AzzyBot.Bot.Structs;

[StructLayout(LayoutKind.Auto)]
public readonly struct AzzyExceptionEmbedStruct : IEquatable<AzzyExceptionEmbedStruct>
{
    public required Exception Exception { get; init; }
    public required string Timestamp { get; init; }
    public string? JsonMessage { get; init; }
    public string? Guild { get; init; }
    public string? Message { get; init; }
    public string? UserMention { get; init; }
    public string? CommandName { get; init; }
    public IReadOnlyDictionary<string, string>? CommandOptions { get; init; }

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is AzzyExceptionEmbedStruct other && Equals(other);

    public bool Equals(AzzyExceptionEmbedStruct other)
        => Exception.Equals(other.Exception) &&
            Timestamp == other.Timestamp &&
            JsonMessage == other.JsonMessage &&
            Guild == other.Guild &&
            Message == other.Message &&
            UserMention == other.UserMention &&
            CommandName == other.CommandName &&
            EqualityComparer<IReadOnlyDictionary<string, string>?>.Default.Equals(CommandOptions, other.CommandOptions);

    public override int GetHashCode()
        => HashCode.Combine(Exception, Timestamp, JsonMessage, Guild, Message, UserMention, CommandName, CommandOptions);

    public static bool operator ==(in AzzyExceptionEmbedStruct left, in AzzyExceptionEmbedStruct right)
        => left.Equals(right);

    public static bool operator !=(in AzzyExceptionEmbedStruct left, in AzzyExceptionEmbedStruct right)
        => !left.Equals(right);
}
