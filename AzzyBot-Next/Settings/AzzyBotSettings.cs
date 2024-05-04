using System;

namespace AzzyBot.Settings;

public sealed record AzzyBotSettings
{
    public required string BotToken { get; init; }
    public required DiscordStatus DiscordStatus { get; init; }
}

public sealed record DiscordStatus
{
    public required int Activity { get; init; }
    public required string Doing { get; init; }
    public required int Status { get; init; }
    public Uri? StreamUrl { get; init; }
}
