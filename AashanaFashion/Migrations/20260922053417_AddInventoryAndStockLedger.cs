using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryAndStockLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReferenceId",
                table: "RawMaterialTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceType",
                table: "RawMaterialTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "RawMaterialTransactions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RawMaterialId",
                table: "PurchaseOrderDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderDetails_RawMaterialId",
                table: "PurchaseOrderDetails",
                column: "RawMaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderDetails_RawMaterials_RawMaterialId",
                table: "PurchaseOrderDetails",
                column: "RawMaterialId",
                principalTable: "RawMaterials",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderDetails_RawMaterials_RawMaterialId",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderDetails_RawMaterialId",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropColumn(
                name: "ReferenceId",
                table: "RawMaterialTransactions");

            migrationBuilder.DropColumn(
                name: "ReferenceType",
                table: "RawMaterialTransactions");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "RawMaterialTransactions");

            migrationBuilder.DropColumn(
                name: "RawMaterialId",
                table: "PurchaseOrderDetails");
        }
    }
}
