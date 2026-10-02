using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiCompanySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultCompanyId",
                table: "UserList",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "TaxInvoices",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "SalesReturns",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "SalesOrders",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "ReadyProductTransactions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "ReadyProducts",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "PurchaseOrders",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "ProductionOrders",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "PaymentReceipts",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "AccountingTransactions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CompanyCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Gstin = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Pan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Address1 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Address2 = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StateCode = table.Column<int>(type: "int", nullable: false),
                    PinCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankAccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BankIfsc = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    BankBranch = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpiId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AuthorizedSignatory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InvoicePrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SalesOrderPrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PurchaseOrderPrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserCompanies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCompanies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCompanies_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserCompanies_UserList_UserId",
                        column: x => x.UserId,
                        principalTable: "UserList",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Companies",
                columns: new[] { "Id", "Address1", "Address2", "AuthorizedSignatory", "BankAccountNumber", "BankBranch", "BankIfsc", "BankName", "City", "CompanyCode", "CompanyName", "CreatedDate", "Email", "Gstin", "InvoicePrefix", "IsActive", "IsDefault", "Pan", "Phone", "PinCode", "PurchaseOrderPrefix", "SalesOrderPrefix", "State", "StateCode", "UpiId" },
                values: new object[] { 1, "101-104, Surat Textile Market, Ring Road", "Ring Road", "Authorized Signatory", "50200012345678", "Ring Road Branch, Surat", "HDFC0001234", "HDFC Bank", "Surat", "AF", "Aashana Fashion", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "sales@aashanafashion.com", "24AABCA1234F1Z8", "INV-", true, true, "AABCA1234F", "+91 98765 43210", "395002", "PO-", "SO-", "Gujarat", 24, null });

            migrationBuilder.Sql("UPDATE [TaxInvoices] SET [CompanyId] = 1 WHERE [CompanyId] = 0 OR [CompanyId] IS NULL;");
            migrationBuilder.Sql("UPDATE [SalesReturns] SET [CompanyId] = 1 WHERE [CompanyId] = 0 OR [CompanyId] IS NULL;");
            migrationBuilder.Sql("UPDATE [SalesOrders] SET [CompanyId] = 1 WHERE [CompanyId] = 0 OR [CompanyId] IS NULL;");
            migrationBuilder.Sql("UPDATE [ReadyProductTransactions] SET [CompanyId] = 1 WHERE [CompanyId] = 0 OR [CompanyId] IS NULL;");
            migrationBuilder.Sql("UPDATE [ReadyProducts] SET [CompanyId] = 1 WHERE [CompanyId] = 0 OR [CompanyId] IS NULL;");
            migrationBuilder.Sql("UPDATE [PurchaseOrders] SET [CompanyId] = 1 WHERE [CompanyId] = 0 OR [CompanyId] IS NULL;");
            migrationBuilder.Sql("UPDATE [ProductionOrders] SET [CompanyId] = 1 WHERE [CompanyId] = 0 OR [CompanyId] IS NULL;");
            migrationBuilder.Sql("UPDATE [PaymentReceipts] SET [CompanyId] = 1 WHERE [CompanyId] = 0 OR [CompanyId] IS NULL;");
            migrationBuilder.Sql("UPDATE [AccountingTransactions] SET [CompanyId] = 1 WHERE [CompanyId] = 0 OR [CompanyId] IS NULL;");
            migrationBuilder.Sql("UPDATE [UserList] SET [DefaultCompanyId] = 1 WHERE [DefaultCompanyId] IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_UserList_DefaultCompanyId",
                table: "UserList",
                column: "DefaultCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxInvoices_CompanyId",
                table: "TaxInvoices",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_CompanyId",
                table: "SalesReturns",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CompanyId",
                table: "SalesOrders",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadyProductTransactions_CompanyId",
                table: "ReadyProductTransactions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadyProducts_CompanyId",
                table: "ReadyProducts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CompanyId",
                table: "PurchaseOrders",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_CompanyId",
                table: "ProductionOrders",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReceipts_CompanyId",
                table: "PaymentReceipts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingTransactions_CompanyId",
                table: "AccountingTransactions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanies_CompanyId",
                table: "UserCompanies",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanies_UserId",
                table: "UserCompanies",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountingTransactions_Companies_CompanyId",
                table: "AccountingTransactions",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentReceipts_Companies_CompanyId",
                table: "PaymentReceipts",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrders_Companies_CompanyId",
                table: "ProductionOrders",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Companies_CompanyId",
                table: "PurchaseOrders",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReadyProducts_Companies_CompanyId",
                table: "ReadyProducts",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReadyProductTransactions_Companies_CompanyId",
                table: "ReadyProductTransactions",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_Companies_CompanyId",
                table: "SalesOrders",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesReturns_Companies_CompanyId",
                table: "SalesReturns",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaxInvoices_Companies_CompanyId",
                table: "TaxInvoices",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserList_Companies_DefaultCompanyId",
                table: "UserList",
                column: "DefaultCompanyId",
                principalTable: "Companies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountingTransactions_Companies_CompanyId",
                table: "AccountingTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentReceipts_Companies_CompanyId",
                table: "PaymentReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrders_Companies_CompanyId",
                table: "ProductionOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Companies_CompanyId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ReadyProducts_Companies_CompanyId",
                table: "ReadyProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_ReadyProductTransactions_Companies_CompanyId",
                table: "ReadyProductTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_Companies_CompanyId",
                table: "SalesOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesReturns_Companies_CompanyId",
                table: "SalesReturns");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxInvoices_Companies_CompanyId",
                table: "TaxInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_UserList_Companies_DefaultCompanyId",
                table: "UserList");

            migrationBuilder.DropTable(
                name: "UserCompanies");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_UserList_DefaultCompanyId",
                table: "UserList");

            migrationBuilder.DropIndex(
                name: "IX_TaxInvoices_CompanyId",
                table: "TaxInvoices");

            migrationBuilder.DropIndex(
                name: "IX_SalesReturns_CompanyId",
                table: "SalesReturns");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_CompanyId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_ReadyProductTransactions_CompanyId",
                table: "ReadyProductTransactions");

            migrationBuilder.DropIndex(
                name: "IX_ReadyProducts_CompanyId",
                table: "ReadyProducts");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_CompanyId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_CompanyId",
                table: "ProductionOrders");

            migrationBuilder.DropIndex(
                name: "IX_PaymentReceipts_CompanyId",
                table: "PaymentReceipts");

            migrationBuilder.DropIndex(
                name: "IX_AccountingTransactions_CompanyId",
                table: "AccountingTransactions");

            migrationBuilder.DropColumn(
                name: "DefaultCompanyId",
                table: "UserList");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "TaxInvoices");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "SalesReturns");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ReadyProductTransactions");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ReadyProducts");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "PaymentReceipts");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AccountingTransactions");
        }
    }
}
