using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class AddBiometricDeviceIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeviceId",
                table: "AttendanceRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceLogId",
                table: "AttendanceRecords",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BiometricDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeviceIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApiKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeviceModel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastHeartbeat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BiometricDevices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_DeviceId",
                table: "AttendanceRecords",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_BiometricDevices_DeviceIdentifier",
                table: "BiometricDevices",
                column: "DeviceIdentifier",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_BiometricDevices_DeviceId",
                table: "AttendanceRecords",
                column: "DeviceId",
                principalTable: "BiometricDevices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_BiometricDevices_DeviceId",
                table: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "BiometricDevices");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_DeviceId",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "DeviceLogId",
                table: "AttendanceRecords");
        }
    }
}
