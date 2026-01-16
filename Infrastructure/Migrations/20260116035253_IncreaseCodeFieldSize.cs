using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseCodeFieldSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Otps",
                type: "char(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(44)",
                oldMaxLength: 44);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Otps",
                type: "char(44)",
                maxLength: 44,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(128)",
                oldMaxLength: 128);
        }
    }
}
