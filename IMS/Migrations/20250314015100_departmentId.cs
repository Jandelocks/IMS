using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IMS.Migrations
{
    /// <inheritdoc />
    public partial class departmentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "department_id",
                table: "incidents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_incidents_department_id",
                table: "incidents",
                column: "department_id");

            migrationBuilder.AddForeignKey(
                name: "FK_incidents_departments_department_id",
                table: "incidents",
                column: "department_id",
                principalTable: "departments",
                principalColumn: "department_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_incidents_departments_department_id",
                table: "incidents");

            migrationBuilder.DropIndex(
                name: "IX_incidents_department_id",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "department_id",
                table: "incidents");
        }
    }
}
