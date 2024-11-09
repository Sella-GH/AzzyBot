using System;
using System.IO;
using AzzyBot.Core.Utilities;
using AzzyBot.Core.Utilities.Records;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzzyBot.Core.Extensions;

public static class IServiceCollectionExtensions
{
    public static void AzzyBotStats(this IServiceCollection services, bool isDev)
    {
        if (isDev)
        {
            services.AddSingleton(new AppStatsRecord("Unknown", DateTimeOffset.Now, 0));
            return;
        }

        string path = Path.Combine("Modules", "Core", "Files", "AppStats.json");

        AppStatsRecord? stats = Misc.GetConfiguration(path).Get<AppStatsRecord>();
        if (stats is null)
        {
            Console.Error.Write("There is something wrong with your configuration. Did you followed the installation instructions?");
            if (!HardwareStats.CheckIfLinuxOs)
                Console.ReadKey();

            Environment.Exit(1);
        }

        services.AddSingleton(stats);
    }
}
