using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudWatcher.Migrations
{
    /// <inheritdoc />
    public partial class FixInventoryLocationNavigationProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventory_Locations_LocationId1",
                table: "Inventory");

            migrationBuilder.DropIndex(
                name: "IX_Inventory_LocationId1",
                table: "Inventory");

            migrationBuilder.DropColumn(
                name: "LocationId1",
                table: "Inventory");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LocationId1",
                table: "Inventory",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_LocationId1",
                table: "Inventory",
                column: "LocationId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventory_Locations_LocationId1",
                table: "Inventory",
                column: "LocationId1",
                principalTable: "Locations",
                principalColumn: "Id");
        }
    }
}
