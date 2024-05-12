using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable

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
                    UniqueId = table.Column<ulong>(type: "bigint unsigned", nullable: false)
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
                    ApiKey = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApiUrl = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    MusicRequestsChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    OutagesChannelId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    ShowPlaylistInNowPlaying = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    GuildId = table.Column<int>(type: "int", nullable: false)
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
                    AzuraCastId = table.Column<int>(type: "int", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AzuraCastChecks");

            migrationBuilder.DropTable(
                name: "AzuraCast");

            migrationBuilder.DropTable(
                name: "Guilds");
        }
    }
}
