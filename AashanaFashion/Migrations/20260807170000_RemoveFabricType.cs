using System;
using AashanaFashion.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disabled

namespace AashanaFashion.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260807170000_RemoveFabricType")]
    public partial class RemoveFabricType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FabricType",
                table: "ProductionOrders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FabricType",
                table: "ProductionOrders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
