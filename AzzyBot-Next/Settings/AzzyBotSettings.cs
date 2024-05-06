using System;

namespace AzzyBot.Settings;

public sealed record AzzyBotSettings
{
    public required string BotToken { get; init; }
    public DiscordStatus? DiscordStatus { get; init; }
}

public sealed record DiscordStatus
{
    public int Activity { get; init; }
    public string? Doing { get; init; }
    public int Status { get; init; }
    public Uri? StreamUrl { get; init; }
}
