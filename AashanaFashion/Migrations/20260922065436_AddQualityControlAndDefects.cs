using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class AddQualityControlAndDefects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QualityInspections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InspectionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InspectionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InspectorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductionOrderId = table.Column<int>(type: "int", nullable: false),
                    ProductionEntityId = table.Column<int>(type: "int", nullable: true),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    VendorId = table.Column<int>(type: "int", nullable: true),
                    TotalInspected = table.Column<int>(type: "int", nullable: false),
                    TotalPassed = table.Column<int>(type: "int", nullable: false),
                    TotalRework = table.Column<int>(type: "int", nullable: false),
                    TotalScrap = table.Column<int>(type: "int", nullable: false),
                    OverallResult = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityInspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityInspections_ProductionEntities_ProductionEntityId",
                        column: x => x.ProductionEntityId,
                        principalTable: "ProductionEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_QualityInspections_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QualityInspections_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "QualityDefects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QualityInspectionId = table.Column<int>(type: "int", nullable: false),
                    DefectCategory = table.Column<int>(type: "int", nullable: false),
                    DefectReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    ReworkStatus = table.Column<int>(type: "int", nullable: false),
                    ReworkAssignedTo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReworkCompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityDefects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QualityDefects_QualityInspections_QualityInspectionId",
                        column: x => x.QualityInspectionId,
                        principalTable: "QualityInspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QualityDefects_QualityInspectionId",
                table: "QualityDefects",
                column: "QualityInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_ProductionEntityId",
                table: "QualityInspections",
                column: "ProductionEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_ProductionOrderId",
                table: "QualityInspections",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspections_VendorId",
                table: "QualityInspections",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QualityDefects");

            migrationBuilder.DropTable(
                name: "QualityInspections");
        }
    }
}
