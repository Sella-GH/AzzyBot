namespace AzzyBot.Bot.Utilities.Records.AzuraCast;

public sealed record AzuraPlaylistStateRecord
{
    public string PlaylistName { get; init; }
    public bool PlaylistState { get; init; }

    public AzuraPlaylistStateRecord(string playlistName, bool playlistState)
    {
        PlaylistName = playlistName;
        PlaylistState = playlistState;
    }
}
