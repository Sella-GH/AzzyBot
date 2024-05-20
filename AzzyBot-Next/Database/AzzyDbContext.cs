using AzzyBot.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace AzzyBot.Database;

public sealed class AzzyDbContext : DbContext
{
#pragma warning disable CS8618
    public AzzyDbContext()
    { }

    public AzzyDbContext(DbContextOptions<AzzyDbContext> options) : base(options)
    { }
#pragma warning restore CS8618

    public DbSet<GuildsEntity> Guilds { get; set; }
    public DbSet<AzuraCastEntity> AzuraCast { get; set; }
    public DbSet<AzuraCastChecksEntity> AzuraCastChecks { get; set; }
    public DbSet<AzuraCastMountsEntity> AzuraCastMounts { get; set; }
}
