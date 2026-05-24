using AzzyBot.Data.Settings;

namespace AzzyBot.Bot.Settings;

public sealed record class AppSettingsModel
{
    public required AzzyBotSettings AzzyBotSettings { get; init; }
    public required DatabaseSettings DatabaseSettings { get; init; }
    public required DiscordStatusSettings DiscordStatusSettings { get; init; }
    public required MusicStreamingSettings MusicStreamingSettings { get; init; }
    public required CoreUpdaterSettings CoreUpdaterSettings { get; init; }
}
