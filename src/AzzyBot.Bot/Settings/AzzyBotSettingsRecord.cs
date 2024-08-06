using System;
using System.Text.Json.Serialization;
using AzzyBot.Data.Settings;

namespace AzzyBot.Bot.Settings;

public sealed record AzzyBotSettingsRecord
{
    public required string BotToken { get; init; }
    public required ulong ServerId { get; init; }
    public required ulong ErrorChannelId { get; init; }
    public required ulong NotificationChannelId { get; init; }
    public required int LogRetentionDays { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AppDatabaseSettings? Database { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DiscordStatus? DiscordStatus { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MusicStreamingSettings? MusicStreaming { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public required CoreUpdater Updater { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? SettingsFile { get; set; }
}

public sealed record DiscordStatus
{
    public int Activity { get; init; } = 2;
    public string? Doing { get; init; } = "Music";
    public int Status { get; init; } = 1;
    public Uri? StreamUrl { get; init; } = string.Empty;
}

public sealed record MusicStreamingSettings
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LavalinkHost { get; init; } = "lavalink";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int LavalinkPort { get; init; } = 2333;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LavalinkPassword { get; init; } = "AzzyB0TMus1cStr3am!ng";
}

public sealed record CoreUpdater
{
    public required bool DisplayChangelog { get; init; }
    public required bool DisplayInstructions { get; init; }
}
