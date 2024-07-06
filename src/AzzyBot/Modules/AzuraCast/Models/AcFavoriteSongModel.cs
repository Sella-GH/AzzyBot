using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AzzyBot.Modules.AzuraCast.Models;

internal sealed class AcFavoriteSongModel
{
    [JsonPropertyName(nameof(UserSongList))]
    public List<UserSongList> UserSongList { get; set; } = [];
}

internal sealed class UserSongList
{
    [JsonPropertyName(nameof(SongId))]
    public string SongId { get; set; } = string.Empty;

    [JsonPropertyName(nameof(UserId))]
    public ulong UserId { get; set; }
}
