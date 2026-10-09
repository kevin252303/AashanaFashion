using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class AddManualTenantPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DeskSeatDiscountPercent",
                table: "Tenants",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DeskSeatPricePerUser",
                table: "Tenants",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FloorWorkersPriceYearly",
                table: "Tenants",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalContractAmount",
                table: "Tenants",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DeskSeatDiscountPercent", "DeskSeatPricePerUser", "FloorWorkersPriceYearly", "TotalContractAmount" },
                values: new object[] { 0m, 500m, 0m, 0m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeskSeatDiscountPercent",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DeskSeatPricePerUser",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "FloorWorkersPriceYearly",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TotalContractAmount",
                table: "Tenants");
        }
    }
}
