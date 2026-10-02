using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class AddBarcodeTagConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BarcodeTagConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConfigName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    TagWidthMm = table.Column<int>(type: "int", nullable: false),
                    TagHeightMm = table.Column<int>(type: "int", nullable: false),
                    FontSize = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShowBrandName = table.Column<bool>(type: "bit", nullable: false),
                    BrandName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShowSubtitle = table.Column<bool>(type: "bit", nullable: false),
                    Subtitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShowDesignNumber = table.Column<bool>(type: "bit", nullable: false),
                    DesignNumberLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShowLotNo = table.Column<bool>(type: "bit", nullable: false),
                    LotNoLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShowSerialNumber = table.Column<bool>(type: "bit", nullable: false),
                    SerialNumberLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShowComponent = table.Column<bool>(type: "bit", nullable: false),
                    ComponentLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShowColour = table.Column<bool>(type: "bit", nullable: false),
                    ColourLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShowSize = table.Column<bool>(type: "bit", nullable: false),
                    SizeLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShowBarcode = table.Column<bool>(type: "bit", nullable: false),
                    ShowBarcodeText = table.Column<bool>(type: "bit", nullable: false),
                    ShowQrCode = table.Column<bool>(type: "bit", nullable: false),
                    BarcodeHeight = table.Column<int>(type: "int", nullable: false),
                    ShowPrice = table.Column<bool>(type: "bit", nullable: false),
                    PriceLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShowCategory = table.Column<bool>(type: "bit", nullable: false),
                    CategoryLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShowHsnCode = table.Column<bool>(type: "bit", nullable: false),
                    HsnLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShowMfgDate = table.Column<bool>(type: "bit", nullable: false),
                    MfgDateLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShowCustomField1 = table.Column<bool>(type: "bit", nullable: false),
                    CustomField1Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomField1Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ShowCustomField2 = table.Column<bool>(type: "bit", nullable: false),
                    CustomField2Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomField2Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ShowCustomField3 = table.Column<bool>(type: "bit", nullable: false),
                    CustomField3Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomField3Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ShowFooterText = table.Column<bool>(type: "bit", nullable: false),
                    FooterText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DynamicCustomFieldsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarcodeTagConfigs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BarcodeTagConfigs");
        }
    }
}
