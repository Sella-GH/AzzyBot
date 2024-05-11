using System;

namespace AzzyBot.Settings;

public sealed record AzzyBotSettingsRecord
{
    public required string BotToken { get; init; }
    public required ulong ServerId { get; init; }
    public required ulong ErrorChannelId { get; init; }
    public Database? Database { get; init; }
    public DiscordStatus? DiscordStatus { get; init; }
    public required CoreSettings CoreSettings { get; init; }
}

public sealed record Database
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string User { get; init; }
    public string Password { get; init; } = string.Empty;
    public required string DatabaseName { get; init; }
}

public sealed record DiscordStatus
{
    public int Activity { get; init; }
    public string? Doing { get; init; }
    public int Status { get; init; }
    public Uri? StreamUrl { get; init; }
}

public sealed record CoreSettings
{
    public required CoreUpdater CoreUpdater { get; init; }
}

public sealed record CoreUpdater
{
    public required int CheckInterval { get; init; }
    public required bool DisplayChangelog { get; init; }
    public required bool DisplayInstructions { get; init; }
    public required ulong MessageChannelId { get; init; }
}
