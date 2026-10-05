using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class AddBankBulkPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankPaymentBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BankFormat = table.Column<int>(type: "int", nullable: false),
                    DebitAccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DebitBankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DebitIfsc = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalBeneficiaries = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BankReferenceUtr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExportedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExportedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankPaymentBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankPaymentBatches_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BankPaymentBatchItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BankPaymentBatchId = table.Column<int>(type: "int", nullable: false),
                    BeneficiaryType = table.Column<int>(type: "int", nullable: false),
                    BeneficiaryId = table.Column<int>(type: "int", nullable: true),
                    BeneficiaryName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IfscCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SourceId = table.Column<int>(type: "int", nullable: true),
                    BeneficiaryEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BeneficiaryPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Narration = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UtrNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ValidationErrors = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankPaymentBatchItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankPaymentBatchItems_BankPaymentBatches_BankPaymentBatchId",
                        column: x => x.BankPaymentBatchId,
                        principalTable: "BankPaymentBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankPaymentBatches_CompanyId",
                table: "BankPaymentBatches",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_BankPaymentBatchItems_BankPaymentBatchId",
                table: "BankPaymentBatchItems",
                column: "BankPaymentBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankPaymentBatchItems");

            migrationBuilder.DropTable(
                name: "BankPaymentBatches");
        }
    }
}
