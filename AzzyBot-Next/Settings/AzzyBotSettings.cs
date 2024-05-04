namespace AzzyBot.Settings;

public sealed record AzzyBotSettings
{
    public required string BotToken { get; init; }
}
