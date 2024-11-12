using AzzyBot.Web.Components;
using Microsoft.AspNetCore.Builder;

namespace AzzyBot.Web.Extensions;

public static class IWebApplicationExtensions
{
    public static void AzzyBotWebApp(this WebApplication webApp, bool isDev)
    {
        webApp.MapRazorComponents<App>().AddInteractiveServerRenderMode();

        if (isDev)
        {
            webApp.UseDeveloperExceptionPage();
        }
        else
        {
            webApp.UseExceptionHandler("/Error");
            webApp.UseHsts();
        }

        webApp.UseHttpsRedirection();
        webApp.UseStaticFiles();

        // TODO Add Cookie Stuff
        //webApp.UseCookiePolicy();

        webApp.UseRouting();

        webApp.UseAntiforgery();
    }
}
