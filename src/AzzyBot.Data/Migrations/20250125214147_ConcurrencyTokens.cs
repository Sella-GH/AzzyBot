using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AzzyBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConcurrencyTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Guilds",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "GuildPreferences",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "AzzyBot",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "AzuraCastStations",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "AzuraCastStationRequests",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "AzuraCastStationPreferences",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "AzuraCastStationChecks",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "AzuraCastPreferences",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "AzuraCastChecks",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "AzuraCast",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "GuildPreferences");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "AzzyBot");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "AzuraCastStations");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "AzuraCastStationRequests");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "AzuraCastStationPreferences");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "AzuraCastStationChecks");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "AzuraCastPreferences");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "AzuraCastChecks");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "AzuraCast");
        }
    }
}
