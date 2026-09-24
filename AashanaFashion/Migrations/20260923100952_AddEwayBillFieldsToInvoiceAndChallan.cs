using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class AddEwayBillFieldsToInvoiceAndChallan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DistanceKm",
                table: "TaxInvoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EwayBillDate",
                table: "TaxInvoices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EwayBillNumber",
                table: "TaxInvoices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransMode",
                table: "TaxInvoices",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransporterId",
                table: "TaxInvoices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransporterName",
                table: "TaxInvoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleNumber",
                table: "TaxInvoices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "TaxInvoices",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DistanceKm",
                table: "DeliveryChallans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EwayBillDate",
                table: "DeliveryChallans",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransMode",
                table: "DeliveryChallans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransporterId",
                table: "DeliveryChallans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "DeliveryChallans",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistanceKm",
                table: "TaxInvoices");

            migrationBuilder.DropColumn(
                name: "EwayBillDate",
                table: "TaxInvoices");

            migrationBuilder.DropColumn(
                name: "EwayBillNumber",
                table: "TaxInvoices");

            migrationBuilder.DropColumn(
                name: "TransMode",
                table: "TaxInvoices");

            migrationBuilder.DropColumn(
                name: "TransporterId",
                table: "TaxInvoices");

            migrationBuilder.DropColumn(
                name: "TransporterName",
                table: "TaxInvoices");

            migrationBuilder.DropColumn(
                name: "VehicleNumber",
                table: "TaxInvoices");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "TaxInvoices");

            migrationBuilder.DropColumn(
                name: "DistanceKm",
                table: "DeliveryChallans");

            migrationBuilder.DropColumn(
                name: "EwayBillDate",
                table: "DeliveryChallans");

            migrationBuilder.DropColumn(
                name: "TransMode",
                table: "DeliveryChallans");

            migrationBuilder.DropColumn(
                name: "TransporterId",
                table: "DeliveryChallans");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "DeliveryChallans");
        }
    }
}
