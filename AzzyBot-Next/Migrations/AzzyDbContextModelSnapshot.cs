﻿// <auto-generated />
using AzzyBot.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace AzzyBot.Migrations
{
    [DbContext(typeof(AzzyDbContext))]
    partial class AzzyDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("AzzyBot.Database.Entities.AzuraCastChecksEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("AzuraCastId")
                        .HasColumnType("int");

                    b.Property<bool>("FileChanges")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("ServerStatus")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("Updates")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("UpdatesShowChangelog")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("Id");

                    b.HasIndex("AzuraCastId")
                        .IsUnique();

                    b.ToTable("AzuraCastChecks");
                });

            modelBuilder.Entity("AzzyBot.Database.Entities.AzuraCastEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("ApiKey")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("ApiUrl")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<int>("GuildId")
                        .HasColumnType("int");

                    b.Property<ulong>("MusicRequestsChannelId")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("OutagesChannelId")
                        .HasColumnType("bigint unsigned");

                    b.Property<bool>("PreferHlsStreaming")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("ShowPlaylistInNowPlaying")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("StationId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("GuildId")
                        .IsUnique();

                    b.ToTable("AzuraCast");
                });

            modelBuilder.Entity("AzzyBot.Database.Entities.AzuraCastMountsEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("AzuraCastId")
                        .HasColumnType("int");

                    b.Property<string>("Mount")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("AzuraCastId");

                    b.ToTable("AzuraCastMounts");
                });

            modelBuilder.Entity("AzzyBot.Database.Entities.GuildsEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("ConfigSet")
                        .HasColumnType("tinyint(1)");

                    b.Property<ulong>("ErrorChannelId")
                        .HasColumnType("bigint unsigned");

                    b.Property<bool>("IsDebugAllowed")
                        .HasColumnType("tinyint(1)");

                    b.Property<ulong>("UniqueId")
                        .HasColumnType("bigint unsigned");

                    b.HasKey("Id");

                    b.ToTable("Guilds");
                });

            modelBuilder.Entity("AzzyBot.Database.Entities.AzuraCastChecksEntity", b =>
                {
                    b.HasOne("AzzyBot.Database.Entities.AzuraCastEntity", "AzuraCast")
                        .WithOne("AutomaticChecks")
                        .HasForeignKey("AzzyBot.Database.Entities.AzuraCastChecksEntity", "AzuraCastId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AzuraCast");
                });

            modelBuilder.Entity("AzzyBot.Database.Entities.AzuraCastEntity", b =>
                {
                    b.HasOne("AzzyBot.Database.Entities.GuildsEntity", "Guild")
                        .WithOne("AzuraCast")
                        .HasForeignKey("AzzyBot.Database.Entities.AzuraCastEntity", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("AzzyBot.Database.Entities.AzuraCastMountsEntity", b =>
                {
                    b.HasOne("AzzyBot.Database.Entities.AzuraCastEntity", "AzuraCast")
                        .WithMany("MountPoints")
                        .HasForeignKey("AzuraCastId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AzuraCast");
                });

            modelBuilder.Entity("AzzyBot.Database.Entities.AzuraCastEntity", b =>
                {
                    b.Navigation("AutomaticChecks");

                    b.Navigation("MountPoints");
                });

            modelBuilder.Entity("AzzyBot.Database.Entities.GuildsEntity", b =>
                {
                    b.Navigation("AzuraCast");
                });
#pragma warning restore 612, 618
        }
    }
}
