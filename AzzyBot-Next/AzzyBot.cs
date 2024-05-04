using AzzyBot.Utilities;
using Microsoft.Extensions.Hosting;

namespace AzzyBot;

internal sealed class AzzyBot
{
    private static void Main()
    {
        HostApplicationBuilderSettings builderSettings = new()
        {
            EnvironmentName = Misc.GetAppEnvironment()
        };

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(builderSettings);
    }
}
