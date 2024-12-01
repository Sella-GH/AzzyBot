using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AzzyBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class IsInternalRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsInternal",
                table: "AzuraCastStationRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsInternal",
                table: "AzuraCastStationRequests");
        }
    }
}
