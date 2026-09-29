using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkersAndHandworkComponents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HandworkChaniya",
                table: "ProductionOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HandworkCholi",
                table: "ProductionOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HandworkDupatta",
                table: "ProductionOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "HandworkWorkerId",
                table: "ProductionOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StitchingWorkerId",
                table: "ProductionOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HandworkChaniya",
                table: "Designs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HandworkCholi",
                table: "Designs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HandworkDupatta",
                table: "Designs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "HandworkWorkerId",
                table: "Designs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StitchingWorkerId",
                table: "Designs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_HandworkWorkerId",
                table: "ProductionOrders",
                column: "HandworkWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_StitchingWorkerId",
                table: "ProductionOrders",
                column: "StitchingWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Designs_HandworkWorkerId",
                table: "Designs",
                column: "HandworkWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_Designs_StitchingWorkerId",
                table: "Designs",
                column: "StitchingWorkerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Designs_Vendors_HandworkWorkerId",
                table: "Designs",
                column: "HandworkWorkerId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Designs_Vendors_StitchingWorkerId",
                table: "Designs",
                column: "StitchingWorkerId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrders_Vendors_HandworkWorkerId",
                table: "ProductionOrders",
                column: "HandworkWorkerId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrders_Vendors_StitchingWorkerId",
                table: "ProductionOrders",
                column: "StitchingWorkerId",
                principalTable: "Vendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Designs_Vendors_HandworkWorkerId",
                table: "Designs");

            migrationBuilder.DropForeignKey(
                name: "FK_Designs_Vendors_StitchingWorkerId",
                table: "Designs");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrders_Vendors_HandworkWorkerId",
                table: "ProductionOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrders_Vendors_StitchingWorkerId",
                table: "ProductionOrders");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_HandworkWorkerId",
                table: "ProductionOrders");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_StitchingWorkerId",
                table: "ProductionOrders");

            migrationBuilder.DropIndex(
                name: "IX_Designs_HandworkWorkerId",
                table: "Designs");

            migrationBuilder.DropIndex(
                name: "IX_Designs_StitchingWorkerId",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "HandworkChaniya",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "HandworkCholi",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "HandworkDupatta",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "HandworkWorkerId",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "StitchingWorkerId",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "HandworkChaniya",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "HandworkCholi",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "HandworkDupatta",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "HandworkWorkerId",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "StitchingWorkerId",
                table: "Designs");
        }
    }
}
