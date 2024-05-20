using System;
using AzzyBot.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AzzyBot.Extensions;

public static class PrepareDb
{
    public static void ApplyDbMigrations(this IHost app)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));

        using IServiceScope scope = app.Services.CreateScope();
        using AzzyDbContext db = scope.ServiceProvider.GetRequiredService<Database.AzzyDbContext>();
        db.Database.Migrate();
    }
}
