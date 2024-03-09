using System.Collections.Generic;
using Newtonsoft.Json;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class ConfigSlogansModel
{
    [JsonProperty(nameof(Slogans))]
    public Slogans Slogans { get; set; } = new();
}

internal sealed class Slogans
{
    [JsonProperty(nameof(DefaultSlogan))]
    public string DefaultSlogan { get; set; } = string.Empty;

    [JsonProperty(nameof(DefaultSloganListener))]
    public SloganContent DefaultSloganListener { get; set; } = new();

    [JsonProperty(nameof(LiveStream))]
    public string LiveStream { get; set; } = string.Empty;

    [JsonProperty(nameof(LiveStreamListener))]
    public SloganContent LiveStreamListener { get; set; } = new();

    [JsonProperty(nameof(SongRequests))]
    public string SongRequests { get; set; } = string.Empty;

    [JsonProperty(nameof(SongRequestListener))]
    public SloganContent SongRequestListener { get; set; } = new();

    [JsonProperty(nameof(UserDefined))]
    public List<UserDefined> UserDefined { get; set; } = [];
}

internal class SloganContent
{
    [JsonProperty(nameof(None))]
    public string None { get; set; } = string.Empty;

    [JsonProperty(nameof(OnePerson))]
    public string OnePerson { get; set; } = string.Empty;

    [JsonProperty(nameof(TwoPersons))]
    public string TwoPersons { get; set; } = string.Empty;

    [JsonProperty(nameof(Multiple))]
    public string Multiple { get; set; } = string.Empty;
}

internal sealed class UserDefined : SloganContent
{
    [JsonProperty(nameof(Name))]
    public string Name { get; set; } = string.Empty;

    [JsonProperty(nameof(Slogan))]
    public string Slogan { get; set; } = string.Empty;
}
