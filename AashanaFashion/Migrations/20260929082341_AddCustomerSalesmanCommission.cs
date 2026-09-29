using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerSalesmanCommission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerSalesmanCommissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    SalesmanName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Basis = table.Column<int>(type: "int", nullable: false),
                    TargetValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DesignId = table.Column<int>(type: "int", nullable: true),
                    CalcType = table.Column<int>(type: "int", nullable: false),
                    CommissionRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSalesmanCommissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerSalesmanCommissions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerSalesmanCommissions_Designs_DesignId",
                        column: x => x.DesignId,
                        principalTable: "Designs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CustomerSalesmanCommissions_UserList_UserId",
                        column: x => x.UserId,
                        principalTable: "UserList",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SalesmanCommissionEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaxInvoiceId = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    SalesmanName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Basis = table.Column<int>(type: "int", nullable: false),
                    CategoryOrTarget = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SalesAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CommissionRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CalcType = table.Column<int>(type: "int", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AccountingTransactionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesmanCommissionEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesmanCommissionEntries_AccountingTransactions_AccountingTransactionId",
                        column: x => x.AccountingTransactionId,
                        principalTable: "AccountingTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesmanCommissionEntries_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SalesmanCommissionEntries_TaxInvoices_TaxInvoiceId",
                        column: x => x.TaxInvoiceId,
                        principalTable: "TaxInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSalesmanCommissions_CustomerId",
                table: "CustomerSalesmanCommissions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSalesmanCommissions_DesignId",
                table: "CustomerSalesmanCommissions",
                column: "DesignId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSalesmanCommissions_UserId",
                table: "CustomerSalesmanCommissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesmanCommissionEntries_AccountingTransactionId",
                table: "SalesmanCommissionEntries",
                column: "AccountingTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesmanCommissionEntries_CustomerId",
                table: "SalesmanCommissionEntries",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesmanCommissionEntries_TaxInvoiceId",
                table: "SalesmanCommissionEntries",
                column: "TaxInvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerSalesmanCommissions");

            migrationBuilder.DropTable(
                name: "SalesmanCommissionEntries");
        }
    }
}
