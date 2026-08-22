using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountReceivable",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "AddDesignOnScan",
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
                name: "Pricelist",
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
                name: "TotalReceivable",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Transporter",
                table: "Vendors");

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GstNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactPerson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PinCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PanNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CustomerCompany = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Website = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PartnerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Salesperson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddDesignOnScan = table.Column<bool>(type: "bit", nullable: false),
                    SalesPaymentTerms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SalesPaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Pricelist = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeliveryMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Transporter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Distance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AccountReceivable = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AutoPostBills = table.Column<bool>(type: "bit", nullable: false),
                    CustomerInvoices = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceReport = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PeppolId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FollowUpLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FollowUpStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reminders = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NextReminder = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AccountingResponsible = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JournalItems = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Send = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalReceivable = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DaysSalesOutstanding = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PartnerLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AnalyticDistribution = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IfscCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SM1Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SM1CommissionPct = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SM2Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SM2CommissionPct = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SM3Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SM3CommissionPct = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CommissionStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CommissionEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activation = table.Column<bool>(type: "bit", nullable: false),
                    LevelWeight = table.Column<int>(type: "int", nullable: true),
                    LatestReview = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextReview = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PartnershipDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GeoLatitude = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    GeoLongitude = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    ComputeBasedOnAddress = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactRole = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerContacts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerContacts_CustomerId",
                table: "CustomerContacts",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerContacts");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.AddColumn<string>(
                name: "AccountReceivable",
                table: "Vendors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AddDesignOnScan",
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
                name: "Pricelist",
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
        }
    }
}
