using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace AzzyBot.Web.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static void AzzyBotWebAppBuilder(this ConfigureWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        builder.UseKestrel(o =>
        {
            o.AddServerHeader = false;
            o.AllowResponseHeaderCompression = true;
        });

        builder.UseUrls("https://localhost:5001", "http://localhost:5000");
    }
}
