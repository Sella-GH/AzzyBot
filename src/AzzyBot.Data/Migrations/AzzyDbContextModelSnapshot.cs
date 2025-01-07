﻿// <auto-generated />
using System;
using AzzyBot.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AzzyBot.Data.Migrations
{
    [DbContext(typeof(AzzyDbContext))]
    partial class AzzyDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("AzzyBot.Data.Entities.AzuraCastChecksEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("AzuraCastId")
                        .HasColumnType("integer");

                    b.Property<DateTimeOffset>("LastServerStatusCheck")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset>("LastUpdateCheck")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("ServerStatus")
                        .HasColumnType("boolean");

                    b.Property<int>("UpdateNotificationCounter")
                        .HasColumnType("integer");

                    b.Property<bool>("Updates")
                        .HasColumnType("boolean");

                    b.Property<bool>("UpdatesShowChangelog")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.HasIndex("AzuraCastId")
                        .IsUnique();

                    b.ToTable("AzuraCastChecks");
                });

            modelBuilder.Entity("AzzyBot.Data.Entities.AzuraCastEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("AdminApiKey")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("BaseUrl")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("GuildId")
                        .HasColumnType("integer");

                    b.Property<bool>("IsOnline")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.HasIndex("GuildId")
                        .IsUnique();

                    b.ToTable("AzuraCast");
                });

            modelBuilder.Entity("AzzyBot.Data.Entities.AzuraCastPreferencesEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("AzuraCastId")
                        .HasColumnType("integer");

                    b.Property<decimal>("InstanceAdminRoleId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("NotificationChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("OutagesChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("AzuraCastId")
                        .IsUnique();

                    b.ToTable("AzuraCastPreferences");
                });

            modelBuilder.Entity("AzzyBot.Data.Entities.AzuraCastStationChecksEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<bool>("FileChanges")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset>("LastFileChangesCheck")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("StationId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("StationId")
                        .IsUnique();

                    b.ToTable("AzuraCastStationChecks");
                });

            modelBuilder.Entity("AzzyBot.Data.Entities.AzuraCastStationEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ApiKey")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("AzuraCastId")
                        .HasColumnType("integer");

                    b.Property<DateTimeOffset>("LastRequestTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset>("LastSkipTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("StationId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("AzuraCastId");

                    b.ToTable("AzuraCastStations");
                });

            modelBuilder.Entity("AzzyBot.Data.Entities.AzuraCastStationPreferencesEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<decimal>("FileUploadChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("FileUploadPath")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("RequestsChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<bool>("ShowPlaylistInNowPlaying")
                        .HasColumnType("boolean");

                    b.Property<decimal>("StationAdminRoleId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("StationDjRoleId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("StationId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("StationId")
                        .IsUnique();

                    b.ToTable("AzuraCastStationPreferences");
                });

            modelBuilder.Entity("AzzyBot.Data.Entities.AzuraCastStationRequestEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<bool>("IsInternal")
                        .HasColumnType("boolean");

                    b.Property<string>("SongId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("StationId")
                        .HasColumnType("integer");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("StationId");

                    b.ToTable("AzuraCastStationRequests");
                });

            modelBuilder.Entity("AzzyBot.Data.Entities.AzzyBotEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTimeOffset>("LastDatabaseCleanup")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset>("LastUpdateCheck")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("AzzyBot");
                });

            modelBuilder.Entity("AzzyBot.Data.Entities.GuildEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<bool>("ConfigSet")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset>("LastPermissionCheck")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("LegalsAccepted")
                        .HasColumnType("boolean");

                    b.Property<decimal>("UniqueId")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id");

                    b.ToTable("Guilds");
                });

            modelBuilder.Entity("AzzyBot.Data.Entities.GuildPreferencesEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<decimal>("AdminNotifyChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("AdminRoleId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("ErrorChannelId")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("GuildId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("GuildId")
                        .IsUnique();

                    b.ToTable("GuildPreferences");
                });

            modelBuilder.Entity("AzzyBot.Data.Entities.AzuraCastChecksEntity", b =>
                {
                    b.HasOne("AzzyBot.Data.Entities.AzuraCastEntity", "AzuraCast")
                        .WithOne("Checks")
                        .HasForeignKey("AzzyBot.Data.Entities.AzuraCastChecksEntity", "AzuraCastId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AzuraCast");
                });

            modelBuilder.Entity("AzzyBot.Data.Entities.AzuraCastEntity", b =>
                {
                    b.HasOne("AzzyBot.Data.Entities.GuildEntity", "Guild")
                        .WithOne("AzuraCast")
                        .HasForeignKey("AzzyBot.Data.Entities.AzuraCastEntity", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("AzzyBot.Data.Entities.AzuraCastPreferencesEntity", b =>
                {
                    b.HasOne("AzzyBot.Data.Entities.AzuraCastEntity", "AzuraCast")
                        .WithOne("Preferences")
                        .HasForeignKey("AzzyBot.Data.Entities.AzuraCastPreferencesEntity", "AzuraCastId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AzuraCast");
                });

            modelBuilder.Entity("AzzyBot.Data.Entities.AzuraCastStationChecksEntity", b =>
                {
                    b.HasOne("AzzyBot.Data.Entities.AzuraCastStationEntity", "Station")
                        .WithOne("Checks")
                        .HasForeignKey("AzzyBot.Data.Entities.AzuraCastStationChecksEntity", "StationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Station");
                });

            modelBuilder.Entity("AzzyBot.Data.Entities.AzuraCastStationEntity", b =>
                {
                    b.HasOne("AzzyBot.Data.Entities.AzuraCastEntity", "AzuraCast")
                        .WithMany("Stations")
                        .HasForeignKey("AzuraCastId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AzuraCast");
                });

            modelBuilder.Entity("AzzyBot.Data.Entities.AzuraCastStationPreferencesEntity", b =>
                {
                    b.HasOne("AzzyBot.Data.Entities.AzuraCastStationEntity", "Station")
                        .WithOne("Preferences")
                        .HasForeignKey("AzzyBot.Data.Entities.AzuraCastStationPreferencesEntity", "StationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Station");
                });

            modelBuilder.Entity("AzzyBot.Data.Entities.AzuraCastStationRequestEntity", b =>
                {
                    b.HasOne("AzzyBot.Data.Entities.AzuraCastStationEntity", "Station")
                        .WithMany("Requests")
                        .HasForeignKey("StationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Station");
                });

            modelBuilder.Entity("AzzyBot.Data.Entities.GuildPreferencesEntity", b =>
                {
                    b.HasOne("AzzyBot.Data.Entities.GuildEntity", "Guild")
                        .WithOne("Preferences")
                        .HasForeignKey("AzzyBot.Data.Entities.GuildPreferencesEntity", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("AzzyBot.Data.Entities.AzuraCastEntity", b =>
                {
                    b.Navigation("Checks")
                        .IsRequired();

                    b.Navigation("Preferences")
                        .IsRequired();

                    b.Navigation("Stations");
                });

            modelBuilder.Entity("AzzyBot.Data.Entities.AzuraCastStationEntity", b =>
                {
                    b.Navigation("Checks")
                        .IsRequired();

                    b.Navigation("Preferences")
                        .IsRequired();

                    b.Navigation("Requests");
                });

            modelBuilder.Entity("AzzyBot.Data.Entities.GuildEntity", b =>
                {
                    b.Navigation("AzuraCast");

                    b.Navigation("Preferences")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
