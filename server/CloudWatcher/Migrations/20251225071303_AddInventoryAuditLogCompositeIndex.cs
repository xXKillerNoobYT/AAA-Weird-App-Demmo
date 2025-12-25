using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudWatcher.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryAuditLogCompositeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_InventoryAuditLogs_PartId_ChangedAt",
                table: "InventoryAuditLogs",
                columns: new[] { "PartId", "ChangedAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryAuditLogs_PartId_ChangedAt",
                table: "InventoryAuditLogs");
        }
    }
}
