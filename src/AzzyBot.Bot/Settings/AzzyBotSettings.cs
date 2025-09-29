using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzzyBot.Bot.Settings;

public sealed record AzzyBotSettings
{
    /// <summary>
    /// The discord bot token.
    /// </summary>
    [Required(ErrorMessage = "The bot token is the important part of running the bot and cannot be missing.")]
    public required string BotToken { get; set; }

    /// <summary>
    /// The main server id where the bot is running.
    /// </summary>
    [Required, Range(ulong.MinValue, ulong.MaxValue, ErrorMessage = "A server id can only contain numbers!")]
    public required ulong ServerId { get; set; }

    /// <summary>
    /// The error channel id where the bot will send error messages.
    /// </summary>
    [Required, Range(ulong.MinValue, ulong.MaxValue, ErrorMessage = "A channel id can only contain numbers!")]
    public required ulong ErrorChannelId { get; set; }

    /// <summary>
    /// The notification channel id where the bot will send notifications.
    /// </summary>
    [Required, Range(ulong.MinValue, ulong.MaxValue, ErrorMessage = "A channel id can only contain numbers!")]
    public required ulong NotificationChannelId { get; set; }

    /// <summary>
    /// The announcement channel id where the bot will send its announcements.
    /// </summary>
    [Range(ulong.MinValue, ulong.MaxValue, ErrorMessage = "A channel id can only contain numbers!")]
    public ulong BotAnnouncementChannelId { get; set; }

    /// <summary>
    /// The settings file when it gets updated.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? SettingsFile { get; set; }
}

public sealed record DiscordStatusSettings
{
    /// <summary>
    /// The activity type the bot is showing.
    /// </summary>
    [Range(0, 5, ErrorMessage = "The Activity number is out of range. Please choose one between 0 and 5."), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Activity { get; set; } = 2;

    /// <summary>
    /// What the bot is doing.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Doing { get; set; } = "Music";

    /// <summary>
    /// The status the bot is showing.
    /// </summary>
    [Range(0, 5, ErrorMessage = "The Status number is out of range. Please choose one between 0 and 5."), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Status { get; set; } = 1;

    /// <summary>
    /// The stream url if the activity is set to streaming.
    /// </summary>
    [Url(ErrorMessage = "Your SteamUrl is no real Url!"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Uri? StreamUrl { get; set; }
}

public sealed record MusicStreamingSettings
{
    /// <summary>
    /// The hostname of the lavalink server.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LavalinkHost { get; set; } = "AzzyBot-Ms";

    /// <summary>
    /// The port of the lavalink server.
    /// </summary>
    [Range(0, ushort.MaxValue, ErrorMessage = "The LavalinkPort number is out of range. Please choose one between 0 and 65535."), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int LavalinkPort { get; set; } = 2333;

    /// <summary>
    /// The password of the lavalink server.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LavalinkPassword { get; set; } = "AzzyB0TMus1cStr3am!ng";
}

public sealed record CoreUpdaterSettings
{
    /// <summary>
    /// Whether to display the changelog when the bot finds an update.
    /// </summary>
    [Required]
    public required bool DisplayChangelog { get; set; }

    /// <summary>
    /// Whether to display instructions on how to update when the bot finds an update.
    /// </summary>
    [Required]
    public required bool DisplayInstructions { get; set; }
}
