using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyToDesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Designs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Designs_CompanyId",
                table: "Designs",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Designs_Companies_CompanyId",
                table: "Designs",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Designs_Companies_CompanyId",
                table: "Designs");

            migrationBuilder.DropIndex(
                name: "IX_Designs_CompanyId",
                table: "Designs");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Designs");
        }
    }
}
