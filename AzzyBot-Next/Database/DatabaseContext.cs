using AzzyBot.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace AzzyBot.Database;

internal sealed class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public DbSet<GuildsEntity> Guilds { get; set; }
    public DbSet<AzuraCastEntity> AzuraCast { get; set; }
    public DbSet<AzuraCastChecksEntity> AzuraCastChecks { get; set; }
}
