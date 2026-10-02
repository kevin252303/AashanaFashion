using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserList_Username",
                table: "UserList");

            migrationBuilder.DropIndex(
                name: "IX_Employees_EmployeeCode",
                table: "Employees");

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Vendors",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "VendorPayments",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "UserList",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "TaxInvoices",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SalesReturns",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SalesOrders",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SalaryRecords",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "ReadyProducts",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "RawMaterials",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "QualityInspections",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PurchaseOrders",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "ProductionOrders",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "ProductCategories",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Pricelists",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PaymentReceipts",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Journals",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "JournalEntries",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "JobSlips",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Designs",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "DeliveryChallans",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Companies",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "BiometricDevices",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AttendanceRecords",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Accounts",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AccountingTransactions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Subdomain = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BusinessName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PlanType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AllowedErpSeats = table.Column<int>(type: "int", nullable: false),
                    AllowedEmployeeRecords = table.Column<int>(type: "int", nullable: false),
                    TrialEndsAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubscriptionEndsAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdminEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CustomConnectionString = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Companies",
                keyColumn: "Id",
                keyValue: 1,
                column: "TenantId",
                value: 1);

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "AdminEmail", "AllowedEmployeeRecords", "AllowedErpSeats", "BusinessName", "CreatedAt", "CustomConnectionString", "IsActive", "Phone", "PlanType", "Status", "Subdomain", "SubscriptionEndsAt", "TrialEndsAt" },
                values: new object[] { 1, null, 50, 10, "Aashana Fashion", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, null, 2, 0, "default", null, new DateTime(2099, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.CreateIndex(
                name: "IX_UserList_TenantId_Username",
                table: "UserList",
                columns: new[] { "TenantId", "Username" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_TenantId_EmployeeCode",
                table: "Employees",
                columns: new[] { "TenantId", "EmployeeCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_TenantId",
                table: "Companies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Subdomain",
                table: "Tenants",
                column: "Subdomain",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Tenants_TenantId",
                table: "Companies",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Tenants_TenantId",
                table: "Employees",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserList_Tenants_TenantId",
                table: "UserList",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Tenants_TenantId",
                table: "Companies");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Tenants_TenantId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_UserList_Tenants_TenantId",
                table: "UserList");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_UserList_TenantId_Username",
                table: "UserList");

            migrationBuilder.DropIndex(
                name: "IX_Employees_TenantId_EmployeeCode",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Companies_TenantId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "VendorPayments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "UserList");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TaxInvoices");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SalesReturns");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SalaryRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ReadyProducts");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "QualityInspections");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ProductCategories");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Pricelists");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PaymentReceipts");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Journals");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "JobSlips");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "DeliveryChallans");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "BiometricDevices");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AccountingTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_UserList_Username",
                table: "UserList",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeCode",
                table: "Employees",
                column: "EmployeeCode",
                unique: true);
        }
    }
}
