using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AzzyBot.Database;

internal static class PrepareDb
{
    internal static void ApplyDbMigrations(this IHost app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        using DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        db.Database.Migrate();
    }
}
