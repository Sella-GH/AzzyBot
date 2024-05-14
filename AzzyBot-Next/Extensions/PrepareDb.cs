using AzzyBot.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AzzyBot.Extensions;

internal static class PrepareDb
{
    internal static void ApplyDbMigrations(this IHost app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        using AzzyDbContext db = scope.ServiceProvider.GetRequiredService<Database.AzzyDbContext>();
        db.Database.Migrate();
    }
}
