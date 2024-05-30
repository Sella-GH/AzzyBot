using System;
using System.Diagnostics.CodeAnalysis;

namespace AzzyBot.Settings;

public sealed record AzzyBotSettingsRecord
{
    public required string BotToken { get; init; }
    public required ulong ServerId { get; init; }
    public required ulong ErrorChannelId { get; init; }
    public required ulong NotificationChannelId { get; init; }
    public DatabaseSettings? Database { get; init; }
    public DiscordStatus? DiscordStatus { get; init; }
    public required CoreUpdater Updater { get; init; }
}

[SuppressMessage("Roslynator", "RCS1181:Convert comment to documentation comment", Justification = "Informational comment")]
public sealed record DatabaseSettings
{
    public required string EncryptionKey { get; init; } // 32 Characters
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

public sealed record CoreUpdater
{
    public required bool DisplayChangelog { get; init; }
    public required bool DisplayInstructions { get; init; }
}
