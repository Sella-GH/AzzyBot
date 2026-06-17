using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;

namespace AzzyBot.Bot.Structs;

[StructLayout(LayoutKind.Auto)]
public readonly struct AzzyDebugWebRequestStruct : IEquatable<AzzyDebugWebRequestStruct>
{
    public required Uri RequestUri { get; init; }
    public required HttpMethod Method { get; init; }
    public required Version HttpVersion { get; init; }
    public required HttpStatusCode StatusCode { get; init; }
    public required HttpRequestHeaders ReqHeaders { get; init; }
    public required HttpResponseHeaders ResHeaders { get; init; }
    public required int Retries { get; init; }
    public string? Content { get; init; }

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is AzzyDebugWebRequestStruct other && Equals(other);

    public bool Equals(AzzyDebugWebRequestStruct other)
        => RequestUri == other.RequestUri &&
            Method == other.Method &&
            HttpVersion == other.HttpVersion &&
            StatusCode == other.StatusCode &&
            ReqHeaders == other.ReqHeaders &&
            ResHeaders == other.ResHeaders &&
            Retries == other.Retries &&
            Content == other.Content;

    public override int GetHashCode()
        => HashCode.Combine(RequestUri, Method, HttpVersion, StatusCode, ReqHeaders, ResHeaders, Retries, Content);

    public static bool operator ==(in AzzyDebugWebRequestStruct left, in AzzyDebugWebRequestStruct right)
        => left.Equals(right);

    public static bool operator !=(in AzzyDebugWebRequestStruct left, in AzzyDebugWebRequestStruct right)
        => !left.Equals(right);
}
