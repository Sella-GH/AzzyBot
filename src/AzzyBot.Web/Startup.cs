using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Core.Extensions;
using AzzyBot.Core.Utilities;
using AzzyBot.Web.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace AzzyBot.Web;

public static class Startup
{
    public static async Task Main(string[] args)
    {
        string environment = SoftwareStats.GetAppEnvironment;
        bool isDev = environment == Environments.Development;
        bool isDocker = HardwareStats.CheckIfDocker;
        bool forceDebug = (isDocker) ? (Environment.GetEnvironmentVariable("FORCE_DEBUG") is "true") : (args?.Length > 0 && args.Contains("-forceDebug"));
        bool SkipWaiting = (isDocker) ? (Environment.GetEnvironmentVariable("SKIP_WAITING") is "true") : (args?.Length > 0 && args.Contains("-skipWaiting"));

        if (isDocker && !SkipWaiting)
        {
            // Give the database time to start up
            await Task.Delay(TimeSpan.FromSeconds(30));
        }

        WebApplicationOptions webSettings = new()
        {
            ContentRootPath = Directory.GetCurrentDirectory(),
            EnvironmentName = (isDev) ? Environments.Development : Environments.Production,
            WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
        };

        WebApplicationBuilder webBuilder = WebApplication.CreateEmptyBuilder(webSettings);
        webBuilder.WebHost.AzzyBotWebAppBuilder();

        #region Add logging

        webBuilder.Logging.AzzyBotLogging(isDev, forceDebug);
        webBuilder.Logging.AddAzzyBotWebFilters();

        #endregion Add logging

        #region Add services

        //webBuilder.Services.AzzyBotWebSettings(isDev, isDocker);
        webBuilder.Services.AzzyBotWebServices(isDev);

        #endregion Add services

        WebApplication webHost = webBuilder.Build();
        webHost.AzzyBotWebApp(isDev);

        await webHost.RunAsync();
    }
}
