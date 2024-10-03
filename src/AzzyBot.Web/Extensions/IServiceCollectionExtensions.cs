using System;
//using System.IO;
//using AzzyBot.Core.Settings;
//using AzzyBot.Core.Utilities;
//using AzzyBot.Web.Utilities.Records;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AzzyBot.Web.Extensions;

public static class IServiceCollectionExtensions
{
    public static void AzzyBotWebServices(this IServiceCollection services, bool isDev)
    {
        if (!isDev)
        {
            services.AddHsts(s =>
            {
                s.IncludeSubDomains = true;
                s.MaxAge = TimeSpan.FromDays(360);
                s.Preload = true;
            });

            services.AddHttpsRedirection(s =>
            {
                s.HttpsPort = 443;
                s.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
            });
        }

        services.AddRouting(s =>
        {
            s.AppendTrailingSlash = true;
            s.LowercaseQueryStrings = true;
            s.LowercaseUrls = true;
            s.SuppressCheckForUnhandledSecurityMetadata = false;
        });

        services.AddAntiforgery(s => s.SuppressXFrameOptionsHeader = true);
        services.AddRazorComponents(r => r.DetailedErrors = isDev).AddInteractiveServerComponents(c => c.DetailedErrors = isDev);

        // TODO Add Cookie Stuff
        //services.Configure<CookiePolicyOptions>(c =>
        //{
        //    c.CheckConsentNeeded = context => true;
        //    c.ConsentCookieValue = "true";
        //    c.MinimumSameSitePolicy = SameSiteMode.Strict;
        //});
    }

    //public static void AzzyBotWebSettings(this IServiceCollection services, bool isDev = false, bool isDocker = false)
    //{
        //string settingsFile = "AzzyBotWebSettings.json";
        //if (isDev)
        //{
        //    settingsFile = "AzzyBotWebSettings-Dev.json";
        //}
        //else if (isDocker)
        //{
        //    settingsFile = "AzzyBotWebSettings-Docker.json";
        //}

        //string path = Path.Combine("Settings", settingsFile);

        //AzzyBotWebSettingsRecord? settings = GetConfiguration(path).Get<AzzyBotWebSettingsRecord>();
        //if (settings is null)
        //{
        //    Console.Error.WriteLine("No web configuration found! Please set your settings.");
        //    if (!HardwareStats.CheckIfLinuxOs)
        //        Console.ReadKey();

        //    Environment.Exit(1);
        //}

        //SettingsCheck.CheckSettings(settings);

        //services.AddSingleton(settings);
    //}

    //private static IConfiguration GetConfiguration(string path)
    //{
    //    ConfigurationBuilder configBuilder = new();
    //    configBuilder.Sources.Clear();
    //    configBuilder.AddJsonFile(path, false, false);

    //    return configBuilder.Build();
    //}
}
