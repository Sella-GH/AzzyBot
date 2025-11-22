using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

using AzzyBot.Data.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Npgsql;

namespace AzzyBot.Bot.Extensions;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Known Issue.")]
[SuppressMessage("Minor Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "Known Issue.")]
public static class IHostExtensions
{
    extension(IHost app)
    {
        public void ApplyDbMigrations()
        {
            ArgumentNullException.ThrowIfNull(app);

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
}
