using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddenAuditActionTypeConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Users_ChangedBy",
                table: "AuditLogs");

            migrationBuilder.AlterColumn<int>(
                name: "ChangedBy",
                table: "AuditLogs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "Action",
                table: "AuditLogs",
                type: "tinyint",
                nullable: false,
                comment: "Action type: Creation=1, Modified=2, Deleted=3",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuditLogs_Action_ValidRange",
                table: "AuditLogs",
                sql: "[Action] BETWEEN 1 AND 3");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Users_ChangedBy",
                table: "AuditLogs",
                column: "ChangedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Users_ChangedBy",
                table: "AuditLogs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AuditLogs_Action_ValidRange",
                table: "AuditLogs");

            migrationBuilder.AlterColumn<int>(
                name: "ChangedBy",
                table: "AuditLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Action",
                table: "AuditLogs",
                type: "int",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldComment: "Action type: Creation=1, Modified=2, Deleted=3");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Users_ChangedBy",
                table: "AuditLogs",
                column: "ChangedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
