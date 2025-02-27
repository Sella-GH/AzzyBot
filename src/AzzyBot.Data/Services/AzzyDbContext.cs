using AzzyBot.Data.Entities;

using Microsoft.EntityFrameworkCore;

namespace AzzyBot.Data.Services;

public sealed class AzzyDbContext : DbContext
{
#pragma warning disable CS8618
    public AzzyDbContext()
    { }

    public AzzyDbContext(DbContextOptions<AzzyDbContext> options) : base(options)
    { }
#pragma warning restore CS8618

    public DbSet<AzzyBotEntity> AzzyBot { get; set; }
    public DbSet<GuildEntity> Guilds { get; set; }
    public DbSet<GuildPreferencesEntity> GuildPreferences { get; set; }
    public DbSet<AzuraCastEntity> AzuraCast { get; set; }
    public DbSet<AzuraCastChecksEntity> AzuraCastChecks { get; set; }
    public DbSet<AzuraCastPreferencesEntity> AzuraCastPreferences { get; set; }
    public DbSet<AzuraCastStationEntity> AzuraCastStations { get; set; }
    public DbSet<AzuraCastStationChecksEntity> AzuraCastStationChecks { get; set; }
    public DbSet<AzuraCastStationPreferencesEntity> AzuraCastStationPreferences { get; set; }
    public DbSet<AzuraCastStationRequestEntity> AzuraCastStationRequests { get; set; }
}
