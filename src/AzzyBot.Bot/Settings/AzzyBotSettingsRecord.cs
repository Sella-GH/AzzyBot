using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using AzzyBot.Core.Settings;
using AzzyBot.Data.Settings;

namespace AzzyBot.Bot.Settings;

public sealed record AzzyBotSettingsRecord : ISettings
{
    public required string BotToken { get; init; }
    public required ulong ServerId { get; init; }
    public required ulong ErrorChannelId { get; init; }
    public required ulong NotificationChannelId { get; init; }

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

    public Dictionary<string, object?> GetProperties()
    {
        return new()
        {
            { nameof(BotToken), BotToken },
            { nameof(ServerId), ServerId },
            { nameof(ErrorChannelId), ErrorChannelId },
            { nameof(NotificationChannelId), NotificationChannelId },
            { nameof(Database), Database },
            { nameof(DiscordStatus), DiscordStatus },
            { nameof(MusicStreaming), MusicStreaming },
            { nameof(Updater), Updater }
        };
    }
}

public sealed record DiscordStatus : ISettings
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Activity { get; init; } = 2;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Doing { get; init; } = "Music";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Status { get; init; } = 1;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Uri? StreamUrl { get; init; }

    public Dictionary<string, object?> GetProperties()
    {
        return new()
        {
            { nameof(Activity), Activity },
            { nameof(Doing), Doing },
            { nameof(Status), Status },
            { nameof(StreamUrl), StreamUrl }
        };
    }
}

public sealed record MusicStreamingSettings : ISettings
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LavalinkHost { get; init; } = "AzzyBot-Ms";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int LavalinkPort { get; init; } = 2333;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LavalinkPassword { get; init; } = "AzzyB0TMus1cStr3am!ng";

    public Dictionary<string, object?> GetProperties()
    {
        return new()
        {
            { nameof(LavalinkHost), LavalinkHost },
            { nameof(LavalinkPort), LavalinkPort },
            { nameof(LavalinkPassword), LavalinkPassword }
        };
    }
}

public sealed record CoreUpdater : ISettings
{
    public required bool DisplayChangelog { get; init; }
    public required bool DisplayInstructions { get; init; }

    public Dictionary<string, object?> GetProperties()
    {
        return new()
        {
            { nameof(DisplayChangelog), DisplayChangelog },
            { nameof(DisplayInstructions), DisplayInstructions }
        };
    }
}
