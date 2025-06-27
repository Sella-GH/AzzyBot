using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AzzyBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNowPlayingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NowPlayingChannelId",
                table: "GuildPreferences",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NowPlayingMessageId",
                table: "GuildPreferences",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NowPlayingChannelId",
                table: "GuildPreferences");

            migrationBuilder.DropColumn(
                name: "NowPlayingMessageId",
                table: "GuildPreferences");
        }
    }
}