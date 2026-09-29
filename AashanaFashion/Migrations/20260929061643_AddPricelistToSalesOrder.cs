using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class AddPricelistToSalesOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PricelistId",
                table: "SalesOrders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_PricelistId",
                table: "SalesOrders",
                column: "PricelistId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_Pricelists_PricelistId",
                table: "SalesOrders",
                column: "PricelistId",
                principalTable: "Pricelists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_Pricelists_PricelistId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_PricelistId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "PricelistId",
                table: "SalesOrders");
        }
    }
}
