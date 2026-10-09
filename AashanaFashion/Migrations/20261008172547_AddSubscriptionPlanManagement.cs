using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlanManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingCycle",
                table: "Tenants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionPlanId",
                table: "Tenants",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkerSlab",
                table: "Tenants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IncludedDeskSeats = table.Column<int>(type: "int", nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    YearlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ThreeYearlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FiveYearlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExtraDeskSeatPriceMonthly = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WorkerSlab10To50Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WorkerSlab50To100Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WorkerSlab100To200Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WorkerSlab200PlusPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IncludedFloorWorkers = table.Column<int>(type: "int", nullable: false),
                    EnabledModules = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPopular = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BillingCycle", "SubscriptionPlanId", "WorkerSlab" },
                values: new object[] { "Monthly", null, "10-50" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_SubscriptionPlanId",
                table: "Tenants",
                column: "SubscriptionPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_SubscriptionPlans_SubscriptionPlanId",
                table: "Tenants",
                column: "SubscriptionPlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_SubscriptionPlans_SubscriptionPlanId",
                table: "Tenants");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_SubscriptionPlanId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "BillingCycle",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlanId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "WorkerSlab",
                table: "Tenants");
        }
    }
}
