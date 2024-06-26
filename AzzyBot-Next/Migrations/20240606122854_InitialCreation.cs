﻿using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AzzyBot.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UniqueId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ErrorChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    IsDebugAllowed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ConfigSet = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AzuraCastSet = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AzuraCast",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BaseUrl = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OutagesChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    GuildId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AzuraCast", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AzuraCast_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AzuraCastStations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApiKey = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RequestsChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    PreferHls = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ShowPlaylistInNowPlaying = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AzuraCastId = table.Column<int>(type: "int", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AzuraCastChecks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FileChanges = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ServerStatus = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Updates = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UpdatesShowChangelog = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    StationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AzuraCastChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AzuraCastChecks_AzuraCastStations_StationId",
                        column: x => x.StationId,
                        principalTable: "AzuraCastStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AzuraCastMounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mount = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AzuraCastMounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AzuraCastMounts_AzuraCastStations_StationId",
                        column: x => x.StationId,
                        principalTable: "AzuraCastStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AzuraCast_GuildId",
                table: "AzuraCast",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AzuraCastChecks_StationId",
                table: "AzuraCastChecks",
                column: "StationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AzuraCastMounts_StationId",
                table: "AzuraCastMounts",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_AzuraCastStations_AzuraCastId",
                table: "AzuraCastStations",
                column: "AzuraCastId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AzuraCastChecks");

            migrationBuilder.DropTable(
                name: "AzuraCastMounts");

            migrationBuilder.DropTable(
                name: "AzuraCastStations");

            migrationBuilder.DropTable(
                name: "AzuraCast");

            migrationBuilder.DropTable(
                name: "Guilds");
        }
    }
}
