﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Settings;

public sealed record AzzyBotSettingsRecord
{
    public required string BotToken { get; init; }
    public required ulong ServerId { get; init; }
    public required ulong ErrorChannelId { get; init; }
    public required ulong NotificationChannelId { get; init; }
    public required int LogRetentionDays { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DatabaseSettings? Database { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DiscordStatus? DiscordStatus { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MusicStreamingSettings? MusicStreaming { get; init; }
    public required CoreUpdater Updater { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? SettingsFile { get; set; }
}

[SuppressMessage("Roslynator", "RCS1181:Convert comment to documentation comment", Justification = "Informational comment")]
public sealed record DatabaseSettings
{
    public required string EncryptionKey { get; set; } // 32 Characters
    public string? NewEncryptionKey { get; set; } // 32 Characters

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Host { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Port { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? User { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Password { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DatabaseName { get; init; }
}

public sealed record DiscordStatus
{
    public int Activity { get; init; }
    public string? Doing { get; init; }
    public int Status { get; init; }
    public Uri? StreamUrl { get; init; }
}

public sealed record MusicStreamingSettings
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LavalinkHost { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int LavalinkPort { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LavalinkPassword { get; init; }
}

public sealed record CoreUpdater
{
    public required bool DisplayChangelog { get; init; }
    public required bool DisplayInstructions { get; init; }
}