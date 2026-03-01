using System.Diagnostics.CodeAnalysis;
#if DOCKER_DEBUG || DOCKER || RELEASE
using System.IO;
#endif

using Microsoft.Extensions.Configuration;

namespace AzzyBot.Bot.Extensions;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "https://github.com/dotnet/sdk/issues/51681")]
public static class IConfigurationManagerExtensions
{
    extension(IConfigurationManager configurationManager)
    {
        public void AddAppConfiguration(string settingsFile)
        {
            configurationManager.AddJsonFile(settingsFile);
#if DOCKER_DEBUG || DOCKER || RELEASE
            settingsFile = Path.Combine("Modules", "Core", "Files", "AppStats.json");

            configurationManager.AddJsonFile(settingsFile);
#endif
        }
    }
}
