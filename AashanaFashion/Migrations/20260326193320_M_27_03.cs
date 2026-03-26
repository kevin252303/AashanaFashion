using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AashanaFashion.Migrations
{
    /// <inheritdoc />
    public partial class M_27_03 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "UserList");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "UserList",
                newName: "LastName");

            migrationBuilder.RenameIndex(
                name: "IX_Users_Username",
                table: "UserList",
                newName: "IX_UserList_Username");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "UserList",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactNumber",
                table: "UserList",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "UserList",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "UserList",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "UserList",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                table: "UserList",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserList",
                table: "UserList",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserList",
                table: "UserList");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "UserList");

            migrationBuilder.DropColumn(
                name: "ContactNumber",
                table: "UserList");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "UserList");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "UserList");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "UserList");

            migrationBuilder.DropColumn(
                name: "MiddleName",
                table: "UserList");

            migrationBuilder.RenameTable(
                name: "UserList",
                newName: "Users");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Users",
                newName: "FullName");

            migrationBuilder.RenameIndex(
                name: "IX_UserList_Username",
                table: "Users",
                newName: "IX_Users_Username");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");
        }
    }
}
