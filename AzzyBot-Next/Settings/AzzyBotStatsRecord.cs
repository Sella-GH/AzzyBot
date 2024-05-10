using System;

namespace AzzyBot.Settings;

internal record AzzyBotStatsRecord
{
    public required string Commit { get; init; }
    public required DateTime CompilationDate { get; init; }
    public required int LocCs { get; init; }
    public required int LocJson { get; init; }
}
