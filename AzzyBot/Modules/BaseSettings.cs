using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Modules.AzuraCast.Settings;
using AzzyBot.Modules.ClubManagement.Settings;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Settings;
using AzzyBot.Modules.MusicStreaming.Settings;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;

namespace AzzyBot.Modules;

internal abstract class BaseSettings
{
    internal static bool ActivateAzuraCast { get; private set; }
    internal static bool ActivateClubManagement { get; private set; }
    internal static bool ActivateMusicStreaming { get; private set; }
    protected static readonly IConfigurationBuilder builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings", "appsettings.json"), true, false);
    protected static IConfiguration? Config { get; private set; }

    private static void SetDevConfig()
    {
        if (CoreAzzyStatsGeneral.GetBotName != "AzzyBot-Dev")
            return;

        builder.Sources.Clear();
        builder.AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings", "appsettings.development.json"), true, false);
    }

    [SuppressMessage("Roslynator", "RCS1208:Reduce 'if' nesting", Justification = "Code Style")]
    internal static async Task LoadSettingsAsync()
    {
        SetDevConfig();

        Config = builder.Build();

        ActivateAzuraCast = Convert.ToBoolean(Config["AzuraCast:ActivateAzuraCast"], CultureInfo.InvariantCulture);
        ActivateClubManagement = Convert.ToBoolean(Config["ClubManagement:ActivateClubManagement"], CultureInfo.InvariantCulture);
        ActivateMusicStreaming = Convert.ToBoolean(Config["MusicStreaming:ActivateMusicStreaming"], CultureInfo.InvariantCulture);

        // Ensure core is activated first
        if (!CoreSettings.LoadCore() || !CoreSettings.CoreSettingsLoaded)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "Core settings aren't loaded", null);
            await AzzyBot.BotShutdownAsync();
        }

        if (ActivateAzuraCast && !await AcSettings.LoadAzuraCastAsync())
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "AzuraCast settings aren't loaded", null);
            await AzzyBot.BotShutdownAsync();
        }

        if (ActivateClubManagement && ActivateAzuraCast && !CmSettings.LoadClubManagement())
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "ClubManagement settings aren't loaded", null);
            await AzzyBot.BotShutdownAsync();
        }

        if (ActivateMusicStreaming && ActivateAzuraCast && !await MsSettings.LoadMusicStreamingAsync())
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "MusicStreaming settings aren't loaded", null);
            await AzzyBot.BotShutdownAsync();
        }
    }

    internal static bool CheckIfChannelsExist(DiscordGuild guild)
    {
        bool core = CoreDiscordCommands.CheckIfChannelExists(guild, CoreSettings.ErrorChannelId);
        bool azuraCast = true;
        bool clubManagement = true;

        if (ActivateAzuraCast)
        {
            List<ulong> azuraCastChannels = CoreDiscordCommands.CheckIfChannelsExist(guild, [AcSettings.MusicRequestsChannelId, AcSettings.OutagesChannelId]);
            if (azuraCastChannels.Count > 0)
            {
                azuraCast = false;

                foreach (ulong channel in azuraCastChannels)
                {
                    LoggerBase.LogError(LoggerBase.GetLogger, $"Channel with ID **{channel}** does not exist in guild with ID **{guild.Id}** and name **{guild.Name}**!", null);
                }
            }
        }

        if (ActivateClubManagement)
            clubManagement = CoreDiscordCommands.CheckIfChannelExists(guild, CmSettings.ClubNotifyChannelId);

        return core && azuraCast && clubManagement;
    }

    protected static bool CheckSettings(Type type, List<string>? excludedStrings = null, List<string>? excludedInts = null)
    {
        // Get all Properties of this class
        PropertyInfo[] properties = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Static);

        List<string> failed = [];

        // Loop through all properties and check if they are null, whitespace or 0
        // If yes add them to the list
        foreach (PropertyInfo property in properties)
        {
            object? value = property.GetValue(null);

            switch (property.PropertyType)
            {
                case Type t when t == typeof(string):
                    if (excludedStrings?.Contains(property.Name) == true)
                        continue;

                    if (string.IsNullOrWhiteSpace(value as string))
                        failed.Add(property.Name);

                    break;

                case Type t when t == typeof(ulong) || t == typeof(int):
                    if (excludedInts?.Contains(property.Name) == true)
                        continue;

                    if (Convert.ToInt64(value, CultureInfo.InvariantCulture) == 0)
                        failed.Add(property.Name);

                    break;

                case Type t when t == typeof(TimeSpan):
                    if (value is TimeSpan timeSpan && timeSpan == TimeSpan.Zero)
                        failed.Add(property.Name);

                    break;
            }
        }

        if (failed.Count == 0)
            return true;

        foreach (string item in failed)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, $"{item} has to be filled out!", null);
        }

        if (CoreMisc.CheckIfLinuxOs())
            return false;

        LoggerBase.LogInfo(LoggerBase.GetLogger, "Press any key to acknowledge...", null);
        Console.ReadKey();

        return false;
    }
}
