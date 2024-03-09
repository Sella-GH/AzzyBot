using System.Collections.Generic;
using Newtonsoft.Json;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class FavoriteSongModel
{
    [JsonProperty(nameof(UserSongList))]
    public List<UserSongList> UserSongList { get; set; } = [];
}

internal sealed class UserSongList
{
    [JsonProperty(nameof(SongId))]
    public string SongId { get; set; } = string.Empty;

    [JsonProperty(nameof(UserId))]
    public ulong UserId { get; set; }
}
