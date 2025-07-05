using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AzzyBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersistentNowPlayingEmbed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NowPlayingEmbedChannelId",
                table: "AzuraCastStationPreferences",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NowPlayingEmbedMessageId",
                table: "AzuraCastStationPreferences",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "MusicStreaming",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NowPlayingEmbedChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    NowPlayingEmbedMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Volume = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    GuildId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicStreaming", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MusicStreaming_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MusicStreaming_GuildId",
                table: "MusicStreaming",
                column: "GuildId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MusicStreaming");

            migrationBuilder.DropColumn(
                name: "NowPlayingEmbedChannelId",
                table: "AzuraCastStationPreferences");

            migrationBuilder.DropColumn(
                name: "NowPlayingEmbedMessageId",
                table: "AzuraCastStationPreferences");
        }
    }
}
