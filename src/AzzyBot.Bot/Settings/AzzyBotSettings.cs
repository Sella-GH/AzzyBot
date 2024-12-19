using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Settings;

public sealed record AzzyBotSettings
{
    [Required]
    public required string BotToken { get; set; }

    [Required, Range(ulong.MinValue, ulong.MaxValue)]
    public required ulong ServerId { get; set; }

    [Required, Range(ulong.MinValue, ulong.MaxValue)]
    public required ulong ErrorChannelId { get; set; }

    [Required, Range(ulong.MinValue, ulong.MaxValue)]
    public required ulong NotificationChannelId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? SettingsFile { get; set; }
}

public sealed record DiscordStatusSettings
{
    [Range(0, 5), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Activity { get; set; } = 2;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Doing { get; set; } = "Music";

    [Range(0, 5), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Status { get; set; } = 1;

    [Url, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Uri? StreamUrl { get; set; }
}

public sealed record MusicStreamingSettings
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LavalinkHost { get; set; } = "AzzyBot-Ms";

    [Range(0, ushort.MaxValue), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int LavalinkPort { get; set; } = 2333;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LavalinkPassword { get; set; } = "AzzyB0TMus1cStr3am!ng";
}

public sealed record CoreUpdaterSettings
{
    [Required]
    public required bool DisplayChangelog { get; set; }

    [Required]
    public required bool DisplayInstructions { get; set; }
}
