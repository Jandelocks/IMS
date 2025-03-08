using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IMS.Migrations
{
    /// <inheritdoc />
    public partial class sec : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "incidents");

            migrationBuilder.RenameColumn(
                name: "decription",
                table: "categories",
                newName: "description");

            migrationBuilder.AddColumn<bool>(
                name: "isRistrict",
                table: "users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "attachments",
                table: "updates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "assigned_too",
                table: "incidents",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "incidents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "incident_id",
                table: "attachments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "user_id",
                table: "attachments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_updates_incident_id",
                table: "updates",
                column: "incident_id");

            migrationBuilder.CreateIndex(
                name: "IX_updates_user_id",
                table: "updates",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_incidents_user_id",
                table: "incidents",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_comments_incident_id",
                table: "comments",
                column: "incident_id");

            migrationBuilder.CreateIndex(
                name: "IX_comments_user_id",
                table: "comments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_incident_id",
                table: "attachments",
                column: "incident_id");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_user_id",
                table: "attachments",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_incidents_incident_id",
                table: "attachments",
                column: "incident_id",
                principalTable: "incidents",
                principalColumn: "incident_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_users_user_id",
                table: "attachments",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_comments_incidents_incident_id",
                table: "comments",
                column: "incident_id",
                principalTable: "incidents",
                principalColumn: "incident_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_comments_users_user_id",
                table: "comments",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_incidents_users_user_id",
                table: "incidents",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_updates_incidents_incident_id",
                table: "updates",
                column: "incident_id",
                principalTable: "incidents",
                principalColumn: "incident_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_updates_users_user_id",
                table: "updates",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attachments_incidents_incident_id",
                table: "attachments");

            migrationBuilder.DropForeignKey(
                name: "FK_attachments_users_user_id",
                table: "attachments");

            migrationBuilder.DropForeignKey(
                name: "FK_comments_incidents_incident_id",
                table: "comments");

            migrationBuilder.DropForeignKey(
                name: "FK_comments_users_user_id",
                table: "comments");

            migrationBuilder.DropForeignKey(
                name: "FK_incidents_users_user_id",
                table: "incidents");

            migrationBuilder.DropForeignKey(
                name: "FK_updates_incidents_incident_id",
                table: "updates");

            migrationBuilder.DropForeignKey(
                name: "FK_updates_users_user_id",
                table: "updates");

            migrationBuilder.DropIndex(
                name: "IX_updates_incident_id",
                table: "updates");

            migrationBuilder.DropIndex(
                name: "IX_updates_user_id",
                table: "updates");

            migrationBuilder.DropIndex(
                name: "IX_incidents_user_id",
                table: "incidents");

            migrationBuilder.DropIndex(
                name: "IX_comments_incident_id",
                table: "comments");

            migrationBuilder.DropIndex(
                name: "IX_comments_user_id",
                table: "comments");

            migrationBuilder.DropIndex(
                name: "IX_attachments_incident_id",
                table: "attachments");

            migrationBuilder.DropIndex(
                name: "IX_attachments_user_id",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "isRistrict",
                table: "users");

            migrationBuilder.DropColumn(
                name: "attachments",
                table: "updates");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "incidents");

            migrationBuilder.DropColumn(
                name: "incident_id",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "attachments");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "categories",
                newName: "decription");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "users",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "assigned_too",
                table: "incidents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "incidents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
