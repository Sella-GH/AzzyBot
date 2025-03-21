﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AzzyBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLegals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LegalsAccepted",
                table: "Guilds",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LegalsAccepted",
                table: "Guilds");
        }
    }
}
