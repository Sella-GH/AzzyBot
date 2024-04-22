using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Modules;
using AzzyBot.Modules.AzuraCast.Strings;
using AzzyBot.Modules.ClubManagement.Strings;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Strings;
using AzzyBot.Modules.MusicStreaming.Strings;

namespace AzzyBot.Strings;

internal abstract class BaseStringBuilder
{
    [SuppressMessage("Roslynator", "RCS1208:Reduce 'if' nesting", Justification = "Code Style")]
    internal static async Task LoadStringsAsync()
    {
        if (!await CoreStringBuilder.LoadCoreStringsAsync())
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "Core strings can't be loaded", null);
            await AzzyBot.BotShutdownAsync();
        }

        if (ModuleStates.AzuraCast && !await AcStringBuilder.LoadAzuraCastStringsAsync())
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "AzuraCast strings can't be loaded", null);
            await AzzyBot.BotShutdownAsync();
        }

        if (ModuleStates.ClubManagement && !await CmStringBuilder.LoadClubManagementStringsAsync())
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "ClubManagement strings can't be loaded", null);
            await AzzyBot.BotShutdownAsync();
        }

        if (ModuleStates.MusicStreaming && !await MsStringBuilder.LoadMusicStreamingStringsAsync())
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "MusicStreaming strings can't be loaded", null);
            await AzzyBot.BotShutdownAsync();
        }
    }

    protected static bool CheckStrings(Type type)
    {
        // Get all Properties of the class
        PropertyInfo[] properties = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Static);

        List<string> failed = [];

        // Loop through all properties and check if they are null, whitespace or 0
        // If yes add them to the list
        foreach (PropertyInfo property in properties)
        {
            if (property.PropertyType != typeof(string))
                continue;

            string? value = property.GetValue(null) as string;

            if (string.IsNullOrWhiteSpace(value))
                failed.Add(property.Name);
        }

        if (failed.Count == 0)
            return true;

        foreach (string item in failed)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, $"String {item} has to be filled out!", null);
        }

        if (CoreMisc.CheckIfLinuxOs())
            return false;

        LoggerBase.LogInfo(LoggerBase.GetLogger, "Press any key to acknowledge...", null);
        Console.ReadKey();

        return false;
    }

    protected static string BuildString(string template, string parameterName, double parameterValue) => template.Replace(parameterName, parameterValue.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase);
    protected static string BuildString(string template, string parameterName, int parameterValue) => template.Replace(parameterName, parameterValue.ToString(CultureInfo.InvariantCulture), StringComparison.InvariantCultureIgnoreCase);
    protected static string BuildString(string template, string parameterName, string parameterValue) => template.Replace(parameterName, parameterValue, StringComparison.InvariantCultureIgnoreCase);
}
