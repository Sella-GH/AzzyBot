using AzzyBot.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AzzyBot.Data;

public sealed class AzzyDbContext : DbContext
{
#pragma warning disable CS8618
    public AzzyDbContext()
    { }

    public AzzyDbContext(DbContextOptions<AzzyDbContext> options) : base(options)
    { }
#pragma warning restore CS8618

    public DbSet<GuildEntity> Guilds { get; set; }
    public DbSet<AzuraCastEntity> AzuraCast { get; set; }
    public DbSet<AzuraCastChecksEntity> AzuraCastChecks { get; set; }
    public DbSet<AzuraCastStationEntity> AzuraCastStations { get; set; }
    public DbSet<AzuraCastStationChecksEntity> AzuraCastStationChecks { get; set; }
    public DbSet<AzuraCastStationMountEntity> AzuraCastStationMounts { get; set; }
}
