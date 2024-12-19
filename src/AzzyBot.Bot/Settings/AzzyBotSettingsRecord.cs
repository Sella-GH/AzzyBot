using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Settings;

public sealed record AzzyBotSettingsRecord
{
    [Required]
    public required string BotToken { get; init; }

    [Required, Range(ulong.MinValue, ulong.MaxValue)]
    public required ulong ServerId { get; init; }

    [Required, Range(ulong.MinValue, ulong.MaxValue)]
    public required ulong ErrorChannelId { get; init; }

    [Required, Range(ulong.MinValue, ulong.MaxValue)]
    public required ulong NotificationChannelId { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? SettingsFile { get; set; }
}

public sealed record DiscordStatus
{
    [Range(0, 5), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Activity { get; init; } = 2;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Doing { get; init; } = "Music";

    [Range(0, 5), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Status { get; init; } = 1;

    [Url, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Uri? StreamUrl { get; init; }
}

public sealed record MusicStreamingSettings
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LavalinkHost { get; init; } = "AzzyBot-Ms";

    [Range(0, ushort.MaxValue), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int LavalinkPort { get; init; } = 2333;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LavalinkPassword { get; init; } = "AzzyB0TMus1cStr3am!ng";
}

public sealed record CoreUpdater
{
    [Required]
    public required bool DisplayChangelog { get; init; }

    [Required]
    public required bool DisplayInstructions { get; init; }
}
