namespace AzzyBot.Utilities.Records.AzuraCast;

public sealed record AzuraPlaylistStateRecord(string name, bool state)
{
    public string PlaylistName { get; init; } = name;
    public bool PlaylistState { get; init; } = state;
}
