using System;

namespace AzzyBot.Settings;

public sealed record AzzyBotSettingsRecord
{
    public required string BotToken { get; init; }
    public required ulong ServerId { get; init; }
    public required ulong ErrorChannelId { get; init; }
    public required string EncryptionKey { get; init; }
    public Database? Database { get; init; }
    public DiscordStatus? DiscordStatus { get; init; }
    public required CoreSettings CoreSettings { get; init; }
}

public sealed record Database
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 3306;
    public string User { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = string.Empty;
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
