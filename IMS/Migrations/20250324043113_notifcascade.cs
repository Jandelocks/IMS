using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IMS.Migrations
{
    /// <inheritdoc />
    public partial class notifcascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_users_user_id",
                table: "Notifications");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_users_user_id",
                table: "Notifications",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_users_user_id",
                table: "Notifications");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_users_user_id",
                table: "Notifications",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id");
        }
    }
}
