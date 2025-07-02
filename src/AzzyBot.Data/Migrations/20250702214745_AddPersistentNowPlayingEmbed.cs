using Microsoft.EntityFrameworkCore.Migrations;

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NowPlayingEmbedChannelId",
                table: "AzuraCastStationPreferences");

            migrationBuilder.DropColumn(
                name: "NowPlayingEmbedMessageId",
                table: "AzuraCastStationPreferences");
        }
    }
}
