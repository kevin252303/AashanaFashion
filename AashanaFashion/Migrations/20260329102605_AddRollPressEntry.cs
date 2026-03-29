using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class AddRollPressEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RollPressEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LotNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DesignNo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GivenDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArrivalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NumberOfPieces = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RollPressEntries", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RollPressEntries");
        }
    }
}
