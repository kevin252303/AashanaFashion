using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorTabs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountPayable",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountReceivable",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountingResponsible",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Activation",
                table: "Vendors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AddDesignOnScan",
                table: "Vendors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AnalyticDistribution",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoPostBills",
                table: "Vendors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Box1099",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Buyer",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CommissionEndDate",
                table: "Vendors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CommissionStartDate",
                table: "Vendors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyId",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ComputeBasedOnAddress",
                table: "Vendors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CustomerInvoices",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DaysSalesOutstanding",
                table: "Vendors",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryMethod",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Distance",
                table: "Vendors",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FiscalPosition",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FollowUpLevel",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FollowUpStatus",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GeoLatitude",
                table: "Vendors",
                type: "decimal(18,8)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GeoLongitude",
                table: "Vendors",
                type: "decimal(18,8)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GroupRFQ",
                table: "Vendors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Industry",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceReport",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JournalItems",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LatestReview",
                table: "Vendors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LevelWeight",
                table: "Vendors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextReminder",
                table: "Vendors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextReview",
                table: "Vendors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartnerId",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PartnerLimit",
                table: "Vendors",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PartnershipDate",
                table: "Vendors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PeppolId",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pricelist",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchasePaymentMethod",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchasePaymentTerms",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptReminder",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reminders",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SM1CommissionPct",
                table: "Vendors",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SM1Name",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SM2CommissionPct",
                table: "Vendors",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SM2Name",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SM3CommissionPct",
                table: "Vendors",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SM3Name",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalesPaymentMethod",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalesPaymentTerms",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Salesperson",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Send",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalReceivable",
                table: "Vendors",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Transporter",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendorCompany",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VendorContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VendorId = table.Column<int>(type: "int", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactRole = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendorContacts_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorContacts_VendorId",
                table: "VendorContacts",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VendorContacts");

            migrationBuilder.DropColumn(
                name: "AccountPayable",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "AccountReceivable",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "AccountingResponsible",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Activation",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "AddDesignOnScan",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "AnalyticDistribution",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "AutoPostBills",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Box1099",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Buyer",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "CommissionEndDate",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "CommissionStartDate",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "ComputeBasedOnAddress",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "CustomerInvoices",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "DaysSalesOutstanding",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "DeliveryMethod",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Distance",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "FiscalPosition",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "FollowUpLevel",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "FollowUpStatus",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "GeoLatitude",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "GeoLongitude",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "GroupRFQ",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Industry",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "InvoiceReport",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "JournalItems",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "LatestReview",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "LevelWeight",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "NextReminder",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "NextReview",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "PartnerId",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "PartnerLimit",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "PartnershipDate",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "PeppolId",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Pricelist",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "PurchasePaymentMethod",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "PurchasePaymentTerms",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "ReceiptReminder",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Reminders",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "SM1CommissionPct",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "SM1Name",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "SM2CommissionPct",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "SM2Name",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "SM3CommissionPct",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "SM3Name",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "SalesPaymentMethod",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "SalesPaymentTerms",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Salesperson",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Send",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "TotalReceivable",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Transporter",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "VendorCompany",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Vendors");
        }
    }
}
