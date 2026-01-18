using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class modifyHashLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Otps",
                type: "char(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(64)",
                oldMaxLength: 64);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Otps",
                type: "char(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(60)",
                oldMaxLength: 60);
        }
    }
}
