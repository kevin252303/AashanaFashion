using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductionOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DesignNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FabricType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalQuantity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsRawMaterialVerified = table.Column<bool>(type: "bit", nullable: false),
                    IsDyingVerified = table.Column<bool>(type: "bit", nullable: false),
                    IsHandworkVerified = table.Column<bool>(type: "bit", nullable: false),
                    IsStitchingVerified = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrders", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionOrders");
        }
    }
}
