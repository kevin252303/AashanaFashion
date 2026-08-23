using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorToProcessTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VendorId",
                table: "ProcessTrackings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessTrackings_VendorId",
                table: "ProcessTrackings",
                column: "VendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessTrackings_Vendors_VendorId",
                table: "ProcessTrackings",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProcessTrackings_Vendors_VendorId",
                table: "ProcessTrackings");

            migrationBuilder.DropIndex(
                name: "IX_ProcessTrackings_VendorId",
                table: "ProcessTrackings");

            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "ProcessTrackings");
        }
    }
}
