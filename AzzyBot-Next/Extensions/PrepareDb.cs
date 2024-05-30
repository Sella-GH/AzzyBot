using System;
using System.Linq;
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
        IDbContextFactory<AzzyDbContext> factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AzzyDbContext>>();
        using AzzyDbContext db = factory.CreateDbContext();
        if (db.Database.GetPendingMigrations().Any())
            db.Database.Migrate();
    }
}
