using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Settings;

public sealed record AzzyBotSettings
{
    [Required(ErrorMessage = "The bot token is the important part of running the bot and cannot be missing.")]
    public required string BotToken { get; set; }

    [Required, Range(ulong.MinValue, ulong.MaxValue, ErrorMessage = "A server id can only contain numbers!")]
    public required ulong ServerId { get; set; }

    [Required, Range(ulong.MinValue, ulong.MaxValue, ErrorMessage = "A channel id can only contain numbers!")]
    public required ulong ErrorChannelId { get; set; }

    [Required, Range(ulong.MinValue, ulong.MaxValue, ErrorMessage = "A channel id can only contain numbers!")]
    public required ulong NotificationChannelId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? SettingsFile { get; set; }
}

public sealed record DiscordStatusSettings
{
    [Range(0, 5, ErrorMessage = "The Activity number is out of range. Please choose one between 0 and 5."), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Activity { get; set; } = 2;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Doing { get; set; } = "Music";

    [Range(0, 5, ErrorMessage = "The Status number is out of range. Please choose one between 0 and 5."), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Status { get; set; } = 1;

    [Url(ErrorMessage = "Your SteamUrl is no real Url!"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Uri? StreamUrl { get; set; }
}

public sealed record MusicStreamingSettings
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LavalinkHost { get; set; } = "AzzyBot-Ms";

    [Range(0, ushort.MaxValue, ErrorMessage = "The LavalinkPort number is out of range. Please choose one between 0 and 65535."), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
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
