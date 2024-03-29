using System;
using System.Collections.Generic;
using System.Globalization;

namespace AzzyBot.Settings.Core;

internal sealed class CoreSettings : BaseSettings
{
    internal static bool CoreSettingsLoaded { get; private set; }
    internal static int LogLevel { get; private set; }
    internal static string BotToken { get; private set; } = string.Empty;
    internal static ulong? ServerId { get; private set; }
    internal static ulong OwnerUserId { get; private set; }
    internal static ulong ErrorChannelId { get; private set; }
    internal static ulong AdminRoleId { get; private set; }
    internal static int BotStatus { get; private set; }
    internal static int BotActivity { get; private set; }
    internal static string BotDoing { get; private set; } = string.Empty;
    internal static string? BotStreamUrl { get; private set; } = string.Empty;
    internal static int UpdateCheckInterval { get; private set; }
    internal static string LogoUrl { get; private set; } = string.Empty;

    internal static bool LoadCore()
    {
        ArgumentNullException.ThrowIfNull(Config);

        // Core config
        LogLevel = Convert.ToInt32(Config["Core:LogLevel"], CultureInfo.InvariantCulture);
        BotToken = Config["Core:BotToken"] ?? string.Empty;
        ServerId = Convert.ToUInt64(Config["Core:ServerId"], CultureInfo.InvariantCulture);
        if (ServerId == 0)
            ServerId = null;

        OwnerUserId = Convert.ToUInt64(Config["Core:OwnerUserId"], CultureInfo.InvariantCulture);
        ErrorChannelId = Convert.ToUInt64(Config["Core:ErrorChannelId"], CultureInfo.InvariantCulture);
        AdminRoleId = Convert.ToUInt64(Config["Core:AdminRoleId"], CultureInfo.InvariantCulture);
        BotStatus = Convert.ToInt32(Config["Core:BotStatus"], CultureInfo.InvariantCulture);
        BotActivity = Convert.ToInt32(Config["Core:BotActivity"], CultureInfo.InvariantCulture);
        BotDoing = Config["Core:BotDoing"] ?? string.Empty;
        BotStreamUrl = Config["Core:BotStreamUrl"];
        if (string.IsNullOrWhiteSpace(BotStreamUrl))
            BotStreamUrl = null;

        UpdateCheckInterval = Convert.ToInt32(Config["Core:UpdateCheckInterval"], CultureInfo.InvariantCulture);
        LogoUrl = Config["Core:LogoUrl"] ?? string.Empty;

        List<string> excludedStrings = [nameof(BotStreamUrl), nameof(LogoUrl)];
        List<string> excludedInts = [nameof(ServerId)];
        return CoreSettingsLoaded = CheckSettings(typeof(CoreSettings), excludedStrings, excludedInts);
    }
}
