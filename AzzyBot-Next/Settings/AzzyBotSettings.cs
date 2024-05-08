﻿using System;

namespace AzzyBot.Settings;

public sealed record AzzyBotSettings
{
    public required string BotToken { get; init; }
    public required ulong ServerId { get; init; }
    public required ulong ErrorChannelId { get; init; }
    public DiscordStatus? DiscordStatus { get; init; }
    public required CoreSettings CoreSettings { get; init; }
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
