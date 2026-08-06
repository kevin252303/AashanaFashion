using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDyingEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DyingEntries_ProductionOrders_ProductionOrderId",
                table: "DyingEntries");

            migrationBuilder.DropIndex(
                name: "IX_DyingEntries_ProductionOrderId",
                table: "DyingEntries");

            migrationBuilder.DropColumn(
                name: "ProductionOrderId",
                table: "DyingEntries");

            migrationBuilder.AddColumn<string>(
                name: "LotNo",
                table: "DyingEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LotNo",
                table: "DyingEntries");

            migrationBuilder.AddColumn<int>(
                name: "ProductionOrderId",
                table: "DyingEntries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DyingEntries_ProductionOrderId",
                table: "DyingEntries",
                column: "ProductionOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_DyingEntries_ProductionOrders_ProductionOrderId",
                table: "DyingEntries",
                column: "ProductionOrderId",
                principalTable: "ProductionOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
