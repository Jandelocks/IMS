using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IMS.Migrations
{
    /// <inheritdoc />
    public partial class foreign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "department",
                table: "users",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "department",
                table: "departments",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_departments_department",
                table: "departments",
                column: "department");

            migrationBuilder.CreateIndex(
                name: "IX_users_department",
                table: "users",
                column: "department");

            migrationBuilder.AddForeignKey(
                name: "FK_users_departments_department",
                table: "users",
                column: "department",
                principalTable: "departments",
                principalColumn: "department",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_departments_department",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_department",
                table: "users");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_departments_department",
                table: "departments");

            migrationBuilder.AlterColumn<string>(
                name: "department",
                table: "users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "department",
                table: "departments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
