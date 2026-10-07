using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentNumberFormatsToCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumberFormat",
                table: "Companies",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PurchaseOrderNumberFormat",
                table: "Companies",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SalesOrderNumberFormat",
                table: "Companies",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Companies",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "InvoiceNumberFormat", "PurchaseOrderNumberFormat", "SalesOrderNumberFormat" },
                values: new object[] { "{PREFIX}{YYYY}{MM}-{0000}", "{PREFIX}{0000}", "{PREFIX}{YYYY}-{0000}" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceNumberFormat",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderNumberFormat",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "SalesOrderNumberFormat",
                table: "Companies");
        }
    }
}
