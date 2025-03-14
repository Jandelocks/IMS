using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IMS.Migrations
{
    /// <inheritdoc />
    public partial class adddepartment_idincategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "department_id",
                table: "categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_categories_department_id",
                table: "categories",
                column: "department_id");

            migrationBuilder.AddForeignKey(
                name: "FK_categories_departments_department_id",
                table: "categories",
                column: "department_id",
                principalTable: "departments",
                principalColumn: "department_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_categories_departments_department_id",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_categories_department_id",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "department_id",
                table: "categories");
        }
    }
}
