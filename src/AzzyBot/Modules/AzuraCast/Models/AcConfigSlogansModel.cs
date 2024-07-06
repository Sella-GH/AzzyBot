using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class AcConfigSlogansModel
{
    [JsonPropertyName(nameof(Slogans))]
    public Slogans Slogans { get; set; } = new();
}

internal sealed class Slogans
{
    [JsonPropertyName(nameof(DefaultSlogan))]
    public string DefaultSlogan { get; set; } = string.Empty;

    [JsonPropertyName(nameof(DefaultSloganListener))]
    public SloganContent DefaultSloganListener { get; set; } = new();

    [JsonPropertyName(nameof(LiveStream))]
    public string LiveStream { get; set; } = string.Empty;

    [JsonPropertyName(nameof(LiveStreamListener))]
    public SloganContent LiveStreamListener { get; set; } = new();

    [JsonPropertyName(nameof(SongRequests))]
    public string SongRequests { get; set; } = string.Empty;

    [JsonPropertyName(nameof(SongRequestListener))]
    public SloganContent SongRequestListener { get; set; } = new();

    [JsonPropertyName(nameof(UserDefined))]
    public List<UserDefined> UserDefined { get; set; } = [];
}

internal class SloganContent
{
    [JsonPropertyName(nameof(None))]
    public string None { get; set; } = string.Empty;

    [JsonPropertyName(nameof(OnePerson))]
    public string OnePerson { get; set; } = string.Empty;

    [JsonPropertyName(nameof(TwoPersons))]
    public string TwoPersons { get; set; } = string.Empty;

    [JsonPropertyName(nameof(Multiple))]
    public string Multiple { get; set; } = string.Empty;
}

internal sealed class UserDefined : SloganContent
{
    [JsonPropertyName(nameof(Name))]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName(nameof(Slogan))]
    public string Slogan { get; set; } = string.Empty;
}
