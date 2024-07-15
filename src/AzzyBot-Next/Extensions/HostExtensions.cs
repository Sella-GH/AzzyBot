using System;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace AzzyBot.Extensions;

public static class HostExtensions
{
    public static void ApplyDbMigrations(this IHost app)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));

        using IServiceScope scope = app.Services.CreateScope();
        IDbContextFactory<AzzyDbContext> factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AzzyDbContext>>();
        using AzzyDbContext db = factory.CreateDbContext();

        bool isOnline = false;
        while (!isOnline)
        {
            try
            {
                if (db.Database.GetPendingMigrations().Any())
                    db.Database.Migrate();

                isOnline = true;
            }
            catch (NpgsqlException)
            {
                Console.Out.WriteLine("Database is not online yet. Retrying in 5 seconds...");
                Task.Delay(TimeSpan.FromSeconds(5)).Wait();
            }
        }
    }
}
