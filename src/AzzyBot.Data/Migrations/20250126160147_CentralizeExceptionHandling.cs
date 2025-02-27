using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AzzyBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class CentralizeExceptionHandling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorChannelId",
                table: "GuildPreferences");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ErrorChannelId",
                table: "GuildPreferences",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
