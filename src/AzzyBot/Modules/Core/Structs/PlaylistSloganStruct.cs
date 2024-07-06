namespace AzzyBot.Modules.Core.Structs;

internal readonly struct PlaylistSloganStruct
{
    internal string Slogan { get; }
    internal string ListenerSlogan { get; }
    internal int Listeners { get; }

    internal PlaylistSloganStruct(string slogan, string listenerSlogan, int listeners)
    {
        Slogan = slogan;
        ListenerSlogan = listenerSlogan;
        Listeners = listeners;
    }
}
