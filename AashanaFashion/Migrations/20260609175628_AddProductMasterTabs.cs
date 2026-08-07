using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class AddProductMasterTabs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CommonDNo",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Company",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ControlPolicy",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CustomerLeadTime",
                table: "Designs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionForDeliveryOrders",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionForInternalTransfers",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionForReceipts",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Discontinued",
                table: "Designs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EcommerceDescription",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HsnSacCode",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternalNotes",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoicingPolicy",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Designs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Designs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OutOfStockMessage",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductType",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Property1",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseDescription",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseTaxes",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityOnHand",
                table: "Designs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuotationDescription",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReInvoiceCosts",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Responsible",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ribbon",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RouteBuy",
                table: "Designs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RouteManufacture",
                table: "Designs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RouteResupplySubcontractor",
                table: "Designs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RouteResupplySubcontractorOnOrder",
                table: "Designs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SafetyFactor",
                table: "Designs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalesPrice",
                table: "Designs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SalesTaxes",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SellWhenOutOfStock",
                table: "Designs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowAvailableQty",
                table: "Designs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TrackInventory",
                table: "Designs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VisibilityOfProducts",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarningOnPurchaseOrders",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarningOnSalesOrders",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Designs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductAttributeLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DesignId = table.Column<int>(type: "int", nullable: false),
                    Attribute = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Values = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ColourCheck = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributeLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductAttributeLines_Designs_DesignId",
                        column: x => x.DesignId,
                        principalTable: "Designs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductPackagings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DesignId = table.Column<int>(type: "int", nullable: false),
                    PackagingName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPackagings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductPackagings_Designs_DesignId",
                        column: x => x.DesignId,
                        principalTable: "Designs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductPricelists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DesignId = table.Column<int>(type: "int", nullable: false),
                    Pricelist = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AppliedOn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPricelists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductPricelists_Designs_DesignId",
                        column: x => x.DesignId,
                        principalTable: "Designs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVendors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DesignId = table.Column<int>(type: "int", nullable: false),
                    VendorId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LeadTime = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVendors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVendors_Designs_DesignId",
                        column: x => x.DesignId,
                        principalTable: "Designs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductVendors_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeLines_DesignId",
                table: "ProductAttributeLines",
                column: "DesignId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPackagings_DesignId",
                table: "ProductPackagings",
                column: "DesignId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPricelists_DesignId",
                table: "ProductPricelists",
                column: "DesignId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVendors_DesignId",
                table: "ProductVendors",
                column: "DesignId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVendors_VendorId",
                table: "ProductVendors",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductAttributeLines");

            migrationBuilder.DropTable(
                name: "ProductPackagings");

            migrationBuilder.DropTable(
                name: "ProductPricelists");

            migrationBuilder.DropTable(
                name: "ProductVendors");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "CommonDNo",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "Company",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "ControlPolicy",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "CustomerLeadTime",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "DescriptionForDeliveryOrders",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "DescriptionForInternalTransfers",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "DescriptionForReceipts",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "Discontinued",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "EcommerceDescription",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "HsnSacCode",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "InternalNotes",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "InvoicingPolicy",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "OutOfStockMessage",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "Property1",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "PurchaseDescription",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "PurchaseTaxes",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "QuantityOnHand",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "QuotationDescription",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "ReInvoiceCosts",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "Responsible",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "Ribbon",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "RouteBuy",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "RouteManufacture",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "RouteResupplySubcontractor",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "RouteResupplySubcontractorOnOrder",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "SafetyFactor",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "SalesPrice",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "SalesTaxes",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "SellWhenOutOfStock",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "ShowAvailableQty",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "TrackInventory",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "VisibilityOfProducts",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "WarningOnPurchaseOrders",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "WarningOnSalesOrders",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Designs");
        }
    }
}
