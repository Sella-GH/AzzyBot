using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AzzyBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UniqueId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ConfigSet = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AzuraCast",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BaseUrl = table.Column<string>(type: "text", nullable: false),
                    AdminApiKey = table.Column<string>(type: "text", nullable: false),
                    IsOnline = table.Column<bool>(type: "boolean", nullable: false),
                    GuildId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AzuraCast", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AzuraCast_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdminRoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    AdminNotifyChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ErrorChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    GuildId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuildPreferences_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AzuraCastChecks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServerStatus = table.Column<bool>(type: "boolean", nullable: false),
                    Updates = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatesShowChangelog = table.Column<bool>(type: "boolean", nullable: false),
                    UpdateNotificationCounter = table.Column<int>(type: "integer", nullable: false),
                    LastUpdateCheck = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AzuraCastId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AzuraCastChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AzuraCastChecks_AzuraCast_AzuraCastId",
                        column: x => x.AzuraCastId,
                        principalTable: "AzuraCast",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AzuraCastPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InstanceAdminRoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    NotificationChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    OutagesChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    AzuraCastId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AzuraCastPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AzuraCastPreferences_AzuraCast_AzuraCastId",
                        column: x => x.AzuraCastId,
                        principalTable: "AzuraCast",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AzuraCastStations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StationId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ApiKey = table.Column<string>(type: "text", nullable: false),
                    LastSkipTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AzuraCastId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AzuraCastStations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AzuraCastStations_AzuraCast_AzuraCastId",
                        column: x => x.AzuraCastId,
                        principalTable: "AzuraCast",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AzuraCastStationChecks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileChanges = table.Column<bool>(type: "boolean", nullable: false),
                    StationId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AzuraCastStationChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AzuraCastStationChecks_AzuraCastStations_StationId",
                        column: x => x.StationId,
                        principalTable: "AzuraCastStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AzuraCastStationPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileUploadChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    FileUploadPath = table.Column<string>(type: "text", nullable: false),
                    RequestsChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ShowPlaylistInNowPlaying = table.Column<bool>(type: "boolean", nullable: false),
                    StationAdminRoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    StationDjRoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    StationId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AzuraCastStationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AzuraCastStationPreferences_AzuraCastStations_StationId",
                        column: x => x.StationId,
                        principalTable: "AzuraCastStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AzuraCast_GuildId",
                table: "AzuraCast",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AzuraCastChecks_AzuraCastId",
                table: "AzuraCastChecks",
                column: "AzuraCastId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AzuraCastPreferences_AzuraCastId",
                table: "AzuraCastPreferences",
                column: "AzuraCastId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AzuraCastStationChecks_StationId",
                table: "AzuraCastStationChecks",
                column: "StationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AzuraCastStationPreferences_StationId",
                table: "AzuraCastStationPreferences",
                column: "StationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AzuraCastStations_AzuraCastId",
                table: "AzuraCastStations",
                column: "AzuraCastId");

            migrationBuilder.CreateIndex(
                name: "IX_GuildPreferences_GuildId",
                table: "GuildPreferences",
                column: "GuildId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AzuraCastChecks");

            migrationBuilder.DropTable(
                name: "AzuraCastPreferences");

            migrationBuilder.DropTable(
                name: "AzuraCastStationChecks");

            migrationBuilder.DropTable(
                name: "AzuraCastStationPreferences");

            migrationBuilder.DropTable(
                name: "GuildPreferences");

            migrationBuilder.DropTable(
                name: "AzuraCastStations");

            migrationBuilder.DropTable(
                name: "AzuraCast");

            migrationBuilder.DropTable(
                name: "Guilds");
        }
    }
}
