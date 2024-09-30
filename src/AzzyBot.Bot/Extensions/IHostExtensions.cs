using System;
using System.Linq;
using System.Threading.Tasks;
using AzzyBot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace AzzyBot.Bot.Extensions;

public static class IHostExtensions
{
    public static void ApplyDbMigrations(this IHost app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using IServiceScope scope = app.Services.CreateScope();
        AzzyDbContext db = scope.ServiceProvider.GetRequiredService<AzzyDbContext>();

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
