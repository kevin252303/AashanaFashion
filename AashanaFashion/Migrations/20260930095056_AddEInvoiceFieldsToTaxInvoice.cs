using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class AddEInvoiceFieldsToTaxInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EInvoiceAckDate",
                table: "TaxInvoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EInvoiceAckNo",
                table: "TaxInvoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EInvoiceCancelDate",
                table: "TaxInvoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EInvoiceCancelReason",
                table: "TaxInvoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EInvoiceCancelRemarks",
                table: "TaxInvoices",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EInvoiceErrors",
                table: "TaxInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EInvoiceSignedInvoice",
                table: "TaxInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EInvoiceSignedQrCode",
                table: "TaxInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EInvoiceStatus",
                table: "TaxInvoices",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EInvoiceSupplyType",
                table: "TaxInvoices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Irn",
                table: "TaxInvoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EInvoiceAckDate",
                table: "TaxInvoices");

            migrationBuilder.DropColumn(
                name: "EInvoiceAckNo",
                table: "TaxInvoices");

            migrationBuilder.DropColumn(
                name: "EInvoiceCancelDate",
                table: "TaxInvoices");

            migrationBuilder.DropColumn(
                name: "EInvoiceCancelReason",
                table: "TaxInvoices");

            migrationBuilder.DropColumn(
                name: "EInvoiceCancelRemarks",
                table: "TaxInvoices");

            migrationBuilder.DropColumn(
                name: "EInvoiceErrors",
                table: "TaxInvoices");

            migrationBuilder.DropColumn(
                name: "EInvoiceSignedInvoice",
                table: "TaxInvoices");

            migrationBuilder.DropColumn(
                name: "EInvoiceSignedQrCode",
                table: "TaxInvoices");

            migrationBuilder.DropColumn(
                name: "EInvoiceStatus",
                table: "TaxInvoices");

            migrationBuilder.DropColumn(
                name: "EInvoiceSupplyType",
                table: "TaxInvoices");

            migrationBuilder.DropColumn(
                name: "Irn",
                table: "TaxInvoices");
        }
    }
}
