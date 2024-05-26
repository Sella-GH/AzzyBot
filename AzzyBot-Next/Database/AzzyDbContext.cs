using System;
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
    public DbSet<AzuraCastStationEntity> AzuraCastStations { get; set; }
    public DbSet<AzuraCastChecksEntity> AzuraCastChecks { get; set; }
    public DbSet<AzuraCastMountEntity> AzuraCastMounts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder, nameof(modelBuilder));

        modelBuilder.Entity<GuildsEntity>().Navigation(g => g.AzuraCast).AutoInclude();
        modelBuilder.Entity<AzuraCastEntity>().Navigation(a => a.Stations).AutoInclude();
        modelBuilder.Entity<AzuraCastStationEntity>().Navigation(s => s.Checks).AutoInclude();
        modelBuilder.Entity<AzuraCastStationEntity>().Navigation(s => s.Mounts).AutoInclude();

        base.OnModelCreating(modelBuilder);
    }
}
